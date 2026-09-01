# Reactive tactical AI for infantry and foot archers

Date: 2026-09-01

## Context

The previous pass (see `docs/superpowers/specs/2026-09-01-reactive-tactical-ai-design.md`, merged
to `main`) replaced blind cavalry charges in `TuranTacticMissionBehavior`/
`ByzantineTacticMissionBehavior` with a reactive layer, `TacticalSituationAssessor`, that reads
Native's own `Formation.QuerySystem` data instead of fixed timers. That pass deliberately scoped
out infantry and foot archers as non-goals.

Requested this session, in the user's own words: "gerçekten oyuncu vay be desin, aşırı iyi olsun"
(make the tactical AI genuinely impressive — the player should go "wow"). Infantry is the most
visually prominent formation in most battles and is currently the least reactive part of the
system:

- **Staging** (`ExecuteStagingAndSkirmish`): infantry unconditionally forms `ArrangementOrderShieldWall`
  and marches to `_anchorHighGround` — every battle, regardless of whether the enemy has any
  cavalry or missile troops at all.
- **`DualFlankEncirclement`**: infantry is not touched at all — it simply continues holding its
  Staging-phase order for the whole phase.
- **`ExecuteDecisiveHammerCharge`**: infantry unconditionally switches to `ArrangementOrderLine`
  and charges — regardless of casualties, regardless of whether that charge is winnable.

Foot archers (`FormationClass.Ranged`) have the exact same defect the previous pass fixed for
horse archers: `ExecuteDecisiveHammerCharge` unconditionally issues them a melee
`MovementOrderCharge` once that phase begins, with no check for remaining ammunition or whether
melee is actually favorable.

## Goals

- Foot archers behave like the (already-fixed) horse archers: hold and shoot while they have
  ammunition, only join melee when out of ammo and the fight is locally favorable.
- Infantry forms a shield wall specifically when it matters (facing a cavalry-heavy enemy
  formation, or already taking missile fire) rather than unconditionally every battle, and only
  commits to an advance/charge when that is actually a good idea — with the same kind of
  mid-advance disengage the cavalry fix already has for a charge that is genuinely going badly.
- Reuse the proven `TacticalSituationAssessor`/`Formation.QuerySystem` architecture and its
  established conventions (stateless pure functions, primitives in/enum out, decompile-verified
  inputs, zero-GC wiring) rather than inventing a new pattern.

## Non-goals

- No changes to `TacticalDoctrine` selection (`EvaluateAndSelectDoctrine`'s thresholds are
  untouched) or to the cavalry logic already shipped.
- No new localization keys / no new `DisplayDoctrineMessage` calls (same deliberate scope decision
  as the previous pass, for the same reason — see that spec's Global Constraints).
- No siege-battle behavior (both mission behaviors already exit early on
  `Mission.Current.IsSiegeBattle`, unchanged).

## Architecture

Two additions to `Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs` (still zero
`TaleWorlds.*` dependencies, still unit-testable without the engine):

### Foot archers: literal reuse, no new function

`ApplyFootArcherStance` (new, in both mission-behavior files) calls the **existing**
`TacticalSituationAssessor.AssessHorseArcherStance` unchanged — a ranged formation's decision
logic (hold-and-skirmish while ammo remains; once empty, pursue if the nearest significant enemy
is locally weaker, else regroup) does not depend on whether the unit is mounted. The only
difference is in translating the returned `FormationStance` into orders: foot archers use
`ArrangementOrderLoose` throughout (no `ArrangementOrderSkein` — that is a cavalry wedge
formation, meaningless without horses) and their `Pursue`/kite movement uses the same
`CalculateFallbackVector`/live-position pattern already fixed for horse archers in the previous
pass (`Formation.CachedAveragePosition`, not `OrderPosition`).

### Infantry: a new function with the same shape, a different brace trigger

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
```

Same shape as `AssessShockCavalryStance` (an `isCurrentlyAdvancing` branch that polls for a
mid-advance disengage, and a not-yet-advancing branch that gates on a "should I still be
bracing" condition with a softened/timeout release) — reused deliberately for consistency, not
merged into one shared function: infantry's battlefield role (anchor a line, hold a chokepoint)
is different enough from cavalry's (mobile shock, capable of true hit-and-run) that forcing both
into one over-parameterized function would cost more in clarity than the duplication costs in
repetition. This mirrors the same judgment call already made for the Turan/Byzantine file pair,
which the final review of the previous pass explicitly examined and did not flag as a problem.

The brace trigger is infantry-specific: not "is the enemy stationary" (that was cavalry's,
already fixed once from a decompile mistake) but "does the enemy pose a threat shield-wall
discipline actually helps against" — a cavalry-heavy formation, or missile fire already landing
on us:

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

`CavalryThreatRatioThreshold = 0.30f` — reusing the exact threshold this codebase's own
`EvaluateAndSelectDoctrine` already uses for "cavalry heavy" (`cavRatio >= 0.30f`), not inventing
a new number. `AssessInfantryStance` calls `ShouldFormShieldWall` internally for its own brace
gate, and the mission-behavior wiring calls the same function separately to choose
`ArrangementOrderShieldWall` vs `ArrangementOrderLine` — the threshold is defined once, consumed
in two places, never re-derived.

`isUnderHeavyRangedAttack` is fed from `Formation.QuerySystem.IsUnderRangedAttack` directly — a
boolean Native already computes every tick — not a hand-picked threshold on the raw
`UnderRangedAttackRatio` float. This is a deliberate lesson from the previous pass's final review:
prefer an engine-native boolean/enum over reinterpreting a raw float with an invented cutoff when
one is available, since that is exactly the class of mistake (`MovementSpeedMaximum`,
`CasualtyRatio`) that pass's decompile-verified fixes had to correct.

## Behavior rules

**Bracing (not yet advancing):** `ShouldFormShieldWall(...)` true → hold at `_anchorHighGround`,
stance `AwaitOpening`. Two release conditions open the gate to `AdvanceAndCharge`: the enemy
formation is already softened (`enemyCasualtyRatio > 0.15`, the same corrected "fraction of
casualties taken" reading the cavalry fix established — computed at the wiring site as
`1f - closestEnemyQs.CasualtyRatio`, never a raw `.CasualtyRatio` read), or 35 seconds have
elapsed since the hold began (`secondsSinceHoldStarted >= 35f` — longer than cavalry's 25s
timeout, reflecting that holding a defensive line is a normal, sustainable posture for infantry
in a way that idling cavalry at a flank is not). If `ShouldFormShieldWall(...)` is false to begin
with, there is nothing to brace against — stance is `AdvanceAndCharge` immediately.

**Advancing:** stance `AdvanceAndCharge`, arrangement `ArrangementOrderLine`, movement
`MovementOrderCharge`. Polled every tick like the cavalry fix: if `selfCasualtyRatio` (again,
`1f - infantry.QuerySystem.CasualtyRatio`) exceeds 0.35 (0.25 when `isDefensivePosture`, matching
the cavalry thresholds exactly) **and** `selfLocalPowerRatio <= 1.0`, the stance flips to
`Regroup` — fall back to `_anchorHighGround` and unconditionally reform
`ArrangementOrderShieldWall` (a retreating line defaults to its safest arrangement rather than
re-deriving the threat check at that exact moment — simpler and more predictable than a second
`ShouldFormShieldWall` evaluation mid-withdrawal), and (mirroring the cavalry
`_shockCavalryRegrouped` field) stays regrouped for the rest of the battle rather than
re-entering the hold/advance cycle.

**Foot archers:** identical rules to horse archers (see the previous spec) — hold-and-skirmish
while ammo remains, kite away only when the enemy is within `MissileRangeAdjusted * 0.85` using
live positions, pursue in melee only if out of ammo and the nearest significant enemy formation
is locally weaker (`< 1.0`, the corrected sign from the previous pass's I1 fix), otherwise regroup
toward the infantry line.

## Wiring

New per-formation state fields in both mission-behavior files, mirroring the existing cavalry
fields exactly: `_infantryCommittedToAdvance`, `_infantryRegrouped`, `_infantryHoldStartTime`
(nullable `MissionTime?`, same pattern as `_awaitOpeningStartTime`).

- **`ExecuteStagingAndSkirmish`**: infantry still marches to `_anchorHighGround` (positioning, not
  yet a commit/hold decision — mirrors how cavalry's staging phase only repositions to
  `_leftFlankPosition`, it doesn't run the full stance machine either), but its arrangement is now
  `ShouldFormShieldWall(...) ? ArrangementOrderShieldWall : ArrangementOrderLine` instead of an
  unconditional shield wall. Foot archers call `ApplyFootArcherStance` instead of the current
  unconditional `MovementOrderCharge`.
- **`ExecuteDualFlankEncirclement`**: gains a new `ApplyInfantryStance(infantry)` call (currently
  infantry is untouched here) and `ApplyFootArcherStance(footArchers)`.
- **`ExecuteDecisiveHammerCharge`**: the existing unconditional `infantry.SetArrangementOrder(Line);
  infantry.SetMovementOrder(Charge)` and `footArchers.SetMovementOrder(Charge)` are replaced with
  `ApplyInfantryStance(infantry)` / `ApplyFootArcherStance(footArchers)`.

Applied identically to `ByzantineTacticMissionBehavior.cs` with the established `_byzantineTeam`/
`ThematicLastStand` substitutions.

## Error handling & performance

Unchanged conventions: the existing `try/catch → StandardEngineFallback` wrapper in
`OnMissionTick` already covers this new code; no new per-agent loops (every new signal — 
`CavalryUnitRatio`, `IsUnderRangedAttack`, `CasualtyRatio`, `LocalPowerRatio` on the closest
significant enemy formation — is a `FormationQuerySystem` property Native already computes every
tick, the same category of read the cavalry fix already established as free).

## Testing

Same split as the previous pass: `AssessInfantryStance` and `ShouldFormShieldWall` are pure
functions with zero `TaleWorlds.*` dependencies, unit-tested with xunit in the existing
`Source/SeljukEmpire.Tests/` project (no reuse-testing needed for foot archers — 
`AssessHorseArcherStance` is already fully covered by its existing 5 tests, and this pass adds no
new logic to it, only a new caller). The wiring files remain untestable outside a running
Bannerlord battle; verification is `dotnet build` plus manual playtest, same as before.

## Files touched

- Modify: `Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs` (add `AssessInfantryStance`,
  `ShouldFormShieldWall`)
- Modify: `Source/SeljukEmpire/Tactics/TuranTacticMissionBehavior.cs`
- Modify: `Source/SeljukEmpire/Tactics/ByzantineTacticMissionBehavior.cs`
- New tests: `Source/SeljukEmpire.Tests/InfantryStanceTests.cs`,
  `Source/SeljukEmpire.Tests/ShieldWallThresholdTests.cs` (or combined into one file — left to the
  implementation plan to decide file granularity)
- Not touched: `TacticalFormationsHelper.cs` (no new geometry helper needed — foot archer kiting
  reuses `CalculateFallbackVector` exactly as horse archers already do)

## Risks

- **Threshold guesswork for `isUnderHeavyRangedAttack`'s real-battle feel**: using Native's own
  boolean avoids the previous pass's float-misreading class of bug, but whether Native's own
  definition of "under ranged attack" trips at a moment that *feels* right for shield-wall timing
  is still something only a live playtest can confirm — flagged the same way the previous plan
  flagged its own two playtest scenarios.
- **`_infantryRegrouped` one-way disengage**: same design as cavalry's, same known tradeoff — an
  infantry line that regroups once will not re-engage that battle even if the tactical picture
  improves later. Accepted for consistency with the already-shipped cavalry behavior; revisiting
  both together, if ever, is a separate future decision, not part of this pass.
