using UnityEngine;
using ARJourneyIntoMovies.Server;

namespace ARJourneyIntoMovies.AR
{
    /// <summary>
    /// Visualizes target camera frustum in AR world space
    /// Instantiates and positions frustum prefab based on server pose
    /// </summary>
    public class TargetFrustumVisualizer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Frustum prefab to instantiate")]
        public GameObject frustumPrefab;

        [Tooltip("PoseFusion component for coordinate transformation")]
        public PoseFusion poseFusion;

        [Header("Settings")]
        [Tooltip("Auto-show frustum when pose is received")]
        public bool autoShow = true;

        // Instantiated frustum object
        private GameObject frustumInstance;
        private FrustumBuilder frustumBuilder;

        // Is frustum currently visible?
        private bool isVisible = false;

        private void Awake()
        {
            Debug.Log("[TargetFrustumVisualizer] Initialized");
        }

        private void OnEnable()
        {
            // Subscribe to PoseFusion events
            if (poseFusion != null)
            {
                poseFusion.OnPoseUpdated += HandlePoseUpdated;
                Debug.Log("[TargetFrustumVisualizer] Subscribed to PoseFusion.OnPoseUpdated");
            }
            else
            {
                Debug.LogWarning("[TargetFrustumVisualizer] PoseFusion reference is null!");
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (poseFusion != null)
            {
                poseFusion.OnPoseUpdated -= HandlePoseUpdated;
            }
        }

        /// <summary>
        /// Handle pose updated event from PoseFusion
        /// </summary>
        private void HandlePoseUpdated(PoseData pose)
        {
            Debug.Log("[TargetFrustumVisualizer] HandlePoseUpdated called");

            // Create frustum instance if not exists
            if (frustumInstance == null)
            {
                CreateFrustumInstance();
            }

            // Update frustum transform and mesh
            UpdateFrustum(pose);

            // Show frustum if auto-show is enabled
            if (autoShow)
            {
                ShowFrustum();
            }
        }

        /// <summary>
        /// Create frustum instance from prefab
        /// </summary>
        private void CreateFrustumInstance()
        {
            if (frustumPrefab == null)
            {
                Debug.LogError("[TargetFrustumVisualizer] Frustum prefab is null!");
                return;
            }

            Debug.Log("[TargetFrustumVisualizer] Creating frustum instance");

            // Instantiate frustum as child of this GameObject
            frustumInstance = Instantiate(frustumPrefab, transform);
            frustumInstance.name = "TargetFrustum";

            // Get FrustumBuilder component
            frustumBuilder = frustumInstance.GetComponent<FrustumBuilder>();
            if (frustumBuilder == null)
            {
                Debug.LogWarning("[TargetFrustumVisualizer] Frustum prefab does not have FrustumBuilder component!");
            }

            // Hide by default
            frustumInstance.SetActive(false);
        }

        /// <summary>
        /// Update frustum position, rotation, and mesh
        /// </summary>
        private void UpdateFrustum(PoseData pose)
        {
            if (frustumInstance == null || poseFusion == null)
            {
                return;
            }

            // Get target pose in AR world space
            Matrix4x4 targetPose = poseFusion.GetTargetPoseInARWorld();

            // Extract position and rotation
            Vector3 position = targetPose.GetPosition();
            Quaternion rotation = targetPose.rotation;

            // Update frustum transform
            frustumInstance.transform.position = position;
            frustumInstance.transform.rotation = rotation;

            Debug.Log($"[TargetFrustumVisualizer] Frustum positioned at {position}, rotation {rotation.eulerAngles}");

            // Update frustum mesh based on FOV and aspect
            if (frustumBuilder != null)
            {
                frustumBuilder.UpdateFrustum(pose.fov, pose.aspect);
                Debug.Log($"[TargetFrustumVisualizer] Frustum mesh updated - FOV: {pose.fov}°, Aspect: {pose.aspect:F2}");
            }
        }

        /// <summary>
        /// Show frustum
        /// </summary>
        public void ShowFrustum()
        {
            if (frustumInstance != null)
            {
                frustumInstance.SetActive(true);
                isVisible = true;
                Debug.Log("[TargetFrustumVisualizer] Frustum visible");
            }
        }

        /// <summary>
        /// Hide frustum
        /// </summary>
        public void HideFrustum()
        {
            if (frustumInstance != null)
            {
                frustumInstance.SetActive(false);
                isVisible = false;
                Debug.Log("[TargetFrustumVisualizer] Frustum hidden");
            }
        }

        /// <summary>
        /// Get frustum visibility state
        /// </summary>
        public bool IsVisible()
        {
            return isVisible;
        }

        /// <summary>
        /// Get frustum instance (for external access)
        /// </summary>
        public GameObject GetFrustumInstance()
        {
            return frustumInstance;
        }

        public void UpdateFrustumPose(Matrix4x4 pose)
        {
            transform.SetPositionAndRotation(
                MatrixHelper.GetPosition(pose),
                MatrixHelper.GetRotation(pose)
            );
        }
    }
}
