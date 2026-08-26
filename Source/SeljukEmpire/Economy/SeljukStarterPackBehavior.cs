using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SeljukEmpire.Economy
{
    /// <summary>
    /// Grants the one-time Seljuk starter pack (gold + gear) exactly once per campaign.
    /// Must live in a CampaignBehaviorBase with SyncData: a plain bool field on the
    /// SubModule itself is not part of the save graph and resets to false every time the
    /// game process restarts, so re-loading a save used to re-grant the pack every time.
    /// </summary>
    public class SeljukStarterPackBehavior : CampaignBehaviorBase
    {
        private bool _isStarterPackGranted;
        private bool _isNewGamePending;

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_isStarterPackGranted", ref _isStarterPackGranted);
        }

        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            _isNewGamePending = true;
        }

        // Only grants once Mission.Current is null and stays null - past the intro cinematic and
        // the optional tutorial battle, both of which are Missions - so the player is actually free
        // on the map instead of mid-cutscene or mid-fight.
        private void OnTick(float dt)
        {
            if (!_isNewGamePending || _isStarterPackGranted) return;

            try
            {
                if (Mission.Current != null || Hero.MainHero == null) return;
                if (MobileParty.MainParty?.ItemRoster == null) return;

                Hero.MainHero.Gold += 1250;

                var horseItem = Game.Current.ObjectManager.GetObject<ItemObject>("seljuk_turkoman_horse");
                if (horseItem != null)
                {
                    MobileParty.MainParty.ItemRoster.AddToCounts(horseItem, 1);
                }

                var bowItem = Game.Current.ObjectManager.GetObject<ItemObject>("seljuk_danismend_bow");
                if (bowItem != null)
                {
                    MobileParty.MainParty.ItemRoster.AddToCounts(bowItem, 1);
                }

                var arrowItem = Game.Current.ObjectManager.GetObject<ItemObject>("seljuk_heavy_arrows");
                if (arrowItem != null)
                {
                    MobileParty.MainParty.ItemRoster.AddToCounts(arrowItem, 1);
                }

                MBInformationManager.AddQuickInformation(
                    new TextObject("{=seljuk_starter_notif}The Grand Seljuk Gazi Starter Pack has been credited to your inventory! (+1,250 Dinars, Noble Turkoman Horse, Composite Bow, and Armor Piercing Arrows)"));

                _isStarterPackGranted = true;
                _isNewGamePending = false;
            }
            catch (Exception) { }
        }
    }
}
