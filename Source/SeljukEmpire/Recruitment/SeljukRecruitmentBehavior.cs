using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace SeljukEmpire.Recruitment
{
    /// <summary>
    /// Ensures that all notables across 34+ Seljuk settlements (villages, castles, towns)
    /// offer authentic, affordable Tier 1 Seljuk and Ottoman recruits (20-50 Dinars) in their volunteer slots.
    /// </summary>
    public class SeljukRecruitmentBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Dynamic runtime behavior
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            RefreshAllSeljukNotables();
        }

        private void OnDailyTickSettlement(Settlement settlement)
        {
            if (settlement != null && IsSeljukSettlement(settlement))
            {
                RefreshSettlementNotables(settlement);
            }
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party != null && party.IsMainParty && settlement != null && IsSeljukSettlement(settlement))
            {
                RefreshSettlementNotables(settlement);
            }
        }

        private void RefreshAllSeljukNotables()
        {
            try
            {
                if (Settlement.All == null) return;

                foreach (var settlement in Settlement.All)
                {
                    if (IsSeljukSettlement(settlement))
                    {
                        RefreshSettlementNotables(settlement);
                    }
                }
            }
            catch (Exception)
            {
                // Engine safety catch
            }
        }

        private void RefreshSettlementNotables(Settlement settlement)
        {
            if (settlement?.Notables == null) return;

            var iktaPeasant = CharacterObject.Find("seljuk_peasant") ?? Game.Current?.ObjectManager?.GetObject<CharacterObject>("seljuk_peasant");
            var gulamRecruit = CharacterObject.Find("seljuk_ghulam_recruit") ?? Game.Current?.ObjectManager?.GetObject<CharacterObject>("seljuk_ghulam_recruit");
            var acemiJanissary = CharacterObject.Find("acemi_janissary") ?? Game.Current?.ObjectManager?.GetObject<CharacterObject>("acemi_janissary");
            var azapRecruit = CharacterObject.Find("azap_recruit") ?? Game.Current?.ObjectManager?.GetObject<CharacterObject>("azap_recruit");
            var ottomanScout = CharacterObject.Find("ottoman_scout") ?? Game.Current?.ObjectManager?.GetObject<CharacterObject>("ottoman_scout");

            if (iktaPeasant == null) return;

            foreach (var notable in settlement.Notables)
            {
                if (notable == null || !notable.IsAlive || notable.VolunteerTypes == null) continue;

                for (int i = 0; i < notable.VolunteerTypes.Length; i++)
                {
                    var currentRecruit = notable.VolunteerTypes[i];
                    if (currentRecruit == null || (!currentRecruit.StringId.StartsWith("seljuk_") && !currentRecruit.StringId.Contains("janissary") && !currentRecruit.StringId.Contains("azap") && !currentRecruit.StringId.Contains("ottoman")))
                    {
                        if (settlement.IsVillage)
                        {
                            // Slot 0, 1, 2, 3: Tier 1 Seljuk Peasant (20 Dinars)
                            // Slot 4, 5 (Notable Power >= 150): Tier 2 Ghulam Recruit (50 Dinars)
                            if (i >= 4 && notable.Power >= 150f && gulamRecruit != null)
                            {
                                notable.VolunteerTypes[i] = gulamRecruit;
                            }
                            else
                            {
                                notable.VolunteerTypes[i] = iktaPeasant;
                            }
                        }
                        else if (settlement.IsTown || settlement.IsCastle)
                        {
                            // Town basic slots: Tier 1 Peasant or Tier 1 Azap (20 Dinars)
                            if (i <= 2)
                            {
                                notable.VolunteerTypes[i] = (i % 2 == 0) ? iktaPeasant : (azapRecruit ?? iktaPeasant);
                            }
                            else if (i == 3)
                            {
                                notable.VolunteerTypes[i] = (ottomanScout ?? iktaPeasant);
                            }
                            else // High tier slots (Slot 4, 5) for prominent merchants / artisans
                            {
                                if (notable.IsArtisan && acemiJanissary != null)
                                {
                                    notable.VolunteerTypes[i] = acemiJanissary;
                                }
                                else if (gulamRecruit != null)
                                {
                                    notable.VolunteerTypes[i] = gulamRecruit;
                                }
                                else
                                {
                                    notable.VolunteerTypes[i] = iktaPeasant;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool IsSeljukSettlement(Settlement settlement)
        {
            if (settlement == null) return false;

            string sid = settlement.StringId;
            if (sid.StartsWith("town_ES1") || sid.StartsWith("town_A2") ||
                sid.StartsWith("castle_ES4") || sid.StartsWith("castle_A6") || sid.StartsWith("castle_ES5") ||
                sid.StartsWith("village_ES1_") || sid.StartsWith("village_A2_") ||
                sid.StartsWith("castle_village_ES4_") || sid.StartsWith("castle_village_A6_") ||
                sid.StartsWith("castle_village_ES5_"))
            {
                return true;
            }

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
