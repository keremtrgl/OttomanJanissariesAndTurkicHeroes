using System;

namespace SeljukEmpire.Recruitment
{
    /// <summary>
    /// Pure volunteer-slot rules for <see cref="SeljukRecruitmentBehavior"/> and
    /// <see cref="LatinEmpireRecruitmentBehavior"/>: which slots get replaced, and with which troop.
    /// Deliberately free of any TaleWorlds.* reference (troops are identified by string id) so the
    /// rules are unit tested in Source/SeljukEmpire.Tests without a running game.
    ///
    /// Each Get*Candidates method returns an ordered fallback chain of troop ids - the behavior
    /// places the first one that resolves to a loaded CharacterObject, so a troop removed from the
    /// XML degrades to the next-best one instead of leaving a foreign troop in the slot. Chains
    /// are static arrays: no per-slot allocation on the daily tick.
    /// </summary>
    public static class VolunteerSlotPolicy
    {
        /// <summary>Slots from this index up are Native's "higher tier" volunteer slots.</summary>
        public const int FirstHighTierSlot = 4;

        /// <summary>Minimum notable power for a village's high-tier slots to offer a tier-2 troop.</summary>
        public const float HighTierVillageNotablePower = 150f;

        public const string SeljukPeasant = "seljuk_peasant";
        public const string SeljukGhulamRecruit = "seljuk_ghulam_recruit";
        public const string AcemiJanissary = "acemi_janissary";
        public const string AzapRecruit = "azap_recruit";
        public const string OttomanScout = "ottoman_scout";

        public const string LatinRecruit = "lat2_recruit";
        public const string LatinFootman = "lat2_footman";
        public const string LatinCrossbowman = "lat2_crossbowman";
        public const string LatinSquire = "lat2_squire";

        private static readonly string[] SeljukPeasantChain = { SeljukPeasant };
        private static readonly string[] SeljukAzapChain = { AzapRecruit, SeljukPeasant };
        private static readonly string[] SeljukScoutChain = { OttomanScout, SeljukPeasant };
        private static readonly string[] SeljukGhulamChain = { SeljukGhulamRecruit, SeljukPeasant };
        private static readonly string[] SeljukJanissaryChain = { AcemiJanissary, SeljukGhulamRecruit, SeljukPeasant };

        private static readonly string[] LatinRecruitChain = { LatinRecruit };
        private static readonly string[] LatinFootmanChain = { LatinFootman, LatinRecruit };
        private static readonly string[] LatinCrossbowChain = { LatinCrossbowman, LatinSquire, LatinFootman, LatinRecruit };
        private static readonly string[] LatinSquireChain = { LatinSquire, LatinFootman, LatinRecruit };

        /// <summary>
        /// Whether a volunteer slot currently holding <paramref name="currentTroopId"/> should be
        /// overwritten with a Seljuk-tree troop. Only an occupied slot holding another tree's troop
        /// is replaced. An empty slot (null) means its volunteer was already recruited and is left
        /// for Native's own regeneration to refill at its normal pace - this used to refill empty
        /// slots too, every in-game day and on every visit, so a player could empty a town, step
        /// out, step back in and recruit the whole roster again, as often as they liked (and AI
        /// lords recruiting in Seljuk lands got a full roster back every single day).
        /// </summary>
        public static bool ShouldReplaceSeljukVolunteer(string currentTroopId)
        {
            return currentTroopId != null && !IsSeljukTreeTroop(currentTroopId);
        }

        /// <summary>Latin Empire counterpart of <see cref="ShouldReplaceSeljukVolunteer"/>.</summary>
        public static bool ShouldReplaceLatinVolunteer(string currentTroopId)
        {
            return currentTroopId != null && !IsLatinTreeTroop(currentTroopId);
        }

        public static bool IsSeljukTreeTroop(string troopId)
        {
            return troopId.StartsWith("seljuk_", StringComparison.Ordinal)
                || troopId.IndexOf("janissary", StringComparison.Ordinal) >= 0
                || troopId.IndexOf("azap", StringComparison.Ordinal) >= 0
                || troopId.IndexOf("ottoman", StringComparison.Ordinal) >= 0;
        }

        public static bool IsLatinTreeTroop(string troopId)
        {
            return troopId.StartsWith("lat2_", StringComparison.Ordinal);
        }

        /// <summary>
        /// Candidate chain for a Seljuk settlement's volunteer slot, or null if this kind of
        /// settlement's slots are left alone.
        /// Villages: tier-1 peasants, with ghulam recruits in the high-tier slots of powerful notables.
        /// Towns/castles: alternating peasants and azaps, a scout in slot 3, and janissary cadets
        /// (artisans) or ghulam recruits (everyone else) in the high-tier slots.
        /// </summary>
        public static string[] GetSeljukCandidates(bool isVillage, bool isTownOrCastle, int slotIndex, float notablePower, bool isArtisan)
        {
            if (isVillage)
            {
                return slotIndex >= FirstHighTierSlot && notablePower >= HighTierVillageNotablePower
                    ? SeljukGhulamChain
                    : SeljukPeasantChain;
            }

            if (!isTownOrCastle)
            {
                return null;
            }

            if (slotIndex <= 2)
            {
                return slotIndex % 2 == 0 ? SeljukPeasantChain : SeljukAzapChain;
            }

            if (slotIndex == 3)
            {
                return SeljukScoutChain;
            }

            return isArtisan ? SeljukJanissaryChain : SeljukGhulamChain;
        }

        /// <summary>
        /// Candidate chain for a Latin Empire settlement's volunteer slot, or null if this kind of
        /// settlement's slots are left alone.
        /// Villages: recruits, with footmen in the high-tier slots of powerful notables.
        /// Towns/castles: recruits in slots 0-2, a crossbowman in slot 3, and crossbowmen
        /// (artisans) or squires (everyone else) in the high-tier slots.
        /// </summary>
        public static string[] GetLatinCandidates(bool isVillage, bool isTownOrCastle, int slotIndex, float notablePower, bool isArtisan)
        {
            if (isVillage)
            {
                return slotIndex >= FirstHighTierSlot && notablePower >= HighTierVillageNotablePower
                    ? LatinFootmanChain
                    : LatinRecruitChain;
            }

            if (!isTownOrCastle)
            {
                return null;
            }

            if (slotIndex <= 2)
            {
                return LatinRecruitChain;
            }

            if (slotIndex == 3)
            {
                return LatinCrossbowChain;
            }

            return isArtisan ? LatinCrossbowChain : LatinSquireChain;
        }
    }
}
