using Xunit;
using SeljukEmpire.Tactics;

namespace SeljukEmpire.Tests
{
    public class InfantryStanceTests
    {
        [Fact]
        public void ShouldFormShieldWall_NoEnemyNoRangedAttack_False()
        {
            bool result = TacticalSituationAssessor.ShouldFormShieldWall(
                hasSignificantEnemyFormation: false,
                enemyCavalryUnitRatio: 0f,
                isUnderHeavyRangedAttack: false);

            Assert.False(result);
        }

        [Fact]
        public void ShouldFormShieldWall_CavalryHeavyEnemy_True()
        {
            bool result = TacticalSituationAssessor.ShouldFormShieldWall(
                hasSignificantEnemyFormation: true,
                enemyCavalryUnitRatio: 0.5f,
                isUnderHeavyRangedAttack: false);

            Assert.True(result);
        }

        [Fact]
        public void ShouldFormShieldWall_LowCavalryRatioNoRangedAttack_False()
        {
            bool result = TacticalSituationAssessor.ShouldFormShieldWall(
                hasSignificantEnemyFormation: true,
                enemyCavalryUnitRatio: 0.1f,
                isUnderHeavyRangedAttack: false);

            Assert.False(result);
        }

        [Fact]
        public void ShouldFormShieldWall_LowCavalryRatioButUnderRangedAttack_True()
        {
            bool result = TacticalSituationAssessor.ShouldFormShieldWall(
                hasSignificantEnemyFormation: true,
                enemyCavalryUnitRatio: 0.1f,
                isUnderHeavyRangedAttack: true);

            Assert.True(result);
        }

        [Fact]
        public void ShouldFormShieldWall_UnderRangedAttackWithNoSignificantEnemyFormation_True()
        {
            // Missile fire can land without a single "significant large enemy formation" being
            // identified (e.g. scattered skirmishers) - the ranged-attack trigger is independent
            // of hasSignificantEnemyFormation by design.
            bool result = TacticalSituationAssessor.ShouldFormShieldWall(
                hasSignificantEnemyFormation: false,
                enemyCavalryUnitRatio: 0f,
                isUnderHeavyRangedAttack: true);

            Assert.True(result);
        }

        [Fact]
        public void ShouldFormShieldWall_CavalryRatioExactlyAtThreshold_True()
        {
            // 0.30 exactly must count (inclusive '>=') - this is the same threshold value
            // EvaluateAndSelectDoctrine already uses for "cavalry heavy" elsewhere in this file's
            // sibling mission-behavior classes.
            bool result = TacticalSituationAssessor.ShouldFormShieldWall(
                hasSignificantEnemyFormation: true,
                enemyCavalryUnitRatio: 0.30f,
                isUnderHeavyRangedAttack: false);

            Assert.True(result);
        }

        [Fact]
        public void ShouldFormShieldWall_CavalryRatioJustBelowThreshold_False()
        {
            bool result = TacticalSituationAssessor.ShouldFormShieldWall(
                hasSignificantEnemyFormation: true,
                enemyCavalryUnitRatio: 0.29f,
                isUnderHeavyRangedAttack: false);

            Assert.False(result);
        }

        [Fact]
        public void NotAdvancing_NoThreat_AdvancesImmediately()
        {
            var stance = TacticalSituationAssessor.AssessInfantryStance(
                isCurrentlyAdvancing: false,
                hasSignificantEnemyFormation: true,
                enemyCavalryUnitRatio: 0.1f,
                isUnderHeavyRangedAttack: false,
                enemyCasualtyRatio: 0f,
                secondsSinceHoldStarted: 0f,
                selfCasualtyRatio: 0f,
                selfLocalPowerRatio: 1f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AdvanceAndCharge, stance);
        }

        [Fact]
        public void NotAdvancing_CavalryThreat_FreshHold_AwaitsOpening()
        {
            var stance = TacticalSituationAssessor.AssessInfantryStance(
                isCurrentlyAdvancing: false,
                hasSignificantEnemyFormation: true,
                enemyCavalryUnitRatio: 0.5f,
                isUnderHeavyRangedAttack: false,
                enemyCasualtyRatio: 0f,
                secondsSinceHoldStarted: 3f,
                selfCasualtyRatio: 0f,
                selfLocalPowerRatio: 1f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AwaitOpening, stance);
        }

        [Fact]
        public void NotAdvancing_RangedAttackThreatOnly_AwaitsOpening()
        {
            // No cavalry-heavy enemy formation identified, but we're taking missile fire - the
            // brace trigger still fires (verifies AssessInfantryStance actually delegates to
            // ShouldFormShieldWall's OR logic, not just its cavalry half).
            var stance = TacticalSituationAssessor.AssessInfantryStance(
                isCurrentlyAdvancing: false,
                hasSignificantEnemyFormation: false,
                enemyCavalryUnitRatio: 0f,
                isUnderHeavyRangedAttack: true,
                enemyCasualtyRatio: 0f,
                secondsSinceHoldStarted: 3f,
                selfCasualtyRatio: 0f,
                selfLocalPowerRatio: 1f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AwaitOpening, stance);
        }

        [Fact]
        public void NotAdvancing_CavalryThreat_EnemyAlreadySoftened_Advances()
        {
            var stance = TacticalSituationAssessor.AssessInfantryStance(
                isCurrentlyAdvancing: false,
                hasSignificantEnemyFormation: true,
                enemyCavalryUnitRatio: 0.5f,
                isUnderHeavyRangedAttack: false,
                enemyCasualtyRatio: 0.2f,
                secondsSinceHoldStarted: 3f,
                selfCasualtyRatio: 0f,
                selfLocalPowerRatio: 1f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AdvanceAndCharge, stance);
        }

        [Fact]
        public void NotAdvancing_CavalryThreat_TimedOut_AdvancesAnyway()
        {
            var stance = TacticalSituationAssessor.AssessInfantryStance(
                isCurrentlyAdvancing: false,
                hasSignificantEnemyFormation: true,
                enemyCavalryUnitRatio: 0.5f,
                isUnderHeavyRangedAttack: false,
                enemyCasualtyRatio: 0f,
                secondsSinceHoldStarted: 36f,
                selfCasualtyRatio: 0f,
                selfLocalPowerRatio: 1f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AdvanceAndCharge, stance);
        }

        [Fact]
        public void Advancing_LowCasualties_KeepsAdvancing()
        {
            var stance = TacticalSituationAssessor.AssessInfantryStance(
                isCurrentlyAdvancing: true,
                hasSignificantEnemyFormation: true,
                enemyCavalryUnitRatio: 0.1f,
                isUnderHeavyRangedAttack: false,
                enemyCasualtyRatio: 0.1f,
                secondsSinceHoldStarted: 0f,
                selfCasualtyRatio: 0.1f,
                selfLocalPowerRatio: 1.2f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AdvanceAndCharge, stance);
        }

        [Fact]
        public void Advancing_HeavyCasualties_UnfavorablePower_Regroups()
        {
            var stance = TacticalSituationAssessor.AssessInfantryStance(
                isCurrentlyAdvancing: true,
                hasSignificantEnemyFormation: true,
                enemyCavalryUnitRatio: 0.1f,
                isUnderHeavyRangedAttack: false,
                enemyCasualtyRatio: 0.1f,
                secondsSinceHoldStarted: 0f,
                selfCasualtyRatio: 0.5f,
                selfLocalPowerRatio: 0.7f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.Regroup, stance);
        }

        [Fact]
        public void Advancing_HeavyCasualties_ButWinning_KeepsAdvancing()
        {
            var stance = TacticalSituationAssessor.AssessInfantryStance(
                isCurrentlyAdvancing: true,
                hasSignificantEnemyFormation: true,
                enemyCavalryUnitRatio: 0.1f,
                isUnderHeavyRangedAttack: false,
                enemyCasualtyRatio: 0.6f,
                secondsSinceHoldStarted: 0f,
                selfCasualtyRatio: 0.5f,
                selfLocalPowerRatio: 1.4f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AdvanceAndCharge, stance);
        }

        [Fact]
        public void Advancing_DefensivePosture_TighterThreshold_RegroupsEarlier()
        {
            var stance = TacticalSituationAssessor.AssessInfantryStance(
                isCurrentlyAdvancing: true,
                hasSignificantEnemyFormation: true,
                enemyCavalryUnitRatio: 0.1f,
                isUnderHeavyRangedAttack: false,
                enemyCasualtyRatio: 0.1f,
                secondsSinceHoldStarted: 0f,
                selfCasualtyRatio: 0.28f,
                selfLocalPowerRatio: 0.9f,
                isDefensivePosture: true);

            Assert.Equal(FormationStance.Regroup, stance);
        }

        [Fact]
        public void Advancing_ExactlyAtDisengageThreshold_NormalPosture_KeepsAdvancing()
        {
            var stance = TacticalSituationAssessor.AssessInfantryStance(
                isCurrentlyAdvancing: true,
                hasSignificantEnemyFormation: true,
                enemyCavalryUnitRatio: 0.1f,
                isUnderHeavyRangedAttack: false,
                enemyCasualtyRatio: 0.1f,
                secondsSinceHoldStarted: 0f,
                selfCasualtyRatio: 0.35f,
                selfLocalPowerRatio: 0.9f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AdvanceAndCharge, stance);
        }

        [Fact]
        public void NotAdvancing_TimeoutExactlyAtThreshold_AdvancesAnyway()
        {
            var stance = TacticalSituationAssessor.AssessInfantryStance(
                isCurrentlyAdvancing: false,
                hasSignificantEnemyFormation: true,
                enemyCavalryUnitRatio: 0.5f,
                isUnderHeavyRangedAttack: false,
                enemyCasualtyRatio: 0f,
                secondsSinceHoldStarted: 35f,
                selfCasualtyRatio: 0f,
                selfLocalPowerRatio: 1f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AdvanceAndCharge, stance);
        }

        [Fact]
        public void NotAdvancing_EnemyCasualtyRatioExactlyAtSoftenedThreshold_StillAwaitsOpening()
        {
            var stance = TacticalSituationAssessor.AssessInfantryStance(
                isCurrentlyAdvancing: false,
                hasSignificantEnemyFormation: true,
                enemyCavalryUnitRatio: 0.5f,
                isUnderHeavyRangedAttack: false,
                enemyCasualtyRatio: 0.15f,
                secondsSinceHoldStarted: 3f,
                selfCasualtyRatio: 0f,
                selfLocalPowerRatio: 1f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AwaitOpening, stance);
        }
    }
}
