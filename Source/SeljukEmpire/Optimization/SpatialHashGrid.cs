using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SeljukEmpire.Optimization
{
    /// <summary>
    /// High-performance 2D Spatial Hash Grid for Zero-GC O(1) agent proximity queries.
    /// Replaces expensive O(N*M) distance loops with fast cell-bucket lookups.
    /// </summary>
    public class SpatialHashGrid
    {
        private readonly float _cellSize;
        private readonly float _invCellSize;
        private readonly Dictionary<int, List<Agent>> _grid;
        private readonly List<Agent> _emptyList = new List<Agent>(0);

        public SpatialHashGrid(float cellSize = 35f)
        {
            _cellSize = cellSize;
            _invCellSize = 1.0f / cellSize;
            _grid = new Dictionary<int, List<Agent>>(128);
        }

        public void Clear()
        {
            foreach (var bucket in _grid.Values)
            {
                bucket.Clear();
            }
        }

        public void Insert(Agent agent)
        {
            if (agent == null || !agent.IsActive()) return;

            int hash = GetCellHash(agent.Position.x, agent.Position.y);
            if (!_grid.TryGetValue(hash, out List<Agent> list))
            {
                list = new List<Agent>(16);
                _grid[hash] = list;
            }
            list.Add(agent);
        }

        public void Rebuild(Team targetTeam)
        {
            Clear();
            if (targetTeam == null) return;

            foreach (var formation in targetTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0) continue;
                formation.ApplyActionOnEachUnit(Insert);
            }
        }

        /// <summary>
        /// Finds the closest enemy agent in nearby grid cells in O(1) average time.
        /// </summary>
        public Agent FindClosestAgentInRadius(Vec3 searchPos, float maxRadius, Team enemyTeam)
        {
            int centerCellX = (int)Math.Floor(searchPos.x * _invCellSize);
            int centerCellY = (int)Math.Floor(searchPos.y * _invCellSize);
            int cellRadius = (int)Math.Ceiling(maxRadius * _invCellSize);

            Agent bestAgent = null;
            float bestDistSq = maxRadius * maxRadius;

            // Search 3x3 or 5x5 neighboring cell window
            for (int x = centerCellX - cellRadius; x <= centerCellX + cellRadius; x++)
            {
                for (int y = centerCellY - cellRadius; y <= centerCellY + cellRadius; y++)
                {
                    int hash = ComputeHash(x, y);
                    if (_grid.TryGetValue(hash, out List<Agent> cellAgents))
                    {
                        int count = cellAgents.Count;
                        for (int i = 0; i < count; i++)
                        {
                            Agent candidate = cellAgents[i];
                            if (candidate == null || !candidate.IsActive() || candidate.Team != enemyTeam) continue;

                            float distSq = searchPos.DistanceSquared(candidate.Position);
                            if (distSq < bestDistSq)
                            {
                                bestDistSq = distSq;
                                bestAgent = candidate;
                            }
                        }
                    }
                }
            }

            return bestAgent;
        }

        private int GetCellHash(float x, float y)
        {
            int cellX = (int)Math.Floor(x * _invCellSize);
            int cellY = (int)Math.Floor(y * _invCellSize);
            return ComputeHash(cellX, cellY);
        }

        private static int ComputeHash(int cellX, int cellY)
        {
            unchecked
            {
                return (cellX * 73856093) ^ (cellY * 19349663);
            }
        }
    }
}
