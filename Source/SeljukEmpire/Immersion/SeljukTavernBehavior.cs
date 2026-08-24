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

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Transient immersion behavior
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
                        if (Hero.MainHero == null || Hero.MainHero.Gold < BARD_COST)
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
                            if (Hero.MainHero != null && Hero.MainHero.Gold >= BARD_COST)
                            {
                                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, BARD_COST, true);

                                if (MobileParty.MainParty != null)
                                {
                                    MobileParty.MainParty.RecentEventsMorale += MORALE_BONUS;
                                }

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
