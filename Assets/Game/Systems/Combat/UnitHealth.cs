using System;
using UnityEngine;

namespace Game.Systems.Combat
{
    /// <summary>
    /// Reusable health component for all combat and worker units (Miners, Explorers, Archers, Enemies).
    /// Strictly prevents friendly fire by rejecting damage from the same faction.
    /// </summary>
    [DisallowMultipleComponent]
    public class UnitHealth : MonoBehaviour, ICombatTarget
    {
        [Header("Health Configuration")]
        [SerializeField] private TeamFaction faction = TeamFaction.Ally;
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private float targetHeightOffset = 1.0f;

        [Header("Health Bar")]
        [SerializeField] private bool showHealthBar = true;
        [SerializeField] private bool hideHealthBarWhenFull = true;
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 2.35f, 0f);
        [SerializeField] private Vector2 healthBarSize = new Vector2(1.15f, 0.16f);

        private float currentHealth;
        private bool isDead;
        private WorldHealthBarUI healthBar;

        public TeamFaction Faction => faction;
        public bool IsAlive => !isDead && currentHealth > 0f;
        public bool IsStructure => false;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float NormalizedHealth => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        public Vector3 TargetPosition => transform.position + Vector3.up * targetHeightOffset;
        public Transform TargetTransform => transform;

        public event Action<float, float> OnHealthChanged;
        public event Action<float> OnDamaged;
        public event Action OnDied;

        private void Awake()
        {
            currentHealth = Mathf.Max(1f, maxHealth);
            isDead = false;

            if (showHealthBar)
            {
                EnsureHealthBar();
            }
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

        public void Configure(TeamFaction unitFaction, float initialMaxHealth, bool hideWhenFull = true)
        {
            faction = unitFaction;
            maxHealth = Mathf.Max(1f, initialMaxHealth);
            currentHealth = maxHealth;
            isDead = false;
            hideHealthBarWhenFull = hideWhenFull;

            if (showHealthBar)
            {
                EnsureHealthBar();
                UpdateHealthBar();
            }
        }

        public bool TakeDamage(float amount, TeamFaction attackerFaction)
        {
            if (isDead || amount <= 0f)
            {
                return false;
            }

            // Friendly fire protection: never allow same-faction damage.
            if (attackerFaction == faction)
            {
                return false;
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            UpdateHealthBar();
            OnDamaged?.Invoke(amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                Die();
            }

            return true;
        }

        public void Heal(float amount)
        {
            if (isDead || amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            UpdateHealthBar();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            CombatRegistry.Unregister(this);
            UpdateHealthBar();
            OnDied?.Invoke();
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

            healthBar.Initialize(faction, healthBarOffset, healthBarSize, hideHealthBarWhenFull);
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
