# IT/PL/PT Localization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add full Italian, Polish, and Brazilian Portuguese translations (~1795 keys each, matching every key in `ModuleData/Languages/strings.xml`) at the same optional/WARN tier as the mod's existing DE/FR/ES/RU/AR/CN languages.

**Architecture:** Three new sibling folders under `ModuleData/Languages/` (`IT/`, `PL/`, `PT/`), each with a minimal `language_data.xml` and a full `strings.xml` translation. No code changes — this is content-only, mirroring the existing 6 optional languages' file shape exactly.

**Tech Stack:** XML content files; `tools/verify_mod.py` as the correctness gate (no custom validation tooling needed — it already implements `check_localization_coverage`/`check_language_sync`).

## Global Constraints

- `language_data.xml` `id=` values, verified against Native's own shipped language files, must be exactly: `Italiano` (IT), `Polski` (PL), `Português (BR)` (PT). Getting these wrong produces a silently orphaned language — no error, the content just never merges into what a player selects.
- Every `<string id="X" text="...">` in the EN source (`ModuleData/Languages/strings.xml`) must have a matching `id="X"` entry in each new file — same id set, full coverage, not a partial subset.
- `{PLAYER.NAME}`, `{FACTION_LEADER}`, `[if:...]`, `[ib:...]`, `{=key}` and any other bracketed/braced token inside a `text=` value is a game-engine placeholder, not translatable content — copy it through unchanged in every translated string.
- XML attribute values must stay valid: escape `&` as `&amp;`, `<` as `&lt;`, `>` as `&gt;`, and preserve the file's `utf-8` encoding (accented characters written directly, not as numeric entities, matching how the existing DE/FR/ES files already handle `ä`/`é`/`ñ` etc.).
- This pass covers EN+TR-sourced content only (the current ~1795-key file) — content added after this pass ships is a normal follow-up commit, not a gap to backfill retroactively here.

## Source file shape (confirmed)

`ModuleData/Languages/strings.xml` is 1803 lines: a 6-line header (`<?xml version='1.0' encoding='utf-8'?>`,
`<base type="string">`, `<tags>`, `<tag language="English" />`, `</tags>`, `<strings>`), 1795
`<string id="..." text="..." />` entries (lines 7-1801), then a 2-line closer (`</strings>`,
`</base>`). Every existing non-English language file mirrors this exact shape with translated
`text=` values and identical `id=` values.

---

### Task 1: Italian (`IT/`)

**Files:**
- Create: `ModuleData/Languages/IT/language_data.xml`
- Create: `ModuleData/Languages/IT/strings.xml`

**Interfaces:** none (content-only, independent of the other two language tasks).

- [ ] **Step 1: Create the language registration file**

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData id="Italiano">
  <LanguageFile xml_path="IT/strings.xml" />
</LanguageData>
```

- [ ] **Step 2: Translate the source file in 6 batches**

Read `ModuleData/Languages/strings.xml`. For each of the following line ranges, translate every
`text=` value into Italian while copying `id=` values and every bracketed/braced engine token through
unchanged (per Global Constraints), keeping proper nouns (clan names, place names, character names)
transliterated consistently the same way across all 6 batches, not re-decided per batch:

- Batch 1: lines 7-305
- Batch 2: lines 306-604
- Batch 3: lines 605-903
- Batch 4: lines 904-1202
- Batch 5: lines 1203-1501
- Batch 6: lines 1502-1801 (up to, not including, the closing `</strings>`/`</base>`)

These 6 batches may be translated as parallel background tasks since they don't depend on each
other's output, but all 6 write into the same `IT/strings.xml`, wrapped in the same header/footer
from Step 1's file shape — assemble them into one well-formed XML file in id order matching the
source file's order (not batch-completion order), so a later diff against the source is easy to
read.

- [ ] **Step 3: Verify well-formedness and coverage**

```bash
python -c "
import xml.etree.ElementTree as ET
src = {el.get('id') for el in ET.parse('ModuleData/Languages/strings.xml').getroot().iter('string')}
it = {el.get('id') for el in ET.parse('ModuleData/Languages/IT/strings.xml').getroot().iter('string')}
missing = src - it
extra = it - src
print('missing:', len(missing), sorted(missing)[:10])
print('extra:', len(extra), sorted(extra)[:10])
"
```

Expected: `missing: 0 []` and `extra: 0 []` — the id set must match exactly. If not, find and fix the
gap before proceeding (a missing id is what `verify_mod.py`'s `check_localization_coverage` would
otherwise catch, but checking it directly here is faster to iterate on than a full mod-wide run).

- [ ] **Step 4: Run the mod's own check**

```bash
python tools/verify_mod.py
```

Expected: 0 errors. Italian should no longer appear in `check_language_sync`'s WARN list for any
existing key.

- [ ] **Step 5: Spot-check quality**

Read 25-30 translated entries spread across the 6 batches (not just the first batch) — confirm
natural Italian phrasing, correct historical/military terminology (e.g. "cavalleria pesante" not a
literal word-for-word calque), and that no engine token got mistranslated or dropped.

- [ ] **Step 6: Commit**

```bash
git add ModuleData/Languages/IT/
git commit -m "$(cat <<'EOF'
Add full Italian localization (~1795 keys)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Polish (`PL/`)

**Files:**
- Create: `ModuleData/Languages/PL/language_data.xml`
- Create: `ModuleData/Languages/PL/strings.xml`

**Interfaces:** none — identical shape to Task 1, independent of it.

- [ ] **Step 1: Create the language registration file**

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData id="Polski">
  <LanguageFile xml_path="PL/strings.xml" />
</LanguageData>
```

- [ ] **Step 2: Translate the source file in 6 batches**

Same batch line ranges and rules as Task 1 Step 2, target language Polish. Note Polish grammatical
gender/case agreement is more involved than Italian for titles and profession names (e.g. a female
character's title needs its own inflected form where the source text distinguishes gender via
`{?PLAYER.GENDER}...{?}...{\?}` tokens) — preserve those conditional tokens exactly and provide the
correctly-inflected Polish text inside each branch, not a single ungendered translation.

- [ ] **Step 3: Verify well-formedness and coverage**

Same script as Task 1 Step 3, pointed at `ModuleData/Languages/PL/strings.xml`. Expected: `missing: 0`, `extra: 0`.

- [ ] **Step 4: Run the mod's own check**

```bash
python tools/verify_mod.py
```

Expected: 0 errors.

- [ ] **Step 5: Spot-check quality**

Same as Task 1 Step 5, in Polish — pay particular attention to the gendered-title entries flagged in
Step 2.

- [ ] **Step 6: Commit**

```bash
git add ModuleData/Languages/PL/
git commit -m "$(cat <<'EOF'
Add full Polish localization (~1795 keys)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Brazilian Portuguese (`PT/`)

**Files:**
- Create: `ModuleData/Languages/PT/language_data.xml`
- Create: `ModuleData/Languages/PT/strings.xml`

**Interfaces:** none — identical shape to Task 1, independent of it.

- [ ] **Step 1: Create the language registration file**

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData id="Português (BR)">
  <LanguageFile xml_path="PT/strings.xml" />
</LanguageData>
```

- [ ] **Step 2: Translate the source file in 6 batches**

Same batch line ranges and rules as Task 1 Step 2, target language Brazilian Portuguese specifically
(not European Portuguese) — vocabulary and register should match Brazilian usage (e.g. "você" forms
rather than "tu", terminology consistent with Native's own `BR/` files' general register where it
overlaps with common Bannerlord vocabulary like troop tiers or settlement types).

- [ ] **Step 3: Verify well-formedness and coverage**

Same script as Task 1 Step 3, pointed at `ModuleData/Languages/PT/strings.xml`. Expected: `missing: 0`, `extra: 0`.

- [ ] **Step 4: Run the mod's own check**

```bash
python tools/verify_mod.py
```

Expected: 0 errors.

- [ ] **Step 5: Spot-check quality**

Same as Task 1 Step 5, in Brazilian Portuguese.

- [ ] **Step 6: Commit**

```bash
git add ModuleData/Languages/PT/
git commit -m "$(cat <<'EOF'
Add full Brazilian Portuguese localization (~1795 keys)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: Cross-language verification and in-game confirmation

**Files:** none (verification only).

**Interfaces:** Consumes the completed `IT/`, `PL/`, `PT/` folders from Tasks 1-3.

- [ ] **Step 1: Full mod-wide check**

```bash
python tools/run_all_checks.py
```

Expected: 0 errors, 0 warnings for EN/TR (unchanged), and IT/PL/PT now appear alongside
DE/FR/ES/RU/AR/CN in whatever summary `check_language_sync` prints, with no missing-key WARNs for any
of the three.

- [ ] **Step 2: Confirm the language selector shows three new merged options, not orphans**

Deploy the rebuilt mod (`ModuleData/`, matching this mod's existing deploy convention) to the local
game install, open the game's language selector in the options menu, and confirm "Italiano",
"Polski", and "Português (BR)" each appear as a single entry (not duplicated) and that selecting one
shows this mod's translated strings alongside Native's own already-Italian/Polish/Portuguese UI text
— this is the one failure mode `verify_mod.py` cannot catch (a wrong `id=` value produces a
syntactically valid but orphaned language, not an error).

- [ ] **Step 3: Fix and re-verify if Step 2 finds an orphaned language**

If any of the three doesn't appear merged correctly, the `id=` value in that language's
`language_data.xml` doesn't match Native's exact string — re-check against
`<game_install>/Modules/Native/ModuleData/Languages/<IT|PL|BR>/language_data.xml`'s own `id=`
attribute, fix, rebuild, and re-run Step 2.

```bash
git add ModuleData/Languages/
git commit -m "$(cat <<'EOF'
Fix language id mismatch found during in-game verification

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```
