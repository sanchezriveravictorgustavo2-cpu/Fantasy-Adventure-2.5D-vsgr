using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.MainMenu
{
    /// <summary>
    /// Adds subtle, tranquil life to the 2.5D Frutiger Aero Main Menu without distracting:
    /// - Gentle horizontal drifting of stylized low-poly/Frutiger Aero clouds
    /// - Subtle water/waterfall shimmer pulse
    /// - Soft foliage & floating island breathing motion
    /// - Subtle camera/background parallax response
    /// </summary>
    public class MainMenuAmbientAnimator : MonoBehaviour
    {
        [System.Serializable]
        public class CloudLayer
        {
            public RectTransform cloudRect;
            public float speedX = 14f;
            public float bobAmplitudeY = 4f;
            public float bobFrequency = 0.4f;
            public float wrapExtentX = 1150f;
            [HideInInspector] public Vector2 initialPos;
        }

        [Header("Cloud Drift Layers")]
        [SerializeField] private List<CloudLayer> cloudLayers = new List<CloudLayer>();

        [Header("Water & Waterfall Shimmer")]
        [SerializeField] private Image waterShimmerOverlay;
        [SerializeField] private float shimmerMinAlpha = 0.05f;
        [SerializeField] private float shimmerMaxAlpha = 0.18f;
        [SerializeField] private float shimmerFrequency = 1.35f;

        [Header("Subtle Panorama Breathing / Parallax")]
        [SerializeField] private RectTransform panoramaContainer;
        [SerializeField] private float parallaxAmplitudeX = 6f;
        [SerializeField] private float parallaxAmplitudeY = 3.5f;
        [SerializeField] private float breathingFrequency = 0.25f;

        [Header("3D Environment Props (Optional Live 3D Elements)")]
        [SerializeField] private Transform floatingCrystal3D;
        [SerializeField] private Transform foliageGroup3D;
        [SerializeField] private Transform protagonistRoot3D;

        private Vector2 panoramaBasePos;
        private Vector3 crystalBasePos;
        private Vector3 protagonistBasePos;

        private void Start()
        {
            for (int i = 0; i < cloudLayers.Count; i++)
            {
                if (cloudLayers[i].cloudRect != null)
                {
                    cloudLayers[i].initialPos = cloudLayers[i].cloudRect.anchoredPosition;
                }
            }

            if (panoramaContainer != null)
            {
                panoramaBasePos = panoramaContainer.anchoredPosition;
            }

            if (floatingCrystal3D != null)
            {
                crystalBasePos = floatingCrystal3D.localPosition;
            }

            if (protagonistRoot3D != null)
            {
                protagonistBasePos = protagonistRoot3D.localPosition;
            }
        }

        private void Update()
        {
            float t = Time.unscaledTime;

            // 1. Subtle horizontal cloud drift & gentle vertical bob
            for (int i = 0; i < cloudLayers.Count; i++)
            {
                CloudLayer cl = cloudLayers[i];
                if (cl.cloudRect == null) continue;

                Vector2 pos = cl.cloudRect.anchoredPosition;
                pos.x += cl.speedX * Time.unscaledDeltaTime;
                if (pos.x > cl.wrapExtentX)
                {
                    pos.x = -cl.wrapExtentX;
                }
                else if (pos.x < -cl.wrapExtentX)
                {
                    pos.x = cl.wrapExtentX;
                }
                pos.y = cl.initialPos.y + Mathf.Sin(t * cl.bobFrequency + i * 1.7f) * cl.bobAmplitudeY;
                cl.cloudRect.anchoredPosition = pos;
            }

            // 2. Water & Cascade crystalline shimmer pulse
            if (waterShimmerOverlay != null)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(t * shimmerFrequency * Mathf.PI * 2f);
                Color c = waterShimmerOverlay.color;
                c.a = Mathf.Lerp(shimmerMinAlpha, shimmerMaxAlpha, wave);
                waterShimmerOverlay.color = c;
            }

            // 3. Gentle breathing & subtle mouse/time parallax on the 2.5D panorama container
            if (panoramaContainer != null)
            {
                float bx = Mathf.Sin(t * breathingFrequency * Mathf.PI * 2f) * parallaxAmplitudeX;
                float by = Mathf.Cos(t * breathingFrequency * 0.8f * Mathf.PI * 2f) * parallaxAmplitudeY;
                panoramaContainer.anchoredPosition = panoramaBasePos + new Vector2(bx, by);
            }

            // 4. Optional 3D Crystal hover & rotation
            if (floatingCrystal3D != null)
            {
                floatingCrystal3D.localPosition = crystalBasePos + new Vector3(0f, Mathf.Sin(t * 1.6f) * 0.08f, 0f);
                floatingCrystal3D.Rotate(Vector3.up, 22f * Time.unscaledDeltaTime, Space.Self);
            }

            // 5. Optional 3D Foliage gentle breeze sway
            if (foliageGroup3D != null)
            {
                float sway = Mathf.Sin(t * 1.4f) * 1.6f;
                foliageGroup3D.localRotation = Quaternion.Euler(0f, 0f, sway);
            }

            // 6. Optional 3D Explorer subtle breathing idle if no Animator clip overrides it
            if (protagonistRoot3D != null)
            {
                float breath = Mathf.Sin(t * 1.8f) * 0.012f;
                protagonistRoot3D.localPosition = protagonistBasePos + new Vector3(0f, breath, 0f);
            }
        }
    }
}
