using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Worlds.World01.Structures
{
    /// <summary>
    /// Prepares a Mineral_Ore deposit for progressive stage-based size reduction when mined.
    /// All mineral deposits start at uniform 100% size (State 1: Vector3.one) and reduce in
    /// discrete states after reaching a hit threshold (not on every animation frame).
    /// Enforces a strict maximum number of assigned miners per deposit.
    /// </summary>
    [DisallowMultipleComponent]
    public class MineralOreDeposit : MonoBehaviour
    {
        public enum ExtractionState
        {
            State1_100Percent = 1,
            State2_75Percent = 2,
            State3_50Percent = 3,
            State4_25Percent = 4,
            State5_0Percent_Depleted = 5
        }

        [Header("Uniform Initial Configuration")]
        [Tooltip("All mineral deposits share the exact same initial resource capacity (4 stages x hitsPerStateReduction x 10).")]
        [SerializeField] private int initialResourceAmount = 2200;

        [Tooltip("Number of pickaxe hits required before advancing to the next reduction state (55 hits/stage = 220 hits total).")]
        [SerializeField] private int hitsPerStateReduction = 55;

        [Tooltip("Maximum number of miners that can be assigned to this ore simultaneously.")]
        [SerializeField, Min(1)] private int maxAssignedMiners = 2;

        [Tooltip("Optional child transform to scale around the ground pivot (defaults to this transform).")]
        [SerializeField] private Transform scaleTarget;

        [Header("Extraction State (Read-Only Preview)")]
        [SerializeField] private ExtractionState currentState = ExtractionState.State1_100Percent;
        [SerializeField] private int currentHitsInStage;

        private static readonly Vector3 UniformInitialScale = Vector3.one;
        private readonly List<Component> assignedMiners = new List<Component>();

        public ExtractionState CurrentState => currentState;
        public int InitialResourceAmount => initialResourceAmount;
        public int HitsPerStateReduction => hitsPerStateReduction;
        public int TotalHitsToDeplete => Mathf.Max(1, hitsPerStateReduction) * 4;
        public int CurrentHitsInStage => currentHitsInStage;
        public int MaxAssignedMiners => Mathf.Max(1, maxAssignedMiners);
        public int AssignedMinerCount
        {
            get
            {
                PruneNullAssignments();
                return assignedMiners.Count;
            }
        }
        public int ActiveMinerCount => AssignedMinerCount;
        public bool HasCapacity => !IsDepleted && AssignedMinerCount < MaxAssignedMiners;
        public bool IsDepleted => currentState == ExtractionState.State5_0Percent_Depleted || !gameObject.activeInHierarchy;

        public event Action<MineralOreDeposit, ExtractionState> OnStateChanged;
        public event Action<MineralOreDeposit> OnDepleted;

        private void Awake()
        {
            if (scaleTarget == null)
            {
                scaleTarget = transform;
            }
            ApplyStateScale(currentState);
        }

        /// <summary>
        /// Atomically assigns a miner to this ore if capacity allows (max 2 miners).
        /// Returns true if the miner is now assigned to this ore.
        /// </summary>
        public bool TryAssignMiner(Component miner)
        {
            if (miner == null || IsDepleted)
            {
                return false;
            }

            PruneNullAssignments();

            if (assignedMiners.Contains(miner))
            {
                return true;
            }

            if (assignedMiners.Count >= MaxAssignedMiners)
            {
                return false;
            }

            assignedMiners.Add(miner);
            return true;
        }

        public void UnassignMiner(Component miner)
        {
            if (miner == null)
            {
                return;
            }

            assignedMiners.Remove(miner);
            PruneNullAssignments();
        }

        public bool IsMinerAssigned(Component miner)
        {
            if (miner == null)
            {
                return false;
            }

            PruneNullAssignments();
            return assignedMiners.Contains(miner);
        }

        public int GetMinerSlotIndex(Component miner)
        {
            PruneNullAssignments();
            int idx = miner != null ? assignedMiners.IndexOf(miner) : -1;
            return idx >= 0 ? idx : 0;
        }

        public void ClearAllAssignments()
        {
            assignedMiners.Clear();
        }

        private void PruneNullAssignments()
        {
            for (int i = assignedMiners.Count - 1; i >= 0; i--)
            {
                if (assignedMiners[i] == null)
                {
                    assignedMiners.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Registers a single pickaxe hit from a miner. Size is only reduced once
        /// currentHitsInStage reaches hitsPerStateReduction.
        /// </summary>
        public bool RegisterMiningHit(out bool stageReduced, out bool depleted)
        {
            stageReduced = false;
            depleted = false;

            if (IsDepleted)
            {
                depleted = true;
                return false;
            }

            currentHitsInStage++;
            int requiredHits = Mathf.Max(1, hitsPerStateReduction);
            if (currentHitsInStage >= requiredHits)
            {
                currentHitsInStage = 0;
                stageReduced = true;

                ExtractionState nextState = GetNextExtractionState(currentState);
                SetExtractionState(nextState);
                OnStateChanged?.Invoke(this, currentState);

                if (currentState == ExtractionState.State5_0Percent_Depleted)
                {
                    depleted = true;
                    ClearAllAssignments();
                    OnDepleted?.Invoke(this);
                }
            }

            return true;
        }

        private static ExtractionState GetNextExtractionState(ExtractionState state)
        {
            return state switch
            {
                ExtractionState.State1_100Percent => ExtractionState.State2_75Percent,
                ExtractionState.State2_75Percent => ExtractionState.State3_50Percent,
                ExtractionState.State3_50Percent => ExtractionState.State4_25Percent,
                ExtractionState.State4_25Percent => ExtractionState.State5_0Percent_Depleted,
                _ => ExtractionState.State5_0Percent_Depleted
            };
        }

        /// <summary>
        /// Sets the visual extraction state (100% -> 75% -> 50% -> 25% -> 0% disappears)
        /// while keeping the ground contact point anchored at local Y = 0.
        /// </summary>
        public void SetExtractionState(ExtractionState newState)
        {
            currentState = newState;
            ApplyStateScale(currentState);
        }

        public static float GetScaleFactorForState(ExtractionState state)
        {
            return state switch
            {
                ExtractionState.State1_100Percent => 1.00f,
                ExtractionState.State2_75Percent => 0.75f,
                ExtractionState.State3_50Percent => 0.50f,
                ExtractionState.State4_25Percent => 0.25f,
                ExtractionState.State5_0Percent_Depleted => 0.00f,
                _ => 1.00f
            };
        }

        private void ApplyStateScale(ExtractionState state)
        {
            Transform target = scaleTarget != null ? scaleTarget : transform;
            float factor = GetScaleFactorForState(state);

            if (state == ExtractionState.State5_0Percent_Depleted || factor <= 0.001f)
            {
                target.localScale = Vector3.zero;
                gameObject.SetActive(false);
                return;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            target.localScale = UniformInitialScale * factor;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (initialResourceAmount < 1) initialResourceAmount = 2200;
            if (hitsPerStateReduction < 1) hitsPerStateReduction = 55;
            if (maxAssignedMiners < 1) maxAssignedMiners = 2;
            if (scaleTarget == null) scaleTarget = transform;

            if (!Application.isPlaying && currentState != ExtractionState.State5_0Percent_Depleted)
            {
                float factor = GetScaleFactorForState(currentState);
                scaleTarget.localScale = UniformInitialScale * factor;
            }
        }
#endif
    }
}
