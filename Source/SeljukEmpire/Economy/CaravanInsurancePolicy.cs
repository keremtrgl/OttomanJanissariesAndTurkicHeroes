namespace SeljukEmpire.Economy
{
    /// <summary>
    /// Pure claim rules for <see cref="SeljukCaravanInsuranceBehavior"/>. No TaleWorlds.* reference,
    /// so the rules are unit tested in Source/SeljukEmpire.Tests without a running game.
    /// </summary>
    public static class CaravanInsurancePolicy
    {
        /// <summary>Flat reimbursement per eligible loss (average value of lost cargo and troops).</summary>
        public const int CompensationAmount = 18500;

        /// <summary>
        /// At most one claim per this many in-game days - matches the weekly dividend cadence and
        /// stops a policy from funding a "send a caravan to die, collect, repeat" loop.
        /// </summary>
        public const double ClaimCooldownDays = 7.0;

        /// <summary>Caravans smaller than this are throwaways, not real trade losses.</summary>
        public const int MinimumCrewForClaim = 5;

        /// <param name="policyActive">The player has bought the insurance policy.</param>
        /// <param name="isPlayerOwnedCaravan">The destroyed party was a caravan owned by the player.</param>
        /// <param name="wasDisbanding">The caravan was being disbanded - a voluntary loss, not an insured one.</param>
        /// <param name="daysSinceLastClaim">In-game days since the last paid claim.</param>
        /// <param name="crewAtRisk">
        /// The caravan's crew going into the fight that destroyed it, not merely its roster at the
        /// moment of destruction: by then a defeat has removed the killed and captured men, so the
        /// minimum-crew rule would otherwise reject exactly the genuine battle losses it exists to
        /// pay out on and only let through caravans that somehow died with men still aboard.
        /// </param>
        public static bool IsClaimEligible(bool policyActive, bool isPlayerOwnedCaravan, bool wasDisbanding, double daysSinceLastClaim, int crewAtRisk)
        {
            return policyActive
                && isPlayerOwnedCaravan
                && !wasDisbanding
                && daysSinceLastClaim >= ClaimCooldownDays
                && crewAtRisk >= MinimumCrewForClaim;
        }
    }
}
