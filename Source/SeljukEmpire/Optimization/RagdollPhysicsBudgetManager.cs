using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SeljukEmpire.Optimization
{
    /// <summary>
    /// Enforces a strict physics-solver budget for battlefield ragdolls.
    /// Prevents CPU-bound PhysX thread bottleneck in 500+ soldier clashes by freezing settled corpses.
    /// </summary>
    public class RagdollPhysicsBudgetManager
    {
        private const int MAX_ACTIVE_PHYSICS_RAGDOLLS = 32;
        private const float RAGDOLL_SETTLE_TIMEOUT = 3.5f; // Settle corpses after 3.5 seconds

        private readonly Queue<RagdollTracker> _activeRagdolls = new Queue<RagdollTracker>(64);

        private struct RagdollTracker
        {
            public Agent DeadAgent;
            public MissionTime DeathTime;

            public RagdollTracker(Agent agent)
            {
                DeadAgent = agent;
                DeathTime = MissionTime.Now;
            }
        }

        public void RegisterDeadAgent(Agent agent)
        {
            if (agent == null) return;

            // If we exceed our active budget, immediately freeze the oldest ragdoll
            if (_activeRagdolls.Count >= MAX_ACTIVE_PHYSICS_RAGDOLLS)
            {
                RagdollTracker oldest = _activeRagdolls.Dequeue();
                FreezeRagdollPhysics(oldest.DeadAgent);
            }

            _activeRagdolls.Enqueue(new RagdollTracker(agent));
        }

        public void Update(float dt)
        {
            if (_activeRagdolls.Count == 0) return;

            // Check if any tracked ragdolls have timed out and settled
            int checkCount = Math.Min(4, _activeRagdolls.Count);
            for (int i = 0; i < checkCount; i++)
            {
                RagdollTracker current = _activeRagdolls.Peek();
                if (current.DeathTime.ElapsedSeconds > RAGDOLL_SETTLE_TIMEOUT)
                {
                    _activeRagdolls.Dequeue();
                    FreezeRagdollPhysics(current.DeadAgent);
                }
                else
                {
                    break;
                }
            }
        }

        private static void FreezeRagdollPhysics(Agent agent)
        {
            if (agent == null) return;

            try
            {
                // Put ragdoll physics to sleep to eliminate CPU collision calculation overhead
                if (agent.AgentVisuals != null)
                {
                    Skeleton skeleton = agent.AgentVisuals.GetSkeleton();
                    if (skeleton != null)
                    {
                        skeleton.Freeze(true);
                    }
                }
            }
            catch (Exception)
            {
                // Fail-safe protection
            }
        }

        public void Clear()
        {
            _activeRagdolls.Clear();
        }
    }
}
