using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ARJourneyIntoMovies.Server;

namespace ARJourneyIntoMovies.AR
{
    public class FrustumPoseARController : MonoBehaviour
    {
        [Header("Target Object")]
        public Transform frustumTarget;  // 👈 要控制的四棱锥对象
        [Header("AR Settings")]
        public ARCameraManager cameraManager;
        public bool convertToOpenCVCamera = false;
        public bool verboseLog = false;
        public float updateInterval = 0.05f;

        [Header("UI References")]
        public TMP_InputField inputQx, inputQy, inputQz, inputQw;
        public TMP_InputField inputTx, inputTy, inputTz;
        public Button toggleFollowButton;
        public Button applyPoseButton;
        public TMP_Text modeLabel;

        [Header("Events")]
        public Action<PoseData> OnPoseUpdated; // 发送给 ServerClient

        private bool followCameraRotation = true;
        private float timer = 0f;
        private Quaternion currentRotation = Quaternion.identity;
        private Vector3 currentTranslation = Vector3.zero;

        void Start()
        {
            if (cameraManager == null)
                cameraManager = FindObjectOfType<ARCameraManager>();

            if (toggleFollowButton != null)
                toggleFollowButton.onClick.AddListener(ToggleFollowMode);

            if (applyPoseButton != null)
                applyPoseButton.onClick.AddListener(ReadInputsAndApplyPose);

            UpdateModeLabel();
            UpdateInputFields();
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= updateInterval)
            {
                timer = 0f;

                if (followCameraRotation)
                    UpdateRotationFromCamera();
            }
        }

        // 📱 从相机更新旋转并实时推送 pose
        void UpdateRotationFromCamera()
        {
            if (ARSession.state <= ARSessionState.Ready) return;

            Camera cam = cameraManager.GetComponent<Camera>();
            if (cam == null) return;

            Quaternion qUnity = cam.transform.rotation;
            if (convertToOpenCVCamera)
                qUnity = qUnity * Quaternion.Euler(0f, 180f, 0f);

            currentRotation = qUnity;
            if (frustumTarget != null)
                frustumTarget.rotation = currentRotation;

            UpdateInputFields();

            // 推送新的姿态（顶点在相机原点）
            PoseData pose = new PoseData
            {
                success = true,
                rotation = new float[] { qUnity.w, qUnity.x, qUnity.y, qUnity.z },
                translation = new float[] { currentTranslation.x, currentTranslation.y, currentTranslation.z },
                fov = 60f,
                aspect = 9f / 16f,
                movie_frame_id = "auto_mock",
                confidence = 1.0f
            };

            OnPoseUpdated?.Invoke(pose);

            if (verboseLog)
                Debug.Log($"[FrustumPoseARController] Auto rotation sent: {currentRotation.eulerAngles}");
        }

        // 🔘 切换模式
        void ToggleFollowMode()
        {
            followCameraRotation = !followCameraRotation;
            UpdateModeLabel();

            if (followCameraRotation)
            {
                // ✅ 切回自动模式时，只更新旋转，不动位置
                Camera cam = cameraManager.GetComponent<Camera>();
                if (cam != null)
                {
                    currentRotation = cam.transform.rotation;
                    if (convertToOpenCVCamera)
                        currentRotation = currentRotation * Quaternion.Euler(0f, 180f, 0f);

                    if (frustumTarget != null)
                        frustumTarget.rotation = currentRotation;
                }

                Debug.Log("[FrustumPoseARController] Switched to AUTO mode (keeping last position).");
            }
            else
            {
                Debug.Log("[FrustumPoseARController] Switched to MANUAL mode.");
            }
        }

        void UpdateModeLabel()
        {
            if (modeLabel != null)
                modeLabel.text = followCameraRotation ? "Mode: AUTO (Following Camera)" : "Mode: MANUAL (User Input)";
        }

        // 🧮 手动输入并应用
        public void ReadInputsAndApplyPose()
        {
            if (followCameraRotation)
            {
                Debug.LogWarning("[FrustumPoseARController] Cannot apply manual pose in AUTO mode.");
                return;
            }

            try
            {
                float qx = float.Parse(inputQx.text);
                float qy = float.Parse(inputQy.text);
                float qz = float.Parse(inputQz.text);
                float qw = float.Parse(inputQw.text);

                float tx = float.Parse(inputTx.text);
                float ty = float.Parse(inputTy.text);
                float tz = float.Parse(inputTz.text);

                currentRotation = new Quaternion(qx, qy, qz, qw).normalized;
                currentTranslation = new Vector3(tx, ty, tz);

                if (frustumTarget != null)
                    frustumTarget.SetPositionAndRotation(currentTranslation, currentRotation);
                UpdateInputFields();

                // 推送手动姿态
                PoseData pose = new PoseData
                {
                    success = true,
                    rotation = new float[] { qw, qx, qy, qz },
                    translation = new float[] { tx, ty, tz },
                    fov = 60f,
                    aspect = 16f / 9f,
                    movie_frame_id = "manual_mock",
                    confidence = 1.0f
                };
                OnPoseUpdated?.Invoke(pose);

                if (verboseLog)
                    Debug.Log($"[FrustumPoseARController] Manual pose applied:\nPos={currentTranslation}\nRot={currentRotation}");
            }
            catch
            {
                Debug.LogWarning("[FrustumPoseARController] Invalid input format.");
            }
        }

        // 🧾 输入框更新
        void UpdateInputFields()
        {
            inputQx?.SetTextWithoutNotify(currentRotation.x.ToString("F3"));
            inputQy?.SetTextWithoutNotify(currentRotation.y.ToString("F3"));
            inputQz?.SetTextWithoutNotify(currentRotation.z.ToString("F3"));
            inputQw?.SetTextWithoutNotify(currentRotation.w.ToString("F3"));

            inputTx?.SetTextWithoutNotify(currentTranslation.x.ToString("F3"));
            inputTy?.SetTextWithoutNotify(currentTranslation.y.ToString("F3"));
            inputTz?.SetTextWithoutNotify(currentTranslation.z.ToString("F3"));
        }
    }
}