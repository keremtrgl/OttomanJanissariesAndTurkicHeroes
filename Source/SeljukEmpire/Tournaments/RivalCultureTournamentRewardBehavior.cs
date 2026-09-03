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
    /// Rival-Kingdom Tournament Champion Reward System.
    /// Mirrors SeljukTournamentRewardBehavior for the 7 rival kingdoms - each one's tournament
    /// winner gets a single culture-flavored champion helm (on top of Native's regular prize),
    /// on top of, not instead of, the normal purse. Before this, only Seljuk towns had any
    /// flourish; a rival-kingdom champion (including the player) got nothing but the vanilla
    /// prize even in their own capital.
    /// </summary>
    public class RivalCultureTournamentRewardBehavior : CampaignBehaviorBase
    {
        private static readonly Dictionary<string, string> KingdomChampionItemIds = new Dictionary<string, string>
        {
            { "empire_s", "byz2_champion_helm" },
            { "empire_w", "lat2_champion_helm" },
            { "aserai", "abb2_champion_helm" },
            { "sturgia", "geo2_champion_helm" },
            { "vlandia", "crus2_champion_helm" },
            { "battania", "arm2_champion_helm" },
            { "khuzait", "kk2_champion_helm" },
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
            if (winner == null || town?.Settlement?.OwnerClan?.Kingdom == null) return;

            try
            {
                string kingdomId = town.Settlement.OwnerClan.Kingdom.StringId;
                if (!KingdomChampionItemIds.TryGetValue(kingdomId, out string prizeItemId)) return;

                ItemObject prize = Game.Current.ObjectManager.GetObject<ItemObject>(prizeItemId);
                if (prize == null) return;

                if (winner.IsPlayerCharacter)
                {
                    Hero.MainHero.PartyBelongedTo?.ItemRoster?.AddToCounts(prize, 1);
                    GainRenownAction.Apply(Hero.MainHero, 20f, true);

                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=rival_tourney_victory}🏆 [Tournament Champion] You have become the Champion of {TOWN}'s Tournament! Prize: {PRIZE} (+20 Renown gained)!")
                            .SetTextVariable("TOWN", town.Name)
                            .SetTextVariable("PRIZE", prize.Name)
                            .ToString(),
                        Colors.Yellow));
                }
                else if (winner.HeroObject?.Clan?.Kingdom?.StringId == kingdomId)
                {
                    // AI champion of their own kingdom's tournament gains renown
                    GainRenownAction.Apply(winner.HeroObject, 12f, false);
                }
            }
            catch (Exception)
            {
                // Safe degradation
            }
        }
    }
}
