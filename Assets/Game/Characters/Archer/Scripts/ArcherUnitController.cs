using System;
using Game.Systems.Combat;
using Game.Systems.Mining;
using UnityEngine;

namespace Game.Characters.Archer
{
    /// <summary>
    /// Defensive Archer unit controller:
    /// - Enters from offscreen and moves to its assigned defensive slot behind the allied barricade
    /// - Stays stationary in Idle facing the enemy (+X) once positioned
    /// - Fires stylized ArcherProjectile arrows at opposing-faction targets within range
    /// - Protected from friendly fire via UnitHealth (TeamFaction.Ally), while detectable/attackable by Enemy units
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnitHealth))]
    public class ArcherUnitController : MonoBehaviour
    {
        public enum ArcherState
        {
            MovingToDefenseSlot,
            Guarding,
            Dead
        }

        [Header("Movement")]
        [SerializeField, Min(0.5f)] private float walkSpeed = 2.8f;
        [SerializeField, Min(1f)] private float rotationSpeed = 10f;
        [SerializeField, Min(0.05f)] private float arrivalThreshold = 0.14f;
        [SerializeField] private float minWalkableZ = -2.45f;
        [SerializeField] private float maxWalkableZ = 2.25f;

        [Header("Ranged Combat")]
        [SerializeField, Min(2f)] private float attackRange = 22f;
        [SerializeField, Min(1f)] private float arrowDamage = 14f;
        [SerializeField, Min(0.3f)] private float fireCooldown = 1.85f;
        [SerializeField, Min(4f)] private float projectileSpeed = 20f;
        [SerializeField] private Vector3 projectileSpawnOffset = new Vector3(0.45f, 1.35f, 0f);
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField, Min(0.2f)] private float deathDestroyDelay = 1.8f;

        [Header("Components")]
        [SerializeField] private ArcherAnimatorController animatorController;
        [SerializeField] private UnitHealth unitHealth;

        private TerrainHeightSampler terrainSampler;
        private ArcherState currentState = ArcherState.MovingToDefenseSlot;
        private Vector3 assignedDefenseSlot;
        private int assignedSlotIndex = -1;
        private float fireTimer;

        public ArcherState CurrentState => currentState;
        public int AssignedSlotIndex => assignedSlotIndex;
        public Vector3 AssignedDefenseSlot => assignedDefenseSlot;
        public bool IsAlive => unitHealth == null || unitHealth.IsAlive;

        public event Action<ArcherUnitController> OnArcherDied;

        private void Awake()
        {
            if (animatorController == null) animatorController = GetComponent<ArcherAnimatorController>();
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

        public void Initialize(
            TerrainHeightSampler sampler,
            Vector3 defenseSlotPos,
            int slotIndex,
            GameObject arrowPrefab = null)
        {
            terrainSampler = sampler;
            assignedDefenseSlot = new Vector3(
                defenseSlotPos.x,
                defenseSlotPos.y,
                Mathf.Clamp(defenseSlotPos.z, minWalkableZ, maxWalkableZ));
            assignedSlotIndex = slotIndex;

            if (arrowPrefab != null)
            {
                projectilePrefab = arrowPrefab;
            }

            if (terrainSampler != null)
            {
                Vector3 pos = transform.position;
                pos.y = terrainSampler.SampleHeight(pos);
                transform.position = pos;
            }

            currentState = ArcherState.MovingToDefenseSlot;
        }

        private void Update()
        {
            if (currentState == ArcherState.Dead)
            {
                return;
            }

            float dt = Time.deltaTime;
            if (fireTimer > 0f)
            {
                fireTimer -= dt;
            }

            if (currentState == ArcherState.MovingToDefenseSlot)
            {
                TickMoveToDefenseSlot(dt);
            }
            else if (currentState == ArcherState.Guarding)
            {
                TickGuarding(dt);
            }
        }

        private void TickMoveToDefenseSlot(float dt)
        {
            Vector3 currentPos = transform.position;
            Vector3 toSlot = assignedDefenseSlot - currentPos;
            toSlot.y = 0f;
            float dist = toSlot.magnitude;

            if (dist <= arrivalThreshold)
            {
                Vector3 snapped = currentPos;
                snapped.x = assignedDefenseSlot.x;
                snapped.z = assignedDefenseSlot.z;
                if (terrainSampler != null)
                {
                    snapped.y = terrainSampler.SampleHeight(snapped);
                }
                transform.position = snapped;

                currentState = ArcherState.Guarding;
                if (animatorController != null)
                {
                    animatorController.SetMoveSpeed(0f);
                }
                return;
            }

            Vector3 dir = toSlot / dist;
            float step = Mathf.Min(walkSpeed * dt, dist);
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
        }

        private void TickGuarding(float dt)
        {
            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(0f);
            }

            TeamFaction myFaction = unitHealth != null ? unitHealth.Faction : TeamFaction.Ally;
            ICombatTarget enemy = CombatRegistry.FindClosestEnemy(transform.position, myFaction, attackRange, preferUnitsOverStructures: true);

            if (enemy != null && enemy.IsAlive)
            {
                FaceDirection(enemy.TargetPosition - transform.position, dt);

                if (fireTimer <= 0f)
                {
                    fireTimer = fireCooldown;
                    FireArrowAt(enemy, myFaction);
                }
            }
            else
            {
                // Always watch the enemy side (+X) while idle behind the barricade.
                FaceDirection(Vector3.right, dt);
            }
        }

        private void FireArrowAt(ICombatTarget target, TeamFaction myFaction)
        {
            if (animatorController != null)
            {
                animatorController.TriggerAttack();
            }

            Vector3 spawnPos = transform.position + transform.TransformDirection(projectileSpawnOffset);
            ArcherProjectile projectile = null;

            if (projectilePrefab != null)
            {
                GameObject arrowGo = Instantiate(projectilePrefab, spawnPos, transform.rotation);
                projectile = arrowGo.GetComponent<ArcherProjectile>();
                if (projectile == null)
                {
                    projectile = arrowGo.AddComponent<ArcherProjectile>();
                }
            }
            else
            {
                projectile = CreateFallbackArrow(spawnPos);
            }

            projectile.Launch(spawnPos, target, arrowDamage, myFaction, projectileSpeed);
        }

        private static ArcherProjectile CreateFallbackArrow(Vector3 origin)
        {
            GameObject arrowGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrowGo.name = "Archer_Arrow_Projectile";
            arrowGo.transform.position = origin;
            arrowGo.transform.localScale = new Vector3(0.08f, 0.08f, 0.72f);

            Collider col = arrowGo.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            Renderer rend = arrowGo.GetComponent<Renderer>();
            if (rend != null)
            {
                Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
                Material mat = new Material(urpLit != null ? urpLit : Shader.Find("Standard"));
                mat.color = new Color(0.25f, 0.92f, 1f, 1f);
                rend.material = mat;
            }

            return arrowGo.AddComponent<ArcherProjectile>();
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

        private void HandleDamaged(float amount)
        {
            if (currentState != ArcherState.Dead && animatorController != null)
            {
                animatorController.TriggerHurt();
            }
        }

        private void HandleDied()
        {
            if (currentState == ArcherState.Dead)
            {
                return;
            }

            currentState = ArcherState.Dead;
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

            OnArcherDied?.Invoke(this);
            Destroy(gameObject, deathDestroyDelay);
        }
    }
}
