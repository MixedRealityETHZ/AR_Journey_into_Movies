using UnityEngine;
using TMPro;
using ARJourneyIntoMovies.Server;

public class LocalizationProgressUI : MonoBehaviour
{
    [Header("Server")]
    public ServerClient serverClient;

    [Header("UI References")]
    public TMP_Text progressText;

    private void OnEnable()
    {
        if (serverClient != null)
        {
            serverClient.OnError += HandleServerError;
            serverClient.OnPoseReceived += HandleSuccess;
        }
    }

    private void OnDisable()
    {
        if (serverClient != null)
        {
            serverClient.OnError -= HandleServerError;
            serverClient.OnPoseReceived -= HandleSuccess;
        }
    }

    // 🟥 服务器返回 success = false
    private void HandleServerError(string msg)
    {
        if (progressText != null)
        {
            progressText.text =
                $"📸 继续拍摄...\n\n服务器提示：\n{msg}";
        }
    }

    // 🟩 成功后清空提示
    private void HandleSuccess(PoseData pose)
    {
        if (progressText != null)
        {
            progressText.text = "定位成功！🎉";
        }
    }
}