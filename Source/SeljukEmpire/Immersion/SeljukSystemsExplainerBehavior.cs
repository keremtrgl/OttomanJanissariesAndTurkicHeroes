using System;
using SeljukEmpire;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SeljukEmpire.Immersion
{
    /// <summary>
    /// The Atabeg governance system, the Silk Road & Caravan Insurance Divan, and the tavern bard
    /// had no encyclopedia entry and no in-game explanation of any kind - players only discovered
    /// them by accident. This shows a single one-time popup the first time the player's own party
    /// enters any Seljuk-owned town, which covers both a Seljuk playthrough (the very first town
    /// entered is a Seljuk one) and any other player who later visits Seljuk territory.
    /// </summary>
    public class SeljukSystemsExplainerBehavior : CampaignBehaviorBase
    {
        private bool _hasShownSystemsIntro;

        public override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_seljukHasShownSystemsIntro", ref _hasShownSystemsIntro);
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            try
            {
                if (_hasShownSystemsIntro || party == null || !party.IsMainParty || settlement == null || !settlement.IsTown) return;
                if (!SeljukFactionUtility.IsSeljukSettlement(settlement)) return;

                _hasShownSystemsIntro = true;

                InformationManager.ShowInquiry(new InquiryData(
                    new TextObject("{=seljuk_systems_intro_title}The Great Seljuk Empire").ToString(),
                    new TextObject("{=seljuk_systems_intro_text}You have entered Seljuk lands. Three institutions set this empire apart from its neighbors:\n\nAtabeg Governance - quietly stabilizes loyalty, food stocks, and security in every Seljuk town and castle, and trains any hero serving as governor.\n\nSilk Road & Caravan Insurance Divan - found in the Trade menu of any Seljuk town, lets you insure caravans against loss and invest capital for weekly dividends.\n\nTavern Bard - in the backstreet tavern, a steppe bard recites Ghazavat epics for a modest fee, lifting your army's morale.\n\nLook for these options whenever you visit a Seljuk settlement.").ToString(),
                    true,
                    false,
                    new TextObject("{=seljuk_ok_button}OK").ToString(),
                    null,
                    null,
                    null), true);
            }
            catch (Exception)
            {
                // Engine safety catch
            }
        }
    }
}
