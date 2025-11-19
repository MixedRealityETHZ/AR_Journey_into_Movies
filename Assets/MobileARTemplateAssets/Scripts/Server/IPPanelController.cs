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
    public Button testButton;

    [Header("Status Icon UI")]  
    public Image iconLoading;   // ⏳ 测试中（可加动画）
    public Image iconSuccess;   // ✅ ping 成功
    public Image iconFail;      // ❌ ping 失败

    [Header("Targets to Update")]
    public ARFrameUploaderV2 frameUploader;
    private string remembered_ip;

    private void Start()
    {
        remembered_ip = "http://";
        IPPanel.SetActive(false);
        iconFail.gameObject.SetActive(true);         // 初始显示 X
        iconSuccess.gameObject.SetActive(false);
        iconLoading.gameObject.SetActive(false);

        confirmButton.onClick.AddListener(OnConfirm);
        testButton.onClick.AddListener(OnTestClicked);

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
    }

    private void OnTestClicked()
    {
        iconFail.gameObject.SetActive(false);
        iconSuccess.gameObject.SetActive(false);
        iconLoading.gameObject.SetActive(true);  // 显示旋转 ⭕

        string ip = inputFieldIP.text.Trim();
        StartCoroutine(TestPing(ip));
    }

    private IEnumerator TestPing(string ip)
    {
        string baseUrl = ip;
        if (baseUrl.EndsWith("/upload"))
            baseUrl = baseUrl.Replace("/upload", "");

        string pingUrl = baseUrl + "/ping";

        using (UnityWebRequest www = UnityWebRequest.Get(pingUrl))
        {
            www.timeout = 5;

            yield return www.SendWebRequest();

            // 一定要先关闭 loading
            iconLoading.gameObject.SetActive(false);

            // ===== 情况 1：网络失败 / 超时 =====
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[IPPanel] Ping failed or timeout: " + www.error);

                iconFail.gameObject.SetActive(true);
                iconSuccess.gameObject.SetActive(false);
                yield break;
            }

            // ===== 情况 2：HTTP 成功 =====
            if (www.responseCode == 200)
            {
                iconSuccess.gameObject.SetActive(true);
                iconFail.gameObject.SetActive(false);
            }
            else
            {
                // HTTP 非 200 = 失败
                Debug.LogWarning("[IPPanel] Ping response non-200: " + www.responseCode);

                iconFail.gameObject.SetActive(true);
                iconSuccess.gameObject.SetActive(false);
            }
        }
    }
}