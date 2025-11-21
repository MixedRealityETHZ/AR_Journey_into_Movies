using UnityEngine;
using UnityEngine.XR.ARFoundation;
using ARJourneyIntoMovies.AR;
using ARJourneyIntoMovies.Server;
using TMPro;

namespace ARJourneyIntoMovies.UI
{
    /// <summary>
    /// Handles UI button click events
    /// TODO: Step 4 - Implement camera capture and server communication
    /// </summary>
    public class ButtonEvents : MonoBehaviour
    {
        [Header("Manager References")]
        public ARFrameUploader uploader;
        public ServerClient serverClient;
        public PoseFusion poseFusion;
        public GuidanceController guidanceController;
        public OverlayManager overlayManager;
        public CanvasHUD canvasHUD;

        [Header("AR Components")]
        public ARCameraManager arCameraManager;
        [Header("UI Buttons")]
        public GameObject photoButton;

        private void Awake()
        {
            Debug.Log("[ButtonEvents] Initialized (camera capture in Step 4)");
        }

        /// <summary>
        /// Called when "Localize" button is clicked
        /// TODO: Step 4 - Implement actual camera capture from ARCameraManager
        /// </summary>
        public void OnClickLocalize()
        {   
            Debug.Log("[ButtonEvents] OnClickLocalize called");

            if (canvasHUD != null)
                canvasHUD.SetStatus("Localizing...");

            if (serverClient == null)
            {
                Debug.LogError("[ButtonEvents] ServerClient reference is null!");
                return;
            }

            if (uploader == null)
            {
                Debug.LogError("[ButtonEvents] ARFrameUploaderV2 reference is null!");
                return;
            }

            // ⭐⭐⭐ 启动 ARFrameUploader 自动上传
            // uploader.enabled = true;
            // ⭐ 显示拍照按钮
            // if (photoButton != null)
            //     photoButton.SetActive(true);
            // // ⭐ 显示提示用户开始拍照的面板
            // if (localizeInfoPanel != null)
            // {
            //     localizeInfoPanel.SetActive(true);

            //     if (localizeInfoText != null)
            //         localizeInfoText.text = "Connecting to server..."; // 👈 你需要的文本
            // }

            Debug.Log("[ButtonEvents] Localization started — ARFrameUploaderV2 enabled.");
        }

        /// <summary>
        /// Called when "Test Overlay" button is clicked
        /// </summary>
        public void OnClickTestOverlay()
        {
            Debug.Log("[ButtonEvents] OnClickTestOverlay called");

            if (overlayManager != null)
            {
                if (overlayManager.IsOverlayActive())
                {
                    overlayManager.HideOverlay();
                }
                else
                {
                    overlayManager.ShowOverlayWithPlaceholder();
                }
            }
            else
            {
                Debug.LogWarning("[ButtonEvents] OverlayManager reference is null!");
            }
        }

        /// <summary>
        /// Called when "Reset" button is clicked
        /// </summary>
        public void OnClickReset()
        {
            Debug.Log("[ButtonEvents] OnClickReset called");

            if (guidanceController != null)
            {
                guidanceController.SetActive(false);
            }

            if (overlayManager != null)
            {
                overlayManager.HideOverlay();
            }

            if (canvasHUD != null)
            {
                canvasHUD.SetHint("Ready to localize");
                canvasHUD.SetStatus("");
            }
        }

        /// <summary>
        /// Test button - trigger mock server response
        /// </summary>
        public void OnClickTestServerResponse()
        {
            Debug.Log("[ButtonEvents] OnClickTestServerResponse called");

            if (serverClient != null)
            {
                serverClient.TriggerMockResponse();
            }
            else
            {
                Debug.LogWarning("[ButtonEvents] ServerClient reference is null!");
            }
        }
    }
}
