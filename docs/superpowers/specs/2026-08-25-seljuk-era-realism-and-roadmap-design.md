# Seljuk Empire mod — era-realism factions & long-term development roadmap

Date: 2026-08-25

## Context

Following the crash-fix session (2026-08-24/25, see prior specs and commit history), the mod is
now stable: New Campaign completes, the 11 Seljuk clans are tier-rebalanced (3–6 instead of
uniformly 5–6), the royal domain was expanded with Konya/Söğüt (renamed from Native's leftover
placeholder names Danustica/Husn Fulq) plus İsfahan/Nişabur/Rey Kalesi, and the mod carries full
English/Turkish localization. It's published to Steam Workshop (private, marked as in-development)
under item id `3789607078`.

This spec covers the next round of work, requested as "an extremely detailed, long development
plan" plus one specific new content item: non-playable rival/neighbor factions from the same
historical period, to be added purely as reskins of existing Native kingdoms (confirmed with user:
no new `is_main_culture="true"` cultures — that path is what caused every crash this mod hit this
session, since Native requires dozens of culture-scoped registries — `notable_templates`,
`child_template_*`, `teenager_template_*`, the full child-education equipment/character set,
`father_char_creation_*`/`mother_char_creation_*`/`player_char_creation_*` — that don't exist for
any culture we'd invent from scratch).

All Native IDs/values below were read directly from the installed game at
`C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\SandBox\ModuleData\{spkingdoms,spclans}.xml`, not guessed.

---

## Work stream A — Byzantine / Abbasid / Georgian reskins (this session's requested addition)

**Approach (confirmed with user):** rename three existing Native kingdoms via the same
attribute-only partial-XML-override pattern this mod already uses for settlements — add a
`<Kingdom id="...">` entry to our own `ModuleData/kingdoms.xml` with only the changed attributes.
Troops, heroes, AI, and internal mechanics of these kingdoms are untouched; only name, title,
description text, banner colors, and select diplomatic relationships change.

### A.1 — Byzantine Empire (`Kingdom.empire_s`)

Native: `id="empire_s"`, name/title `{=frBQ9mbP}Southern Empire`, `ruler_title="Senator"`,
`culture="Culture.empire"`, owner `Hero.lord_1_14`. **Already at war with Aserai natively**
(`<relationship kingdom="Kingdom.aserai" value="-1" isAtWar="true"/>`). This is the correct pick
over the other two Empire successor states (`empire`, `empire_w`) — it's the one whose clans
(`clan_empire_south_3`, `clan_empire_south_4`) we already took Konya's/İsfahan's territory from,
so it's our actual, already-adjacent neighbor. The Northern/Western Empire stay Native-named; they
have no border with us and renaming them adds no realism, only inconsistency risk (three
"Byzantiums" fighting each other reads worse than one, correctly placed).

Changes: `name`/`short_name`/`title` → "Byzantine Empire" (EN) / "Bizans İmparatorluğu" (TR),
`ruler_title` "Senator" → "Emperor" / "İmparator", new `text` description grounding it as the
post-Manzikert Anatolian remnant. Banner colors kept Native (already visually Roman/Byzantine —
purple/gold, no change needed).

### A.2 — Abbasid Caliphate (`Kingdom.aserai`)

Native: `id="aserai"`, title `{=UsbwPmYb}Sultanate of the Aserai`, `ruler_title="Sultan"`,
`culture="Culture.aserai"`, owner `Hero.lord_3_1`. Single kingdom (no north/south split like
Empire), already at war with `empire_s`, already the kingdom we took Rey Kalesi/Nişabur territory
from (`clan_aserai_2`, `clan_aserai_8`).

Changes: `title` → "Abbasid Caliphate" (EN) / "Abbasi Halifeliği" (TR), `ruler_title` "Sultan" →
"Caliph" / "Halife" (historically the Seljuk Sultan and the Abbasid Caliph are *different*
titles held by different men — this distinguishes them from our own `ruler_title="Sultan"` on
`kingdom_seljuks`, which is correct: Seljuks held real power, the Caliph was nominal suzerain).
New `text` describing Baghdad and the nominal-suzerainty dynamic with the Seljuks.

### A.3 — Kingdom of Georgia (`Kingdom.sturgia`)

Native: `id="sturgia"`, title `{=iYCR3xuQ}Principality of Sturgia`, `ruler_title="Grand Prince"`,
`culture="Culture.sturgia"`, owner `Hero.lord_2_1`. This is the weakest geographic fit (Sturgia is
Calradia's "far north," not adjacent to our territory the way Empire-South/Aserai are) but it's
the closest available analogue for a non-Seljuk Caucasus-flavored power, and the user explicitly
asked for it — noted as a deliberate creative-license pick, not a verified border relationship.

Changes: `title` → "Kingdom of Georgia" (EN) / "Gürcistan Krallığı" (TR), `ruler_title`
"Grand Prince" → "King" / "Kral". New `text` describing the historical Seljuk–Georgian frontier
conflict.

### A.4 — Diplomatic relationships (bug found + new entries)

Our `kingdoms.xml` currently declares `kingdom_seljuks`'s relationships against
`Kingdom.empire`/`Kingdom.aserai`/`Kingdom.khuzait` — **`Kingdom.empire` is the wrong Empire
successor state**: we border and took territory from `empire_s`, not `empire`. Fix: change that
relationship entry's target to `Kingdom.empire_s`. Also add a `Kingdom.sturgia` entry (currently
missing entirely). Proposed values, reflecting the sources above: `empire_s` hostile (`-30`,
matching the Manzikert-era conflict — not `isAtWar` at campaign start, since that removes player
choice, but clearly antagonistic), `aserai` cautiously respectful (`20`, nominal-suzerain
dynamic), `khuzait` unchanged (`15`), `sturgia` neutral-cool (`0`, distant, no real history to
draw on).

### A.5 — Files touched, in order

1. `ModuleData/kingdoms.xml` — add the three `<Kingdom id="empire_s|aserai|sturgia">` override
   blocks; fix/add `kingdom_seljuks`'s `<relationships>`.
2. `ModuleData/Languages/strings.xml` + `ModuleData/Languages/TR/strings.xml` — new `{=key}` pairs
   for every new name/title/description text introduced above (this mod learned the hard way this
   session that every `{=key}` needs both files or it silently falls back to whichever language's
   raw XML text was typed in).
3. No C# changes — this work stream touches zero code, only two XML files (kingdoms.xml has no
   equivalent to the settlements.xml "C# is the real source of truth" convention; Kingdom
   ownership/relationships are read directly from XML by Native, confirmed by how
   `kingdom_seljuks`'s own relationships already work with no C# involvement).
4. No rebuild needed. Deploy is a straight `ModuleData` sync + redeploy, verify in-game (Kingdoms
   encyclopedia page for each of the three, diplomacy screen relationship values).

**Risk:** Low. This is attribute-only partial overrides of kingdoms that are already fully
populated by Native — none of the culture-scoped registries this session spent hours chasing are
touched, because `culture="Culture.empire/aserai/sturgia"` stays exactly as Native declared it.

---

## Work stream B — Proactive stability stress-test

Every crash fixed this session was found *reactively* (user hit it, we diagnosed from a
`dotnet-dump` capture). Now that `dotnet-dump` is set up and proven effective, do one deliberate
pass *before* the next feature lands, aimed specifically at late-game systems the New-Campaign
fix never exercised: hero marriage/succession into `clan_seljuk_royal`'s few members, a child
aging from the newly-added `child_template_seljuk_*`/`teenager_template_seljuk_*` templates all
the way to adulthood, a Seljuk lord dying and a clan needing to promote a new leader, a full siege
of one of our 8 settlements (attacker and defender), a prisoner exchange or ransom involving a
Seljuk hero, and an AI-initiated peace/war negotiation with the newly-renamed Byzantine/Abbasid/
Georgian kingdoms. Keep the crash-capture watcher pattern (background PowerShell loop copying
`C:\ProgramData\Mount and Blade II Bannerlord\crashes\<latest>` the moment `dump.dmp` exceeds
50MB) ready to fire during this pass.

---

## Work stream C — Depth for the five "thin" clans

`clan_mengucek`, `clan_saltuk`, `clan_caka`, `clan_karaman`, `clan_ahi_order` (all now tier 3
after the rebalance) currently exist only as a name + one lord + `initial_home_settlement` pointing
at Konya (they own no actual settlement — `home_settlement` and "owns a fief" are different things
in this mod's data). Options, in increasing effort: (1) leave as landless court clans — valid and
Native-normal, many small vassal clans never hold a fief; (2) give 1 village each pulled from
existing settlements' bound-village lists (no new territory, just village-level reassignment,
same low-risk pattern as this session's territory work); (3) short flavor quest/dialogue hooks via
`SeljukDialogueBehavior.cs`. Needs its own follow-up brainstorming pass — flagged here, not
speced, since it's a genuinely separate design decision (which clans get what, and how much).

---

## Work stream D — Troop-tree balance audit

Not yet done this session: a tier-by-tier comparison of `troops.xml`'s Seljuk tree against Native
Khuzait's equivalent tree (skills, equipment value, upgrade costs) the way the prior fix-pack spec
did for the economy/policy modules. Needed to confirm the tree is neither over- nor under-tuned
relative to Native factions now that clan tiers (which gate available troop tiers for AI armies)
have changed.

---

## Work stream E — Repository hygiene

Dev-session artifacts currently sitting in the repo root are not part of the mod and shouldn't
ship to Workshop reviewers or GitHub visitors browsing the source: `Inspect.cs`/`Inspect.exe`,
`InspectTemp.cs`/`InspectTemp.exe`, `inspect_types.py`, `scratch/`, plus the stray nested git
checkout at `OttomanJanissariesAndTurkicHeroes/` (a second, stale clone of this same repo sitting
inside itself — confirmed harmless but confusing) and `.agents/`/`skill-creator/`/`skills/`
(tooling scaffolding, not mod content). Low-risk cleanup pass: verify each is genuinely unused by
the mod/build (the `.csproj` and `SubModule.xml` don't reference any of them — already true, since
the mod has been building and running successfully all session without them), then remove and
commit.

---

## Work stream F — Steam Workshop presentation

Current Workshop page has one preview image. Add 3–4 in-game screenshots once Work stream A/B are
in and stable: the world map showing Seljuk territory bordering the newly-renamed Byzantine/
Abbasid neighbors, a Konya city screen, a Seljuk army in battle, and the character-creation
culture-select screen. Needs an actual play session to capture, not something to script.

---

## Sequencing

A (factions) and E (cleanup) are independent, low-risk, and ready to implement now. B (stress-test)
should happen before C/D so any newly-discovered crash gets fixed before more content is layered
on top of it. C and D each need their own short brainstorming pass before a plan is written. F
depends on A/B being done and stable enough to screenshot.

Recommended order: **A → E → B → (branch: C and D, either order) → F**.
