using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

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

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_isStarterPackGranted", ref _isStarterPackGranted);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                GrantStarterPackIfNewGame();
            }
            catch (Exception) { }
        }

        private void GrantStarterPackIfNewGame()
        {
            if (_isStarterPackGranted || Hero.MainHero == null) return;

            try
            {
                // MobileParty.MainParty should already exist by OnSessionLaunchedEvent, but if it
                // doesn't for some reason, don't mark the pack granted - retry next session-launch
                // instead of permanently losing the horse/bow/arrows (only the gold, which needs
                // no party, would have been granted).
                if (MobileParty.MainParty?.ItemRoster == null) return;

                // 1. Grant +2,500 Gold Dinars
                Hero.MainHero.Gold += 2500;

                // 2. Grant Asil Türkmen Savaş Atı
                var horseItem = Game.Current.ObjectManager.GetObject<ItemObject>("seljuk_turkoman_horse");
                if (horseItem != null)
                {
                    MobileParty.MainParty.ItemRoster.AddToCounts(horseItem, 1);
                }

                // 3. Grant Danişmend Kompozit Yayı
                var bowItem = Game.Current.ObjectManager.GetObject<ItemObject>("seljuk_danismend_bow");
                if (bowItem != null)
                {
                    MobileParty.MainParty.ItemRoster.AddToCounts(bowItem, 1);
                }

                // 4. Grant Zırh Delen Temren Okları
                var arrowItem = Game.Current.ObjectManager.GetObject<ItemObject>("seljuk_heavy_arrows");
                if (arrowItem != null)
                {
                    MobileParty.MainParty.ItemRoster.AddToCounts(arrowItem, 1);
                }

                MBInformationManager.AddQuickInformation(
                    new TextObject("{=seljuk_starter_notif}The Grand Seljuk Gazi Starter Pack has been credited to your inventory! (+2,500 Dinars, Noble Turkoman Horse, Composite Bow, and Armor Piercing Arrows)"));

                _isStarterPackGranted = true;
            }
            catch (Exception) { }
        }
    }
}
