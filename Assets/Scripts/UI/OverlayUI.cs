using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ARJourneyIntoMovies.UI
{
    /// <summary>
    /// Controls overlay UI elements (slider, return button, labels)
    /// </summary>
    public class OverlayUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Alpha slider")]
        public Slider alphaSlider;

        [Tooltip("Alpha value text (displays current alpha)")]
        public TextMeshProUGUI alphaValueText;

        [Tooltip("Return to guidance button")]
        public Button returnButton;

        [Tooltip("Instruction text")]
        public TextMeshProUGUI instructionText;

        [Header("References")]
        [Tooltip("OverlayManager to control")]
        public AR.OverlayManager overlayManager;

        private void Awake()
        {
            // Setup slider listener
            if (alphaSlider != null)
            {
                alphaSlider.onValueChanged.AddListener(OnAlphaChanged);
            }

            // Setup return button
            if (returnButton != null)
            {
                returnButton.onClick.AddListener(OnReturnClicked);
            }

            // Set instruction text
            if (instructionText != null)
            {
                instructionText.text = "Adjust transparency to align with the scene";
            }
        }

        private void OnDestroy()
        {
            // Cleanup listeners
            if (alphaSlider != null)
            {
                alphaSlider.onValueChanged.RemoveListener(OnAlphaChanged);
            }

            if (returnButton != null)
            {
                returnButton.onClick.RemoveListener(OnReturnClicked);
            }
        }

        private void Update()
        {
            // Update alpha value text
            if (alphaValueText != null && overlayManager != null)
            {
                float alpha = overlayManager.GetAlpha();
                alphaValueText.text = $"{alpha:P0}"; // Display as percentage (e.g., "50%")
            }
        }

        /// <summary>
        /// Handle alpha slider value change
        /// </summary>
        private void OnAlphaChanged(float value)
        {
            if (overlayManager != null)
            {
                overlayManager.SetAlpha(value);
            }
        }

        /// <summary>
        /// Handle return button click
        /// </summary>
        private void OnReturnClicked()
        {
            if (overlayManager != null)
            {
                overlayManager.HideOverlay();
            }
        }
    }
}
