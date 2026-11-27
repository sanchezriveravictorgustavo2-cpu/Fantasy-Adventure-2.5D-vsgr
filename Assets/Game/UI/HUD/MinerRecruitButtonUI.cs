using Game.Systems.Economy;
using Game.Systems.Recruitment;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.HUD
{
    /// <summary>
    /// Controls the top-bar Miner recruitment button:
    /// - Miner icon, 150 gold cost indicator, pending queue badge
    /// - Semi-transparent black radial clock overlay strictly confined to the button area
    /// - Visual feedback when gold is insufficient
    /// </summary>
    [DisallowMultipleComponent]
    public class MinerRecruitButtonUI : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private MinerRecruitmentQueue recruitmentQueue;
        [SerializeField] private GoldManager goldManager;

        [Header("Button UI Elements")]
        [SerializeField] private Button recruitButton;
        [SerializeField] private RectTransform buttonRect;
        [SerializeField] private Image minerIconImage;
        [SerializeField] private Image radialClockOverlay;
        [SerializeField] private Image insufficientFlashOverlay;
        [SerializeField] private Text costText;
        [SerializeField] private GameObject queueBadgeRoot;
        [SerializeField] private Text queueCountText;
        [SerializeField] private Text feedbackToastText;

        [Header("Visual Feedback")]
        [SerializeField] private Color affordableCostColor = new Color(1f, 0.92f, 0.52f, 1f);
        [SerializeField] private Color unaffordableCostColor = new Color(1f, 0.36f, 0.36f, 1f);
        [SerializeField] private Color normalIconTint = Color.white;
        [SerializeField] private Color unaffordableIconTint = new Color(0.75f, 0.75f, 0.78f, 1f);
        [SerializeField] private float shakeDuration = 0.35f;
        [SerializeField] private float shakeAmplitude = 7f;
        [SerializeField] private float toastDuration = 1.25f;

        private Vector2 baseAnchoredPosition;
        private bool hasBasePosition;
        private float shakeTimer;
        private float toastTimer;

        private void Awake()
        {
            if (recruitmentQueue == null)
            {
                recruitmentQueue = FindFirstObjectByType<MinerRecruitmentQueue>();
            }

            if (goldManager == null)
            {
                goldManager = FindFirstObjectByType<GoldManager>();
            }

            if (buttonRect == null)
            {
                buttonRect = GetComponent<RectTransform>();
            }

            if (buttonRect != null)
            {
                baseAnchoredPosition = buttonRect.anchoredPosition;
                hasBasePosition = true;
            }

            EnsureRadialOverlayConfigured();
        }

        private void OnEnable()
        {
            if (recruitButton != null)
            {
                recruitButton.onClick.AddListener(HandleRecruitClicked);
            }

            if (recruitmentQueue != null)
            {
                recruitmentQueue.OnQueueChanged += HandleQueueChanged;
                recruitmentQueue.OnTrainingProgressChanged += HandleTrainingProgressChanged;
                recruitmentQueue.OnRecruitmentRejectedInsufficientGold += HandleInsufficientGoldFeedback;

                if (costText != null)
                {
                    costText.text = recruitmentQueue.MinerCost.ToString();
                }

                HandleQueueChanged(recruitmentQueue.PendingCount);
                HandleTrainingProgressChanged(recruitmentQueue.NormalizedProgress, recruitmentQueue.IsTraining);
            }

            if (goldManager != null)
            {
                goldManager.OnGoldChanged += HandleGoldChanged;
                HandleGoldChanged(goldManager.CurrentGold);
            }

            if (feedbackToastText != null)
            {
                feedbackToastText.gameObject.SetActive(false);
            }

            if (insufficientFlashOverlay != null)
            {
                Color c = insufficientFlashOverlay.color;
                c.a = 0f;
                insufficientFlashOverlay.color = c;
            }
        }

        private void OnDisable()
        {
            if (recruitButton != null)
            {
                recruitButton.onClick.RemoveListener(HandleRecruitClicked);
            }

            if (recruitmentQueue != null)
            {
                recruitmentQueue.OnQueueChanged -= HandleQueueChanged;
                recruitmentQueue.OnTrainingProgressChanged -= HandleTrainingProgressChanged;
                recruitmentQueue.OnRecruitmentRejectedInsufficientGold -= HandleInsufficientGoldFeedback;
            }

            if (goldManager != null)
            {
                goldManager.OnGoldChanged -= HandleGoldChanged;
            }
        }

        private void Update()
        {
            if (shakeTimer > 0f && buttonRect != null && hasBasePosition)
            {
                shakeTimer -= Time.deltaTime;
                float normalized = Mathf.Clamp01(shakeTimer / Mathf.Max(0.01f, shakeDuration));
                float offset = Mathf.Sin(normalized * Mathf.PI * 6f) * shakeAmplitude * normalized;
                buttonRect.anchoredPosition = baseAnchoredPosition + new Vector2(offset, 0f);

                if (insufficientFlashOverlay != null)
                {
                    Color c = insufficientFlashOverlay.color;
                    c.a = normalized * 0.48f;
                    insufficientFlashOverlay.color = c;
                }

                if (shakeTimer <= 0f)
                {
                    buttonRect.anchoredPosition = baseAnchoredPosition;
                    if (insufficientFlashOverlay != null)
                    {
                        Color c = insufficientFlashOverlay.color;
                        c.a = 0f;
                        insufficientFlashOverlay.color = c;
                    }
                }
            }

            if (toastTimer > 0f && feedbackToastText != null)
            {
                toastTimer -= Time.deltaTime;
                if (toastTimer <= 0f)
                {
                    feedbackToastText.gameObject.SetActive(false);
                }
            }
        }

        private void EnsureRadialOverlayConfigured()
        {
            if (radialClockOverlay == null)
            {
                return;
            }

            radialClockOverlay.type = Image.Type.Filled;
            radialClockOverlay.fillMethod = Image.FillMethod.Radial360;
            radialClockOverlay.fillOrigin = (int)Image.Origin360.Top;
            radialClockOverlay.fillClockwise = true;
            radialClockOverlay.raycastTarget = false;
        }

        private void HandleRecruitClicked()
        {
            if (recruitmentQueue == null)
            {
                return;
            }

            recruitmentQueue.TryEnqueueMiner();
        }

        private void HandleQueueChanged(int pendingCount)
        {
            if (queueBadgeRoot != null)
            {
                queueBadgeRoot.SetActive(pendingCount > 0);
            }

            if (queueCountText != null)
            {
                queueCountText.text = pendingCount.ToString();
            }
        }

        private void HandleTrainingProgressChanged(float normalizedProgress, bool isTraining)
        {
            if (radialClockOverlay == null)
            {
                return;
            }

            if (!isTraining)
            {
                radialClockOverlay.fillAmount = 0f;
                radialClockOverlay.gameObject.SetActive(false);
                return;
            }

            if (!radialClockOverlay.gameObject.activeSelf)
            {
                radialClockOverlay.gameObject.SetActive(true);
            }

            radialClockOverlay.fillAmount = Mathf.Clamp01(normalizedProgress);
        }

        private void HandleGoldChanged(int currentGold)
        {
            int cost = recruitmentQueue != null ? recruitmentQueue.MinerCost : 150;
            bool canAfford = currentGold >= cost;

            if (costText != null)
            {
                costText.color = canAfford ? affordableCostColor : unaffordableCostColor;
            }

            if (minerIconImage != null)
            {
                minerIconImage.color = canAfford ? normalIconTint : unaffordableIconTint;
            }
        }

        private void HandleInsufficientGoldFeedback()
        {
            shakeTimer = shakeDuration;
            toastTimer = toastDuration;

            if (feedbackToastText != null)
            {
                feedbackToastText.text = "Oro insuficiente";
                feedbackToastText.gameObject.SetActive(true);
            }
        }

        public void ConfigureReferences(
            MinerRecruitmentQueue queue,
            GoldManager gold,
            Button button,
            RectTransform rect,
            Image icon,
            Image radialOverlay,
            Image flashOverlay,
            Text costLabel,
            GameObject badgeRoot,
            Text badgeText,
            Text toastText)
        {
            recruitmentQueue = queue;
            goldManager = gold;
            recruitButton = button;
            buttonRect = rect;
            minerIconImage = icon;
            radialClockOverlay = radialOverlay;
            insufficientFlashOverlay = flashOverlay;
            costText = costLabel;
            queueBadgeRoot = badgeRoot;
            queueCountText = badgeText;
            feedbackToastText = toastText;
        }
    }
}
