using UnityEngine;
using ARJourneyIntoMovies.Server;

namespace ARJourneyIntoMovies.AR
{
    /// <summary>
    /// Play mode test for GuidanceController
    /// Simulates target poses at different distances/angles
    /// </summary>
    public class GuidanceTest : MonoBehaviour
    {
        [Header("References")]
        public GuidanceController guidanceController;

        [Header("Test Scenarios")]
        [Tooltip("Test 1: Target 5m straight ahead")]
        public bool testStraightAhead = false;

        [Tooltip("Test 2: Target 3m to the left")]
        public bool testLeftTurn = false;

        [Tooltip("Test 3: Target 2m to the right")]
        public bool testRightTurn = false;

        [Tooltip("Test 4: Target 0.5m near (overlay trigger)")]
        public bool testNearTarget = false;

        // Internal test camera
        private Transform testCamera;
        private GameObject testCameraObj;

        private void Awake()
        {
            // Create a test camera transform (separate from Main Camera)
            testCameraObj = new GameObject("TestCameraTransform");
            testCamera = testCameraObj.transform;
            testCamera.SetParent(transform);
            testCamera.position = Vector3.zero;
            testCamera.rotation = Quaternion.identity;

            Debug.Log("[GuidanceTest] Test camera created (independent of Main Camera)");
        }

        private void OnDestroy()
        {
            if (testCameraObj != null)
            {
                Destroy(testCameraObj);
            }
        }

        private void Update()
        {
            if (testStraightAhead)
            {
                testStraightAhead = false;
                TestStraightAhead();
            }

            if (testLeftTurn)
            {
                testLeftTurn = false;
                TestLeftTurn();
            }

            if (testRightTurn)
            {
                testRightTurn = false;
                TestRightTurn();
            }

            if (testNearTarget)
            {
                testNearTarget = false;
                TestNearTarget();
            }
        }

        /// <summary>
        /// Test 1: Target 5m straight ahead
        /// Expected: "Keep going straight", distance ~5m
        /// </summary>
        [ContextMenu("Test 1: Straight Ahead")]
        public void TestStraightAhead()
        {
            Debug.Log("=== [GuidanceTest] Test 1: Straight Ahead ===");

            if (guidanceController == null || testCamera == null)
            {
                Debug.LogError("[GuidanceTest] References not set!");
                return;
            }

            // IMPORTANT: Temporarily replace GuidanceController's AR camera with test camera
            Transform originalCamera = guidanceController.arCamera;
            guidanceController.arCamera = testCamera;

            // Camera at origin, facing Z+
            testCamera.position = Vector3.zero;
            testCamera.rotation = Quaternion.identity;

            // Target 5m ahead in Z+ direction
            Vector3 targetPosition = new Vector3(0f, 0f, 5f);
            Quaternion targetRotation = Quaternion.identity;

            Matrix4x4 targetPose = MatrixHelper.TRS(targetPosition, targetRotation);
            guidanceController.SetTargetPose(targetPose);

            Debug.Log($"[GuidanceTest] Test Camera: {testCamera.position}, Rotation: {testCamera.rotation.eulerAngles}");
            Debug.Log($"[GuidanceTest] Target: {targetPosition}");
            Debug.Log($"[GuidanceTest] Expected: 'Keep going straight', Distance: 5.0m");
            Debug.Log("=== Test 1 Complete ===\n");

            // Note: Camera will be restored when test ends or when real pose is received
        }

        /// <summary>
        /// Test 2: Target 3m to the left (90° turn required)
        /// Expected: "Turn left", distance ~3m
        /// </summary>
        [ContextMenu("Test 2: Left Turn")]
        public void TestLeftTurn()
        {
            Debug.Log("=== [GuidanceTest] Test 2: Left Turn ===");

            if (guidanceController == null || testCamera == null)
            {
                Debug.LogError("[GuidanceTest] References not set!");
                return;
            }

            // Replace camera
            guidanceController.arCamera = testCamera;

            // Camera at origin, facing Z+
            testCamera.position = Vector3.zero;
            testCamera.rotation = Quaternion.identity;

            // Target 3m to the left (X- direction)
            Vector3 targetPosition = new Vector3(-3f, 0f, 0f);
            Quaternion targetRotation = Quaternion.identity;

            Matrix4x4 targetPose = MatrixHelper.TRS(targetPosition, targetRotation);
            guidanceController.SetTargetPose(targetPose);

            Debug.Log($"[GuidanceTest] Test Camera: {testCamera.position}, Target: {targetPosition}");
            Debug.Log($"[GuidanceTest] Expected: 'Turn left', Distance: 3.0m");
            Debug.Log("=== Test 2 Complete ===\n");
        }

        /// <summary>
        /// Test 3: Target 2m to the right (90° turn required)
        /// Expected: "Turn right", distance ~2m
        /// </summary>
        [ContextMenu("Test 3: Right Turn")]
        public void TestRightTurn()
        {
            Debug.Log("=== [GuidanceTest] Test 3: Right Turn ===");

            if (guidanceController == null || testCamera == null)
            {
                Debug.LogError("[GuidanceTest] References not set!");
                return;
            }

            // Replace camera
            guidanceController.arCamera = testCamera;

            // Camera at origin, facing Z+
            testCamera.position = Vector3.zero;
            testCamera.rotation = Quaternion.identity;

            // Target 2m to the right (X+ direction)
            Vector3 targetPosition = new Vector3(2f, 0f, 0f);
            Quaternion targetRotation = Quaternion.identity;

            Matrix4x4 targetPose = MatrixHelper.TRS(targetPosition, targetRotation);
            guidanceController.SetTargetPose(targetPose);

            Debug.Log($"[GuidanceTest] Test Camera: {testCamera.position}, Target: {targetPosition}");
            Debug.Log($"[GuidanceTest] Expected: 'Turn right', Distance: 2.0m");
            Debug.Log("=== Test 3 Complete ===\n");
        }

        /// <summary>
        /// Test 4: Target very close (0.5m) - should trigger overlay mode
        /// Expected: "Near target - Ready for overlay", distance < 1.5m
        /// </summary>
        [ContextMenu("Test 4: Near Target")]
        public void TestNearTarget()
        {
            Debug.Log("=== [GuidanceTest] Test 4: Near Target ===");

            if (guidanceController == null || testCamera == null)
            {
                Debug.LogError("[GuidanceTest] References not set!");
                return;
            }

            // Replace camera
            guidanceController.arCamera = testCamera;

            // Camera at origin, facing Z+
            testCamera.position = Vector3.zero;
            testCamera.rotation = Quaternion.identity;

            // Target 0.5m ahead (very close)
            Vector3 targetPosition = new Vector3(0f, 0f, 0.5f);
            Quaternion targetRotation = Quaternion.identity;

            Matrix4x4 targetPose = MatrixHelper.TRS(targetPosition, targetRotation);
            guidanceController.SetTargetPose(targetPose);

            Debug.Log($"[GuidanceTest] Test Camera: {testCamera.position}, Target: {targetPosition}");
            Debug.Log($"[GuidanceTest] Expected: 'Near target - Ready for overlay', Distance: 0.5m");
            Debug.Log($"[GuidanceTest] IsNearTarget: {guidanceController.IsNearTarget()} (should be True)");
            Debug.Log("=== Test 4 Complete ===\n");
        }
    }
}
