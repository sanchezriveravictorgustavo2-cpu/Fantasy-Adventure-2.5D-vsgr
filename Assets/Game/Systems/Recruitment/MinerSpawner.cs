using Game.Characters.Miner;
using Game.Systems.Economy;
using Game.Systems.Mining;
using UnityEngine;

namespace Game.Systems.Recruitment
{
    /// <summary>
    /// Instantiates the existing Miner prefab from an off-screen position behind the left
    /// edge of the camera so the miner walks naturally into the 2.5D base area.
    /// </summary>
    [DisallowMultipleComponent]
    public class MinerSpawner : MonoBehaviour
    {
        [Header("Prefab & Parent")]
        [SerializeField] private GameObject minerPrefab;
        [SerializeField] private Transform spawnedMinersParent;

        [Header("Entry & Base Points")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform offscreenSpawnPoint;
        [SerializeField] private Transform baseEntryPoint;
        [SerializeField] private Transform safeIdlePoint;
        [SerializeField] private Transform depositPoint;
        [SerializeField] private float extraOffscreenMarginX = 3.2f;
        [SerializeField] private float entryPlaneZ = -2.05f;

        [Header("Gameplay System References")]
        [SerializeField] private MineralOreRegistry oreRegistry;
        [SerializeField] private TerrainHeightSampler terrainSampler;
        [SerializeField] private GoldManager goldManager;
        [SerializeField] private Transform playerStatueTransform;

        private int spawnedCount;

        public int SpawnedCount => spawnedCount;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (oreRegistry == null)
            {
                oreRegistry = FindFirstObjectByType<MineralOreRegistry>();
            }

            if (terrainSampler == null)
            {
                terrainSampler = FindFirstObjectByType<TerrainHeightSampler>();
            }

            if (goldManager == null)
            {
                goldManager = FindFirstObjectByType<GoldManager>();
            }

            if (depositPoint == null)
            {
                GameObject foundDeposit = GameObject.Find("Player_Statue_DepositPoint");
                if (foundDeposit != null)
                {
                    depositPoint = foundDeposit.transform;
                }
            }
        }

        public MinerUnitController SpawnMiner()
        {
            if (minerPrefab == null)
            {
                Debug.LogError("[MinerSpawner] minerPrefab is not assigned.", this);
                return null;
            }

            Vector3 spawnPos = ResolveOffscreenSpawnPosition();
            Vector3 entryPos = baseEntryPoint != null
                ? baseEntryPoint.position
                : new Vector3(3.8f, 0f, -1.35f);
            Vector3 idlePos = safeIdlePoint != null
                ? safeIdlePoint.position
                : new Vector3(4.0f, 0f, -0.65f);
            Vector3 depositPos = ResolveDepositPosition();

            if (terrainSampler != null)
            {
                spawnPos.y = terrainSampler.SampleHeight(spawnPos);
                entryPos.y = terrainSampler.SampleHeight(entryPos);
                idlePos.y = terrainSampler.SampleHeight(idlePos);
                depositPos.y = terrainSampler.SampleHeight(depositPos);
            }

            Quaternion facingRight = Quaternion.LookRotation(Vector3.right, Vector3.up);
            GameObject instance = Instantiate(minerPrefab, spawnPos, facingRight, spawnedMinersParent);
            instance.name = string.Format("Miner_Collector_{0:00}", spawnedCount + 1);

            MinerUnitController unit = instance.GetComponent<MinerUnitController>();
            if (unit != null)
            {
                unit.Initialize(
                    oreRegistry,
                    terrainSampler,
                    goldManager,
                    playerStatueTransform,
                    entryPos,
                    idlePos,
                    spawnedCount,
                    depositPos);
            }

            spawnedCount++;
            return unit;
        }

        private Vector3 ResolveDepositPosition()
        {
            if (depositPoint != null)
            {
                return depositPoint.position;
            }

            if (playerStatueTransform != null)
            {
                Vector3 behindDir = -playerStatueTransform.forward;
                behindDir.y = 0f;
                if (behindDir.sqrMagnitude < 0.001f)
                {
                    behindDir = Vector3.left;
                }
                else
                {
                    behindDir.Normalize();
                }

                return playerStatueTransform.position + behindDir * 3.0f;
            }

            return new Vector3(-1.85f, 0f, 1.45f);
        }

        private Vector3 ResolveOffscreenSpawnPosition()
        {
            Vector3 pos = offscreenSpawnPoint != null
                ? offscreenSpawnPoint.position
                : new Vector3(-4.8f, 0f, entryPlaneZ);

            // Guarantee the miner starts genuinely outside the left edge of the camera frustum.
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
            Transform unitsParent,
            Camera cam,
            Transform spawnPt,
            Transform entryPt,
            Transform idlePt,
            MineralOreRegistry registry,
            TerrainHeightSampler sampler,
            GoldManager economy,
            Transform statue)
        {
            minerPrefab = prefab;
            spawnedMinersParent = unitsParent;
            mainCamera = cam;
            offscreenSpawnPoint = spawnPt;
            baseEntryPoint = entryPt;
            safeIdlePoint = idlePt;
            oreRegistry = registry;
            terrainSampler = sampler;
            goldManager = economy;
            playerStatueTransform = statue;
        }
    }
}
