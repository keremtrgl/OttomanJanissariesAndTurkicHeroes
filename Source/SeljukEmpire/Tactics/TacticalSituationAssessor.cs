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
        private const float MeleeDisengageCasualtyThreshold = 0.35f;
        private const float MeleeDisengageCasualtyThresholdDefensive = 0.25f;
        private const float DoctrineDowngradeCasualtyThreshold = 0.40f;
        private const float CavalryThreatRatioThreshold = 0.30f;
        private const float InfantryHoldTimeoutSeconds = 35f;

        public static FormationStance AssessHorseArcherStance(
            bool hasAmmo,
            bool hasSignificantEnemyFormation,
            float enemyLocalPowerRatio)
        {
            if (hasAmmo)
            {
                return FormationStance.HoldAndSkirmish;
            }

            // enemyLocalPowerRatio is the ENEMY formation's own view of local power (their
            // ally / their enemy, i.e. them / us) - a value ABOVE 1.0 means the enemy considers
            // itself stronger, which is unfavorable to us. Strict '<' keeps a tied ratio (1.0)
            // on the unfavorable/Regroup side, the same safe-default the existing tests pin down.
            bool favorable = hasSignificantEnemyFormation && enemyLocalPowerRatio < FavorablePowerRatioThreshold;
            return favorable ? FormationStance.Pursue : FormationStance.Regroup;
        }

        public static FormationStance AssessShockCavalryStance(
            bool isCurrentlyCharging,
            bool hasSignificantEnemyFormation,
            float enemyCurrentSpeed,
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
                    ? MeleeDisengageCasualtyThresholdDefensive
                    : MeleeDisengageCasualtyThreshold;

                bool losingBadly = selfCasualtyRatio > disengageThreshold
                    && selfLocalPowerRatio <= FavorablePowerRatioThreshold;

                return losingBadly ? FormationStance.Regroup : FormationStance.AdvanceAndCharge;
            }

            bool enemyIsBracedLine = hasSignificantEnemyFormation
                && enemyCurrentSpeed < BracedLineSpeedEpsilon
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

        public static bool ShouldFormShieldWall(
            bool hasSignificantEnemyFormation,
            float enemyCavalryUnitRatio,
            bool isUnderHeavyRangedAttack)
        {
            return (hasSignificantEnemyFormation && enemyCavalryUnitRatio >= CavalryThreatRatioThreshold)
                || isUnderHeavyRangedAttack;
        }

        public static FormationStance AssessInfantryStance(
            bool isCurrentlyAdvancing,
            bool hasSignificantEnemyFormation,
            float enemyCavalryUnitRatio,
            bool isUnderHeavyRangedAttack,
            float enemyCasualtyRatio,
            float secondsSinceHoldStarted,
            float selfCasualtyRatio,
            float selfLocalPowerRatio,
            bool isDefensivePosture)
        {
            if (isCurrentlyAdvancing)
            {
                float disengageThreshold = isDefensivePosture
                    ? MeleeDisengageCasualtyThresholdDefensive
                    : MeleeDisengageCasualtyThreshold;

                bool losingBadly = selfCasualtyRatio > disengageThreshold
                    && selfLocalPowerRatio <= FavorablePowerRatioThreshold;

                return losingBadly ? FormationStance.Regroup : FormationStance.AdvanceAndCharge;
            }

            bool shouldBrace = ShouldFormShieldWall(hasSignificantEnemyFormation, enemyCavalryUnitRatio, isUnderHeavyRangedAttack);

            if (!shouldBrace)
            {
                return FormationStance.AdvanceAndCharge;
            }

            bool enemySoftened = enemyCasualtyRatio > EnemySoftenedCasualtyThreshold;
            bool timedOut = secondsSinceHoldStarted >= InfantryHoldTimeoutSeconds;

            return (enemySoftened || timedOut) ? FormationStance.AdvanceAndCharge : FormationStance.AwaitOpening;
        }
    }
}
