using Xunit;
using SeljukEmpire.Tactics;

namespace SeljukEmpire.Tests
{
    public class TacticalSituationAssessorTests
    {
        [Fact]
        public void HorseArcher_WithAmmo_HoldsAndSkirmishes()
        {
            var stance = TacticalSituationAssessor.AssessHorseArcherStance(
                hasAmmo: true,
                hasSignificantEnemyFormation: true,
                enemyLocalPowerRatio: 2.0f);

            Assert.Equal(FormationStance.HoldAndSkirmish, stance);
        }

        [Fact]
        public void HorseArcher_OutOfAmmo_FavorableFight_Pursues()
        {
            var stance = TacticalSituationAssessor.AssessHorseArcherStance(
                hasAmmo: false,
                hasSignificantEnemyFormation: true,
                enemyLocalPowerRatio: 1.5f);

            Assert.Equal(FormationStance.Pursue, stance);
        }

        [Fact]
        public void HorseArcher_OutOfAmmo_UnfavorableFight_Regroups()
        {
            var stance = TacticalSituationAssessor.AssessHorseArcherStance(
                hasAmmo: false,
                hasSignificantEnemyFormation: true,
                enemyLocalPowerRatio: 0.6f);

            Assert.Equal(FormationStance.Regroup, stance);
        }

        [Fact]
        public void HorseArcher_OutOfAmmo_NoSignificantEnemy_Regroups()
        {
            var stance = TacticalSituationAssessor.AssessHorseArcherStance(
                hasAmmo: false,
                hasSignificantEnemyFormation: false,
                enemyLocalPowerRatio: 0f);

            Assert.Equal(FormationStance.Regroup, stance);
        }

        [Fact]
        public void HorseArcher_OutOfAmmo_ExactlyEvenFight_Regroups()
        {
            // A tied LocalPowerRatio is treated as unfavorable - the safer default (design spec,
            // "Behavior rules" section).
            var stance = TacticalSituationAssessor.AssessHorseArcherStance(
                hasAmmo: false,
                hasSignificantEnemyFormation: true,
                enemyLocalPowerRatio: 1.0f);

            Assert.Equal(FormationStance.Regroup, stance);
        }
    }
}
