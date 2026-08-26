# Seljuk Empire: Sword of Islam

A Mount & Blade II: Bannerlord mod that adds the Great Seljuk Empire (Büyük Selçuklu Devleti) as a playable historical faction — 11 Seljuk/Turkic clans and beyliks, custom troop trees, historical lords, and dedicated campaign systems (recruitment, diplomacy, tournaments, caravans, dialogue).

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

This requires the game to be installed locally, since the project references Bannerlord's managed assemblies from the game's `bin/Win64_Shipping_Client` folder.

## Verifying content changes

Before committing any change to `ModuleData/`, run the integrity checker:

```bash
python tools/verify_mod.py
```

It checks XML validity, that every content file is registered in `SubModule.xml`, that no
id is defined twice across the mod's own files, that every `{=key}` used anywhere has a
matching localization string in both `strings.xml` and `TR/strings.xml` (and, as a warning,
in the mod's other 6 shipped languages — see "Keeping all 8 languages in sync" below), and —
when a local Bannerlord install is found (or passed via `--game-path`) — that every equipped
item id and troop upgrade target actually exists, and that renamed Native characters keep a
consistent gender flag. Run with `--quick` to skip the game-install-dependent checks, or
`--json` for machine-readable output. Every one of these checks has caught a real bug during
this mod's development at least once.

### Automatic pre-commit check

Run once per clone to make `verify_mod.py` run automatically before every commit, blocking
the commit if it finds an ERROR-level issue:

```bash
tools/install-hooks.sh          # Git Bash / macOS / Linux
```
```powershell
tools\install-hooks.ps1         # PowerShell
```

This points git at the tracked `.githooks/` directory (`git config core.hooksPath .githooks`)
— `.git/hooks/` itself is never committed, so every clone needs this one-time step. To bypass
deliberately for a single commit (not recommended), use `git commit --no-verify`.

### Keeping all 8 languages in sync

The mod ships full localization in 8 languages, but only `strings.xml` (EN) and
`TR/strings.xml` are treated as required — English is what an unset or mistranslated key
falls back to at runtime, and Turkish is this mod's original authoring language, so those two
fail the check with an ERROR. The other 6 (`DE`/`FR`/`ES`/`RU`/`AR`/`CN`) are checked too, but
as a WARN, listing exactly which languages a newly-added `{=key}` hasn't reached yet — visible
on every run instead of silently drifting for months, which is exactly how this mod once
shipped with those 6 languages frozen at 228 of 892 keys while EN/TR kept growing untranslated
underneath them. Translating a new key into all 6 languages in a follow-up commit is a normal
workflow and this check will never block it; it only makes sure the gap can't go unnoticed.

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

## Repository layout

- `ModuleData/` — troops, heroes, kingdoms, factions, settlements, items, localization, etc.
- `Source/SeljukEmpire/` — the mod's C# gameplay behaviors.
- `tools/` — `verify_mod.py`, the content integrity checker, and `install-hooks.sh`/`.ps1` (see above).
- `.githooks/` — the tracked `pre-commit` hook that `install-hooks.sh`/`.ps1` wires up.
- `bin/` — prebuilt `SeljukTactics.dll` (already included, so building from source is optional for players).
