using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace SeljukEmpire.Culture
{
    /// <summary>
    /// Culture passive bonuses/debuffs for all four of this mod's custom-content cultures
    /// (Seljuk/empire[Byzantine]/aserai[Abbasid]/sturgia[Georgian]), implemented as GameModel
    /// overrides. Native Bannerlord culture bonuses (e.g. Vlandia's cheaper crossbows) are not
    /// XML-configurable; they're C# GameModel overrides that branch on Hero/CharacterObject/
    /// Settlement culture, matching the wrap/override pattern used throughout this mod's GameModel
    /// overrides.
    ///
    /// Bannerlord only lets one GameModel instance be registered per model type (SeljukSubModule
    /// calls campaignStarter.AddModel(...) once per type; a second AddModel of the same base type
    /// would silently replace the first, not stack with it) - so all four cultures' bonuses live as
    /// branches inside these same four model classes rather than one class per culture.
    ///
    /// - SeljukWageModel: Seljuk-culture mounted troops cost 10% less wage (Iqta cavalry economy);
    ///   Abbasid-culture mounted troops also cost 10% less (ghulam cavalry economy); Georgian-culture
    ///   mounted troops also cost 10% less (Aznauri/Didebuli/Eristavi noble cavalry economy).
    /// - SeljukConstructionSpeedModel: Seljuk settlements build 10% faster (Nizamiye public works);
    ///   Abbasid settlements also build 10% faster (Baghdad's House of Wisdom architectural
    ///   golden age); Georgian settlements also build 10% faster (David IV "the Builder"'s
    ///   fortress and church construction program, 1089-1125 - this mod's own Georgian era).
    /// - SeljukSiegeEngineeringModel: siege engines (rams, towers, trebuchets) built by a
    ///   Seljuk-culture side -- attacker camp or defending settlement -- construct 15% slower (weak
    ///   siege engineering tradition); a Byzantine-culture (empire) side instead constructs 15%
    ///   FASTER (the historically well-documented Byzantine engineering/Greek-fire tradition -
    ///   the one culture bonus here that is a bonus rather than a debuff, mirroring history rather
    ///   than needing symmetry with Seljuk). This is the actual native hook for in-siege engine
    ///   construction speed (SiegeEventModel.GetConstructionProgressPerHour), kept as a separate
    ///   model from SeljukConstructionSpeedModel because they are genuinely different game systems
    ///   (see deviation notes below).
    /// - SeljukCaravanTradeModel: Seljuk-culture-owned caravans (AI and player) earn 15% more trade
    ///   profit (PartyTradeModel.GetTradePenaltyFactor); Byzantine-culture caravans earn 10% more
    ///   (Constantinople's sophisticated tax/mercantile bureaucracy); Abbasid-culture caravans earn
    ///   15% more (Baghdad's position astride the Silk Road, historically the wealthiest trade hub
    ///   of this era).
    ///
    /// Deviation notes (all verified via System.Reflection.MetadataLoadContext against the
    /// installed game DLLs, since no IDE/decompiler was available in this environment -- see the
    /// task-4-report.md fix-round entry for the full investigation):
    ///   - PartyWageModel.GetCharacterWage(CharacterObject character) -> int (no includeDescriptions
    ///     parameter, no ExplainedNumber return -- the brief's guessed signature was wrong).
    ///   - There is no SettlementBuildingModel/DefaultSettlementBuildingModel at all. Ordinary
    ///     settlement construction speed is BuildingConstructionModel/DefaultBuildingConstructionModel,
    ///     operating per-Town via CalculateDailyConstructionPower(Town town, bool includeDescriptions
    ///     = false) -> ExplainedNumber. This governs ALL town construction (walls included) as one
    ///     aggregate daily power number, so it cannot itself distinguish "siege engine" work.
    ///   - Siege engine construction during an active siege IS a real GameModel override:
    ///     SiegeEventModel (abstract) / DefaultSiegeEventModel (concrete), with
    ///     GetConstructionProgressPerHour(SiegeEngineType type, SiegeEvent siegeEvent, ISiegeEventSide
    ///     side) -> float. ISiegeEventSide is implemented by both BesiegerCamp (attacker) and
    ///     Settlement (defender), so a side's culture is read via
    ///     ((BesiegerCamp)side).MapFaction?.Culture or ((Settlement)side).Culture respectively.
    ///   - PartyTradeModel.GetTradePenaltyFactor(MobileParty party) -> float exists and was confirmed
    ///     safe to use by disassembling its compiled IL body directly (MetadataLoadContext exposes
    ///     GetMethodBody()/GetILAsByteArray() even though it cannot resolve member tokens): the method
    ///     builds `new ExplainedNumber(1.0f, false, null)`, applies penalty factors to it, then
    ///     returns `1.0f / result.ResultNumber`. Because the accumulator starts at 1.0 and grows as
    ///     penalty conditions worsen, the reciprocal shrinks as the penalty worsens -- so a *higher*
    ///     returned value means *less* penalty (more profit), confirming multiplying by 1.15f is the
    ///     correct direction for a buff, not a nerf.
    /// </summary>
    public class SeljukWageModel : DefaultPartyWageModel
    {
        private const string SeljukCultureId = "seljuk";
        private const string AbbasidCultureId = "aserai";
        private const string GeorgianCultureId = "sturgia";

        public override int GetCharacterWage(CharacterObject character)
        {
            int baseWage = base.GetCharacterWage(character);
            if (character == null || character.Culture == null || !character.IsMounted)
            {
                return baseWage;
            }

            string cultureId = character.Culture.StringId;
            if (cultureId == SeljukCultureId || cultureId == AbbasidCultureId || cultureId == GeorgianCultureId)
            {
                // -10% wage for mounted troops of Seljuk (Iqta cavalry economy), Abbasid (ghulam
                // cavalry economy), and Georgian (Aznauri/Didebuli/Eristavi noble cavalry economy)
                return Math.Max(1, (int)Math.Round(baseWage * 0.90f));
            }
            return baseWage;
        }
    }

    public class SeljukConstructionSpeedModel : DefaultBuildingConstructionModel
    {
        private const string SeljukCultureId = "seljuk";
        private const string AbbasidCultureId = "aserai";
        private const string GeorgianCultureId = "sturgia";

        public override ExplainedNumber CalculateDailyConstructionPower(Town town, bool includeDescriptions = false)
        {
            ExplainedNumber result = base.CalculateDailyConstructionPower(town, includeDescriptions);

            if (town == null || town.Culture == null)
            {
                return result;
            }

            string cultureId = town.Culture.StringId;
            if (cultureId == SeljukCultureId)
            {
                // +10% build speed for Seljuk town/castle construction (Nizamiye public works)
                result.AddFactor(0.10f, new TextObject("{=seljuk_bonus_construction}Nizamiye Public Works"));
            }
            else if (cultureId == AbbasidCultureId)
            {
                // +10% build speed for Abbasid town/castle construction (House of Wisdom golden age)
                result.AddFactor(0.10f, new TextObject("{=abb_bonus_construction}House of Wisdom"));
            }
            else if (cultureId == GeorgianCultureId)
            {
                // +10% build speed for Georgian town/castle construction (David IV "the Builder"'s
                // own fortress and church program, 1089-1125 - this mod's own Georgian era)
                result.AddFactor(0.10f, new TextObject("{=geo_bonus_construction}David the Builder's Works"));
            }

            return result;
        }
    }

    public class SeljukSiegeEngineeringModel : DefaultSiegeEventModel
    {
        private const string SeljukCultureId = "seljuk";
        private const string ByzantineCultureId = "empire";

        public override float GetConstructionProgressPerHour(SiegeEngineType type, SiegeEvent siegeEvent, ISiegeEventSide side)
        {
            float baseProgress = base.GetConstructionProgressPerHour(type, siegeEvent, side);

            CultureObject sideCulture = null;
            if (side is BesiegerCamp camp)
            {
                sideCulture = camp.MapFaction?.Culture;
            }
            else if (side is Settlement settlement)
            {
                sideCulture = settlement.Culture;
            }

            if (sideCulture == null)
            {
                return baseProgress;
            }

            if (sideCulture.StringId == SeljukCultureId)
            {
                // -15% siege engine construction speed for the Seljuk-culture side (attacker or
                // defender) of the siege -- weak siege engineering tradition
                return baseProgress * 0.85f;
            }
            if (sideCulture.StringId == ByzantineCultureId)
            {
                // +15% siege engine construction speed for the Byzantine-culture side -- the
                // historically well-documented Byzantine engineering tradition (Greek fire,
                // advanced siege craft), a genuine bonus rather than Seljuk's debuff
                return baseProgress * 1.15f;
            }

            return baseProgress;
        }
    }

    public class SeljukCaravanTradeModel : DefaultPartyTradeModel
    {
        private const string SeljukCultureId = "seljuk";
        private const string ByzantineCultureId = "empire";
        private const string AbbasidCultureId = "aserai";

        public override float GetTradePenaltyFactor(MobileParty party)
        {
            float baseFactor = base.GetTradePenaltyFactor(party);

            if (party == null || !party.IsCaravan)
            {
                return baseFactor;
            }

            Hero owner = party.CaravanPartyComponent?.Owner;
            if (owner == null || owner.Clan == null || owner.Clan.Culture == null)
            {
                return baseFactor;
            }

            string cultureId = owner.Clan.Culture.StringId;
            if (cultureId == SeljukCultureId)
            {
                // +15% caravan trade profit for Seljuk-culture-owned caravans (AI and player)
                return baseFactor * 1.15f;
            }
            if (cultureId == ByzantineCultureId)
            {
                // +10% caravan trade profit for Byzantine-culture-owned caravans (Constantinople's
                // sophisticated tax and mercantile bureaucracy)
                return baseFactor * 1.10f;
            }
            if (cultureId == AbbasidCultureId)
            {
                // +15% caravan trade profit for Abbasid-culture-owned caravans (Baghdad's position
                // astride the Silk Road, historically the wealthiest trade hub of this era)
                return baseFactor * 1.15f;
            }

            return baseFactor;
        }
    }
}
