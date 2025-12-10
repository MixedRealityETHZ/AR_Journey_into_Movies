using UnityEngine;
using UnityEngine.UI;
using ARJourneyIntoMovies.AR;   // OverlayManager 所在命名空间
using TMPro;

public class PersonPositionController : MonoBehaviour
{
    [Header("References")]
    public OverlayManager overlayManager;   // 拖入 OverlayManager
    public Slider sliderX;                  // 控制左右
    public Slider sliderY;                  // 控制上下
    public Slider sliderZ;                  // 控制前后

    [Header("UI Text Displays")]
    public TMP_Text textX;                      // 显示 X 值
    public TMP_Text textY;                      // 显示 Y 值
    public TMP_Text textZ;                      // 显示 Z 值

    [Header("Range Settings")]
    public float rangeX = 10.0f;
    public float rangeY = 10.0f;
    public float rangeZ = 10.0f;

    private void Start()
    {
        if (sliderX != null)
            sliderX.onValueChanged.AddListener(OnSliderChanged);
        if (sliderY != null)
            sliderY.onValueChanged.AddListener(OnSliderChanged);
        if (sliderZ != null)
            sliderZ.onValueChanged.AddListener(OnSliderChanged);

        // 初始化时更新一次 UI
        UpdatePositionAndText();
    }

    private void OnDestroy()
    {
        if (sliderX != null)
            sliderX.onValueChanged.RemoveListener(OnSliderChanged);
        if (sliderY != null)
            sliderY.onValueChanged.RemoveListener(OnSliderChanged);
        if (sliderZ != null)
            sliderZ.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnSliderChanged(float _)
    {
        UpdatePositionAndText();
    }

    private void UpdatePositionAndText()
    {
        if (overlayManager == null)
            return;

        float x = (sliderX != null ? sliderX.value : 0f) * -rangeX;
        float y = (sliderY != null ? sliderY.value : 0f) * -rangeY;
        float z = (sliderZ != null ? sliderZ.value : 0f) * rangeZ;

        // 更新人物位置
        overlayManager.UpdatePersonPosition(x, y, z);

        // 显示数值（保留两位小数）
        if (textX != null)
            textX.text = $"X: {x:F2} m";
        if (textY != null)
            textY.text = $"Y: {y:F2} m";
        if (textZ != null)
            textZ.text = $"Z: {z:F2} m";
    }
}