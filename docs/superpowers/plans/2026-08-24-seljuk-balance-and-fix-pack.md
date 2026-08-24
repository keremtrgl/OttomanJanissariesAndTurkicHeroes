# Seljuk Empire Balance & Fix Pack Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the six-module balance/fix pack from
`docs/superpowers/specs/2026-08-24-seljuk-balance-and-fix-pack-design.md` for the "Seljuk Empire:
Sword of Islam" Bannerlord mod: a real playable Culture.seljuk, a contiguous Seljuk territory,
correct troop economics, fixed diplomacy, historically-consistent lords, and full TR/EN
localization coverage.

**Architecture:** This is a Bannerlord total-conversion-lite mod: declarative XML content
(`ModuleData/*.xml`) plus a handful of C# `CampaignBehaviorBase`/model-override classes
(`Source/SeljukEmpire/**/*.cs`) compiled into `SeljukTactics.dll`. Tasks that only touch XML take
effect on next game launch. Tasks that touch C# require `dotnet build` and copying the resulting
DLL into `bin/Win64_Shipping_Client/` and `bin/Win64_Shipping_wEditor/` (both already contain a
build of the DLL, committed to the repo, and must be replaced together).

**Tech Stack:** Bannerlord ModuleData XML schema (Native `SandBox`/`SandBoxCore` conventions), C#
/ .NET Standard 2.0 (`Source/SeljukEmpire/SeljukTactics.csproj`), `dotnet` CLI for builds, Python
3.13 for one-off audit/validation scripts (no XML unit-test framework exists for this project, so
"tests" are: well-formedness checks, `dotnet build` success, and grep-based invariant scripts).

## Global Constraints

- Repo root for all paths below: `OttomanJanissariesAndTurkicHeroes/` (the git repo already pushed
  to `https://github.com/keremtrgl/OttomanJanissariesAndTurkicHeroes`).
- Every new/changed display string uses a `{=some_id}Fallback text` `TextObject` pattern and gets
  entries in **both** `ModuleData/Languages/strings.xml` (English) and
  `ModuleData/Languages/TR/strings.xml` (Turkish) — never hardcode literal display text with no
  `{=id}`.
- Preserve existing `{=id}` string IDs wherever the underlying concept doesn't change (renames of
  *text* are fine; renames of *ids* break existing save-game compatibility for no benefit — don't).
- Reference game install for ground truth (already used throughout research):
  `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\{SandBoxCore,SandBox,Native}`.
- Commit after every task (small, working-state commits), per repo convention already established
  (`first commit`, `Add installation instructions...`, `Add design spec...`).
- Windows/PowerShell environment; `dotnet` SDK 10.0.400 is installed; no `msbuild`/`zip` CLI —
  use `dotnet build` and PowerShell `Compress-Archive` if packaging is ever needed again.

---

### Task 1: New playable Culture.seljuk

**Files:**
- Create: `ModuleData/seljuk_culture.xml`
- Modify: `SubModule.xml` (add `<XmlNode>` entry)
- Test: none (XML content task) — verification is well-formedness + attribute presence checks below

**Interfaces:**
- Produces: `Culture.seljuk` — consumed by Task 2 (culture migration), Task 6 (new settlements),
  Task 4 (bonus behavior reads `Hero.Culture`/`Settlement.Culture` against this id).

- [ ] **Step 1: Write `ModuleData/seljuk_culture.xml`**

Base every non-military attribute on Native's `khuzait` culture block (confirmed at
`SandBoxCore/ModuleData/spcultures.xml` line 3054) so all supporting civilian NPCs, party
templates, and equipment rosters resolve to assets that already exist in the base game — only the
identity, flavor, and the four `*_troop` attributes are Seljuk-specific per the design spec's YAGNI
scope decision:

```xml
<?xml version="1.0" encoding="utf-8"?>
<SPCultures>
	<Culture
		id="seljuk"
		name="{=seljuk_culture_name}Selçuklu"
		is_main_culture="true"
		color="0xFF168B8D"
		color2="0xFFD4AF37"
		elite_basic_troop="NPCCharacter.seljuk_ghulam_recruit"
		basic_troop="NPCCharacter.seljuk_peasant"
		melee_militia_troop="NPCCharacter.khuzait_militia_spearman"
		ranged_militia_troop="NPCCharacter.khuzait_militia_archer"
		melee_elite_militia_troop="NPCCharacter.khuzait_militia_veteran_spearman"
		ranged_elite_militia_troop="NPCCharacter.khuzait_militia_veteran_archer"
		can_have_settlement="true"
		villager_party_template="PartyTemplate.villager_khuzait_template"
		default_party_template="PartyTemplate.kingdom_hero_party_khuzait_template"
		settlement_patrol_template_level_1="PartyTemplate.patrol_party_khuzait_template_level_1"
		settlement_patrol_template_level_2="PartyTemplate.patrol_party_khuzait_template_level_2"
		settlement_patrol_template_level_3="PartyTemplate.patrol_party_khuzait_template_level_3"
		militia_party_template="PartyTemplate.militia_khuzait_template"
		rebels_party_template="PartyTemplate.rebels_khuzait_template"
		vassal_reward_party_template="PartyTemplate.vassal_reward_troops_khuzait"
		encounter_background_mesh="encounter_khuzait"
		faction_banner_key="11.144.144.1536.1536.768.768.1.0.0.103.143.143.512.512.768.768.1.0.0"
		text="{=seljuk_culture_desc}Büyük Selçuklu Devleti'nin ve Anadolu uç beyliklerinin kurduğu Türkmen konfederasyonu. İkta sistemiyle beslenen atlı ordular, Nizamiye medreseleri ve İpek Yolu kervanlarıyla zenginleşen şehirler bu kültürün temelini oluşturur."
		tournament_master="NPCCharacter.tournament_master_khuzait"
		villager="NPCCharacter.villager_khuzait"
		caravan_master="NPCCharacter.caravan_master_khuzait"
		caravan_guard="NPCCharacter.caravan_guard_khuzait"
		veteran_caravan_guard="NPCCharacter.veteran_caravan_guard_khuzait"
		prison_guard="NPCCharacter.prison_guard_khuzait"
		guard="NPCCharacter.guard_khuzait"
		blacksmith="NPCCharacter.blacksmith_khuzait"
		weaponsmith="NPCCharacter.weaponsmith_khuzait"
		townswoman="NPCCharacter.townswoman_khuzait"
		townswoman_infant="NPCCharacter.townswoman_infant_khuzait"
		townswoman_child="NPCCharacter.townswoman_child_khuzait"
		townswoman_teenager="NPCCharacter.townswoman_teenager_khuzait"
		townsman="NPCCharacter.townsman_khuzait"
		townsman_infant="NPCCharacter.townsman_infant_khuzait"
		townsman_child="NPCCharacter.townsman_child_khuzait"
		townsman_teenager="NPCCharacter.townsman_teenager_khuzait"
		village_woman="NPCCharacter.village_woman_khuzait"
		villager_male_child="NPCCharacter.villager_child_khuzait"
		villager_male_teenager="NPCCharacter.villager_teenager_khuzait"
		villager_female_child="NPCCharacter.village_woman_child_khuzait"
		villager_female_teenager="NPCCharacter.village_woman_teenager_khuzait"
		ransom_broker="NPCCharacter.ransom_broker_khuzait"
		gangleader_bodyguard="NPCCharacter.gangleader_bodyguard_khuzait"
		merchant_notary="NPCCharacter.merchant_notary_khuzait"
		artisan_notary="NPCCharacter.artisan_notary_khuzait"
		preacher_notary="NPCCharacter.preacher_notary_khuzait"
		rural_notable_notary="NPCCharacter.rural_notable_notary_khuzait"
		shop_worker="NPCCharacter.shop_worker_khuzait"
		tavernkeeper="NPCCharacter.tavernkeeper_khuzait"
		taverngamehost="NPCCharacter.taverngamehost_khuzait"
		musician="NPCCharacter.musician_khuzait"
		tavern_wench="NPCCharacter.tavern_wench_khuzait"
		armorer="NPCCharacter.armorer_khuzait"
		horseMerchant="NPCCharacter.horseMerchant_khuzait"
		barber="NPCCharacter.barber_khuzait"
		merchant="NPCCharacter.merchant_khuzait"
		beggar="NPCCharacter.beggar_khuzait"
		female_beggar="NPCCharacter.female_beggar_khuzait"
		female_dancer="NPCCharacter.female_dancer_khuzait"
		default_battle_equipment_roster="EquipmentRoster.khu_civ_template_default"
		default_civilian_equipment_roster="EquipmentRoster.khu_civ_template_default"
		default_stealth_equipment_roster="EquipmentRoster.default_stealth_equipment_roster"
		duel_preset_equipment_roster="EquipmentRoster.khu_duel_preset_template"
		marriage_bride_equipment_roster="EquipmentRoster.marriage_female_khu_cutscene_template"
		board_game_type="Puluc"
		default_character_creation_body_property="BodyProperty.default_character_creation_body_property_khuzait">
		<caravan_party_templates>
			<caravan_party_template id="PartyTemplate.caravan_template_khuzait" />
		</caravan_party_templates>
		<elite_caravan_party_templates>
			<caravan_party_template id="PartyTemplate.elite_caravan_template_khuzait" />
		</elite_caravan_party_templates>
		<available_ship_hulls></available_ship_hulls>
		<vassal_reward_items>
			<item id="Item.steppe_war_bow" />
		</vassal_reward_items>
		<banner_bearer_replacement_weapons>
			<item id="Item.khuzait_sword_1_t2" />
			<item id="Item.khuzait_sword_2_t3" />
		</banner_bearer_replacement_weapons>
	</Culture>
</SPCultures>
```

Note: `elite_basic_troop="NPCCharacter.seljuk_ghulam_recruit"` and
`basic_troop="NPCCharacter.seljuk_peasant"` both already exist in `troops.xml` — confirmed present
at the time this plan was written (`seljuk_peasant` id, `seljuk_ghulam_recruit` id). Task 2
re-tags their `culture=` attribute; this task only needs them to exist by id, which they already
do.

- [ ] **Step 2: Register the file in `SubModule.xml`**

Add this `<XmlNode>` inside the existing `<Xmls>` block (after the `Items` node, before
`NPCCharacters`/troops, so cultures load before anything that references them):

```xml
    <XmlNode>
      <XmlName id="SPCultures" path="seljuk_culture"/>
      <IncludedGameTypes>
        <GameType value="Campaign"/>
        <GameType value="CampaignStoryMode"/>
        <GameType value="CustomGame"/>
      </IncludedGameTypes>
    </XmlNode>
```

- [ ] **Step 3: Validate well-formedness and required references**

Run:
```bash
python -c "import xml.etree.ElementTree as ET; ET.parse('ModuleData/seljuk_culture.xml'); ET.parse('SubModule.xml'); print('OK')"
```
Expected: `OK` (no parse errors).

Then confirm the two troop ids this file depends on are still present:
```bash
grep -c 'id="seljuk_peasant"' ModuleData/troops.xml
grep -c 'id="seljuk_ghulam_recruit"' ModuleData/troops.xml
```
Expected: both print `1`.

- [ ] **Step 4: Commit**

```bash
git add ModuleData/seljuk_culture.xml SubModule.xml
git commit -m "Add playable Culture.seljuk, reusing Khuzait support NPCs/templates"
```

---

### Task 2: Migrate the Seljuk faction from Culture.khuzait to Culture.seljuk

**Files:**
- Modify: `ModuleData/kingdoms.xml`
- Modify: `ModuleData/factions.xml`
- Modify: `ModuleData/troops.xml`
- Modify: `ModuleData/lords.xml`

**Interfaces:**
- Consumes: `Culture.seljuk` (Task 1).
- Produces: every Seljuk kingdom/clan/troop/lord tagged `Culture.seljuk` — Task 6 relies on this
  same id when tagging the newly-transferred settlements.

- [ ] **Step 1: Replace `culture="Culture.khuzait"` with `culture="Culture.seljuk"`**

In each of the four files, every occurrence of `culture="Culture.khuzait"` is a Seljuk entity (this
mod dedicates these four files entirely to the Seljuk kingdom — confirmed by inspection, there are
no non-Seljuk entries mixed in), so a blanket replace is correct and safe:

```bash
sed -i 's/culture="Culture.khuzait"/culture="Culture.seljuk"/g' \
  ModuleData/kingdoms.xml ModuleData/factions.xml ModuleData/troops.xml ModuleData/lords.xml
```

- [ ] **Step 2: Verify no stray khuzait references remain in these four files, and count matches expectations**

```bash
grep -c 'Culture.khuzait' ModuleData/kingdoms.xml ModuleData/factions.xml ModuleData/troops.xml ModuleData/lords.xml
```
Expected: `0` for all four (grep with `-c` on no match prints `0` per matched file, or "No matches
found" per-file — either way confirm none remain).

```bash
grep -c 'Culture.seljuk' ModuleData/kingdoms.xml ModuleData/factions.xml
```
Expected: `kingdoms.xml` → `1` (the single `<Kingdom>` element), `factions.xml` → `11` (one per
clan).

- [ ] **Step 3: Validate XML well-formedness**

```bash
python -c "
import xml.etree.ElementTree as ET
for f in ['ModuleData/kingdoms.xml','ModuleData/factions.xml','ModuleData/troops.xml','ModuleData/lords.xml']:
    ET.parse(f)
print('OK')
"
```
Expected: `OK`.

- [ ] **Step 4: Commit**

```bash
git add ModuleData/kingdoms.xml ModuleData/factions.xml ModuleData/troops.xml ModuleData/lords.xml
git commit -m "Migrate Seljuk kingdom/clans/troops/lords from Culture.khuzait to Culture.seljuk"
```

---

### Task 3: Fix mercenary-flag recruit cost bug (Ottoman Scout Cavalry, Acemi Janissary, Azap Recruit)

**Files:**
- Modify: `ModuleData/troops.xml`

**Interfaces:**
- No new ids produced or consumed; this only changes attributes on existing `NPCCharacter`
  elements `ottoman_scout`, `acemi_janissary`, `azap_recruit`.

**Context:** All three troops are currently `occupation="Mercenary" is_mercenary="true"`, which
makes the engine price them as tavern-hire mercenaries (this is where the reported ~540g "recruit
cost" for Ottoman Scout Cavalry comes from — Bannerlord troops have no `recruit_cost`/`wage`
attribute; both are engine-computed from `level` + mercenary status). All three are already
inserted into normal town/castle volunteer slots for free by `SeljukRecruitmentBehavior.cs`
(`RefreshSettlementNotables`, slots for `azapRecruit`, `acemiJanissary`, `ottomanScout`) — so the
mercenary flag is pure inconsistency, not an intentional pricing lever.

- [ ] **Step 1: Remove the mercenary flags from all three troops**

For each of `ottoman_scout` (currently `level="11"`), `acemi_janissary` (currently `level="11"`),
`azap_recruit` (currently `level="6"`), change:
```
occupation="Mercenary" is_mercenary="true"
```
to:
```
occupation="Soldier"
```
(i.e. delete `is_mercenary="true"` entirely and change the occupation value — matches every other
`seljuk_*` troop in the file, which uses `occupation="Soldier"` with no `is_mercenary` attribute at
all).

- [ ] **Step 2: Verify the fix**

```bash
grep -n 'id="ottoman_scout"\|id="acemi_janissary"\|id="azap_recruit"' ModuleData/troops.xml
```
Expected: none of the three matched lines contain `is_mercenary` or `occupation="Mercenary"`.

- [ ] **Step 3: Validate XML well-formedness**

```bash
python -c "import xml.etree.ElementTree as ET; ET.parse('ModuleData/troops.xml'); print('OK')"
```
Expected: `OK`.

- [ ] **Step 4: Commit**

```bash
git add ModuleData/troops.xml
git commit -m "Fix Ottoman Scout/Acemi Janissary/Azap Recruit mercenary-flag pricing bug"
```

---

### Task 4: Seljuk culture bonus/debuff behavior

**Files:**
- Create: `Source/SeljukEmpire/Culture/SeljukCultureBonusBehavior.cs`
- Modify: `Source/SeljukEmpire/SeljukSubModule.cs` (register the new behavior)

**Interfaces:**
- Consumes: `Culture.seljuk` (Task 1), `Campaign.Current.Models.WageModel`,
  `Campaign.Current.Models.SettlementBuildingModel` — these are the two Bannerlord `GameModel`
  slots this behavior overrides, matching the existing override pattern already used by
  `SeljukVolunteerModel.cs` (`Campaign.Current.Models.VolunteerModel`).
- Produces: no new public API — pure behavior registration.

**Context:** Native Bannerlord culture bonuses (e.g. Vlandia's cheaper crossbows) are not
XML-configurable; they're implemented as C# `GameModel` overrides that branch on
`Hero.Culture`/`Settlement.Culture`/`Clan.Culture`. This task implements the three buffs + one
debuff from the design spec via the same override mechanism `SeljukVolunteerModel.cs` already
uses (wrap-the-base-model pattern), registered in `SeljukSubModule.cs` next to the existing
`AddModel(new SeljukVolunteerModel(...))` call.

- [ ] **Step 1: Read the existing registration pattern**

```bash
grep -n "AddModel\|AddBehavior" Source/SeljukEmpire/SeljukSubModule.cs
```
Use this to find exactly where `SeljukVolunteerModel` is registered — the new model overrides go
in the same method, same style.

- [ ] **Step 2: Write `Source/SeljukEmpire/Culture/SeljukCultureBonusBehavior.cs`**

```csharp
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace SeljukEmpire.Culture
{
    /// <summary>
    /// Seljuk culture passive bonuses: -10% mounted-troop wage, +10% construction speed in
    /// Seljuk settlements. Paired debuff (-15% siege engine build speed) lives in the same
    /// construction-speed override for a single source of truth.
    /// </summary>
    public class SeljukWageModel : DefaultPartyWageModel
    {
        public override ExplainedNumber GetCharacterWage(CharacterObject character, bool includeDescriptions = false)
        {
            ExplainedNumber result = base.GetCharacterWage(character, includeDescriptions);
            if (character != null && character.Culture != null && character.Culture.StringId == "seljuk" && character.IsMounted)
            {
                result.AddFactor(-0.10f, includeDescriptions ? new TaleWorlds.Localization.TextObject("{=seljuk_bonus_cavalry_wage}Seljuk İkta Süvari Ekonomisi") : TaleWorlds.Localization.TextObject.Empty);
            }
            return result;
        }
    }

    public class SeljukConstructionSpeedModel : DefaultSettlementBuildingModel
    {
        public override ExplainedNumber CalculateBuildingProgressChange(Settlement settlement, Building building, ExplainedNumber baseResult = default)
        {
            ExplainedNumber result = base.CalculateBuildingProgressChange(settlement, building, baseResult);
            if (settlement?.Culture != null && settlement.Culture.StringId == "seljuk")
            {
                bool isSiegeEngine = building?.BuildingType?.BuildingLocation == BuildingLocation.Daily && building.BuildingType.IsDefaultProject;
                float factor = isSiegeEngine ? -0.15f : 0.10f;
                result.AddFactor(factor, isSiegeEngine
                    ? new TaleWorlds.Localization.TextObject("{=seljuk_debuff_siege_engineering}Zayıf Kuşatma Mühendisliği")
                    : new TaleWorlds.Localization.TextObject("{=seljuk_bonus_construction}Nizamiye İmar Teşkilatı"));
            }
            return result;
        }
    }
}
```

Note: `DefaultSettlementBuildingModel.CalculateBuildingProgressChange`'s exact signature and the
`BuildingType` API for distinguishing siege engines from ordinary construction projects vary
slightly between Bannerlord versions — **before wiring this in**, run:
```bash
grep -rn "class DefaultSettlementBuildingModel" "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client" 2>/dev/null
```
If that turns up nothing useful (it's a compiled DLL, not source), instead open the project in an
IDE with the game's `TaleWorlds.CampaignSystem.dll` referenced (already referenced by
`SeljukTactics.csproj`) and use Go-to-Definition / IntelliSense on `DefaultSettlementBuildingModel`
to confirm the exact virtual method name and signature before finalizing this file — do not guess
if `dotnet build` (Step 4) reports a signature mismatch; fix the signature to match what
IntelliSense/the compiler reports instead of forcing this exact snippet.

- [ ] **Step 3: Register both models in `SeljukSubModule.cs`**

Find the method containing the existing `AddModel(new SeljukVolunteerModel(...))` call (from
Step 1's grep) and add two lines immediately after it, following the exact same
`campaignGameStarter.AddModel(new X(campaignGameStarter.Models.OfType<Y>().First()))`-or-equivalent
pattern already used there for `SeljukVolunteerModel`:

```csharp
starter.AddModel(new SeljukEmpire.Culture.SeljukWageModel());
starter.AddModel(new SeljukEmpire.Culture.SeljukConstructionSpeedModel());
```

(Adjust the receiver variable name — `starter` above — to match whatever the surrounding method
actually calls its `CampaignGameStarter` parameter; confirmed by the Step 1 grep output.)

Add the caravan trade-profit buff (+15% for Seljuk-culture caravans) to
`Source/SeljukEmpire/Economy/SeljukCaravanInsuranceBehavior.cs` instead of a new file, since that
file already owns Seljuk economic behavior — find its caravan profit calculation (grep
`grep -n "profit\|Profit" Source/SeljukEmpire/Economy/SeljukCaravanInsuranceBehavior.cs`) and
multiply by `1.15f` when the caravan-owning hero's `Clan.Culture.StringId == "seljuk"`, following
whatever guard-clause style the surrounding method already uses.

- [ ] **Step 4: Build and verify**

```bash
dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release
```
Expected: `Build succeeded.` with 0 errors. Fix any compile errors by checking the actual
`TaleWorlds.CampaignSystem` API via IntelliSense/decompilation as noted in Step 2 — do not leave
this task with a failing build.

- [ ] **Step 5: Add TR/EN strings for the three new `{=...}` ids**

Add to `ModuleData/Languages/strings.xml`:
```xml
    <string id="seljuk_culture_name" text="Seljuk" />
    <string id="seljuk_culture_desc" text="A Turkmen confederation forged by the Great Seljuk state and the Anatolian frontier beyliks. Cities grow rich on iqta-fed cavalry armies, Nizamiye madrasas, and Silk Road caravans." />
    <string id="seljuk_bonus_cavalry_wage" text="Seljuk Iqta Cavalry Economy" />
    <string id="seljuk_bonus_construction" text="Nizamiye Public Works" />
    <string id="seljuk_debuff_siege_engineering" text="Weak Siege Engineering Tradition" />
```
Add the Turkish equivalents to `ModuleData/Languages/TR/strings.xml`:
```xml
    <string id="seljuk_culture_name" text="Selçuklu" />
    <string id="seljuk_culture_desc" text="Büyük Selçuklu Devleti'nin ve Anadolu uç beyliklerinin kurduğu Türkmen konfederasyonu. İkta sistemiyle beslenen atlı ordular, Nizamiye medreseleri ve İpek Yolu kervanlarıyla zenginleşen şehirler bu kültürün temelini oluşturur." />
    <string id="seljuk_bonus_cavalry_wage" text="Selçuklu İkta Süvari Ekonomisi" />
    <string id="seljuk_bonus_construction" text="Nizamiye İmar Teşkilatı" />
    <string id="seljuk_debuff_siege_engineering" text="Zayıf Kuşatma Mühendisliği" />
```

- [ ] **Step 6: Commit**

```bash
git add Source/SeljukEmpire/Culture/SeljukCultureBonusBehavior.cs Source/SeljukEmpire/SeljukSubModule.cs Source/SeljukEmpire/Economy/SeljukCaravanInsuranceBehavior.cs ModuleData/Languages/strings.xml ModuleData/Languages/TR/strings.xml
git commit -m "Add Seljuk culture bonuses: cavalry wage discount, construction speed, caravan profit, siege engineering debuff"
```

(DLL rebuild + copy into `bin/` happens once, in Task 9, after Task 8's territory C# changes are
also in — don't copy an intermediate DLL here, just confirm `dotnet build` succeeds.)

---

### Task 5: Remove the dead `character_creation.xml`

**Files:**
- Delete: `ModuleData/character_creation.xml`
- Modify: none in `SubModule.xml` (it was never registered there — confirmed by inspection, no
  `XmlName` entry references `character_creation`)

**Interfaces:** none — this file is not loaded by the game (see spec Module 3). The real backstory
implementation is `Source/SeljukEmpire/CharacterCreation/SeljukCharacterCreationContentHandler.cs`,
untouched by this task (its string coverage is handled in Task 15).

- [ ] **Step 1: Delete the file**

```bash
git rm ModuleData/character_creation.xml
```

- [ ] **Step 2: Confirm nothing references it**

```bash
grep -rn "character_creation" SubModule.xml
```
Expected: no output (confirms it was never wired in, so removing it changes nothing at runtime).

- [ ] **Step 3: Commit**

```bash
git commit -m "Remove dead character_creation.xml (backstories are implemented in C#, not XML-loaded)"
```

---

### Task 6: Rewrite settlement ownership (`settlements.xml`) for the new contiguous territory

**Files:**
- Modify: `ModuleData/settlements.xml`
- Modify: `SubModule.xml` (register `Settlements` XmlNode — currently missing)
- Modify: `ModuleData/kingdoms.xml` (move `initial_home_settlement` off the reverted `town_K1`)

**Interfaces:**
- Consumes: `Culture.seljuk` (Task 1).
- Produces: the settlement-id set `{town_ES1, town_A2, castle_ES4, castle_A6, castle_ES5}` plus
  their villages — consumed by Task 7 (`SeljukSettlementBehavior.cs`), Task 8
  (`IsSeljukSettlement` lists), and Task 10 (clan home settlements).

**Context:** confirmed in the design spec — this file currently has no effect in-game because
`SubModule.xml` never registers it (`<XmlName id="Settlements" path="settlements"/>` is the
required entry, confirmed present in Native's `SandBox/SubModule.xml` and absent from this mod's).
Runtime ownership is entirely driven by `SeljukSettlementBehavior.cs` (Task 7). This task makes the
XML match what Task 7 will do, and registers it so the file stops being misleading dead data.

- [ ] **Step 1: Delete the 11 reverting entries and their villages from `settlements.xml`**

Remove these `<Settlement>` elements entirely (deleting the override means the settlement keeps
its Native `owner`/`culture`/`name` — do not replace with Native values, just delete the element):
`town_K1`, `town_K4`, `town_ES4`, `town_ES2`, `town_K6`, `town_A4` (all 6 towns), `castle_K2`,
`castle_K5`, `castle_K1`, `castle_A8`, `castle_ES3` (all 5 castles), and every `village_K1_*`,
`village_K4_*`, `village_ES4_*`, `village_ES2_*`, `village_K6_*`, `village_A4_*`,
`castle_village_K2_1`, `castle_village_K5_1`, `castle_village_K1_1`, `castle_village_A8_1`,
`castle_village_ES3_1` entry (23 villages total). After this step the file should be empty of
`<Settlement>` elements except the ones added in Step 2.

- [ ] **Step 2: Add the 5 transferred settlements and their villages**

```xml
  <!-- DANUSTİCA (Yeni Selçuklu Başkenti) -->
  <Settlement id="town_ES1" name="{=seljuk_town_danustica}Danustica" owner="Faction.clan_seljuk_royal" culture="Culture.seljuk">
    <Components>
      <Town id="town_comp_ES1" is_castle="false" prosperity="7800" />
    </Components>
  </Settlement>
  <Settlement id="village_ES1_2" name="{=seljuk_vil_polisia}Polisia" culture="Culture.seljuk" />
  <Settlement id="village_ES1_3" name="{=seljuk_vil_tegresos}Tegresos" culture="Culture.seljuk" />
  <Settlement id="village_ES1_4" name="{=seljuk_vil_erebulos}Erebulos" culture="Culture.seljuk" />

  <!-- HUSN FULQ (Kayı Boyu Merkezi) -->
  <Settlement id="town_A2" name="{=seljuk_town_husnfulq}Husn Fulq" owner="Faction.clan_kayi_oguz" culture="Culture.seljuk">
    <Components>
      <Town id="town_comp_A2" is_castle="false" prosperity="6600" />
    </Components>
  </Settlement>
  <Settlement id="village_A2_2" name="{=seljuk_vil_abukhih}Abu Khih" culture="Culture.seljuk" />
  <Settlement id="village_A2_3" name="{=seljuk_vil_hoqqa}Hoqqa" culture="Culture.seljuk" />

  <!-- LAVENİA KALESİ (Danişmendliler) -->
  <Settlement id="castle_ES4" name="{=seljuk_castle_lavenia}Lavenia Kalesi" owner="Faction.clan_danismend" culture="Culture.seljuk" />
  <Settlement id="castle_village_ES4_1" name="{=seljuk_vil_lavenia}Lavenia" culture="Culture.seljuk" />

  <!-- ŞİBAL ZÜMR KALESİ (Artuklular) -->
  <Settlement id="castle_A6" name="{=seljuk_castle_shibalzumr}Şibal Zümr Kalesi" owner="Faction.clan_artuk" culture="Culture.seljuk" />
  <Settlement id="castle_village_A6_1" name="{=seljuk_vil_shibalzumr}Şibal Zümr" culture="Culture.seljuk" />

  <!-- MORENİA KALESİ (Ahlatşahlar) -->
  <Settlement id="castle_ES5" name="{=seljuk_castle_morenia}Morenia Kalesi" owner="Faction.clan_ahlatsah" culture="Culture.seljuk" />
  <Settlement id="castle_village_ES5_1" name="{=seljuk_vil_morenia}Morenia" culture="Culture.seljuk" />
```

- [ ] **Step 3: Register `Settlements` in `SubModule.xml`**

Add, inside `<Xmls>`:
```xml
    <XmlNode>
      <XmlName id="Settlements" path="settlements"/>
      <IncludedGameTypes>
        <GameType value="Campaign"/>
        <GameType value="CampaignStoryMode"/>
        <GameType value="CustomGame"/>
      </IncludedGameTypes>
    </XmlNode>
```

- [ ] **Step 4: Move the kingdom's capital reference off the reverted `town_K1`**

In `ModuleData/kingdoms.xml`, change:
```
initial_home_settlement="Settlement.town_K1"
```
to:
```
initial_home_settlement="Settlement.town_ES1"
```

- [ ] **Step 5: Add the new string ids to both language files**

Add to `ModuleData/Languages/strings.xml` (English — use the actual Native English names as the
base, since these are real Bannerlord places): `seljuk_town_danustica`="Danustica",
`seljuk_town_husnfulq`="Husn Fulq", `seljuk_castle_lavenia`="Lavenia Castle",
`seljuk_castle_shibalzumr`="Shibal Zumr Castle", `seljuk_castle_morenia`="Morenia Castle",
`seljuk_vil_polisia`="Polisia", `seljuk_vil_tegresos`="Tegresos", `seljuk_vil_erebulos`="Erebulos",
`seljuk_vil_abukhih`="Abu Khih", `seljuk_vil_hoqqa`="Hoqqa", `seljuk_vil_lavenia`="Lavenia",
`seljuk_vil_shibalzumr`="Shibal Zumr", `seljuk_vil_morenia`="Morenia" — same `<string id="..."
text="..." />` format as existing entries. Add matching Turkish entries to
`ModuleData/Languages/TR/strings.xml` using the Turkish transliterations already used as the
`name=` values in Step 2 (Danustica, Husn Fulq, Lavenia Kalesi, Şibal Zümr Kalesi, Morenia Kalesi,
Polisia, Tegresos, Erebulos, Abu Khih, Hoqqa, Lavenia, Şibal Zümr, Morenia).

- [ ] **Step 6: Validate**

```bash
python -c "
import xml.etree.ElementTree as ET
for f in ['ModuleData/settlements.xml','SubModule.xml','ModuleData/kingdoms.xml','ModuleData/Languages/strings.xml','ModuleData/Languages/TR/strings.xml']:
    ET.parse(f)
print('OK')
"
grep -c '<Settlement ' ModuleData/settlements.xml
```
Expected: `OK`, and the settlement count is `13` (5 towns/castles + 8 villages: 3 + 2 + 1 + 1 + 1
= 8 villages, plus 5 towns/castles = 13).

- [ ] **Step 7: Commit**

```bash
git add ModuleData/settlements.xml SubModule.xml ModuleData/kingdoms.xml ModuleData/Languages/strings.xml ModuleData/Languages/TR/strings.xml
git commit -m "Rewrite settlements.xml for the new contiguous Seljuk territory and register it in SubModule.xml"
```

---

### Task 7: Update `SeljukSettlementBehavior.cs` for the new territory

**Files:**
- Modify: `Source/SeljukEmpire/Settlements/SeljukSettlementBehavior.cs`

**Interfaces:**
- Consumes: the settlement-id set from Task 6.
- No public interface changes — `OnSessionLaunched` behavior only.

- [ ] **Step 1: Replace `InitializeSeljukTerritories()`'s body**

Replace the entire method body (currently calling `SetupTown`/`SetupCastle`/`SetupVillage` for the
11 old settlements) with:

```csharp
private void InitializeSeljukTerritories()
{
    try
    {
        // =====================================================================
        // 1. TOWNS (ŞEHİRLER)
        // =====================================================================
        SetupTown("town_ES1", "clan_seljuk_royal", "{=seljuk_town_danustica}Danustica", 7800f);
        SetupTown("town_A2", "clan_kayi_oguz", "{=seljuk_town_husnfulq}Husn Fulq", 6600f);

        // =====================================================================
        // 2. CASTLES (KALELER)
        // =====================================================================
        SetupCastle("castle_ES4", "clan_danismend", "{=seljuk_castle_lavenia}Lavenia Kalesi");
        SetupCastle("castle_A6", "clan_artuk", "{=seljuk_castle_shibalzumr}Şibal Zümr Kalesi");
        SetupCastle("castle_ES5", "clan_ahlatsah", "{=seljuk_castle_morenia}Morenia Kalesi");

        // =====================================================================
        // 3. VILLAGES (KÖYLER)
        // =====================================================================
        // Danustica Villages
        SetupVillage("village_ES1_2", "{=seljuk_vil_polisia}Polisia");
        SetupVillage("village_ES1_3", "{=seljuk_vil_tegresos}Tegresos");
        SetupVillage("village_ES1_4", "{=seljuk_vil_erebulos}Erebulos");

        // Husn Fulq Villages
        SetupVillage("village_A2_2", "{=seljuk_vil_abukhih}Abu Khih");
        SetupVillage("village_A2_3", "{=seljuk_vil_hoqqa}Hoqqa");

        // Castle Villages
        SetupVillage("castle_village_ES4_1", "{=seljuk_vil_lavenia}Lavenia");
        SetupVillage("castle_village_A6_1", "{=seljuk_vil_shibalzumr}Şibal Zümr");
        SetupVillage("castle_village_ES5_1", "{=seljuk_vil_morenia}Morenia");
    }
    catch (Exception)
    {
        // Engine safety catch
    }
}
```

Leave `SetupTown`, `SetupCastle`, `SetupVillage`, `RenameSettlement`, and
`InitializeSeljukKingdomHierarchy` unchanged — the clan-leader/kingdom-membership logic in
`InitializeSeljukKingdomHierarchy` doesn't reference settlement ids at all, only clan/hero ids, so
it's unaffected by the territory change.

- [ ] **Step 2: Verify no old settlement ids remain in this file**

```bash
grep -n 'town_K1\|town_K4\|town_ES4\|town_ES2\|town_K6\|town_A4\|castle_K2\|castle_K5\|castle_K1\|castle_A8\|castle_ES3' Source/SeljukEmpire/Settlements/SeljukSettlementBehavior.cs
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add Source/SeljukEmpire/Settlements/SeljukSettlementBehavior.cs
git commit -m "Point SeljukSettlementBehavior at the new 5-settlement contiguous territory"
```

(Build verification happens in Task 9, after Task 8's edits to the same assembly — no need to
build twice.)

---

### Task 8: Update the hardcoded settlement lists in the recruitment/volunteer models

**Files:**
- Modify: `Source/SeljukEmpire/Recruitment/SeljukVolunteerModel.cs`
- Modify: `Source/SeljukEmpire/Recruitment/SeljukRecruitmentBehavior.cs`

**Interfaces:**
- Consumes: the settlement-id set from Task 6/7.
- No public interface changes — both files' `IsSeljukSettlement(Settlement)` private static
  methods only.

**Context:** both files currently have an identical hardcoded `IsSeljukSettlement` method (verified
by direct comparison — they're byte-for-byte the same logic). This task updates the `sid.StartsWith`
prefix list in both.

- [ ] **Step 1: Replace the prefix list in both files**

In `SeljukVolunteerModel.cs` and `SeljukRecruitmentBehavior.cs`, replace:
```csharp
            if (sid.StartsWith("town_K1") || sid.StartsWith("town_K4") || sid.StartsWith("town_ES4") ||
                sid.StartsWith("town_ES2") || sid.StartsWith("town_K6") || sid.StartsWith("town_A4") ||
                sid.StartsWith("castle_K2") || sid.StartsWith("castle_K5") || sid.StartsWith("castle_K1") ||
                sid.StartsWith("castle_A8") || sid.StartsWith("castle_ES3") ||
                sid.StartsWith("village_K1_") || sid.StartsWith("village_K4_") || sid.StartsWith("village_ES4_") ||
                sid.StartsWith("village_ES2_") || sid.StartsWith("village_K6_") || sid.StartsWith("village_A4_") ||
                sid.StartsWith("castle_village_K2_") || sid.StartsWith("castle_village_K5_") ||
                sid.StartsWith("castle_village_K1_") || sid.StartsWith("castle_village_A8_") ||
                sid.StartsWith("castle_village_ES3_"))
```
with:
```csharp
            if (sid.StartsWith("town_ES1") || sid.StartsWith("town_A2") ||
                sid.StartsWith("castle_ES4") || sid.StartsWith("castle_A6") || sid.StartsWith("castle_ES5") ||
                sid.StartsWith("village_ES1_") || sid.StartsWith("village_A2_") ||
                sid.StartsWith("castle_village_ES4_") || sid.StartsWith("castle_village_A6_") ||
                sid.StartsWith("castle_village_ES5_"))
```
in both files (each file has exactly one occurrence of the old block, inside its own
`IsSeljukSettlement` method — confirmed by earlier direct reads of both files).

- [ ] **Step 2: Verify**

```bash
grep -c 'town_ES1\|castle_ES4' Source/SeljukEmpire/Recruitment/SeljukVolunteerModel.cs Source/SeljukEmpire/Recruitment/SeljukRecruitmentBehavior.cs
grep -c 'town_K1\|castle_K2' Source/SeljukEmpire/Recruitment/SeljukVolunteerModel.cs Source/SeljukEmpire/Recruitment/SeljukRecruitmentBehavior.cs
```
Expected: first command ≥1 for both files, second command `0` for both.

- [ ] **Step 3: Commit**

```bash
git add Source/SeljukEmpire/Recruitment/SeljukVolunteerModel.cs Source/SeljukEmpire/Recruitment/SeljukRecruitmentBehavior.cs
git commit -m "Point IsSeljukSettlement at the new territory in both recruitment models"
```

---

### Task 9: Rebuild and ship `SeljukTactics.dll`

**Files:**
- Modify (generated): `bin/Win64_Shipping_Client/SeljukTactics.dll`, `.pdb`, `.deps.json`
- Modify (generated): `bin/Win64_Shipping_wEditor/SeljukTactics.dll`, `.pdb`, `.deps.json`

**Interfaces:**
- Consumes: all C# changes from Tasks 4, 7, 8.
- Produces: the shipped DLL players get from the repo/release — nothing downstream in this plan
  depends on it programmatically, but it's the actual deliverable of every C# task above.

- [ ] **Step 1: Build Release**

```bash
dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release
```
Expected: `Build succeeded.`, 0 errors. If Task 4's model-override code doesn't compile because the
actual `TaleWorlds.CampaignSystem` API differs from what was guessed, fix it now using the
compiler's exact error message (wrong method name/signature) rather than the plan's snippet —
the plan's C# is a best-effort based on Native's known patterns, not decompiled ground truth.

- [ ] **Step 2: Locate the build output and copy it into both `bin/` folders**

```bash
find Source/SeljukEmpire/bin -iname "SeljukTactics.dll" 2>/dev/null
```
This finds the freshly-built DLL (likely under `Source/SeljukEmpire/bin/Release/netstandard2.0/` —
confirm the exact path from the find output, since it depends on the `.csproj`'s configured output
path). Copy it and its `.pdb` over both existing shipped copies:

```bash
cp "<path from find above>/SeljukTactics.dll" bin/Win64_Shipping_Client/SeljukTactics.dll
cp "<path from find above>/SeljukTactics.pdb" bin/Win64_Shipping_Client/SeljukTactics.pdb
cp "<path from find above>/SeljukTactics.dll" bin/Win64_Shipping_wEditor/SeljukTactics.dll
cp "<path from find above>/SeljukTactics.pdb" bin/Win64_Shipping_wEditor/SeljukTactics.pdb
```

- [ ] **Step 3: Sanity-check the copied files**

```bash
ls -la bin/Win64_Shipping_Client/SeljukTactics.dll bin/Win64_Shipping_wEditor/SeljukTactics.dll
```
Expected: both files exist with a modification timestamp from this step (not the original commit's
timestamp), confirming the copy happened.

- [ ] **Step 4: Commit**

```bash
git add bin/Win64_Shipping_Client/SeljukTactics.dll bin/Win64_Shipping_Client/SeljukTactics.pdb bin/Win64_Shipping_wEditor/SeljukTactics.dll bin/Win64_Shipping_wEditor/SeljukTactics.pdb
git commit -m "Rebuild SeljukTactics.dll with territory and culture-bonus changes"
```

---

### Task 10: Reassign clan home settlements (`factions.xml`)

**Files:**
- Modify: `ModuleData/factions.xml`

**Interfaces:**
- Consumes: the 5-settlement set from Task 6.

**Context:** 11 clans, 5 settlements. Assignment below follows historical weight (the two Tier-6
clans — the ruling dynasty and the Kayı/Ottoman-founding line — get the two towns; three of the
largest/longest-lived historical beyliks get the three castles) and leaves the remaining 6 clans
landless at game start, which mirrors how many Native minor clans start with zero fiefs. Landless
clans still need a valid `initial_home_settlement` (used as a spawn/gathering reference, not an
ownership claim) — point them at the new capital.

- [ ] **Step 1: Update `initial_home_settlement` for all 11 `<Faction>` elements**

| Clan id | New `initial_home_settlement` | Reason |
|---|---|---|
| `clan_seljuk_royal` | `Settlement.town_ES1` | Ruling clan, gets the capital |
| `clan_kayi_oguz` | `Settlement.town_A2` | Only other Tier-6 clan |
| `clan_danismend` | `Settlement.castle_ES4` | Owns Lavenia Kalesi (Task 6) |
| `clan_artuk` | `Settlement.castle_A6` | Owns Şibal Zümr Kalesi (Task 6) |
| `clan_ahlatsah` | `Settlement.castle_ES5` | Owns Morenia Kalesi (Task 6) |
| `clan_nizamiye` | `Settlement.town_ES1` | Landless — administrative clan, based at capital |
| `clan_mengucek` | `Settlement.town_ES1` | Landless at game start |
| `clan_saltuk` | `Settlement.town_ES1` | Landless at game start |
| `clan_caka` | `Settlement.town_ES1` | Landless at game start |
| `clan_karaman` | `Settlement.town_ES1` | Landless at game start |
| `clan_ahi_order` | `Settlement.town_ES1` | Landless at game start |

For each `<Faction id="...">` element, replace its `initial_home_settlement="Settlement.town_..."`
or `="Settlement.castle_..."` value with the value from this table (5 clans change to town/castle
ids introduced in Task 6, 6 clans change to `Settlement.town_ES1`).

- [ ] **Step 2: Verify**

```bash
grep -o 'id="clan_[a-z_]*"[^>]*initial_home_settlement="Settlement\.[a-zA-Z0-9_]*"' ModuleData/factions.xml
```
Expected: 11 lines, each showing one of the 5 new settlement ids from the table above — no
`town_K1`, `town_K4`, `town_ES4`, `town_ES2`, `town_K6`, `town_A4`, `castle_K2`, `castle_K5`,
`castle_K1`, `castle_A8`, or `castle_ES3` anywhere in the output.

- [ ] **Step 3: Validate XML well-formedness**

```bash
python -c "import xml.etree.ElementTree as ET; ET.parse('ModuleData/factions.xml'); print('OK')"
```

- [ ] **Step 4: Commit**

```bash
git add ModuleData/factions.xml
git commit -m "Reassign clan home settlements across the new 5-settlement territory"
```

---

### Task 11: Fix age/family consistency in `lords.xml` and `heroes.xml`

**Files:**
- Modify: `ModuleData/lords.xml`
- Modify: `ModuleData/heroes.xml` (no changes needed — relations are correct, only ages in
  `lords.xml` need to change to make those relations biologically consistent)

**Interfaces:** none — attribute-only edits on existing `NPCCharacter` elements.

**Context — concrete bugs found by cross-referencing `heroes.xml`'s family links against
`lords.xml`'s ages:**

`heroes.xml` declares Ertuğrul Gazi (`father`/`mother` of `gunduz_alp`, `savci_bey`, `osman_gazi`;
`mother` is `hayme_ana`) and Halime Hatun (`mother` of the same three sons). At the ages currently
in `lords.xml`, this is impossible: Ertuğrul is `age="34"` while his son Gündüz Alp is
`age="24"` (would make Ertuğrul 10 at his son's birth); Halime Hatun is `age="30"` against the same
son (would make her 6). This needs a full, internally-consistent age rework across the whole Kayı
Boyu branch, not just Ertuğrul.

- [ ] **Step 1: Update ages in `lords.xml`**

| Hero id | Old age | New age | Consistency check |
|---|---|---|---|
| `hayme_ana` | 55 | 74 | Mother of Gündoğdu(52), Sungur(50), Ertuğrul(48), Dündar(46) — ages at birth 22, 24, 26, 28: all plausible |
| `gundogdu_bey` | 37 | 52 | Eldest brother |
| `sungur_tekin` | 36 | 50 | Second brother |
| `ertugrul_gazi` | 34 | 48 | Third brother; father of sons below |
| `dundar_the_hawk` | 21 | 46 | Youngest brother (was implausibly the same generation as his own nephews before this fix) |
| `halime_hatun` | 30 | 44 | Ertuğrul's wife, close in age (4 years younger) |
| `gunduz_alp` | 24 | 26 | Ertuğrul was 22, Halime was 18 at his birth |
| `savci_bey` | 22 | 23 | Ertuğrul was 25, Halime was 21 |
| `osman_gazi` | 20 | 19 | Ertuğrul was 29, Halime was 25 — matches the historical relative birth order (Gündüz > Savcı > Osman in age) while fixing the absolute numbers |

Also tighten (not strictly impossible, but very young for a ruling sultan's household — matches
the same category of issue, fix for consistency with the rest of this task):

| Hero id | Old age | New age | Consistency check |
|---|---|---|---|
| `lord_seljuk_alp_arslan` | 42 | 46 | Father of Melikşah/Mesud below |
| `lord_seljuk_terken_hatun` | 38 | 40 | Mother of Melikşah/Mesud |
| `lord_seljuk_meliksah` | 22 | 24 | Alp Arslan was 22, Terken Hatun was 16 at birth — leave as-is; already the least severe case and further widening risks pushing Alp Arslan/Terken Hatun into an age bracket that reads oddly for their own `age=` fields — do not change `lord_seljuk_meliksah`'s or `lord_seljuk_mesud`'s ages, only the parents' |
| `lord_seljuk_mesud` | 20 | 20 | unchanged |

For every id in the two tables above, change only the `age="..."` attribute value on that
`NPCCharacter` element — do not touch skills, equipment, traits, or any other attribute.

- [ ] **Step 2: Verify every changed value landed**

```bash
grep -o 'id="hayme_ana"[^>]*age="[0-9]*"\|id="gundogdu_bey"[^>]*age="[0-9]*"\|id="sungur_tekin"[^>]*age="[0-9]*"\|id="ertugrul_gazi"[^>]*age="[0-9]*"\|id="dundar_the_hawk"[^>]*age="[0-9]*"\|id="halime_hatun"[^>]*age="[0-9]*"\|id="gunduz_alp"[^>]*age="[0-9]*"\|id="savci_bey"[^>]*age="[0-9]*"\|id="osman_gazi"[^>]*age="[0-9]*"\|id="lord_seljuk_alp_arslan"[^>]*age="[0-9]*"\|id="lord_seljuk_terken_hatun"[^>]*age="[0-9]*"' ModuleData/lords.xml
```
Expected: 11 lines matching the "New age" column values from both tables above exactly.

- [ ] **Step 3: Validate XML well-formedness**

```bash
python -c "import xml.etree.ElementTree as ET; ET.parse('ModuleData/lords.xml'); print('OK')"
```

- [ ] **Step 4: Commit**

```bash
git add ModuleData/lords.xml
git commit -m "Fix biologically-impossible parent/child ages in the Kayi Boyu and royal family trees"
```

---

### Task 12: Troop tree upgrade audit against Native Khuzait conventions

**Files:**
- Modify: `ModuleData/troops.xml` (only where the audit script in Step 1 flags a real deviation)
- Create (scratch, not committed): audit script, run inline

**Interfaces:** none — attribute-level tuning within existing `NPCCharacter`/`upgrade_targets`
elements.

- [ ] **Step 1: Run a tier-jump audit comparing this mod's troop tree to Native Khuzait's**

```bash
python3 - <<'PY'
import re

def load_troops(path):
    text = open(path, encoding='utf-8').read()
    troops = {}
    for m in re.finditer(r'<NPCCharacter\s+id="([^"]+)"[^>]*level="(\d+)"', text):
        troops[m.group(1)] = int(m.group(2))
    return troops

def load_upgrade_chain(path, start_id):
    text = open(path, encoding='utf-8').read()
    chain = [start_id]
    current = start_id
    seen = {start_id}
    while True:
        block_match = re.search(r'<NPCCharacter\s+id="%s".*?</NPCCharacter>' % re.escape(current), text, re.S)
        if not block_match:
            break
        targets = re.findall(r'<upgrade_target id="NPCCharacter\.([^"]+)"', block_match.group(0))
        if not targets or targets[0] in seen:
            break
        current = targets[0]
        seen.add(current)
        chain.append(current)
    return chain

mod_troops = load_troops('ModuleData/troops.xml')
native_path = r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\SandBoxCore\ModuleData\spnpccharacters.xml"
native_troops = load_troops(native_path)

seljuk_infantry_chain = load_upgrade_chain('ModuleData/troops.xml', 'seljuk_peasant')
khuzait_infantry_chain = load_upgrade_chain(native_path, 'khuzait_recruit')  # confirm this id is correct via grep below if it errors

print("Seljuk infantry chain levels:", [mod_troops.get(t) for t in seljuk_infantry_chain])
print("Khuzait infantry chain levels:", [native_troops.get(t) for t in khuzait_infantry_chain])
PY
```

If `khuzait_recruit` isn't the right starting id (confirm with
`grep -o 'id="khuzait_[a-z_]*" default_group="Infantry" level="1"' <native path>` first — Native
Khuzait's tier-1 basic troop id needs to be read off the actual file, not assumed), adjust the
script's `start_id` and re-run. Do the same comparison for the Cavalry chain (`seljuk_horseman` →
... vs. Native's Khuzait cavalry chain) and the HorseArcher chain (`seljuk_clan_scout` → ... vs.
Native's Khuzait horse archer chain).

- [ ] **Step 2: Fix any tier where the level-per-upgrade-step gap deviates by more than 1 from Native's pattern**

For each chain, compute the level delta between consecutive troops in both the mod's chain and
Native's chain. Native Khuzait's steps are consistently +5 levels per tier (confirmed earlier by
direct inspection: recruit-equivalent tier1 → tier6 in ~5-level increments). Where this mod's chain
has a step significantly larger or smaller than +5 (already spot-checked in Task 3's neighborhood:
`seljuk_peasant` level 6 → `seljuk_footman` level 11 → `seljuk_armored_infantry` level 16 →
`seljuk_nizam_infantry` level 21 is already a clean +5 pattern and needs no change) — only touch
entries the script actually flags as deviating. Adjust the flagged troop's `level=` attribute (and
proportionally its `<skills>` values, keeping the same relative skill distribution the troop
already has) to close the gap to within 1 of Native's per-tier delta.

- [ ] **Step 3: Validate**

```bash
python -c "import xml.etree.ElementTree as ET; ET.parse('ModuleData/troops.xml'); print('OK')"
```

- [ ] **Step 4: Commit**

```bash
git add ModuleData/troops.xml
git commit -m "Align Seljuk troop tree tier progression with Native Khuzait conventions"
```

---

### Task 13: Localization coverage audit (all modules)

**Files:**
- Modify: `ModuleData/Languages/strings.xml` (fill EN gaps)
- Modify: `ModuleData/Languages/TR/strings.xml` (fill TR gaps)

**Interfaces:** none — this is a coverage-closing task run last, after every other task has
introduced whatever new `{=id}` references it needed.

- [ ] **Step 1: Run the coverage audit script**

```bash
python3 - <<'PY'
import re, glob

def defined_ids(path):
    text = open(path, encoding='utf-8').read()
    return set(re.findall(r'<string id="([^"]+)"', text))

def used_ids(paths):
    ids = set()
    for p in paths:
        text = open(p, encoding='utf-8', errors='ignore').read()
        ids |= set(re.findall(r'\{=([a-zA-Z0-9_]+)\}', text))
    return ids

xml_sources = glob.glob('ModuleData/*.xml')
cs_sources = glob.glob('Source/SeljukEmpire/**/*.cs', recursive=True)

used = used_ids(xml_sources + cs_sources)
en_defined = defined_ids('ModuleData/Languages/strings.xml')
tr_defined = defined_ids('ModuleData/Languages/TR/strings.xml')

missing_en = sorted(used - en_defined)
missing_tr = sorted(used - tr_defined)

print(f"Total {{=id}} references found: {len(used)}")
print(f"Missing from EN strings.xml ({len(missing_en)}):")
for i in missing_en: print(" ", i)
print(f"Missing from TR strings.xml ({len(missing_tr)}):")
for i in missing_tr: print(" ", i)
PY
```

- [ ] **Step 2: Fill every gap the script reports**

For each id in `missing_en`, add a `<string id="ID" text="..." />` to
`ModuleData/Languages/strings.xml` with an accurate English translation of what that id represents
(cross-reference the `{=ID}Fallback text` usage site the script found it in — the fallback text
after `}` at each usage site is the source text to translate). For each id in `missing_tr`, add the
equivalent to `ModuleData/Languages/TR/strings.xml` — for ids whose usage-site fallback is already
Turkish, the TR entry can reuse that exact text; only the EN entry needs translating in that case.

- [ ] **Step 3: Re-run the audit script from Step 1**

Expected: `Missing from EN strings.xml (0):` and `Missing from TR strings.xml (0):` — empty lists
in both. Do not proceed to commit until both are empty.

- [ ] **Step 4: Validate XML well-formedness**

```bash
python -c "
import xml.etree.ElementTree as ET
for f in ['ModuleData/Languages/strings.xml','ModuleData/Languages/TR/strings.xml']:
    ET.parse(f)
print('OK')
"
```

- [ ] **Step 5: Commit**

```bash
git add ModuleData/Languages/strings.xml ModuleData/Languages/TR/strings.xml
git commit -m "Close TR/EN localization coverage gaps across all mod content"
```

---

### Task 14: Diplomacy — remove the Northern Empire auto-war entry

**Files:**
- Modify: `ModuleData/kingdoms.xml`

**Interfaces:** none.

**Context:** confirmed against Native `SandBox/ModuleData/spkingdoms.xml` — the kingdom id
`"empire"` is the **Northern Empire** (its `name=` attribute resolves to
`{=NF627oiX}Northern Empire`). This mod's `<relationship kingdom="Kingdom.empire" value="-20" />`
is exactly the "auto-war with Northern Empire" the request describes.
**"Karakhergit" doesn't need a fix** — it isn't a kingdom or faction anywhere in this mod or the
base game; it's a sub-tribe name that appears only in Khuzait culture's own flavor text
(`spcultures.xml`: "Nachaghan, Arkits, Khergits, Karakhergits" as historical names for the peoples
who make up the Khuzait confederacy). This mod already has `<relationship kingdom="Kingdom.khuzait"
value="15" />`, which already covers the Khuzait/Karakhergit people. No separate entry is needed or
possible — skip any change for this part of the original request.

- [ ] **Step 1: Change the Northern Empire relationship value**

In `ModuleData/kingdoms.xml`, change:
```xml
      <relationship kingdom="Kingdom.empire" value="-20" />
```
to:
```xml
      <relationship kingdom="Kingdom.empire" value="0" />
```

- [ ] **Step 2: Verify**

```bash
grep -n 'Kingdom.empire' ModuleData/kingdoms.xml
```
Expected: one line, `value="0"`.

- [ ] **Step 3: Validate XML well-formedness**

```bash
python -c "import xml.etree.ElementTree as ET; ET.parse('ModuleData/kingdoms.xml'); print('OK')"
```

- [ ] **Step 4: Commit**

```bash
git add ModuleData/kingdoms.xml
git commit -m "Remove Seljuk auto-war with the Northern Empire at campaign start"
```

---

## Self-review notes (completed during plan authoring, not a task to execute)

- **Spec coverage:** Module 1 → Tasks 3, 12. Module 2 → Tasks 6, 7, 8, 9. Module 3 → Tasks 1, 2, 4,
  5. Module 4 → Task 14. Module 5 → Tasks 10, 11. Module 6 → Task 13. All six spec modules have at
  least one task; territory (Module 2) and culture (Module 3) — the two modules with real C#
  dependencies — each got split into multiple tasks along their natural file boundaries rather than
  one oversized task.
- **Ordering:** Tasks 1–2 (culture) before Task 6 (territory, which tags new settlements
  `Culture.seljuk`) before Task 10 (clan homes, which needs Task 6's settlement ids) — matches the
  spec's Implementation Order section. Task 9 (DLL rebuild) is placed after both Task 7 and Task 8
  so the C# assembly is only built once with all territory-related changes in. Task 4's C# is
  written before Task 9 too, so one rebuild covers both the culture-bonus and territory C# work.
  Task 13 (localization) is last, after every task that could introduce a new `{=id}`.
- **Placeholder scan:** every task has concrete file paths, concrete XML/C#/Python content, and a
  concrete verification command with an expected result — the two audit-script tasks (12, 13) are
  the closest thing to open-ended, but each has a runnable script plus an explicit "re-run until
  empty/no output" exit condition, not a vague "review and fix" instruction.
- **Type/id consistency check:** `Culture.seljuk` (Task 1) is the exact id string used in every
  later task's `culture="Culture.seljuk"` and `Culture.StringId == "seljuk"` checks. The 5
  settlement ids from Task 6 (`town_ES1`, `town_A2`, `castle_ES4`, `castle_A6`, `castle_ES5`) and
  their village ids are used identically across Tasks 6, 7, 8, and 10. `SeljukWageModel` and
  `SeljukConstructionSpeedModel` class names introduced in Task 4 Step 2 are the same names used in
  Task 4 Step 3's registration call.
