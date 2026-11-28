using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Combat
{
    /// <summary>
    /// Stylized world-space health bar that stays upright, faces the main camera,
    /// and updates smoothly when health changes. Can be attached to units or statues.
    /// </summary>
    public class WorldHealthBarUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private Vector2 barSize = new Vector2(1.4f, 0.22f);
        [SerializeField] private bool hideWhenFull = false;
        [SerializeField] private bool hideWhenDead = true;

        [Header("Colors")]
        [SerializeField] private Color allyFillColor = new Color(0.22f, 0.86f, 0.45f, 1f);
        [SerializeField] private Color enemyFillColor = new Color(0.92f, 0.26f, 0.24f, 1f);
        [SerializeField] private Color lowHealthColor = new Color(0.96f, 0.65f, 0.14f, 1f);
        [SerializeField] private Color backgroundColor = new Color(0.05f, 0.08f, 0.12f, 0.82f);
        [SerializeField] private Color borderColor = new Color(0.95f, 0.98f, 1f, 0.65f);

        private Canvas barCanvas;
        private Image fillImage;
        private RectTransform fillRect;
        private Camera cachedCamera;
        private TeamFaction barFaction = TeamFaction.Ally;
        private float currentNormalized = 1f;
        private bool isBuilt;

        public void Initialize(TeamFaction faction, Vector3 offset, Vector2 size, bool hideAtFull)
        {
            barFaction = faction;
            localOffset = offset;
            barSize = size;
            hideWhenFull = hideAtFull;
            EnsureBuilt();
            SetNormalizedHealth(1f);
        }

        private void Awake()
        {
            EnsureBuilt();
        }

        private void LateUpdate()
        {
            if (barCanvas == null || !barCanvas.gameObject.activeSelf)
            {
                return;
            }

            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
            }

            if (cachedCamera != null)
            {
                barCanvas.transform.rotation = cachedCamera.transform.rotation;
            }
        }

        public void SetNormalizedHealth(float normalized)
        {
            EnsureBuilt();
            currentNormalized = Mathf.Clamp01(normalized);

            if (fillRect != null)
            {
                fillRect.anchorMax = new Vector2(currentNormalized, 1f);
            }

            if (fillImage != null)
            {
                Color baseColor = barFaction == TeamFaction.Ally ? allyFillColor : enemyFillColor;
                fillImage.color = currentNormalized < 0.35f && barFaction == TeamFaction.Ally
                    ? Color.Lerp(enemyFillColor, lowHealthColor, currentNormalized / 0.35f)
                    : baseColor;
            }

            if (barCanvas != null)
            {
                bool shouldShow = true;
                if (hideWhenDead && currentNormalized <= 0.0001f)
                {
                    shouldShow = false;
                }
                else if (hideWhenFull && currentNormalized >= 0.999f)
                {
                    shouldShow = false;
                }

                if (barCanvas.gameObject.activeSelf != shouldShow)
                {
                    barCanvas.gameObject.SetActive(shouldShow);
                }
            }
        }

        private void EnsureBuilt()
        {
            if (isBuilt && barCanvas != null)
            {
                barCanvas.transform.localPosition = localOffset;
                RectTransform canvasRt = barCanvas.GetComponent<RectTransform>();
                if (canvasRt != null)
                {
                    canvasRt.sizeDelta = barSize * 100f;
                }
                return;
            }

            Transform existing = transform.Find("World_HealthBar_Canvas");
            if (existing != null)
            {
                barCanvas = existing.GetComponent<Canvas>();
                Transform fillTf = existing.Find("Border/Background/Fill");
                if (fillTf != null)
                {
                    fillImage = fillTf.GetComponent<Image>();
                    fillRect = fillTf.GetComponent<RectTransform>();
                }
                if (barCanvas != null && fillImage != null)
                {
                    isBuilt = true;
                    return;
                }
            }

            GameObject canvasGo = new GameObject("World_HealthBar_Canvas");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = localOffset;
            canvasGo.transform.localRotation = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one * 0.01f;

            barCanvas = canvasGo.AddComponent<Canvas>();
            barCanvas.renderMode = RenderMode.WorldSpace;
            barCanvas.sortingOrder = 15;

            RectTransform rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = barSize * 100f;

            // Border
            GameObject borderGo = new GameObject("Border");
            borderGo.transform.SetParent(canvasGo.transform, false);
            RectTransform borderRt = borderGo.AddComponent<RectTransform>();
            StretchFull(borderRt);
            Image borderImg = borderGo.AddComponent<Image>();
            borderImg.color = borderColor;
            borderImg.raycastTarget = false;

            // Background
            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(borderGo.transform, false);
            RectTransform bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(3f, 3f);
            bgRt.offsetMax = new Vector2(-3f, -3f);
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = backgroundColor;
            bgImg.raycastTarget = false;

            // Fill
            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bgGo.transform, false);
            fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillImage = fillGo.AddComponent<Image>();
            fillImage.color = barFaction == TeamFaction.Ally ? allyFillColor : enemyFillColor;
            fillImage.raycastTarget = false;

            isBuilt = true;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
