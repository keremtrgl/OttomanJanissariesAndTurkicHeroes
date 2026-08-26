using System;
using System.Collections.Generic;
using SeljukEmpire;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
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
        private const int BASE_CARAVAN_COMPENSATION = 18500; // Average value of lost caravan cargo & troops
        private const int INVESTMENT_TIER_1 = 10000;
        private const float CLAIM_COOLDOWN_DAYS = 7f;
        private const int MIN_MEMBERS_FOR_CLAIM = 5; // Blocks farming near-empty throwaway caravans for the flat payout

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

        /// <summary>
        /// Handles Caravan destruction. If insured, Sultanate treasury immediately reimburses the player.
        /// </summary>
        private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
        {
            if (mobileParty == null || !_isPlayerCaravanInsuranceActive) return;

            // Check if destroyed party was a player-owned caravan
            if (mobileParty.IsCaravan && mobileParty.Party?.Owner == Hero.MainHero)
            {
                bool onCooldown = CampaignTime.Now - _lastInsuranceClaimTime < CampaignTime.Days(CLAIM_COOLDOWN_DAYS);
                int memberCount = mobileParty.MemberRoster?.TotalManCount ?? 0;
                if (onCooldown || memberCount < MIN_MEMBERS_FOR_CLAIM)
                {
                    return;
                }

                int compensation = BASE_CARAVAN_COMPENSATION;
                GiveGoldToPlayer(compensation);
                _lastInsuranceClaimTime = CampaignTime.Now;

                InformationManager.DisplayMessage(new InformationMessage(
                    $"🛡️ [Selçuklu Kervan Sigortası] Kervanınız vuruldu! Selçuklu Hazine-i Âmire'si zararınızı karşıladı (+{compensation:N0} Dinar ödendi)!",
                    Colors.Yellow));
            }
        }

        /// <summary>
        /// Weekly dividend payout from Silk Road capital investments (3.5% to 5.5% weekly ROI).
        /// </summary>
        private void OnWeeklyTick()
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
                    $"🪙 [İpek Yolu Kâr Payı] Kervansaray ve Liman yatırımlarınızdan haftalık kâr payı tahsil edildi (+{totalDividend:N0} Dinar)!", 
                    Colors.Green));
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
                    InformationManager.DisplayMessage(new InformationMessage("📜 [Selçuklu Kervan Sigortası] Kervanlarınız Devlet Hazinesi teminatı altına alındı!", Colors.Yellow));
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
                        
                        if (!_settlementInvestments.ContainsKey(s.StringId))
                            _settlementInvestments[s.StringId] = 0;
                        _settlementInvestments[s.StringId] += INVESTMENT_TIER_1;

                        InformationManager.DisplayMessage(new InformationMessage($"🪙 [Kâr Ortaklığı] {s.Name} kervansaray fonuna 10,000 Dinar yatırıldı! Her hafta düzenli kâr payı alacaksınız.", Colors.Green));
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
