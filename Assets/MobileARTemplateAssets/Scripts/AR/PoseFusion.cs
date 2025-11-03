using System;
using UnityEngine;
using ARJourneyIntoMovies.Server;

namespace ARJourneyIntoMovies.AR
{
    /// <summary>
    /// Manages coordinate transformation between server map space and AR world space
    /// ΔT = map-to-AR-world transform
    /// Calculates ΔT on first localization: ΔT = T_ar_current * inv(T_map_target)
    /// </summary>
    public class PoseFusion : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Server client to listen for pose events")]
        public ServerClient serverClient;

        [Tooltip("AR Camera transform for pose tracking")]
        public Transform arCamera;

        [Header("Settings")]
        [Tooltip("Auto-calculate ΔT on first pose received")]
        public bool autoCalculateDeltaT = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Events
        public event Action<PoseData> OnPoseUpdated;

        // Transform from map space to AR world space
        private Matrix4x4 deltaT = Matrix4x4.identity;

        // Last received server pose (in map space)
        private PoseData lastServerPose;

        // Current AR camera pose (in AR world space)
        private Matrix4x4 currentARCameraPose = Matrix4x4.identity;
        // Target pose in AR world (cached for visualization)
        private Matrix4x4 targetPoseInARWorld = Matrix4x4.identity;

        // AR camera pose at the time of query (used for ΔT calculation)
        private Matrix4x4 arCameraPoseAtQuery = Matrix4x4.identity;

        // Is ΔT calculated?
        private bool isDeltaTCalculated = false;

        private void Awake()
        {
            Debug.Log("[PoseFusion] Initialized - ΔT = Identity (placeholder)");
        }

        private void OnEnable()
        {
            // Subscribe to ServerClient events
            if (serverClient != null)
            {
                serverClient.OnPoseReceived += HandlePoseReceived;
                Debug.Log("[PoseFusion] Subscribed to ServerClient.OnPoseReceived");
            }
            else
            {
                Debug.LogWarning("[PoseFusion] ServerClient reference is null!");
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (serverClient != null)
            {
                serverClient.OnPoseReceived -= HandlePoseReceived;
            }
        }

        /// <summary>
        /// Handle pose received event from ServerClient
        /// </summary>
        private void HandlePoseReceived(PoseData pose)
        {
            Debug.Log("[PoseFusion] HandlePoseReceived called");
            ApplyServerPose(pose);

            // Trigger our own event for other components
            OnPoseUpdated?.Invoke(pose);
        }

        /// <summary>
        /// Apply server-returned pose (in map space)
        /// Calculates ΔT on first localization
        /// </summary>
        public void ApplyServerPose(PoseData pose)
        {
            if (pose == null || !pose.success)
            {
                Debug.LogWarning("[PoseFusion] Invalid or failed pose data received");
                return;
            }

            lastServerPose = pose;

            if (showDebugInfo)
            {
                Debug.Log($"[PoseFusion] Server pose received:");
                Debug.Log($"  - Translation: {pose.GetTranslation()}");
                Debug.Log($"  - Rotation: {pose.GetRotation().eulerAngles}");
                Debug.Log($"  - FOV: {pose.fov}°, Aspect: {pose.aspect}");
                Debug.Log($"  - Confidence: {pose.confidence}");
            }

            // Calculate ΔT on first localization
            if (!isDeltaTCalculated && autoCalculateDeltaT)
            {
                CalculateDeltaT(pose);
            }
        }

        /// <summary>
        /// Calculate ΔT transformation
        /// Establishes mapping: point_AR = ΔT * point_map
        /// ΔT represents the transformation from map origin to AR origin
        /// </summary>
        private void CalculateDeltaT(PoseData serverPose)
        {
            // Update AR camera pose (current position)
            UpdateARCameraPose(arCamera);

            // Get server pose matrix (in map space)
            // This is where the AR camera is located in the map coordinate system
            Matrix4x4 T_map_arcam = serverPose.GetPoseMatrix();

            // Current AR camera pose (in AR world space)
            Matrix4x4 T_ar_arcam = currentARCameraPose;

            // Calculate ΔT
            // We want: T_ar_arcam = ΔT * T_map_arcam
            // Therefore: ΔT = T_ar_arcam * inv(T_map_arcam)
            Matrix4x4 T_map_arcam_inv = MatrixHelper.InverseTransform(T_map_arcam);
            deltaT = MatrixHelper.Multiply(T_ar_arcam, T_map_arcam_inv);

            // Validate result
            if (!MatrixHelper.IsValid(deltaT))
            {
                Debug.LogError("[PoseFusion] Calculated ΔT is invalid! Using identity.");
                deltaT = Matrix4x4.identity;
                return;
            }

            isDeltaTCalculated = true;

            if (showDebugInfo)
            {
                Debug.Log("[PoseFusion] ΔT calculated successfully!");
                Debug.Log($"[PoseFusion] AR Camera (AR space): pos={MatrixHelper.GetPosition(T_ar_arcam)}, rot={MatrixHelper.GetRotation(T_ar_arcam).eulerAngles}");
                Debug.Log($"[PoseFusion] AR Camera (Map space): pos={MatrixHelper.GetPosition(T_map_arcam)}, rot={MatrixHelper.GetRotation(T_map_arcam).eulerAngles}");
                Debug.Log($"[PoseFusion] ΔT: pos={MatrixHelper.GetPosition(deltaT)}, rot={MatrixHelper.GetRotation(deltaT).eulerAngles}");

                // Verify: T_ar_arcam should equal ΔT * T_map_arcam
                Matrix4x4 verify = MatrixHelper.Multiply(deltaT, T_map_arcam);
                Debug.Log($"[PoseFusion] Verification (should match AR cam): pos={MatrixHelper.GetPosition(verify)}");
            }
        }

        /// <summary>
        /// Get target camera pose in AR world space
        /// Returns: T_ar_target = ΔT * T_map_target
        /// </summary>
        public Matrix4x4 GetTargetPoseInARWorld()
        {
            if (lastServerPose == null)
            {
                Debug.LogWarning("[PoseFusion] No server pose available, returning identity");
                return Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            }

            // Apply ΔT transformation: targetPose_AR = ΔT * serverPose_map
            Matrix4x4 serverPoseMatrix = lastServerPose.GetPoseMatrix();
            Matrix4x4 targetInARWorld = MatrixHelper.Multiply(deltaT, serverPoseMatrix);

            if (showDebugInfo)
            {
                Vector3 targetPos = MatrixHelper.GetPosition(targetInARWorld);
                Quaternion targetRot = MatrixHelper.GetRotation(targetInARWorld);
                Debug.Log($"[PoseFusion] Target pose in AR world - Pos: {targetPos}, Rot: {targetRot.eulerAngles}");
            }

            return targetInARWorld;
        }

        /// <summary>
        /// Update current AR camera pose
        /// </summary>
        public void UpdateARCameraPose(Transform camera)
        {
            if (camera == null)
            {
                Debug.LogWarning("[PoseFusion] AR Camera is null, using identity");
                currentARCameraPose = Matrix4x4.identity;
                return;
            }

            currentARCameraPose = MatrixHelper.TRS(camera.position, camera.rotation);
        }

        /// <summary>
        /// Manually set ΔT (for testing or re-localization)
        /// </summary>
        public void SetDeltaT(Matrix4x4 newDeltaT)
        {
            deltaT = newDeltaT;
            isDeltaTCalculated = true;

            if (showDebugInfo)
            {
                Debug.Log("[PoseFusion] ΔT manually set");
                MatrixHelper.LogMatrix("New ΔT", deltaT);
            }
        }

        /// <summary>
        /// Reset ΔT (allows recalculation on next pose)
        /// </summary>
        public void ResetDeltaT()
        {
            deltaT = Matrix4x4.identity;
            isDeltaTCalculated = false;
            Debug.Log("[PoseFusion] ΔT reset to identity");
        }

        /// <summary>
        /// Get last server pose data (if available)
        /// </summary>
        public PoseData GetLastServerPose()
        {
            return lastServerPose;
        }

        /// <summary>
        /// Get current ΔT transform
        /// </summary>
        public Matrix4x4 GetDeltaT()
        {
            return deltaT;
        }

        /// <summary>
        /// Check if ΔT has been calculated
        /// </summary>
        public bool IsDeltaTCalculated()
        {
            return isDeltaTCalculated;
        }

        public void SetManualPose(Matrix4x4 manualPose)
        {
            // 保存手动姿态
            targetPoseInARWorld = manualPose;

            // 同步生成 PoseData（带平移、旋转、FOV）
            PoseData manualData = new PoseData
            {
                success = true,
                translation = new float[] {
                    manualPose.m03,
                    manualPose.m13,
                    manualPose.m23
                },
                rotation = new float[] {
                    MatrixHelper.GetRotation(manualPose).x,
                    MatrixHelper.GetRotation(manualPose).y,
                    MatrixHelper.GetRotation(manualPose).z,
                    MatrixHelper.GetRotation(manualPose).w
                },
                fov = 60f,   // 给一个默认视角
                aspect = 16f / 9f,
                movie_frame_id = "manual_input",
                confidence = 1.0f
            };

            lastServerPose = manualData; // ✅ 缓存为上一个 pose
            isDeltaTCalculated = true;   // ✅ 防止 identity 触发
            Debug.Log("[PoseFusion] SetManualPose - manual pose injected.");

            // 通知所有监听者
            OnPoseUpdated?.Invoke(manualData);
        }

        
    }
}
