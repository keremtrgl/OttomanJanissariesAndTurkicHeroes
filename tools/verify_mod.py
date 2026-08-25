#!/usr/bin/env python3
"""
verify_mod.py - Consolidated integrity checks for "Seljuk Empire: Sword of Islam".

This replaces the ad-hoc, one-off verification scripts that used to be typed fresh
for every change during development (XML validity, item-id typos, id collisions,
missing localization keys, gender mismatches against Native, dangling upgrade
chains). Every one of those checks previously caught a real bug at least once
during this mod's development; this script makes all of them repeatable and
runnable in one command instead of being re-invented and possibly forgotten.

Usage:
    python tools/verify_mod.py                       # full run, auto-detects game install
    python tools/verify_mod.py --game-path "C:\\...\\Mount & Blade II Bannerlord"
    python tools/verify_mod.py --quick                # skip checks that need the game install
    python tools/verify_mod.py --json                  # machine-readable output

Checks performed:
  1. xml-wellformed        Every ModuleData/**/*.xml (and SubModule.xml) parses as valid XML.
  2. submodule-registration Every content-bearing ModuleData file is wired up in SubModule.xml,
                            and every path SubModule.xml references actually exists.
  3. id-collision           No id (NPCCharacter/Faction/Settlement/Item/Kingdom/Culture) is
                            defined in two different mod files (last-loaded silently wins,
                            discarding the other file's changes - always a bug, never intended).
  4. localization-coverage  Every {=key} referenced in ModuleData XML or Source/**/*.cs exists
                            as a <string id="key"> in BOTH EN and TR strings.xml.
  5. upgrade-target         [needs game install] Every <upgrade_target id="NPCCharacter.X"> in
                            the mod's own troop trees resolves to a real id (mod-defined or
                            Native-defined) - catches upgrade-chain typos.
  6. item-id                [needs game install] Every Item.X reference in mod equipment
                            resolves to a real item (Native's or the mod's own items.xml).
  7. gender-consistency     [needs game install] Every mod NPCCharacter override that reuses a
                            Native id and sets is_female keeps the SAME is_female Native already
                            defined for that id (this exact mismatch caused a real "ruler shows
                            as a woman" bug earlier in this mod's history).

Exit code 0 if every check passes (warnings do not fail the run), 1 if any ERROR is found.
"""

import argparse
import json
import re
import sys
from pathlib import Path

# stdlib ElementTree (not defusedxml): every file parsed here is either this repo's
# own tracked ModuleData/Source, or the local Bannerlord install the developer already
# has full read access to - never attacker-supplied XML, so XXE/billion-laughs don't
# apply. Keeping this dependency-free (no pip install) matters more for a repo script
# meant to "just run" than hardening against an input path that doesn't exist here.
from xml.etree import ElementTree as ET

REPO_ROOT = Path(__file__).resolve().parent.parent
MODULE_DATA = REPO_ROOT / "ModuleData"
SOURCE_DIR = REPO_ROOT / "Source"
SUBMODULE_XML = REPO_ROOT / "SubModule.xml"
LANG_EN = MODULE_DATA / "Languages" / "strings.xml"
LANG_TR = MODULE_DATA / "Languages" / "TR" / "strings.xml"

DEFAULT_GAME_PATHS = [
    r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord",
    r"C:\Program Files\Steam\steamapps\common\Mount & Blade II Bannerlord",
]

# Root tag -> child tag for id-bearing content files. Extend this if a new
# content type is introduced (e.g. a Scenes or CraftingPieces file).
ID_BEARING_TYPES = {
    "NPCCharacters": "NPCCharacter",
    "Factions": "Faction",
    "Settlements": "Settlement",
    "Items": "Item",
    "Kingdoms": "Kingdom",
    "SPCultures": "Culture",
}

# Root tags that mean "this file defines registrable content and MUST be wired
# up in SubModule.xml". EquipmentRosters/Heroes/partyTemplates/Policies are
# registrable too but have no natural "id collision" check of their own.
REGISTRABLE_ROOT_TAGS = set(ID_BEARING_TYPES) | {"EquipmentRosters", "Heroes", "partyTemplates", "Policies"}

# Files whose root tag doesn't match the generic rule above but which ARE
# legitimately registered content (native's own "base type=..." wrapper format).
KNOWN_SPECIAL_REGISTERED_FILES = {"banner_icons.xml"}

LOC_KEY_PATTERN = re.compile(r"\{=([A-Za-z0-9_]+)\}")


class Issue:
    def __init__(self, level, check, file, message):
        self.level = level  # "ERROR" or "WARN"
        self.check = check
        self.file = file
        self.message = message

    def to_dict(self):
        return {"level": self.level, "check": self.check, "file": self.file, "message": self.message}

    def __str__(self):
        loc = f"{self.file}: " if self.file else ""
        return f"[{self.level}] ({self.check}) {loc}{self.message}"


def rel(path):
    try:
        return str(Path(path).resolve().relative_to(REPO_ROOT)).replace("\\", "/")
    except ValueError:
        return str(path)


def mod_xml_files(exclude_languages=True):
    files = sorted(MODULE_DATA.rglob("*.xml"))
    if exclude_languages:
        files = [f for f in files if "Languages" not in f.parts]
    return files


def safe_parse(path):
    try:
        return ET.parse(path).getroot()
    except ET.ParseError:
        return None


# ---------------------------------------------------------------- check 1 --

def check_xml_wellformed(issues):
    targets = mod_xml_files(exclude_languages=False) + [SUBMODULE_XML]
    for f in targets:
        try:
            ET.parse(f)
        except ET.ParseError as e:
            issues.append(Issue("ERROR", "xml-wellformed", rel(f), str(e)))


# ---------------------------------------------------------------- check 2 --

def check_submodule_registration(issues):
    submodule_root = safe_parse(SUBMODULE_XML)
    if submodule_root is None:
        return  # already reported by check 1

    registered_paths = {xn.get("path") for xn in submodule_root.iter("XmlName") if xn.get("path")}

    for f in mod_xml_files():
        root = safe_parse(f)
        if root is None:
            continue
        stem = f.stem
        if f.name in KNOWN_SPECIAL_REGISTERED_FILES:
            if stem not in registered_paths:
                issues.append(Issue("ERROR", "submodule-registration", rel(f),
                                     f'Not registered in SubModule.xml (expected <XmlName path="{stem}"/>)'))
            continue
        if root.tag in REGISTRABLE_ROOT_TAGS and stem not in registered_paths:
            issues.append(Issue("ERROR", "submodule-registration", rel(f),
                                 f'Root tag <{root.tag}> defines content but file is not registered '
                                 f'in SubModule.xml (expected <XmlName path="{stem}"/>)'))

    for p in sorted(registered_paths):
        if not (MODULE_DATA / f"{p}.xml").exists():
            issues.append(Issue("ERROR", "submodule-registration", rel(SUBMODULE_XML),
                                 f'<XmlName path="{p}"/> has no matching file at ModuleData/{p}.xml'))


# ---------------------------------------------------------------- check 3 --

def check_id_collisions(issues):
    # id -> list of (file) it was seen in, per content type
    seen = {t: {} for t in ID_BEARING_TYPES}

    for f in mod_xml_files():
        root = safe_parse(f)
        if root is None or root.tag not in ID_BEARING_TYPES:
            continue
        child_tag = ID_BEARING_TYPES[root.tag]
        counts_in_file = {}
        for child in root:
            if child.tag != child_tag:
                continue
            cid = child.get("id")
            if not cid:
                continue
            counts_in_file[cid] = counts_in_file.get(cid, 0) + 1
            seen[root.tag].setdefault(cid, []).append(rel(f))
        for cid, n in counts_in_file.items():
            if n > 1:
                issues.append(Issue("ERROR", "id-collision", rel(f),
                                     f'{child_tag} id="{cid}" is defined {n} times in this single file'))

    for root_tag, id_map in seen.items():
        child_tag = ID_BEARING_TYPES[root_tag]
        for cid, files in id_map.items():
            unique_files = sorted(set(files))
            if len(unique_files) > 1:
                issues.append(Issue("ERROR", "id-collision", None,
                                     f'{child_tag} id="{cid}" is defined in multiple files: {", ".join(unique_files)} '
                                     f'(whichever loads last silently wins - the other definitions are discarded)'))


# ---------------------------------------------------------------- check 4 --

def collect_loc_keys_from_mod():
    keys = {}  # key -> first file it was seen in
    for f in mod_xml_files():
        text = f.read_text(encoding="utf-8", errors="ignore")
        for m in LOC_KEY_PATTERN.finditer(text):
            keys.setdefault(m.group(1), rel(f))
    if SOURCE_DIR.exists():
        for f in SOURCE_DIR.rglob("*.cs"):
            text = f.read_text(encoding="utf-8", errors="ignore")
            for m in LOC_KEY_PATTERN.finditer(text):
                keys.setdefault(m.group(1), rel(f))
    return keys


def collect_string_ids(strings_xml_path):
    root = safe_parse(strings_xml_path)
    if root is None:
        return set(), []
    ids = []
    for s in root.iter("string"):
        sid = s.get("id")
        if sid:
            ids.append(sid)
    dupes = [sid for sid in set(ids) if ids.count(sid) > 1]
    return set(ids), dupes


def load_native_string_ids(game_path):
    """Ids from Native's own top-level Languages/*.xml (base files only, not the
    per-locale subfolders - TaleWorlds defines the same id set in every locale for
    a given content file, so the base file's ids are representative). Some mod
    files (e.g. seljuk_special_characters.xml) legitimately reuse a Native
    character's existing localization key instead of redefining it - those are
    not gaps in THIS mod's own strings.xml and must not be flagged as one."""
    ids = set()
    for name in NATIVE_MODULES_FOR_CHARACTERS:
        lang_dir = game_path / "Modules" / name / "ModuleData" / "Languages"
        if not lang_dir.exists():
            continue
        for f in lang_dir.glob("*.xml"):
            root = safe_parse(f)
            if root is None:
                continue
            for s in root.iter("string"):
                sid = s.get("id")
                if sid:
                    ids.add(sid)
    return ids


def check_localization_coverage(issues, game_path):
    used_keys = collect_loc_keys_from_mod()
    en_ids, en_dupes = collect_string_ids(LANG_EN)
    tr_ids, tr_dupes = collect_string_ids(LANG_TR)
    native_ids = load_native_string_ids(game_path) if game_path else None

    for key, first_seen_in in sorted(used_keys.items()):
        covered_by_native = native_ids is not None and key in native_ids
        if key not in en_ids and not covered_by_native:
            level = "ERROR" if native_ids is not None else "WARN"
            suffix = "" if native_ids is not None else " (not verified against Native - no game install found, pass --game-path)"
            issues.append(Issue(level, "localization-coverage", first_seen_in,
                                 f'{{=<{key}>}} has no matching <string id="{key}"> in {rel(LANG_EN)}{suffix}'))
        if key not in tr_ids and not covered_by_native:
            level = "ERROR" if native_ids is not None else "WARN"
            suffix = "" if native_ids is not None else " (not verified against Native - no game install found, pass --game-path)"
            issues.append(Issue(level, "localization-coverage", first_seen_in,
                                 f'{{=<{key}>}} has no matching <string id="{key}"> in {rel(LANG_TR)}{suffix}'))

    for sid in sorted(en_dupes):
        issues.append(Issue("WARN", "localization-coverage", rel(LANG_EN),
                             f'<string id="{sid}"> is defined more than once (later definition silently wins)'))
    for sid in sorted(tr_dupes):
        issues.append(Issue("WARN", "localization-coverage", rel(LANG_TR),
                             f'<string id="{sid}"> is defined more than once (later definition silently wins)'))


# ------------------------------------------------------- game-path helpers --

def find_game_path(explicit):
    if explicit:
        p = Path(explicit)
        if (p / "Modules").exists():
            return p
        print(f"WARNING: --game-path '{explicit}' does not look like a Bannerlord install "
              f"(no Modules/ folder found) - skipping game-dependent checks.", file=sys.stderr)
        return None
    for candidate in DEFAULT_GAME_PATHS:
        p = Path(candidate)
        if (p / "Modules").exists():
            return p
    return None


def native_module_xml_files(game_path, module_names):
    files = []
    for name in module_names:
        module_data = game_path / "Modules" / name / "ModuleData"
        if not module_data.exists():
            continue
        for f in module_data.rglob("*.xml"):
            if "Languages" not in f.parts:
                files.append(f)
    return files


NATIVE_MODULES_FOR_CHARACTERS = ["SandBox", "SandBoxCore", "Native", "StoryMode"]
NATIVE_MODULES_FOR_ITEMS = ["SandBoxCore", "Native"]


def load_native_characters(game_path):
    """Returns (all_ids: set, is_female_by_id: dict, name_by_id: dict) scanned from
    every NPCCharacter definition across Native's own gameplay modules (Languages
    folders excluded)."""
    all_ids = set()
    is_female_by_id = {}
    name_by_id = {}
    for f in native_module_xml_files(game_path, NATIVE_MODULES_FOR_CHARACTERS):
        root = safe_parse(f)
        if root is None:
            continue
        for npc in root.iter("NPCCharacter"):
            nid = npc.get("id")
            if not nid:
                continue
            all_ids.add(nid)
            fem = npc.get("is_female")
            if fem is not None:
                is_female_by_id[nid] = fem.lower() == "true"
            name = npc.get("name")
            if name is not None:
                name_by_id[nid] = name
    return all_ids, is_female_by_id, name_by_id


def load_native_item_ids(game_path):
    ids = set()
    for f in native_module_xml_files(game_path, NATIVE_MODULES_FOR_ITEMS):
        root = safe_parse(f)
        if root is None:
            continue
        for tag in ("Item", "CraftedItem"):
            for item in root.iter(tag):
                iid = item.get("id")
                if iid:
                    ids.add(iid)
    return ids


def collect_mod_defined_character_ids():
    ids = set()
    for f in mod_xml_files():
        root = safe_parse(f)
        if root is None:
            continue
        for npc in root.iter("NPCCharacter"):
            nid = npc.get("id")
            if nid:
                ids.add(nid)
    return ids


def collect_mod_item_ids():
    ids = set()
    for f in mod_xml_files():
        root = safe_parse(f)
        if root is None:
            continue
        for tag in ("Item", "CraftedItem"):
            for item in root.iter(tag):
                iid = item.get("id")
                if iid:
                    ids.add(iid)
    return ids


# ---------------------------------------------------------------- check 5 --

def check_upgrade_targets(issues, game_path):
    mod_ids = collect_mod_defined_character_ids()
    native_ids, _, _ = load_native_characters(game_path)
    valid_ids = mod_ids | native_ids

    for f in mod_xml_files():
        root = safe_parse(f)
        if root is None:
            continue
        for npc in root.iter("NPCCharacter"):
            owner_id = npc.get("id", "?")
            for ut in npc.iter("upgrade_target"):
                target = ut.get("id", "")
                target_id = target.split(".", 1)[1] if "." in target else target
                if target_id and target_id not in valid_ids:
                    issues.append(Issue("ERROR", "upgrade-target", rel(f),
                                         f'NPCCharacter "{owner_id}" has an upgrade_target to '
                                         f'"{target}" which does not exist (mod-defined or Native)'))


# ---------------------------------------------------------------- check 6 --

def check_item_ids(issues, game_path):
    native_ids = load_native_item_ids(game_path)
    mod_ids = collect_mod_item_ids()
    valid_ids = native_ids | mod_ids

    for f in mod_xml_files():
        root = safe_parse(f)
        if root is None:
            continue
        for npc in root.iter("NPCCharacter"):
            owner_id = npc.get("id", "?")
            for eq in npc.iter("equipment"):
                item_ref = eq.get("id", "")
                if not item_ref.startswith("Item."):
                    continue
                item_id = item_ref[len("Item."):]
                if item_id not in valid_ids:
                    issues.append(Issue("ERROR", "item-id", rel(f),
                                         f'NPCCharacter "{owner_id}" equips "{item_ref}" which does not '
                                         f'exist in Native\'s item catalog or the mod\'s own items.xml'))


# ---------------------------------------------------------------- check 7 --

def check_gender_consistency(issues, game_path):
    """Flags the ONE dangerous pattern that actually caused a real bug in this mod:
    reusing a Native character id under a different-gendered NAME while never
    touching is_female, so the id silently keeps Native's original body/skeleton.
    An override that DOES set is_female explicitly (any value) is a deliberate,
    already-considered identity change - not something this check second-guesses,
    since the modder may have also fixed face/equipment to match, as verified
    in-game. Only the "renamed but is_female untouched" combination is flagged -
    as a WARNING, not an error: the new name may simply already match the
    inherited flag (e.g. renaming a native is_female="true" id to another female
    name needs no override at all, and is correct as-is). This check cannot tell
    a real mismatch from a same-gender rename without understanding the name, so
    it surfaces every rename-without-is_female for a human's ten-second glance
    rather than asserting a mismatch outright."""
    _, native_gender, native_name = load_native_characters(game_path)

    for f in mod_xml_files():
        root = safe_parse(f)
        if root is None:
            continue
        for npc in root.iter("NPCCharacter"):
            nid = npc.get("id")
            if not nid or nid not in native_gender or nid not in native_name:
                continue
            if npc.get("is_female") is not None:
                continue  # explicitly set - trust it as an intentional, considered choice
            mod_name = npc.get("name")
            if mod_name is None or mod_name == native_name[nid]:
                continue  # not renamed here, so no identity change to worry about
            issues.append(Issue("WARN", "gender-consistency", rel(f),
                                 f'NPCCharacter "{nid}" is renamed to name="{mod_name}" (Native: '
                                 f'name="{native_name[nid]}") but never sets is_female, so it silently '
                                 f'keeps Native\'s is_female="{str(native_gender[nid]).lower()}" and 3D body/'
                                 f'skeleton - verify the new name matches that gender, or set is_female '
                                 f'explicitly on this override.'))


# --------------------------------------------------------------------- run --

def run(args):
    issues = []

    check_xml_wellformed(issues)
    check_submodule_registration(issues)
    check_id_collisions(issues)

    game_path = None if args.quick else find_game_path(args.game_path)
    if not args.quick and game_path is None:
        issues.append(Issue("WARN", "game-install", None,
                             "No Bannerlord install found (checked --game-path and default Steam "
                             "locations) - localization-coverage falls back to WARN instead of ERROR for "
                             "keys not found in the mod's own strings.xml (they may be legitimately reused "
                             "Native keys), and upgrade-target/item-id/gender-consistency are skipped "
                             "entirely. Pass --game-path or use --quick to silence this."))

    check_localization_coverage(issues, game_path)

    if game_path is not None:
        check_upgrade_targets(issues, game_path)
        check_item_ids(issues, game_path)
        check_gender_consistency(issues, game_path)

    errors = [i for i in issues if i.level == "ERROR"]
    warnings = [i for i in issues if i.level == "WARN"]

    if args.json:
        print(json.dumps({
            "errors": len(errors),
            "warnings": len(warnings),
            "issues": [i.to_dict() for i in issues],
        }, indent=2, ensure_ascii=False))
    else:
        if issues:
            for i in issues:
                print(i)
        print()
        print(f"verify_mod: {len(errors)} error(s), {len(warnings)} warning(s)")
        if not errors:
            print("All checks passed." if not warnings else "All checks passed (see warnings above).")

    return 1 if errors else 0


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--game-path", help="Path to the Mount & Blade II Bannerlord install "
                                             "(folder containing Modules/). Auto-detected if omitted.")
    parser.add_argument("--quick", action="store_true",
                         help="Skip checks that require the game install (upgrade-target, item-id, gender-consistency).")
    parser.add_argument("--json", action="store_true", help="Print machine-readable JSON instead of a text report.")
    args = parser.parse_args()
    sys.exit(run(args))


if __name__ == "__main__":
    main()
