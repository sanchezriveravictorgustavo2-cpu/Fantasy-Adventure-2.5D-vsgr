using System.Collections.Generic;
using Game.Worlds.World01.Structures;
using UnityEngine;

namespace Game.Systems.Mining
{
    /// <summary>
    /// Caches existing mineral deposits under Mineral_Resources once at startup
    /// and selects accessible non-depleted ores for miners without per-frame scene searches.
    /// </summary>
    [DisallowMultipleComponent]
    public class MineralOreRegistry : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform mineralResourcesRoot;
        [SerializeField] private Transform baseReferencePoint;

        [Header("Accessibility Filtering")]
        [Tooltip("Excludes enemy base mineral deposits by name prefix.")]
        [SerializeField] private string excludedNamePrefix = "Enemy_";

        [Tooltip("Maximum horizontal distance from base for accessible player ores (0 = unlimited).")]
        [SerializeField, Min(0f)] private float maxDistanceFromBase = 40f;

        private readonly List<MineralOreDeposit> registeredOres = new List<MineralOreDeposit>();

        public IReadOnlyList<MineralOreDeposit> RegisteredOres => registeredOres;

        private void Awake()
        {
            RefreshOreList();
        }

        public void RefreshOreList()
        {
            registeredOres.Clear();

            if (mineralResourcesRoot == null)
            {
                GameObject found = GameObject.Find("Mineral_Resources");
                if (found != null)
                {
                    mineralResourcesRoot = found.transform;
                }
            }

            if (mineralResourcesRoot == null)
            {
                Debug.LogWarning("[MineralOreRegistry] Mineral_Resources root not found.", this);
                return;
            }

            MineralOreDeposit[] deposits = mineralResourcesRoot.GetComponentsInChildren<MineralOreDeposit>(false);
            Vector3 basePos = baseReferencePoint != null ? baseReferencePoint.position : Vector3.zero;

            for (int i = 0; i < deposits.Length; i++)
            {
                MineralOreDeposit ore = deposits[i];
                if (ore == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(excludedNamePrefix) &&
                    ore.gameObject.name.StartsWith(excludedNamePrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (maxDistanceFromBase > 0f && baseReferencePoint != null)
                {
                    float dx = Mathf.Abs(ore.transform.position.x - basePos.x);
                    if (dx > maxDistanceFromBase)
                    {
                        continue;
                    }
                }

                registeredOres.Add(ore);
            }
        }

        public bool HasAvailableOre()
        {
            for (int i = 0; i < registeredOres.Count; i++)
            {
                MineralOreDeposit ore = registeredOres[i];
                if (ore != null && !ore.IsDepleted && ore.HasCapacity)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Selects the best accessible non-depleted ore with free miner capacity (max 2 per ore),
        /// preferring ores with fewer assigned miners and shorter distance.
        /// </summary>
        public MineralOreDeposit FindBestAvailableOre(Vector3 fromPosition)
        {
            MineralOreDeposit bestOre = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < registeredOres.Count; i++)
            {
                MineralOreDeposit ore = registeredOres[i];
                if (ore == null || ore.IsDepleted || !ore.HasCapacity)
                {
                    continue;
                }

                Vector3 delta = ore.transform.position - fromPosition;
                delta.y = 0f;
                float dist = delta.magnitude;

                // Spread miners evenly across ores while strictly respecting max capacity.
                float score = dist + (ore.AssignedMinerCount * 4.5f);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestOre = ore;
                }
            }

            return bestOre;
        }

        /// <summary>
        /// Atomically finds and assigns the best available ore with free capacity to the requesting miner.
        /// Prevents multiple miners from simultaneously claiming a slot on a full ore.
        /// </summary>
        public bool TryAssignBestAvailableOre(Component miner, Vector3 fromPosition, out MineralOreDeposit assignedOre)
        {
            assignedOre = null;
            if (miner == null)
            {
                return false;
            }

            MineralOreDeposit candidate = FindBestAvailableOre(fromPosition);
            if (candidate == null)
            {
                return false;
            }

            if (candidate.TryAssignMiner(miner))
            {
                assignedOre = candidate;
                return true;
            }

            return false;
        }

        public void ConfigureReferences(Transform resourcesRoot, Transform baseRef)
        {
            mineralResourcesRoot = resourcesRoot;
            baseReferencePoint = baseRef;
        }
    }
}
