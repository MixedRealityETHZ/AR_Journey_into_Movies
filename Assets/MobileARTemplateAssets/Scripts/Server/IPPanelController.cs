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

    private string remembered_ip;

    private void Start()
    {
        remembered_ip = "http://";
        IPPanel.SetActive(false);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);

        Debug.Log("[IPPanel] Auto ip = " + remembered_ip);
    }
    
    public void OpenIPPanel()
    {
        IPPanel.SetActive(true);

        // 默认显示自动 IP（可覆盖）
        inputFieldIP.text = remembered_ip;
    }

    private void OnConfirm()
    {
        string ip = inputFieldIP.text.Trim();
        remembered_ip = ip;

        if (serverClient != null)
            serverClient.serverUrl = ip + "/localize";

        if (frameUploader != null)
            frameUploader.serverUrl = ip + "/upload";

        IPPanel.SetActive(false);
    }
}