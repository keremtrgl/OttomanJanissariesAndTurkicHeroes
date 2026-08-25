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
matching localization string in both `strings.xml` and `TR/strings.xml`, and — when a local
Bannerlord install is found (or passed via `--game-path`) — that every equipped item id and
troop upgrade target actually exists, and that renamed Native characters keep a consistent
gender flag. Run with `--quick` to skip the game-install-dependent checks, or `--json` for
machine-readable output. Every one of these checks has caught a real bug during this mod's
development at least once.

## Repository layout

- `ModuleData/` — troops, heroes, kingdoms, factions, settlements, items, localization, etc.
- `Source/SeljukEmpire/` — the mod's C# gameplay behaviors.
- `tools/` — `verify_mod.py`, the content integrity checker (see above).
- `bin/` — prebuilt `SeljukTactics.dll` (already included, so building from source is optional for players).
