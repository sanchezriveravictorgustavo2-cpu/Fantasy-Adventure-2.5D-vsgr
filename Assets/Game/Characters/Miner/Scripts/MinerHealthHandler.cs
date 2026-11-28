using Game.Systems.Combat;
using UnityEngine;

namespace Game.Characters.Miner
{
    /// <summary>
    /// Connects UnitHealth death events to MinerUnitController without modifying
    /// any existing mining, movement, or economy scripts.
    /// Ensures ore assignments are immediately released when a miner is defeated.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnitHealth))]
    public class MinerHealthHandler : MonoBehaviour
    {
        [SerializeField, Min(0.2f)] private float deathDestroyDelay = 1.6f;

        private UnitHealth unitHealth;
        private MinerUnitController unitController;
        private MinerMiningController miningController;
        private MinerMovement movement;
        private MinerAnimatorController animatorController;
        private bool deathHandled;

        private void Awake()
        {
            unitHealth = GetComponent<UnitHealth>();
            unitController = GetComponent<MinerUnitController>();
            miningController = GetComponent<MinerMiningController>();
            movement = GetComponent<MinerMovement>();
            animatorController = GetComponent<MinerAnimatorController>();
        }

        private void OnEnable()
        {
            if (unitHealth != null)
            {
                unitHealth.OnDied += HandleMinerDied;
            }
        }

        private void OnDisable()
        {
            if (unitHealth != null)
            {
                unitHealth.OnDied -= HandleMinerDied;
            }
        }

        private void HandleMinerDied()
        {
            if (deathHandled)
            {
                return;
            }

            deathHandled = true;

            if (miningController != null)
            {
                miningController.StopMining();
            }

            if (movement != null)
            {
                movement.Stop();
            }

            // Disabling MinerUnitController triggers OnDisable -> ReleaseTargetOre().
            if (unitController != null)
            {
                unitController.enabled = false;
            }

            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(0f);
                animatorController.SetMining(false);
                animatorController.SetCarrying(false);
                animatorController.TriggerDeath();
            }

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            Destroy(gameObject, deathDestroyDelay);
        }
    }
}
