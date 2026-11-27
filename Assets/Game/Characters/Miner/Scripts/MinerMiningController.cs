using System;
using Game.Systems.Economy;
using Game.Worlds.World01.Structures;
using UnityEngine;

namespace Game.Characters.Miner
{
    /// <summary>
    /// Executes pickaxe mining hits driven by the Mine_Loop animation event.
    /// Accumulates 10 resource units per valid hit into the miner's personal inventory
    /// up to 100 units, then deposits into GoldManager when the miner reaches Player_Statue_DepositPoint.
    /// </summary>
    [DisallowMultipleComponent]
    public class MinerMiningController : MonoBehaviour
    {
        [Header("Mining Rewards & Inventory")]
        [Tooltip("Resource units added to the miner's inventory on each valid mining hit.")]
        [SerializeField, Min(1)] private int goldPerHit = 10;

        [Tooltip("Maximum resource capacity the miner can carry before returning to deposit.")]
        [SerializeField, Min(10)] private int maxInventoryCapacity = 100;

        [Tooltip("Minimum real-time seconds between registered hits (synchronized with slowed Mine_Loop).")]
        [SerializeField, Min(0.1f)] private float minHitInterval = 2.4f;

        [SerializeField] private MinerAnimatorController animatorController;
        [SerializeField] private MinerMovement movement;

        private GoldManager goldManager;
        private MineralOreDeposit activeOre;
        private bool isMining;
        private int currentInventory;
        private float lastHitTime = -100f;

        public bool IsMining => isMining;
        public MineralOreDeposit ActiveOre => activeOre;
        public int GoldPerHit => goldPerHit;
        public int CurrentInventory => currentInventory;
        public int MaxInventoryCapacity => maxInventoryCapacity;
        public bool IsInventoryFull => currentInventory >= maxInventoryCapacity;
        public bool HasResources => currentInventory > 0;
        public float MinHitInterval => minHitInterval;

        public event Action<MineralOreDeposit> OnOreDepleted;
        public event Action<int, int> OnInventoryChanged;
        public event Action OnInventoryFull;
        public event Action<int> OnResourcesDeposited;

        private void Awake()
        {
            if (animatorController == null)
            {
                animatorController = GetComponent<MinerAnimatorController>();
            }

            if (movement == null)
            {
                movement = GetComponent<MinerMovement>();
            }
        }

        private void OnEnable()
        {
            if (animatorController != null)
            {
                animatorController.OnMiningHitEvent += HandleMiningHitAnimationEvent;
            }
        }

        private void OnDisable()
        {
            if (animatorController != null)
            {
                animatorController.OnMiningHitEvent -= HandleMiningHitAnimationEvent;
            }
        }

        public void Initialize(GoldManager economy)
        {
            goldManager = economy;
            currentInventory = 0;
            lastHitTime = -100f;
            OnInventoryChanged?.Invoke(currentInventory, maxInventoryCapacity);
        }

        public void StartMining(MineralOreDeposit ore)
        {
            if (ore == null || ore.IsDepleted || IsInventoryFull)
            {
                StopMining();
                return;
            }

            activeOre = ore;
            isMining = true;
            lastHitTime = -100f;

            if (movement != null)
            {
                movement.Stop();
            }

            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(0f);
                animatorController.SetCarrying(false);
                animatorController.SetMining(true);
            }
        }

        public void StopMining()
        {
            isMining = false;
            activeOre = null;

            if (animatorController != null)
            {
                animatorController.SetMining(false);
            }
        }

        public void TickMining(float deltaTime)
        {
            if (!isMining)
            {
                return;
            }

            if (IsInventoryFull)
            {
                StopMining();
                OnInventoryFull?.Invoke();
                return;
            }

            if (activeOre == null || activeOre.IsDepleted)
            {
                MineralOreDeposit finished = activeOre;
                StopMining();
                OnOreDepleted?.Invoke(finished);
                return;
            }

            // Keep feet firmly planted while mining to prevent any sliding.
            if (movement != null)
            {
                movement.Stop();
                movement.FacePosition(activeOre.transform.position, deltaTime);
            }

            if (animatorController != null)
            {
                animatorController.SetMoveSpeed(0f);
            }
        }

        /// <summary>
        /// Deposits all carried resources into GoldManager and resets inventory to 0.
        /// </summary>
        public int DepositAllResources()
        {
            if (currentInventory <= 0)
            {
                return 0;
            }

            int amountToDeposit = currentInventory;
            currentInventory = 0;

            if (goldManager != null)
            {
                goldManager.AddGold(amountToDeposit);
            }

            OnInventoryChanged?.Invoke(currentInventory, maxInventoryCapacity);
            OnResourcesDeposited?.Invoke(amountToDeposit);
            return amountToDeposit;
        }

        private void HandleMiningHitAnimationEvent()
        {
            if (!isMining || activeOre == null || IsInventoryFull)
            {
                return;
            }

            // Enforce minimum real-time hit interval matching the slowed pickaxe cycle.
            if (Time.time - lastHitTime < minHitInterval)
            {
                return;
            }

            if (activeOre.IsDepleted)
            {
                MineralOreDeposit finished = activeOre;
                StopMining();
                OnOreDepleted?.Invoke(finished);
                return;
            }

            bool hitApplied = activeOre.RegisterMiningHit(out bool stageReduced, out bool depleted);
            if (hitApplied && goldPerHit > 0)
            {
                lastHitTime = Time.time;
                int spaceLeft = Mathf.Max(0, maxInventoryCapacity - currentInventory);
                int gained = Mathf.Min(goldPerHit, spaceLeft);
                if (gained > 0)
                {
                    currentInventory += gained;
                    OnInventoryChanged?.Invoke(currentInventory, maxInventoryCapacity);
                }
            }

            if (depleted)
            {
                MineralOreDeposit finished = activeOre;
                StopMining();
                OnOreDepleted?.Invoke(finished);
                return;
            }

            if (IsInventoryFull)
            {
                StopMining();
                OnInventoryFull?.Invoke();
            }
        }
    }
}
