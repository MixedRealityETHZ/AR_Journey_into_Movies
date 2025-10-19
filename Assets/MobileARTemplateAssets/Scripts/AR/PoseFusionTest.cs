using UnityEngine;
using ARJourneyIntoMovies.Server;

namespace ARJourneyIntoMovies.AR
{
    /// <summary>
    /// Play mode unit test for PoseFusion ΔT calculation
    /// Attach to a GameObject in scene and trigger tests via Inspector
    /// </summary>
    public class PoseFusionTest : MonoBehaviour
    {
        [Header("References")]
        public PoseFusion poseFusion;

        [Header("Test Actions")]
        [Tooltip("Run basic ΔT calculation test")]
        public bool runBasicTest = false;

        [Tooltip("Run identity transform test")]
        public bool runIdentityTest = false;

        [Tooltip("Run translation test")]
        public bool runTranslationTest = false;

        [Header("Test Camera Simulation")]
        [Tooltip("Simulated AR camera position")]
        public Vector3 arCameraPosition = new Vector3(1f, 0.5f, 3f);

        [Tooltip("Simulated AR camera rotation (Euler angles)")]
        public Vector3 arCameraRotation = new Vector3(0f, 45f, 0f);

        private Transform testCameraTransform;

        private void Awake()
        {
            // Create a test camera transform
            GameObject testCamera = new GameObject("TestARCamera");
            testCameraTransform = testCamera.transform;
            testCameraTransform.SetParent(transform);
        }

        private void Update()
        {
            if (runBasicTest)
            {
                runBasicTest = false;
                RunBasicTest();
            }

            if (runIdentityTest)
            {
                runIdentityTest = false;
                RunIdentityTest();
            }

            if (runTranslationTest)
            {
                runTranslationTest = false;
                RunTranslationTest();
            }
        }

        /// <summary>
        /// Test 1: Basic ΔT calculation with offset AR camera
        /// AR Camera at (1, 0.5, 3), Server says camera at (0, 0, 2) in map
        /// Target is also at (0, 0, 2) in map (same as query camera)
        /// Expected: Target at AR camera position
        /// </summary>
        [ContextMenu("Test 1: Basic ΔT Calculation")]
        public void RunBasicTest()
        {
            Debug.Log("=== [PoseFusionTest] Test 1: Basic ΔT Calculation ===");

            if (poseFusion == null)
            {
                Debug.LogError("[PoseFusionTest] PoseFusion reference is null!");
                return;
            }

            // Reset ΔT
            poseFusion.ResetDeltaT();

            // Set up test AR camera
            testCameraTransform.position = arCameraPosition;
            testCameraTransform.rotation = Quaternion.Euler(arCameraRotation);

            // Update PoseFusion's AR camera reference
            poseFusion.arCamera = testCameraTransform;

            Debug.Log($"[PoseFusionTest] AR Camera (AR space): {arCameraPosition}, Rotation: {arCameraRotation}");

            // Server says: query camera at (0, 0, 2) in map space
            PoseData mockPose = new PoseData
            {
                success = true,
                rotation = new float[] { 1f, 0f, 0f, 0f }, // Identity rotation
                translation = new float[] { 0f, 0f, 2f },   // Query camera position in map
                fov = 60f,
                aspect = 16f / 9f,
                movie_frame_id = "test_frame_001",
                confidence = 0.95f
            };

            Debug.Log($"[PoseFusionTest] Server says camera (Map space): {mockPose.GetTranslation()}");

            // Apply pose (this will calculate ΔT)
            poseFusion.ApplyServerPose(mockPose);

            // Get target pose in AR world (target = query camera in map)
            Matrix4x4 targetPose = poseFusion.GetTargetPoseInARWorld();
            Vector3 targetPos = MatrixHelper.GetPosition(targetPose);
            Quaternion targetRot = MatrixHelper.GetRotation(targetPose);

            Debug.Log($"[PoseFusionTest] Target (same as query camera in map) -> AR world: {targetPos}");
            Debug.Log($"[PoseFusionTest] Expected: {arCameraPosition} (AR camera position)");

            float distance = Vector3.Distance(arCameraPosition, targetPos);
            Debug.Log($"[PoseFusionTest] Distance difference: {distance:F3}m");
            Debug.Log($"[PoseFusionTest] Match: {distance < 0.01f} ✅");

            Debug.Log("=== Test 1 Complete ===\n");
        }

        /// <summary>
        /// Test 2: Same position test
        /// AR Camera at origin, Server says camera at (0, 0, 2) in map space
        /// Target is also at (0, 0, 2) in map space (same as query camera)
        /// Expected: Target at (0, 0, 0) in AR world (same as AR camera)
        /// </summary>
        [ContextMenu("Test 2: Same Position Test")]
        public void RunIdentityTest()
        {
            Debug.Log("=== [PoseFusionTest] Test 2: Same Position Test ===");

            if (poseFusion == null)
            {
                Debug.LogError("[PoseFusionTest] PoseFusion reference is null!");
                return;
            }

            // Reset
            poseFusion.ResetDeltaT();

            // AR camera at origin
            testCameraTransform.position = Vector3.zero;
            testCameraTransform.rotation = Quaternion.identity;
            poseFusion.arCamera = testCameraTransform;

            Debug.Log("[PoseFusionTest] AR Camera (AR space): (0, 0, 0)");

            // Server says: query camera at (0, 0, 2) in map space
            PoseData mockPose = new PoseData
            {
                success = true,
                rotation = new float[] { 1f, 0f, 0f, 0f },
                translation = new float[] { 0f, 0f, 2f }, // Query camera position in map
                fov = 60f,
                aspect = 16f / 9f,
                movie_frame_id = "test_same_position",
                confidence = 1f
            };

            Debug.Log("[PoseFusionTest] Server says camera (Map space): (0, 0, 2)");

            poseFusion.ApplyServerPose(mockPose);

            // Target is the same pose (query camera location)
            Matrix4x4 targetPose = poseFusion.GetTargetPoseInARWorld();
            Vector3 targetPos = MatrixHelper.GetPosition(targetPose);

            Debug.Log($"[PoseFusionTest] ΔT: {poseFusion.GetDeltaT()}");
            Debug.Log($"[PoseFusionTest] Target (same as query camera in map) -> AR world: {targetPos}");
            Debug.Log($"[PoseFusionTest] Expected: (0, 0, 0) because target = query camera = AR camera");
            Debug.Log($"[PoseFusionTest] Match: {Vector3.Distance(Vector3.zero, targetPos) < 0.01f} ✅");

            Debug.Log("=== Test 2 Complete ===\n");
        }

        /// <summary>
        /// Test 3: Translation offset
        /// AR Camera at (5, 0, 0), Server says camera at (0, 0, 2) in map
        /// Target is also at (0, 0, 2) in map (same as query camera)
        /// Expected: Target at (5, 0, 0) in AR world (same as AR camera)
        /// </summary>
        [ContextMenu("Test 3: Translation Test")]
        public void RunTranslationTest()
        {
            Debug.Log("=== [PoseFusionTest] Test 3: Translation Test ===");

            if (poseFusion == null)
            {
                Debug.LogError("[PoseFusionTest] PoseFusion reference is null!");
                return;
            }

            // Reset
            poseFusion.ResetDeltaT();

            // AR camera at (5, 0, 0)
            testCameraTransform.position = new Vector3(5f, 0f, 0f);
            testCameraTransform.rotation = Quaternion.identity;
            poseFusion.arCamera = testCameraTransform;

            Debug.Log("[PoseFusionTest] AR Camera (AR space): (5, 0, 0)");

            // Server says: query camera at (0, 0, 2) in map space
            PoseData mockPose = new PoseData
            {
                success = true,
                rotation = new float[] { 1f, 0f, 0f, 0f },
                translation = new float[] { 0f, 0f, 2f }, // Query camera position in map
                fov = 60f,
                aspect = 16f / 9f,
                movie_frame_id = "test_translation",
                confidence = 1f
            };

            Debug.Log("[PoseFusionTest] Server says camera (Map space): (0, 0, 2)");

            poseFusion.ApplyServerPose(mockPose);

            // Target is the same pose (query camera location in map)
            Matrix4x4 targetPose = poseFusion.GetTargetPoseInARWorld();
            Vector3 targetPos = MatrixHelper.GetPosition(targetPose);

            Debug.Log($"[PoseFusionTest] Target (same as query camera in map) -> AR world: {targetPos}");
            Debug.Log($"[PoseFusionTest] Expected: (5, 0, 0) because target = query camera = AR camera");
            Debug.Log($"[PoseFusionTest] Match: {Vector3.Distance(new Vector3(5, 0, 0), targetPos) < 0.01f} ✅");

            Debug.Log("=== Test 3 Complete ===\n");
        }
    }
}
