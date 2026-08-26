using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace SeljukEmpire.Recruitment
{
    /// <summary>
    /// Kingdom.empire_w (Latin Empire) shares Culture.empire with Kingdom.empire_s (Bizans), so
    /// Culture.basic_troop (byz2_recruit) is claimed by Bizans and notables in Latin Empire
    /// settlements would otherwise recruit Byzantine troops. This overwrites VolunteerTypes in
    /// empire_w-owned settlements with the lat2_ tree, mirroring SeljukRecruitmentBehavior's
    /// approach for the same "one Culture, several kingdoms" problem.
    /// </summary>
    public class LatinEmpireRecruitmentBehavior : CampaignBehaviorBase
    {
        private const string LatinEmpireKingdomId = "empire_w";

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            RefreshAllLatinEmpireNotables();
        }

        private void OnDailyTickSettlement(Settlement settlement)
        {
            try
            {
                if (IsLatinEmpireSettlement(settlement))
                {
                    RefreshSettlementNotables(settlement);
                }
            }
            catch (Exception)
            {
                // Engine safety catch - fires once per settlement per day, must never crash the tick
            }
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            try
            {
                if (party != null && party.IsMainParty && IsLatinEmpireSettlement(settlement))
                {
                    RefreshSettlementNotables(settlement);
                }
            }
            catch (Exception)
            {
                // Engine safety catch - fires on every settlement visit, must never crash the game
            }
        }

        private static bool IsLatinEmpireSettlement(Settlement settlement)
        {
            if (settlement == null) return false;

            if (settlement.OwnerClan?.Kingdom != null && settlement.OwnerClan.Kingdom.StringId == LatinEmpireKingdomId)
            {
                return true;
            }

            if (settlement.Village?.Bound?.OwnerClan?.Kingdom != null && settlement.Village.Bound.OwnerClan.Kingdom.StringId == LatinEmpireKingdomId)
            {
                return true;
            }

            return false;
        }

        private void RefreshAllLatinEmpireNotables()
        {
            try
            {
                if (Settlement.All == null) return;

                foreach (var settlement in Settlement.All)
                {
                    if (IsLatinEmpireSettlement(settlement))
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

            var recruit = CharacterObject.Find("lat2_recruit") ?? Game.Current?.ObjectManager?.GetObject<CharacterObject>("lat2_recruit");
            var footman = CharacterObject.Find("lat2_footman") ?? Game.Current?.ObjectManager?.GetObject<CharacterObject>("lat2_footman");
            var crossbowman = CharacterObject.Find("lat2_crossbowman") ?? Game.Current?.ObjectManager?.GetObject<CharacterObject>("lat2_crossbowman");
            var squire = CharacterObject.Find("lat2_squire") ?? Game.Current?.ObjectManager?.GetObject<CharacterObject>("lat2_squire");

            if (recruit == null) return;

            foreach (var notable in settlement.Notables)
            {
                if (notable == null || !notable.IsAlive || notable.VolunteerTypes == null) continue;

                for (int i = 0; i < notable.VolunteerTypes.Length; i++)
                {
                    var currentRecruit = notable.VolunteerTypes[i];
                    if (currentRecruit == null || !currentRecruit.StringId.StartsWith("lat2_"))
                    {
                        if (settlement.IsVillage)
                        {
                            notable.VolunteerTypes[i] = (i >= 4 && notable.Power >= 150f && footman != null) ? footman : recruit;
                        }
                        else
                        {
                            if (i <= 2)
                            {
                                notable.VolunteerTypes[i] = recruit;
                            }
                            else if (i == 3 && crossbowman != null)
                            {
                                notable.VolunteerTypes[i] = crossbowman;
                            }
                            else
                            {
                                notable.VolunteerTypes[i] = (notable.IsArtisan && crossbowman != null) ? crossbowman : (squire ?? footman ?? recruit);
                            }
                        }
                    }
                }
            }
        }
    }
}
