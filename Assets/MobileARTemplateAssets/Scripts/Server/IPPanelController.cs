using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ARJourneyIntoMovies.Server;

public class IPPanelController : MonoBehaviour
{
    [Header("UI")]
    public GameObject IPPanel; 
    public TMP_InputField inputFieldIP;
    public Button confirmButton;

    [Header("Targets to Update")]
    public ARFrameUploaderV2 frameUploader;
    public ServerClient serverClient;

    private string autoDetectedIP;

    private void Start()
    {
        IPPanel.SetActive(false);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);

        // 自动检测本机 IP
        autoDetectedIP = NetworkUtils.GetLocalIPv4();
        Debug.Log("[IPPanel] Auto ip = " + autoDetectedIP);
    }
    
    public void OpenIPPanel()
    {
        IPPanel.SetActive(true);

        // 默认显示自动 IP（可覆盖）
        inputFieldIP.text = "http://" + autoDetectedIP + ":5000";
    }

    private void OnConfirm()
    {
        string ip = inputFieldIP.text.Trim();

        if (serverClient != null)
            serverClient.serverUrl = ip + "/localize";

        if (frameUploader != null)
            frameUploader.serverUrl = ip + "/upload";

        IPPanel.SetActive(false);
    }
}