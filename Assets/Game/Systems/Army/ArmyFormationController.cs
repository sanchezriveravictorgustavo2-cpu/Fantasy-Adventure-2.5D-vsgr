using System.Collections.Generic;
using Game.Characters.Explorer;
using Game.Systems.Mining;
using UnityEngine;

namespace Game.Systems.Army
{
    /// <summary>
    /// Organizes allied Explorer units into deterministic rows of 4 units each
    /// with uniform spacing and zero overlap, supporting both forward formation
    /// (Formar Filas) and defensive garrison behind the allied barricade (Esconder Ejército).
    /// </summary>
    [DisallowMultipleComponent]
    public class ArmyFormationController : MonoBehaviour
    {
        [Header("Formation Layout (Rows of 4)")]
        [SerializeField, Min(1)] private int unitsPerRow = 4;
        [SerializeField, Min(0.5f)] private float unitSpacingZ = 1.10f;
        [SerializeField, Min(0.5f)] private float rowSpacingX = 1.45f;

        [Header("Anchor Points")]
        [SerializeField] private Transform forwardFormationAnchor;
        [SerializeField] private Transform defensiveFormationAnchor;
        [SerializeField] private Vector3 defaultForwardOrigin = new Vector3(16.5f, 0f, -0.1f);
        [SerializeField] private Vector3 defaultDefenseOrigin = new Vector3(3.8f, 0f, -0.1f);
        [SerializeField] private TerrainHeightSampler terrainSampler;

        private readonly List<ExplorerUnitController> registeredExplorers = new List<ExplorerUnitController>();
        private ArmyOrder activeOrder = ArmyOrder.FormRanks;

        public int UnitsPerRow => Mathf.Max(1, unitsPerRow);
        public float UnitSpacingZ => unitSpacingZ;
        public float RowSpacingX => rowSpacingX;
        public int ActiveExplorerCount
        {
            get
            {
                PruneDead();
                return registeredExplorers.Count;
            }
        }
        public IReadOnlyList<ExplorerUnitController> RegisteredExplorers => registeredExplorers;

        private void Awake()
        {
            if (terrainSampler == null)
            {
                terrainSampler = FindFirstObjectByType<TerrainHeightSampler>();
            }
        }

        public Vector3 RegisterExplorer(ExplorerUnitController explorer)
        {
            PruneDead();

            if (explorer == null)
            {
                return GetSlotPosition(0, activeOrder == ArmyOrder.HideArmy);
            }

            if (!registeredExplorers.Contains(explorer))
            {
                registeredExplorers.Add(explorer);
                explorer.OnExplorerDied += HandleExplorerDied;
            }

            int index = registeredExplorers.IndexOf(explorer);
            bool useDefense = activeOrder == ArmyOrder.HideArmy;
            return GetSlotPosition(index, useDefense);
        }

        public void UnregisterExplorer(ExplorerUnitController explorer)
        {
            if (explorer == null)
            {
                return;
            }

            explorer.OnExplorerDied -= HandleExplorerDied;
            if (registeredExplorers.Remove(explorer))
            {
                RefreshFormationPositions(moveImmediately: activeOrder != ArmyOrder.Attack);
            }
        }

        public void ApplyOrderToAll(ArmyOrder order)
        {
            activeOrder = order;
            PruneDead();

            bool useDefense = order == ArmyOrder.HideArmy;
            for (int i = 0; i < registeredExplorers.Count; i++)
            {
                ExplorerUnitController unit = registeredExplorers[i];
                if (unit != null && unit.IsAlive)
                {
                    Vector3 slot = GetSlotPosition(i, useDefense);
                    unit.ApplyOrder(order, slot);
                }
            }
        }

        public void RefreshFormationPositions(bool moveImmediately)
        {
            PruneDead();
            bool useDefense = activeOrder == ArmyOrder.HideArmy;

            for (int i = 0; i < registeredExplorers.Count; i++)
            {
                ExplorerUnitController unit = registeredExplorers[i];
                if (unit != null && unit.IsAlive)
                {
                    Vector3 slot = GetSlotPosition(i, useDefense);
                    unit.SetFormationSlot(slot, moveImmediately);
                }
            }
        }

        /// <summary>
        /// Computes the world position for slot index in rows of 4.
        /// Row 0 is front-most; each subsequent row steps backward (-X) behind the previous row.
        /// </summary>
        public Vector3 GetSlotPosition(int slotIndex, bool defensiveMode)
        {
            int perRow = Mathf.Max(1, unitsPerRow);
            int row = Mathf.Max(0, slotIndex) / perRow;
            int col = Mathf.Max(0, slotIndex) % perRow;

            Vector3 origin = defensiveMode
                ? (defensiveFormationAnchor != null ? defensiveFormationAnchor.position : defaultDefenseOrigin)
                : (forwardFormationAnchor != null ? forwardFormationAnchor.position : defaultForwardOrigin);

            // Center 4 units symmetrically across Z: -1.5, -0.5, +0.5, +1.5 * unitSpacingZ
            float centeredCol = col - (perRow - 1) * 0.5f;
            float x = origin.x - row * rowSpacingX;
            float z = origin.z + centeredCol * unitSpacingZ;
            float y = terrainSampler != null ? terrainSampler.SampleHeight(new Vector3(x, origin.y, z)) : origin.y;

            return new Vector3(x, y, z);
        }

        private void HandleExplorerDied(ExplorerUnitController explorer)
        {
            UnregisterExplorer(explorer);
        }

        private void PruneDead()
        {
            for (int i = registeredExplorers.Count - 1; i >= 0; i--)
            {
                ExplorerUnitController unit = registeredExplorers[i];
                if (unit == null || !unit.IsAlive)
                {
                    if (unit != null)
                    {
                        unit.OnExplorerDied -= HandleExplorerDied;
                    }
                    registeredExplorers.RemoveAt(i);
                }
            }
        }

        public void ConfigureReferences(Transform forwardAnchor, Transform defenseAnchor, TerrainHeightSampler sampler)
        {
            forwardFormationAnchor = forwardAnchor;
            defensiveFormationAnchor = defenseAnchor;
            terrainSampler = sampler;
        }
    }
}
