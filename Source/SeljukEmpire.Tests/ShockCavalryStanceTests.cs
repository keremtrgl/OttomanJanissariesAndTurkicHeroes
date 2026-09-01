using Xunit;
using SeljukEmpire.Tactics;

namespace SeljukEmpire.Tests
{
    public class ShockCavalryStanceTests
    {
        [Fact]
        public void NotCharging_NoSignificantEnemy_Charges()
        {
            var stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: false,
                hasSignificantEnemyFormation: false,
                enemyMovementSpeedMaximum: 0f,
                enemyInfantryUnitRatio: 0f,
                enemyHasShieldUnitRatio: 0f,
                enemyCasualtyRatio: 0f,
                secondsSinceAwaitOpeningStarted: 0f,
                selfCasualtyRatio: 0f,
                selfLocalPowerRatio: 1f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AdvanceAndCharge, stance);
        }

        [Fact]
        public void NotCharging_BracedStationaryLine_FreshWait_AwaitsOpening()
        {
            var stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: false,
                hasSignificantEnemyFormation: true,
                enemyMovementSpeedMaximum: 0.1f,
                enemyInfantryUnitRatio: 0.8f,
                enemyHasShieldUnitRatio: 0.9f,
                enemyCasualtyRatio: 0.0f,
                secondsSinceAwaitOpeningStarted: 3f,
                selfCasualtyRatio: 0f,
                selfLocalPowerRatio: 1f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AwaitOpening, stance);
        }

        [Fact]
        public void NotCharging_BracedLine_ButAlreadySoftened_Charges()
        {
            var stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: false,
                hasSignificantEnemyFormation: true,
                enemyMovementSpeedMaximum: 0.1f,
                enemyInfantryUnitRatio: 0.8f,
                enemyHasShieldUnitRatio: 0.9f,
                enemyCasualtyRatio: 0.2f,
                secondsSinceAwaitOpeningStarted: 3f,
                selfCasualtyRatio: 0f,
                selfLocalPowerRatio: 1f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AdvanceAndCharge, stance);
        }

        [Fact]
        public void NotCharging_BracedLine_TimedOut_ChargesAnyway()
        {
            var stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: false,
                hasSignificantEnemyFormation: true,
                enemyMovementSpeedMaximum: 0.1f,
                enemyInfantryUnitRatio: 0.8f,
                enemyHasShieldUnitRatio: 0.9f,
                enemyCasualtyRatio: 0.0f,
                secondsSinceAwaitOpeningStarted: 26f,
                selfCasualtyRatio: 0f,
                selfLocalPowerRatio: 1f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AdvanceAndCharge, stance);
        }

        [Fact]
        public void NotCharging_EnemyFormationMoving_TreatedAsNotBraced_Charges()
        {
            var stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: false,
                hasSignificantEnemyFormation: true,
                enemyMovementSpeedMaximum: 3.5f,
                enemyInfantryUnitRatio: 0.8f,
                enemyHasShieldUnitRatio: 0.9f,
                enemyCasualtyRatio: 0.0f,
                secondsSinceAwaitOpeningStarted: 0f,
                selfCasualtyRatio: 0f,
                selfLocalPowerRatio: 1f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AdvanceAndCharge, stance);
        }

        [Fact]
        public void Charging_LowCasualties_KeepsCharging()
        {
            var stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: true,
                hasSignificantEnemyFormation: true,
                enemyMovementSpeedMaximum: 2f,
                enemyInfantryUnitRatio: 0.5f,
                enemyHasShieldUnitRatio: 0.5f,
                enemyCasualtyRatio: 0.1f,
                secondsSinceAwaitOpeningStarted: 0f,
                selfCasualtyRatio: 0.1f,
                selfLocalPowerRatio: 1.2f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AdvanceAndCharge, stance);
        }

        [Fact]
        public void Charging_HeavyCasualties_UnfavorablePower_Regroups()
        {
            var stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: true,
                hasSignificantEnemyFormation: true,
                enemyMovementSpeedMaximum: 2f,
                enemyInfantryUnitRatio: 0.5f,
                enemyHasShieldUnitRatio: 0.5f,
                enemyCasualtyRatio: 0.1f,
                secondsSinceAwaitOpeningStarted: 0f,
                selfCasualtyRatio: 0.5f,
                selfLocalPowerRatio: 0.7f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.Regroup, stance);
        }

        [Fact]
        public void Charging_HeavyCasualties_ButWinning_KeepsCharging()
        {
            // High casualties alone shouldn't pull cavalry out of a fight it's actually winning.
            var stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: true,
                hasSignificantEnemyFormation: true,
                enemyMovementSpeedMaximum: 2f,
                enemyInfantryUnitRatio: 0.5f,
                enemyHasShieldUnitRatio: 0.5f,
                enemyCasualtyRatio: 0.6f,
                secondsSinceAwaitOpeningStarted: 0f,
                selfCasualtyRatio: 0.5f,
                selfLocalPowerRatio: 1.4f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AdvanceAndCharge, stance);
        }

        [Fact]
        public void Charging_DefensivePosture_TighterThreshold_RegroupsEarlier()
        {
            // 0.28 wouldn't trip the normal 0.35 disengage threshold, but does trip the tighter
            // 0.25 threshold used when the army-wide doctrine has downgraded to defensive.
            var stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: true,
                hasSignificantEnemyFormation: true,
                enemyMovementSpeedMaximum: 2f,
                enemyInfantryUnitRatio: 0.5f,
                enemyHasShieldUnitRatio: 0.5f,
                enemyCasualtyRatio: 0.1f,
                secondsSinceAwaitOpeningStarted: 0f,
                selfCasualtyRatio: 0.28f,
                selfLocalPowerRatio: 0.9f,
                isDefensivePosture: true);

            Assert.Equal(FormationStance.Regroup, stance);
        }
    }
}
