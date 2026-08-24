using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
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
    /// - SeljukConstructionSpeedModel: Seljuk settlements build ordinary (non-military) projects 10%
    ///   faster (Nizamiye public works), but military/defensive projects (walls, towers, and other
    ///   IsMilitaryProject-flagged construction closest to native "siege engineering" work) build 15%
    ///   slower (weak siege engineering tradition). Both live in the same override for a single
    ///   source of truth, as the design intends.
    ///
    /// Deviation note: the design brief's guessed signatures assumed
    /// DefaultPartyWageModel.GetCharacterWage(CharacterObject, bool) returning ExplainedNumber, and a
    /// DefaultSettlementBuildingModel.CalculateBuildingProgressChange(Settlement, Building, ...)
    /// override. Neither exists in the actual TaleWorlds.CampaignSystem API (verified via
    /// System.Reflection.MetadataLoadContext against the installed game DLLs, since no
    /// IDE/decompiler was available). The real signatures are:
    ///   - PartyWageModel.GetCharacterWage(CharacterObject character) -> int (no includeDescriptions,
    ///     no ExplainedNumber).
    ///   - There is no SettlementBuildingModel/DefaultSettlementBuildingModel at all. The real
    ///     construction-speed model is BuildingConstructionModel/DefaultBuildingConstructionModel,
    ///     operating per-Town (not per-Building) via
    ///     CalculateDailyConstructionPower(Town town, bool includeDescriptions = false) -> ExplainedNumber.
    ///   - Siege engine construction during an active siege is not model-driven at all (it's hardcoded,
    ///     non-virtual, in SiegeEvent.ConstructionTick()), so there is no GameModel override slot for it.
    ///     The debuff is instead expressed via BuildingType.IsMilitaryProject (a real public field
    ///     distinguishing military/defensive construction from economic construction) within the same
    ///     CalculateDailyConstructionPower override, preserving the "single source of truth" structure
    ///     the brief asked for while using an API surface that actually exists.
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
                bool isMilitaryProject = town.CurrentBuilding != null
                    && town.CurrentBuilding.BuildingType != null
                    && town.CurrentBuilding.BuildingType.IsMilitaryProject;

                if (isMilitaryProject)
                {
                    // -15% build speed on military/defensive (siege-related) construction
                    result.AddFactor(-0.15f, new TextObject("{=seljuk_debuff_siege_engineering}Weak Siege Engineering Tradition"));
                }
                else
                {
                    // +10% build speed on ordinary town/castle construction
                    result.AddFactor(0.10f, new TextObject("{=seljuk_bonus_construction}Nizamiye Public Works"));
                }
            }

            return result;
        }
    }
}
