using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SeljukEmpire.Optimization
{
    /// <summary>
    /// Keeps a budget on simultaneously simulated battlefield ragdolls by freezing the skeletons of
    /// corpses that have had time to settle, so 500+ soldier clashes don't pile an unbounded number
    /// of live ragdolls onto the physics thread.
    /// </summary>
    public class RagdollPhysicsBudgetManager
    {
        private const int MaxActivePhysicsRagdolls = 32;

        /// <summary>A corpse is considered settled, and frozen, this many seconds after death.</summary>
        private const float RagdollSettleTimeoutSeconds = 3.5f;

        /// <summary>
        /// Even over budget, a corpse is never frozen younger than this. Deaths in a big clash
        /// easily outpace 32 per 3.5s, and the previous "over budget -> freeze the oldest right now"
        /// rule could then freeze a body that had died a fraction of a second earlier, mid-fall -
        /// leaving it hanging in the air or propped at an impossible angle for the rest of the
        /// battle. The budget may now be exceeded briefly; it is restored as soon as the backlog
        /// ages past this point.
        /// </summary>
        private const float MinimumAgeBeforeForcedFreezeSeconds = 1.5f;

        /// <summary>Upper bound on corpses frozen per <see cref="Update"/> call, to spread the work.</summary>
        private const int MaxFreezesPerUpdate = 4;

        private readonly Queue<RagdollTracker> _activeRagdolls = new Queue<RagdollTracker>(64);

        private readonly struct RagdollTracker
        {
            public readonly Agent DeadAgent;
            public readonly MissionTime DeathTime;

            public RagdollTracker(Agent agent)
            {
                DeadAgent = agent;
                DeathTime = MissionTime.Now;
            }
        }

        public int ActiveCount => _activeRagdolls.Count;

        public void RegisterDeadAgent(Agent agent)
        {
            if (agent == null) return;

            if (_activeRagdolls.Count >= MaxActivePhysicsRagdolls)
            {
                TryFreezeOldest(overBudget: true);
            }

            _activeRagdolls.Enqueue(new RagdollTracker(agent));
        }

        /// <summary>
        /// Freezes up to <see cref="MaxFreezesPerUpdate"/> of the oldest tracked corpses that have
        /// either settled or are holding the budget over its limit. The queue is in death order, so
        /// the scan stops at the first corpse that doesn't qualify yet.
        /// </summary>
        public void Update()
        {
            for (int i = 0; i < MaxFreezesPerUpdate && _activeRagdolls.Count > 0; i++)
            {
                if (!TryFreezeOldest(overBudget: _activeRagdolls.Count > MaxActivePhysicsRagdolls))
                {
                    break;
                }
            }
        }

        private bool TryFreezeOldest(bool overBudget)
        {
            float age = _activeRagdolls.Peek().DeathTime.ElapsedSeconds;
            bool qualifies = age > RagdollSettleTimeoutSeconds
                || (overBudget && age >= MinimumAgeBeforeForcedFreezeSeconds);
            if (!qualifies) return false;

            FreezeRagdollPhysics(_activeRagdolls.Dequeue().DeadAgent);
            return true;
        }

        private static void FreezeRagdollPhysics(Agent agent)
        {
            if (agent == null) return;

            try
            {
                // Put the settled ragdoll to sleep to take it off the physics solver.
                Skeleton skeleton = agent.AgentVisuals?.GetSkeleton();
                skeleton?.Freeze(true);
            }
            catch (Exception)
            {
                // The agent's visuals can already be gone (e.g. mission teardown) - nothing to freeze.
            }
        }

        public void Clear()
        {
            _activeRagdolls.Clear();
        }
    }
}
