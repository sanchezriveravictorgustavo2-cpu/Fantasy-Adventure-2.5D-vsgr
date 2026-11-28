using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Combat
{
    /// <summary>
    /// Lightweight static registry of active combat targets (units and statues)
    /// so units can find opposing-faction targets without per-frame scene searches.
    /// </summary>
    public static class CombatRegistry
    {
        private static readonly List<ICombatTarget> RegisteredTargets = new List<ICombatTarget>();

        public static IReadOnlyList<ICombatTarget> Targets => RegisteredTargets;

        public static void Register(ICombatTarget target)
        {
            if (target == null || RegisteredTargets.Contains(target))
            {
                return;
            }

            RegisteredTargets.Add(target);
        }

        public static void Unregister(ICombatTarget target)
        {
            if (target == null)
            {
                return;
            }

            RegisteredTargets.Remove(target);
        }

        public static void Clear()
        {
            RegisteredTargets.Clear();
        }

        /// <summary>
        /// Finds the closest living opposing-faction target within maxRange (0 or negative = unlimited).
        /// Optionally prioritizes non-structure units over statues when both are in range.
        /// </summary>
        public static ICombatTarget FindClosestEnemy(
            Vector3 fromPosition,
            TeamFaction myFaction,
            float maxRange,
            bool preferUnitsOverStructures = true)
        {
            PruneInvalid();

            ICombatTarget bestUnit = null;
            float bestUnitDistSq = float.MaxValue;
            ICombatTarget bestStructure = null;
            float bestStructureDistSq = float.MaxValue;

            float maxRangeSq = maxRange > 0f ? maxRange * maxRange : float.MaxValue;

            for (int i = 0; i < RegisteredTargets.Count; i++)
            {
                ICombatTarget candidate = RegisteredTargets[i];
                if (candidate == null || !candidate.IsAlive || candidate.Faction == myFaction)
                {
                    continue;
                }

                Vector3 delta = candidate.TargetPosition - fromPosition;
                delta.y = 0f;
                float distSq = delta.sqrMagnitude;
                if (distSq > maxRangeSq)
                {
                    continue;
                }

                if (candidate.IsStructure)
                {
                    if (distSq < bestStructureDistSq)
                    {
                        bestStructureDistSq = distSq;
                        bestStructure = candidate;
                    }
                }
                else
                {
                    if (distSq < bestUnitDistSq)
                    {
                        bestUnitDistSq = distSq;
                        bestUnit = candidate;
                    }
                }
            }

            if (preferUnitsOverStructures && bestUnit != null)
            {
                return bestUnit;
            }

            if (bestUnit != null && bestStructure != null)
            {
                return bestUnitDistSq <= bestStructureDistSq ? bestUnit : bestStructure;
            }

            return bestUnit ?? bestStructure;
        }

        public static ICombatTarget FindEnemyStatue(TeamFaction myFaction)
        {
            PruneInvalid();

            for (int i = 0; i < RegisteredTargets.Count; i++)
            {
                ICombatTarget candidate = RegisteredTargets[i];
                if (candidate != null && candidate.IsAlive && candidate.IsStructure && candidate.Faction != myFaction)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void PruneInvalid()
        {
            for (int i = RegisteredTargets.Count - 1; i >= 0; i--)
            {
                ICombatTarget target = RegisteredTargets[i];
                if (target == null || target.TargetTransform == null)
                {
                    RegisteredTargets.RemoveAt(i);
                }
            }
        }
    }
}
