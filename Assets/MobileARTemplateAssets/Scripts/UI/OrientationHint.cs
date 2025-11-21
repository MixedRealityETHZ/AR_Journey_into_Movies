using UnityEngine;
using UnityEngine.UI;

public class OrientationHint : MonoBehaviour
{
    public GameObject rotateHintPanel;   // UI 提示面板，例如一个横屏图标
    public float triggerDistance = 2.0f; // 靠近四棱锥的距离
    public Transform pyramid;            // 四棱锥的中心位置
    public Transform cameraTransform;     // 手机相机

    private bool hintShown = false;

    void Update()
    {
        float d = Vector3.Distance(cameraTransform.position, pyramid.position);

        // 靠近四棱锥才需要检测
        if (d < triggerDistance)
        {
            if (IsPortrait())
            {
                ShowHint();
            }
            else
            {
                HideHint();
            }
        }
        else
        {
            HideHint();
        }
    }

    bool IsPortrait()
    {
        return Input.deviceOrientation == DeviceOrientation.Portrait ||
               Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown;
    }

    void ShowHint()
    {
        if (!hintShown)
        {
            rotateHintPanel.SetActive(true);
            hintShown = true;
        }
    }

    void HideHint()
    {
        if (hintShown)
        {
            rotateHintPanel.SetActive(false);
            hintShown = false;
        }
    }
}