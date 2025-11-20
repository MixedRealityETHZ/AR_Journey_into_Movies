using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MapViewController : MonoBehaviour
{
    [Header("UI Elements")]
    public RectTransform mapPanel;      // 上方滑动的 panel（显示 Google Maps）
    public RectTransform hudPanel;      // 引导 HUD 面板（箭头等）
    public Button mapButton;            // 左下角的主按钮

    public Sprite iconMap;              // 地图图标
    public Sprite iconArrow;            // 箭头图标
    public Image ButtonIcon;
    public WebViewObject webView;       // WebView

    [Header("Slide Settings")]
    public float slideDuration = 0.35f;
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector2 mapHiddenPos;
    private Vector2 mapShownPos;

    private Vector2 hudShownPos;
    private Vector2 hudHiddenPos;

    // 状态枚举：地图模式 <-> 箭头模式
    private enum Mode { Map, Arrow }
    private Mode currentMode = Mode.Map;

    // 目标：46°N, 8°E
    private const double targetLat = 47.378352;
    private const double targetLon = 8.548224;
    private bool webViewLoaded = false;

    void Start()
    {
        // 记录 mapPanel 初始位置（滑下来的位置）
        mapShownPos = mapPanel.anchoredPosition;
        mapHiddenPos = mapShownPos + new Vector2(0, mapPanel.rect.height);
        mapPanel.anchoredPosition = mapHiddenPos;

        // HUD：初始显示
        hudShownPos = hudPanel.anchoredPosition;
        hudHiddenPos = hudShownPos + new Vector2(0, hudPanel.rect.height);
        hudPanel.anchoredPosition = hudHiddenPos;

        mapButton.onClick.AddListener(OnModeToggle);
        ButtonIcon.sprite = iconMap; // 初始是地图模式

        // 创建 WebView
        webView = gameObject.AddComponent<WebViewObject>();
        webView.Init(enableWKWebView: true);
        webView.SetVisibility(false);
        UpdateWebViewRect();
    }

    // ========================
    //  切换：地图 ↔ 箭头
    // ========================
    void OnModeToggle()
    {
        if (currentMode == Mode.Map)
        {
            SwitchToMapMode();
        }
        else
        {
            SwitchToArrowMode();
        }
    }

    // ========================
    //   地图导航模式
    // ========================
    void SwitchToMapMode()
    {
        if (!webViewLoaded)
            StartCoroutine(InitializeAndLoadURL());
        
        webView.SetVisibility(true);

        currentMode = Mode.Arrow;

        // 切换按钮图标
        ButtonIcon.sprite = iconArrow;

        // 滑下地图，滑上 HUD
        StartCoroutine(SlideRect(mapPanel, mapPanel.anchoredPosition, mapShownPos));
        StartCoroutine(SlideRect(hudPanel, hudPanel.anchoredPosition, hudHiddenPos));        
    }

    // ========================
    //   箭头引导模式
    // ========================
    void SwitchToArrowMode()
    {
        currentMode = Mode.Map;

        // 切回地图 icon
        ButtonIcon.sprite = iconMap;

        // 隐藏地图，上滑，HUD 下滑
        StartCoroutine(SlideRect(mapPanel, mapPanel.anchoredPosition, mapHiddenPos));
        StartCoroutine(SlideRect(hudPanel, hudPanel.anchoredPosition, hudShownPos));

        // 隐藏 WebView
        webView.SetVisibility(false);
    }

    // ========================
    //   通用滑动动画
    // ========================
    IEnumerator SlideRect(RectTransform rt, Vector2 start, Vector2 end)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            float v = slideCurve.Evaluate(t);
            rt.anchoredPosition = Vector2.Lerp(start, end, v);

            if (rt == mapPanel)
                UpdateWebViewRect();

            yield return null;
        }
        rt.anchoredPosition = end;
    }

    // 更新 WebView 区域
    void UpdateWebViewRect()
    {
        if (webView == null) return;

        Rect r = GetPixelRect(mapPanel);
        webView.SetMargins(
            (int)r.xMin,
            Screen.height - (int)r.yMax,
            Screen.width - (int)r.xMax,
            (int)r.yMin
        );
    }

    Rect GetPixelRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return new Rect(corners[0].x, corners[0].y,
                        corners[2].x - corners[0].x,
                        corners[2].y - corners[0].y);
    }

    private IEnumerator InitializeAndLoadURL()
    {
        if (!Input.location.isEnabledByUser)
            yield break;

        Input.location.Start();
        int wait = 20;

        while (Input.location.status == LocationServiceStatus.Initializing && wait > 0)
        {
            yield return new WaitForSeconds(1);
            wait--;
        }

        if (Input.location.status != LocationServiceStatus.Running)
            yield break;

        double userLat = Input.location.lastData.latitude;
        double userLon = Input.location.lastData.longitude;

        string url =
            $"https://www.google.com/maps/dir/?api=1" +
            $"&origin={userLat},{userLon}" +
            $"&destination={targetLat},{targetLon}" +
            $"&travelmode=walking";

        
        webView.LoadURL(url);
        webViewLoaded = true;
    }
}