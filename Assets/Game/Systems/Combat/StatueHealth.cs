using System;
using UnityEngine;

namespace Game.Systems.Combat
{
    /// <summary>
    /// Health and destruction state controller for Player_Statue and Enemy_Statue.
    /// Does not modify the statue mesh, materials, or world transform.
    /// </summary>
    [DisallowMultipleComponent]
    public class StatueHealth : MonoBehaviour, ICombatTarget
    {
        [Header("Statue Configuration")]
        [SerializeField] private TeamFaction faction = TeamFaction.Ally;
        [SerializeField, Min(1f)] private float maxHealth = 1000f;
        [SerializeField] private float targetHeightOffset = 2.2f;

        [Header("Health Bar")]
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 5.6f, 0f);
        [SerializeField] private Vector2 healthBarSize = new Vector2(3.2f, 0.34f);

        [Header("State (Read-Only)")]
        [SerializeField] private float currentHealth;
        [SerializeField] private bool isDestroyed;

        private WorldHealthBarUI healthBar;

        public TeamFaction Faction => faction;
        public bool IsAlive => !isDestroyed && currentHealth > 0f;
        public bool IsStructure => true;
        public bool IsDestroyed => isDestroyed;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float NormalizedHealth => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        public Vector3 TargetPosition => transform.position + Vector3.up * targetHeightOffset;
        public Transform TargetTransform => transform;

        public event Action<StatueHealth, float, float> OnHealthChanged;
        public event Action<StatueHealth, float> OnDamaged;
        public event Action<StatueHealth> OnStatueDestroyed;

        private void Awake()
        {
            currentHealth = Mathf.Max(1f, maxHealth);
            isDestroyed = false;
            EnsureHealthBar();
        }

        private void OnEnable()
        {
            CombatRegistry.Register(this);
            UpdateHealthBar();
        }

        private void OnDisable()
        {
            CombatRegistry.Unregister(this);
        }

        public void Configure(TeamFaction statueFaction, float configuredMaxHealth, Vector3 barOffset)
        {
            faction = statueFaction;
            maxHealth = Mathf.Max(1f, configuredMaxHealth);
            currentHealth = maxHealth;
            isDestroyed = false;
            healthBarOffset = barOffset;

            EnsureHealthBar();
            UpdateHealthBar();
        }

        public bool TakeDamage(float amount, TeamFaction attackerFaction)
        {
            if (isDestroyed || amount <= 0f)
            {
                return false;
            }

            // Prevent friendly fire against own statue.
            if (attackerFaction == faction)
            {
                return false;
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            UpdateHealthBar();
            OnDamaged?.Invoke(this, amount);
            OnHealthChanged?.Invoke(this, currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                TriggerDestructionState();
            }

            return true;
        }

        public void ResetHealth()
        {
            currentHealth = Mathf.Max(1f, maxHealth);
            isDestroyed = false;
            CombatRegistry.Register(this);
            UpdateHealthBar();
            OnHealthChanged?.Invoke(this, currentHealth, maxHealth);
        }

        private void TriggerDestructionState()
        {
            if (isDestroyed)
            {
                return;
            }

            isDestroyed = true;
            CombatRegistry.Unregister(this);
            UpdateHealthBar();
            Debug.Log($"[StatueHealth] {gameObject.name} ({faction}) reached 0 HP -> Destruction/Defeat state activated.", this);
            OnStatueDestroyed?.Invoke(this);
        }

        private void EnsureHealthBar()
        {
            if (healthBar == null)
            {
                healthBar = GetComponent<WorldHealthBarUI>();
            }

            if (healthBar == null)
            {
                healthBar = gameObject.AddComponent<WorldHealthBarUI>();
            }

            healthBar.Initialize(faction, healthBarOffset, healthBarSize, hideAtFull: false);
        }

        private void UpdateHealthBar()
        {
            if (healthBar != null)
            {
                healthBar.SetNormalizedHealth(NormalizedHealth);
            }
        }
    }
}
