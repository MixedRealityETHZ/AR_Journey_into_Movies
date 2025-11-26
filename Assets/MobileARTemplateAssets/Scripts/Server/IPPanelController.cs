using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ARJourneyIntoMovies.Server;
using UnityEngine.Networking;
using System.Collections;

public class IPPanelController : MonoBehaviour
{
    [Header("UI")]
    public GameObject IPPanel; 
    public TMP_InputField inputFieldIP;
    public Button confirmButton;
    public Button cancelButton;

    [Header("Targets to Update")]
    public ARFrameUploader frameUploader;
    private string remembered_ip;

    private void Start()
    {
        remembered_ip = "http://";
        IPPanel.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);

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

        if (frameUploader != null)
            frameUploader.serverUrl = ip + "/upload";

        IPPanel.SetActive(false);
        frameUploader.enabled = true;
    }

    private void OnCancel()
    {
        IPPanel.SetActive(false);
    }
}