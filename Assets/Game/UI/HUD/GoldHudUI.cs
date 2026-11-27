using Game.Systems.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.HUD
{
    /// <summary>
    /// Displays the player's current gold in the top HUD bar and gives subtle feedback on changes.
    /// </summary>
    [DisallowMultipleComponent]
    public class GoldHudUI : MonoBehaviour
    {
        [SerializeField] private GoldManager goldManager;
        [SerializeField] private Text goldAmountText;
        [SerializeField] private Image panelBackground;

        [Header("Feedback Colors")]
        [SerializeField] private Color normalTextColor = new Color(1f, 0.93f, 0.62f, 1f);
        [SerializeField] private Color insufficientFlashColor = new Color(1f, 0.32f, 0.32f, 1f);
        [SerializeField] private float flashDuration = 0.45f;

        private float flashTimer;

        private void Awake()
        {
            if (goldManager == null)
            {
                goldManager = FindFirstObjectByType<GoldManager>();
            }
        }

        private void OnEnable()
        {
            if (goldManager != null)
            {
                goldManager.OnGoldChanged += HandleGoldChanged;
                goldManager.OnInsufficientGold += HandleInsufficientGold;
                HandleGoldChanged(goldManager.CurrentGold);
            }
        }

        private void OnDisable()
        {
            if (goldManager != null)
            {
                goldManager.OnGoldChanged -= HandleGoldChanged;
                goldManager.OnInsufficientGold -= HandleInsufficientGold;
            }
        }

        private void Update()
        {
            if (flashTimer > 0f && goldAmountText != null)
            {
                flashTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(flashTimer / Mathf.Max(0.01f, flashDuration));
                goldAmountText.color = Color.Lerp(normalTextColor, insufficientFlashColor, t);
            }
        }

        private void HandleGoldChanged(int amount)
        {
            if (goldAmountText != null)
            {
                goldAmountText.text = amount.ToString();
            }
        }

        private void HandleInsufficientGold(int required, int current)
        {
            flashTimer = flashDuration;
        }

        public void ConfigureReferences(GoldManager manager, Text amountText, Image background)
        {
            goldManager = manager;
            goldAmountText = amountText;
            panelBackground = background;
        }
    }
}
