using System;
using UnityEngine;

namespace Game.Systems.Economy
{
    /// <summary>
    /// Central gold resource manager for the level.
    /// Provides atomic validation/spending and notifies listeners on balance changes.
    /// </summary>
    [DisallowMultipleComponent]
    public class GoldManager : MonoBehaviour
    {
        [Header("Initial Economy")]
        [SerializeField, Min(0)] private int startingGold = 500;

        private int currentGold;

        public int CurrentGold => currentGold;

        public event Action<int> OnGoldChanged;
        public event Action<int, int> OnInsufficientGold;

        private void Awake()
        {
            currentGold = Mathf.Max(0, startingGold);
        }

        private void Start()
        {
            OnGoldChanged?.Invoke(currentGold);
        }

        public bool HasEnoughGold(int amount)
        {
            return amount <= 0 || currentGold >= amount;
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (currentGold < amount)
            {
                OnInsufficientGold?.Invoke(amount, currentGold);
                return false;
            }

            currentGold -= amount;
            OnGoldChanged?.Invoke(currentGold);
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currentGold += amount;
            OnGoldChanged?.Invoke(currentGold);
        }

        public void SetGold(int amount)
        {
            currentGold = Mathf.Max(0, amount);
            OnGoldChanged?.Invoke(currentGold);
        }
    }
}
