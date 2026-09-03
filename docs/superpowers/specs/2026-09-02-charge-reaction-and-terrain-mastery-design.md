# Front-facing charge reaction and native terrain mastery

Date: 2026-09-02

## Context

Two reactive-AI passes have already shipped and merged to `main`: cavalry (v1.7.9) and
infantry/foot archers (v1.8.0), both built on `TacticalSituationAssessor` reading
`Formation.QuerySystem` data instead of fixed timers. This session's final whole-branch reviews
caught real, decompile-verified semantic bugs in both passes — a property meant something
different from what its name suggested (`CasualtyRatio` is a *survival* fraction;
`MovementSpeedMaximum` is a speed *capability*, not current speed; `CavalryUnitRatio` excludes
horse archers). The user explicitly asked this pass be done with the same rigor that caught those
bugs, before finalizing anything — every signal below was decompiled and its exact computation
read line-by-line, not inferred from its name.

This pass adds two independent, narrowly-scoped enhancements, both using engine-precomputed
signals the mod does not currently read at all:

1. **Front-facing cavalry charge reaction** — infantry currently has no mechanism to interrupt an
   in-progress advance when an enemy cavalry charge is about to strike its front; the existing
   mid-advance disengage only reacts to casualties already taken.
2. **Native terrain mastery** — `TacticalFormationsHelper.FindOptimalHighGround` samples 8 fixed
   radial points at a fixed 70-80m radius; Native's own engine runs a real slope-search algorithm,
   oriented toward the anticipated battle line and radius-scaled to the actual distance to the
   enemy.

## Decompiled signal semantics (verified, not assumed)

Both signals live on `FormationQuerySystem`, decompiled from `TaleWorlds.MountAndBlade.dll`
(`ilspycmd`), the same convention this mod has used throughout its tactical AI work.

### `IsUnderCavalryChargeFromFront` (bool, 2s refresh)

```csharp
FormationQuerySystem closestSignificantlyLargeEnemyFormation = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
if (closestSignificantlyLargeEnemyFormation != null && closestSignificantlyLargeEnemyFormation.IsCavalryFormationReadOnly)
{
    Vec2 enemyVelocity = closestSignificantlyLargeEnemyFormation.Formation.CachedCurrentVelocity;
    float enemySpeed = enemyVelocity.Normalize();
    Vec2 towardUs = Formation.CachedMedianPosition.AsVec2 - closestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2;
    float distance = towardUs.Normalize();
    bool chargingAtUs = enemyVelocity.DotProduct(towardUs) > 0.75f;
    bool weAreFacingItOrBraced = Formation.Arrangement is CircularFormation || Formation.Arrangement is SquareFormation
        || towardUs.DotProduct(Formation.Direction) < -0.75f;
    if (chargingAtUs & weAreFacingItOrBraced)
    {
        return distance / enemySpeed < 15f; // time-to-impact under 15 seconds
    }
}
return false;
```

Four conditions must all hold: the nearest significant enemy formation's *main class* is shock
cavalry (`IsCavalryFormation` — this is `FormationClass.Cavalry` only; mounted archers are a
disjoint `FormationClass.HorseArcher`, tracked by the separate `IsRangedCavalryFormation`, exactly
the same class split the previous pass's `CavalryUnitRatio`/`RangedCavalryUnitRatio` fix already
established); its velocity is aimed at us (within ~41° of directly toward us); we are either
already in a circular/square defensive formation or are facing the threat (not being hit in the
flank or rear — "FromFront" is literal); and time-to-impact at current closing speed is under 15
seconds. This is not an early-warning signal — it is a "the charge is about to land, right now"
signal, refreshed every 2 seconds.

**Deliberately not covered:** mounted-archer charges (a different `FormationClass`), charges
against a flank or rear the formation isn't facing, and anything more than ~15 seconds out. None
of these gaps are bugs to work around — they define exactly what "front-facing charge reaction"
means as a feature.

### `HighGroundCloseToForeseenBattleGround` (Vec2, 10s refresh)

```csharp
WorldPosition center = Formation.CachedMedianPosition;
center.SetVec2(Formation.CachedAveragePosition);
WorldPosition referencePosition = Team.MedianTargetFormationPosition;
return mission.FindPositionWithBiggestSlopeTowardsDirectionInSquare(
    ref center,
    Formation.CachedAveragePosition.Distance(Team.MedianTargetFormationPosition.AsVec2) * 0.5f,
    ref referencePosition
).AsVec2;
```

A genuine engine terrain-slope search (`FindPositionWithBiggestSlopeTowardsDirectionInSquare`),
not a fixed-radius height sample: it searches a square region around the formation's own current
position, oriented by `Team.MedianTargetFormationPosition` (where the team's formations are
collectively converging — i.e. the anticipated battle line), with a search radius that scales to
half the actual distance to that line. This is strictly more informed than the mod's own
`FindOptimalHighGround` (8 fixed compass points, fixed 70-80m radius, simple peak height compare,
no awareness of which direction the battle is actually coming from).

## Goals

- Infantry that is mid-advance snaps to a defensive brace the instant a shock-cavalry charge is
  about to strike its front, instead of only reacting after casualties mount.
- The army's chosen high-ground anchor point comes from the engine's own slope-search when
  available, falling back to the mod's existing 8-point scan only when the engine's answer looks
  degenerate (most likely very early in a battle, before `Team.MedianTargetFormationPosition` has
  settled).

## Non-goals

- No change to shock cavalry, horse archer, or foot archer stance logic — this signal is scoped to
  infantry only, matching what "FromFront" (a formation with a faced defensive line) actually
  describes. Cavalry already has its own mid-charge disengage; ranged formations already default
  to holding range.
- No new localization keys / no new `DisplayDoctrineMessage` calls — same deliberate scope
  decision as both previous passes, for the same reason (an 8-language translation cost unrelated
  to the AI-behavior work itself).
- No change to `EvaluateAndSelectDoctrine`'s doctrine-selection thresholds, or to any already-
  shipped cavalry/archer behavior.

## Architecture

### Charge reaction: a wiring-level short-circuit, not a new pure-function parameter

`ApplyInfantryStance` gains a check immediately after its existing null/empty-formation guard
(`if (infantry == null || infantry.CountOfUnits <= 0) return;`, unchanged) and immediately
*before* the existing `if (_infantryRegrouped) return;` line:

```csharp
if (infantry.QuerySystem.IsUnderCavalryChargeFromFront)
{
    infantry.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
    infantry.SetMovementOrder(MovementOrder.MovementOrderStop);
    return;
}
```

So the method's opening sequence becomes: null/empty check → charge-reaction override → one-way
regroup check → the rest of the stance machine, unchanged.

This deliberately does **not** touch `TacticalSituationAssessor.AssessInfantryStance` or its
tests — the existing pure function's decision surface (advance/brace/regroup based on enemy
composition and casualties) is unchanged and still fully covered by its existing 12 tests. The
charge reaction is a different kind of decision — an immediate, engine-driven emergency override,
not a case the stance machine needs to reason about — so it stays entirely in the wiring layer,
the same way `_infantryRegrouped`'s one-way early-return already does.

Consequence: `_infantryCommittedToAdvance` is **not** reset by the override. Once the 15-second
window passes (`IsUnderCavalryChargeFromFront` naturally returns to `false` — the charge either
struck, was repelled, or the enemy formation changed course), `ApplyInfantryStance` falls through
to the normal stance machine on the very next tick, which resumes whatever it was doing before the
interruption. This reads as "brace for the impact, then continue" — the correct behavior for a
formation that was already committed to advancing and gets caught by a flanking charge along the
way — rather than abandoning a otherwise-sound advance over one close call.

`MovementOrder.MovementOrderStop` (a real, pre-existing static value —
`MovementOrderEnum.Stop`, confirmed in the decompiled `MovementOrder` class) is used instead of
moving anywhere: a formation that is actively trying to relocate while bracing for a charge
receives worse footing than one that plants and holds.

Applied to both `TuranTacticMissionBehavior.cs` and `ByzantineTacticMissionBehavior.cs`
identically — this check reads only `infantry.QuerySystem`, which needs no team-specific state.

### Terrain mastery: an optional parameter on the existing helper, with a fallback

`TacticalFormationsHelper.FindOptimalHighGround` gains one new optional parameter:

```csharp
public static Vec3 FindOptimalHighGround(Vec3 centerPos, float searchRadius = 70f, FormationQuerySystem preferredFormationQuerySystem = null)
{
    if (preferredFormationQuerySystem != null)
    {
        Vec2 engineSuggestion = preferredFormationQuerySystem.HighGroundCloseToForeseenBattleGround;
        if (engineSuggestion.DistanceSquared(centerPos.AsVec2) > 1f)
        {
            float z = Mission.Current?.Scene != null ? Mission.Current.Scene.GetTerrainHeight(engineSuggestion) : centerPos.z;
            return ClampToMapBoundaries(new Vec3(engineSuggestion.x, engineSuggestion.y, z));
        }
    }

    // existing 8-point radial scan, completely unchanged, below this point
    ...
}
```

`preferredFormationQuerySystem` defaults to `null`, so every existing call site (there are none
elsewhere in the codebase today besides `EvaluateAndSelectDoctrine`, but the method is public) that
doesn't pass one keeps its exact current behavior — this is a strictly additive change to the
method's public surface, not a rewrite.

The `DistanceSquared(...) > 1f` check is a lightweight sanity guard, not a proof of correctness: if
the engine's slope search returns a point essentially identical to the formation's current
position (within 1 meter), that's read as "the engine had nothing meaningfully better to offer
right now" — most plausible very early in a battle, before `Team.MedianTargetFormationPosition`
has had a chance to settle into a real anticipated battle line — and the mod falls back to its own
8-point scan rather than anchoring the whole army's defensive position on a possibly-uninitialized
answer. This is explicitly flagged as something to confirm sane in the playtest (see Testing) —
the guard is defensive engineering, not a substitute for watching it choose a real hillside in an
actual battle.

`EvaluateAndSelectDoctrine` (in both mission-behavior files) needs a genuine `Formation` reference
to pass in, not just a headcount. **Correction caught during this spec's own self-review:** the
method already has a local named `infantry` at this point, but it is an `int` (a running headcount
from the existing unit-tally loop — `int infantry = 0;` accumulated inside `foreach (var formation
in _seljukTeam.FormationsIncludingEmpty)`), not a `Formation`. Reusing that name for a `Formation`
would be a duplicate-identifier compile error, and the int itself has no `.QuerySystem` to call.
The fix is a new, distinctly-named local, fetched via the same `GetFormation` pattern
`ExecuteStagingAndSkirmish` already uses elsewhere in the same file:

```csharp
Vec3 teamCenter = GetTeamCenterPosition(_seljukTeam);

// Establish terrain anchor on closest highest ground
Formation anchorInfantryFormation = _seljukTeam.GetFormation(FormationClass.Infantry);
_anchorHighGround = TacticalFormationsHelper.FindOptimalHighGround(teamCenter, 80f, anchorInfantryFormation?.QuerySystem);
_designatedKillzone = teamCenter;
```

(`GetTeamCenterPosition(_seljukTeam)` and the `_designatedKillzone` assignment are existing lines,
shown only for placement context — the two new lines are `Formation anchorInfantryFormation = ...`
and the `FindOptimalHighGround` call's new third argument.)

Infantry specifically, not any other formation class, because infantry is what actually occupies
`_anchorHighGround` for the whole battle (`ExecuteStagingAndSkirmish` moves infantry there and
holds it as the defensive anchor); a cavalry-only or archer-only army (no infantry formation) falls
back to the existing 8-point scan via the `null` default, unchanged from today's behavior.

## Testing

- `FindOptimalHighGround`'s new branch is engine-dependent (calls into `Mission.Current.Scene` and
  a live `FormationQuerySystem`) and cannot be unit tested, matching every other method in
  `TacticalFormationsHelper` — verification is `dotnet build` plus playtest.
- The `IsUnderCavalryChargeFromFront` short-circuit is likewise pure wiring — no unit test
  possible, and (per Architecture above) it deliberately does not touch any of the already-tested
  pure logic in `TacticalSituationAssessor.cs`, so the existing 43 tests need no changes and no
  additions for this pass.
- Playtest scenarios to confirm by eye, matching this project's established verification pattern:
  1. A Seljuk/Byzantine infantry line mid-advance, with enemy shock cavalry closing on its front —
     confirm the line visibly halts and raises shields shortly before impact, rather than
     continuing to walk into the charge.
  2. The same charge approaching from a flank the infantry is not facing — confirm the override
     does *not* fire (this is deliberately out of scope, per the decompiled semantics), so a
     regression here would mean the guard condition was mis-copied.
  3. A field battle on visibly hilly terrain — confirm the army's initial staging position
     (`_anchorHighGround`) lands on a real, sensible defensive rise facing the enemy's approach,
     not a flat or oddly-oriented spot.

## Files touched

- Modify: `Source/SeljukEmpire/Tactics/TacticalFormationsHelper.cs` (add the optional parameter to
  `FindOptimalHighGround`; the existing 8-point scan body is otherwise untouched)
- Modify: `Source/SeljukEmpire/Tactics/TuranTacticMissionBehavior.cs` (the `ApplyInfantryStance`
  short-circuit; the `EvaluateAndSelectDoctrine` call-site update)
- Modify: `Source/SeljukEmpire/Tactics/ByzantineTacticMissionBehavior.cs` (identical, with the
  established `_byzantineTeam` naming)
- Not touched: `TacticalSituationAssessor.cs` and its test files — neither enhancement changes the
  pure decision logic or its test surface, by design (see Architecture)

## Risks

- **`Team.MedianTargetFormationPosition` readiness at `InitialAssessment` time.** This runs at the
  very start of a battle, before any orders have been issued. If this Native value isn't
  meaningfully populated yet, `HighGroundCloseToForeseenBattleGround` could return something
  arbitrary. The `DistanceSquared > 1f` sanity guard catches the most likely failure shape (engine
  returns ~current position, meaning "nothing found"), but cannot catch a confidently-wrong answer
  that's merely *different* from the current position. This is exactly the kind of thing only a
  live playtest (scenario 3 above) can actually confirm — flagged, not assumed safe.
- **15-second charge-reaction window vs. the 2-second signal refresh.** `IsUnderCavalryChargeFromFront`
  is cached for 2 seconds and this mod's tick throttle is 1.25 seconds, so the override can lag the
  true state by up to ~2 seconds in the worst case — for a threat whose own window is 15 seconds,
  this is a small fraction of the warning time and not expected to matter in practice, but is worth
  naming rather than leaving implicit.
- **Interaction with `_infantryRegrouped`.** The new check runs *before* the existing
  `if (_infantryRegrouped) return;` guard is reached in the method (it's placed first). A formation
  that has already permanently disengaged this battle will still snap to a defensive brace if
  caught by a charge, rather than the early-return silently doing nothing — this is judged correct
  (a regrouped formation should still protect itself from an active charge) but is a deliberate
  ordering choice worth flagging explicitly rather than leaving as an accident of where the code
  was inserted.
