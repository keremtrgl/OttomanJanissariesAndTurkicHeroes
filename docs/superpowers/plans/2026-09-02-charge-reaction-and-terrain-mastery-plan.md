# Front-Facing Charge Reaction and Native Terrain Mastery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Infantry snaps to a defensive brace and halts the instant a shock-cavalry charge is
about to strike its front (using Native's own `IsUnderCavalryChargeFromFront` signal), and the
army's high-ground anchor point prefers Native's own slope-search terrain evaluator over the
mod's fixed 8-point radial scan.

**Architecture:** Both enhancements are pure wiring-layer additions — no new pure-logic function,
no change to `TacticalSituationAssessor.cs` or its existing 43 tests. `TacticalFormationsHelper.
FindOptimalHighGround` gains one optional parameter with a safe default (existing callers
unaffected); both mission-behavior files gain a short-circuit check and a call-site update.

**Tech Stack:** C# / .NET (`netstandard2.0`), TaleWorlds Bannerlord modding API
(`TaleWorlds.MountAndBlade`).

## Global Constraints

- Spec: `docs/superpowers/specs/2026-09-02-charge-reaction-and-terrain-mastery-design.md` — every
  value and code shape below is copied verbatim from it.
- `dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release` must report 0 warnings/0
  errors after every task that touches `Source/SeljukEmpire/`.
- `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj` must still report 43/43 passing
  after every task — this plan adds no new tests and changes no tested pure logic, so the count
  must stay exactly 43 throughout.
- No new localization keys / no new `DisplayDoctrineMessage` calls.
- `python tools/verify_mod.py` must report 0 errors/0 warnings before the final commit.
- `IsUnderCavalryChargeFromFront` is scoped to infantry only — do not wire it into shock cavalry,
  horse archer, or foot archer stance methods (see spec's Non-goals).
- The `FindOptimalHighGround` engine-preference branch is purely additive: the new
  `preferredFormationQuerySystem` parameter defaults to `null`, and when `null` the method's
  behavior must be byte-identical to today's.
- `EvaluateAndSelectDoctrine` in both mission-behavior files already has a **local variable named
  `infantry` that is an `int` headcount**, not a `Formation` — do not reuse that name for the new
  `Formation` reference this plan needs; use a distinctly-named local (`anchorInfantryFormation`,
  specified exactly in Task 2/3 below) or the build will fail on a duplicate-identifier error.

---

## Task 1: `FindOptimalHighGround` engine-preference branch

**Files:**
- Modify: `Source/SeljukEmpire/Tactics/TacticalFormationsHelper.cs`

**Interfaces:**
- Consumes: nothing new (uses the existing `ClampToMapBoundaries`, already in this file).
- Produces: `public static Vec3 FindOptimalHighGround(Vec3 centerPos, float searchRadius = 70f,
  FormationQuerySystem preferredFormationQuerySystem = null)` — the same method, with one new
  optional trailing parameter. Used by Task 2/3.

This method is engine-dependent (`Mission.Current.Scene`), so no unit test is possible — matching
every other method in this file. Verification is `dotnet build` only.

- [ ] **Step 1: Add the engine-preference branch**

Find:

```csharp
        public static Vec3 FindOptimalHighGround(Vec3 centerPos, float searchRadius = 70f)
        {
            if (Mission.Current?.Scene == null) return centerPos;

            Scene scene = Mission.Current.Scene;
```

Replace with:

```csharp
        public static Vec3 FindOptimalHighGround(Vec3 centerPos, float searchRadius = 70f, FormationQuerySystem preferredFormationQuerySystem = null)
        {
            // Prefer Native's own slope-search terrain evaluator (oriented toward the
            // anticipated battle line, radius-scaled to distance) when a formation is given.
            // Fall back to the 8-point scan below if it looks degenerate (essentially our
            // current position - most likely Team.MedianTargetFormationPosition hasn't settled
            // yet, very early in a battle) or if no formation was provided at all.
            if (preferredFormationQuerySystem != null)
            {
                Vec2 engineSuggestion = preferredFormationQuerySystem.HighGroundCloseToForeseenBattleGround;
                if (engineSuggestion.DistanceSquared(centerPos.AsVec2) > 1f && Mission.Current?.Scene != null)
                {
                    float engineZ = Mission.Current.Scene.GetTerrainHeight(engineSuggestion);
                    return ClampToMapBoundaries(new Vec3(engineSuggestion.x, engineSuggestion.y, engineZ));
                }
            }

            if (Mission.Current?.Scene == null) return centerPos;

            Scene scene = Mission.Current.Scene;
```

Everything below this point in the method (the 8-point radial scan and its `return
ClampToMapBoundaries(bestPos);`) is unchanged.

- [ ] **Step 2: Build and verify**

Run: `dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release`
Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 3: Run the existing test suite (regression check)**

Run: `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 43, Skipped: 0` (unchanged — this task touches no
tested pure logic, this step only confirms nothing broke).

- [ ] **Step 4: Commit**

```bash
git add Source/SeljukEmpire/Tactics/TacticalFormationsHelper.cs
git commit -m "Add native terrain-slope preference to FindOptimalHighGround"
```

---

## Task 2: Wire both enhancements into `TuranTacticMissionBehavior`

**Files:**
- Modify: `Source/SeljukEmpire/Tactics/TuranTacticMissionBehavior.cs`

**Interfaces:**
- Consumes: `TacticalFormationsHelper.FindOptimalHighGround`'s new third parameter (Task 1).
- Produces: no new public API — internal behavior changes only.

No unit tests are possible for this task (live-engine `MissionBehavior` integration). Verification
is `dotnet build`; real behavioral verification happens in Task 4.

- [ ] **Step 1: Add the charge-reaction short-circuit to `ApplyInfantryStance`**

Find:

```csharp
        private void ApplyInfantryStance(Formation infantry)
        {
            if (infantry == null || infantry.CountOfUnits <= 0) return;
            if (_infantryRegrouped) return; // one-way disengage for the rest of this battle
```

Replace with:

```csharp
        private void ApplyInfantryStance(Formation infantry)
        {
            if (infantry == null || infantry.CountOfUnits <= 0) return;

            // Emergency override: an enemy shock-cavalry charge is about to strike our front
            // within ~15 seconds (decompile-verified: excludes horse archers and flank/rear
            // hits by design). Snap to a brace immediately, regardless of what the stance
            // machine below was doing - it resumes on its own once the window passes.
            if (infantry.QuerySystem.IsUnderCavalryChargeFromFront)
            {
                infantry.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
                infantry.SetMovementOrder(MovementOrder.MovementOrderStop);
                return;
            }

            if (_infantryRegrouped) return; // one-way disengage for the rest of this battle
```

- [ ] **Step 2: Add a distinctly-named `Formation` reference and update the `FindOptimalHighGround` call site**

Find:

```csharp
            Vec3 teamCenter = GetTeamCenterPosition(_seljukTeam);

            // Establish terrain anchor on closest highest ground
            _anchorHighGround = TacticalFormationsHelper.FindOptimalHighGround(teamCenter, 80f);
            _designatedKillzone = teamCenter;
```

Replace with:

```csharp
            Vec3 teamCenter = GetTeamCenterPosition(_seljukTeam);

            // Establish terrain anchor on closest highest ground - prefer Native's own
            // slope-search evaluator when infantry exists to run it from (named distinctly
            // from the pre-existing 'infantry' int headcount local a few lines above).
            Formation anchorInfantryFormation = _seljukTeam.GetFormation(FormationClass.Infantry);
            _anchorHighGround = TacticalFormationsHelper.FindOptimalHighGround(teamCenter, 80f, anchorInfantryFormation?.QuerySystem);
            _designatedKillzone = teamCenter;
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release`
Expected: `0 Warning(s)`, `0 Error(s)`. If this fails with a duplicate-identifier error on
`infantry`, the `anchorInfantryFormation` name from Step 2 was not used exactly as specified -
re-check against the Global Constraints note about the pre-existing `int infantry` local.

- [ ] **Step 4: Run the existing test suite (regression check)**

Run: `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 43, Skipped: 0`

- [ ] **Step 5: Commit**

```bash
git add Source/SeljukEmpire/Tactics/TuranTacticMissionBehavior.cs
git commit -m "Wire charge reaction and terrain mastery into Seljuk tactical AI"
```

---

## Task 3: Mirror the same wiring into `ByzantineTacticMissionBehavior`

**Files:**
- Modify: `Source/SeljukEmpire/Tactics/ByzantineTacticMissionBehavior.cs`

**Interfaces:** same as Task 2.

This file has the identical structure at both edit points, with the established naming
difference: `_seljukTeam` → `_byzantineTeam`. It has the same pre-existing `int infantry`
headcount local as Turan's file (confirmed present at the same point in
`EvaluateAndSelectDoctrine`) - the same distinctly-named `anchorInfantryFormation` local avoids
the same collision here.

- [ ] **Step 1: Add the charge-reaction short-circuit to `ApplyInfantryStance`**

Find:

```csharp
        private void ApplyInfantryStance(Formation infantry)
        {
            if (infantry == null || infantry.CountOfUnits <= 0) return;
            if (_infantryRegrouped) return; // one-way disengage for the rest of this battle
```

Replace with:

```csharp
        private void ApplyInfantryStance(Formation infantry)
        {
            if (infantry == null || infantry.CountOfUnits <= 0) return;

            // Emergency override: an enemy shock-cavalry charge is about to strike our front
            // within ~15 seconds (decompile-verified: excludes horse archers and flank/rear
            // hits by design). Snap to a brace immediately, regardless of what the stance
            // machine below was doing - it resumes on its own once the window passes.
            if (infantry.QuerySystem.IsUnderCavalryChargeFromFront)
            {
                infantry.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
                infantry.SetMovementOrder(MovementOrder.MovementOrderStop);
                return;
            }

            if (_infantryRegrouped) return; // one-way disengage for the rest of this battle
```

- [ ] **Step 2: Add a distinctly-named `Formation` reference and update the `FindOptimalHighGround` call site**

Find:

```csharp
            Vec3 teamCenter = GetTeamCenterPosition(_byzantineTeam);

            // Establish terrain anchor on closest highest ground
            _anchorHighGround = TacticalFormationsHelper.FindOptimalHighGround(teamCenter, 80f);
            _designatedKillzone = teamCenter;
```

Replace with:

```csharp
            Vec3 teamCenter = GetTeamCenterPosition(_byzantineTeam);

            // Establish terrain anchor on closest highest ground - prefer Native's own
            // slope-search evaluator when infantry exists to run it from (named distinctly
            // from the pre-existing 'infantry' int headcount local a few lines above).
            Formation anchorInfantryFormation = _byzantineTeam.GetFormation(FormationClass.Infantry);
            _anchorHighGround = TacticalFormationsHelper.FindOptimalHighGround(teamCenter, 80f, anchorInfantryFormation?.QuerySystem);
            _designatedKillzone = teamCenter;
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release`
Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 4: Run the existing test suite (regression check)**

Run: `dotnet test Source/SeljukEmpire.Tests/SeljukEmpire.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 43, Skipped: 0`

- [ ] **Step 5: Commit**

```bash
git add Source/SeljukEmpire/Tactics/ByzantineTacticMissionBehavior.cs
git commit -m "Wire charge reaction and terrain mastery into Byzantine tactical AI"
```

---

## Task 4: Full verification and local deploy

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

Mirror `bin/` to
`C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\OttomanJanissariesAndTurkicHeroes\bin`,
the same way this project's README describes and the previous two passes already did (`ModuleData`
and `SubModule.xml` are unchanged by this pass until Task 5's version bump, but re-mirror them too
for completeness).

- [ ] **Step 5: Record the outcome**

If any command fails, that's a real defect in Task 1/2/3's wiring - fix it, re-run the failed
command, and re-verify before moving to Task 5. Live in-game behavioral verification (watching
infantry actually brace against an incoming charge, and watching the army's staging position land
on sensible terrain) is out of scope for this task, the same way it was for both previous passes -
flag it to the user as something to confirm in their own play session. In particular, call out the
spec's two named risks for the user to watch for: whether `HighGroundCloseToForeseenBattleGround`
picks a sensible spot very early in a battle (before `Team.MedianTargetFormationPosition` has
settled), and whether the charge-reaction override actually correctly does *not* fire for a charge
hitting a flank the formation isn't facing (this is deliberately out of scope for the feature, so
if it fires there anyway, that's a real bug, not an enhancement request).

---

## Task 5: Version bump and release commit

**Files:**
- Modify: `SubModule.xml`

- [ ] **Step 1: Bump the version**

In `SubModule.xml`, find:

```xml
  <Version value="v1.8.0"/>
```

Replace with:

```xml
  <Version value="v1.8.1"/>
```

(A patch bump, not minor - this pass adds two small, narrowly-scoped reactive-AI signals to
already-shipped infrastructure, not a new formation-type-wide behavior layer like v1.8.0 was.)

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
git commit -m "Bump to v1.8.1: front-facing charge reaction and native terrain mastery

Infantry now snaps to a defensive brace and halts when an enemy
shock-cavalry charge is about to strike its front within ~15 seconds
(Formation.QuerySystem.IsUnderCavalryChargeFromFront), instead of only
reacting after casualties mount. The army's high-ground anchor point now
prefers Native's own slope-search terrain evaluator
(HighGroundCloseToForeseenBattleGround) over the mod's fixed 8-point
radial scan, falling back to it only when the engine's answer looks
uninitialized.

See docs/superpowers/specs/2026-09-02-charge-reaction-and-terrain-mastery-design.md."
```

**Note for a future session:** `graphify-out/graph.md` section 4 still describes the tactical AI
as it existed before all three reactive-AI passes this session (cavalry, infantry/archers, and
now this one) - it needs another update pass, same as flagged after each previous plan.
