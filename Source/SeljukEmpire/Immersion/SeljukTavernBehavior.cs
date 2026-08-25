using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace SeljukEmpire.Immersion
{
    /// <summary>
    /// Implements authentic Seljuk tavern bard (Ozan) performances in towns.
    /// Players can pay 100 Dinars to listen to heroic Ghazavat epics of Alp Arslan and Dede Korkut,
    /// instantly boosting army morale by +15 points.
    /// </summary>
    public class SeljukTavernBehavior : CampaignBehaviorBase
    {
        private const int BARD_COST = 100;
        private const float MORALE_BONUS = 15f;
        private const float COOLDOWN_HOURS = 24f;

        // Was never persisted (SyncData was a no-op "transient" stub) and optionLeaveType was
        // Submenu, which keeps the player on the same menu after clicking - nothing stopped
        // clicking it over and over for unlimited morale as long as gold lasted. Now gated by a
        // 24h in-game cooldown, tracked here and saved with the campaign.
        private CampaignTime _lastListenTime = CampaignTime.Zero;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_seljukTavernBardLastListenTime", ref _lastListenTime);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                // Add option to town tavern menu
                starter.AddGameMenuOption(
                    "town_backstreet",
                    "seljuk_listen_bard",
                    "{=seljuk_bard_opt}Bozkır Ozanından Gazavat Destanı Dinle (100 Dinar)",
                    gameMenuOption =>
                    {
                        gameMenuOption.optionLeaveType = GameMenuOption.LeaveType.Submenu;

                        bool onCooldown = CampaignTime.Now - _lastListenTime < CampaignTime.Hours(COOLDOWN_HOURS);
                        if (onCooldown)
                        {
                            gameMenuOption.IsEnabled = false;
                            double hoursLeft = COOLDOWN_HOURS - (CampaignTime.Now - _lastListenTime).ToHours;
                            gameMenuOption.Tooltip = new TextObject("{=seljuk_bard_cooldown}Ozan yorgun, dinlenmesi gerek ({HOURS} saat sonra tekrar gelin).")
                                .SetTextVariable("HOURS", Math.Max(1, (int)Math.Ceiling(hoursLeft)));
                        }
                        else if (Hero.MainHero == null || Hero.MainHero.Gold < BARD_COST)
                        {
                            gameMenuOption.IsEnabled = false;
                            gameMenuOption.Tooltip = new TextObject("{=seljuk_bard_no_gold}Yeterli altınınız yok (100 Dinar gerekli).");
                        }
                        return Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown;
                    },
                    gameMenuOption =>
                    {
                        try
                        {
                            bool onCooldown = CampaignTime.Now - _lastListenTime < CampaignTime.Hours(COOLDOWN_HOURS);
                            if (!onCooldown && Hero.MainHero != null && Hero.MainHero.Gold >= BARD_COST)
                            {
                                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, BARD_COST, true);

                                if (MobileParty.MainParty != null)
                                {
                                    MobileParty.MainParty.RecentEventsMorale += MORALE_BONUS;
                                }

                                _lastListenTime = CampaignTime.Now;

                                MBInformationManager.AddQuickInformation(
                                    new TextObject("{=seljuk_bard_notif}Ozanın kopuzundan dökülen Selçuklu gazavat destanı erlerin yüreğini coşturdu! (+15 Ordu Morali)"));
                            }
                        }
                        catch (Exception)
                        {
                            // Fail-safe protection
                        }
                    },
                    false,
                    3);
            }
            catch (Exception)
            {
                // Safety
            }
        }
    }
}
