using UnityEngine;

namespace Game.Systems.CameraControl
{
    /// <summary>
    /// Synchronizes CameraDragController's horizontal limits with the visual Left_Barricade
    /// and Right_Barricade transforms so the camera viewport never scrolls past either barricade.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(CameraDragController))]
    public class CameraBoundaryController : MonoBehaviour
    {
        [Header("Visual Barricade References")]
        [SerializeField] private Transform leftBarricade;
        [SerializeField] private Transform rightBarricade;

        [Header("Framing Offset")]
        [Tooltip("Small horizontal margin (in world units) so the barricade stays cleanly framed at the edge.")]
        [SerializeField] private float edgePadding = 0.6f;

        private Camera targetCamera;
        private CameraDragController dragController;
        private float lastAspect = -1f;

        public Transform LeftBarricade => leftBarricade;
        public Transform RightBarricade => rightBarricade;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            dragController = GetComponent<CameraDragController>();
            ApplyBarricadeLimits();
        }

        private void Start()
        {
            ApplyBarricadeLimits();
        }

        private void LateUpdate()
        {
            if (targetCamera != null && !Mathf.Approximately(targetCamera.aspect, lastAspect))
            {
                ApplyBarricadeLimits();
            }
        }

        public void ApplyBarricadeLimits()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>();
            if (dragController == null) dragController = GetComponent<CameraDragController>();
            if (targetCamera == null || dragController == null || leftBarricade == null || rightBarricade == null)
            {
                return;
            }

            float leftWorldX = Mathf.Min(leftBarricade.position.x, rightBarricade.position.x);
            float rightWorldX = Mathf.Max(leftBarricade.position.x, rightBarricade.position.x);

            float halfViewWidth = targetCamera.orthographic
                ? targetCamera.orthographicSize * targetCamera.aspect
                : 10.35f;

            float minCamX = leftWorldX + halfViewWidth - edgePadding;
            float maxCamX = rightWorldX - halfViewWidth + edgePadding;

            if (minCamX > maxCamX)
            {
                float mid = (leftWorldX + rightWorldX) * 0.5f;
                minCamX = mid;
                maxCamX = mid;
            }

            dragController.SetLimits(minCamX, maxCamX);
            lastAspect = targetCamera.aspect;
        }

        public void ConfigureReferences(Transform left, Transform right, float padding = 0.6f)
        {
            leftBarricade = left;
            rightBarricade = right;
            edgePadding = padding;
            ApplyBarricadeLimits();
        }
    }
}
