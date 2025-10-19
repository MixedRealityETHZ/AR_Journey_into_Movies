using UnityEngine;
using UnityEngine.XR.ARFoundation;
using ARJourneyIntoMovies.Server;
using ARJourneyIntoMovies.AR;

namespace ARJourneyIntoMovies.UI
{
    /// <summary>
    /// Handles UI button click events
    /// TODO: Step 4 - Implement camera capture and server communication
    /// </summary>
    public class ButtonEvents : MonoBehaviour
    {
        [Header("Manager References")]
        public ServerClient serverClient;
        public PoseFusion poseFusion;
        public GuidanceController guidanceController;
        public OverlayManager overlayManager;
        public CanvasHUD canvasHUD;

        [Header("AR Components")]
        public ARCameraManager arCameraManager;

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
            {
                canvasHUD.SetStatus("Localizing...");
            }

            if (serverClient == null)
            {
                Debug.LogError("[ButtonEvents] ServerClient reference is null!");
                return;
            }

            // Use dummy texture for testing (both Editor and Device)
            // TODO: Later replace with actual ARCameraManager.TryAcquireLatestCpuImage() or image picker
            Debug.Log("[ButtonEvents] Using dummy image for testing");
            Texture2D dummyTexture = CreateDummyTexture(1920, 1080);
            Vector4 dummyIntrinsics = new Vector4(1000f, 1000f, 960f, 540f); // fx, fy, cx, cy
            StartCoroutine(serverClient.SendQueryImage(dummyTexture, dummyIntrinsics));
        }

        /// <summary>
        /// Create dummy texture for Editor testing
        /// </summary>
        private Texture2D CreateDummyTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);

            // Fill with gradient pattern
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float r = (float)x / width;
                    float g = (float)y / height;
                    float b = 0.5f;
                    pixels[y * width + x] = new Color(r, g, b);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
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
        public void OnClickTestMockResponse()
        {
            Debug.Log("[ButtonEvents] OnClickTestMockResponse called");

            if (serverClient != null)
            {
                serverClient.TriggerMockResponse();
            }
        }
    }
}
