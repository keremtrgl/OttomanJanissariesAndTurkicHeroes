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
    /// Seljuk culture passive bonuses/debuffs, implemented as GameModel overrides. Native Bannerlord
    /// culture bonuses (e.g. Vlandia's cheaper crossbows) are not XML-configurable; they're C#
    /// GameModel overrides that branch on Hero/CharacterObject/Settlement culture, matching the
    /// wrap/override pattern already used by SeljukVolunteerModel.cs.
    ///
    /// - SeljukWageModel: Seljuk-culture mounted troops cost 10% less wage (Iqta cavalry economy).
    /// - SeljukConstructionSpeedModel: Seljuk settlements build 10% faster (Nizamiye public works).
    /// - SeljukSiegeEngineeringModel: siege engines (rams, towers, trebuchets) built by a
    ///   Seljuk-culture side -- attacker camp or defending settlement -- construct 15% slower (weak
    ///   siege engineering tradition). This is the actual native hook for in-siege engine
    ///   construction speed (SiegeEventModel.GetConstructionProgressPerHour), kept as a separate
    ///   model from SeljukConstructionSpeedModel because they are genuinely different game systems
    ///   (see deviation notes below).
    /// - SeljukCaravanTradeModel: Seljuk-culture-owned caravans (AI and player) earn 15% more trade
    ///   profit (PartyTradeModel.GetTradePenaltyFactor).
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

        public override int GetCharacterWage(CharacterObject character)
        {
            int baseWage = base.GetCharacterWage(character);
            if (character != null && character.Culture != null && character.Culture.StringId == SeljukCultureId && character.IsMounted)
            {
                // -10% wage for Seljuk-culture mounted troops (Iqta cavalry economy)
                return Math.Max(1, (int)Math.Round(baseWage * 0.90f));
            }
            return baseWage;
        }
    }

    public class SeljukConstructionSpeedModel : DefaultBuildingConstructionModel
    {
        private const string SeljukCultureId = "seljuk";

        public override ExplainedNumber CalculateDailyConstructionPower(Town town, bool includeDescriptions = false)
        {
            ExplainedNumber result = base.CalculateDailyConstructionPower(town, includeDescriptions);

            if (town != null && town.Culture != null && town.Culture.StringId == SeljukCultureId)
            {
                // +10% build speed for Seljuk town/castle construction (Nizamiye public works)
                result.AddFactor(0.10f, new TextObject("{=seljuk_bonus_construction}Nizamiye Public Works"));
            }

            return result;
        }
    }

    public class SeljukSiegeEngineeringModel : DefaultSiegeEventModel
    {
        private const string SeljukCultureId = "seljuk";

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

            if (sideCulture != null && sideCulture.StringId == SeljukCultureId)
            {
                // -15% siege engine construction speed for the Seljuk-culture side (attacker or
                // defender) of the siege -- weak siege engineering tradition
                return baseProgress * 0.85f;
            }

            return baseProgress;
        }
    }

    public class SeljukCaravanTradeModel : DefaultPartyTradeModel
    {
        private const string SeljukCultureId = "seljuk";

        public override float GetTradePenaltyFactor(MobileParty party)
        {
            float baseFactor = base.GetTradePenaltyFactor(party);

            if (party != null && party.IsCaravan)
            {
                Hero owner = party.CaravanPartyComponent?.Owner;
                if (owner != null && owner.Clan != null && owner.Clan.Culture != null && owner.Clan.Culture.StringId == SeljukCultureId)
                {
                    // +15% caravan trade profit for Seljuk-culture-owned caravans (AI and player)
                    return baseFactor * 1.15f;
                }
            }

            return baseFactor;
        }
    }
}
