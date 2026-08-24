using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SeljukEmpire.Tactics
{
    /// <summary>
    /// High-performance geometric and spatial mathematics utility for Seljuk Tactical AI.
    /// Provides zero-GC terrain evaluation, elevation scoring, and map boundary safety buffers.
    /// </summary>
    public static class TacticalFormationsHelper
    {
        private const float MAP_BOUNDARY_BUFFER = 65f; // Keep troops 65m away from invisible map barriers

        /// <summary>
        /// Finds the safest high-ground anchor position near the friendly deployment zone.
        /// </summary>
        public static Vec3 FindOptimalHighGround(Vec3 centerPos, float searchRadius = 70f)
        {
            if (Mission.Current?.Scene == null) return centerPos;

            Scene scene = Mission.Current.Scene;
            Vec3 bestPos = centerPos;
            float highestZ = centerPos.z;

            // Sample 8 radial terrain points in cardinal and diagonal directions (Zero-GC stack loop)
            float stepAngle = (float)(Math.PI / 4.0);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * stepAngle;
                float sampleX = centerPos.x + (float)Math.Cos(angle) * searchRadius;
                float sampleY = centerPos.y + (float)Math.Sin(angle) * searchRadius;

                float terrainZ = scene.GetTerrainHeight(new Vec2(sampleX, sampleY));
                if (terrainZ > highestZ + 1.2f) // Must be at least 1.2 meters higher to consider high ground
                {
                    highestZ = terrainZ;
                    bestPos = new Vec3(sampleX, sampleY, terrainZ);
                }
            }

            return ClampToMapBoundaries(bestPos);
        }

        /// <summary>
        /// Clamps positions away from map edges to prevent horse archer wall-stuck glitches.
        /// </summary>
        public static Vec3 ClampToMapBoundaries(Vec3 pos)
        {
            if (Mission.Current?.Scene == null) return pos;

            // Bannerlord standard battlefield boundaries
            Vec3 min, max;
            Mission.Current.Scene.GetBoundingBox(out min, out max);

            float clampedX = MBMath.ClampFloat(pos.x, min.x + MAP_BOUNDARY_BUFFER, max.x - MAP_BOUNDARY_BUFFER);
            float clampedY = MBMath.ClampFloat(pos.y, min.y + MAP_BOUNDARY_BUFFER, max.y - MAP_BOUNDARY_BUFFER);
            float z = Mission.Current.Scene.GetTerrainHeight(new Vec2(clampedX, clampedY));

            return new Vec3(clampedX, clampedY, z);
        }

        /// <summary>
        /// Computes the flanking offset position on the left or right wing of a target position.
        /// </summary>
        public static Vec3 CalculateFlankVector(Vec3 center, Vec3 enemyPos, bool isLeftFlank, float flankDistance = 85f)
        {
            Vec2 dir = (enemyPos.AsVec2 - center.AsVec2).Normalized();
            // Perpendicular vector for flanking (90 degrees left or right)
            Vec2 perpendicular = isLeftFlank ? new Vec2(-dir.y, dir.x) : new Vec2(dir.y, -dir.x);
            
            Vec2 target2D = center.AsVec2 + (perpendicular * flankDistance);
            float z = Mission.Current?.Scene != null ? Mission.Current.Scene.GetTerrainHeight(target2D) : center.z;

            return ClampToMapBoundaries(new Vec3(target2D.x, target2D.y, z));
        }

        /// <summary>
        /// Checks if a formation has exhausted most of its missile ammunition.
        /// </summary>
        public static bool IsRangedAmmoDepleted(Formation formation, float threshold = 0.20f)
        {
            if (formation == null || formation.CountOfUnits <= 0) return true;

            int lowAmmoCount = 0;
            int totalRanged = 0;

            formation.ApplyActionOnEachUnit(agent =>
            {
                if (agent.IsActive() && agent.IsRangedCached)
                {
                    totalRanged++;
                    // Check if agent has 3 or fewer arrows left
                    if (agent.Equipment.GetAmmoAmount(EquipmentIndex.Weapon0) +
                        agent.Equipment.GetAmmoAmount(EquipmentIndex.Weapon1) +
                        agent.Equipment.GetAmmoAmount(EquipmentIndex.Weapon2) +
                        agent.Equipment.GetAmmoAmount(EquipmentIndex.Weapon3) <= 3)
                    {
                        lowAmmoCount++;
                    }
                }
            });

            return totalRanged > 0 && ((float)lowAmmoCount / totalRanged) >= (1.0f - threshold);
        }
    }
}
