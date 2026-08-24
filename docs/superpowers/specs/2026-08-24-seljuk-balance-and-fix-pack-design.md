# Seljuk Empire mod — balance & fix pack design

Date: 2026-08-24

## Context

This spec covers six requested changes to the "Seljuk Empire: Sword of Islam" Bannerlord mod
(`OttomanJanissariesAndTurkicHeroes`). The request used generic Native Bannerlord filenames
(`spnpccharacters.xml`, `spcultures.xml`, `spclans.xml`, `character_creation_backstories.xml`).
This mod uses its own filenames for the same concepts:

| Requested (Native name) | Actual file in this mod |
|---|---|
| `spnpccharacters.xml` | `ModuleData/troops.xml` |
| `spcultures.xml` | does not exist yet — new file needed |
| `spclans.xml` | `ModuleData/factions.xml` |
| `character_creation_backstories.xml` | `ModuleData/character_creation.xml` (XML is currently **not loaded by the game** — see Module 3) |
| `settlements.xml` | `ModuleData/settlements.xml` (also currently **not loaded** — see Module 2) |
| `kingdoms.xml`, `lords.xml`, `strings.xml` | same names, exist as expected |

All ground truth below (Native settlement IDs/owners/coordinates, Native culture schema, Native
kingdom IDs) was verified against the actual installed game at
`C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\{SandBoxCore,SandBox,Native}`,
not guessed from memory.

---

## Module 1 — Troop & economy balance (`troops.xml`)

**Ottoman Scout Cavalry (`ottoman_scout`) recruit cost.** Bannerlord's troop XML has no
`recruit_cost` or `wage` attribute — both are engine-computed from a troop's `level` and whether
it's tavern-hireable (`occupation="Mercenary"` `is_mercenary="true"`). `ottoman_scout` currently
carries both flags, which is what produces the ~540g tavern hire price, and it's inconsistent with
the rest of the tree: every other Seljuk troop is recruited for free through notables via
`SeljukRecruitmentBehavior.cs`, which already inserts `ottoman_scout` into town/castle volunteer
slot 3.

Plan: remove `occupation="Mercenary"` / `is_mercenary="true"` from `ottoman_scout`, set
`culture="Culture.seljuk"` (see Module 3), and re-tune its `level` and skills to sit as a T2/T3
cavalry unit consistent with `seljuk_horseman`/`seljuk_clan_scout` in the existing tree, so it
behaves as a normal free-recruit branch rather than a mispriced mercenary.

**Horse requirement on İkta Gönüllüsü → Türkmen Atlısı.** Checked: `seljuk_horseman` (Türkmen
Atlısı) already has `Item.khuzait_horse` equipped, and `seljuk_peasant` (İkta Gönüllüsü) has no
horse. Horse consumption on upgrade to a mounted troop is automatic Bannerlord engine behavior
tied to comparing the two troops' equipped `Horse` slot — there is no XML attribute for it. This
item is likely already working; I'll verify in-game rather than add a non-existent tag, and only
change something if testing shows otherwise.

**Full tree cost/upgrade-item audit.** I'll walk every `upgrade_targets` chain in `troops.xml` and
compare skill/equipment jumps tier-by-tier against the equivalent Khuzait Native tree
(`spnpccharacters.xml`), correcting any tier where equipment value or skill totals are out of line
with Native conventions.

**Troop names.** Pass over `troops.xml` names for historical/military-terminology accuracy (e.g.
confirm "İkta Gönüllüsü", "Çeri Yayak", "Boy Akıncısı" etc. read as authentic period terms rather
than generic labels); adjust where a clearer historical term exists, keeping existing `{=ID}` keys
stable and updating both TR and EN strings.

---

## Module 2 — Territory (`settlements.xml` + C# behaviors)

**settlements.xml is currently dead data.** `SubModule.xml` never registers it
(`<XmlName id="Settlements" path="settlements"/>` is missing), so none of its `name=`/`owner=`
overrides are applied by the game. All actual ownership/renaming happens at runtime in
`Source/SeljukEmpire/Settlements/SeljukSettlementBehavior.cs`. Two more files hardcode the same
settlement-ID list to decide "is this Seljuk territory": `SeljukVolunteerModel.cs` and
`SeljukRecruitmentBehavior.cs` (`IsSeljukSettlement`). Changing Seljuk territory means editing all
three C# files and rebuilding `SeljukTactics.dll` (I have `dotnet` SDK 10.0.400 available locally,
and the `.csproj` already references the installed game's assemblies, so this is buildable in this
environment) — not just an XML edit. I will also register `Settlements` in `SubModule.xml` so the
file stops being dead weight and mod-manager/other tooling that reads it sees accurate data.

**Decision (confirmed with user): full revert.** All currently Seljuk-held towns and castles
revert to their Native owner/culture/name, including the four that weren't in the original
explicit list (Alâiye, Divriği, Lârende, Erzurum Hisarı) — the user chose the "revert all four"
option over keeping any of them.

Settlements reverting to Native (owner, culture, Native name — from `SandBox/ModuleData/settlements.xml`):

| ID | Current Seljuk name | Reverts to | Native owner clan | Native culture |
|---|---|---|---|---|
| `town_K1` | Rey-i Saltanat | Baltakhand | `clan_khuzait_2` | khuzait |
| `town_K4` | Kayseriyye | Ortongard | `clan_khuzait_3` | khuzait |
| `town_ES4` | Diyarbekir | Lycaron | `clan_empire_south_1` | empire |
| `town_ES2` | Divriği | Vostrum | `clan_empire_south_4` | empire |
| `town_K6` | Alâiye | Odokh | `clan_khuzait_5` | khuzait |
| `town_A4` | Lârende | Razih | `clan_aserai_2` | aserai |
| `castle_K2` | Deylem Hisarı | Akiser Castle | `clan_khuzait_4` | khuzait |
| `castle_K5` | Niksar Kalesi | Khimli Castle | `clan_khuzait_7` | khuzait |
| `castle_K1` | Hasankeyf Kalesi | Usek Castle | `clan_khuzait_3` | khuzait |
| `castle_A8` | Erzurum Hisarı | Tamnuh Castle | `clan_aserai_8` | aserai |
| `castle_ES3` | Söğüt Uç Kalesi | Melion Castle | `clan_empire_south_7` | empire |

All villages currently bound to these (the `village_K1_*`, `village_K4_*`, `village_ES4_*`,
`village_ES2_*`, `village_K6_*`, `village_A4_*`, `castle_village_{K2,K5,K1,A8,ES3}_*` entries)
revert with their parent settlement, reverting to their Native names as well.

Settlements transferring to Seljuk (new, verified via game files, all in one contiguous
Southern-Empire/Aserai border cluster — Danustica 661,276 / Lavenia 621,255 / Morenia 557,279 /
Shibal Zumr 667,245 / Husn Fulq 686,234, map units):

| ID | Native name | Native owner | Native culture |
|---|---|---|---|
| `town_ES1` | Danustica | `clan_empire_south_3` | empire |
| `town_A2` | Husn Fulq | `clan_aserai_2` | aserai |
| `castle_ES4` | Lavenia Castle | `clan_empire_south_3` | empire |
| `castle_ES5` | Morenia Castle | `clan_empire_south_8` | empire |
| `castle_A6` | Shibal Zumr Castle | `clan_aserai_3` | aserai |

I'll pull each one's bound villages from Native `settlements.xml` (already confirmed
`castle_village_ES4_1` Lavenia, `castle_village_ES5_1` Morenia, `castle_village_A6_1` Shibal Zumr
exist; town_ES1/town_A2 villages need the same lookup) and rename/reassign them the same way the
current mod does for its existing villages. Their `culture=` tag is set to `Culture.seljuk` (Module
3), not left as Native empire/aserai culture — see Implementation order below for why Module 3 has
to land first.

**Net effect:** Seljuk territory shrinks from 6 towns + 5 castles to 2 towns + 3 castles, but
becomes genuinely contiguous instead of scattered across Khuzait/Aserai/Empire territory. This
change cascades into Module 5 (clan home settlements) — with only 5 settlements for 11 clans, most
clans will start landless (normal in Native; many minor clans start with zero fiefs and gain them
through conquest), with the 5 settlements distributed to the most senior clans by historical
weight.

---

## Module 3 — Culture & character creation (`character_creation.xml`, new culture file)

**Backstory text is already `{=ID}`-based, but the XML file doesn't run it.**
`character_creation.xml`'s `<CharacterCreationCategory>`/`<Option>` schema isn't a real,
game-loaded Bannerlord format — it's not registered in `SubModule.xml`, and Bannerlord's character
creation menu isn't XML-driven. The actual backstory options are added in C#, in
`Source/SeljukEmpire/CharacterCreation/SeljukCharacterCreationContentHandler.cs`, which already
uses the same `{=ID}Turkish fallback text` `TextObject` pattern (good — that's the correct
localization pattern), duplicating the same options as the XML. Plan: keep the XML as
documentation/reference (or delete it to avoid drift — leaning toward deleting since it's dead and
a second source of truth invites drift; will confirm in the plan step), and treat
`SeljukCharacterCreationContentHandler.cs` as the real implementation to audit for EN string
coverage (Module 6).

**New playable Culture.seljuk.** Currently the whole mod (kingdom, 11 clans, all troops, all
heroes) piggybacks on `Culture.khuzait` — there's no distinct Seljuk culture at all, so it can't
appear as a character-creation heritage option. Building a fully independent culture from scratch
would require ~60 new generic NPCCharacters (villager, tavernkeeper, blacksmith, caravan guards,
notaries, etc. — confirmed by inspecting Native's `empire` culture definition) and ~10 new party
templates, which is far more than this request calls for. Plan (YAGNI-respecting, standard
practice for this kind of "sub-culture" addition):

- New file `ModuleData/seljuk_culture.xml`, registered via
  `<XmlName id="SPCultures" path="seljuk_culture"/>` in `SubModule.xml`.
- `<Culture id="seljuk" ... can_be_selected_in_character_creation="true">` with its own name,
  banner, colors, and flavor text.
- `basic_troop`/`elite_basic_troop` and the culture's tier-1 recruit references point at this
  mod's own `seljuk_peasant` tree instead of Khuzait's, so playing as this culture and owning
  settlements recruits authentic Seljuk troops.
- All other supporting roles (`villager`, `tavernkeeper`, `blacksmith`, `caravan_guard`,
  `guard`, notaries, party templates, etc.) point at Khuzait's existing Native NPCCharacters/party
  templates — reusing proven assets instead of duplicating dozens of new characters.
- Migrate `kingdom_seljuks`, all 11 clans in `factions.xml`, all `seljuk_*`/`ottoman_*`/`azap_*`/
  `acemi_*` troops in `troops.xml`, all Seljuk heroes in `lords.xml`/`heroes.xml`, and the final
  settlement list from Module 2 to `culture="Culture.seljuk"` instead of `Culture.khuzait`, so the
  in-game faction is actually built from this new culture rather than just offering it as a cosmetic
  character-creation option disconnected from the kingdom.

**Culture bonuses/malus (proposed, tunable):**
1. **Buff — İkta Süvari Ekonomisi:** Seljuk-culture parties pay 10% less wage for mounted troops.
2. **Buff — İpek Yolu Ticareti:** +15% profit on caravan trade for Seljuk-culture caravans (pairs
   with the mod's existing `SeljukCaravanInsuranceBehavior.cs` economy system).
3. **Buff — Nizamiye İmar Teşkilatı:** +10% construction speed for settlement projects in
   Seljuk-owned settlements.
4. **Debuff — Zayıf Kuşatma Mühendisliği:** -15% siege engine construction speed (nomadic-cavalry
   heritage traded off against weaker siege-engineering tradition — a real capability trade-off,
   not just a flat stat penalty).

These require a small new C# model override (following the same pattern as
`SeljukVolunteerModel.cs`) rather than XML flags, since Native culture bonuses like this aren't
XML-configurable in Bannerlord — they're implemented via `GameModel` overrides.

---

## Module 4 — Diplomacy (`kingdoms.xml`)

Verified against Native `spkingdoms.xml`: this mod's `<relationship kingdom="Kingdom.empire"
value="-20" />` **is** the Northern Empire — Native's kingdom id `"empire"` displays as
`{=NF627oiX}Northern Empire`. So the "auto-war with Northern Empire" complaint maps directly to
this existing `-20` entry; I'll change it to `0` (neutral) or a small positive value.

**"Karakhergit" doesn't exist anywhere** — not in this mod's files, not in the base game's Native/
SandBox/SandBoxCore modules (confirmed via a full search of both trees). There's no kingdom,
faction, or relationship by that name to remove. I'll leave this out of the implementation and
flag it back to you rather than invent a fictional kingdom relationship — if you meant a specific
in-game entity, let me know and I'll target it directly.

Current `kingdoms.xml` also lacks any of the Bannerlord-required late-game rebalancing (aserai +10,
khuzait +15 stay as-is, no changes requested there).

---

## Module 5 — Lords, clans, dynasties (`factions.xml`, `lords.xml`)

Depends on Module 2's final territory. With only 5 settlements (2 towns, 3 castles) for 11 clans,
I'll assign home settlements to the historically/narratively most senior clans first
(`clan_seljuk_royal` gets a capital town) and leave lower-tier clans landless at game start (this
mirrors how Native itself starts many minor clans with zero fiefs) rather than inventing
settlements that don't exist. I'll do a pass on `lords.xml` for age/title/family-relation
consistency (e.g. Ertuğrul Gazi's generation relative to Alp Arslan/Melikşah's actual historical
era — Great Seljuk sultans vs. the Kayı boy leader are roughly a century apart historically, so
I'll flag any anachronisms I find rather than silently "fixing" the mod's existing creative
timeline compression, since that's a judgment call about the mod's premise, not a bug).

---

## Module 6 — Localization (`Languages/`)

I'll write a small audit script (compare every `{=ID}` referenced across `troops.xml`, `lords.xml`,
`heroes.xml`, `factions.xml`, `kingdoms.xml`, `character_creation.xml`, and the C# source, against
the `<string id="ID">` entries defined in `Languages/strings.xml` (EN) and `Languages/TR/strings.xml`)
and fill every gap in both directions, so switching language in-game never falls back to the
hardcoded fallback text embedded in the `{=ID}Fallback text` calls (which is Turkish in several
C# call sites, e.g. `SeljukCharacterCreationContentHandler.cs`). This closes the "stuck in one
language" bug at the root instead of patching individual strings.

---

## Implementation order

Modules have real dependencies, not just topical grouping:

1. **Module 3 (culture) first** — `Culture.seljuk` has to exist before anything else can be tagged
   with it.
2. **Module 2 (territory)** — reassigns settlements/villages using `Culture.seljuk`, updates the
   3 C# files, rebuilds the DLL.
3. **Module 5 (clans/lords)** — clan home settlements depend on Module 2's final territory list.
4. **Module 1 (troops), Module 4 (diplomacy)** — independent of the above, can happen in any order.
5. **Module 6 (localization audit)** — last, since it needs every `{=ID}` introduced by the other
   five modules to exist before it can verify coverage.

## Assumptions carried into implementation (flag here if wrong)

- "Rey, Sultanat" in the city revert list = the single settlement `town_K1` ("Rey-i Saltanat");
  there's no Native or mod settlement literally named "Sultanat" — it's the Turkish word for
  "Sultanate" used elsewhere as the faction title string, not a place.
- `graphify-out/graph.md` (an older architecture doc in the repo) names different settlements for
  the same IDs (e.g. `town_K1` = "Konya" there vs. "Rey-i Saltanat" in the live files) — treating it
  as stale background, not a target, since your instructions used the live files' names.
- Deleting the dead `character_creation.xml` in favor of the C# handler being the single source of
  truth (open for reconsideration in the plan step if you'd rather keep it as documentation).
