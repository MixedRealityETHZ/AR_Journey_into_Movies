using UnityEngine;
using TMPro;
using ARJourneyIntoMovies.Server;

public class LocalizationProgressUI : MonoBehaviour
{
    [Header("Server")]
    public ServerClient serverClient;

    [Header("Uploader")]
    public ARFrameUploader uploader;

    [Header("UI")]
    public GameObject progressPanel;
    public TMP_Text progressText;

    private void Start()
    {
        progressPanel.SetActive(false);

        if (uploader != null)
        {
            // 👇 拍照按钮触发 → 显示 panel
            uploader.OnCaptureStarted += ShowProgressPanel;
        }
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

    // ===================================================
    // 📸 用户点击“拍照”后
    // ===================================================
    private void ShowProgressPanel()
    {
        progressPanel.SetActive(true);
        progressText.text = "Processing...";
    }

    // ===================================================
    // ❌ success = false
    // ===================================================
    private void HandleServerError(string msg)
    {
        progressPanel.SetActive(true);
        progressText.text = msg;    // reason
    }

    // ===================================================
    // ✅ success = true
    // ===================================================
    private void HandleServerSuccess(PoseData pose)
    {
        progressText.text = "Localization Success";

        // 0.5s 后关闭
        Invoke(nameof(HidePanel), 1.0f);
    }

    private void HidePanel()
    {
        progressPanel.SetActive(false);
    }
}