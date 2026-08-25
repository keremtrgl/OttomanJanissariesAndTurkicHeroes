using System;
using System.Collections.Generic;
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
    /// Replaces generic tournament prizes in Seljuk towns with mastercrafted Seljuk arms, armor, and Turkoman steeds.
    /// Grants imperial renown and sultanic favor to the champion.
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
            if (winner == null || town == null || !IsSeljukTown(town.Settlement)) return;

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

        private static bool IsSeljukTown(Settlement settlement)
        {
            if (settlement == null) return false;

            // Kingdom-based check only. The previous hardcoded settlement-id whitelist
            // (town_K1/K4/K6, town_ES4, ...) predated the contiguous-territory rewrite and the
            // Byzantine reskin: town_ES4 is now the Byzantine city of Ankara, so that whitelist was
            // incorrectly granting the Seljuk imperial tournament prize in a rival kingdom's town.
            // The Kingdom check below already covers every settlement we actually own and stays
            // correct as territory changes.
            return settlement.OwnerClan?.Kingdom != null && settlement.OwnerClan.Kingdom.StringId == "kingdom_seljuks";
        }
    }
}
