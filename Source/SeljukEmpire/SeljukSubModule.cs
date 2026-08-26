using System;
using SeljukEmpire.Administration;
using SeljukEmpire.CharacterCreation;
using SeljukEmpire.Culture;
using SeljukEmpire.Economy;
using SeljukEmpire.Immersion;
using SeljukEmpire.Optimization;
using SeljukEmpire.Recruitment;
using SeljukEmpire.Settlements;
using SeljukEmpire.Tactics;
using SeljukEmpire.Tournaments;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SeljukEmpire
{
    /// <summary>
    /// Master entry point for Seljuk Empire: Sword of Islam mod.
    /// Manages sub-module lifecycle, territorial initialization, economy/insurance campaign behaviors,
    /// dynamic village & town recruitment, tournament prize enhancements, Atabeg governance, tactical AI, and battlefield performance optimizers.
    /// </summary>
    public class SeljukSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            base.InitializeGameStarter(game, starterObject);

            if (starterObject is CampaignGameStarter campaignStarter)
            {
                // Each registration below was individually swallowed with a bare `catch (Exception)
                // {}` and no diagnostics - unlike the mod's many per-tick defensive catches (which
                // guard against transient engine-state edge cases and are fine), a throw HERE happens
                // once at startup and far more likely means an actual code defect than an engine
                // hiccup. Previously, if any one behavior failed to register, that entire feature
                // silently vanished for the whole campaign with no crash and no way to learn why.
                TryRegister(campaignStarter, "SeljukSettlementBehavior", () => campaignStarter.AddBehavior(new SeljukSettlementBehavior()));
                TryRegister(campaignStarter, "SeljukCaravanInsuranceBehavior", () => campaignStarter.AddBehavior(new SeljukCaravanInsuranceBehavior()));
                TryRegister(campaignStarter, "SeljukAtabegTitleBehavior", () => campaignStarter.AddBehavior(new SeljukAtabegTitleBehavior()));
                TryRegister(campaignStarter, "SeljukTournamentRewardBehavior", () => campaignStarter.AddBehavior(new SeljukTournamentRewardBehavior()));
                TryRegister(campaignStarter, "SeljukRecruitmentBehavior", () => campaignStarter.AddBehavior(new SeljukRecruitmentBehavior()));
                TryRegister(campaignStarter, "SeljukTavernBehavior", () => campaignStarter.AddBehavior(new SeljukTavernBehavior()));
                TryRegister(campaignStarter, "SeljukDialogueBehavior", () => campaignStarter.AddBehavior(new SeljukDialogueBehavior()));
                TryRegister(campaignStarter, "RivalCultureDialogueBehavior", () => campaignStarter.AddBehavior(new RivalCultureDialogueBehavior()));
                TryRegister(campaignStarter, "SeljukSystemsExplainerBehavior", () => campaignStarter.AddBehavior(new SeljukSystemsExplainerBehavior()));
                TryRegister(campaignStarter, "SeljukCultureBonusModels", () =>
                {
                    // -10% mounted-troop wage, +10% construction speed, -15% siege engine speed, +15% caravan trade profit
                    campaignStarter.AddModel(new SeljukWageModel());
                    campaignStarter.AddModel(new SeljukConstructionSpeedModel());
                    campaignStarter.AddModel(new SeljukSiegeEngineeringModel());
                    campaignStarter.AddModel(new SeljukCaravanTradeModel());
                });
                // One-time Starter Pack grant (gold + gear). Must be a CampaignBehaviorBase with
                // SyncData, not a SubModule-level flag: see SeljukStarterPackBehavior's summary for why.
                TryRegister(campaignStarter, "SeljukStarterPackBehavior", () => campaignStarter.AddBehavior(new SeljukStarterPackBehavior()));
                // Seljuk & rival-culture (Byzantine/Abbasid/Georgian) character creation narrative
                // content. Must go through CampaignBehaviorBase.RegisterEvents
                // (CampaignEvents.OnCharacterCreationInitializedEvent), not a SubModule tick poll -
                // see each handler's class remarks for why the previous approach never actually
                // showed any custom content in character creation.
                TryRegister(campaignStarter, "SeljukCharacterCreationContentHandler", () => campaignStarter.AddBehavior(new SeljukCharacterCreationContentHandler()));
                TryRegister(campaignStarter, "RivalCultureCharacterCreationContentHandler", () => campaignStarter.AddBehavior(new RivalCultureCharacterCreationContentHandler()));
            }
        }

        private static void TryRegister(CampaignGameStarter campaignStarter, string name, Action register)
        {
            try
            {
                register();
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Seljuk Empire] Failed to register {name}: {ex.Message}", Colors.Red));
            }
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);

            try
            {
                // Register Multi-Doctrine Combat AI & Battle Performance Optimizer
                if (mission.Mode == MissionMode.Battle)
                {
                    // Active in all field & siege engagements to smooth frametimes and enforce ragdoll budgets
                    mission.AddMissionBehavior(new BattlePerformanceOptimizer());

                    // Multi-doctrine dynamic maneuvers active in open field battles
                    if (!mission.IsSiegeBattle)
                    {
                        mission.AddMissionBehavior(new TuranTacticMissionBehavior());
                    }
                }
            }
            catch (Exception)
            {
                // Mission initialization safety
            }
        }
    }
}
