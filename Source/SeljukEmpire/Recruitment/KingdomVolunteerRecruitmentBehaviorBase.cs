using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace SeljukEmpire.Recruitment
{
    /// <summary>
    /// Shared engine for "notables in kingdom X's settlements offer kingdom X's own troop tree":
    /// on session launch (every settlement), once a day per settlement, and whenever the player
    /// enters one, every notable's volunteer slot that holds another tree's troop is overwritten
    /// with the kingdom's own troop picked by <see cref="VolunteerSlotPolicy"/>. Empty slots are
    /// left to Native's own volunteer regeneration (see
    /// <see cref="VolunteerSlotPolicy.ShouldReplaceSeljukVolunteer"/> for why).
    ///
    /// Needed because Bannerlord fills volunteer slots from the settlement's *culture*, while this
    /// mod has several kingdoms sharing one Culture (and conquered settlements keep their original
    /// culture), so culture alone would hand out the wrong kingdom's troops.
    /// </summary>
    public abstract class KingdomVolunteerRecruitmentBehaviorBase : CampaignBehaviorBase
    {
        /// <summary>Kingdom.StringId whose settlements this behavior manages.</summary>
        protected abstract string KingdomId { get; }

        protected abstract bool ShouldReplaceVolunteer(string currentTroopId);

        protected abstract string[] GetCandidates(bool isVillage, bool isTownOrCastle, int slotIndex, float notablePower, bool isArtisan);

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Fully derived from live settlement ownership - nothing to persist.
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                foreach (var settlement in Settlement.All)
                {
                    if (SeljukFactionUtility.IsSettlementOfKingdom(settlement, KingdomId))
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

        private void OnDailyTickSettlement(Settlement settlement)
        {
            try
            {
                if (SeljukFactionUtility.IsSettlementOfKingdom(settlement, KingdomId))
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
                if (party != null && party.IsMainParty && SeljukFactionUtility.IsSettlementOfKingdom(settlement, KingdomId))
                {
                    RefreshSettlementNotables(settlement);
                }
            }
            catch (Exception)
            {
                // Engine safety catch - fires on every settlement visit, must never crash the game
            }
        }

        private void RefreshSettlementNotables(Settlement settlement)
        {
            if (settlement.Notables == null) return;

            bool isVillage = settlement.IsVillage;
            bool isTownOrCastle = settlement.IsTown || settlement.IsCastle;

            foreach (var notable in settlement.Notables)
            {
                CharacterObject[] volunteers = notable?.VolunteerTypes;
                if (volunteers == null || !notable.IsAlive) continue;

                for (int i = 0; i < volunteers.Length; i++)
                {
                    if (!ShouldReplaceVolunteer(volunteers[i]?.StringId)) continue;

                    CharacterObject replacement = ResolveFirst(GetCandidates(isVillage, isTownOrCastle, i, notable.Power, notable.IsArtisan));
                    if (replacement != null)
                    {
                        volunteers[i] = replacement;
                    }
                }
            }
        }

        private static CharacterObject ResolveFirst(string[] candidateTroopIds)
        {
            if (candidateTroopIds == null) return null;

            foreach (string troopId in candidateTroopIds)
            {
                CharacterObject troop = CharacterObject.Find(troopId);
                if (troop != null) return troop;
            }
            return null;
        }
    }
}
