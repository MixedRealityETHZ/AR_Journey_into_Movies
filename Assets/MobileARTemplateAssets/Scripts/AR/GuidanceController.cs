using UnityEngine;
using ARJourneyIntoMovies.UI;
using UnityEngine.UI;

namespace ARJourneyIntoMovies.AR
{
    /// <summary>
    /// Controls navigation guidance (arrows, path lines, hints)
    /// Calculates distance and direction to target, updates HUD
    /// </summary>
    public class GuidanceController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("PoseFusion component for target pose")]
        public PoseFusion poseFusion;

        [Tooltip("AR Camera transform")]
        public Transform arCamera;

        [Tooltip("Arrow instance (manually placed in scene or prefab)")]
        public GameObject arrowInstance;

        [Tooltip("HUD component for text updates")]
        public CanvasHUD canvasHUD;

        [Tooltip("OverlayManager for switching to overlay mode")]
        public OverlayManager overlayManager;

        [Header("Arrow Settings")]
        public Sprite LeftArrow;             
        public Sprite SlightlyLeftArrow;     
        public Sprite RightArrow;
        public Sprite SlightlyRightArrow;
        public Sprite StraightArrow;     
        public Image ArrowIcon;

        [Tooltip("Distance in front of camera to place arrow (meters)")]
        public float arrowDistanceFromCamera = 1.5f;

        [Tooltip("Height offset for arrow above camera forward direction")]
        public float arrowHeightOffset = -0.2f;

        [Tooltip("Arrow scale multiplier")]
        public float arrowScale = 0.3f;

        [Header("Smoothing")]
        [Tooltip("Low-pass filter factor (0-1, higher = less smoothing)")]
        [Range(0.01f, 1f)]
        public float smoothFactor = 0.15f;

        [Header("Thresholds")]
        [Tooltip("Distance threshold to switch to overlay mode (meters)")]
        public float overlayDistanceThreshold = 0.5f;

        [Tooltip("Angle threshold (yaw) to switch to overlay mode (degrees)")]
        public float overlayAngleThreshold = 6f;

        [Tooltip("Angle threshold to show 'aligned' hint (degrees)")]
        public float alignedAngleThreshold = 10f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        [Tooltip("Show frame-by-frame updates in console")]
        [SerializeField] private bool showFrameUpdates = false;

        // Smoothed values
        private float smoothedDistance = 0f;
        private float smoothedYaw = 0f;

        // Last hint to avoid spamming console
        private string lastHint = "";
        private string lastStatus = "";

        // Is guidance active?
        private bool isActive = false;

        // Overlay trigger state
        private bool overlayTriggered = false;

        // Cooldown time after returning from overlay (prevent immediate re-trigger)
        private float overlayCooldownTime = 2f; // 2 seconds cooldown
        private float lastOverlayExitTime = -999f;

        // Target pose cache
        private Matrix4x4 cachedTargetPose = Matrix4x4.identity;

        private void Awake()
        {
            Debug.Log("[GuidanceController] Initialized");

            // Set arrow scale and ensure it's visible for debugging
            if (arrowInstance != null)
            {
                arrowInstance.transform.localScale = Vector3.one * arrowScale;

                // Check if arrow has required components
                MeshRenderer renderer = arrowInstance.GetComponent<MeshRenderer>();
                MeshFilter filter = arrowInstance.GetComponent<MeshFilter>();

                if (renderer == null)
                {
                    Debug.LogError("[GuidanceController] NavigationArrow missing MeshRenderer!");
                }
                if (filter == null || filter.sharedMesh == null)
                {
                    Debug.LogError("[GuidanceController] NavigationArrow missing MeshFilter or Mesh!");
                }

                Debug.Log($"[GuidanceController] Arrow setup - Scale: {arrowScale}, Active: {arrowInstance.activeSelf}");
            }
            else
            {
                Debug.LogWarning("[GuidanceController] Arrow instance is null!");
            }
        }

        private void OnEnable()
        {
            // Subscribe to pose updates
            if (poseFusion != null)
            {
                poseFusion.OnPoseUpdated += HandlePoseUpdated;
                Debug.Log("[GuidanceController] Subscribed to PoseFusion.OnPoseUpdated");
            }
        }

        private void OnDisable()
        {
            if (poseFusion != null)
            {
                poseFusion.OnPoseUpdated -= HandlePoseUpdated;
            }
        }

        /// <summary>
        /// Handle pose updated event
        /// </summary>
        private void HandlePoseUpdated(Server.PoseData pose)
        {
            if (pose == null || !pose.success) return;

            // Cache target pose
            cachedTargetPose = poseFusion.GetTargetPoseInARWorld();
            isActive = true;

            // Show arrow
            if (arrowInstance != null)
            {
                arrowInstance.SetActive(true);
                Debug.Log($"[GuidanceController] Arrow activated - Pos: {arrowInstance.transform.position}, Active: {arrowInstance.activeSelf}");
            }
            else
            {
                Debug.LogWarning("[GuidanceController] Arrow instance is null when trying to activate!");
            }

            if (showDebugInfo)
            {
                Debug.Log("[GuidanceController] Pose updated, guidance active");
            }
        }

        private void Update()
        {
            if (!isActive || arCamera == null) return;

            // Update guidance visuals
            UpdateGuidance();
        }

        /// <summary>
        /// Update guidance visuals (called each frame)
        /// </summary>
        private void UpdateGuidance()
        {
            // Get target position
            Vector3 targetPosition = MatrixHelper.GetPosition(cachedTargetPose);

            // Calculate distance
            float currentDistance = Vector3.Distance(arCamera.position, targetPosition);

            // Calculate horizontal direction (ignore Y axis)
            Vector3 cameraPos2D = new Vector3(arCamera.position.x, 0f, arCamera.position.z);
            Vector3 targetPos2D = new Vector3(targetPosition.x, 0f, targetPosition.z);
            Vector3 directionToTarget2D = (targetPos2D - cameraPos2D).normalized;
            Vector3 cameraForward2D = new Vector3(arCamera.forward.x, 0f, arCamera.forward.z).normalized;

            // Calculate yaw angle (horizontal rotation needed)
            float currentYaw = Vector3.SignedAngle(cameraForward2D, directionToTarget2D, Vector3.up);

            // Apply low-pass filter (smooth transitions)
            smoothedDistance = Mathf.Lerp(smoothedDistance, currentDistance, smoothFactor * Time.deltaTime * 60f);
            smoothedYaw = Mathf.Lerp(smoothedYaw, currentYaw, smoothFactor * Time.deltaTime * 60f);

            // Update arrow position and rotation
            UpdateArrow(targetPosition, directionToTarget2D);

            // Update HUD text
            UpdateHUD(smoothedDistance, smoothedYaw);

            // Debug frame updates
            if (showFrameUpdates)
            {
                Debug.Log($"[GuidanceController] Distance: {smoothedDistance:F2}m, Yaw: {smoothedYaw:F1}°");
            }

            // Check overlay trigger conditions
            CheckOverlayTrigger(smoothedDistance, smoothedYaw);
        }

        /// <summary>
        /// Update arrow position and rotation
        /// </summary>
        private void UpdateArrow(Vector3 targetPosition, Vector3 directionToTarget2D)
        {
            if (arrowInstance == null) return;

            // Place arrow in front of camera
            Vector3 arrowPosition = arCamera.position + arCamera.forward * arrowDistanceFromCamera;
            arrowPosition.y += arrowHeightOffset;

            arrowInstance.transform.position = arrowPosition;

            // Rotate arrow to point towards target (horizontal plane only)
            if (directionToTarget2D.magnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget2D, Vector3.up);
                arrowInstance.transform.rotation = targetRotation;
            }

            // Debug arrow visibility
            if (showFrameUpdates)
            {
                Debug.Log($"[GuidanceController] Arrow - Pos: {arrowPosition}, Active: {arrowInstance.activeSelf}");
            }
        }

        /// <summary>
        /// Update HUD text displays
        /// </summary>
        private void UpdateHUD(float distance, float yaw)
        {
            if (canvasHUD == null) return;

            // Update distance text
            canvasHUD.SetDistance(distance);

            // Generate hint based on angle and distance
            string hint = GenerateHint(yaw, distance);

            // Only update hint if changed (avoid spamming console)
            if (hint != lastHint)
            {
                canvasHUD.SetHint(hint);
                lastHint = hint;
            }

            // Update status
            string status = IsNearTarget() ? "Near target - Ready for overlay" : "Navigating...";

            // Only update status if changed
            if (status != lastStatus)
            {
                canvasHUD.SetStatus(status);
                lastStatus = status;
            }
        }

        /// <summary>
        /// Generate directional hint text
        /// </summary>
        private string GenerateHint(float yaw, float distance)
        {
            // Check alignment
            if (Mathf.Abs(yaw) < alignedAngleThreshold)
            {
                ArrowIcon.sprite = StraightArrow;
                if (distance < overlayDistanceThreshold * 1.5f)
                {
                    return "Almost there!";
                }
                return "Keep going straight";
            }

            // Directional hints
            if (yaw > 45f)
            {
                ArrowIcon.sprite = RightArrow;
                return "Turn right";
            }
            else if (yaw > 15f)
            {
                ArrowIcon.sprite = SlightlyRightArrow;
                return "Turn slightly right";
            }
            else if (yaw < -45f)
            {
                ArrowIcon.sprite = LeftArrow;
                return "Turn left";
            }
            else if (yaw < -15f)
            {
                ArrowIcon.sprite = SlightlyLeftArrow;
                return "Turn slightly left";
            }

            return "Adjust direction";
        }

        /// <summary>
        /// Calculate distance from AR camera to target
        /// </summary>
        public float GetDistanceToTarget()
        {
            if (arCamera == null) return float.MaxValue;

            Vector3 targetPosition = MatrixHelper.GetPosition(cachedTargetPose);
            float distance = Vector3.Distance(arCamera.position, targetPosition);

            return distance;
        }

        /// <summary>
        /// Check if user is close enough to switch to overlay mode
        /// </summary>
        public bool IsNearTarget()
        {
            return GetDistanceToTarget() < overlayDistanceThreshold;
        }

        /// <summary>
        /// Check if overlay should be triggered
        /// Conditions: distance < threshold AND |yaw| < angle threshold
        /// </summary>
        private void CheckOverlayTrigger(float distance, float yaw)
        {
            // Already triggered, don't trigger again
            if (overlayTriggered) return;

            // Check cooldown (prevent immediate re-trigger after exiting overlay)
            if (Time.time - lastOverlayExitTime < overlayCooldownTime)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[GuidanceController] Overlay cooldown active: {overlayCooldownTime - (Time.time - lastOverlayExitTime):F1}s remaining");
                }
                return;
            }

            // Check both distance and angle conditions
            bool distanceCondition = distance < overlayDistanceThreshold;
            bool angleCondition = Mathf.Abs(yaw) < overlayAngleThreshold;

            if (distanceCondition && angleCondition)
            {
                // Trigger overlay mode
                TriggerOverlay();
            }
        }

        /// <summary>
        /// Trigger overlay mode (called when conditions are met)
        /// </summary>
        private void TriggerOverlay()
        {
            overlayTriggered = true;

            Debug.Log($"[GuidanceController] Overlay triggered! Distance: {smoothedDistance:F2}m, Yaw: {smoothedYaw:F1}°");

            // Call OverlayManager to show overlay
            if (overlayManager != null)
            {
                // overlayManager.ShowOverlayWithPlaceholder();
                overlayManager.ShowOverlayWithTestPhoto();
            }
            else
            {
                Debug.LogWarning("[GuidanceController] OverlayManager reference is null!");
            }
        }

        /// <summary>
        /// Reset overlay trigger state (called when returning to guidance mode)
        /// </summary>
        public void ResetOverlayTrigger()
        {
            overlayTriggered = false;
            lastOverlayExitTime = Time.time; // Record exit time for cooldown
            Debug.Log($"[GuidanceController] Overlay trigger reset, cooldown: {overlayCooldownTime}s");
        }

        /// <summary>
        /// Show/hide guidance
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;

            if (arrowInstance != null)
            {
                arrowInstance.SetActive(active);
            }

            // Reset overlay trigger when reactivating guidance
            if (active)
            {
                ResetOverlayTrigger();
            }

            Debug.Log($"[GuidanceController] Guidance active: {active}");
        }

        /// <summary>
        /// Manually set target pose (for testing)
        /// </summary>
        public void SetTargetPose(Matrix4x4 targetPose)
        {
            cachedTargetPose = targetPose;
            isActive = true;

            if (arrowInstance != null)
            {
                arrowInstance.SetActive(true);
                Debug.Log($"[GuidanceController] Arrow manually activated - Pos: {arrowInstance.transform.position}, LocalPos: {arrowInstance.transform.localPosition}, Active: {arrowInstance.activeSelf}");

                // Force initial position update
                if (arCamera != null)
                {
                    Vector3 testPos = arCamera.position + arCamera.forward * 1.5f;
                    arrowInstance.transform.position = testPos;
                    Debug.Log($"[GuidanceController] Arrow moved to: {testPos} (camera forward)");
                }
            }
            else
            {
                Debug.LogError("[GuidanceController] Arrow instance is NULL in SetTargetPose!");
            }

            Debug.Log("[GuidanceController] Target pose manually set");
        }
    }
}
