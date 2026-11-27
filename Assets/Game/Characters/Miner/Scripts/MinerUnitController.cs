using Game.Systems.Economy;
using Game.Systems.Mining;
using Game.Worlds.World01.Structures;
using UnityEngine;

namespace Game.Characters.Miner
{
    /// <summary>
    /// High-level autonomous behavior state machine for a recruited Miner unit:
    /// EnteringBase -> MovingToOre -> Mining -> MovingToDeposit -> Depositing -> (back to assigned ore or safe Idle).
    /// </summary>
    [DisallowMultipleComponent]
    public class MinerUnitController : MonoBehaviour
    {
        public enum MinerState
        {
            EnteringBase,
            MovingToOre,
            Mining,
            MovingToDeposit,
            Depositing,
            ReturningToIdle,
            Idle
        }

        [Header("Mining Positioning")]
        [SerializeField, Min(0.6f)] private float miningStandDistance = 1.25f;

        [Header("Deposit & Idle")]
        [SerializeField, Min(0.2f)] private float depositDuration = 1.25f;
        [SerializeField, Min(0.1f)] private float idleRetryInterval = 0.5f;

        [Header("Components")]
        [SerializeField] private MinerMovement movement;
        [SerializeField] private MinerMiningController miningController;
        [SerializeField] private MinerAnimatorController animatorController;

        private MineralOreRegistry oreRegistry;
        private MineralOreDeposit targetOre;
        private Transform playerStatueTransform;
        private Vector3 baseEntryPosition;
        private Vector3 depositPosition;
        private Vector3 safeIdlePosition;
        private MinerState currentState = MinerState.Idle;
        private int minerIndex;
        private float depositTimer;
        private float idleRetryTimer;

        public MinerState CurrentState => currentState;
        public MineralOreDeposit TargetOre => targetOre;
        public int CurrentInventory => miningController != null ? miningController.CurrentInventory : 0;
        public int MaxInventoryCapacity => miningController != null ? miningController.MaxInventoryCapacity : 100;

        private void Awake()
        {
            if (movement == null) movement = GetComponent<MinerMovement>();
            if (miningController == null) miningController = GetComponent<MinerMiningController>();
            if (animatorController == null) animatorController = GetComponent<MinerAnimatorController>();
        }

        private void OnEnable()
        {
            if (miningController != null)
            {
                miningController.OnOreDepleted += HandleOreDepleted;
                miningController.OnInventoryFull += HandleInventoryFull;
            }
        }

        private void OnDisable()
        {
            if (miningController != null)
            {
                miningController.OnOreDepleted -= HandleOreDepleted;
                miningController.OnInventoryFull -= HandleInventoryFull;
            }

            ReleaseTargetOre();
        }

        public void Initialize(
            MineralOreRegistry registry,
            TerrainHeightSampler terrainSampler,
            GoldManager goldManager,
            Transform playerStatue,
            Vector3 entryPosition,
            Vector3 idlePosition,
            int spawnIndex,
            Vector3? customDepositPosition = null)
        {
            oreRegistry = registry;
            playerStatueTransform = playerStatue;
            baseEntryPosition = entryPosition;
            minerIndex = spawnIndex;

            // Offset safe idle position slightly in front of Player_Statue so idle miners never overlap.
            float idleOffsetX = (spawnIndex % 3) * 0.85f;
            float idleOffsetZ = ((spawnIndex / 3) % 2 == 0 ? -0.55f : 0.55f);
            safeIdlePosition = idlePosition + new Vector3(idleOffsetX, 0f, idleOffsetZ);

            // Resolve rear deposit point behind Player_Statue.
            Vector3 resolvedDeposit = customDepositPosition.HasValue
                ? customDepositPosition.Value
                : ResolveDefaultDepositPosition(playerStatue);
            float depositOffsetZ = ((spawnIndex % 3) - 1) * 0.35f;
            depositPosition = resolvedDeposit + new Vector3(0f, 0f, depositOffsetZ);

            if (movement != null)
            {
                movement.Initialize(terrainSampler, playerStatue);
            }

            if (miningController != null)
            {
                miningController.Initialize(goldManager);
            }

            EnterBase();
        }

        private static Vector3 ResolveDefaultDepositPosition(Transform playerStatue)
        {
            GameObject namedPoint = GameObject.Find("Player_Statue_DepositPoint");
            if (namedPoint != null)
            {
                return namedPoint.transform.position;
            }

            if (playerStatue != null)
            {
                Vector3 behindDir = -playerStatue.forward;
                behindDir.y = 0f;
                if (behindDir.sqrMagnitude < 0.001f)
                {
                    behindDir = Vector3.left;
                }
                else
                {
                    behindDir.Normalize();
                }

                return playerStatue.position + behindDir * 3.0f;
            }

            return new Vector3(-1.85f, 0f, 1.45f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            switch (currentState)
            {
                case MinerState.EnteringBase:
                    TickEnteringBase(dt);
                    break;

                case MinerState.MovingToOre:
                    TickMovingToOre(dt);
                    break;

                case MinerState.Mining:
                    if (miningController != null)
                    {
                        miningController.TickMining(dt);
                    }
                    break;

                case MinerState.MovingToDeposit:
                    TickMovingToDeposit(dt);
                    break;

                case MinerState.Depositing:
                    TickDepositing(dt);
                    break;

                case MinerState.ReturningToIdle:
                    TickReturningToIdle(dt);
                    break;

                case MinerState.Idle:
                    TickIdle(dt);
                    break;
            }
        }

        private void EnterBase()
        {
            currentState = MinerState.EnteringBase;
            if (animatorController != null)
            {
                animatorController.SetMining(false);
                animatorController.SetCarrying(false);
            }

            if (movement != null)
            {
                movement.SetDestination(baseEntryPosition);
            }
        }

        private void TickEnteringBase(float dt)
        {
            if (movement == null)
            {
                AcquireNextOreOrIdle();
                return;
            }

            bool arrived = movement.TickMovement(dt);
            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(movement.CurrentSpeed);
            }

            if (arrived)
            {
                AcquireNextOreOrIdle();
            }
        }

        private void AcquireNextOreOrIdle()
        {
            // Keep existing individual assignment if that ore is still available.
            if (targetOre != null && !targetOre.IsDepleted && targetOre.IsMinerAssigned(this))
            {
                MoveToAssignedOre();
                return;
            }

            ReleaseTargetOre();

            if (oreRegistry != null &&
                oreRegistry.TryAssignBestAvailableOre(this, transform.position, out MineralOreDeposit nextOre))
            {
                targetOre = nextOre;
                MoveToAssignedOre();
                return;
            }

            // No ore available: if carrying pending resources, deposit them before waiting.
            if (miningController != null && miningController.HasResources)
            {
                StartMovingToDeposit();
                return;
            }

            GoToSafeIdle();
        }

        private void MoveToAssignedOre()
        {
            if (targetOre == null || targetOre.IsDepleted)
            {
                ReleaseTargetOre();
                AcquireNextOreOrIdle();
                return;
            }

            Vector3 standPos = ComputeMiningStandPosition(targetOre);
            currentState = MinerState.MovingToOre;

            if (animatorController != null)
            {
                animatorController.SetMining(false);
                animatorController.SetCarrying(false);
            }

            if (movement != null)
            {
                movement.SetDestination(standPos);
            }
        }

        private void TickMovingToOre(float dt)
        {
            if (targetOre == null || targetOre.IsDepleted)
            {
                ReleaseTargetOre();
                AcquireNextOreOrIdle();
                return;
            }

            if (movement == null)
            {
                BeginMiningTarget();
                return;
            }

            bool arrived = movement.TickMovement(dt);
            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(movement.CurrentSpeed);
            }

            if (arrived)
            {
                BeginMiningTarget();
            }
        }

        private void BeginMiningTarget()
        {
            if (targetOre == null || targetOre.IsDepleted)
            {
                ReleaseTargetOre();
                AcquireNextOreOrIdle();
                return;
            }

            if (miningController != null && miningController.IsInventoryFull)
            {
                StartMovingToDeposit();
                return;
            }

            currentState = MinerState.Mining;
            if (movement != null)
            {
                movement.Stop();
            }

            if (miningController != null)
            {
                miningController.StartMining(targetOre);
            }
        }

        private void HandleInventoryFull()
        {
            // Keep targetOre assigned so this miner returns to its own ore after depositing.
            StartMovingToDeposit();
        }

        private void HandleOreDepleted(MineralOreDeposit depletedOre)
        {
            ReleaseTargetOre();

            if (miningController != null && miningController.IsInventoryFull)
            {
                StartMovingToDeposit();
                return;
            }

            AcquireNextOreOrIdle();
        }

        private void StartMovingToDeposit()
        {
            if (miningController != null)
            {
                miningController.StopMining();
            }

            currentState = MinerState.MovingToDeposit;

            if (animatorController != null)
            {
                animatorController.SetMining(false);
                animatorController.SetCarrying(true);
            }

            if (movement != null)
            {
                movement.SetDestination(depositPosition);
            }
            else
            {
                BeginDepositing();
            }
        }

        private void TickMovingToDeposit(float dt)
        {
            if (movement == null)
            {
                BeginDepositing();
                return;
            }

            bool arrived = movement.TickMovement(dt);
            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(movement.CurrentSpeed);
            }

            if (arrived)
            {
                BeginDepositing();
            }
        }

        private void BeginDepositing()
        {
            currentState = MinerState.Depositing;
            depositTimer = depositDuration;

            if (movement != null)
            {
                movement.Stop();
            }

            if (animatorController != null)
            {
                animatorController.SetCarrying(false);
                animatorController.SetMoveSpeed(0f);
                animatorController.SetMining(false);
                animatorController.TriggerDeposit();
            }
        }

        private void TickDepositing(float dt)
        {
            if (movement != null && playerStatueTransform != null)
            {
                movement.FacePosition(playerStatueTransform.position, dt);
            }

            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(0f);
            }

            depositTimer -= dt;
            if (depositTimer <= 0f)
            {
                if (miningController != null)
                {
                    miningController.DepositAllResources();
                }

                AcquireNextOreOrIdle();
            }
        }

        private void GoToSafeIdle()
        {
            ReleaseTargetOre();

            if (miningController != null)
            {
                miningController.StopMining();
            }

            if (animatorController != null)
            {
                animatorController.SetCarrying(false);
                animatorController.SetMining(false);
            }

            Vector3 delta = safeIdlePosition - transform.position;
            delta.y = 0f;
            if (delta.magnitude <= 0.35f || movement == null)
            {
                EnterIdleState();
                return;
            }

            currentState = MinerState.ReturningToIdle;
            movement.SetDestination(safeIdlePosition);
        }

        private void TickReturningToIdle(float dt)
        {
            if (movement == null)
            {
                EnterIdleState();
                return;
            }

            bool arrived = movement.TickMovement(dt);
            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(movement.CurrentSpeed);
            }

            if (arrived)
            {
                EnterIdleState();
            }
        }

        private void EnterIdleState()
        {
            currentState = MinerState.Idle;
            idleRetryTimer = idleRetryInterval;

            if (movement != null)
            {
                movement.Stop();
            }

            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(0f);
                animatorController.SetMining(false);
                animatorController.SetCarrying(false);
            }
        }

        private void TickIdle(float dt)
        {
            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(0f);
                animatorController.SetMining(false);
                animatorController.SetCarrying(false);
            }

            // Turn around to face out toward the terrain and center of the map (+X).
            if (movement != null)
            {
                movement.FaceDirection(Vector3.right, dt);
            }

            // Periodically check if an ore has become available (new ore or freed slot).
            idleRetryTimer -= dt;
            if (idleRetryTimer <= 0f)
            {
                idleRetryTimer = idleRetryInterval;
                if (oreRegistry != null && oreRegistry.HasAvailableOre())
                {
                    AcquireNextOreOrIdle();
                }
            }
        }

        private Vector3 ComputeMiningStandPosition(MineralOreDeposit ore)
        {
            Vector3 orePos = ore.transform.position;

            // Default approach direction faces the ore from the player base side (-X) with
            // deterministic angular separation for Slot 0 and Slot 1 so two miners stand cleanly side by side.
            Vector3 baseDir = Vector3.left;
            int slotIndex = ore.GetMinerSlotIndex(this);
            float angleDeg = slotIndex == 0 ? -32f : 32f;
            Vector3 standDir = (Quaternion.Euler(0f, angleDeg, 0f) * baseDir).normalized;

            return orePos + standDir * miningStandDistance;
        }

        private void ReleaseTargetOre()
        {
            if (targetOre != null)
            {
                targetOre.UnassignMiner(this);
                targetOre = null;
            }
        }
    }
}
