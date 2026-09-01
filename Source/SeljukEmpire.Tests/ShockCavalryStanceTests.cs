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

        [Fact]
        public void Charging_ExactlyAtDisengageThreshold_NormalPosture_KeepsCharging()
        {
            // selfCasualtyRatio == 0.35 exactly should NOT trip the strict '>' comparison.
            var stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: true,
                hasSignificantEnemyFormation: true,
                enemyMovementSpeedMaximum: 2f,
                enemyInfantryUnitRatio: 0.5f,
                enemyHasShieldUnitRatio: 0.5f,
                enemyCasualtyRatio: 0.1f,
                secondsSinceAwaitOpeningStarted: 0f,
                selfCasualtyRatio: 0.35f,
                selfLocalPowerRatio: 0.9f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AdvanceAndCharge, stance);
        }

        [Fact]
        public void Charging_ExactlyAtDisengageThreshold_DefensivePosture_KeepsCharging()
        {
            // selfCasualtyRatio == 0.25 exactly (the tighter defensive threshold) should NOT trip
            // the strict '>' comparison either.
            var stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: true,
                hasSignificantEnemyFormation: true,
                enemyMovementSpeedMaximum: 2f,
                enemyInfantryUnitRatio: 0.5f,
                enemyHasShieldUnitRatio: 0.5f,
                enemyCasualtyRatio: 0.1f,
                secondsSinceAwaitOpeningStarted: 0f,
                selfCasualtyRatio: 0.25f,
                selfLocalPowerRatio: 0.9f,
                isDefensivePosture: true);

            Assert.Equal(FormationStance.AdvanceAndCharge, stance);
        }

        [Fact]
        public void Charging_HeavyCasualties_ExactlyEvenPowerRatio_Regroups()
        {
            // selfLocalPowerRatio == 1.0 exactly is unfavorable (<=), so with casualties above
            // threshold this should disengage.
            var stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: true,
                hasSignificantEnemyFormation: true,
                enemyMovementSpeedMaximum: 2f,
                enemyInfantryUnitRatio: 0.5f,
                enemyHasShieldUnitRatio: 0.5f,
                enemyCasualtyRatio: 0.1f,
                secondsSinceAwaitOpeningStarted: 0f,
                selfCasualtyRatio: 0.5f,
                selfLocalPowerRatio: 1.0f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.Regroup, stance);
        }

        [Fact]
        public void NotCharging_EnemySpeedExactlyAtEpsilon_TreatedAsNotBraced_Charges()
        {
            // enemyMovementSpeedMaximum == 0.5 exactly should NOT count as stationary (strict '<').
            var stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: false,
                hasSignificantEnemyFormation: true,
                enemyMovementSpeedMaximum: 0.5f,
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
        public void NotCharging_InfantryRatioExactlyAtCompositionThreshold_CountsAsBraced_AwaitsOpening()
        {
            // enemyInfantryUnitRatio == 0.5 exactly should count (inclusive '>='), even with
            // enemyHasShieldUnitRatio below the threshold.
            var stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: false,
                hasSignificantEnemyFormation: true,
                enemyMovementSpeedMaximum: 0.1f,
                enemyInfantryUnitRatio: 0.5f,
                enemyHasShieldUnitRatio: 0.2f,
                enemyCasualtyRatio: 0.0f,
                secondsSinceAwaitOpeningStarted: 3f,
                selfCasualtyRatio: 0f,
                selfLocalPowerRatio: 1f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AwaitOpening, stance);
        }

        [Fact]
        public void NotCharging_EnemyCasualtyRatioExactlyAtSoftenedThreshold_StillAwaitsOpening()
        {
            // enemyCasualtyRatio == 0.15 exactly should NOT count as softened (strict '>').
            var stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: false,
                hasSignificantEnemyFormation: true,
                enemyMovementSpeedMaximum: 0.1f,
                enemyInfantryUnitRatio: 0.8f,
                enemyHasShieldUnitRatio: 0.9f,
                enemyCasualtyRatio: 0.15f,
                secondsSinceAwaitOpeningStarted: 3f,
                selfCasualtyRatio: 0f,
                selfLocalPowerRatio: 1f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AwaitOpening, stance);
        }

        [Fact]
        public void NotCharging_ExactlyAtTimeoutThreshold_ChargesAnyway()
        {
            // secondsSinceAwaitOpeningStarted == 25 exactly should count as timed out (inclusive '>=').
            var stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: false,
                hasSignificantEnemyFormation: true,
                enemyMovementSpeedMaximum: 0.1f,
                enemyInfantryUnitRatio: 0.8f,
                enemyHasShieldUnitRatio: 0.9f,
                enemyCasualtyRatio: 0.0f,
                secondsSinceAwaitOpeningStarted: 25f,
                selfCasualtyRatio: 0f,
                selfLocalPowerRatio: 1f,
                isDefensivePosture: false);

            Assert.Equal(FormationStance.AdvanceAndCharge, stance);
        }
    }
}
