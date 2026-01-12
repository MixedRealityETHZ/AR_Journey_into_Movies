using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using ARJourneyIntoMovies.Server;

namespace ARJourneyIntoMovies.AR
{
    /// <summary>
    /// Manages full-screen semi-transparent movie frame overlay
    /// Displays when user is near target location
    /// </summary>
    public class OverlayManager : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Overlay Canvas (Screen Space - Overlay)")]
        public Canvas overlayCanvas;
        public Canvas OriginCanvas;

        [Tooltip("RawImage for full-screen movie frame display")]
        public RawImage overlayImage;

        [Tooltip("Slider for alpha control")]
        public Slider alphaSlider;
        public GameObject spawnedPerson_einstein;  
        public GameObject spawnedPerson_trueman;  
        private GameObject spawnedPerson;

        [Tooltip("Button to return to guidance mode")]
        public Button returnButton;

        [Header("Overlay Settings")]
        [Tooltip("Default alpha value (0-1)")]
        [Range(0f, 1f)]
        public float defaultAlpha = 0.5f;

        [Tooltip("Min alpha value")]
        [Range(0f, 1f)]
        public float minAlpha = 0.3f;

        [Tooltip("Max alpha value")]
        [Range(0f, 1f)]
        public float maxAlpha = 0.7f;

        [Header("References")]
        [Tooltip("GuidanceController to hide/show guidance")]
        public GuidanceController guidanceController;

        [Tooltip("TargetFrustumVisualizer to hide/show frustum")]
        public TargetFrustumVisualizer frustumVisualizer;
        public URPFilterSwitcher filterSwitcher;
        public ARFrameUploader uploader;
        public MovieSceneFrameController movieSceneFrameController;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // Current overlay state
        private bool isOverlayActive = false;

        // Current alpha value
        private float currentAlpha = 0.5f;
        private Texture2D MovieFrameTexture;
        private bool isPhotoSelected = false;
        private LongPressDetector ImageLongPress;
        private bool isPersonVisible = false;

        private void Awake()
        {
            Debug.Log("[OverlayManager] Initialized");

            // Set initial alpha
            currentAlpha = defaultAlpha;

            // Hide overlay by default
            if (overlayCanvas != null)
            {
                overlayCanvas.gameObject.SetActive(false);
            }

            // Setup slider
            if (alphaSlider != null)
            {
                alphaSlider.minValue = minAlpha;
                alphaSlider.maxValue = maxAlpha;
                alphaSlider.value = defaultAlpha;
                alphaSlider.onValueChanged.AddListener(OnAlphaSliderChanged);
            }

            // Setup return button
            if (returnButton != null)
            {
                returnButton.onClick.AddListener(OnReturnButtonClicked);
            }

            ImageLongPress = overlayImage.gameObject.AddComponent<LongPressDetector>();
            ImageLongPress.onLongPress = OnLongPressToggle;
        }

        private void OnDestroy()
        {
            // Cleanup listeners
            if (alphaSlider != null)
            {
                alphaSlider.onValueChanged.RemoveListener(OnAlphaSliderChanged);
            }

            if (returnButton != null)
            {
                returnButton.onClick.RemoveListener(OnReturnButtonClicked);
            }
        }

        public void setMovieFrame()
        {
            var info = movieSceneFrameController.GetSelectedFrameInfo();
            Texture2D texture = info.frameTexture;
            if (texture == null)
                return;

            // Texture2D readableTex = MakeReadable(texture);
            // Texture2D rotated = RotateTexture90(readableTex);
            MovieFrameTexture = texture;
            isPhotoSelected = true;
        }

        private Texture2D RotateTexture90(Texture2D src)
        {
            int width = src.width;
            int height = src.height;

            Texture2D dst = new Texture2D(height, width, src.format, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    dst.SetPixel(height - 1 - y, x, src.GetPixel(x, y));
                }
            }

            dst.Apply();
            return dst;
        }
        private Texture2D MakeReadable(Texture2D nonReadableTexture)
        {
            RenderTexture rt = RenderTexture.GetTemporary(
                nonReadableTexture.width,
                nonReadableTexture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(nonReadableTexture, rt);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D readableTex = new Texture2D(nonReadableTexture.width, nonReadableTexture.height, TextureFormat.RGBA32, false);
            readableTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            readableTex.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            return readableTex;
        }


        /// <summary>
        /// Show overlay with movie frame texture
        /// </summary>
        /// <param name="movieFrame">Movie frame texture</param>
        /// <param name="alpha">Alpha value (0-1), uses default if not specified</param>
        public void ShowOverlay(Texture2D movieFrame, float alpha = -1f)
        {
            if (overlayCanvas == null || overlayImage == null)
            {
                Debug.LogError("[OverlayManager] Overlay Canvas or RawImage is null!");
                return;
            }

            // Use default alpha if not specified
            if (alpha < 0f)
            {
                alpha = defaultAlpha;
            }

            // Clamp alpha
            alpha = Mathf.Clamp(alpha, minAlpha, maxAlpha);
            currentAlpha = alpha;

            // Set texture
            overlayImage.texture = movieFrame;
            // Keep the original frame aspect ratio in the full-screen RawImage

            float aspect = (float)movieFrame.width / movieFrame.height;
            var fitter = overlayImage.GetComponent<AspectRatioFitter>();
            if (fitter == null)
                fitter = overlayImage.gameObject.AddComponent<AspectRatioFitter>();

            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = aspect;


            // Set alpha
            SetAlpha(alpha);

            // Update slider
            if (alphaSlider != null)
            {
                alphaSlider.value = alpha;
            }

            // Show canvas
            OriginCanvas.gameObject.SetActive(false);
            overlayCanvas.gameObject.SetActive(true);
            isOverlayActive = true;

            // Hide guidance elements
            HideGuidanceElements();

            if (showDebugInfo)
            {
                Debug.Log($"[OverlayManager] Overlay shown - Alpha: {alpha:F2}");
            }
        }

        /// <summary>
        /// Show overlay with placeholder texture (for testing)
        /// </summary>
        public void ShowOverlayWithPlaceholder()
        {
            // Create a placeholder texture (gradient)
            Texture2D placeholder = CreatePlaceholderTexture(1920, 1080);
            if (!isPhotoSelected)
                ShowOverlay(placeholder, defaultAlpha);
            else
                ShowOverlay(MovieFrameTexture, defaultAlpha);

            Debug.Log("[OverlayManager] Overlay shown with placeholder texture");
        }

        /// <summary>
        /// Hide overlay and return to guidance mode
        /// </summary>
        public void HideOverlay()
        {
            Debug.Log("[OverlayManager] HideOverlay called");

            if (overlayCanvas != null)
            {
                overlayCanvas.gameObject.SetActive(false);
                Debug.Log("[OverlayManager] OverlayCanvas deactivated");
            }
            else
            {
                Debug.LogWarning("[OverlayManager] OverlayCanvas is null!");
            }

            isOverlayActive = false;
            OriginCanvas.gameObject.SetActive(true);

            // Show guidance elements
            ShowGuidanceElements();

            Debug.Log("[OverlayManager] Overlay hidden, returning to guidance mode");
        }

        /// <summary>
        /// Set overlay alpha (transparency)
        /// </summary>
        /// <param name="alpha">Alpha value (0-1)</param>
        public void SetAlpha(float alpha)
        {
            if (overlayImage == null) return;

            alpha = Mathf.Clamp(alpha, 0f, 1f);
            currentAlpha = alpha;

            Color color = overlayImage.color;
            color.a = alpha;
            overlayImage.color = color;

            if (showDebugInfo)
            {
                Debug.Log($"[OverlayManager] Alpha set to {alpha:F2}");
            }
        }

        /// <summary>
        /// Get current alpha value
        /// </summary>
        public float GetAlpha()
        {
            return currentAlpha;
        }

        /// <summary>
        /// Check if overlay is currently active
        /// </summary>
        public bool IsOverlayActive()
        {
            return isOverlayActive;
        }

        /// <summary>
        /// Load texture from byte array (e.g., from server response)
        /// </summary>
        /// <param name="imageBytes">JPEG/PNG byte array</param>
        /// <returns>Texture2D or null if failed</returns>
        public static Texture2D LoadTextureFromBytes(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                Debug.LogError("[OverlayManager] Image bytes are null or empty!");
                return null;
            }

            Texture2D texture = new Texture2D(2, 2); // Initial size, will be replaced
            bool success = texture.LoadImage(imageBytes); // Automatically resizes

            if (!success)
            {
                Debug.LogError("[OverlayManager] Failed to load image from bytes!");
                return null;
            }

            Debug.Log($"[OverlayManager] Texture loaded from bytes - Size: {texture.width}x{texture.height}");
            return texture;
        }

        /// <summary>
        /// Create a placeholder texture for testing
        /// </summary>
        private Texture2D CreatePlaceholderTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            // Create gradient pattern
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float r = (float)x / width;
                    float g = (float)y / height;
                    float b = 0.5f;
                    pixels[y * width + x] = new Color(r, g, b, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

        /// <summary>
        /// Hide guidance elements (frustum, arrow, HUD hints)
        /// </summary>
        private void HideGuidanceElements()
        {
            // Hide frustum
            if (frustumVisualizer != null)
            {
                frustumVisualizer.HideFrustum();
            }

            // Hide guidance (arrow)
            if (guidanceController != null)
            {
                guidanceController.SetActive(false);
            }

            if (showDebugInfo)
            {
                Debug.Log("[OverlayManager] Guidance elements hidden");
            }
        }

        /// <summary>
        /// Show guidance elements (frustum, arrow, HUD hints)
        /// </summary>
        private void ShowGuidanceElements()
        {
            // Show frustum
            if (frustumVisualizer != null)
            {
                frustumVisualizer.ShowFrustum();
            }

            // Show guidance (arrow)
            if (guidanceController != null)
            {
                guidanceController.SetActive(true);
            }

            if (showDebugInfo)
            {
                Debug.Log("[OverlayManager] Guidance elements shown");
            }
        }

        /// <summary>
        /// Handle alpha slider value change
        /// </summary>
        private void OnAlphaSliderChanged(float value)
        {
            SetAlpha(value);
        }

        /// <summary>
        /// Handle return button click
        /// </summary>
        private void OnReturnButtonClicked()
        {
            Debug.Log("========================================");
            Debug.Log("[OverlayManager] Return button clicked!");
            Debug.Log("========================================");
            filterSwitcher.DisableAllFilters();

            HideOverlay();
        }

        private void OnLongPressToggle()
        {
            float right = 0.0f;
            float up = 0.0f;
            float forward = 1.0f;
            var info = movieSceneFrameController.GetSelectedFrameInfo();
            string movieName = info.movie;
            string sceneName = info.scene;
            string frameId = info.frame;
            if(movieName == "ETHz-CAB" && sceneName == "Outdoor1" && frameId == "frame1")
            {
                spawnedPerson = spawnedPerson_einstein;
                right = 0.31f;
                up = -2.02f;
                forward = 11.0f;
            }
            else if(movieName == "ETHz-HG" && sceneName == "EO-Nord" && frameId == "frame2")
            {
                spawnedPerson = spawnedPerson_trueman;
                right = -0.31f;
                up = -1.22f;
                forward = 10.57f;
            }
            else
            {
                Debug.LogWarning("[OverlayManager] No person prefab assigned for this frame.");
                return;
            }
            // Long-press toggles between showing the captured frame and a scene-specific character prefab
            if (!isPersonVisible)
            {
                // Movie frame → character overlay transition
                StartCoroutine(TransitionOverlayToPerson(right, up, forward));
            }
            else
            {
                // Character → movie frame transition
                StartCoroutine(TransitionPersonToOverlay());
            }
        }

        private IEnumerator TransitionOverlayToPerson(float right, float up, float forward)
        {
            float duration = 0.6f;
            float t = 0;

            CanvasGroup cg = overlayImage.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = overlayImage.gameObject.AddComponent<CanvasGroup>();

            // Spawn/activate the character near the camera using a tuned local offset
            SpawnPersonInWorld(right, up, forward);
            // Start character fully transparent to support fade-in
            Renderer rend = spawnedPerson.GetComponentInChildren<Renderer>();
            Material mat = rend.material;
            Color mc = mat.color; 
            mc.a = 0f;         
            mat.color = mc;

            while (t < duration)
            {
                t += Time.deltaTime;
                float v = t / duration;

                // Fade overlay out (1 → 0)
                cg.alpha = Mathf.Lerp(1f, 0f, v);

                // Fade character in (0 → 1)
                mc.a = Mathf.Lerp(0f, 1f, v);
                mat.color = mc;

                yield return null;
            }
            // spawnedPerson.SetActive(true);
            // overlayImage.gameObject.SetActive(false);
            isPersonVisible = true; 
        }

        private IEnumerator TransitionPersonToOverlay()
        {
            float duration = 0.8f;
            float t = 0;

            if (spawnedPerson == null)
                yield break;

            Renderer rend = spawnedPerson.GetComponentInChildren<Renderer>();
            Material mat = rend.material;
            Color mc = mat.color;

            // Prepare overlay to fade back in
            overlayImage.gameObject.SetActive(true);
            CanvasGroup cg = overlayImage.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = overlayImage.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float v = t / duration;

                // Fade character out
                mc.a = Mathf.Lerp(1f, 0f, v);
                mat.color = mc;

                // Fade overlay in
                cg.alpha = Mathf.Lerp(0f, 0.5f, v);

                yield return null;
            }
            spawnedPerson.SetActive(false);
            isPersonVisible = false; 
        }

        private void SpawnPersonInWorld(float right, float up, float forward)
        {
            if (spawnedPerson == null)
            {
                Debug.LogError("personPrefab not assigned!");
                return;
            }

            // Activate the selected character prefab instance
            spawnedPerson.transform.localScale = new Vector3(
                -spawnedPerson.transform.localScale.x,   // Mirror on X to fix orientation
                spawnedPerson.transform.localScale.y,
                spawnedPerson.transform.localScale.z
            );
            spawnedPerson.SetActive(true);

            // Place relative to the AR camera (local camera-space offsets)
            Transform cam = Camera.main.transform;

            Vector3 newPos =
                cam.position +
                cam.right * right +
                cam.up * up +
                cam.forward * forward;
            spawnedPerson.transform.position = newPos;  

            // Keep character facing the camera (yaw only)
            Vector3 look = cam.position;
            look.y = spawnedPerson.transform.position.y;
            spawnedPerson.transform.LookAt(look);

            Debug.Log("[OverlayManager] Person spawned at local offset (1.47, 0.15, 0.52).");
        }

        // Control spawned character position (camera-local offsets)
        public void UpdatePersonPosition(float offsetX, float offsetY, float offsetZ)
        {
            if (spawnedPerson == null)
            {
                Debug.LogWarning("[OverlayManager] No spawned person to move.");
                return;
            }

            Transform cam = Camera.main.transform;

            // Base forward position (1m ahead)
            Vector3 basePos = cam.position + cam.forward * 1.0f;

            // Apply offsets (local camera space)
            Vector3 right = cam.right;
            Vector3 up = cam.up;
            Vector3 forward = cam.forward;

            Vector3 newPos =
                basePos +
                right * offsetX +
                up * offsetY +
                forward * offsetZ;

            spawnedPerson.transform.position = newPos;

            // Keep person facing camera (Y axis only)
            Vector3 look = cam.position;
            look.y = spawnedPerson.transform.position.y;
            spawnedPerson.transform.LookAt(look);
        }
    }
}
