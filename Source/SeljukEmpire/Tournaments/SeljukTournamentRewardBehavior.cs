using System;
using System.Collections.Generic;
using SeljukEmpire;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SeljukEmpire.Tournaments
{
    /// <summary>
    /// Historical Seljuk Imperial Festival & Tournament Reward System.
    /// Adds a bonus mastercrafted Seljuk arm/armor/steed and imperial renown to a Seljuk-town
    /// tournament's human winner, on top of (not instead of) Native's own regular prize -
    /// the town's sultanate honors its champion in addition to the standard purse.
    /// </summary>
    public class SeljukTournamentRewardBehavior : CampaignBehaviorBase
    {
        private static readonly string[] SeljukPrizeItemIds = new[]
        {
            "seljuk_danismend_bow",
            "seljuk_turkoman_horse",
            "seljuk_eagle_shield",
            "seljuk_caka_crossbow",
            "seljuk_royal_feather_helm",
            "seljuk_ghulam_armor",
            "seljuk_noble_spiked_helm",
            "seljuk_barda_horse_armor",
            "seljuk_cavalry_boots",
            "seljuk_damascene_bracers"
        };

        public override void RegisterEvents()
        {
            CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Stateless event listener, no persistence overhead
        }

        private void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject regularPrize)
        {
            if (winner == null || town == null || !SeljukFactionUtility.IsSeljukSettlement(town.Settlement)) return;

            try
            {
                // If player is the winner, grant masterwork Seljuk prize & imperial honor
                if (winner.IsPlayerCharacter)
                {
                    ItemObject seljukPrize = GetRandomSeljukPrize();
                    if (seljukPrize != null)
                    {
                        Hero.MainHero.PartyBelongedTo?.ItemRoster?.AddToCounts(seljukPrize, 1);
                        GainRenownAction.Apply(Hero.MainHero, 25f, true);

                        InformationManager.DisplayMessage(new InformationMessage(
                            $"🏆 [Selçuklu Şenliği Zaferi] {town.Name} Meydan Turnuvası Şampiyonu Oldunuz! Sultanlık Ödülü: {seljukPrize.Name} (+25 Nam Kazandınız)!", 
                            Colors.Yellow));
                    }
                }
                else if (winner.HeroObject != null && winner.HeroObject.Clan?.Kingdom?.StringId == "kingdom_seljuks")
                {
                    // AI Seljuk champion gains renown & loyalty
                    GainRenownAction.Apply(winner.HeroObject, 15f, false);
                }
            }
            catch (Exception)
            {
                // Safe degradation
            }
        }

        private static ItemObject GetRandomSeljukPrize()
        {
            List<ItemObject> validItems = new List<ItemObject>();
            foreach (var id in SeljukPrizeItemIds)
            {
                ItemObject item = Game.Current.ObjectManager.GetObject<ItemObject>(id);
                if (item != null)
                {
                    validItems.Add(item);
                }
            }

            if (validItems.Count > 0)
            {
                int index = MBRandom.RandomInt(validItems.Count);
                return validItems[index];
            }

            return null;
        }

    }
}
