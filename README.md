# Seljuk Empire: Sword of Islam

A Mount & Blade II: Bannerlord mod that adds the Great Seljuk Empire (Büyük Selçuklu Devleti) as a playable historical faction — 11 Seljuk/Turkic clans and beyliks, custom troop trees, historical lords, and dedicated campaign systems (recruitment, diplomacy, tournaments, caravans, dialogue).

See [CHANGELOG.md](CHANGELOG.md) for what changed in each version.

## Requirements

- Mount & Blade II: Bannerlord (current live branch)
- Depended modules (already part of the base game): `Native`, `SandBoxCore`, `Sandbox`, `CustomBattle`, `StoryMode`

## Installation

1. Download the latest release zip from the [Releases](../../releases) page.
2. Extract it. You should get a single folder named `OttomanJanissariesAndTurkicHeroes`.
3. Copy that folder into your Bannerlord `Modules` directory, e.g.:
   ```
   C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\
   ```
   After copying, the path `...\Modules\OttomanJanissariesAndTurkicHeroes\SubModule.xml` should exist.
4. Launch the game through the Bannerlord Launcher (not directly through Steam), open the **Mods** tab, and enable **Seljuk Empire: Sword of Islam**.
5. Start a new campaign to see the Seljuk faction in the world.

## Building from source

The repository also includes the mod's C# source (`Source/SeljukEmpire`) for anyone who wants to modify the behaviors. To rebuild the DLL:

```bash
dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release
```

The build writes `bin/Win64_Shipping_Client/SeljukTactics.dll` and mirrors it into
`bin/Win64_Shipping_wEditor/`. It finds Bannerlord's engine assemblies like this:

1. **Game installed in the default Steam location** – used automatically.
2. **Game installed elsewhere** – point the build at it:
   `dotnet build ... -p:BannerlordGameFolder="D:\Games\Mount & Blade II Bannerlord"`
   (or set a `BannerlordGameFolder` / `BannerlordBinPath` environment variable).
3. **No game install at all** (Linux, macOS, CI) – the build falls back to the public
   [`Bannerlord.ReferenceAssemblies.Core`](https://www.nuget.org/packages/Bannerlord.ReferenceAssemblies.Core)
   NuGet package. Its default version is pinned in the `.csproj`; override it with
   `-p:BannerlordReferenceAssembliesVersion=<version>` to match your game version.

A reference-assembly build is not a lesser build. TaleWorlds assemblies are all versioned
1.0.0.0 and bound by name, so a DLL compiled against the matching version's reference assemblies
has exactly the same external member references as one compiled against the game's own DLLs.
This was checked by diffing both builds' metadata tables.

## Verifying content changes

Before committing any change, run the checker:

```bash
python tools/run_all_checks.py
```

This is the single entry point for both of the mod's independent check systems: `tools/
verify_mod.py` (content integrity — see below) and `dotnet test` on `Source/
SeljukEmpire.Tests/` (unit tests for the mod's engine-independent logic: the reactive tactical
AI's `TacticalSituationAssessor`, the volunteer-slot recruitment rules, the caravan-insurance
claim rules and the greeting rotation). Either suite is skipped gracefully (not failed)
if its tool isn't available on the machine (`dotnet`, or no local Bannerlord install for
verify_mod.py's game-dependent checks). Any flag `run_all_checks.py` doesn't recognize itself
(`--game-path`, `--quick`, `--json`, `--check-workshop-conflicts`, `--update-baseline`) is
forwarded straight through to `verify_mod.py`; use `--skip-dotnet-test` or `--skip-verify-mod`
to run only one suite.

`verify_mod.py` on its own can still be run directly the same way it always has
(`python tools/verify_mod.py ...`) if you only want the content-integrity half. It checks XML
validity, that every content file is registered in `SubModule.xml`, that no id is defined
twice across the mod's own files, that every `{=key}` used anywhere has a matching
localization string in both `strings.xml` and `TR/strings.xml` (and, as a warning, in the
mod's other 9 shipped languages — see "Keeping all 11 languages in sync" below), and — when a
local Bannerlord install is found (or passed via `--game-path`) — that every equipped item id
and troop upgrade target actually exists, that renamed Native characters keep a consistent
gender flag, and several other checks documented in its own module docstring. Run with
`--quick` to skip the game-install-dependent checks, or `--json` for machine-readable output.
Every one of these checks has caught a real bug during this mod's development at least once.

### Continuous integration

`.github/workflows/ci.yml` runs on every push and pull request. It builds the mod DLL against
the oldest and newest supported game versions' reference assemblies, into a scratch folder so
the committed `bin/` is never touched. It then runs `tools/run_all_checks.py --quick`, which is
the unit tests plus every `verify_mod.py` check that doesn't need a local game install.

### Automatic pre-commit check

Run once per clone to make `run_all_checks.py` run automatically before every commit, blocking
the commit if it finds an issue:

```bash
tools/install-hooks.sh          # Git Bash / macOS / Linux
```
```powershell
tools\install-hooks.ps1         # PowerShell
```

This points git at the tracked `.githooks/` directory (`git config core.hooksPath .githooks`)
— `.git/hooks/` itself is never committed, so every clone needs this one-time step. To bypass
deliberately for a single commit (not recommended), use `git commit --no-verify`.

### Keeping all 11 languages in sync

The mod ships full localization in 11 languages, but only `strings.xml` (EN) and
`TR/strings.xml` are treated as required — English is what an unset or mistranslated key
falls back to at runtime, and Turkish is this mod's original authoring language, so those two
fail the check with an ERROR. The other 9 (`DE`/`FR`/`ES`/`RU`/`AR`/`CN`/`IT`/`PL`/`PT`) are
checked too, but as a WARN, listing exactly which languages a newly-added `{=key}` hasn't
reached yet — visible on every run instead of silently drifting for months, which is exactly
how this mod once shipped with its original 6 secondary languages frozen at 228 of 892 keys
while EN/TR kept growing untranslated underneath them. Translating a new key into all 9
languages in a follow-up commit is a normal workflow and this check will never block it; it
only makes sure the gap can't go unnoticed.

### Save compatibility (id order)

Bannerlord assigns every `NPCCharacter`/`Item`/`Faction`/`Settlement`/`Kingdom`/`Culture` a
save-file identity by **registration order** during XML load, not by its string id (confirmed
by decompiling `TroopRosterElement`'s serialization). Appending new ids at the very end — of
both a file's own element list and `SubModule.xml`'s `<XmlNode>` list — is safe. Inserting,
removing, or reordering anything *before* an already-shipped id silently shifts every later
id's assigned identity, so an existing save's roster or inventory can resolve to a
**different, wrong** troop or item on load — not a crash, a silent mismatch that's much
harder to notice and diagnose.

`verify_mod.py` guards against this by comparing the current order against a frozen snapshot,
`tools/shipped_ids_baseline.json`. Only advance that snapshot once you've confirmed the
current state is actually safe to ship (typically: right after a release goes out):

```bash
python tools/verify_mod.py --update-baseline
```

Do **not** run `--update-baseline` to silence a failure you haven't understood — that defeats
the point of the check. If it fails, either move your change so it only appends after the
existing entries, or, if breaking existing saves for this change is a deliberate, accepted
tradeoff, update the baseline knowingly.

### Reactive tactical AI performance

`Source/SeljukEmpire.Benchmarks/` is a small microbenchmark for
`TacticalSituationAssessor` (the pure decision-logic layer `TuranTacticMissionBehavior`/
`ByzantineTacticMissionBehavior` call from their own tick handler), built the same
engine-independent way as `Source/SeljukEmpire.Tests/`:

```bash
dotnet run -c Release --project Source/SeljukEmpire.Benchmarks
```

It measures nanoseconds-per-call for each stance-assessment method and reports that cost
against the actual call frequency: both tactical mission behaviors gate their entire decision
loop behind a 1.25-second throttle timer (`_tickThrottleTimer` in the shared `DoctrineTacticMissionBehaviorBase.OnMissionTick`), the
same "don't do the expensive thing every frame" approach `BattlePerformanceOptimizer` already
uses elsewhere. This can't measure the engine-side cost of reading `Formation.QuerySystem` or
issuing orders (that only exists inside a running mission), but it does confirm the assessor's
own logic — even at a deliberately pessimistic call count — is a negligible fraction of a
60fps frame budget. Re-run this if `TacticalSituationAssessor.cs` ever grows a loop or
allocation it doesn't have today; that's the kind of change this benchmark exists to catch.

## Repository layout

- `ModuleData/` — troops, heroes, kingdoms, factions, settlements, items, localization, etc.
- `Source/SeljukEmpire/` — the mod's C# gameplay behaviors.
- `Source/SeljukEmpire.Tests/` — xUnit unit tests for the mod's engine-independent logic
  (tactical AI decisions, recruitment slot rules, insurance claim rules, greeting rotation);
  builds and runs without the game installed.
- `Source/SeljukEmpire.Benchmarks/` — microbenchmark for the same decision logic's raw CPU cost
  (see "Reactive tactical AI performance" below); also builds and runs without the game.
- `tools/` — `run_all_checks.py` (single entry point for both check systems), `verify_mod.py`
  (the content integrity checker), and `install-hooks.sh`/`.ps1` (see above).
- `.githooks/` — the tracked `pre-commit` hook that `install-hooks.sh`/`.ps1` wires up.
- `.github/workflows/` — CI (see "Continuous integration" above).
- `bin/` — prebuilt `SeljukTactics.dll` (already included, so building from source is optional for players).
