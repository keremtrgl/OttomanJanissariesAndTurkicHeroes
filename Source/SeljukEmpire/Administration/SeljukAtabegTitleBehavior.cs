using System;
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
            if (settlement == null || (!settlement.IsTown && !settlement.IsCastle)) return;

            try
            {
                // Verify Seljuk ownership
                if (IsSeljukFief(settlement))
                {
                    // 1. Loyalty Stabilizer (Adalet-i Selçukiye)
                    if (settlement.Town != null)
                    {
                        if (settlement.Town.Loyalty < 80f)
                        {
                            settlement.Town.Loyalty = Math.Min(100f, settlement.Town.Loyalty + 0.75f);
                        }

                        // 2. Food Stocks Buffer for Granaries (İkta Ambarları)
                        if (settlement.Town.FoodStocks < 250f)
                        {
                            settlement.Town.FoodStocks += 1.5f;
                        }

                        // 3. Security (Subaşı Nizamı)
                        if (settlement.Town.Security < 75f)
                        {
                            settlement.Town.Security = Math.Min(100f, settlement.Town.Security + 0.5f);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Engine safety catch
            }
        }

        private void OnDailyTickHero(Hero hero)
        {
            if (hero == null || !hero.IsAlive || hero.IsChild) return;

            try
            {
                // Mentorship experience for Seljuk governors and commanders
                if (hero.Clan?.Kingdom?.StringId == "kingdom_seljuks" && hero.GovernorOf != null)
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

        private static bool IsSeljukFief(Settlement settlement)
        {
            if (settlement == null) return false;

            // Kingdom-based check only. The previous hardcoded settlement-id whitelist
            // (town_K1/K4/K6, castle_K1/K2/K5, ...) predated the contiguous-territory rewrite and
            // the Byzantine reskin: castle_ES3 is now the Byzantine fortress of Chonae, so that
            // whitelist was incorrectly granting our daily loyalty/food/security stabilization to a
            // rival kingdom's castle. The Kingdom check below already covers every fief we actually
            // own and stays correct as territory changes.
            return settlement.OwnerClan?.Kingdom != null && settlement.OwnerClan.Kingdom.StringId == "kingdom_seljuks";
        }
    }
}
