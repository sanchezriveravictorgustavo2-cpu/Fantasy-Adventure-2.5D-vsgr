using Game.Characters.Explorer;
using Game.Systems.Army;
using Game.Systems.Mining;
using UnityEngine;

namespace Game.Systems.Recruitment
{
    /// <summary>
    /// Instantiates the Explorer prefab behind the left edge of the camera so
    /// newly recruited Explorers walk into the scene and automatically join their
    /// assigned 4-per-row formation slot.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExplorerSpawner : MonoBehaviour
    {
        [Header("Prefab & Parent")]
        [SerializeField] private GameObject explorerPrefab;
        [SerializeField] private Transform spawnedExplorersParent;

        [Header("Spawn & Scene References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform offscreenSpawnPoint;
        [SerializeField] private float extraOffscreenMarginX = 3.2f;
        [SerializeField] private float entryPlaneZ = -1.1f;

        [Header("Gameplay System References")]
        [SerializeField] private TerrainHeightSampler terrainSampler;
        [SerializeField] private ArmyFormationController formationController;
        [SerializeField] private ArmyCommandController commandController;

        private int spawnedCount;

        public int SpawnedCount => spawnedCount;

        private void Awake()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (terrainSampler == null) terrainSampler = FindFirstObjectByType<TerrainHeightSampler>();
            if (formationController == null) formationController = FindFirstObjectByType<ArmyFormationController>();
            if (commandController == null) commandController = FindFirstObjectByType<ArmyCommandController>();
        }

        public ExplorerUnitController SpawnExplorer()
        {
            if (explorerPrefab == null)
            {
                Debug.LogError("[ExplorerSpawner] explorerPrefab is not assigned.", this);
                return null;
            }

            Vector3 spawnPos = ResolveOffscreenSpawnPosition();
            if (terrainSampler != null)
            {
                spawnPos.y = terrainSampler.SampleHeight(spawnPos);
            }

            Quaternion facingRight = Quaternion.LookRotation(Vector3.right, Vector3.up);
            GameObject instance = Instantiate(explorerPrefab, spawnPos, facingRight, spawnedExplorersParent);
            instance.name = string.Format("Explorer_Soldier_{0:00}", spawnedCount + 1);

            ExplorerUnitController unit = instance.GetComponent<ExplorerUnitController>();
            if (unit != null)
            {
                ArmyOrder currentOrder = commandController != null ? commandController.CurrentOrder : ArmyOrder.FormRanks;
                Vector3 slotPos = formationController != null
                    ? formationController.RegisterExplorer(unit)
                    : new Vector3(16.5f, spawnPos.y, 0f);

                unit.Initialize(terrainSampler, slotPos, currentOrder);
            }

            spawnedCount++;
            return unit;
        }

        private Vector3 ResolveOffscreenSpawnPosition()
        {
            Vector3 pos = offscreenSpawnPoint != null
                ? offscreenSpawnPoint.position
                : new Vector3(-5.0f, 0f, entryPlaneZ);

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

        public void ConfigureReferences(
            GameObject prefab,
            Transform parent,
            Camera cam,
            Transform spawnPt,
            TerrainHeightSampler sampler,
            ArmyFormationController formation,
            ArmyCommandController command)
        {
            explorerPrefab = prefab;
            spawnedExplorersParent = parent;
            mainCamera = cam;
            offscreenSpawnPoint = spawnPt;
            terrainSampler = sampler;
            formationController = formation;
            commandController = command;
        }
    }
}
