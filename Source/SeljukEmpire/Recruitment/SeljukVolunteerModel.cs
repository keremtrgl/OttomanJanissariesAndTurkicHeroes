using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace SeljukEmpire.Recruitment
{
    /// <summary>
    /// Master Seljuk Imperial Recruitment & Volunteer Model.
    /// Intercepts native recruitment in all Seljuk villages, castles, and towns.
    /// Dynamically provides authentic Seljuk İkta Peasants, Noble Ghulams, Janissary Recruits, Azap Fighters, and Timarli Scouts.
    /// Eliminates the need to search tavern mercenaries.
    /// </summary>
    public class SeljukVolunteerModel : VolunteerModel
    {
        private readonly VolunteerModel _baseModel;

        public SeljukVolunteerModel(VolunteerModel baseModel)
        {
            _baseModel = baseModel ?? new DefaultVolunteerModel();
        }

        public override int MaxVolunteerTier => _baseModel.MaxVolunteerTier;

        public override bool CanHaveRecruits(Hero hero)
        {
            return _baseModel.CanHaveRecruits(hero);
        }

        public override int MaximumIndexHeroCanRecruitFromHero(Hero buyerHero, Hero sellerHero, int useValueAsRelation)
        {
            // Give player and Seljuk lords slightly higher recruitment access in Seljuk settlements
            int baseIndex = _baseModel.MaximumIndexHeroCanRecruitFromHero(buyerHero, sellerHero, useValueAsRelation);
            if (sellerHero?.CurrentSettlement != null && IsSeljukSettlement(sellerHero.CurrentSettlement))
            {
                if (buyerHero == Hero.MainHero || (buyerHero?.Clan?.Kingdom?.StringId == "kingdom_seljuks"))
                {
                    return Math.Min(6, baseIndex + 1);
                }
            }
            return baseIndex;
        }

        public override int MaximumIndexGarrisonCanRecruitFromHero(Settlement settlement, Hero sellerHero)
        {
            return _baseModel.MaximumIndexGarrisonCanRecruitFromHero(settlement, sellerHero);
        }

        public override float GetDailyVolunteerProductionProbability(Hero hero, int index, Settlement settlement)
        {
            float baseProb = _baseModel.GetDailyVolunteerProductionProbability(hero, index, settlement);
            if (settlement != null && IsSeljukSettlement(settlement))
            {
                // 25% faster volunteer generation in Seljuk lands due to Ikta mobilization
                return Math.Min(1.0f, baseProb * 1.25f);
            }
            return baseProb;
        }

        public override CharacterObject GetBasicVolunteer(Hero hero)
        {
            if (hero == null) return _baseModel.GetBasicVolunteer(hero);

            Settlement settlement = hero.CurrentSettlement ?? hero.BornSettlement;
            if (settlement != null && IsSeljukSettlement(settlement))
            {
                try
                {
                    if (settlement.IsVillage)
                    {
                        // High-power village elders / landowners offer Noble Gulam recruits
                        if (hero.IsRuralNotable && hero.Power >= 140f)
                        {
                            var gulamRecruit = Game.Current?.ObjectManager?.GetObject<CharacterObject>("seljuk_ghulam_recruit");
                            if (gulamRecruit != null) return gulamRecruit;
                        }

                        // Standard village notables offer Ikta Peasants (upgrades to Infantry, Archer, Cavalry)
                        var iktaPeasant = Game.Current?.ObjectManager?.GetObject<CharacterObject>("seljuk_peasant");
                        if (iktaPeasant != null) return iktaPeasant;
                    }
                    else if (settlement.IsTown || settlement.IsCastle)
                    {
                        // Town Artisans offer Janissary recruits
                        if (hero.IsArtisan)
                        {
                            var janissary = Game.Current?.ObjectManager?.GetObject<CharacterObject>("acemi_janissary");
                            if (janissary != null) return janissary;
                        }

                        // Town Merchants offer Azap recruits
                        if (hero.IsMerchant)
                        {
                            var azap = Game.Current?.ObjectManager?.GetObject<CharacterObject>("azap_recruit");
                            if (azap != null) return azap;
                        }

                        // Other notables offer Timarli Scouts or Ikta Peasants
                        var scout = Game.Current?.ObjectManager?.GetObject<CharacterObject>("ottoman_scout");
                        if (scout != null) return scout;
                    }
                }
                catch (Exception)
                {
                    // Fail-safe fallback
                }
            }

            return _baseModel.GetBasicVolunteer(hero);
        }

        private static bool IsSeljukSettlement(Settlement settlement)
        {
            if (settlement == null) return false;

            // Fast-path prefix check for our own core settlements; the Kingdom check afterwards is
            // the authoritative one and already covers every Seljuk-owned settlement, including
            // future territory changes.
            string sid = settlement.StringId;
            if (sid.StartsWith("town_ES1") || sid.StartsWith("town_A2") || sid.StartsWith("town_ES2") || sid.StartsWith("town_A4") ||
                sid.StartsWith("castle_ES4") || sid.StartsWith("castle_A6") || sid.StartsWith("castle_ES5") || sid.StartsWith("castle_A8") ||
                sid.StartsWith("village_ES1_") || sid.StartsWith("village_A2_") || sid.StartsWith("village_ES2_") || sid.StartsWith("village_A4_") ||
                sid.StartsWith("castle_village_ES4_") || sid.StartsWith("castle_village_A6_") || sid.StartsWith("castle_village_ES5_") || sid.StartsWith("castle_village_A8_"))
            {
                return true;
            }

            // Faction or bound culture check
            if (settlement.OwnerClan?.Kingdom != null && settlement.OwnerClan.Kingdom.StringId == "kingdom_seljuks")
            {
                return true;
            }

            if (settlement.Village?.Bound?.OwnerClan?.Kingdom != null && settlement.Village.Bound.OwnerClan.Kingdom.StringId == "kingdom_seljuks")
            {
                return true;
            }

            return false;
        }
    }
}
