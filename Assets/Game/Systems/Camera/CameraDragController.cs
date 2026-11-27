using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game.Systems.CameraControl
{
    /// <summary>
    /// Allows horizontal-only (X-axis) camera panning by holding left mouse button and dragging.
    /// Locks Y and Z completely to preserve the 2.5D orthographic perspective and avoids interfering with UI.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class CameraDragController : MonoBehaviour
    {
        [Header("Drag Settings")]
        [Tooltip("Multiplier for horizontal drag speed relative to viewport world width.")]
        [SerializeField, Range(0.1f, 3f)] private float dragSensitivity = 0.85f;

        [Tooltip("Smoothing time in seconds for camera horizontal glide (lower = snappier, higher = smoother).")]
        [SerializeField, Range(0.01f, 0.35f)] private float smoothTime = 0.08f;

        [Header("Horizontal Map Limits (X Axis)")]
        [Tooltip("Minimum allowed camera X position (left boundary).")]
        [SerializeField] private float minX = 6.0f;

        [Tooltip("Maximum allowed camera X position (right boundary).")]
        [SerializeField] private float maxX = 91.0f;

        private Camera targetCamera;
        private float lockedY;
        private float lockedZ;
        private float targetX;
        private float currentVelocityX;
        private bool isDragging;
        private float lastPointerX;

        public float DragSensitivity => dragSensitivity;
        public float SmoothTime => smoothTime;
        public float MinX => minX;
        public float MaxX => maxX;
        public bool IsDragging => isDragging;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            Vector3 pos = transform.position;
            lockedY = pos.y;
            lockedZ = pos.z;
            targetX = Mathf.Clamp(pos.x, minX, maxX);
        }

        private void Update()
        {
            HandlePointerDragInput();
            ApplyHorizontalMovement();
        }

        private void HandlePointerDragInput()
        {
            bool pressedThisFrame;
            bool isHeld;
            bool releasedThisFrame;
            float pointerX;

            if (!TryReadPrimaryPointer(out pressedThisFrame, out isHeld, out releasedThisFrame, out pointerX))
            {
                isDragging = false;
                return;
            }

            if (pressedThisFrame)
            {
                // Do not begin camera drag if the click started over any UI element (HUD buttons, panels, etc.).
                if (IsPointerOverUI())
                {
                    isDragging = false;
                    return;
                }

                isDragging = true;
                lastPointerX = pointerX;
            }

            if (releasedThisFrame || !isHeld)
            {
                isDragging = false;
                return;
            }

            if (isDragging)
            {
                float deltaPixelsX = pointerX - lastPointerX;
                lastPointerX = pointerX;

                if (Mathf.Abs(deltaPixelsX) > 0.001f)
                {
                    float worldUnitsPerPixel = ComputeWorldUnitsPerPixel();
                    // Dragging left (deltaPixelsX < 0) moves camera left; dragging right (deltaPixelsX > 0) moves camera right.
                    targetX = Mathf.Clamp(targetX + deltaPixelsX * worldUnitsPerPixel * dragSensitivity, minX, maxX);
                }
            }
        }

        private void ApplyHorizontalMovement()
        {
            Vector3 pos = transform.position;
            float nextX = Mathf.SmoothDamp(pos.x, targetX, ref currentVelocityX, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            nextX = Mathf.Clamp(nextX, minX, maxX);

            // Lock Y and Z strictly to initial 2.5D camera configuration.
            transform.position = new Vector3(nextX, lockedY, lockedZ);
        }

        private float ComputeWorldUnitsPerPixel()
        {
            float screenWidth = Mathf.Max(1f, Screen.width);
            if (targetCamera != null && targetCamera.orthographic)
            {
                float worldWidth = targetCamera.orthographicSize * 2f * targetCamera.aspect;
                return worldWidth / screenWidth;
            }

            return 20f / screenWidth;
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static bool TryReadPrimaryPointer(
            out bool pressedThisFrame,
            out bool isHeld,
            out bool releasedThisFrame,
            out float pointerX)
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                pressedThisFrame = mouse.leftButton.wasPressedThisFrame;
                isHeld = mouse.leftButton.isPressed;
                releasedThisFrame = mouse.leftButton.wasReleasedThisFrame;
                pointerX = mouse.position.ReadValue().x;
                return true;
            }

            pressedThisFrame = false;
            isHeld = false;
            releasedThisFrame = false;
            pointerX = 0f;
            return false;
#else
            pressedThisFrame = Input.GetMouseButtonDown(0);
            isHeld = Input.GetMouseButton(0);
            releasedThisFrame = Input.GetMouseButtonUp(0);
            pointerX = Input.mousePosition.x;
            return true;
#endif
        }

        /// <summary>
        /// Programmatic horizontal drag helper (useful for automated testing or keyboard shortcuts).
        /// </summary>
        public void DragByWorldDeltaX(float deltaWorldX)
        {
            targetX = Mathf.Clamp(targetX + deltaWorldX, minX, maxX);
        }

        public void SetLimits(float leftLimit, float rightLimit)
        {
            minX = Mathf.Min(leftLimit, rightLimit);
            maxX = Mathf.Max(leftLimit, rightLimit);
            targetX = Mathf.Clamp(targetX, minX, maxX);
        }
    }
}
