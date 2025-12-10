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
    public Button cancelButton;
    public LocalizationProgressUI localizationProgressUI;

    [Header("Targets to Update")]
    public ARFrameUploader frameUploader;
    public MovieSceneFrameController frameSelect;
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
        // var info = frameSelect.GetSelectedFrameInfo();
        // Texture2D frameTexture = info.frameTexture;
        // if (frameSelect != null && frameTexture == null)
        // {
        //     Debug.LogWarning("No movie frame selected yet.");
        //     return;
        // }

        // if(localizationProgressUI.progressPanel.activeSelf)
        // {
        //     return;
        // }

        IPPanel.SetActive(true);
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