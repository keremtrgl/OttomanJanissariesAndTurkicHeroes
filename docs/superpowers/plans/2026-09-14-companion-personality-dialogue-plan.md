# Companion Personality Dialogue Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give all 25 recruitable companions 3 rotating greeting-variant lines each for repeated in-party conversations, using the exact same mechanism already proven for Seljuk clan leaders.

**Architecture:** One new `CampaignBehaviorBase`, `CompanionPersonalityDialogueBehavior.cs`, registered alongside the mod's other Immersion behaviors. Each companion gets a private `IsX()` predicate checking `Hero.OneToOneConversationHero.StringId` and 3 `AddDialogLine` calls on `"start"→"lord_pretalk"` gated by a per-companion salted `GetGreetingVariant`, mirroring `SeljukDialogueBehavior.cs` line-for-line in structure.

**Tech Stack:** C# / `TaleWorlds.CampaignSystem` (`CampaignBehaviorBase`, `CampaignGameStarter.AddDialogLine`), `netstandard2.0` (`SeljukTactics.csproj`).

## Global Constraints

- Salts must not collide with any existing `GetGreetingVariant` call in the mod — existing files use salts 0 through 46; this file starts at **47**.
- EN + TR localization only this pass; other 6 languages are a normal follow-up commit per README's documented convention.
- Every greeting line must be traceable to something already documented in that companion's existing `GameText` backstory content (profession, personality trait, or notable biographical detail) — not invented personality unconnected to it.
- `verify_mod.py` must stay at 0 errors after each task, including its `dialogue-hero-ids` check (check 17), which validates every `Hero.StringId` condition against a real character id.

## Companion roster and confirmed StringIds

Verified via `grep -oE 'id="spc_wanderer_[a-z_]+"' ModuleData/rival_culture_companions.xml` and the
equivalent for `ModuleData/seljuk_special_characters.xml`:

**14 named historical companions** (`rival_culture_companions.xml`), 2 per culture:
- Seljuk: `spc_wanderer_seljuk_khusraw` (Nasir Khusraw), `spc_wanderer_seljuk_khayyam` (Ömer Hayyam)
- Byzantine: `spc_wanderer_empire_psellos` (Michael Psellos), `spc_wanderer_empire_roussel` (Roussel de Bailleul)
- Abbasid: `spc_wanderer_aserai_ghazali` (Gazali), `spc_wanderer_aserai_usama` (Usame bin Münkız)
- Georgian: `spc_wanderer_sturgia_petritsi` (İoane Petritsi), `spc_wanderer_sturgia_vardan` (Svanetili Vardan)
- Crusader: `spc_wanderer_vlandia_peterhermit` (Keşiş Piyer), `spc_wanderer_vlandia_bartholomew` (Bartholomeuslu Petrus)
- Armenian: `spc_wanderer_battania_matthew` (Urfalı Mateos), `spc_wanderer_battania_heratsi` (Mıhitar Heratsi)
- Karakhanid: `spc_wanderer_khuzait_kashgari` (Kaşgarlı Mahmud), `spc_wanderer_khuzait_yasawi` (Ahmed Yesevi)

**11 generic Seljuk wanderers** (`seljuk_special_characters.xml`): `spc_wanderer_seljuk_0` through
`spc_wanderer_seljuk_10`, matching graph.md section 9b's established personas: 0=Bilge (steppe sage),
1=Şahin (horse archer), 2=Babasız (orphaned vagrant), 3=Demirgöz (marksman), 4=Dışlanmış (exiled
warrior), 5=Deli (berserker), 6=Boz Şahin (veteran horse archer), 7=Dişi Kurt (female warrior),
8=Yalnız (lone survivor), 9=Çevik (messenger-scout), 10=Perişan (destitute survivor).

**Salt assignment** (sequential from 47, in the order listed above): Khusraw=47, Khayyam=48,
Psellos=49, Roussel=50, Gazali=51, Usama=52, Petritsi=53, Vardan=54, PeterHermit=55, Bartholomew=56,
Matthew=57, Heratsi=58, Kashgari=59, Yasawi=60, wanderer_0=61 .. wanderer_10=71.

---

### Task 1: File scaffold, helper, and the 14 named companions

**Files:**
- Create: `Source/SeljukEmpire/Immersion/CompanionPersonalityDialogueBehavior.cs`
- Modify: `Source/SeljukEmpire/SeljukSubModule.cs:61` (insert a new `TryRegister` line immediately
  after the existing `"NewKingdomsDialogueBehavior"` line)

**Interfaces:**
- Produces: `CompanionPersonalityDialogueBehavior` class with a `RegisterEvents()`/`OnSessionLaunched(CampaignGameStarter)` shape identical to `SeljukDialogueBehavior`, and a private static `GetGreetingVariant(int salt, int variantCount)` (`((int)CampaignTime.Now.ToHours + salt) % variantCount`, copied from `SeljukDialogueBehavior.cs:37-40` — this mod's established convention is one private copy per file, not a shared helper).
- Consumes: nothing from other tasks.

- [ ] **Step 1: Read the reference pattern and the 14 companions' existing backstory content**

Read `Source/SeljukEmpire/Immersion/SeljukDialogueBehavior.cs` in full (the pattern to mirror) and
`ModuleData/rival_culture_companion_backstories.xml` for the 14 named companions' `prebackstory`/
`backstory_a` entries (their documented personality/profession — the source each greeting line must
trace back to).

- [ ] **Step 2: Create the file with the class shell and helper**

```csharp
using TaleWorlds.CampaignSystem;

namespace SeljukEmpire.Immersion
{
    /// <summary>
    /// Gives all 25 recruitable companions 3 alternative repeated-conversation greetings each,
    /// reflecting each one's documented profession/personality - same rotation mechanism as
    /// SeljukDialogueBehavior (see its GetGreetingVariant remarks). Companions already in the
    /// player's party route through the same "start"-&gt;"lord_start" flow as any other Hero
    /// conversation (confirmed by decompiling LordConversationsCampaignBehavior - the only
    /// companion-specific branch is the one-time "meet in main party" line, not the repeated path).
    /// </summary>
    public class CompanionPersonalityDialogueBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Transient dialog behavior
        }

        private static int GetGreetingVariant(int salt, int variantCount)
        {
            return ((int)CampaignTime.Now.ToHours + salt) % variantCount;
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                // Named historical companions (salts 47-60) go here in Step 3.
                // Generic Seljuk wanderers (salts 61-71) go here in Task 2.
            }
            catch (System.Exception)
            {
                // Matches this mod's established defensive pattern for Immersion behaviors.
            }
        }
    }
}
```

- [ ] **Step 3: Add the 14 named companions' greeting blocks**

For each of the 14 StringIds listed above, add an `IsX()` predicate and 3 `AddDialogLine` calls,
following `SeljukDialogueBehavior.cs`'s exact call shape:

```csharp
bool IsKhayyam() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "spc_wanderer_seljuk_khayyam";

starter.AddDialogLine("companion_khayyam_greeting_1", "start", "lord_pretalk",
    "{=companion_khayyam_greet_1}Back so soon? I was in the middle of a calculation the tavern-keeper will never understand. What troubles you now?",
    () => IsKhayyam() && GetGreetingVariant(48, 3) == 0, null, 200);
starter.AddDialogLine("companion_khayyam_greeting_2", "start", "lord_pretalk",
    "{=companion_khayyam_greet_2}The stars keep their own counsel, {PLAYER.NAME}, same as I do. What is it you actually want to ask me?",
    () => IsKhayyam() && GetGreetingVariant(48, 3) == 1, null, 200);
starter.AddDialogLine("companion_khayyam_greeting_3", "start", "lord_pretalk",
    "{=companion_khayyam_greet_3}A wise man doubts twice before he answers once. Go on, then - ask, and let me doubt.",
    () => IsKhayyam() && GetGreetingVariant(48, 3) == 2, null, 200);
```

Write the equivalent 3-line block for the other 13 named companions, each reflecting their own
documented trait from Step 1's reading: Khusraw (wandering poet-scout, reflective/questioning),
Psellos (court philosopher, verbally elaborate/charming), Roussel (Norman mercenary captain, blunt
and martial), Gazali (ascetic theologian, measured/moralizing), Usama (warrior-poet, proud of both
sword and verse), Petritsi (philosopher-monk, gentle and searching), Vardan (rebel mountain lord,
defiant/terse), Peter the Hermit (preacher, fervent), Bartholomew (pilgrim-visionary, awed/urgent),
Matthew of Edessa (historian-monk, precise and archival), Heratsi (physician, clinical but caring),
Kashgari (linguist-traveler, curious about the player's own origin), Yasawi (mystic, calm and
aphoristic). Use the next sequential salt for each (49 through 60, in the roster order above).

- [ ] **Step 4: Register the behavior**

In `Source/SeljukEmpire/SeljukSubModule.cs`, immediately after line 61
(`TryRegister(campaignStarter, "NewKingdomsDialogueBehavior", ...)`), insert:

```csharp
TryRegister(campaignStarter, "CompanionPersonalityDialogueBehavior", () => campaignStarter.AddBehavior(new CompanionPersonalityDialogueBehavior()));
```

- [ ] **Step 5: Build and verify**

```bash
dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release
python tools/verify_mod.py
```

Expected: build succeeds with 0 errors; `verify_mod.py` reports 0 errors (localization coverage for
the 14 new `{=companion_*_greet_N}` keys in EN+TR, and `dialogue-hero-ids` check 17 confirming all 14
StringIds are real).

- [ ] **Step 6: Commit**

```bash
git add Source/SeljukEmpire/Immersion/CompanionPersonalityDialogueBehavior.cs Source/SeljukEmpire/SeljukSubModule.cs
git commit -m "$(cat <<'EOF'
Add repeated-conversation greetings for the 14 named companions

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: The 11 generic Seljuk wanderers

**Files:**
- Modify: `Source/SeljukEmpire/Immersion/CompanionPersonalityDialogueBehavior.cs` (fills the Task 1
  Step 2 placeholder comment for generic wanderers)

**Interfaces:**
- Consumes: the class shell and `GetGreetingVariant` helper from Task 1.

- [ ] **Step 1: Read the 11 wanderers' existing backstory content**

Read `ModuleData/seljuk_special_characters_backstories.xml`'s entries for `spc_wc2_0` through
`spc_wc2_10` (the established persona each greeting must reflect — see the roster list above for
which name/persona maps to which index).

- [ ] **Step 2: Add the 11 generic wanderers' greeting blocks**

Same 3-variant pattern as Task 1 Step 3, salts 61 through 71 in index order 0 through 10. Example for
index 3 (Demirgöz, marksman):

```csharp
bool IsWanderer3() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "spc_wanderer_seljuk_3";

starter.AddDialogLine("companion_wanderer3_greeting_1", "start", "lord_pretalk",
    "{=companion_wanderer3_greet_1}Still breathing, I see. Good - means you haven't needed me yet.",
    () => IsWanderer3() && GetGreetingVariant(64, 3) == 0, null, 200);
starter.AddDialogLine("companion_wanderer3_greeting_2", "start", "lord_pretalk",
    "{=companion_wanderer3_greet_2}Keep your voice down. A man who talks too much in camp dies quietly later.",
    () => IsWanderer3() && GetGreetingVariant(64, 3) == 1, null, 200);
starter.AddDialogLine("companion_wanderer3_greeting_3", "start", "lord_pretalk",
    "{=companion_wanderer3_greet_3}I've been counting arrows. We have enough. For now.",
    () => IsWanderer3() && GetGreetingVariant(64, 3) == 2, null, 200);
```

Write the equivalent for the other 10 indices, each matching its established persona: Bilge
(reflective, half-riddling), Şahin (watchful, terse), Babasız (guarded, testing the player's
trustworthiness), Dışlanmış (bitter about the past, guarded), Deli (volatile, unpredictable warmth
under the edge), Boz Şahin (world-weary veteran, dry humor), Dişi Kurt (proud, no patience for
condescension), Yalnız (spare, uncomfortable with too much talk), Çevik (quick, restless, always
half-watching the door), Perişan (humble, grateful, easily moved).

- [ ] **Step 3: Build and verify**

```bash
dotnet build Source/SeljukEmpire/SeljukTactics.csproj -c Release
python tools/verify_mod.py
```

Expected: 0 build errors, `verify_mod.py` 0 errors (all 25 companions now covered by both
localization coverage and check 17).

- [ ] **Step 4: Commit**

```bash
git add Source/SeljukEmpire/Immersion/CompanionPersonalityDialogueBehavior.cs
git commit -m "$(cat <<'EOF'
Add repeated-conversation greetings for the 11 generic wanderers

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Full verification pass

**Files:** none (verification only)

**Interfaces:** none.

- [ ] **Step 1: Run the full automated check suite**

```bash
python tools/run_all_checks.py
```

Expected: `dotnet test` passes (unaffected — this feature adds no engine-independent logic, so no new
unit tests are expected), `verify_mod.py` passes 0 errors/0 warnings.

- [ ] **Step 2: Manual playtest verification**

Deploy the rebuilt DLL to the local game install (`bin/`, matching this mod's existing deploy
convention), start or load a campaign with at least 2-3 recruited companions from different cultures
(e.g. one named historical companion and one generic wanderer), talk to each companion multiple times
across different in-game hours, and confirm the greeting line visibly changes rather than repeating
the same line verbatim every time.

- [ ] **Step 3: Final commit if any fixes were needed**

If Step 2 surfaces any issue (wrong StringId, a line that reads oddly, a variant that never seems to
trigger), fix it directly in `CompanionPersonalityDialogueBehavior.cs`, re-run Step 1, and commit:

```bash
git add Source/SeljukEmpire/Immersion/CompanionPersonalityDialogueBehavior.cs
git commit -m "$(cat <<'EOF'
Fix companion greeting issue found in manual playtest

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```
