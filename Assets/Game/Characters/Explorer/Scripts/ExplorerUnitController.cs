using System;
using Game.Systems.Army;
using Game.Systems.Combat;
using Game.Systems.Mining;
using UnityEngine;

namespace Game.Characters.Explorer
{
    /// <summary>
    /// Autonomous unit controller for the Explorer soldier:
    /// - Enters from behind the camera and marches into its assigned 4-per-row formation slot
    /// - Stops cleanly in Idle facing the enemy (+X) upon reaching its slot
    /// - Responds to Attack, FormRanks, and HideArmy commands cleanly and orderly
    /// - Engages opposing-faction targets via CombatRegistry (zero friendly fire)
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnitHealth))]
    public class ExplorerUnitController : MonoBehaviour
    {
        public enum ExplorerState
        {
            MovingToFormation,
            IdleInFormation,
            AdvancingToAttack,
            MovingToTarget,
            Attacking,
            MovingToDefense,
            Defending,
            Dead
        }

        [Header("Movement")]
        [SerializeField, Min(0.5f)] private float walkSpeed = 3.2f;
        [SerializeField, Min(1f)] private float rotationSpeed = 10f;
        [SerializeField, Min(0.05f)] private float arrivalThreshold = 0.14f;
        [SerializeField] private float minWalkableZ = -2.45f;
        [SerializeField] private float maxWalkableZ = 2.25f;

        [Header("Combat")]
        [SerializeField, Min(0.5f)] private float detectionRange = 14f;
        [SerializeField, Min(0.5f)] private float attackRange = 1.65f;
        [SerializeField, Min(0.5f)] private float structureAttackRange = 2.6f;
        [SerializeField, Min(1f)] private float attackDamage = 18f;
        [SerializeField, Min(0.2f)] private float attackCooldown = 1.15f;
        [SerializeField] private float enemyZoneStopX = 91.5f;
        [SerializeField, Min(0.2f)] private float deathDestroyDelay = 1.8f;

        [Header("Components")]
        [SerializeField] private ExplorerAnimatorController animatorController;
        [SerializeField] private UnitHealth unitHealth;

        private TerrainHeightSampler terrainSampler;
        private ExplorerState currentState = ExplorerState.IdleInFormation;
        private ArmyOrder currentOrder = ArmyOrder.FormRanks;
        private Vector3 assignedSlotPosition;
        private bool hasAssignedSlot;
        private ICombatTarget currentTarget;
        private float attackTimer;
        private float laneOffsetZ;

        public ExplorerState CurrentState => currentState;
        public ArmyOrder CurrentOrder => currentOrder;
        public Vector3 AssignedSlotPosition => assignedSlotPosition;
        public bool IsAlive => unitHealth == null || unitHealth.IsAlive;

        public event Action<ExplorerUnitController> OnExplorerDied;

        private void Awake()
        {
            if (animatorController == null) animatorController = GetComponent<ExplorerAnimatorController>();
            if (unitHealth == null) unitHealth = GetComponent<UnitHealth>();
        }

        private void OnEnable()
        {
            if (unitHealth != null)
            {
                unitHealth.OnDamaged += HandleDamaged;
                unitHealth.OnDied += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (unitHealth != null)
            {
                unitHealth.OnDamaged -= HandleDamaged;
                unitHealth.OnDied -= HandleDied;
            }
        }

        public void Initialize(TerrainHeightSampler sampler, Vector3 initialSlotPosition, ArmyOrder initialOrder)
        {
            terrainSampler = sampler;
            assignedSlotPosition = ClampToCorridor(initialSlotPosition);
            hasAssignedSlot = true;
            laneOffsetZ = assignedSlotPosition.z;
            SnapToGround();
            ApplyOrder(initialOrder, assignedSlotPosition);
        }

        public void SetFormationSlot(Vector3 slotPosition, bool moveImmediately)
        {
            if (currentState == ExplorerState.Dead)
            {
                return;
            }

            assignedSlotPosition = ClampToCorridor(slotPosition);
            hasAssignedSlot = true;
            laneOffsetZ = assignedSlotPosition.z;

            if (!moveImmediately)
            {
                return;
            }

            if (currentOrder == ArmyOrder.FormRanks)
            {
                if (HorizontalDistance(transform.position, assignedSlotPosition) > arrivalThreshold)
                {
                    currentState = ExplorerState.MovingToFormation;
                }
                else
                {
                    EnterIdleInFormation();
                }
            }
            else if (currentOrder == ArmyOrder.HideArmy)
            {
                if (HorizontalDistance(transform.position, assignedSlotPosition) > arrivalThreshold)
                {
                    currentState = ExplorerState.MovingToDefense;
                }
                else
                {
                    EnterDefending();
                }
            }
        }

        public void ApplyOrder(ArmyOrder order, Vector3 slotPosition)
        {
            if (currentState == ExplorerState.Dead)
            {
                return;
            }

            currentOrder = order;
            assignedSlotPosition = ClampToCorridor(slotPosition);
            hasAssignedSlot = true;
            laneOffsetZ = assignedSlotPosition.z;

            switch (order)
            {
                case ArmyOrder.FormRanks:
                    currentTarget = null;
                    if (HorizontalDistance(transform.position, assignedSlotPosition) > arrivalThreshold)
                    {
                        currentState = ExplorerState.MovingToFormation;
                    }
                    else
                    {
                        EnterIdleInFormation();
                    }
                    break;

                case ArmyOrder.Attack:
                    currentState = ExplorerState.AdvancingToAttack;
                    break;

                case ArmyOrder.HideArmy:
                    currentTarget = null;
                    if (HorizontalDistance(transform.position, assignedSlotPosition) > arrivalThreshold)
                    {
                        currentState = ExplorerState.MovingToDefense;
                    }
                    else
                    {
                        EnterDefending();
                    }
                    break;
            }
        }

        private void Update()
        {
            if (currentState == ExplorerState.Dead)
            {
                return;
            }

            float dt = Time.deltaTime;
            if (attackTimer > 0f)
            {
                attackTimer -= dt;
            }

            switch (currentState)
            {
                case ExplorerState.MovingToFormation:
                    TickMoveToSlot(dt, isDefense: false);
                    break;

                case ExplorerState.IdleInFormation:
                    TickHoldPosition(dt);
                    break;

                case ExplorerState.MovingToDefense:
                    TickMoveToSlot(dt, isDefense: true);
                    break;

                case ExplorerState.Defending:
                    TickHoldPosition(dt);
                    break;

                case ExplorerState.AdvancingToAttack:
                    TickAdvancingToAttack(dt);
                    break;

                case ExplorerState.MovingToTarget:
                    TickMovingToTarget(dt);
                    break;

                case ExplorerState.Attacking:
                    TickAttacking(dt);
                    break;
            }
        }

        private void TickMoveToSlot(float dt, bool isDefense)
        {
            if (!hasAssignedSlot)
            {
                if (isDefense) EnterDefending();
                else EnterIdleInFormation();
                return;
            }

            bool arrived = MoveTowards(assignedSlotPosition, dt);
            if (arrived)
            {
                // Snap exactly to slot X/Z so the unit never jitters or drifts.
                Vector3 pos = transform.position;
                pos.x = assignedSlotPosition.x;
                pos.z = assignedSlotPosition.z;
                if (terrainSampler != null)
                {
                    pos.y = terrainSampler.SampleHeight(pos);
                }
                transform.position = pos;

                if (isDefense) EnterDefending();
                else EnterIdleInFormation();
            }
        }

        private void EnterIdleInFormation()
        {
            currentState = ExplorerState.IdleInFormation;
            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(0f);
            }
        }

        private void EnterDefending()
        {
            currentState = ExplorerState.Defending;
            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(0f);
            }
        }

        private void TickHoldPosition(float dt)
        {
            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(0f);
            }

            // Always face the enemy side (+X) cleanly when stopped.
            FaceDirection(Vector3.right, dt);
        }

        private void TickAdvancingToAttack(float dt)
        {
            TeamFaction myFaction = unitHealth != null ? unitHealth.Faction : TeamFaction.Ally;

            // Scan for nearby enemies first; if none in immediate range, check enemy statue.
            ICombatTarget nearby = CombatRegistry.FindClosestEnemy(transform.position, myFaction, detectionRange, preferUnitsOverStructures: true);
            if (nearby != null)
            {
                currentTarget = nearby;
                currentState = ExplorerState.MovingToTarget;
                return;
            }

            // Advance orderly along this soldier's Z lane toward the enemy zone (+X).
            Vector3 marchTarget = new Vector3(enemyZoneStopX, transform.position.y, laneOffsetZ);
            bool reachedEnemyZone = MoveTowards(marchTarget, dt);

            if (reachedEnemyZone)
            {
                ICombatTarget enemyStatue = CombatRegistry.FindEnemyStatue(myFaction);
                if (enemyStatue != null && enemyStatue.IsAlive)
                {
                    currentTarget = enemyStatue;
                    currentState = ExplorerState.MovingToTarget;
                }
                else
                {
                    if (animatorController != null)
                    {
                        animatorController.SetMoveSpeed(0f);
                    }
                    FaceDirection(Vector3.right, dt);
                }
            }
        }

        private void TickMovingToTarget(float dt)
        {
            TeamFaction myFaction = unitHealth != null ? unitHealth.Faction : TeamFaction.Ally;

            if (currentTarget == null || !currentTarget.IsAlive || currentTarget.TargetTransform == null)
            {
                currentTarget = CombatRegistry.FindClosestEnemy(transform.position, myFaction, detectionRange, preferUnitsOverStructures: true);
                if (currentTarget == null)
                {
                    currentState = ExplorerState.AdvancingToAttack;
                    return;
                }
            }

            float effectiveRange = currentTarget.IsStructure ? structureAttackRange : attackRange;
            Vector3 targetPos = ClampToCorridor(currentTarget.TargetPosition);
            float dist = HorizontalDistance(transform.position, targetPos);

            if (dist <= effectiveRange)
            {
                currentState = ExplorerState.Attacking;
                if (animatorController != null)
                {
                    animatorController.SetMoveSpeed(0f);
                }
                return;
            }

            MoveTowards(targetPos, dt);
        }

        private void TickAttacking(float dt)
        {
            TeamFaction myFaction = unitHealth != null ? unitHealth.Faction : TeamFaction.Ally;

            if (currentTarget == null || !currentTarget.IsAlive || currentTarget.TargetTransform == null)
            {
                currentTarget = null;
                currentState = ExplorerState.AdvancingToAttack;
                return;
            }

            float effectiveRange = currentTarget.IsStructure ? structureAttackRange : attackRange;
            float dist = HorizontalDistance(transform.position, currentTarget.TargetPosition);
            if (dist > effectiveRange * 1.15f)
            {
                currentState = ExplorerState.MovingToTarget;
                return;
            }

            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(0f);
            }

            FaceDirection(currentTarget.TargetPosition - transform.position, dt);

            if (attackTimer <= 0f)
            {
                attackTimer = attackCooldown;
                if (animatorController != null)
                {
                    animatorController.TriggerAttack();
                }

                currentTarget.TakeDamage(attackDamage, myFaction);
            }
        }

        private bool MoveTowards(Vector3 targetWorldPos, float dt)
        {
            Vector3 currentPos = transform.position;
            Vector3 toTarget = targetWorldPos - currentPos;
            toTarget.y = 0f;

            float remaining = toTarget.magnitude;
            if (remaining <= arrivalThreshold)
            {
                if (animatorController != null)
                {
                    animatorController.SetMoveSpeed(0f);
                }
                return true;
            }

            Vector3 dir = toTarget / remaining;
            float step = Mathf.Min(walkSpeed * dt, remaining);
            Vector3 nextPos = currentPos + dir * step;
            nextPos.z = Mathf.Clamp(nextPos.z, minWalkableZ, maxWalkableZ);

            if (terrainSampler != null)
            {
                float groundY = terrainSampler.SampleHeight(nextPos);
                nextPos.y = Mathf.Lerp(currentPos.y, groundY, Mathf.Clamp01(dt * 16f));
            }

            transform.position = nextPos;
            FaceDirection(dir, dt);

            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(step / Mathf.Max(0.0001f, dt));
            }

            return false;
        }

        private void FaceDirection(Vector3 dir, float dt)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(rotationSpeed * dt));
        }

        private Vector3 ClampToCorridor(Vector3 pos)
        {
            pos.z = Mathf.Clamp(pos.z, minWalkableZ, maxWalkableZ);
            return pos;
        }

        private void SnapToGround()
        {
            if (terrainSampler == null)
            {
                return;
            }

            Vector3 pos = transform.position;
            pos.y = terrainSampler.SampleHeight(pos);
            transform.position = pos;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private void HandleDamaged(float amount)
        {
            if (currentState != ExplorerState.Dead && animatorController != null)
            {
                animatorController.TriggerHurt();
            }
        }

        private void HandleDied()
        {
            if (currentState == ExplorerState.Dead)
            {
                return;
            }

            currentState = ExplorerState.Dead;
            currentTarget = null;

            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(0f);
                animatorController.TriggerDeath();
            }

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            OnExplorerDied?.Invoke(this);
            Destroy(gameObject, deathDestroyDelay);
        }
    }
}
