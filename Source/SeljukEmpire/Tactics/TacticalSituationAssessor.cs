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
    }
}
