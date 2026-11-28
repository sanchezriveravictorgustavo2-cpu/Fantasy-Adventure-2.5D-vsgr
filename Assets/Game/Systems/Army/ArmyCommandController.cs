using System;
using UnityEngine;

namespace Game.Systems.Army
{
    /// <summary>
    /// Central command dispatcher for the player's army:
    /// - OrderAttack(): Explorers abandon formation and advance/attack; Archer spawning stops (existing Archers stay).
    /// - OrderFormRanks(): Explorers return to forward 4-per-row formation and hold position facing enemy.
    /// - OrderHideArmy(): Explorers retreat in 4-per-row formation behind our barricade; defensive Archer spawning starts (every 10s).
    /// </summary>
    [DisallowMultipleComponent]
    public class ArmyCommandController : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private ArmyFormationController formationController;
        [SerializeField] private ArcherSpawnController archerSpawnController;

        private ArmyOrder currentOrder = ArmyOrder.FormRanks;

        public ArmyOrder CurrentOrder => currentOrder;

        public event Action<ArmyOrder> OnOrderChanged;

        private void Awake()
        {
            if (formationController == null)
            {
                formationController = FindFirstObjectByType<ArmyFormationController>();
            }

            if (archerSpawnController == null)
            {
                archerSpawnController = FindFirstObjectByType<ArcherSpawnController>();
            }
        }

        private void Start()
        {
            if (archerSpawnController != null)
            {
                archerSpawnController.SetDefensiveSpawningActive(currentOrder == ArmyOrder.HideArmy);
            }

            OnOrderChanged?.Invoke(currentOrder);
        }

        public void OrderAttack()
        {
            SetOrder(ArmyOrder.Attack);
        }

        public void OrderFormRanks()
        {
            SetOrder(ArmyOrder.FormRanks);
        }

        public void OrderHideArmy()
        {
            SetOrder(ArmyOrder.HideArmy);
        }

        public void SetOrder(ArmyOrder newOrder)
        {
            currentOrder = newOrder;

            if (formationController != null)
            {
                formationController.ApplyOrderToAll(currentOrder);
            }

            if (archerSpawnController != null)
            {
                // Defensive Archer spawning runs while hidden; stops when attacking (or forming forward ranks),
                // preserving all already-spawned Archers in their defensive positions.
                archerSpawnController.SetDefensiveSpawningActive(currentOrder == ArmyOrder.HideArmy);
            }

            OnOrderChanged?.Invoke(currentOrder);
        }

        public void ConfigureReferences(ArmyFormationController formation, ArcherSpawnController archerSpawner)
        {
            formationController = formation;
            archerSpawnController = archerSpawner;
        }
    }
}
