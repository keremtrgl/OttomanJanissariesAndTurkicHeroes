using System;
using SeljukEmpire;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SeljukEmpire.Administration
{
    /// <summary>
    /// Historical Seljuk Atabeg & Ikta Governance System based on Nizam al-Mulk's Siyasatnama.
    /// Provides daily loyalty stabilization, grain reserves, and mentorship progression for governors and young heroes in Seljuk fiefs.
    /// </summary>
    public class SeljukAtabegTitleBehavior : CampaignBehaviorBase
    {
        private const float LoyaltyFloorTarget = 80f;
        private const float DailyLoyaltyGain = 0.75f;
        private const float FoodStocksBufferTarget = 250f;
        private const float DailyFoodGain = 1.5f;
        private const float SecurityFloorTarget = 75f;
        private const float DailySecurityGain = 0.5f;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
            CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, OnDailyTickHero);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Fully dynamic, zero save bloat
        }

        private void OnDailyTickSettlement(Settlement settlement)
        {
            Town town = settlement?.Town;
            // Town is non-null for both towns and castles; villages and hideouts have none.
            if (town == null) return;

            try
            {
                if (!SeljukFactionUtility.IsSeljukSettlement(settlement)) return;

                // 1. Loyalty Stabilizer (Adalet-i Selçukiye)
                if (town.Loyalty < LoyaltyFloorTarget)
                {
                    town.Loyalty = Math.Min(100f, town.Loyalty + DailyLoyaltyGain);
                }

                // 2. Food Stocks Buffer for Granaries (İkta Ambarları) - never past the granary's
                // own capacity: FoodStocksUpperLimit() can be below the 250 target (it depends on
                // the settlement's type and granary level), and pushing stocks over it produced a
                // store Native itself never allows.
                float foodCapacity = town.FoodStocksUpperLimit();
                if (town.FoodStocks < FoodStocksBufferTarget && town.FoodStocks < foodCapacity)
                {
                    town.FoodStocks = Math.Min(foodCapacity, town.FoodStocks + DailyFoodGain);
                }

                // 3. Security (Subaşı Nizamı)
                if (town.Security < SecurityFloorTarget)
                {
                    town.Security = Math.Min(100f, town.Security + DailySecurityGain);
                }
            }
            catch (Exception)
            {
                // Engine safety catch
            }
        }

        private void OnDailyTickHero(Hero hero)
        {
            // Fires for every hero in the world every day - cheapest, most selective check first:
            // only a sitting governor can qualify at all.
            Town governedTown = hero?.GovernorOf;
            if (governedTown == null || !hero.IsAlive || hero.IsChild) return;

            try
            {
                if (hero.Clan?.Kingdom?.StringId == SeljukFactionUtility.SeljukKingdomId
                    && SeljukFactionUtility.IsSeljukSettlement(governedTown.Settlement))
                {
                    // Daily stewardship & leadership training (Atabeylik Talimi)
                    hero.HeroDeveloper?.AddSkillXp(DefaultSkills.Steward, 35);
                    hero.HeroDeveloper?.AddSkillXp(DefaultSkills.Leadership, 25);
                    hero.HeroDeveloper?.AddSkillXp(DefaultSkills.Charm, 20);
                }
            }
            catch (Exception)
            {
                // Engine safety catch
            }
        }
    }
}
