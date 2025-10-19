using UnityEngine;
using UnityEngine.UI;

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

        [Tooltip("RawImage for full-screen movie frame display")]
        public RawImage overlayImage;

        [Tooltip("Slider for alpha control")]
        public Slider alphaSlider;

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

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // Current overlay state
        private bool isOverlayActive = false;

        // Current alpha value
        private float currentAlpha = 0.5f;

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

            // Set alpha
            SetAlpha(alpha);

            // Update slider
            if (alphaSlider != null)
            {
                alphaSlider.value = alpha;
            }

            // Show canvas
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
            ShowOverlay(placeholder, defaultAlpha);

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
            HideOverlay();
        }
    }
}
