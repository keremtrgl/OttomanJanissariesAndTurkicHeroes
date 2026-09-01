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
    }
}
