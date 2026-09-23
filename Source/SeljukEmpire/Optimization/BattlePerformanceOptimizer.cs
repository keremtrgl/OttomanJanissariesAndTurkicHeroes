using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SeljukEmpire.Optimization
{
    /// <summary>
    /// Battlefield frametime helper: keeps the number of simultaneously simulated corpse ragdolls
    /// bounded in large battles (see <see cref="RagdollPhysicsBudgetManager"/>).
    /// </summary>
    /// <remarks>
    /// This class used to also run a "distance-based LOD" pass that called
    /// Formation.ResetArrangementOrderTickTimer() every 0.4s on every formation more than 140m
    /// from the player. That call restarts the engine's periodic arrangement-order countdown rather
    /// than skipping a tick of it, so restarting it more often than it fires never throttled
    /// anything: at best it did nothing, at worst it stopped those formations' arrangement upkeep
    /// from ever running while they stayed out of the player's sight - which in a large battle is
    /// most formations on the field, including every AI-vs-AI clash. The per-formation work it
    /// was meant to save is negligible next to per-agent AI and physics, so it was removed rather
    /// than kept on an unverifiable timing assumption.
    /// </remarks>
    public class BattlePerformanceOptimizer : MissionBehavior
    {
        private const int RagdollUpdateFrameInterval = 4;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        // Created eagerly (not in AfterStart) so an agent removed before AfterStart - or a mission
        // that never reaches it - can't make OnAgentRemoved, which has no catch of its own, throw a
        // NullReferenceException straight into the engine's mission loop.
        private readonly RagdollPhysicsBudgetManager _ragdollManager = new RagdollPhysicsBudgetManager();
        private int _frameCounter;

        public override void AfterStart()
        {
            base.AfterStart();
            _ragdollManager.Clear();
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

                if (++_frameCounter % RagdollUpdateFrameInterval == 0)
                {
                    _ragdollManager.Update();
                }
            }
            catch (Exception)
            {
                // Engine safety catch
            }
        }

        public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
            base.OnMissionModeChange(oldMissionMode, atStart);

            if (Mission.Current?.Mode != MissionMode.Battle)
            {
                _ragdollManager.Clear();
            }
        }

        public override void OnMissionStateFinalized()
        {
            base.OnMissionStateFinalized();
            _ragdollManager.Clear();
        }

        public override void OnClearScene()
        {
            base.OnClearScene();
            _ragdollManager.Clear();
        }
    }
}
