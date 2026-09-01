# Reactive tactical AI for cavalry — Seljuk & Byzantine doctrine engines

Date: 2026-09-01

## Context

Reported bug (player-observed, in Turkish): Seljuk cavalry "bluntly head-charges into the
enemy, loses the horses, and comes back" — a repeating charge/lose/retreat cycle that reads as
dumb, not tactical, despite the mod's `TuranTacticMissionBehavior` already implementing four
named doctrines (Turan Wolf-Trap, Nizamiye Shield Wall, Steppe Crossfire, High-Ground Ambush)
with historical framing and localized battle messages (documented in
`graphify-out/graph.md` section 4).

Reading the actual source (`Source/SeljukEmpire/Tactics/TuranTacticMissionBehavior.cs`) confirmed
three concrete root causes, not just "the AI feels bad":

1. **Horse archers get `MovementOrderCharge` from the very first phase** (`ExecuteStagingAndSkirmish`),
   before the enemy has even closed — they ride straight into melee instead of skirmishing at
   range, despite being fragile in melee by design.
2. **All four doctrines converge on identical cavalry orders** once `DualFlankEncirclement` is
   reached — the doctrine only changes the opening message and whether a feigned-retreat phase
   runs; by the time cavalry actually charges, every doctrine issues the same
   `MovementOrderCharge` to both Cavalry and HorseArcher formations with no distinction between
   them.
3. **Every phase transition is gated purely by fixed timers and distances** (14s/16s/22s,
   90m/55m/30m) — there is no read of casualties, relative combat power, or whether the enemy
   formation is braced and stationary (spear/shield wall — very effective against cavalry in
   Bannerlord's own combat model). A charge that is going badly is never recalled; the FSM just
   keeps advancing through its scripted phases regardless of outcome.

`ByzantineTacticMissionBehavior.cs` explicitly documents that it "reuses the same proven phase
engine" as the Seljuk one — it has the identical structure and the identical flaw. Confirmed with
the user: both get fixed together, sharing the new reactive logic in one place rather than
duplicating it (matching the existing duplication the Byzantine file's own doc comment already
flags).

Decompiling `TaleWorlds.MountAndBlade.dll` (`ilspycmd`, per this mod's established
decompile-verify convention) confirmed that Native's own `FormationQuerySystem` (accessible as
`Formation.QuerySystem`) already computes, every tick, exactly the signals a genuinely reactive
AI needs — this design consumes that existing data rather than inventing parallel heuristics:

- `CasualtyRatio` — this formation's own losses so far, live.
- `LocalPowerRatio` (+ `LocalAllyPower`/`LocalEnemyPower`) — local combat-power comparison,
  already weighted by Native's own model (not just headcount).
- `MovementSpeedMaximum` — near-zero means the formation is holding position, not advancing.
- `InfantryUnitRatio` / `HasShieldUnitRatio` — composition signal for "this looks like a braced
  line."
- `IsUnderRangedAttack` / `UnderRangedAttackRatio` — whether a formation is currently taking
  missile fire.
- `MissileRangeAdjusted` — this formation's own effective engagement range.
- `ClosestSignificantlyLargeEnemyFormation` — returns another `FormationQuerySystem` for the
  nearest large enemy formation, so all of the above can be read about *the enemy in front of us*,
  not just ourselves.

## Goals

- Horse archers behave like real horse archers: hold range, shoot, reposition away if the enemy
  closes; never default to a melee charge just because ammo ran out.
- Shock cavalry does not charge a braced, stationary, shield/spear-heavy enemy formation head-on;
  it waits for a real opening (the line breaks, moves, or has already been softened) and, if a
  charge already in progress is clearly going badly, disengages to a rally point instead of
  fighting to the last man by default.
- The four existing doctrines keep their identity (names, historical framing, localized UI
  messages) but their execution becomes situational instead of scripted — and the top-level
  doctrine itself can shift once, mid-battle, if the army as a whole is taking a beating.
- Seljuk and Byzantine tactical AI share one reactive decision layer instead of duplicating it.

## Non-goals

- Infantry (`NizamiyeShieldWall` positioning) and foot archer behavior are not reported as broken
  and are not touched by this pass.
- No siege-battle behavior (both mission behaviors already exit early on
  `Mission.Current.IsSiegeBattle`, unchanged — see the existing repo roadmap's own separate,
  unspec'd "siege tactical AI" idea).
- No new doctrines, no doctrine renaming, no changes to `EvaluateAndSelectDoctrine`'s *initial*
  selection thresholds (cavalry ratio, infantry ratio, outnumber ratio) — only when/whether it
  re-runs mid-battle.

## Architecture

A new stateless, shared file: `Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs`. Both
`TuranTacticMissionBehavior` and `ByzantineTacticMissionBehavior` keep their existing 5-phase FSM
(`StagingAndSkirmish` → `FeignedRetreatBait` → `DualFlankEncirclement` → `DecisiveHammerCharge`)
untouched as the *outer* structure — this design does not replace it, it replaces what happens
*inside* the phases that currently issue blind `MovementOrderCharge` calls.

```csharp
public enum FormationStance
{
    AdvanceAndCharge,   // conditions favor committing to melee
    HoldAndSkirmish,    // ranged formation: hold/increase range, keep shooting
    AwaitOpening,       // cavalry: hold at staging position, re-check next tick
    Regroup,             // fall back to a rally point, disengaging from a losing fight
    Pursue                // enemy is broken/fleeing — chase to finish, melee is fine
}
```

`TacticalSituationAssessor` exposes two pure query functions (formation + its `QuerySystem`, plus
whatever timing context the caller already tracks, in; enum out — no side effects, no
order-issuing, and no fields of its own):

- `AssessHorseArcherStance(Formation self, bool isDefensivePosture)`
- `AssessShockCavalryStance(Formation self, float secondsSinceAwaitOpeningStarted, bool isDefensivePosture)`

`isDefensivePosture` is `true` when the currently active doctrine is `HighGroundAmbush` (whether
chosen initially or downgraded into mid-battle — see below); it tightens every threshold below
rather than branching into separate code paths, so the defensive doctrine reuses the same
decision functions instead of a parallel implementation.

`secondsSinceAwaitOpeningStarted` exists for the 25-second timeout described below — the assessor
has no fields to track elapsed time itself, so the mission behavior (which already owns
`_phaseTimer` for the enclosing phase) measures it and passes it in each call, the same way it
already passes phase-elapsed time into its own `_phaseTimer.ElapsedSeconds > 16f`-style checks
today. This keeps `TacticalSituationAssessor` genuinely stateless while still letting the timeout
work.

The two mission behaviors call these functions from inside their existing phase methods and
translate the returned stance into the actual `MovementOrder`/`ArrangementOrder` calls (order
issuing stays in the mission behaviors, which already own `_seljukTeam`/`_anchorHighGround`/etc. —
the assessor never touches `Mission.Current` state directly beyond reading `QuerySystem`).

## Behavior rules

### Horse archers — `HoldAndSkirmish` by default

While the formation has ammunition (`!TacticalFormationsHelper.IsRangedAmmoDepleted`), it never
returns `AdvanceAndCharge`. If the nearest enemy is closer than `MissileRangeAdjusted * 0.85` (a
safety margin so archers start opening distance before they're actually in melee range, not at
the exact moment they are), the stance is `HoldAndSkirmish` and the mission behavior computes a
fall-back point (new helper, see below) opposite the enemy's approach direction, re-issued every
tick like the existing `ExecuteFeignedRetreat` already does for the whole formation.

Once ammo is depleted, the choice is between `Pursue` and `Regroup`, decided by
`ClosestSignificantlyLargeEnemyFormation.LocalPowerRatio` (read from the *enemy* formation's own
query, which is symmetric — if they're locally beaten, our side's ratio is favorable). `> 1.0` is
favorable → `Pursue` (join the mop-up in melee, this is the one case melee is fine for an
empty-quivered horse archer); `<= 1.0` (including the tie case, treated as unfavorable — the safer
default) or no significant enemy formation found → `Regroup`, falling back toward the infantry
line rather than wading into a fight they can't win unarmed-at-range. This same `> 1.0` /
`<= 1.0` reading of `LocalPowerRatio` is used consistently everywhere else in this design (the
cavalry mid-charge disengage below, and the doctrine-level downgrade).

### Shock cavalry — `AwaitOpening` gate before charging, live disengage during

Before a charge is issued, query `Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation`.
If it's stationary (`MovementSpeedMaximum` below a small epsilon, ~0.5) and composed of
infantry/shield units (`InfantryUnitRatio >= 0.5 || HasShieldUnitRatio >= 0.5`) — a braced line —
the stance is `AwaitOpening`: hold at the flank staging position, re-evaluate next tick. Three
things can open the gate: the enemy formation starts moving (speed rises past the epsilon — the
line broke to advance or pursue), the enemy formation's own `CasualtyRatio` has climbed past 0.15
(our infantry/archers are already softening it), or a 25-second hard timeout elapses (a fail-safe
so cavalry can never stall forever against an enemy AI that simply never advances — after the
timeout, charge anyway; a genius commander still has to commit eventually).

Once a charge is underway (`AdvanceAndCharge` already issued), the same function is polled again
on the next tick with the formation now in contact: if `CasualtyRatio > 0.35` (0.25 when
`isDefensivePosture`) **and** `LocalPowerRatio <= 1.0` (the same unfavorable reading used
throughout this design), the stance flips to `Regroup` — a fall-back
order to a rally point, not an unmanaged retreat. This is the direct fix for "loses them and
comes back": today nothing ever recalls a bad charge; this makes that an explicit, controlled
decision instead of an emergent mess.

### Doctrine-level re-evaluation (one-way downgrade)

A new dedicated timer, `_doctrineReevalTimer` (deliberately separate from `_phaseTimer` and
`_tickThrottleTimer` — this codebase already hit and fixed the exact bug of reusing one timer for
two purposes in `OnMissionTick`'s own comment; this design does not repeat it). Every ~9 seconds,
if the average `CasualtyRatio` across all non-empty friendly formations (`CountOfUnits > 0`, the
same filter `EvaluateAndSelectDoctrine` already applies when tallying troop types) exceeds 0.40,
the active doctrine is forced to `HighGroundAmbush` regardless of what was originally selected. This is
one-way for the rest of that battle — it never upgrades back to a more aggressive doctrine
mid-fight, to avoid visibly flip-flopping. This is the top-level answer to "responds differently
to different situations": a Wolf-Trap opening that is actually going badly stops being a
Wolf-Trap.

### New geometry helper

`TacticalFormationsHelper` gains one addition: `CalculateFallbackVector(Vec3 selfPos, Vec3
enemyPos, float distance)` — the mirror of the existing `CalculateFlankVector`, returning a point
directly away from the enemy rather than to the side. Used by both the horse-archer kiting
reposition and the cavalry `Regroup` rally point. No other existing helper changes.

## Error handling & performance

Unchanged conventions, deliberately: the outer `try/catch → StandardEngineFallback` in
`OnMissionTick` still wraps everything, so any unexpected exception in the new assessor still
degrades to native AI rather than crashing the mission. `TacticalSituationAssessor`'s functions
only read already-computed `QuerySystem` values (Native updates these every tick regardless of
this mod's involvement) — no new per-agent loops are introduced beyond what
`IsRangedAmmoDepleted` already does, so the added cost per throttled tick (still every 1.25s) is
a handful of property reads and float comparisons, not a new scan.

## Testing / verification

This is behavioral C# logic, outside `verify_mod.py`'s scope (XML/id/localization integrity, not
runtime AI behavior). Verification is: `dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c
Release` must report 0 warnings/errors, then manual custom-battle verification with two scenarios
chosen to exercise the exact reported failure mode and its fix:

1. Seljuk cavalry-heavy army vs. a Byzantine Tagma army left stationary/braced — expected: Seljuk
   shock cavalry visibly holds at the flank instead of charging immediately; horse archers kite
   and shoot instead of closing to melee.
2. A deliberately lopsided custom battle (Seljuk force heavily outnumbered/outclassed) — expected:
   doctrine downgrades to `HighGroundAmbush` mid-battle and a charge already in progress
   disengages instead of fighting to the last horse.

## Files touched

- New: `Source/SeljukEmpire/Tactics/TacticalSituationAssessor.cs`
- Modify: `Source/SeljukEmpire/Tactics/TuranTacticMissionBehavior.cs`
- Modify: `Source/SeljukEmpire/Tactics/ByzantineTacticMissionBehavior.cs`
- Modify: `Source/SeljukEmpire/Tactics/TacticalFormationsHelper.cs` (add
  `CalculateFallbackVector`)
- Not touched by this spec, but a known follow-up once implemented: `graphify-out/graph.md`
  section 4 ("Çok Doktrinli Taktik Yapay Zeka Motoru") documents the old scripted behavior and
  will need the same kind of update pass this mod already does after significant engine changes.

## Risks

- **Over-cautious cavalry**: if the `AwaitOpening` thresholds are too conservative, cavalry could
  read as passive/useless instead of smart. The 25-second hard timeout and the "enemy formation
  already taking casualties" release condition both exist specifically to bound this.
- **Doctrine downgrade thrashing**: mitigated by making the downgrade one-way per battle (no
  re-upgrade), so the visible behavior is a single decisive shift, not oscillation.
- **Divergence between the two mission behaviors over time**: mitigated by putting the actual
  decision logic in the shared `TacticalSituationAssessor` rather than duplicating it into both
  files again, which is exactly the duplication this pass is trying to not add to.
