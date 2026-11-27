using System;
using Game.Systems.Economy;
using UnityEngine;

namespace Game.Systems.Recruitment
{
    /// <summary>
    /// Manages the miner recruitment queue, gold deduction, and 8-second per-unit training timer.
    /// </summary>
    [DisallowMultipleComponent]
    public class MinerRecruitmentQueue : MonoBehaviour
    {
        [Header("Recruitment Settings")]
        [SerializeField, Min(1)] private int minerCost = 150;
        [SerializeField, Min(0.1f)] private float trainingDurationSeconds = 8f;

        [Header("System References")]
        [SerializeField] private GoldManager goldManager;
        [SerializeField] private MinerSpawner minerSpawner;

        private int pendingCount;
        private float currentUnitElapsed;
        private bool isTraining;

        public int MinerCost => minerCost;
        public float TrainingDurationSeconds => trainingDurationSeconds;
        public int PendingCount => pendingCount;
        public bool IsTraining => isTraining;
        public float NormalizedProgress => isTraining && trainingDurationSeconds > 0f
            ? Mathf.Clamp01(currentUnitElapsed / trainingDurationSeconds)
            : 0f;
        public float RemainingSeconds => isTraining
            ? Mathf.Max(0f, trainingDurationSeconds - currentUnitElapsed)
            : 0f;

        public event Action<int> OnQueueChanged;
        public event Action<float, bool> OnTrainingProgressChanged;
        public event Action OnRecruitmentRejectedInsufficientGold;
        public event Action OnMinerTrainingCompleted;

        private void Awake()
        {
            if (goldManager == null)
            {
                goldManager = FindFirstObjectByType<GoldManager>();
            }

            if (minerSpawner == null)
            {
                minerSpawner = FindFirstObjectByType<MinerSpawner>();
            }
        }

        private void Start()
        {
            OnQueueChanged?.Invoke(pendingCount);
            OnTrainingProgressChanged?.Invoke(0f, false);
        }

        public bool CanAffordMiner()
        {
            return goldManager != null && goldManager.HasEnoughGold(minerCost);
        }

        /// <summary>
        /// Attempts to deduct 150 gold and enqueue one miner for 8-second generation.
        /// </summary>
        public bool TryEnqueueMiner()
        {
            if (goldManager == null)
            {
                Debug.LogError("[MinerRecruitmentQueue] Missing GoldManager reference.", this);
                return false;
            }

            if (!goldManager.TrySpendGold(minerCost))
            {
                OnRecruitmentRejectedInsufficientGold?.Invoke();
                return false;
            }

            pendingCount++;
            if (!isTraining)
            {
                isTraining = true;
                currentUnitElapsed = 0f;
            }

            OnQueueChanged?.Invoke(pendingCount);
            OnTrainingProgressChanged?.Invoke(NormalizedProgress, isTraining);
            return true;
        }

        private void Update()
        {
            if (!isTraining || pendingCount <= 0)
            {
                return;
            }

            currentUnitElapsed += Time.deltaTime;
            if (currentUnitElapsed >= trainingDurationSeconds)
            {
                CompleteCurrentUnit();
            }
            else
            {
                OnTrainingProgressChanged?.Invoke(NormalizedProgress, true);
            }
        }

        private void CompleteCurrentUnit()
        {
            pendingCount = Mathf.Max(0, pendingCount - 1);

            if (minerSpawner != null)
            {
                minerSpawner.SpawnMiner();
            }
            else
            {
                Debug.LogWarning("[MinerRecruitmentQueue] Miner training finished, but MinerSpawner is not assigned.", this);
            }

            OnMinerTrainingCompleted?.Invoke();

            if (pendingCount > 0)
            {
                // Start the next queued miner cleanly for its own full 8-second cycle.
                currentUnitElapsed = 0f;
                isTraining = true;
                OnQueueChanged?.Invoke(pendingCount);
                OnTrainingProgressChanged?.Invoke(0f, true);
            }
            else
            {
                currentUnitElapsed = 0f;
                isTraining = false;
                OnQueueChanged?.Invoke(0);
                OnTrainingProgressChanged?.Invoke(0f, false);
            }
        }

        public void ConfigureReferences(GoldManager gold, MinerSpawner spawner)
        {
            goldManager = gold;
            minerSpawner = spawner;
        }
    }
}
