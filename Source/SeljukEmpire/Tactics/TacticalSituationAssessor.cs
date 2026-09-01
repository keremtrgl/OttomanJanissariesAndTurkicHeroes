namespace SeljukEmpire.Tactics
{
    /// <summary>
    /// Stances TacticalSituationAssessor can direct a cavalry-type formation into. Consumed by
    /// TuranTacticMissionBehavior and ByzantineTacticMissionBehavior.
    /// </summary>
    public enum FormationStance
    {
        AdvanceAndCharge,
        HoldAndSkirmish,
        AwaitOpening,
        Regroup,
        Pursue
    }

    /// <summary>
    /// Pure, stateless decision logic shared by TuranTacticMissionBehavior and
    /// ByzantineTacticMissionBehavior. Every parameter is a plain primitive extracted from
    /// Formation.QuerySystem by the caller - this file has no TaleWorlds.* dependency anywhere,
    /// on purpose, so it can be unit tested without a running Bannerlord instance.
    /// </summary>
    public static class TacticalSituationAssessor
    {
        private const float FavorablePowerRatioThreshold = 1.0f;
        private const float BracedLineSpeedEpsilon = 0.5f;
        private const float BracedLineCompositionThreshold = 0.5f;
        private const float EnemySoftenedCasualtyThreshold = 0.15f;
        private const float AwaitOpeningTimeoutSeconds = 25f;
        private const float CavalryDisengageCasualtyThreshold = 0.35f;
        private const float CavalryDisengageCasualtyThresholdDefensive = 0.25f;
        private const float DoctrineDowngradeCasualtyThreshold = 0.40f;

        public static FormationStance AssessHorseArcherStance(
            bool hasAmmo,
            bool hasSignificantEnemyFormation,
            float enemyLocalPowerRatio)
        {
            if (hasAmmo)
            {
                return FormationStance.HoldAndSkirmish;
            }

            bool favorable = hasSignificantEnemyFormation && enemyLocalPowerRatio > FavorablePowerRatioThreshold;
            return favorable ? FormationStance.Pursue : FormationStance.Regroup;
        }

        public static FormationStance AssessShockCavalryStance(
            bool isCurrentlyCharging,
            bool hasSignificantEnemyFormation,
            float enemyMovementSpeedMaximum,
            float enemyInfantryUnitRatio,
            float enemyHasShieldUnitRatio,
            float enemyCasualtyRatio,
            float secondsSinceAwaitOpeningStarted,
            float selfCasualtyRatio,
            float selfLocalPowerRatio,
            bool isDefensivePosture)
        {
            if (isCurrentlyCharging)
            {
                float disengageThreshold = isDefensivePosture
                    ? CavalryDisengageCasualtyThresholdDefensive
                    : CavalryDisengageCasualtyThreshold;

                bool losingBadly = selfCasualtyRatio > disengageThreshold
                    && selfLocalPowerRatio <= FavorablePowerRatioThreshold;

                return losingBadly ? FormationStance.Regroup : FormationStance.AdvanceAndCharge;
            }

            bool enemyIsBracedLine = hasSignificantEnemyFormation
                && enemyMovementSpeedMaximum < BracedLineSpeedEpsilon
                && (enemyInfantryUnitRatio >= BracedLineCompositionThreshold
                    || enemyHasShieldUnitRatio >= BracedLineCompositionThreshold);

            if (!enemyIsBracedLine)
            {
                return FormationStance.AdvanceAndCharge;
            }

            bool enemySoftened = enemyCasualtyRatio > EnemySoftenedCasualtyThreshold;
            bool timedOut = secondsSinceAwaitOpeningStarted >= AwaitOpeningTimeoutSeconds;

            return (enemySoftened || timedOut) ? FormationStance.AdvanceAndCharge : FormationStance.AwaitOpening;
        }

        public static bool ShouldDowngradeToDefensiveDoctrine(float teamAverageCasualtyRatio)
        {
            return teamAverageCasualtyRatio > DoctrineDowngradeCasualtyThreshold;
        }
    }
}
