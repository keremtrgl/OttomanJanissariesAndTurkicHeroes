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
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
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
        private bool _isCharacterCreationHooked;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);

            try
            {
                if (GameStateManager.Current?.ActiveState is CharacterCreationState ccState)
                {
                    if (!_isCharacterCreationHooked && ccState.CharacterCreationManager != null)
                    {
                        var handler = new SeljukCharacterCreationContentHandler();
                        ccState.CharacterCreationManager.RegisterCharacterCreationContentHandler(handler, 100);
                        handler.InjectSeljukNarratives(ccState.CharacterCreationManager);

                        var rivalHandler = new RivalCultureCharacterCreationContentHandler();
                        ccState.CharacterCreationManager.RegisterCharacterCreationContentHandler(rivalHandler, 100);
                        rivalHandler.InjectRivalNarratives(ccState.CharacterCreationManager);

                        _isCharacterCreationHooked = true;
                    }
                }
                else
                {
                    _isCharacterCreationHooked = false;
                }
            }
            catch (Exception)
            {
                // Fail-safe protection
            }
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            base.InitializeGameStarter(game, starterObject);

            if (starterObject is CampaignGameStarter campaignStarter)
            {
                try
                {
                    // 1. Register Seljuk Territorial Ownership, Clan Hierarchy & City Initialization Behavior
                    campaignStarter.AddBehavior(new SeljukSettlementBehavior());
                }
                catch (Exception) { }

                try
                {
                    // 2. Register Seljuk Economy & Caravan Insurance Campaign Behavior
                    campaignStarter.AddBehavior(new SeljukCaravanInsuranceBehavior());
                }
                catch (Exception) { }

                try
                {
                    // 3. Register Historical Seljuk Atabeg & Ikta Governance System
                    campaignStarter.AddBehavior(new SeljukAtabegTitleBehavior());
                }
                catch (Exception) { }

                try
                {
                    // 4. Register Seljuk Festival & Tournament Mastercraft Rewards System
                    campaignStarter.AddBehavior(new SeljukTournamentRewardBehavior());
                }
                catch (Exception) { }

                try
                {
                    // 5. Register Seljuk Direct Village & Town Recruitment Behavior
                    campaignStarter.AddBehavior(new SeljukRecruitmentBehavior());
                }
                catch (Exception) { }

                try
                {
                    // 6. Register Seljuk Tavern Bard (Ozan) Ghazavat Epic Morale System
                    campaignStarter.AddBehavior(new SeljukTavernBehavior());
                }
                catch (Exception) { }

                try
                {
                    // 7. Register Seljuk Historical Dialogue & Greetings System
                    campaignStarter.AddBehavior(new SeljukDialogueBehavior());
                }
                catch (Exception) { }

                try
                {
                    // 8. Register Seljuk Culture Passive Bonus/Debuff Models
                    //    (-10% mounted-troop wage, +10% construction speed, -15% siege engine speed,
                    //     +15% caravan trade profit)
                    campaignStarter.AddModel(new SeljukWageModel());
                    campaignStarter.AddModel(new SeljukConstructionSpeedModel());
                    campaignStarter.AddModel(new SeljukSiegeEngineeringModel());
                    campaignStarter.AddModel(new SeljukCaravanTradeModel());
                }
                catch (Exception) { }

                try
                {
                    // 9. Register one-time Starter Pack grant (gold + gear). Must be a
                    //    CampaignBehaviorBase with SyncData, not a SubModule-level flag: see
                    //    SeljukStarterPackBehavior's summary for why.
                    campaignStarter.AddBehavior(new SeljukStarterPackBehavior());
                }
                catch (Exception) { }
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
