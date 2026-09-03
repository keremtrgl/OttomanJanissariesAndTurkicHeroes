# Reactive Tactical AI for Infantry and Foot Archers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the already-merged cavalry reactive-AI fix to infantry and foot archers, so
infantry forms a shield wall only when actually facing a cavalry or missile threat (not every
battle unconditionally) and disengages a losing advance, and foot archers stop blind-charging
into melee the same way horse archers were already fixed to stop doing.

**Architecture:** Two additions to the existing `TacticalSituationAssessor` (still zero
`TaleWorlds.*` dependencies, still unit-testable without the engine): `ShouldFormShieldWall` (a
small pure helper) and `AssessInfantryStance` (same shape as the existing
`AssessShockCavalryStance`, different brace trigger). Foot archers need no new decision function
at all — they reuse the existing, already-tested `AssessHorseArcherStance` unchanged. Both mission
behaviors gain two new `Apply*Stance` methods (mirroring the existing
`ApplyHorseArcherStance`/`ApplyShockCavalryStance` pattern exactly) wired into the same three
phases cavalry already uses.

**Tech Stack:** C# / .NET (`netstandard2.0` main project, `net8.0` xunit test project — both
already exist), TaleWorlds Bannerlord modding API (`TaleWorlds.MountAndBlade`).

## Global Constraints

- Spec: `docs/superpowers/specs/2026-09-01-infantry-archer-reactive-tactical-ai-design.md` —
  every rule and threshold below is copied verbatim from it.
- `TacticalSituationAssessor.cs` must keep zero `TaleWorlds.*` references anywhere in its public
  API or implementation.
- `dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release` must report 0 warnings/0
  errors after every task that touches `Source/SeljukEmpire/`.
- `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj` must pass with pristine output
  after every task that touches `Source/SeljukEmpire.Tests/`.
- No new per-agent loops. Every new signal (`CavalryUnitRatio`, `IsUnderRangedAttack`,
  `CasualtyRatio`, `LocalPowerRatio` on a formation or its closest significant enemy formation) is
  a `Formation.QuerySystem` property Native already computes every tick.
- No new localization keys / no new `DisplayDoctrineMessage` calls.
- `python tools/verify_mod.py` must report 0 errors/0 warnings before the final commit (this pass
  touches no XML/localization, but re-run it anyway per this project's established habit).
- `CasualtyRatio` is Native's *surviving* fraction (alive / (alive+dead)), not a casualty
  fraction — every read of it anywhere in this plan is `(1f - qs.CasualtyRatio)`, never a bare
  read. This was a decompile-verified bug in the cavalry pass; do not reintroduce it here.
- `enemyLocalPowerRatio`/`selfLocalPowerRatio` semantics: a formation's own `LocalPowerRatio` is
  from *that formation's own perspective* (its ally power / its enemy power) — reading
  `closestEnemyQs.LocalPowerRatio` therefore means "how strong the enemy considers itself
  relative to us," where a value above 1.0 is unfavorable to us. Get the comparison direction
  right the first time; this was the other decompile-verified sign bug in the cavalry pass.

---

## Task 1: `ShouldFormShieldWall` (TDD)

**Files:**
- Modify: `Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs`
- Create: `Source/SeljukEmpire.Tests/InfantryStanceTests.cs`

**Interfaces:**
- Consumes: nothing new.
- Produces: `public static bool ShouldFormShieldWall(bool hasSignificantEnemyFormation, float
  enemyCavalryUnitRatio, bool isUnderHeavyRangedAttack)` — used by Task 2 (internally) and Task 3
  (mission-behavior wiring, for the Staging-phase arrangement choice).

- [ ] **Step 1: Write the failing tests**

```csharp
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
```

Save as `Source/SeljukEmpire.Tests/InfantryStanceTests.cs`.

- [ ] **Step 2: Run tests, verify they fail to compile**

Run: `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
Expected: build error — `ShouldFormShieldWall` does not exist on `TacticalSituationAssessor`.

- [ ] **Step 3: Write the minimal implementation**

In `Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs`, add this constant alongside the
existing `private const float` block (after `DoctrineDowngradeCasualtyThreshold`):

```csharp
        private const float CavalryThreatRatioThreshold = 0.30f;
```

Add this method after `ShouldDowngradeToDefensiveDoctrine` (the last method in the class):

```csharp
        public static bool ShouldFormShieldWall(
            bool hasSignificantEnemyFormation,
            float enemyCavalryUnitRatio,
            bool isUnderHeavyRangedAttack)
        {
            return (hasSignificantEnemyFormation && enemyCavalryUnitRatio >= CavalryThreatRatioThreshold)
                || isUnderHeavyRangedAttack;
        }
```

- [ ] **Step 4: Run tests, verify they pass**

Run: `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 31, Skipped: 0` (24 existing + 7 new)

- [ ] **Step 5: Commit**

```bash
git add Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs Source/SeljukEmpire.Tests/InfantryStanceTests.cs
git commit -m "Add ShouldFormShieldWall infantry-brace-trigger logic"
```

---

## Task 2: `AssessInfantryStance` (TDD)

**Files:**
- Modify: `Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs`
- Modify: `Source/SeljukEmpire.Tests/InfantryStanceTests.cs`

**Interfaces:**
- Consumes: `ShouldFormShieldWall` (Task 1), `FormationStance` enum, and the existing
  `FavorablePowerRatioThreshold`/`EnemySoftenedCasualtyThreshold` constants (unchanged values,
  reused not redefined).
- Produces: `public static FormationStance AssessInfantryStance(bool isCurrentlyAdvancing, bool
  hasSignificantEnemyFormation, float enemyCavalryUnitRatio, bool isUnderHeavyRangedAttack, float
  enemyCasualtyRatio, float secondsSinceHoldStarted, float selfCasualtyRatio, float
  selfLocalPowerRatio, bool isDefensivePosture)` — used by Task 3/4.

This task also renames two existing private constants for clarity, since they become shared
between cavalry and infantry logic in this task: `CavalryDisengageCasualtyThreshold` →
`MeleeDisengageCasualtyThreshold`, `CavalryDisengageCasualtyThresholdDefensive` →
`MeleeDisengageCasualtyThresholdDefensive`. This is a pure rename (same values, 0.35f/0.25f) — it
does not change `AssessShockCavalryStance`'s behavior, and no test references a private constant
by name, so no existing test needs to change because of it.

- [ ] **Step 1: Write the failing tests**

Append these to `Source/SeljukEmpire.Tests/InfantryStanceTests.cs`, inside the existing
`InfantryStanceTests` class (after the `ShouldFormShieldWall` tests from Task 1):

```csharp
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
```

- [ ] **Step 2: Run tests, verify they fail to compile**

Run: `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
Expected: build error — `AssessInfantryStance` does not exist.

- [ ] **Step 3: Rename the two shared constants**

In `Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs`, find:

```csharp
        private const float CavalryDisengageCasualtyThreshold = 0.35f;
        private const float CavalryDisengageCasualtyThresholdDefensive = 0.25f;
```

Replace with:

```csharp
        private const float MeleeDisengageCasualtyThreshold = 0.35f;
        private const float MeleeDisengageCasualtyThresholdDefensive = 0.25f;
```

Then find, inside `AssessShockCavalryStance`:

```csharp
                float disengageThreshold = isDefensivePosture
                    ? CavalryDisengageCasualtyThresholdDefensive
                    : CavalryDisengageCasualtyThreshold;
```

Replace with:

```csharp
                float disengageThreshold = isDefensivePosture
                    ? MeleeDisengageCasualtyThresholdDefensive
                    : MeleeDisengageCasualtyThreshold;
```

- [ ] **Step 4: Add the new constant and method**

Add this constant alongside the others (after `CavalryThreatRatioThreshold` from Task 1):

```csharp
        private const float InfantryHoldTimeoutSeconds = 35f;
```

Add this method after `ShouldFormShieldWall`:

```csharp
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
```

- [ ] **Step 5: Run tests, verify they pass**

Run: `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 43, Skipped: 0` (31 from Task 1 + 12 new)

- [ ] **Step 6: Commit**

```bash
git add Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs Source/SeljukEmpire.Tests/InfantryStanceTests.cs
git commit -m "Add AssessInfantryStance; rename shared disengage constants"
```

---

## Task 3: Wire foot archers and infantry into `TuranTacticMissionBehavior`

**Files:**
- Modify: `Source/SeljukEmpire/Tactics/TuranTacticMissionBehavior.cs`

**Interfaces:**
- Consumes: `TacticalSituationAssessor.AssessInfantryStance`, `.ShouldFormShieldWall`,
  `.AssessHorseArcherStance` (Tasks 1-2, unchanged), `TacticalFormationsHelper.CalculateFallbackVector`
  (already exists, from the earlier cavalry pass).
- Produces: no new public API — internal behavior changes only.

No unit tests are possible for this task (live-engine `MissionBehavior` integration). Verification
is `dotnet build`; real behavioral verification happens in Task 5.

- [ ] **Step 1: Add three new private fields**

Find (the block added by the previous pass):

```csharp
        private bool _shockCavalryCommittedToCharge;
        private bool _shockCavalryRegrouped;
        private MissionTime? _awaitOpeningStartTime;
        private MissionTime _doctrineReevalTimer;
```

Replace with:

```csharp
        private bool _shockCavalryCommittedToCharge;
        private bool _shockCavalryRegrouped;
        private MissionTime? _awaitOpeningStartTime;
        private MissionTime _doctrineReevalTimer;
        private bool _infantryCommittedToAdvance;
        private bool _infantryRegrouped;
        private MissionTime? _infantryHoldStartTime;
```

- [ ] **Step 2: Initialize the new fields in `AfterStart`**

Find:

```csharp
        public override void AfterStart()
        {
            base.AfterStart();
            _currentPhase = TacticalPhase.InitialAssessment;
            _phaseTimer = MissionTime.Now;
            _tickThrottleTimer = MissionTime.Now;
            _doctrineReevalTimer = MissionTime.Now;
            _shockCavalryCommittedToCharge = false;
            _shockCavalryRegrouped = false;
            _awaitOpeningStartTime = null;
        }
```

Replace with:

```csharp
        public override void AfterStart()
        {
            base.AfterStart();
            _currentPhase = TacticalPhase.InitialAssessment;
            _phaseTimer = MissionTime.Now;
            _tickThrottleTimer = MissionTime.Now;
            _doctrineReevalTimer = MissionTime.Now;
            _shockCavalryCommittedToCharge = false;
            _shockCavalryRegrouped = false;
            _awaitOpeningStartTime = null;
            _infantryCommittedToAdvance = false;
            _infantryRegrouped = false;
            _infantryHoldStartTime = null;
        }
```

- [ ] **Step 3: Reset the new fields in `OnMissionStateFinalized`**

Find:

```csharp
        public override void OnMissionStateFinalized()
        {
            base.OnMissionStateFinalized();
            _seljukTeam = null;
            _enemyTeam = null;
            _activeDoctrine = TacticalDoctrine.Undecided;
            _currentPhase = TacticalPhase.BattleEnded;
            _shockCavalryCommittedToCharge = false;
            _shockCavalryRegrouped = false;
            _awaitOpeningStartTime = null;
        }
```

Replace with:

```csharp
        public override void OnMissionStateFinalized()
        {
            base.OnMissionStateFinalized();
            _seljukTeam = null;
            _enemyTeam = null;
            _activeDoctrine = TacticalDoctrine.Undecided;
            _currentPhase = TacticalPhase.BattleEnded;
            _shockCavalryCommittedToCharge = false;
            _shockCavalryRegrouped = false;
            _awaitOpeningStartTime = null;
            _infantryCommittedToAdvance = false;
            _infantryRegrouped = false;
            _infantryHoldStartTime = null;
        }
```

- [ ] **Step 4: Replace the Staging phase's unconditional infantry shield wall and foot archer positioning**

Find (inside `ExecuteStagingAndSkirmish`):

```csharp
            // Positioning Infantry on High Ground Anchor
            if (infantry != null && infantry.CountOfUnits > 0)
            {
                WorldPosition anchorWorldPos = new WorldPosition(Mission.Current.Scene, _anchorHighGround);
                infantry.SetMovementOrder(MovementOrder.MovementOrderMove(anchorWorldPos));
                infantry.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
            }

            // Foot archers placed right behind shield wall
            if (footArchers != null && footArchers.CountOfUnits > 0)
            {
                Vec3 enemyDir = (enemyPos - _anchorHighGround).NormalizedCopy();
                Vec3 archerPos = _anchorHighGround - (enemyDir * 12f);
                WorldPosition archerWorldPos = new WorldPosition(Mission.Current.Scene, TacticalFormationsHelper.ClampToMapBoundaries(archerPos));
                footArchers.SetMovementOrder(MovementOrder.MovementOrderMove(archerWorldPos));
                footArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
            }
```

Replace with:

```csharp
            // Positioning Infantry on High Ground Anchor - shield wall only when actually needed
            if (infantry != null && infantry.CountOfUnits > 0)
            {
                WorldPosition anchorWorldPos = new WorldPosition(Mission.Current.Scene, _anchorHighGround);
                infantry.SetMovementOrder(MovementOrder.MovementOrderMove(anchorWorldPos));

                FormationQuerySystem closestInfantryEnemyQs = infantry.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
                bool infantryHasSignificantEnemy = closestInfantryEnemyQs != null;
                float infantryEnemyCavalryRatio = infantryHasSignificantEnemy ? closestInfantryEnemyQs.CavalryUnitRatio : 0f;
                bool infantryUnderRangedAttack = infantry.QuerySystem.IsUnderRangedAttack;

                infantry.SetArrangementOrder(
                    TacticalSituationAssessor.ShouldFormShieldWall(infantryHasSignificantEnemy, infantryEnemyCavalryRatio, infantryUnderRangedAttack)
                        ? ArrangementOrder.ArrangementOrderShieldWall
                        : ArrangementOrder.ArrangementOrderLine);
            }

            // Foot archers placed right behind the line - reactive stance, never a blind melee charge
            ApplyFootArcherStance(footArchers);
```

- [ ] **Step 5: Add infantry and foot archers to `ExecuteDualFlankEncirclement`**

Find:

```csharp
        private void ExecuteDualFlankEncirclement()
        {
            Formation shockCavalry = _seljukTeam.GetFormation(FormationClass.Cavalry);
            Formation horseArchers = _seljukTeam.GetFormation(FormationClass.HorseArcher);

            ApplyShockCavalryStance(shockCavalry);
            ApplyHorseArcherStance(horseArchers);
```

Replace with:

```csharp
        private void ExecuteDualFlankEncirclement()
        {
            Formation shockCavalry = _seljukTeam.GetFormation(FormationClass.Cavalry);
            Formation horseArchers = _seljukTeam.GetFormation(FormationClass.HorseArcher);
            Formation infantry = _seljukTeam.GetFormation(FormationClass.Infantry);
            Formation footArchers = _seljukTeam.GetFormation(FormationClass.Ranged);

            ApplyShockCavalryStance(shockCavalry);
            ApplyHorseArcherStance(horseArchers);
            ApplyInfantryStance(infantry);
            ApplyFootArcherStance(footArchers);
```

(The `cavalryReadyToAdvance` phase-transition gate immediately below this, in the same method, is
unchanged — infantry does not need to be added to that gate, because `ApplyInfantryStance` keeps
gating its own decision internally via its `AwaitOpening` branch regardless of which phase is
currently active, the same way it will keep doing so once `DecisiveHammerCharge` begins.)

- [ ] **Step 6: Replace the Decisive phase's unconditional infantry and foot archer charges**

Find (inside `ExecuteDecisiveHammerCharge`):

```csharp
            // All formations unleash full frontal and flanking assault
            if (infantry != null && infantry.CountOfUnits > 0)
            {
                infantry.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
                infantry.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }

            if (footArchers != null && footArchers.CountOfUnits > 0)
            {
                footArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }
```

Replace with:

```csharp
            ApplyInfantryStance(infantry);
            ApplyFootArcherStance(footArchers);
```

- [ ] **Step 7: Add the two new stance-application methods**

Add these two new private methods anywhere after `ApplyShockCavalryStance` and before
`IsSeljukTeam` (continuing the file's numbered-comment style as 9 and 10):

```csharp
        /// <summary>
        /// 9. Foot archer stance: identical decision logic to horse archers (a ranged formation's
        /// hold-and-skirmish/pursue/regroup choice doesn't depend on being mounted) - reuses
        /// AssessHorseArcherStance directly. Only order translation differs: no
        /// ArrangementOrderSkein (a cavalry wedge formation, meaningless without horses).
        /// </summary>
        private void ApplyFootArcherStance(Formation footArchers)
        {
            if (footArchers == null || footArchers.CountOfUnits <= 0) return;

            bool hasAmmo = !TacticalFormationsHelper.IsRangedAmmoDepleted(footArchers);
            FormationQuerySystem closestEnemyQs = footArchers.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
            bool hasSignificantEnemy = closestEnemyQs != null;
            float enemyPowerRatio = hasSignificantEnemy ? closestEnemyQs.LocalPowerRatio : 0f;

            FormationStance stance = TacticalSituationAssessor.AssessHorseArcherStance(hasAmmo, hasSignificantEnemy, enemyPowerRatio);

            switch (stance)
            {
                case FormationStance.HoldAndSkirmish:
                    footArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
                    if (hasSignificantEnemy)
                    {
                        Vec3 archerPos = footArchers.CachedAveragePosition.ToVec3();
                        Vec3 enemyPos = closestEnemyQs.Formation.CachedAveragePosition.ToVec3();
                        float kiteRange = footArchers.QuerySystem.MissileRangeAdjusted * 0.85f;
                        if (archerPos.DistanceSquared(enemyPos) < kiteRange * kiteRange)
                        {
                            Vec3 kitePos = TacticalFormationsHelper.CalculateFallbackVector(archerPos, enemyPos, 25f);
                            footArchers.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, kitePos)));
                        }
                    }
                    break;

                case FormationStance.Pursue:
                    footArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
                    footArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
                    break;

                case FormationStance.Regroup:
                    footArchers.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, _anchorHighGround)));
                    break;
            }
        }

        /// <summary>
        /// 10. Infantry stance: shield wall only when facing a cavalry-heavy enemy or already
        /// under missile fire (ShouldFormShieldWall), commits to an advance only when that's
        /// actually favorable, and disengages a losing advance instead of fighting to the last
        /// man. See design spec section "Infantry".
        /// </summary>
        private void ApplyInfantryStance(Formation infantry)
        {
            if (infantry == null || infantry.CountOfUnits <= 0) return;
            if (_infantryRegrouped) return; // one-way disengage for the rest of this battle

            FormationQuerySystem closestEnemyQs = infantry.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
            bool hasSignificantEnemy = closestEnemyQs != null;
            float enemyCavalryRatio = hasSignificantEnemy ? closestEnemyQs.CavalryUnitRatio : 0f;
            bool isUnderHeavyRangedAttack = infantry.QuerySystem.IsUnderRangedAttack;
            float secondsHolding = _infantryHoldStartTime.HasValue ? _infantryHoldStartTime.Value.ElapsedSeconds : 0f;
            bool isDefensivePosture = _activeDoctrine == TacticalDoctrine.HighGroundAmbush;

            FormationStance stance = TacticalSituationAssessor.AssessInfantryStance(
                isCurrentlyAdvancing: _infantryCommittedToAdvance,
                hasSignificantEnemyFormation: hasSignificantEnemy,
                enemyCavalryUnitRatio: enemyCavalryRatio,
                isUnderHeavyRangedAttack: isUnderHeavyRangedAttack,
                enemyCasualtyRatio: hasSignificantEnemy ? (1f - closestEnemyQs.CasualtyRatio) : 0f,
                secondsSinceHoldStarted: secondsHolding,
                selfCasualtyRatio: (1f - infantry.QuerySystem.CasualtyRatio),
                selfLocalPowerRatio: infantry.QuerySystem.LocalPowerRatio,
                isDefensivePosture: isDefensivePosture);

            switch (stance)
            {
                case FormationStance.AwaitOpening:
                    if (!_infantryHoldStartTime.HasValue)
                    {
                        _infantryHoldStartTime = MissionTime.Now;
                    }
                    infantry.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
                    infantry.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, _anchorHighGround)));
                    break;

                case FormationStance.AdvanceAndCharge:
                    _infantryCommittedToAdvance = true;
                    infantry.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
                    infantry.SetMovementOrder(MovementOrder.MovementOrderCharge);
                    break;

                case FormationStance.Regroup:
                    _infantryRegrouped = true;
                    infantry.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
                    infantry.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, _anchorHighGround)));
                    break;
            }
        }
```

- [ ] **Step 8: Build and verify**

Run: `dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release`
Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 9: Commit**

```bash
git add Source/SeljukEmpire/Tactics/TuranTacticMissionBehavior.cs
git commit -m "Wire reactive infantry/foot-archer stance into Seljuk tactical AI"
```

---

## Task 4: Mirror the same wiring into `ByzantineTacticMissionBehavior`

**Files:**
- Modify: `Source/SeljukEmpire/Tactics/ByzantineTacticMissionBehavior.cs`

**Interfaces:** same as Task 3.

This file is structurally identical to `TuranTacticMissionBehavior.cs` after Task 3, modulo the
established renamings: `_seljukTeam` → `_byzantineTeam`, `TacticalDoctrine.HighGroundAmbush` →
`TacticalDoctrine.ThematicLastStand`. Apply the same 9 edits from Task 3, Steps 1-9, substituting
those two names throughout.

- [ ] **Step 1: Add three new private fields**

Find:

```csharp
        private bool _shockCavalryCommittedToCharge;
        private bool _shockCavalryRegrouped;
        private MissionTime? _awaitOpeningStartTime;
        private MissionTime _doctrineReevalTimer;
```

Replace with:

```csharp
        private bool _shockCavalryCommittedToCharge;
        private bool _shockCavalryRegrouped;
        private MissionTime? _awaitOpeningStartTime;
        private MissionTime _doctrineReevalTimer;
        private bool _infantryCommittedToAdvance;
        private bool _infantryRegrouped;
        private MissionTime? _infantryHoldStartTime;
```

- [ ] **Step 2: Initialize the new fields in `AfterStart`**

Find:

```csharp
        public override void AfterStart()
        {
            base.AfterStart();
            _currentPhase = TacticalPhase.InitialAssessment;
            _phaseTimer = MissionTime.Now;
            _tickThrottleTimer = MissionTime.Now;
            _doctrineReevalTimer = MissionTime.Now;
            _shockCavalryCommittedToCharge = false;
            _shockCavalryRegrouped = false;
            _awaitOpeningStartTime = null;
        }
```

Replace with:

```csharp
        public override void AfterStart()
        {
            base.AfterStart();
            _currentPhase = TacticalPhase.InitialAssessment;
            _phaseTimer = MissionTime.Now;
            _tickThrottleTimer = MissionTime.Now;
            _doctrineReevalTimer = MissionTime.Now;
            _shockCavalryCommittedToCharge = false;
            _shockCavalryRegrouped = false;
            _awaitOpeningStartTime = null;
            _infantryCommittedToAdvance = false;
            _infantryRegrouped = false;
            _infantryHoldStartTime = null;
        }
```

- [ ] **Step 3: Reset the new fields in `OnMissionStateFinalized`**

Find:

```csharp
        public override void OnMissionStateFinalized()
        {
            base.OnMissionStateFinalized();
            _byzantineTeam = null;
            _enemyTeam = null;
            _activeDoctrine = TacticalDoctrine.Undecided;
            _currentPhase = TacticalPhase.BattleEnded;
            _shockCavalryCommittedToCharge = false;
            _shockCavalryRegrouped = false;
            _awaitOpeningStartTime = null;
        }
```

Replace with:

```csharp
        public override void OnMissionStateFinalized()
        {
            base.OnMissionStateFinalized();
            _byzantineTeam = null;
            _enemyTeam = null;
            _activeDoctrine = TacticalDoctrine.Undecided;
            _currentPhase = TacticalPhase.BattleEnded;
            _shockCavalryCommittedToCharge = false;
            _shockCavalryRegrouped = false;
            _awaitOpeningStartTime = null;
            _infantryCommittedToAdvance = false;
            _infantryRegrouped = false;
            _infantryHoldStartTime = null;
        }
```

- [ ] **Step 4: Replace the Staging phase's unconditional infantry shield wall and foot archer positioning**

Find (inside `ExecuteStagingAndSkirmish`):

```csharp
            // Tagma infantry anchors the line on high ground
            if (infantry != null && infantry.CountOfUnits > 0)
            {
                WorldPosition anchorWorldPos = new WorldPosition(Mission.Current.Scene, _anchorHighGround);
                infantry.SetMovementOrder(MovementOrder.MovementOrderMove(anchorWorldPos));
                infantry.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
            }

            // Toxotai foot archers placed right behind the shield wall
            if (footArchers != null && footArchers.CountOfUnits > 0)
            {
                Vec3 enemyDir = (enemyPos - _anchorHighGround).NormalizedCopy();
                Vec3 archerPos = _anchorHighGround - (enemyDir * 12f);
                WorldPosition archerWorldPos = new WorldPosition(Mission.Current.Scene, TacticalFormationsHelper.ClampToMapBoundaries(archerPos));
                footArchers.SetMovementOrder(MovementOrder.MovementOrderMove(archerWorldPos));
                footArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
            }
```

Replace with:

```csharp
            // Tagma infantry anchors the line on high ground - shield wall only when actually needed
            if (infantry != null && infantry.CountOfUnits > 0)
            {
                WorldPosition anchorWorldPos = new WorldPosition(Mission.Current.Scene, _anchorHighGround);
                infantry.SetMovementOrder(MovementOrder.MovementOrderMove(anchorWorldPos));

                FormationQuerySystem closestInfantryEnemyQs = infantry.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
                bool infantryHasSignificantEnemy = closestInfantryEnemyQs != null;
                float infantryEnemyCavalryRatio = infantryHasSignificantEnemy ? closestInfantryEnemyQs.CavalryUnitRatio : 0f;
                bool infantryUnderRangedAttack = infantry.QuerySystem.IsUnderRangedAttack;

                infantry.SetArrangementOrder(
                    TacticalSituationAssessor.ShouldFormShieldWall(infantryHasSignificantEnemy, infantryEnemyCavalryRatio, infantryUnderRangedAttack)
                        ? ArrangementOrder.ArrangementOrderShieldWall
                        : ArrangementOrder.ArrangementOrderLine);
            }

            // Toxotai foot archers placed right behind the line - reactive stance, never a blind melee charge
            ApplyFootArcherStance(footArchers);
```

- [ ] **Step 5: Add infantry and foot archers to `ExecuteDualFlankEncirclement`**

Find:

```csharp
        private void ExecuteDualFlankEncirclement()
        {
            Formation shockCavalry = _byzantineTeam.GetFormation(FormationClass.Cavalry);
            Formation horseArchers = _byzantineTeam.GetFormation(FormationClass.HorseArcher);

            ApplyShockCavalryStance(shockCavalry);
            ApplyHorseArcherStance(horseArchers);
```

Replace with:

```csharp
        private void ExecuteDualFlankEncirclement()
        {
            Formation shockCavalry = _byzantineTeam.GetFormation(FormationClass.Cavalry);
            Formation horseArchers = _byzantineTeam.GetFormation(FormationClass.HorseArcher);
            Formation infantry = _byzantineTeam.GetFormation(FormationClass.Infantry);
            Formation footArchers = _byzantineTeam.GetFormation(FormationClass.Ranged);

            ApplyShockCavalryStance(shockCavalry);
            ApplyHorseArcherStance(horseArchers);
            ApplyInfantryStance(infantry);
            ApplyFootArcherStance(footArchers);
```

- [ ] **Step 6: Replace the Decisive phase's unconditional infantry and foot archer charges**

Find (inside `ExecuteDecisiveHammerCharge`):

```csharp
            // All formations unleash full frontal and flanking assault
            if (infantry != null && infantry.CountOfUnits > 0)
            {
                infantry.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
                infantry.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }

            if (footArchers != null && footArchers.CountOfUnits > 0)
            {
                footArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }
```

Replace with:

```csharp
            ApplyInfantryStance(infantry);
            ApplyFootArcherStance(footArchers);
```

- [ ] **Step 7: Add the two new stance-application methods**

Add these two new private methods anywhere after `ApplyShockCavalryStance` and before
`IsByzantineTeam`:

```csharp
        /// <summary>
        /// 9. Foot archer stance: identical decision logic to horse archers (a ranged formation's
        /// hold-and-skirmish/pursue/regroup choice doesn't depend on being mounted) - reuses
        /// AssessHorseArcherStance directly. Only order translation differs: no
        /// ArrangementOrderSkein (a cavalry wedge formation, meaningless without horses).
        /// </summary>
        private void ApplyFootArcherStance(Formation footArchers)
        {
            if (footArchers == null || footArchers.CountOfUnits <= 0) return;

            bool hasAmmo = !TacticalFormationsHelper.IsRangedAmmoDepleted(footArchers);
            FormationQuerySystem closestEnemyQs = footArchers.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
            bool hasSignificantEnemy = closestEnemyQs != null;
            float enemyPowerRatio = hasSignificantEnemy ? closestEnemyQs.LocalPowerRatio : 0f;

            FormationStance stance = TacticalSituationAssessor.AssessHorseArcherStance(hasAmmo, hasSignificantEnemy, enemyPowerRatio);

            switch (stance)
            {
                case FormationStance.HoldAndSkirmish:
                    footArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
                    if (hasSignificantEnemy)
                    {
                        Vec3 archerPos = footArchers.CachedAveragePosition.ToVec3();
                        Vec3 enemyPos = closestEnemyQs.Formation.CachedAveragePosition.ToVec3();
                        float kiteRange = footArchers.QuerySystem.MissileRangeAdjusted * 0.85f;
                        if (archerPos.DistanceSquared(enemyPos) < kiteRange * kiteRange)
                        {
                            Vec3 kitePos = TacticalFormationsHelper.CalculateFallbackVector(archerPos, enemyPos, 25f);
                            footArchers.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, kitePos)));
                        }
                    }
                    break;

                case FormationStance.Pursue:
                    footArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
                    footArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
                    break;

                case FormationStance.Regroup:
                    footArchers.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, _anchorHighGround)));
                    break;
            }
        }

        /// <summary>
        /// 10. Infantry stance: shield wall only when facing a cavalry-heavy enemy or already
        /// under missile fire (ShouldFormShieldWall), commits to an advance only when that's
        /// actually favorable, and disengages a losing advance instead of fighting to the last
        /// man. See design spec section "Infantry".
        /// </summary>
        private void ApplyInfantryStance(Formation infantry)
        {
            if (infantry == null || infantry.CountOfUnits <= 0) return;
            if (_infantryRegrouped) return; // one-way disengage for the rest of this battle

            FormationQuerySystem closestEnemyQs = infantry.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
            bool hasSignificantEnemy = closestEnemyQs != null;
            float enemyCavalryRatio = hasSignificantEnemy ? closestEnemyQs.CavalryUnitRatio : 0f;
            bool isUnderHeavyRangedAttack = infantry.QuerySystem.IsUnderRangedAttack;
            float secondsHolding = _infantryHoldStartTime.HasValue ? _infantryHoldStartTime.Value.ElapsedSeconds : 0f;
            bool isDefensivePosture = _activeDoctrine == TacticalDoctrine.ThematicLastStand;

            FormationStance stance = TacticalSituationAssessor.AssessInfantryStance(
                isCurrentlyAdvancing: _infantryCommittedToAdvance,
                hasSignificantEnemyFormation: hasSignificantEnemy,
                enemyCavalryUnitRatio: enemyCavalryRatio,
                isUnderHeavyRangedAttack: isUnderHeavyRangedAttack,
                enemyCasualtyRatio: hasSignificantEnemy ? (1f - closestEnemyQs.CasualtyRatio) : 0f,
                secondsSinceHoldStarted: secondsHolding,
                selfCasualtyRatio: (1f - infantry.QuerySystem.CasualtyRatio),
                selfLocalPowerRatio: infantry.QuerySystem.LocalPowerRatio,
                isDefensivePosture: isDefensivePosture);

            switch (stance)
            {
                case FormationStance.AwaitOpening:
                    if (!_infantryHoldStartTime.HasValue)
                    {
                        _infantryHoldStartTime = MissionTime.Now;
                    }
                    infantry.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
                    infantry.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, _anchorHighGround)));
                    break;

                case FormationStance.AdvanceAndCharge:
                    _infantryCommittedToAdvance = true;
                    infantry.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
                    infantry.SetMovementOrder(MovementOrder.MovementOrderCharge);
                    break;

                case FormationStance.Regroup:
                    _infantryRegrouped = true;
                    infantry.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
                    infantry.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, _anchorHighGround)));
                    break;
            }
        }
```

- [ ] **Step 8: Build and verify**

Run: `dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release`
Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 9: Commit**

```bash
git add Source/SeljukEmpire/Tactics/ByzantineTacticMissionBehavior.cs
git commit -m "Wire reactive infantry/foot-archer stance into Byzantine tactical AI"
```

---

## Task 5: Full verification and local deploy

**Files:** none (verification only).

- [ ] **Step 1: Run the full test suite**

Run: `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 43, Skipped: 0`

- [ ] **Step 2: Run the release build**

Run: `dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release`
Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 3: Run the mod integrity checker**

Run: `python tools/verify_mod.py`
Expected: `verify_mod: 0 error(s), 0 warning(s)`

- [ ] **Step 4: Deploy to the local game install**

Mirror `bin/` and `ModuleData/` (unchanged by this pass, but keep the mirror complete) plus
`SubModule.xml` to
`C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\OttomanJanissariesAndTurkicHeroes`,
the same way this project's README describes and the previous pass already did.

- [ ] **Step 5: Record the outcome**

If any command fails, that's a real defect in Task 3 or 4's wiring — fix it, re-run the failed
command, and re-verify before moving to Task 6. Live in-game behavioral verification (actually
watching infantry hold/advance/regroup and foot archers skirmish in a real battle) is out of
scope for this task the same way it was for the previous pass — flag it to the user as something
to confirm in their own play session.

---

## Task 6: Version bump and release commit

**Files:**
- Modify: `SubModule.xml`

- [ ] **Step 1: Bump the version**

In `SubModule.xml`, find:

```xml
  <Version value="v1.7.9"/>
```

Replace with:

```xml
  <Version value="v1.8.0"/>
```

(A minor version bump, not a patch bump like the previous pass's v1.7.9 - this pass adds new
reactive behavior to two more formation types across both cultures, not just a targeted fix.)

- [ ] **Step 2: Final full verification**

Run: `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 43, Skipped: 0`

Run: `dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release`
Expected: `0 Warning(s)`, `0 Error(s)`.

Run: `python tools/verify_mod.py`
Expected: `verify_mod: 0 error(s), 0 warning(s)`

- [ ] **Step 3: Commit**

```bash
git add SubModule.xml
git commit -m "Bump to v1.8.0: reactive tactical AI for infantry and foot archers

Infantry now forms a shield wall only when facing a cavalry-heavy enemy
or already under missile fire, and disengages a losing advance instead of
fighting to the last man. Foot archers reuse the same hold-and-skirmish
logic already shipped for horse archers - no more blind melee charges.

See docs/superpowers/specs/2026-09-01-infantry-archer-reactive-tactical-ai-design.md."
```

**Note for a future session:** `graphify-out/graph.md` section 4 still describes the tactical AI
as it existed before both this pass and the previous cavalry pass - it needs another update pass,
same as flagged after the previous plan.
