using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.MainMenu
{
    /// <summary>
    /// Frutiger Aero crystal-glass button animator:
    /// - Staggered entrance slide & fade-in on startup
    /// - Luminous turquoise-emerald glass glow on hover
    /// - Smooth spring scale pulse on press/release
    /// - Triggers hover & click audio feedback automatically
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FrutigerGlassButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Visual References")]
        [SerializeField] private Image glassBackground;
        [SerializeField] private Image hoverGlowOverlay;
        [SerializeField] private Image leftAccentGem;
        [SerializeField] private Text buttonLabel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Frutiger Aero Palette")]
        [SerializeField] private Color normalGlassColor = new Color(0.08f, 0.62f, 0.56f, 0.78f);
        [SerializeField] private Color hoverGlassColor = new Color(0.12f, 0.84f, 0.78f, 0.94f);
        [SerializeField] private Color pressedGlassColor = new Color(0.05f, 0.48f, 0.45f, 0.98f);
        [SerializeField] private Color normalGemColor = new Color(0.75f, 0.98f, 1.00f, 0.85f);
        [SerializeField] private Color hoverGemColor = new Color(1.00f, 1.00f, 1.00f, 1.00f);

        [Header("Animation Settings")]
        [SerializeField] private float entranceDelay = 0.1f;
        [SerializeField] private float entranceDuration = 0.48f;
        [SerializeField] private float entranceSlideOffsetX = -55f;
        [SerializeField] private float hoverScale = 1.045f;
        [SerializeField] private float pressedScale = 0.965f;
        [SerializeField] private float transitionSpeed = 12f;
        [SerializeField] private bool playEntranceOnEnable = true;

        private RectTransform rectTransform;
        private Vector2 restAnchoredPos;
        private bool restPosInitialized;
        private Vector3 targetScale = Vector3.one;
        private Color targetBgColor;
        private float targetGlowAlpha;
        private Color targetGemColor;
        private bool isHovered;
        private bool isPressed;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
            CacheRestPosition();
            ResetVisualStateImmediate();
        }

        private void OnEnable()
        {
            CacheRestPosition();
            if (playEntranceOnEnable && Application.isPlaying)
            {
                StopAllCoroutines();
                StartCoroutine(PlayEntranceRoutine());
            }
            else
            {
                ResetVisualStateImmediate();
            }
        }

        private void CacheRestPosition()
        {
            if (!restPosInitialized && rectTransform != null)
            {
                restAnchoredPos = rectTransform.anchoredPosition;
                restPosInitialized = true;
            }
        }

        private void ResetVisualStateImmediate()
        {
            isHovered = false;
            isPressed = false;
            targetScale = Vector3.one;
            targetBgColor = normalGlassColor;
            targetGlowAlpha = 0f;
            targetGemColor = normalGemColor;

            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                if (restPosInitialized)
                {
                    rectTransform.anchoredPosition = restAnchoredPos;
                }
            }
            if (glassBackground != null)
            {
                glassBackground.color = normalGlassColor;
            }
            if (hoverGlowOverlay != null)
            {
                Color c = hoverGlowOverlay.color;
                c.a = 0f;
                hoverGlowOverlay.color = c;
            }
            if (leftAccentGem != null)
            {
                leftAccentGem.color = normalGemColor;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        private IEnumerator PlayEntranceRoutine()
        {
            canvasGroup.alpha = 0f;
            rectTransform.anchoredPosition = restAnchoredPos + new Vector2(entranceSlideOffsetX, 0f);
            rectTransform.localScale = new Vector3(0.94f, 0.94f, 1f);

            if (entranceDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(entranceDelay);
            }

            float elapsed = 0f;
            while (elapsed < entranceDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / entranceDuration);
                // Cubic ease out
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                canvasGroup.alpha = ease;
                rectTransform.anchoredPosition = Vector2.LerpUnclamped(
                    restAnchoredPos + new Vector2(entranceSlideOffsetX, 0f),
                    restAnchoredPos,
                    ease
                );
                rectTransform.localScale = Vector3.LerpUnclamped(new Vector3(0.94f, 0.94f, 1f), Vector3.one, ease);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            rectTransform.anchoredPosition = restAnchoredPos;
            rectTransform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            float dt = Time.unscaledDeltaTime * transitionSpeed;
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, dt);

            if (glassBackground != null)
            {
                glassBackground.color = Color.Lerp(glassBackground.color, targetBgColor, dt);
            }
            if (hoverGlowOverlay != null)
            {
                Color c = hoverGlowOverlay.color;
                c.a = Mathf.Lerp(c.a, targetGlowAlpha, dt);
                hoverGlowOverlay.color = c;
            }
            if (leftAccentGem != null)
            {
                leftAccentGem.color = Color.Lerp(leftAccentGem.color, targetGemColor, dt);
            }
        }

        private void RefreshTargets()
        {
            if (isPressed)
            {
                targetScale = new Vector3(pressedScale, pressedScale, 1f);
                targetBgColor = pressedGlassColor;
                targetGlowAlpha = 0.55f;
                targetGemColor = hoverGemColor;
            }
            else if (isHovered)
            {
                targetScale = new Vector3(hoverScale, hoverScale, 1f);
                targetBgColor = hoverGlassColor;
                targetGlowAlpha = 0.38f;
                targetGemColor = hoverGemColor;
            }
            else
            {
                targetScale = Vector3.one;
                targetBgColor = normalGlassColor;
                targetGlowAlpha = 0f;
                targetGemColor = normalGemColor;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            RefreshTargets();
            if (MainMenuAudioController.Instance != null)
            {
                MainMenuAudioController.Instance.PlayHover();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            isPressed = false;
            RefreshTargets();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            RefreshTargets();
            if (MainMenuAudioController.Instance != null)
            {
                MainMenuAudioController.Instance.PlayClick();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            RefreshTargets();
        }

        public void OnSelect(BaseEventData eventData)
        {
            isHovered = true;
            RefreshTargets();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isHovered = false;
            RefreshTargets();
        }
    }
}
