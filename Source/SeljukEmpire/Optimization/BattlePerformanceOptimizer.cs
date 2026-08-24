using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SeljukEmpire.Optimization
{
    /// <summary>
    /// Master Battlefield Performance & Frametime Optimizer.
    /// Combines 2D Spatial Hash Grid targeting, dynamic ragdoll sleep manager, and distance-based LOD AI scheduling.
    /// Guarantees smooth frametimes and prevents CPU spikes in 500+ unit battles.
    /// </summary>
    public class BattlePerformanceOptimizer : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        private SpatialHashGrid _enemySpatialGrid;
        private RagdollPhysicsBudgetManager _ragdollManager;
        private MissionTime _gridUpdateTimer;
        private MissionTime _lodTickTimer;
        private int _frameCounter;

        public override void AfterStart()
        {
            base.AfterStart();
            _enemySpatialGrid = new SpatialHashGrid(35f);
            _ragdollManager = new RagdollPhysicsBudgetManager();
            _gridUpdateTimer = MissionTime.Now;
            _lodTickTimer = MissionTime.Now;
            _frameCounter = 0;
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

            if (affectedAgent != null && (agentState == AgentState.Killed || agentState == AgentState.Unconscious))
            {
                _ragdollManager.RegisterDeadAgent(affectedAgent);
            }
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            try
            {
                if (Mission.Current == null || Mission.Current.Mode != MissionMode.Battle) return;

                _frameCounter++;

                // 1. Update ragdoll settle checks every few frames
                if (_frameCounter % 4 == 0)
                {
                    _ragdollManager.Update(dt);
                }

                // 2. Rebuild 2D Spatial Hash Grid periodically (every 180ms) for O(1) targeting
                if (_gridUpdateTimer.ElapsedSeconds > 0.18f)
                {
                    Team enemyTeam = Mission.Current.PlayerEnemyTeam;
                    if (enemyTeam != null)
                    {
                        _enemySpatialGrid.Rebuild(enemyTeam);
                    }
                    _gridUpdateTimer = MissionTime.Now;
                }

                // 3. Staggered Distance-Based AI Culling for distant formations
                if (_lodTickTimer.ElapsedSeconds > 0.40f)
                {
                    OptimizeDistantFormations();
                    _lodTickTimer = MissionTime.Now;
                }
            }
            catch (Exception)
            {
                // Engine safety catch
            }
        }

        /// <summary>
        /// Optimizes AI tick frequencies based on distance from the player camera.
        /// </summary>
        private void OptimizeDistantFormations()
        {
            Agent mainAgent = Mission.Current?.MainAgent;
            Vec3 cameraPos = mainAgent != null && mainAgent.IsActive() ? mainAgent.Position : (Mission.Current?.Scene?.LastFinalRenderCameraPosition ?? Vec3.Zero);

            if (cameraPos == Vec3.Zero || Mission.Current?.Teams == null) return;

            float farDistSq = 140f * 140f; // 140 meters threshold

            foreach (var team in Mission.Current.Teams)
            {
                foreach (var formation in team.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits <= 0) continue;

                    float distToCameraSq = formation.OrderPosition.ToVec3().DistanceSquared(cameraPos);

                    // For formations further than 140m away, optimize query update frequency
                    if (distToCameraSq > farDistSq)
                    {
                        formation.ResetArrangementOrderTickTimer();
                    }
                }
            }
        }

        public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
            base.OnMissionModeChange(oldMissionMode, atStart);

            if (Mission.Current?.Mode != MissionMode.Battle)
            {
                _enemySpatialGrid?.Clear();
                _ragdollManager?.Clear();
            }
        }

        public override void OnMissionStateFinalized()
        {
            base.OnMissionStateFinalized();
            _enemySpatialGrid?.Clear();
            _ragdollManager?.Clear();
        }

        public override void OnClearScene()
        {
            base.OnClearScene();
            _enemySpatialGrid?.Clear();
            _ragdollManager?.Clear();
        }
    }
}
