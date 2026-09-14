# Italian, Polish, and Brazilian Portuguese localization

Date: 2026-09-14

## Context

The mod ships 8 languages today: EN + TR (required, ERROR-level in `verify_mod.py`) and
DE/FR/ES/RU/AR/CN (optional, WARN-level — "translating a new key into all 6 languages in a
follow-up commit is a normal workflow," per README). Roadmap Track E asked for IT/PL/PT as the next
languages with real Workshop demand. This spec adds them at the same optional/WARN tier as the
existing 6, not the required tier.

Each existing non-English language folder (`ModuleData/Languages/<CODE>/`) holds exactly two files:
a `strings.xml` with ~1791 `<string id="..." text="..."/>` entries (`ModuleData/Languages/strings.xml`,
the EN source, is the key list every other language must match) and a minimal `language_data.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<LanguageData id="Deutsch">
  <LanguageFile xml_path="DE/strings.xml" />
</LanguageData>
```

## Verified: exact `id=` values required for correct merging

Bannerlord's language system merges a module's `<LanguageFile>` content into whichever language the
player has selected by matching `LanguageData`'s `id=` attribute — not the folder name, which is
purely organizational (the mod already deviates from Native's own folder names: its `ES` folder maps
to Native's `SP`/`Español (LA)`, `CN` maps to Native's `CNs`/`简体中文`). Cross-checked every existing
mod language's `id=` against Native's own `ModuleData/Languages/<X>/language_data.xml` (game install)
and confirmed all 7 already match exactly (`Deutsch`, `Français`, `Español (LA)`, `Русский`,
`العربية`, `简体中文`, `Türkçe`). The same check for the three new languages found:

- **Italian** — Native ships `IT/`, `id="Italiano"`. Direct match, no ambiguity.
- **Polish** — Native ships `PL/`, `id="Polski"`. Direct match, no ambiguity.
- **Portuguese** — Native has **no generic Portuguese**. Its only Portuguese variant is
  `BR/`, `id="Português (BR)"` (Brazilian Portuguese). There is nothing to merge a generic "PT" id
  into — using anything other than `"Português (BR)"` would silently create an orphan language
  nobody can select. This mod's new folder is named `PT` (keeping the mod's own ISO-style naming
  convention, matching how `ES`/`CN` already diverge from Native's folder names) but its
  `language_data.xml` uses `id="Português (BR)"`.

Getting any of these three id strings wrong means the translated content exists in the mod's files
but never reaches a player — the failure is silent (no error, no crash, the strings simply never
merge), so this is called out explicitly rather than left implicit in an implementation task.

## Design

**New folders**: `ModuleData/Languages/IT/`, `PL/`, `PT/`, each with `language_data.xml` (id values
above) and a `strings.xml` containing a translation of every key currently in the root
`ModuleData/Languages/strings.xml` (EN source, ~1791 keys) — full completeness, not a partial subset,
matching the other 6 languages' existing tier.

**Translation**: produced directly (no external translation service or additional research
dependency), from the EN source, keeping terminology consistent within each language across the full
key set (a troop name, title, or recurring phrase translated the same way everywhere it appears).
Accepted at best-effort quality — not a certified-translator guarantee — same standing as the
existing DE/FR/ES/RU/AR/CN content.

**Execution shape**: given the volume (~1791 keys × 3 ≈ 5,370 strings), translation work is split by
content category (UI/system strings, troop/faction/title names, dialogue lines, quest/event text,
Encyclopedia lore) and run as parallel background tasks — one translation stream per language,
each internally reading enough of its own prior output to stay consistent, rather than one
5,370-line single pass. Batch boundaries and task count are an implementation-time decision, not
fixed here.

**Registration**: none needed beyond the two files per language — confirmed no `SubModule.xml`
`<XmlNode>` entries exist for any of the other 6 language folders either (language files are
auto-discovered from `ModuleData/Languages/*/language_data.xml`, unlike content XML).

## Verification

- `python tools/verify_mod.py` clean (0 errors) — `check_localization_coverage` confirms every
  `{=key}` used anywhere in the mod resolves in each new language file, `check_language_sync`
  reports the new languages at the same WARN (not ERROR) tier as the other 6.
- Spot-check ~20-30 keys per language across categories (not just the first N alphabetically) for
  obvious mistranslation, wrong register (formal/informal mismatches), or XML/encoding issues
  (accented characters, `&`/`<`/`>` escaping, right-to-left artifacts are not a concern here since
  none of the three use RTL script).
- Confirm no `id=` typos against the verified values above — this is the one failure mode
  `verify_mod.py` cannot catch, since a wrong id produces a syntactically valid, silently-orphaned
  language rather than an error.

## Out of scope

- Any language beyond these three.
- Retroactively translating content added *after* this pass ships — normal follow-up-commit workflow
  applies same as the existing 6 languages.
- A human native-speaker review pass — noted as valuable but not performed here; the user has
  accepted this tradeoff.
