using UnityEngine;
using UnityEngine.UI;
using ARJourneyIntoMovies.AR;
using TMPro;

public class FrustumDebugPanel : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField txInput, tyInput, tzInput;
    public TMP_InputField rxInput, ryInput, rzInput, rwInput;
    public Button applyButton;

    [Header("References")]
    public PoseFusion poseFusion;  // 用来设置新的 Pose
    public TargetFrustumVisualizer frustumVisualizer;

    private void Start()
    {
        applyButton.onClick.AddListener(OnApplyPressed);
    }

    private void OnApplyPressed()
    {
        // 1️⃣ 解析用户输入
        float tx = float.Parse(txInput.text);
        float ty = float.Parse(tyInput.text);
        float tz = float.Parse(tzInput.text);

        float rx = float.Parse(rxInput.text);
        float ry = float.Parse(ryInput.text);
        float rz = float.Parse(rzInput.text);
        float rw = float.Parse(rwInput.text);

        Debug.Log($"[Apply] Input translation = ({tx}, {ty}, {tz})");
        Debug.Log($"[Apply] Input rotation = ({rx}, {ry}, {rz}, {rw})");
        Debug.Log("[Apply] Matrix4x4:");
        // 2️⃣ 生成 Matrix4x4
        Quaternion rot = new Quaternion(rx, ry, rz, rw);
        Vector3 trans = new Vector3(tx, ty, tz);

        Matrix4x4 newPose = Matrix4x4.TRS(trans, rot, Vector3.one);

        // 3️⃣ 应用到 PoseFusion / FrustumVisualizer
        if (poseFusion != null)
        {
            poseFusion.SetManualPose(newPose);
        }

        if (frustumVisualizer != null)
        {
            frustumVisualizer.UpdateFrustumPose(newPose);
        }

        Debug.Log($"[FrustumDebugPanel] Applied pose → T=({tx:F2},{ty:F2},{tz:F2}) R=({rx:F2},{ry:F2},{rz:F2},{rw:F2})");
    }
}