using System;
using System.Collections.Generic;
using Game.Characters.Archer;
using Game.Systems.Mining;
using UnityEngine;

namespace Game.Systems.Army
{
    /// <summary>
    /// Spawns defensive Archers individually every 10 seconds while the army is in HideArmy mode.
    /// Positions each Archer in the next available slot of the defensive line behind our barricade.
    /// Stops spawning when the player switches to Attack (keeping existing Archers in place).
    /// </summary>
    [DisallowMultipleComponent]
    public class ArcherSpawnController : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField, Min(1f)] private float spawnIntervalSeconds = 10f;
        [SerializeField, Min(1)] private int maxDefensiveArchers = 12;
        [SerializeField, Min(1)] private int archersPerLine = 4;
        [SerializeField, Min(0.5f)] private float slotSpacingZ = 1.10f;
        [SerializeField, Min(0.5f)] private float lineSpacingX = 1.15f;

        [Header("Prefab & Scene References")]
        [SerializeField] private GameObject archerPrefab;
        [SerializeField] private GameObject arrowProjectilePrefab;
        [SerializeField] private Transform spawnedArchersParent;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform offscreenSpawnPoint;
        [SerializeField] private Transform defensiveLineAnchor;
        [SerializeField] private TerrainHeightSampler terrainSampler;
        [SerializeField] private float extraOffscreenMarginX = 3.2f;
        [SerializeField] private float entryPlaneZ = -1.6f;

        private readonly Dictionary<int, ArcherUnitController> slotAssignments = new Dictionary<int, ArcherUnitController>();
        private readonly List<ArcherUnitController> activeArchers = new List<ArcherUnitController>();
        private bool isSpawningActive;
        private float spawnTimer;
        private int totalSpawned;

        public bool IsSpawningActive => isSpawningActive;
        public float SpawnIntervalSeconds => spawnIntervalSeconds;
        public float TimeUntilNextSpawn => isSpawningActive ? Mathf.Max(0f, spawnIntervalSeconds - spawnTimer) : 0f;
        public int ActiveArcherCount
        {
            get
            {
                PruneDead();
                return activeArchers.Count;
            }
        }
        public IReadOnlyList<ArcherUnitController> ActiveArchers => activeArchers;

        public event Action<ArcherUnitController> OnArcherSpawned;

        private void Awake()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (terrainSampler == null) terrainSampler = FindFirstObjectByType<TerrainHeightSampler>();
        }

        public void SetDefensiveSpawningActive(bool active)
        {
            if (isSpawningActive == active)
            {
                return;
            }

            isSpawningActive = active;
            if (!isSpawningActive)
            {
                // Pause/reset timer cleanly when leaving defensive mode; keep all existing archers.
                spawnTimer = 0f;
            }
            else
            {
                spawnTimer = 0f;
            }
        }

        private void Update()
        {
            if (!isSpawningActive)
            {
                return;
            }

            PruneDead();
            if (activeArchers.Count >= maxDefensiveArchers)
            {
                return;
            }

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnIntervalSeconds)
            {
                spawnTimer -= spawnIntervalSeconds;
                SpawnSingleDefensiveArcher();
            }
        }

        public ArcherUnitController SpawnSingleDefensiveArcher()
        {
            if (archerPrefab == null)
            {
                Debug.LogError("[ArcherSpawnController] archerPrefab is not assigned.", this);
                return null;
            }

            PruneDead();
            int slotIndex = FindNextAvailableSlotIndex();
            if (slotIndex < 0)
            {
                return null;
            }

            Vector3 spawnPos = ResolveOffscreenSpawnPosition();
            Vector3 defenseSlot = ComputeDefensiveSlotPosition(slotIndex);

            if (terrainSampler != null)
            {
                spawnPos.y = terrainSampler.SampleHeight(spawnPos);
                defenseSlot.y = terrainSampler.SampleHeight(defenseSlot);
            }

            Quaternion facingRight = Quaternion.LookRotation(Vector3.right, Vector3.up);
            GameObject instance = Instantiate(archerPrefab, spawnPos, facingRight, spawnedArchersParent);
            instance.name = string.Format("Archer_Defender_{0:00}", totalSpawned + 1);

            ArcherUnitController archer = instance.GetComponent<ArcherUnitController>();
            if (archer != null)
            {
                archer.Initialize(terrainSampler, defenseSlot, slotIndex, arrowProjectilePrefab);
                archer.OnArcherDied += HandleArcherDied;
                slotAssignments[slotIndex] = archer;
                activeArchers.Add(archer);
            }

            totalSpawned++;
            OnArcherSpawned?.Invoke(archer);
            return archer;
        }

        public Vector3 ComputeDefensiveSlotPosition(int slotIndex)
        {
            int perLine = Mathf.Max(1, archersPerLine);
            int line = Mathf.Max(0, slotIndex) / perLine;
            int col = Mathf.Max(0, slotIndex) % perLine;

            Vector3 origin = defensiveLineAnchor != null
                ? defensiveLineAnchor.position
                : new Vector3(-1.8f, 0f, -0.1f);

            float centeredCol = col - (perLine - 1) * 0.5f;
            float x = origin.x - line * lineSpacingX;
            float z = origin.z + centeredCol * slotSpacingZ;
            float y = terrainSampler != null ? terrainSampler.SampleHeight(new Vector3(x, origin.y, z)) : origin.y;

            return new Vector3(x, y, z);
        }

        private int FindNextAvailableSlotIndex()
        {
            for (int i = 0; i < maxDefensiveArchers; i++)
            {
                if (!slotAssignments.TryGetValue(i, out ArcherUnitController occupant) || occupant == null || !occupant.IsAlive)
                {
                    return i;
                }
            }

            return -1;
        }

        private Vector3 ResolveOffscreenSpawnPosition()
        {
            Vector3 pos = offscreenSpawnPoint != null
                ? offscreenSpawnPoint.position
                : new Vector3(-5.2f, 0f, entryPlaneZ);

            if (mainCamera != null)
            {
                float depth = Mathf.Abs(mainCamera.transform.position.z - entryPlaneZ);
                Vector3 leftEdgeWorld = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0.5f, depth));
                float maxAllowedX = leftEdgeWorld.x - extraOffscreenMarginX;
                if (pos.x > maxAllowedX)
                {
                    pos.x = maxAllowedX;
                }
            }

            pos.z = entryPlaneZ;
            return pos;
        }

        private void HandleArcherDied(ArcherUnitController archer)
        {
            if (archer == null)
            {
                return;
            }

            archer.OnArcherDied -= HandleArcherDied;
            activeArchers.Remove(archer);
            if (archer.AssignedSlotIndex >= 0 && slotAssignments.TryGetValue(archer.AssignedSlotIndex, out ArcherUnitController mapped) && mapped == archer)
            {
                slotAssignments.Remove(archer.AssignedSlotIndex);
            }
        }

        private void PruneDead()
        {
            for (int i = activeArchers.Count - 1; i >= 0; i--)
            {
                ArcherUnitController archer = activeArchers[i];
                if (archer == null || !archer.IsAlive)
                {
                    if (archer != null)
                    {
                        archer.OnArcherDied -= HandleArcherDied;
                        if (archer.AssignedSlotIndex >= 0)
                        {
                            slotAssignments.Remove(archer.AssignedSlotIndex);
                        }
                    }
                    activeArchers.RemoveAt(i);
                }
            }
        }

        public void ConfigureReferences(
            GameObject prefab,
            GameObject arrowPrefab,
            Transform parent,
            Camera cam,
            Transform spawnPt,
            Transform defenseAnchor,
            TerrainHeightSampler sampler)
        {
            archerPrefab = prefab;
            arrowProjectilePrefab = arrowPrefab;
            spawnedArchersParent = parent;
            mainCamera = cam;
            offscreenSpawnPoint = spawnPt;
            defensiveLineAnchor = defenseAnchor;
            terrainSampler = sampler;
        }
    }
}
