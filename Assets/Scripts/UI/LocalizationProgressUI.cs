using UnityEngine;
using TMPro;
using ARJourneyIntoMovies.Server;
using UnityEngine.UI;

/// <summary>
/// UI controller that displays localization progress and results
/// during server-side image-based localization.
/// </summary>
public class LocalizationProgressUI : MonoBehaviour
{
    [Header("Server")]
    public ServerClient serverClient;

    [Header("Uploader")]
    public ARFrameUploader uploader;

    [Header("UI")]
    public GameObject progressPanel;
    public TMP_Text progressText;
    public Button ButtonStop;

    private void Start()
    {
        progressPanel.SetActive(false);

        if (uploader != null)
        {
            // Triggered when capture starts → show progress panel
            uploader.OnCaptureStarted += ShowProgressPanel;
        }

        ButtonStop.onClick.AddListener(OnClickClosePanel);
    }

    private void OnEnable()
    {
        if (serverClient != null)
        {
            serverClient.OnError += HandleServerError;
            serverClient.OnPoseReceived += HandleServerSuccess;
        }
    }

    private void OnDisable()
    {
        if (serverClient != null)
        {
            serverClient.OnError -= HandleServerError;
            serverClient.OnPoseReceived -= HandleServerSuccess;
        }

        if (uploader != null)
        {
            uploader.OnCaptureStarted -= ShowProgressPanel;
        }
    }

    // After user triggers capture
    private void ShowProgressPanel()
    {
        progressPanel.SetActive(true);
        progressText.text = "Processing...";
    }

    // Server response: failure
    private void HandleServerError(string msg)
    {
        progressPanel.SetActive(true);
        progressText.text = msg;    // reason
    }

    // Server response: success
    private void HandleServerSuccess(PoseData pose)
    {
        progressText.text = "Localization Success";

        // Close panel after a short delay
        Invoke(nameof(HidePanel), 1.0f);
    }

    private void HidePanel()
    {
        progressPanel.SetActive(false);
    }

    private void OnClickClosePanel()
    {
        HidePanel();
        uploader.enabled = false; // Stop further uploads
    }
}