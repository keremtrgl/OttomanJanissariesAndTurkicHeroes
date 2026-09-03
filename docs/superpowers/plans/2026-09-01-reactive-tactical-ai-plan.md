# Reactive Tactical AI for Cavalry Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the blind, timer-only cavalry-charge logic in `TuranTacticMissionBehavior` and
`ByzantineTacticMissionBehavior` with a shared, genuinely reactive decision layer that reads
Native's own live `Formation.QuerySystem` data (casualties, local combat power, enemy formation
state) so horse archers stop suicide-charging into melee and shock cavalry stops charging braced
spear/shield lines head-on.

**Architecture:** A new stateless class, `TacticalSituationAssessor`, takes plain primitives (not
live engine objects) in and returns a `FormationStance` enum out — this makes the actual decision
logic unit-testable with plain xunit, something nothing else in this codebase currently is. Both
mission behaviors keep their existing 5-phase FSM as the outer structure; only the phases that
today issue unconditional `MovementOrderCharge` calls are changed to consult the assessor first.

**Tech Stack:** C# / .NET (`netstandard2.0`, existing `SeljukTactics.csproj`), TaleWorlds
Bannerlord modding API (`TaleWorlds.MountAndBlade`), xunit for the new pure-logic test project
(`net8.0`, has zero Bannerlord engine dependencies so it needs no game install to run).

## Global Constraints

- Spec: `docs/superpowers/specs/2026-09-01-reactive-tactical-ai-design.md` — every rule and
  threshold below is copied verbatim from it; do not re-derive or re-guess a number.
- `TacticalSituationAssessor` must have **zero** `TaleWorlds.*` references in its public API or
  implementation — this is what makes it unit-testable without the game engine. If a rule seems to
  need a `Formation`/`FormationQuerySystem` object, extract the specific primitive value(s) it
  needs in the calling mission behavior instead.
- `dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release` must report 0 warnings/0
  errors after every task that touches `Source/SeljukEmpire/` (not the test project).
- The existing `try/catch → StandardEngineFallback` wrapper in each mission behavior's
  `OnMissionTick` must remain intact and untouched — any new code this plan adds inside
  `ExecuteTacticalDecisionLoop` or the phase methods is already covered by it.
- No new per-agent loops. Every new signal comes from properties `Formation.QuerySystem` already
  computes every tick regardless of this mod (confirmed by decompiling
  `TaleWorlds.MountAndBlade.dll`'s `FormationQuerySystem`) — reading them costs a property access,
  not a scan.
- No new localization keys / no new `DisplayDoctrineMessage` calls for the new Regroup/doctrine-
  downgrade events. This is a deliberate scope decision (not in the spec's explicit requirements)
  to avoid an 8-language translation side-task unrelated to the actual AI-behavior fix — see
  `README.md`'s "Keeping all 8 languages in sync" section for why that's a real, non-trivial cost
  in this repo, not a rubber-stamp addition.
- `python tools/verify_mod.py` must still report 0 errors/0 warnings before the final commit (this
  pass touches no XML/localization, but every prior session in this repo re-runs it before calling
  work done — keep that habit).
- `TacticalFormationsHelper`'s existing style (static class, zero-GC, `ClampToMapBoundaries` on
  every computed world position) is the pattern for the one new helper this plan adds — follow it,
  don't introduce a different style.

---

## Task 1: Test project scaffold + `FormationStance` enum + horse archer stance logic (TDD)

**Files:**
- Create: `Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
- Create: `Source/SeljukEmpire.Tests/TacticalSituationAssessorTests.cs`
- Create: `Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs`

**Interfaces:**
- Produces: `namespace SeljukEmpire.Tactics { public enum FormationStance { AdvanceAndCharge,
  HoldAndSkirmish, AwaitOpening, Regroup, Pursue } }` and `public static class
  TacticalSituationAssessor` with `public static FormationStance AssessHorseArcherStance(bool
  hasAmmo, bool hasSignificantEnemyFormation, float enemyLocalPowerRatio)` — both are used by
  every later task.

- [ ] **Step 1: Create the test project**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>disable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>

  <ItemGroup>
    <!-- Compiled directly (not via ProjectReference to SeljukTactics.csproj) so this test
         project never needs the Bannerlord game install or its engine DLLs to build or run -
         TacticalSituationAssessor.cs is required to have zero TaleWorlds.* references, so this
         is always safe. -->
    <Compile Include="..\SeljukEmpire\Tactics\TacticalSituationAssessor.cs" />
  </ItemGroup>

</Project>
```

Save as `Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`.

- [ ] **Step 2: Write the failing tests**

```csharp
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
```

Save as `Source/SeljukEmpire.Tests/TacticalSituationAssessorTests.cs`.

- [ ] **Step 3: Run tests, verify they fail to compile (the type doesn't exist yet)**

Run: `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
Expected: build error — `The type or namespace name 'TacticalSituationAssessor' could not be
found` (or similar; `FormationStance`/`AssessHorseArcherStance` don't exist yet).

- [ ] **Step 4: Write the minimal implementation**

```csharp
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
```

Save as `Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs`.

- [ ] **Step 5: Run tests, verify they pass**

Run: `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 5, Skipped: 0`

- [ ] **Step 6: Commit**

```bash
git add Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj Source/SeljukEmpire.Tests/TacticalSituationAssessorTests.cs Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs
git commit -m "Add TacticalSituationAssessor with horse archer stance logic + tests"
```

---

## Task 2: Shock cavalry stance logic (TDD)

**Files:**
- Modify: `Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs`
- Modify: `Source/SeljukEmpire.Tests/TacticalSituationAssessorTests.cs` (new test class in the
  same file, or a sibling file — either is fine, this plan puts it in a new file for readability)
- Create: `Source/SeljukEmpire.Tests/ShockCavalryStanceTests.cs`

**Interfaces:**
- Consumes: `FormationStance` enum from Task 1.
- Produces: `public static FormationStance AssessShockCavalryStance(bool isCurrentlyCharging, bool
  hasSignificantEnemyFormation, float enemyMovementSpeedMaximum, float enemyInfantryUnitRatio,
  float enemyHasShieldUnitRatio, float enemyCasualtyRatio, float secondsSinceAwaitOpeningStarted,
  float selfCasualtyRatio, float selfLocalPowerRatio, bool isDefensivePosture)` — used by Task 5/6.

- [ ] **Step 1: Write the failing tests**

```csharp
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
```

Save as `Source/SeljukEmpire.Tests/ShockCavalryStanceTests.cs`.

- [ ] **Step 2: Run tests, verify they fail to compile**

Run: `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
Expected: build error — `AssessShockCavalryStance` does not exist on `TacticalSituationAssessor`.

- [ ] **Step 3: Add the minimal implementation**

Add these four `private const float` fields alongside the existing
`FavorablePowerRatioThreshold` inside `TacticalSituationAssessor`, and the new method below
`AssessHorseArcherStance`:

```csharp
        private const float BracedLineSpeedEpsilon = 0.5f;
        private const float BracedLineCompositionThreshold = 0.5f;
        private const float EnemySoftenedCasualtyThreshold = 0.15f;
        private const float AwaitOpeningTimeoutSeconds = 25f;
        private const float CavalryDisengageCasualtyThreshold = 0.35f;
        private const float CavalryDisengageCasualtyThresholdDefensive = 0.25f;
```

```csharp
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
```

- [ ] **Step 4: Run tests, verify they pass**

Run: `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 14, Skipped: 0` (5 from Task 1 + 9 new)

- [ ] **Step 5: Commit**

```bash
git add Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs Source/SeljukEmpire.Tests/ShockCavalryStanceTests.cs
git commit -m "Add shock cavalry stance logic: brace detection + mid-charge disengage"
```

---

## Task 3: Doctrine downgrade logic (TDD)

**Files:**
- Modify: `Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs`
- Create: `Source/SeljukEmpire.Tests/DoctrineDowngradeTests.cs`

**Interfaces:**
- Produces: `public static bool ShouldDowngradeToDefensiveDoctrine(float
  teamAverageCasualtyRatio)` — used by Task 5/6's `MaybeDowngradeDoctrine`.

- [ ] **Step 1: Write the failing tests**

```csharp
using Xunit;
using SeljukEmpire.Tactics;

namespace SeljukEmpire.Tests
{
    public class DoctrineDowngradeTests
    {
        [Fact]
        public void BelowThreshold_DoesNotDowngrade()
        {
            Assert.False(TacticalSituationAssessor.ShouldDowngradeToDefensiveDoctrine(0.39f));
        }

        [Fact]
        public void AtThreshold_DoesNotDowngrade()
        {
            Assert.False(TacticalSituationAssessor.ShouldDowngradeToDefensiveDoctrine(0.40f));
        }

        [Fact]
        public void AboveThreshold_Downgrades()
        {
            Assert.True(TacticalSituationAssessor.ShouldDowngradeToDefensiveDoctrine(0.41f));
        }
    }
}
```

Save as `Source/SeljukEmpire.Tests/DoctrineDowngradeTests.cs`.

- [ ] **Step 2: Run tests, verify they fail to compile**

Run: `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
Expected: build error — `ShouldDowngradeToDefensiveDoctrine` does not exist.

- [ ] **Step 3: Add the minimal implementation**

Add one more constant and method to `TacticalSituationAssessor`:

```csharp
        private const float DoctrineDowngradeCasualtyThreshold = 0.40f;
```

```csharp
        public static bool ShouldDowngradeToDefensiveDoctrine(float teamAverageCasualtyRatio)
        {
            return teamAverageCasualtyRatio > DoctrineDowngradeCasualtyThreshold;
        }
```

- [ ] **Step 4: Run tests, verify they pass**

Run: `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 17, Skipped: 0`

- [ ] **Step 5: Commit**

```bash
git add Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs Source/SeljukEmpire.Tests/DoctrineDowngradeTests.cs
git commit -m "Add doctrine downgrade threshold logic"
```

---

## Task 4: `CalculateFallbackVector` geometry helper

**Files:**
- Modify: `Source/SeljukEmpire/Tactics/TacticalFormationsHelper.cs`

**Interfaces:**
- Consumes: nothing new (uses the same `Mission.Current.Scene`/`Vec2`/`Vec3`/`ClampToMapBoundaries`
  already used by `CalculateFlankVector` in this file).
- Produces: `public static Vec3 CalculateFallbackVector(Vec3 selfPos, Vec3 enemyPos, float
  distance)` — used by Task 5/6 for horse archer kiting repositioning.

This is engine-dependent geometry (uses `Mission.Current.Scene.GetTerrainHeight`), so unlike Tasks
1-3 it cannot be unit tested without a running Bannerlord instance — same as every other method
already in this file. Its correctness is verified by `dotnet build` here, and exercised for real
during Task 7's manual playtest.

- [ ] **Step 1: Add the method**

Add directly below the existing `CalculateFlankVector` method in
`Source/SeljukEmpire/Tactics/TacticalFormationsHelper.cs` (after its closing `}`, before
`IsRangedAmmoDepleted`):

```csharp
        /// <summary>
        /// Computes a point directly away from the enemy - the mirror of CalculateFlankVector.
        /// Used to kite a ranged formation back out of melee range, or to send a disengaging
        /// formation toward a rally point.
        /// </summary>
        public static Vec3 CalculateFallbackVector(Vec3 selfPos, Vec3 enemyPos, float distance)
        {
            Vec2 awayFromEnemy = (selfPos.AsVec2 - enemyPos.AsVec2).Normalized();
            Vec2 target2D = selfPos.AsVec2 + (awayFromEnemy * distance);
            float z = Mission.Current?.Scene != null ? Mission.Current.Scene.GetTerrainHeight(target2D) : selfPos.z;

            return ClampToMapBoundaries(new Vec3(target2D.x, target2D.y, z));
        }
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release`
Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 3: Commit**

```bash
git add Source/SeljukEmpire/Tactics/TacticalFormationsHelper.cs
git commit -m "Add CalculateFallbackVector geometry helper"
```

---

## Task 5: Wire the assessor into `TuranTacticMissionBehavior`

**Files:**
- Modify: `Source/SeljukEmpire/Tactics/TuranTacticMissionBehavior.cs`

**Interfaces:**
- Consumes: `FormationStance`, `TacticalSituationAssessor.AssessHorseArcherStance`,
  `TacticalSituationAssessor.AssessShockCavalryStance`,
  `TacticalSituationAssessor.ShouldDowngradeToDefensiveDoctrine` (Tasks 1-3);
  `TacticalFormationsHelper.CalculateFallbackVector` (Task 4).
- Produces: no new public API — this task only changes internal behavior of an existing
  `MissionBehavior`.

No unit tests are possible for this task (it's `MissionBehavior`/`Formation`/`Mission.Current`
integration code, which needs a running Bannerlord battle to execute at all). Verification is
`dotnet build` here; real behavioral verification happens in Task 7.

- [ ] **Step 1: Add new private fields**

In `Source/SeljukEmpire/Tactics/TuranTacticMissionBehavior.cs`, find this existing block (around
line 51-54):

```csharp
        private Vec3 _leftFlankPosition;
        private Vec3 _rightFlankPosition;
```

Replace with:

```csharp
        private Vec3 _leftFlankPosition;
        private Vec3 _rightFlankPosition;
        private bool _shockCavalryCommittedToCharge;
        private bool _shockCavalryRegrouped;
        private MissionTime? _awaitOpeningStartTime;
        private MissionTime _doctrineReevalTimer;
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
        }
```

- [ ] **Step 4: Run the doctrine re-evaluation from the main decision loop**

Find:

```csharp
        private void ExecuteTacticalDecisionLoop()
        {
            if (_activeDoctrine == TacticalDoctrine.StandardEngineFallback) return;

            switch (_currentPhase)
            {
```

Replace with:

```csharp
        private void ExecuteTacticalDecisionLoop()
        {
            if (_activeDoctrine == TacticalDoctrine.StandardEngineFallback) return;

            if (_currentPhase != TacticalPhase.InitialAssessment && _currentPhase != TacticalPhase.BattleEnded)
            {
                MaybeDowngradeDoctrine();
            }

            switch (_currentPhase)
            {
```

- [ ] **Step 5: Add the doctrine re-evaluation and stance-application helper methods**

Add these five new private methods anywhere after `ExecuteDecisiveHammerCharge` and before
`IsSeljukTeam` (i.e. as new numbered-comment methods 6-10, continuing the file's existing
numbering style):

```csharp
        /// <summary>
        /// 6. Periodic doctrine re-evaluation. One-way: once downgraded to HighGroundAmbush for
        /// heavy losses, this battle never upgrades back to a more aggressive doctrine.
        /// </summary>
        private void MaybeDowngradeDoctrine()
        {
            if (_activeDoctrine == TacticalDoctrine.HighGroundAmbush) return;
            if (_doctrineReevalTimer.ElapsedSeconds < 9f) return;

            _doctrineReevalTimer = MissionTime.Now;

            float totalCasualtyRatio = 0f;
            int formationCount = 0;

            foreach (var formation in _seljukTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0) continue;
                totalCasualtyRatio += formation.QuerySystem.CasualtyRatio;
                formationCount++;
            }

            if (formationCount == 0) return;

            float averageCasualtyRatio = totalCasualtyRatio / formationCount;

            if (TacticalSituationAssessor.ShouldDowngradeToDefensiveDoctrine(averageCasualtyRatio))
            {
                _activeDoctrine = TacticalDoctrine.HighGroundAmbush;
            }
        }

        /// <summary>
        /// 7. Horse archer stance: extracts primitives from Formation.QuerySystem and asks
        /// TacticalSituationAssessor what to do, then translates the answer into actual orders.
        /// Never issues a blind melee charge - see design spec section "Horse archers".
        /// </summary>
        private void ApplyHorseArcherStance(Formation horseArchers)
        {
            if (horseArchers == null || horseArchers.CountOfUnits <= 0) return;

            bool hasAmmo = !TacticalFormationsHelper.IsRangedAmmoDepleted(horseArchers);
            FormationQuerySystem closestEnemyQs = horseArchers.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
            bool hasSignificantEnemy = closestEnemyQs != null;
            float enemyPowerRatio = hasSignificantEnemy ? closestEnemyQs.LocalPowerRatio : 0f;

            FormationStance stance = TacticalSituationAssessor.AssessHorseArcherStance(hasAmmo, hasSignificantEnemy, enemyPowerRatio);

            switch (stance)
            {
                case FormationStance.HoldAndSkirmish:
                    horseArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
                    Vec3 archerPos = horseArchers.OrderPosition.ToVec3();
                    Vec3 enemyPos = GetTeamCenterPosition(_enemyTeam);
                    Vec3 kitePos = TacticalFormationsHelper.CalculateFallbackVector(archerPos, enemyPos, 25f);
                    horseArchers.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, kitePos)));
                    break;

                case FormationStance.Pursue:
                    horseArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
                    horseArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
                    break;

                case FormationStance.Regroup:
                    horseArchers.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, _anchorHighGround)));
                    break;
            }
        }

        /// <summary>
        /// 8. Shock cavalry stance: gates the charge behind a braced-line check before committing,
        /// then keeps polling every tick while charging so a losing fight gets recalled instead of
        /// fought to the last horse. See design spec section "Shock cavalry".
        /// </summary>
        private void ApplyShockCavalryStance(Formation shockCavalry)
        {
            if (shockCavalry == null || shockCavalry.CountOfUnits <= 0) return;
            if (_shockCavalryRegrouped) return; // one-way disengage for the rest of this battle

            FormationQuerySystem closestEnemyQs = shockCavalry.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
            bool hasSignificantEnemy = closestEnemyQs != null;
            float secondsWaiting = _awaitOpeningStartTime.HasValue ? _awaitOpeningStartTime.Value.ElapsedSeconds : 0f;
            bool isDefensivePosture = _activeDoctrine == TacticalDoctrine.HighGroundAmbush;

            FormationStance stance = TacticalSituationAssessor.AssessShockCavalryStance(
                _shockCavalryCommittedToCharge,
                hasSignificantEnemy,
                hasSignificantEnemy ? closestEnemyQs.MovementSpeedMaximum : 0f,
                hasSignificantEnemy ? closestEnemyQs.InfantryUnitRatio : 0f,
                hasSignificantEnemy ? closestEnemyQs.HasShieldUnitRatio : 0f,
                hasSignificantEnemy ? closestEnemyQs.CasualtyRatio : 0f,
                secondsWaiting,
                shockCavalry.QuerySystem.CasualtyRatio,
                shockCavalry.QuerySystem.LocalPowerRatio,
                isDefensivePosture);

            switch (stance)
            {
                case FormationStance.AwaitOpening:
                    if (!_awaitOpeningStartTime.HasValue)
                    {
                        _awaitOpeningStartTime = MissionTime.Now;
                    }
                    shockCavalry.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
                    shockCavalry.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, _leftFlankPosition)));
                    break;

                case FormationStance.AdvanceAndCharge:
                    _shockCavalryCommittedToCharge = true;
                    shockCavalry.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
                    shockCavalry.SetMovementOrder(MovementOrder.MovementOrderCharge);
                    break;

                case FormationStance.Regroup:
                    _shockCavalryRegrouped = true;
                    shockCavalry.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, _anchorHighGround)));
                    break;
            }
        }
```

- [ ] **Step 6: Replace the Staging phase's blind horse-archer charge**

Find (inside `ExecuteStagingAndSkirmish`):

```csharp
            // Horse Archers Probing & Skirmishing
            if (horseArchers != null && horseArchers.CountOfUnits > 0)
            {
                horseArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
                horseArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
            }
```

Replace with:

```csharp
            // Horse Archers Probing & Skirmishing - reactive stance, never a blind melee charge
            ApplyHorseArcherStance(horseArchers);
```

- [ ] **Step 7: Replace the Encirclement phase's blind charges and gate its exit condition**

Find the full `ExecuteDualFlankEncirclement` method:

```csharp
        private void ExecuteDualFlankEncirclement()
        {
            Formation shockCavalry = _seljukTeam.GetFormation(FormationClass.Cavalry);
            Formation horseArchers = _seljukTeam.GetFormation(FormationClass.HorseArcher);

            // Shock Cavalry pincer strike on enemy flanks
            if (shockCavalry != null && shockCavalry.CountOfUnits > 0)
            {
                shockCavalry.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
                shockCavalry.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }

            // Horse archers circle rear
            if (horseArchers != null && horseArchers.CountOfUnits > 0)
            {
                // If horse archers ran out of ammo, they charge with lances/sabers!
                if (TacticalFormationsHelper.IsRangedAmmoDepleted(horseArchers))
                {
                    horseArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
                }
                horseArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }

            if (_phaseTimer.ElapsedSeconds > 16f || IsEnemyWithinDistance(30f))
            {
                _currentPhase = TacticalPhase.DecisiveHammerCharge;
                _phaseTimer = MissionTime.Now;
                DisplayDoctrineMessage("{=seljuk_tactic_full_assault}[Nizamiye Advance] Hammer and Anvil assault! All lines advance!", Colors.Red);
            }
        }
```

Replace with:

```csharp
        private void ExecuteDualFlankEncirclement()
        {
            Formation shockCavalry = _seljukTeam.GetFormation(FormationClass.Cavalry);
            Formation horseArchers = _seljukTeam.GetFormation(FormationClass.HorseArcher);

            ApplyShockCavalryStance(shockCavalry);
            ApplyHorseArcherStance(horseArchers);

            // Only let the fixed-time/distance transition push the battle into the all-in final
            // phase once shock cavalry has actually committed to a charge (or already made its
            // regroup call) - otherwise this would drag a formation still correctly waiting out a
            // braced enemy line into a forced charge.
            bool cavalryReadyToAdvance = shockCavalry == null || shockCavalry.CountOfUnits <= 0
                || _shockCavalryCommittedToCharge || _shockCavalryRegrouped;

            if (cavalryReadyToAdvance && (_phaseTimer.ElapsedSeconds > 16f || IsEnemyWithinDistance(30f)))
            {
                _currentPhase = TacticalPhase.DecisiveHammerCharge;
                _phaseTimer = MissionTime.Now;
                DisplayDoctrineMessage("{=seljuk_tactic_full_assault}[Nizamiye Advance] Hammer and Anvil assault! All lines advance!", Colors.Red);
            }
        }
```

- [ ] **Step 8: Replace the Decisive phase's blind cavalry charges**

Find (inside `ExecuteDecisiveHammerCharge`):

```csharp
            if (shockCav != null && shockCav.CountOfUnits > 0)
            {
                shockCav.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }

            if (horseArchers != null && horseArchers.CountOfUnits > 0)
            {
                horseArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }
```

Replace with:

```csharp
            ApplyShockCavalryStance(shockCav);
            ApplyHorseArcherStance(horseArchers);
```

(The `infantry`/`footArchers` blocks immediately above this in the same method are unchanged -
infantry behavior is out of scope for this pass.)

- [ ] **Step 9: Build and verify**

Run: `dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release`
Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 10: Commit**

```bash
git add Source/SeljukEmpire/Tactics/TuranTacticMissionBehavior.cs
git commit -m "Wire reactive stance logic into Seljuk tactical AI cavalry orders"
```

---

## Task 6: Mirror the same wiring into `ByzantineTacticMissionBehavior`

**Files:**
- Modify: `Source/SeljukEmpire/Tactics/ByzantineTacticMissionBehavior.cs`

**Interfaces:**
- Consumes: same as Task 5 (`TacticalSituationAssessor`, `TacticalFormationsHelper.CalculateFallbackVector`).
- Produces: no new public API.

This file is structurally identical to `TuranTacticMissionBehavior.cs` before Task 5 (confirmed by
reading both in full) except for these renamings: `_seljukTeam` → `_byzantineTeam`,
`IsSeljukTeam` → `IsByzantineTeam`, and the doctrine enum member `HighGroundAmbush` →
`ThematicLastStand`. Apply the exact same 9 edits from Task 5, Steps 1-9, substituting those three
names throughout. Concretely:

- [ ] **Step 1: Add new private fields**

Find (around line 65-68):

```csharp
        private Vec3 _leftFlankPosition;
        private Vec3 _rightFlankPosition;
```

Replace with:

```csharp
        private Vec3 _leftFlankPosition;
        private Vec3 _rightFlankPosition;
        private bool _shockCavalryCommittedToCharge;
        private bool _shockCavalryRegrouped;
        private MissionTime? _awaitOpeningStartTime;
        private MissionTime _doctrineReevalTimer;
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
        }
```

- [ ] **Step 4: Run the doctrine re-evaluation from the main decision loop**

Find:

```csharp
        private void ExecuteTacticalDecisionLoop()
        {
            if (_activeDoctrine == TacticalDoctrine.StandardEngineFallback) return;

            switch (_currentPhase)
            {
```

Replace with:

```csharp
        private void ExecuteTacticalDecisionLoop()
        {
            if (_activeDoctrine == TacticalDoctrine.StandardEngineFallback) return;

            if (_currentPhase != TacticalPhase.InitialAssessment && _currentPhase != TacticalPhase.BattleEnded)
            {
                MaybeDowngradeDoctrine();
            }

            switch (_currentPhase)
            {
```

- [ ] **Step 5: Add the doctrine re-evaluation and stance-application helper methods**

Add these five new private methods anywhere after `ExecuteDecisiveHammerCharge` and before
`IsByzantineTeam` (continuing the file's existing numbered-comment style):

```csharp
        /// <summary>
        /// 6. Periodic doctrine re-evaluation. One-way: once downgraded to ThematicLastStand for
        /// heavy losses, this battle never upgrades back to a more aggressive doctrine.
        /// </summary>
        private void MaybeDowngradeDoctrine()
        {
            if (_activeDoctrine == TacticalDoctrine.ThematicLastStand) return;
            if (_doctrineReevalTimer.ElapsedSeconds < 9f) return;

            _doctrineReevalTimer = MissionTime.Now;

            float totalCasualtyRatio = 0f;
            int formationCount = 0;

            foreach (var formation in _byzantineTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0) continue;
                totalCasualtyRatio += formation.QuerySystem.CasualtyRatio;
                formationCount++;
            }

            if (formationCount == 0) return;

            float averageCasualtyRatio = totalCasualtyRatio / formationCount;

            if (TacticalSituationAssessor.ShouldDowngradeToDefensiveDoctrine(averageCasualtyRatio))
            {
                _activeDoctrine = TacticalDoctrine.ThematicLastStand;
            }
        }

        /// <summary>
        /// 7. Horse archer stance: extracts primitives from Formation.QuerySystem and asks
        /// TacticalSituationAssessor what to do, then translates the answer into actual orders.
        /// Never issues a blind melee charge - see design spec section "Horse archers".
        /// </summary>
        private void ApplyHorseArcherStance(Formation horseArchers)
        {
            if (horseArchers == null || horseArchers.CountOfUnits <= 0) return;

            bool hasAmmo = !TacticalFormationsHelper.IsRangedAmmoDepleted(horseArchers);
            FormationQuerySystem closestEnemyQs = horseArchers.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
            bool hasSignificantEnemy = closestEnemyQs != null;
            float enemyPowerRatio = hasSignificantEnemy ? closestEnemyQs.LocalPowerRatio : 0f;

            FormationStance stance = TacticalSituationAssessor.AssessHorseArcherStance(hasAmmo, hasSignificantEnemy, enemyPowerRatio);

            switch (stance)
            {
                case FormationStance.HoldAndSkirmish:
                    horseArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
                    Vec3 archerPos = horseArchers.OrderPosition.ToVec3();
                    Vec3 enemyPos = GetTeamCenterPosition(_enemyTeam);
                    Vec3 kitePos = TacticalFormationsHelper.CalculateFallbackVector(archerPos, enemyPos, 25f);
                    horseArchers.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, kitePos)));
                    break;

                case FormationStance.Pursue:
                    horseArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
                    horseArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
                    break;

                case FormationStance.Regroup:
                    horseArchers.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, _anchorHighGround)));
                    break;
            }
        }

        /// <summary>
        /// 8. Shock cavalry stance: gates the charge behind a braced-line check before committing,
        /// then keeps polling every tick while charging so a losing fight gets recalled instead of
        /// fought to the last horse. See design spec section "Shock cavalry".
        /// </summary>
        private void ApplyShockCavalryStance(Formation shockCavalry)
        {
            if (shockCavalry == null || shockCavalry.CountOfUnits <= 0) return;
            if (_shockCavalryRegrouped) return; // one-way disengage for the rest of this battle

            FormationQuerySystem closestEnemyQs = shockCavalry.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
            bool hasSignificantEnemy = closestEnemyQs != null;
            float secondsWaiting = _awaitOpeningStartTime.HasValue ? _awaitOpeningStartTime.Value.ElapsedSeconds : 0f;
            bool isDefensivePosture = _activeDoctrine == TacticalDoctrine.ThematicLastStand;

            FormationStance stance = TacticalSituationAssessor.AssessShockCavalryStance(
                _shockCavalryCommittedToCharge,
                hasSignificantEnemy,
                hasSignificantEnemy ? closestEnemyQs.MovementSpeedMaximum : 0f,
                hasSignificantEnemy ? closestEnemyQs.InfantryUnitRatio : 0f,
                hasSignificantEnemy ? closestEnemyQs.HasShieldUnitRatio : 0f,
                hasSignificantEnemy ? closestEnemyQs.CasualtyRatio : 0f,
                secondsWaiting,
                shockCavalry.QuerySystem.CasualtyRatio,
                shockCavalry.QuerySystem.LocalPowerRatio,
                isDefensivePosture);

            switch (stance)
            {
                case FormationStance.AwaitOpening:
                    if (!_awaitOpeningStartTime.HasValue)
                    {
                        _awaitOpeningStartTime = MissionTime.Now;
                    }
                    shockCavalry.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
                    shockCavalry.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, _leftFlankPosition)));
                    break;

                case FormationStance.AdvanceAndCharge:
                    _shockCavalryCommittedToCharge = true;
                    shockCavalry.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
                    shockCavalry.SetMovementOrder(MovementOrder.MovementOrderCharge);
                    break;

                case FormationStance.Regroup:
                    _shockCavalryRegrouped = true;
                    shockCavalry.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, _anchorHighGround)));
                    break;
            }
        }
```

- [ ] **Step 6: Replace the Staging phase's blind horse-archer charge**

Find (inside `ExecuteStagingAndSkirmish`):

```csharp
            // Horse archers (Vardariotai-style) probing and skirmishing
            if (horseArchers != null && horseArchers.CountOfUnits > 0)
            {
                horseArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
                horseArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
            }
```

Replace with:

```csharp
            // Horse archers (Vardariotai-style) probing and skirmishing - reactive stance, never
            // a blind melee charge
            ApplyHorseArcherStance(horseArchers);
```

- [ ] **Step 7: Replace the Encirclement phase's blind charges and gate its exit condition**

Find the full `ExecuteDualFlankEncirclement` method (identical to Turan's pre-Task-5 version,
Byzantine-flavored comments and message key):

```csharp
        private void ExecuteDualFlankEncirclement()
        {
            Formation shockCavalry = _byzantineTeam.GetFormation(FormationClass.Cavalry);
            Formation horseArchers = _byzantineTeam.GetFormation(FormationClass.HorseArcher);

            // Kataphraktoi shock cavalry pincer strike on enemy flanks
            if (shockCavalry != null && shockCavalry.CountOfUnits > 0)
            {
                shockCavalry.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
                shockCavalry.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }

            // Horse archers circle the rear
            if (horseArchers != null && horseArchers.CountOfUnits > 0)
            {
                // If horse archers ran out of ammo, they charge with swords/lances!
                if (TacticalFormationsHelper.IsRangedAmmoDepleted(horseArchers))
                {
                    horseArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
                }
                horseArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }

            if (_phaseTimer.ElapsedSeconds > 16f || IsEnemyWithinDistance(30f))
            {
                _currentPhase = TacticalPhase.DecisiveHammerCharge;
                _phaseTimer = MissionTime.Now;
                DisplayDoctrineMessage("{=byz_tactic_full_assault}[Tagma Advance] Hammer and Anvil complete! All lines advance!", Colors.Red);
            }
        }
```

Replace with:

```csharp
        private void ExecuteDualFlankEncirclement()
        {
            Formation shockCavalry = _byzantineTeam.GetFormation(FormationClass.Cavalry);
            Formation horseArchers = _byzantineTeam.GetFormation(FormationClass.HorseArcher);

            ApplyShockCavalryStance(shockCavalry);
            ApplyHorseArcherStance(horseArchers);

            // Only let the fixed-time/distance transition push the battle into the all-in final
            // phase once shock cavalry has actually committed to a charge (or already made its
            // regroup call) - otherwise this would drag a formation still correctly waiting out a
            // braced enemy line into a forced charge.
            bool cavalryReadyToAdvance = shockCavalry == null || shockCavalry.CountOfUnits <= 0
                || _shockCavalryCommittedToCharge || _shockCavalryRegrouped;

            if (cavalryReadyToAdvance && (_phaseTimer.ElapsedSeconds > 16f || IsEnemyWithinDistance(30f)))
            {
                _currentPhase = TacticalPhase.DecisiveHammerCharge;
                _phaseTimer = MissionTime.Now;
                DisplayDoctrineMessage("{=byz_tactic_full_assault}[Tagma Advance] Hammer and Anvil complete! All lines advance!", Colors.Red);
            }
        }
```

- [ ] **Step 8: Replace the Decisive phase's blind cavalry charges**

Find (inside `ExecuteDecisiveHammerCharge`):

```csharp
            if (shockCav != null && shockCav.CountOfUnits > 0)
            {
                shockCav.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }

            if (horseArchers != null && horseArchers.CountOfUnits > 0)
            {
                horseArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }
```

Replace with:

```csharp
            ApplyShockCavalryStance(shockCav);
            ApplyHorseArcherStance(horseArchers);
```

- [ ] **Step 9: Build and verify**

Run: `dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release`
Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 10: Commit**

```bash
git add Source/SeljukEmpire/Tactics/ByzantineTacticMissionBehavior.cs
git commit -m "Wire reactive stance logic into Byzantine tactical AI cavalry orders"
```

---

## Task 7: Manual playtest verification

**Files:** none (no code changes - this task is a verification checklist, per the design spec's
"Testing / verification" section).

- [ ] **Step 1: Run the full test suite one more time**

Run: `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 17, Skipped: 0`

- [ ] **Step 2: Run the mod integrity checker**

Run: `python tools/verify_mod.py`
Expected: `verify_mod: 0 error(s), 0 warning(s)` (this pass touched no XML/localization, so this
should already be true - this step confirms nothing was accidentally broken).

- [ ] **Step 3: Deploy and launch a custom battle — braced-line scenario**

Deploy the freshly built DLL to the local game install (same `bin/` sync this project's README
describes), launch Bannerlord, start a Custom Battle: Seljuk side with a cavalry-heavy army
(shock cavalry + horse archers) versus a Byzantine side left on defend/hold (Tagma infantry with
shields, stationary). Observe:

Expected: Seljuk shock cavalry stages at the flank and visibly holds instead of charging
immediately into the stationary Tagma line; horse archers stay at range, shooting and
repositioning, instead of closing to melee. If the Byzantine line stays put long enough (~25s),
cavalry eventually charges anyway (the timeout fail-safe) rather than waiting forever.

- [ ] **Step 4: Launch a custom battle — lopsided/losing scenario**

Start a second Custom Battle with the Seljuk side deliberately heavily outnumbered/outclassed.
Observe:

Expected: as Seljuk casualties mount, the tactical command message/doctrine visibly shifts toward
a defensive posture, and a cavalry charge already in progress falls back to the rally point
instead of fighting to the last horse.

- [ ] **Step 5: Record the outcome**

If either scenario doesn't match its expected behavior, that's a bug in Task 5/6's wiring (most
likely a wrong field/threshold substitution during the Byzantine mirroring in Task 6, or an order
of operations issue between `ApplyShockCavalryStance` and the phase-transition guard) - fix it,
re-run `dotnet build`, and re-test the specific scenario that failed before moving on. Do not
proceed to Task 8 until both scenarios pass.

---

## Task 8: Version bump and release commit

**Files:**
- Modify: `SubModule.xml`

- [ ] **Step 1: Bump the version**

In `SubModule.xml`, find:

```xml
  <Version value="v1.7.8"/>
```

Replace with:

```xml
  <Version value="v1.7.9"/>
```

- [ ] **Step 2: Final full verification**

Run: `dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release`
Expected: `0 Warning(s)`, `0 Error(s)`.

Run: `python tools/verify_mod.py`
Expected: `verify_mod: 0 error(s), 0 warning(s)`

- [ ] **Step 3: Commit**

```bash
git add SubModule.xml
git commit -m "Bump to v1.7.9: reactive tactical AI for cavalry (Seljuk & Byzantine)

Cavalry no longer blindly charges braced enemy lines or fights a losing
charge to the last horse - see docs/superpowers/specs/2026-09-01-reactive-tactical-ai-design.md
for the full root-cause analysis and design."
```

**Note for a future session:** `graphify-out/graph.md` section 4 ("Çok Doktrinli Taktik Yapay
Zeka Motoru") still describes the old scripted-only behavior and was deliberately left out of
this plan's scope (see the design spec's "Files touched" section) - it needs the same kind of
update pass this mod already does after significant engine changes (see the `graphify` update
commits in git history for the established pattern).
