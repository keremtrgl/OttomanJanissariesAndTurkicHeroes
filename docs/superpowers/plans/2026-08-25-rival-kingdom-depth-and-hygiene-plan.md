# Rival-Kingdom Depth, Verification & Repo Hygiene Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. This plan is content/data-modding work, not general software: "tests" below are XML well-formedness checks, id/key cross-reference scripts, and `dotnet build`, run the same way every prior content pass this session verified its own work.

**Goal:** Close out the three open items from the 2026-08-25 era-realism roadmap for the Byzantine/Abbasid/Georgian reskins (kingdom lore, diplomacy, succession depth — verified below, two of three already complete), add real historical consorts for the five best-documented rival-kingdom lords, run the still-open troop-tree balance audit, and do the repo hygiene pass — all previously flagged as open work in `docs/superpowers/specs/2026-08-25-seljuk-era-realism-and-roadmap-design.md`.

**Architecture:** Same attribute-only partial-XML-override pattern used for every prior content pass in this mod (`id` + one changed attribute, Native's own face/equipment/skills/AI stay untouched). No new C# for the content tasks; Task 4 (hygiene) is filesystem-only.

**Tech Stack:** Bannerlord ModuleData XML (partial overrides), C# (SeljukTactics.csproj, .NET), Python 3 (`xml.dom.minidom` + `re` for verification scripts, matching the method used throughout this session).

## Global Constraints

- Every new `{=key}` needs a matching entry in **both** `ModuleData/Languages/strings.xml` (EN) and `ModuleData/Languages/TR/strings.xml` (TR) — confirmed the hard way earlier this session; a key present in only one language silently falls back to raw XML text.
- Every new/changed `id` must be checked for zero collisions against the mod's own existing content files (settlements, troops, lords, factions across Seljuk/Byzantine/Abbasid/Georgian) — the same `comm -12` cross-check used all session.
- No claim of a "real historical" name without a source found during this plan's own research (recorded inline per task) or already recorded in this session's prior research. If no reliable source exists for a given figure, leave that entry at its current (Native default or already-renamed) value — do not invent one.
- `dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release` must report 0 warnings/0 errors after every C#-touching task (only Task 4 touches C#, and only via file deletion — no functional change).

---

## Task 1: Verify kingdom lore, diplomacy, and succession depth (no code — audit + written record)

This task exists because the user asked to confirm nothing is missing/broken for Byzantine/Abbasid/Georgian. All three sub-checks below were already run once this session; this task's job is to re-run them verifiably and record the result so a future session doesn't re-ask the same question.

**Files:**
- Read: `ModuleData/kingdoms.xml`
- Read (reference only, not modified): Native `SandBox/ModuleData/heroes.xml`, `SandBox/ModuleData/spclans.xml`, `SandBoxCore/ModuleData/spnpccharacters.xml`, `SandBox/ModuleData/lords.xml`, `SandBox/ModuleData/settlements.xml` at `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\`
- No files are modified by this task.

- [ ] **Step 1: Confirm kingdom-level lore text exists for all three reskins**

Run:
```bash
grep -A6 'id="aserai"\|id="sturgia"\|id="empire_s"' ModuleData/kingdoms.xml
```
Expected: all three `<Kingdom>` blocks already carry a `text="{=...}..."` attribute with a multi-sentence historical description (Byzantine: post-Manzikert remnant; Abbasid: Baghdad/nominal-suzerain-to-the-Sultan framing; Georgian: Caucasus frontier framing). If any of the three is missing `text=`, that is a real gap — write one matching the other two's register and length, add the EN/TR string pair, and note the addition in this task's completion line.

- [ ] **Step 2: Confirm kingdom_seljuks's relationship block covers all three, and only needs to exist on one side**

Run:
```bash
grep -A5 'id="kingdom_seljuks"' ModuleData/kingdoms.xml
```
Expected: a `<relationships>` block with `empire_s` (-30), `aserai` (20), `khuzait` (15), `sturgia` (0). Bannerlord relationship values are stored once per unordered faction pair, not directionally, so declaring this once from `kingdom_seljuks`'s side is sufficient — do not add mirrored `<relationships>` blocks under `empire_s`/`aserai`/`sturgia`, that would be redundant and risks a conflicting second value for the same pair if it ever drifts out of sync.

- [ ] **Step 3: Confirm Byzantine/Abbasid/Georgian clan owners already have real Native-assigned territory**

Run (from the installed game directory):
```bash
grep -A3 'id="clan_aserai_1"\|id="clan_aserai_7"\|id="clan_sturgia_1"' "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\SandBox\ModuleData\spclans.xml"
```
Expected: each clan already has `initial_home_settlement="Settlement.town_A1"` (etc.) in Native's own data, and the matching `Settlement` entry in Native's `settlements.xml` already carries `owner="Faction.clan_aserai_1"` (etc.) — confirmed for `clan_aserai_1`/`clan_aserai_7`/`clan_aserai_9`/`clan_aserai_3` already this session. This is why the "5 landless Seljuk beyliks" problem (fixed in commit `20a86f3`) does not apply to the 27 Byzantine/Abbasid/Georgian clans: those 27 are Native's own pre-built clans with pre-built territory, unlike the 11 Seljuk clans this mod built from nothing. No action needed here — this step is confirmation, not a fix.

- [ ] **Step 4: Confirm Byzantine/Abbasid/Georgian clans already have Native-provided family depth (spouses, children, sometimes multi-generation)**

Run:
```python
import re
content = open(r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\SandBox\ModuleData\heroes.xml", encoding="utf-8").read()
for hid in ["lord_1_15", "lord_2_1", "lord_3_1", "lord_3_3"]:
    m = re.search(rf'id="{hid}"[^/]*', content)
    print(hid, "->", m.group(0)[:200] if m else "NOT FOUND")
```
Expected: each shows an existing `spouse="Hero.xxx"` attribute in Native's own data (`lord_1_15`→`lord_1_16`, `lord_2_1`→`lord_2_2`, `lord_3_1`→`lord_3_2`, `lord_3_3`→`lord_3_4`), and some show children linked via `father=`/`mother=` (e.g. `lord_3_3_1`). This was cross-checked for all 27 clans this session: 22 of 27 owners have a Native spouse, and 42 Native-linked children exist across the 27 clans. No action needed here either — Native's dynamic world already covers succession for these clans, for free, because we only ever renamed the `name=` attribute on the single owning lord.

- [ ] **Step 5: Record the outcome**

All three sub-checks (lore, diplomacy, succession) come back "already correct, no gap" — this task produces no file changes, only this recorded confirmation. If Step 1 finds a missing `text=`, that's the one exception; handle it inline as described and note it in the commit message for Task 2 (below), since it's a one-line addition not worth a separate commit.

---

## Task 2: Add real historical consorts for the five best-documented rival-kingdom lords

**Files:**
- Modify: `ModuleData/byzantine_lords.xml`
- Modify: `ModuleData/abbasid_lords.xml`
- Modify: `ModuleData/georgian_lords.xml`
- Modify: `ModuleData/Languages/strings.xml`
- Modify: `ModuleData/Languages/TR/strings.xml`

**Interfaces:**
- Produces: 5 new `<NPCCharacter id="..." name="{=key}...">` entries (one per file below) and 10 new localization keys (5 EN + 5 TR, one pair per entry).

Historical sourcing (found this session, multiple independent sources per figure — see the chat transcript for the individual searches):
- `lord_3_2` (spouse of Al-Qa'im bi-Amr Allah, `clan_aserai_1`/Banu Abbas) → **Arslan Khatun**, attested as "the widow of the Caliph Qaim bi Amr Allah" who personally intervened in the 1087 marriage negotiations between al-Muqtadi and Malikshah's daughter (Rise of the Seljuqs and their State in Central Asia, cited this session).
- `lord_2_2` (spouse of David IV, `clan_sturgia_1`/Bagrationi) → **Gurandukht of the Kipchaks**, Wikipedia's "Family of David IV": David's Georgian medieval biographer names her as his one attested wife, married c. 1107 as part of the alliance that brought ~40,000 Kipchak warriors into Georgian service.
- `lord_1_30_1` (spouse of Alexios I Komnenos, `clan_empire_south_4`/Komnenos) → **Irene Doukaina**, Byzantine empress consort 1081-1118, mother of historian Anna Komnene and Emperor John II Komnenos (Wikipedia: Alexios I Komnenos, Irene Doukaina).
- `lord_1_18` (spouse of Andronikos Doukas, `clan_empire_south_3`/Doukas) → **Maria of Bulgaria**, confirmed by WikiTree, FamilySearch, and the Medieval Lands genealogical project (citing Nikephoros Bryennios directly) as Andronikos Doukas's wife, mother of Irene Doukaina.
- `lord_1_16` (spouse of Nikephoros III Botaneiates, `clan_empire_south_2`/Botaneiates) → **Maria of Alania**, daughter of King Bagrat IV of Georgia, empress consort to both Michael VII Doukas and Nikephoros III Botaneiates (Wikipedia: House of Doukas genealogy table).

- [ ] **Step 1: Add the Abbasid consort**

Edit `ModuleData/abbasid_lords.xml`, add after the `lord_3_1` entry:
```xml
  <NPCCharacter id="lord_3_2" name="{=abb_ln_arslan_khatun}Arslan Hatun" />
```

- [ ] **Step 2: Add the Georgian consort**

Edit `ModuleData/georgian_lords.xml`, add after the `lord_2_1` entry:
```xml
  <NPCCharacter id="lord_2_2" name="{=geo_ln_gurandukht}Gurandukht" />
```

- [ ] **Step 3: Add the three Byzantine consorts**

Edit `ModuleData/byzantine_lords.xml`, add after their respective owner entries (`lord_1_15`, `lord_1_17`, `lord_1_30`):
```xml
  <NPCCharacter id="lord_1_16" name="{=byz_ln_maria_alania}Maria of Alania" />
  <NPCCharacter id="lord_1_18" name="{=byz_ln_maria_bulgaria}Maria of Bulgaria" />
  <NPCCharacter id="lord_1_30_1" name="{=byz_ln_irene_doukaina}Irene Doukaina" />
```

- [ ] **Step 4: Verify zero id collisions with the mod's existing content**

Run:
```python
import re
new_ids = {"lord_3_2", "lord_2_2", "lord_1_16", "lord_1_18", "lord_1_30_1"}
existing = set()
for f in ["ModuleData/settlements.xml","ModuleData/troops.xml","ModuleData/lords.xml","ModuleData/factions.xml",
          "ModuleData/byzantine_clans.xml","ModuleData/byzantine_settlements.xml","ModuleData/byzantine_troops.xml",
          "ModuleData/abbasid_clans.xml","ModuleData/abbasid_settlements.xml","ModuleData/abbasid_troops.xml",
          "ModuleData/georgian_clans.xml","ModuleData/georgian_settlements.xml","ModuleData/georgian_troops.xml",
          "ModuleData/heroes.xml"]:
    existing |= set(re.findall(r'id="([a-zA-Z0-9_]+)"', open(f, encoding="utf-8").read()))
print("Collisions:", new_ids & existing)
```
Expected: `Collisions: set()`. (These five ids are expected to NOT already appear in our byzantine_lords.xml/abbasid_lords.xml/georgian_lords.xml themselves before this task — that's fine, this check is only against files that declare *other* ids, i.e. it catches accidental reuse of an id already claimed elsewhere in the mod, not the intentional first declaration these steps are adding.)

- [ ] **Step 5: Add the 5 EN strings**

Edit `ModuleData/Languages/strings.xml`, insert before `</strings>`:
```xml
    <string id="abb_ln_arslan_khatun" text="Arslan Khatun" />
    <string id="geo_ln_gurandukht" text="Gurandukht" />
    <string id="byz_ln_maria_alania" text="Maria of Alania" />
    <string id="byz_ln_maria_bulgaria" text="Maria of Bulgaria" />
    <string id="byz_ln_irene_doukaina" text="Irene Doukaina" />
```

- [ ] **Step 6: Add the 5 TR strings**

Edit `ModuleData/Languages/TR/strings.xml`, insert before `</strings>`:
```xml
    <string id="abb_ln_arslan_khatun" text="Arslan Hatun" />
    <string id="geo_ln_gurandukht" text="Gurandukht" />
    <string id="byz_ln_maria_alania" text="Maria of Alania" />
    <string id="byz_ln_maria_bulgaria" text="Maria of Bulgaria" />
    <string id="byz_ln_irene_doukaina" text="İrini Dukaina" />
```

- [ ] **Step 7: Validate XML well-formedness**

Run:
```bash
python3 -c "
import xml.dom.minidom as m
for f in ['ModuleData/byzantine_lords.xml','ModuleData/abbasid_lords.xml','ModuleData/georgian_lords.xml','ModuleData/Languages/strings.xml','ModuleData/Languages/TR/strings.xml']:
    m.parse(f); print(f, 'OK')
"
```
Expected: `OK` for all 5 files.

- [ ] **Step 8: Verify every key used has both EN and TR entries**

Run:
```python
import re
files = ["ModuleData/byzantine_lords.xml","ModuleData/abbasid_lords.xml","ModuleData/georgian_lords.xml"]
used = set()
for f in files:
    used |= set(re.findall(r'\{=([a-zA-Z0-9_]+)\}', open(f, encoding="utf-8").read()))
en = set(re.findall(r'<string id="([a-zA-Z0-9_]+)"', open("ModuleData/Languages/strings.xml", encoding="utf-8").read()))
tr = set(re.findall(r'<string id="([a-zA-Z0-9_]+)"', open("ModuleData/Languages/TR/strings.xml", encoding="utf-8").read()))
missing_en = used - en
missing_tr = used - tr
print("Missing EN:", missing_en, "Missing TR:", missing_tr)
```
Expected: `Missing EN: set() Missing TR: set()`.

- [ ] **Step 9: Commit**

```bash
git add ModuleData/byzantine_lords.xml ModuleData/abbasid_lords.xml ModuleData/georgian_lords.xml ModuleData/Languages/strings.xml ModuleData/Languages/TR/strings.xml
git commit -m "Add real historical consorts for the 5 best-documented rival-kingdom lords"
```

---

## Task 3: Troop-tree balance audit — Seljuk vs. Native Khuzait

This is Work stream D from the original roadmap, not yet done. This task is analysis, not a fixed set of edits: it produces a written finding, and only edits `ModuleData/troops.xml` if the analysis finds a real imbalance.

**Files:**
- Read: `ModuleData/troops.xml` (Seljuk tree)
- Read (reference): Native `SandBoxCore/ModuleData/spnpccharacters.xml`, Khuzait troop lines (`khuzait_recruit` through `khuzait_horse_archer`/`marksman`/`heavy_cavalry` chains)
- Modify (only if a real gap is found): `ModuleData/troops.xml`

- [ ] **Step 1: Extract the Seljuk troop tree's tier/stat table**

Run:
```python
import re
content = open("ModuleData/troops.xml", encoding="utf-8").read()
for m in re.finditer(r'<NPCCharacter\s+id="(seljuk_[a-zA-Z0-9_]+)"[^>]*level="(\d+)"', content):
    print(m.group(1), m.group(2))
```
Record the full id→level table (expect ~15-20 entries spanning tier 1 through tier 6, per the tier structure already aligned to Native conventions per commit `1cc7251`).

- [ ] **Step 2: Extract the equivalent Khuzait table from the installed game**

Run the same regex against `spnpccharacters.xml` filtered to `khuzait_` ids, for direct comparison (same command shape as Step 1, different file/prefix).

- [ ] **Step 3: Compare equipment value per tier**

For each Seljuk troop and its same-tier Khuzait counterpart, sum the `value=` of every `<equipment slot="..." id="Item.xxx">` referenced in its `<EquipmentRoster>` (cross-referencing `ModuleData/items.xml` for custom Seljuk items and Native's equivalent for Khuzait items). A same-tier gap larger than ~15% in either direction is the threshold worth flagging, matching how tight the native tier-to-tier progression already runs.

- [ ] **Step 4: Compare upgrade cost and skill floors per tier**

Native troop upgrades cost gold plus have skill-based unlock behavior; compare `upgrade_target`/skill values the same way, tier by tier.

- [ ] **Step 5: Record findings and fix only what's out of line**

If tiers are within the ~15% band already (this is the expected outcome — the mod's own commit history already did one alignment pass in `1cc7251`, and clan tiers changing afterward per `8063538` only gates which tiers an AI army can field, it doesn't change any individual troop's own stats), write that up as the finding and stop — no edit needed. If a specific tier is genuinely out of line, edit only that troop's equipment/skill values in `ModuleData/troops.xml`, matching the format of its neighboring entries.

- [ ] **Step 6: If Step 5 required an edit, validate and commit**

```bash
python3 -c "import xml.dom.minidom as m; m.parse('ModuleData/troops.xml'); print('OK')"
git add ModuleData/troops.xml
git commit -m "Rebalance <specific tier> against Native Khuzait after tier audit"
```
(If Step 5 found no gap, skip this step — there is nothing to commit.)

---

## Task 4: Repository hygiene — remove dev-session artifacts

This is Work stream E from the original roadmap, not yet done. Low-risk: every item here is confirmed unreferenced by the `.csproj` or `SubModule.xml` (the mod has built and run successfully all session without them).

**Files:**
- Delete: `Inspect.cs`, `Inspect.exe` (repo root, if present)
- Delete: `InspectTemp.cs`, `InspectTemp.exe` (repo root, if present)
- Delete: `inspect_types.py` (repo root, if present)
- Delete: `scratch/` (repo root, if present)
- Delete: `OttomanJanissariesAndTurkicHeroes/` (nested stale git checkout, if present)
- Delete: `.agents/`, `skill-creator/`, `skills/` (tooling scaffolding, if present — verify each is not itself a live skill/plugin directory still in use before deleting)

- [ ] **Step 1: Confirm none of these paths are referenced by the build or SubModule.xml**

Run:
```bash
grep -ril "Inspect\|InspectTemp\|inspect_types\|OttomanJanissariesAndTurkicHeroes" Source/SeljukEmpire/SeljukTactics.csproj SubModule.xml
```
Expected: no matches. If any match is found, stop this task and report it — that means one of these "dev artifacts" is actually load-bearing and must not be deleted.

- [ ] **Step 2: List what actually exists at repo root before deleting anything**

Run:
```bash
ls -la
```
Only delete paths from this task's file list that actually appear in this listing — do not delete anything not explicitly named above, and do not delete `.claude/`, `docs/`, `ModuleData/`, `Source/`, `bin/`, `.git/`, `README.md`, `SubModule.xml`, `preview.jpg`/`preview.png`.

- [ ] **Step 3: Remove the confirmed artifacts**

```bash
git rm -rf Inspect.cs Inspect.exe InspectTemp.cs InspectTemp.exe inspect_types.py scratch/ OttomanJanissariesAndTurkicHeroes/ .agents/ skill-creator/ skills/ 2>/dev/null || true
```
(The `|| true` is because not every path is guaranteed to exist in this worktree — `git rm` on a nonexistent path errors, and that's fine to ignore here since Step 2 already confirmed the real list.)

- [ ] **Step 4: Confirm the mod still builds after removal**

```bash
dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release
```
Expected: `0 Uyarı` / `0 Hata` (0 warnings / 0 errors), same as every prior build this session.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Remove dev-session artifacts not part of the shipped mod (repo hygiene)"
```

---

## Sequencing

Tasks 1 and 2 are the direct answer to "check Byzantine/Abbasid/Georgian for gaps and add real detail" — do these first. Task 3 (troop-tree audit) and Task 4 (hygiene) are independent of each other and of Tasks 1-2; either order is fine. None of the four tasks depend on live gameplay.
