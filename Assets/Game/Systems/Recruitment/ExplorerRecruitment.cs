using System;
using Game.Systems.Economy;
using UnityEngine;

namespace Game.Systems.Recruitment
{
    /// <summary>
    /// Manages Explorer recruitment queue, 80-gold deduction via GoldManager,
    /// and training progress timer (mirroring MinerRecruitmentQueue's pattern).
    /// </summary>
    [DisallowMultipleComponent]
    public class ExplorerRecruitment : MonoBehaviour
    {
        [Header("Recruitment Settings")]
        [SerializeField, Min(1)] private int explorerCost = 80;
        [SerializeField, Min(0.1f)] private float trainingDurationSeconds = 4f;

        [Header("System References")]
        [SerializeField] private GoldManager goldManager;
        [SerializeField] private ExplorerSpawner explorerSpawner;

        private int pendingCount;
        private float currentUnitElapsed;
        private bool isTraining;

        public int ExplorerCost => explorerCost;
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
        public event Action OnExplorerTrainingCompleted;

        private void Awake()
        {
            if (goldManager == null)
            {
                goldManager = FindFirstObjectByType<GoldManager>();
            }

            if (explorerSpawner == null)
            {
                explorerSpawner = FindFirstObjectByType<ExplorerSpawner>();
            }
        }

        private void Start()
        {
            OnQueueChanged?.Invoke(pendingCount);
            OnTrainingProgressChanged?.Invoke(0f, false);
        }

        public bool CanAffordExplorer()
        {
            return goldManager != null && goldManager.HasEnoughGold(explorerCost);
        }

        /// <summary>
        /// Attempts to deduct 80 gold and enqueue one Explorer for training.
        /// </summary>
        public bool TryEnqueueExplorer()
        {
            if (goldManager == null)
            {
                Debug.LogError("[ExplorerRecruitment] Missing GoldManager reference.", this);
                return false;
            }

            if (!goldManager.TrySpendGold(explorerCost))
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

            if (explorerSpawner != null)
            {
                explorerSpawner.SpawnExplorer();
            }
            else
            {
                Debug.LogWarning("[ExplorerRecruitment] Explorer training finished, but ExplorerSpawner is not assigned.", this);
            }

            OnExplorerTrainingCompleted?.Invoke();

            if (pendingCount > 0)
            {
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

        public void ConfigureReferences(GoldManager gold, ExplorerSpawner spawner)
        {
            goldManager = gold;
            explorerSpawner = spawner;
        }
    }
}
