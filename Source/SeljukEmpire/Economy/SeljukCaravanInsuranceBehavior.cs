using System;
using System.Collections.Generic;
using SeljukEmpire;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SeljukEmpire.Economy
{
    /// <summary>
    /// Historical Seljuk Caravan State Insurance and Silk Road Capital Investment System.
    /// Protects player caravans with imperial treasury guarantees and yields weekly trade dividends.
    /// Full save-game persistence (SyncData) and zero CPU tick overhead.
    /// </summary>
    public class SeljukCaravanInsuranceBehavior : CampaignBehaviorBase
    {
        private const int INSURANCE_POLICY_COST = 1500;
        private const int INVESTMENT_TIER_1 = 10000;

        /// <summary>A crew snapshot older than this no longer describes the fight that destroyed a caravan.</summary>
        private const float CREW_SNAPSHOT_MAX_AGE_DAYS = 1f;

        // Save-game persistent fields
        private bool _isPlayerCaravanInsuranceActive;
        private int _totalSilkRoadInvestedGold;
        private Dictionary<string, int> _settlementInvestments;

        // Was payable on every single player-caravan loss with no limit - buy the 1,500 Dinar
        // policy once, then deliberately route a bare-minimum caravan into hostile territory to
        // collect 18,500 Dinars per loss, repeatable indefinitely for pure profit. Now gated by a
        // weekly cooldown (matches this same class's own weekly dividend cadence) and a minimum
        // party size, so a policy protects real trade losses instead of funding a farming loop.
        private CampaignTime _lastInsuranceClaimTime;

        // Transient: each insured player caravan's crew when its most recent battle began - the
        // crew actually at risk (see CaravanInsurancePolicy.IsClaimEligible's crewAtRisk). Not
        // saved: a battle starts and resolves within one play session, and after a load the claim
        // simply falls back to the live roster.
        private readonly Dictionary<MobileParty, CrewSnapshot> _crewAtBattleStart = new Dictionary<MobileParty, CrewSnapshot>();

        private struct CrewSnapshot
        {
            public int Crew;
            public CampaignTime Time;
        }

        public SeljukCaravanInsuranceBehavior()
        {
            _isPlayerCaravanInsuranceActive = false;
            _totalSilkRoadInvestedGold = 0;
            _settlementInvestments = new Dictionary<string, int>();
            _lastInsuranceClaimTime = CampaignTime.Zero;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_isPlayerCaravanInsuranceActive", ref _isPlayerCaravanInsuranceActive);
            dataStore.SyncData("_totalSilkRoadInvestedGold", ref _totalSilkRoadInvestedGold);
            dataStore.SyncData("_settlementInvestments", ref _settlementInvestments);
            dataStore.SyncData("_seljukInsuranceLastClaimTime", ref _lastInsuranceClaimTime);

            if (dataStore.IsLoading && _settlementInvestments == null)
            {
                _settlementInvestments = new Dictionary<string, int>();
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddSeljukTradeMenus(starter);
        }

        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            try
            {
                if (!_isPlayerCaravanInsuranceActive) return;

                RecordCrewIfInsuredCaravan(attackerParty?.MobileParty);
                RecordCrewIfInsuredCaravan(defenderParty?.MobileParty);
            }
            catch (Exception)
            {
                // Safety: bookkeeping only, never let it disturb a battle starting
            }
        }

        private void RecordCrewIfInsuredCaravan(MobileParty party)
        {
            if (party == null || !IsPlayerCaravan(party)) return;

            _crewAtBattleStart[party] = new CrewSnapshot
            {
                Crew = party.MemberRoster?.TotalManCount ?? 0,
                Time = CampaignTime.Now
            };
        }

        private static bool IsPlayerCaravan(MobileParty party)
        {
            return party.IsCaravan && party.Party?.Owner == Hero.MainHero;
        }

        /// <summary>
        /// Handles caravan destruction. If insured, the Sultanate treasury reimburses the player.
        /// </summary>
        private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
        {
            try
            {
                if (mobileParty == null) return;

                int crewAtRisk = mobileParty.MemberRoster?.TotalManCount ?? 0;
                if (_crewAtBattleStart.TryGetValue(mobileParty, out CrewSnapshot snapshot))
                {
                    _crewAtBattleStart.Remove(mobileParty);
                    if ((CampaignTime.Now - snapshot.Time).ToDays <= CREW_SNAPSHOT_MAX_AGE_DAYS)
                    {
                        crewAtRisk = Math.Max(crewAtRisk, snapshot.Crew);
                    }
                }

                bool eligible = CaravanInsurancePolicy.IsClaimEligible(
                    policyActive: _isPlayerCaravanInsuranceActive,
                    isPlayerOwnedCaravan: IsPlayerCaravan(mobileParty),
                    wasDisbanding: mobileParty.IsDisbanding,
                    daysSinceLastClaim: (CampaignTime.Now - _lastInsuranceClaimTime).ToDays,
                    crewAtRisk: crewAtRisk);
                if (!eligible) return;

                int compensation = CaravanInsurancePolicy.CompensationAmount;
                GiveGoldToPlayer(compensation);
                _lastInsuranceClaimTime = CampaignTime.Now;

                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=seljuk_ins_claim_paid}🛡️ [Seljuk Caravan Insurance] Your caravan was struck! The Seljuk Imperial Treasury has covered your losses (+{AMOUNT} Dinars paid)!")
                        .SetTextVariable("AMOUNT", compensation.ToString("N0"))
                        .ToString(),
                    Colors.Yellow));
            }
            catch (Exception)
            {
                // Safety: never let an insurance-claim edge case crash the campaign
            }
        }

        /// <summary>
        /// Weekly dividend payout from Silk Road capital investments (3.5% to 5.5% weekly ROI).
        /// </summary>
        private void OnWeeklyTick()
        {
            PruneStaleCrewSnapshots();

            try
            {
                if (_totalSilkRoadInvestedGold <= 0 || _settlementInvestments == null || _settlementInvestments.Count == 0) return;

                int totalDividend = 0;
                foreach (var kvp in _settlementInvestments)
                {
                    Settlement settlement = Settlement.Find(kvp.Key);
                    if (settlement != null && settlement.IsTown && !settlement.IsUnderSiege
                        && SeljukFactionUtility.IsSeljukSettlement(settlement))
                    {
                        // Town prosperity modulates return on investment
                        float prosperityMultiplier = MBMath.ClampFloat(settlement.Town.Prosperity / 5000f, 0.75f, 1.4f);
                        float weeklyRoi = 0.045f * prosperityMultiplier; // Base ~4.5% weekly return
                        int payout = (int)(kvp.Value * weeklyRoi);
                        totalDividend += payout;
                    }
                }

                if (totalDividend > 0)
                {
                    GiveGoldToPlayer(totalDividend);
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=seljuk_ins_weekly_dividend}🪙 [Silk Road Dividend] Your weekly profit share from caravanserai and harbor investments has been collected (+{AMOUNT} Dinars)!")
                            .SetTextVariable("AMOUNT", totalDividend.ToString("N0"))
                            .ToString(),
                        Colors.Green));
                }
            }
            catch (Exception)
            {
                // Safety: never let a malformed investment record crash the weekly tick for everyone
            }
        }

        /// <summary>
        /// Drops snapshots of caravans that fought and survived (or were otherwise removed without
        /// passing through OnMobilePartyDestroyed), so the dictionary never outgrows the player's
        /// handful of caravans.
        /// </summary>
        private void PruneStaleCrewSnapshots()
        {
            if (_crewAtBattleStart.Count == 0) return;

            var stale = new List<MobileParty>();
            foreach (var entry in _crewAtBattleStart)
            {
                if (!entry.Key.IsActive || (CampaignTime.Now - entry.Value.Time).ToDays > CREW_SNAPSHOT_MAX_AGE_DAYS)
                {
                    stale.Add(entry.Key);
                }
            }

            foreach (var party in stale)
            {
                _crewAtBattleStart.Remove(party);
            }
        }

        private static void GiveGoldToPlayer(int amount)
        {
            try
            {
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, amount, true);
            }
            catch (Exception)
            {
                Hero.MainHero.ChangeHeroGold(amount);
            }
        }

        /// <summary>
        /// Adds historical Seljuk trade & insurance menus in Seljuk towns.
        /// </summary>
        private void AddSeljukTradeMenus(CampaignGameStarter starter)
        {
            // Root menu option in town center
            starter.AddGameMenuOption("town", "seljuk_trade_divan",
                "{=seljuk_menu_trade_divan}Visit the Seljuk Silk Road & Caravan Insurance Divan",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    Settlement s = Settlement.CurrentSettlement;
                    return s != null && s.IsTown && SeljukFactionUtility.IsSeljukSettlement(s);
                },
                args => GameMenu.SwitchToMenu("seljuk_caravan_insurance_menu"),
                false, 4);

            // Submenu
            starter.AddGameMenu("seljuk_caravan_insurance_menu",
                "{=seljuk_menu_insurance_text}Welcome to the Seljuk Caravanserai Administration and Silk Road Insurance Divan. Here, you can place your trade caravans under Imperial Treasury guarantee and invest capital into caravanserais to receive regular weekly dividends.",
                args => { });

            // Option 1: Purchase Insurance
            starter.AddGameMenuOption("seljuk_caravan_insurance_menu", "opt_buy_insurance",
                "{=seljuk_opt_buy_ins}Insure your caravans under Seljuk State Insurance (1,500 Dinars)",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    if (_isPlayerCaravanInsuranceActive)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=seljuk_tip_ins_active}Your trade caravans are already under Seljuk State Insurance protection.");
                    }
                    else if (Hero.MainHero.Gold < INSURANCE_POLICY_COST)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=seljuk_tip_no_gold}You do not have enough gold.");
                    }
                    return true;
                },
                args =>
                {
                    GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, INSURANCE_POLICY_COST, true);
                    _isPlayerCaravanInsuranceActive = true;
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=seljuk_ins_purchased}📜 [Seljuk Caravan Insurance] Your caravans are now under State Treasury guarantee!").ToString(),
                        Colors.Yellow));
                    GameMenu.SwitchToMenu("seljuk_caravan_insurance_menu");
                });

            // Option 2: Invest Capital (10,000 Dinars)
            starter.AddGameMenuOption("seljuk_caravan_insurance_menu", "opt_invest_10k",
                "{=seljuk_opt_invest_10k}Invest 10,000 Dinars into this city's Caravanserai Fund (Weekly Dividends)",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    if (Hero.MainHero.Gold < INVESTMENT_TIER_1)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=seljuk_tip_no_gold}You do not have enough gold.");
                    }
                    return true;
                },
                args =>
                {
                    Settlement s = Settlement.CurrentSettlement;
                    if (s != null)
                    {
                        GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, INVESTMENT_TIER_1, true);
                        _totalSilkRoadInvestedGold += INVESTMENT_TIER_1;

                        _settlementInvestments.TryGetValue(s.StringId, out int alreadyInvested);
                        _settlementInvestments[s.StringId] = alreadyInvested + INVESTMENT_TIER_1;

                        InformationManager.DisplayMessage(new InformationMessage(
                            new TextObject("{=seljuk_ins_invested}🪙 [Profit Partnership] 10,000 Dinars invested into {SETTLEMENT}'s caravanserai fund! You will receive regular weekly dividends.")
                                .SetTextVariable("SETTLEMENT", s.Name)
                                .ToString(),
                            Colors.Green));
                    }
                    GameMenu.SwitchToMenu("seljuk_caravan_insurance_menu");
                });

            // Option 3: Return to town
            starter.AddGameMenuOption("seljuk_caravan_insurance_menu", "opt_leave_insurance",
                "{=seljuk_opt_leave}Return to the Town Center",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                args => GameMenu.SwitchToMenu("town"));
        }

    }
}
