# Companion personality dialogue variation

Date: 2026-09-14

## Context

The mod has 25 recruitable tavern companions: 14 named historical figures (`rival_culture_companions.xml`,
2 per rival culture, each with a documented real biography) and 11 generic Seljuk/Turkmen wanderer
archetypes (`seljuk_special_characters.xml`, `spc_wanderer_seljuk_0`..`_10`, each with an established
persona like "Demirgöz" the marksman or "Deli" the berserker). All 25 already have full, genuinely
distinct `GameText` backstory content (`prebackstory`/`backstory_a-d`/`response_1-2`/`generic_backstory`)
from earlier sessions — confirmed non-templated by this session's audit.

What's missing: once a companion is recruited and sits in the player's party, talking to them again
shows Native's generic repeated-conversation line, not anything reflecting who they are. The existing
`SeljukDialogueBehavior.cs` already solved an equivalent problem for 3 Seljuk clan leaders — repeated
greetings that vary across visits instead of a single line verbatim every time.

## Decompiled confirmation (verified, not assumed)

Decompiling `TaleWorlds.CampaignSystem.CampaignBehaviors.LordConversationsCampaignBehavior`
(`TaleWorlds.CampaignSystem.dll`, `ilspycmd`) confirms companions already in the player's own party
route through the **same** `"start"` input token and the **same** `"lord_start"` output token as any
other Hero conversation — the only companion-specific branch is
`lord_in_main_party_meet_player_response` (line 233), which only fires for the very first meeting
(`lord_meet_in_main_party_player_response`). Every subsequent conversation falls through to the same
`start_default`/`start_default_under_24_hours` → `lord_start` path used for lords met on the campaign
map. This means the exact mechanism `SeljukDialogueBehavior.cs` already uses —
`AddDialogLine(..., "start", "lord_pretalk", ..., () => IsX() && GetGreetingVariant(salt, 3) == n, ...)`
— applies to companions with zero new engine hooks. This is a data/content extension of a proven
pattern, not new dialogue-system territory.

## Design

**New file**: `Source/SeljukEmpire/Immersion/CompanionPersonalityDialogueBehavior.cs`, a
`CampaignBehaviorBase` registered in `SeljukSubModule.cs` alongside the existing Immersion behaviors.
Kept separate from `SeljukDialogueBehavior.cs` (Seljuk clan leaders only) since companions span all 8
cultures.

**Per companion**: 3 greeting-variant `AddDialogLine` calls on `"start"→"lord_pretalk"`, gated by
`Hero.OneToOneConversationHero.StringId == "<companion_id>"` and `GetGreetingVariant(salt, 3) == n`
(salt = a unique integer per companion, continuing past whatever range `SeljukDialogueBehavior`,
`RivalCultureDialogueBehavior`, and `NewKingdomsDialogueBehavior` already occupy — the exact starting
salt is an implementation-time detail, resolved by reading their current salt usage before writing new
lines, not decided here). `GetGreetingVariant` itself is not duplicated — either called via a shared
static helper or the existing private implementation's logic is replicated identically (single-line
formula, no meaningful duplication risk either way; the implementation step picks whichever keeps the
file dependency graph cleanest).

**Content**: 25 companions × 3 lines = 75 new greeting lines, each written to reflect that specific
companion's documented profession and temperament, e.g.:
- Ömer Hayyam (mathematician-astronomer) — skeptical, questioning register
- Gazali (theologian-ascetic) — measured, moralizing register
- Usame bin Münkız (warrior-poet) — martial pride laced with verse
- Kaşgarlı Mahmud (linguist-traveler) — curious about the player's own origins/dialect
- The 11 generic wanderers keep the tone their existing backstory already established (e.g.
  "Demirgöz" stays terse and marksman-minded, "Deli" stays volatile)

Exact wording for all 25 is written during implementation, not enumerated in this spec — the
constraint is that each line must be traceable to something already documented in that companion's
existing backstory content, not invented personality unconnected to it.

**Localization**: EN + TR only for this pass, following this mod's own documented norm (README:
translating a new key into the other 6 languages in a follow-up commit is normal workflow, not
required in the same commit). `verify_mod.py`'s `check_localization_coverage` will flag EN/TR gaps as
ERROR and the other 6 as WARN, exactly as it does for all existing content.

## Testing

This is pure C# `AddDialogLine` wiring plus GameText string content — no engine-independent logic to
unit test (unlike `TacticalSituationAssessor`). Verification is: `dotnet build` succeeds,
`verify_mod.py` (specifically localization coverage and dialogue-hero-ids, check 17) passes clean, and
a manual playtest talking to a few recruited companions confirms variant rotation actually differs
across in-game hours.

## Out of scope

- The one-time recruit/backstory `GameText` categories (`prebackstory` etc.) — unchanged.
- Any new companion content (traits, skills, equipment) — this is dialogue only.
- The other 6 non-required languages — left for a normal follow-up commit per existing convention.
