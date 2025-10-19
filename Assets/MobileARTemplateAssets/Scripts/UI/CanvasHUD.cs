using UnityEngine;
using TMPro;

namespace ARJourneyIntoMovies.UI
{
    /// <summary>
    /// Manages HUD display (hints, distance info, status messages)
    /// TODO: Step 6 - Connect to TextMeshPro UI elements
    /// </summary>
    public class CanvasHUD : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("TextMeshProUGUI for displaying hints")]
        public TextMeshProUGUI hintText;

        [Tooltip("TextMeshProUGUI for displaying distance to target")]
        public TextMeshProUGUI distanceText;

        [Tooltip("TextMeshProUGUI for displaying status messages")]
        public TextMeshProUGUI statusText;

        private void Awake()
        {
            Debug.Log("[CanvasHUD] Initialized (UI updates in Step 6)");
            SetHint("Ready to localize");
        }

        /// <summary>
        /// Update hint text
        /// </summary>
        public void SetHint(string message)
        {
            Debug.Log($"[CanvasHUD] Hint: {message}");

            if (hintText != null)
            {
                hintText.text = message;
            }
        }

        /// <summary>
        /// Update distance display
        /// </summary>
        public void SetDistance(float distance)
        {
            string distanceStr = distance < 100f ? $"{distance:F1}m" : "---";

            if (distanceText != null)
            {
                distanceText.text = $"Distance: {distanceStr}";
            }
        }

        /// <summary>
        /// Update status text (e.g., "Localizing...", "Localized!", "Error")
        /// </summary>
        public void SetStatus(string status)
        {
            Debug.Log($"[CanvasHUD] Status: {status}");

            if (statusText != null)
            {
                statusText.text = status;
            }
        }

        /// <summary>
        /// Clear all text displays
        /// </summary>
        public void ClearAll()
        {
            SetHint("");
            SetDistance(0f);
            SetStatus("");
        }
    }
}
