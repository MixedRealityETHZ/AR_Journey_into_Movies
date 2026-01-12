using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Slides a map panel and HUD panel to toggle between
/// an embedded Google Maps route view and an in-app arrow guidance mode.
/// </summary>
public class MapViewController : MonoBehaviour
{
    [Header("UI Elements")]
    public RectTransform mapPanel;      
    public RectTransform hudPanel;      
    public Button mapButton;          
    public MovieSceneFrameController frameSelect; 

    public Sprite iconMap;            
    public Sprite iconArrow;          
    public Image ButtonIcon;
    public WebViewObject webView;      

    [Header("Slide Settings")]
    public float slideDuration = 0.35f;
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector2 mapHiddenPos;
    private Vector2 mapShownPos;

    private Vector2 hudShownPos;
    private Vector2 hudHiddenPos;

    // UI mode: Map view ↔ Arrow guidance
    private enum Mode { Map, Arrow }
    private Mode currentMode = Mode.Map;

    // Destination coordinates (set per selected scene)
    private double targetLat = 47.378352;
    private double targetLon = 8.548224;
    private bool webViewLoaded = false;

    void Start()
    {
        // Cache shown/hidden positions (map starts hidden above the screen)
        mapShownPos = mapPanel.anchoredPosition;
        mapHiddenPos = mapShownPos + new Vector2(0, mapPanel.rect.height);
        mapPanel.anchoredPosition = mapHiddenPos;

        // Cache HUD positions (HUD starts hidden above the screen)
        hudShownPos = hudPanel.anchoredPosition;
        hudHiddenPos = hudShownPos + new Vector2(0, hudPanel.rect.height);
        hudPanel.anchoredPosition = hudHiddenPos;

        mapButton.onClick.AddListener(OnModeToggle);
        ButtonIcon.sprite = iconMap; 

        // Create and initialize WebView (hidden by default)
        webView = gameObject.AddComponent<WebViewObject>();
        webView.Init(enableWKWebView: true);
        webView.SetVisibility(false);
        UpdateWebViewRect();
    }

    //  Toggle: Map ↔ Arrow
    void OnModeToggle()
    {
        // Require a selected frame before showing navigation
        var info = frameSelect.GetSelectedFrameInfo();
        Texture2D frameTexture = info.frameTexture;
        if (frameSelect != null && frameTexture == null)
        {
            Debug.LogWarning("No movie frame selected yet.");
            return;
        }

        if (currentMode == Mode.Map)
        {
            SwitchToMapMode();
        }
        else
        {
            SwitchToArrowMode();
        }
    }

    //   Google Maps route view
    void SwitchToMapMode()
    {
        if (!webViewLoaded)
            StartCoroutine(InitializeAndLoadURL());
        
        webView.SetVisibility(true);

        currentMode = Mode.Arrow;

        // Swap button icon
        ButtonIcon.sprite = iconArrow;

        // Slide map in and HUD out
        StartCoroutine(SlideRect(mapPanel, mapPanel.anchoredPosition, mapShownPos));
        StartCoroutine(SlideRect(hudPanel, hudPanel.anchoredPosition, hudHiddenPos));        
    }

    //   Arrow guidance mode
    void SwitchToArrowMode()
    {
        currentMode = Mode.Map;

        // Switch back to map icon
        ButtonIcon.sprite = iconMap;

        // Slide map out and HUD in
        StartCoroutine(SlideRect(mapPanel, mapPanel.anchoredPosition, mapHiddenPos));
        StartCoroutine(SlideRect(hudPanel, hudPanel.anchoredPosition, hudShownPos));

        // Hide WebView
        webView.SetVisibility(false);
    }

    IEnumerator SlideRect(RectTransform rt, Vector2 start, Vector2 end)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            float v = slideCurve.Evaluate(t);
            rt.anchoredPosition = Vector2.Lerp(start, end, v);

            // Keep WebView aligned with the moving map panel
            if (rt == mapPanel)
                UpdateWebViewRect();

            yield return null;
        }
        rt.anchoredPosition = end;
    }

    void UpdateWebViewRect()
    {
        if (webView == null) return;

        Rect r = GetPixelRect(mapPanel);
        // Convert RectTransform world corners to screen-space margins
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

        // Use device GPS as the origin for directions
        Input.location.Start();
        int wait = 20;

        while (Input.location.status == LocationServiceStatus.Initializing && wait > 0)
        {
            yield return new WaitForSeconds(1);
            wait--;
        }

        if (Input.location.status != LocationServiceStatus.Running)
            yield break;

        var info = frameSelect.GetSelectedFrameInfo();
        string movie = info.movie;

        // Choose destination based on selected scene/movie identifier
        if(movie =="ETHz-CAB")
        {
            targetLat = 47.378352;
            targetLon = 8.548224;
        }
        else if(movie == "ETHz-HG")
        {
            targetLat = 47.376246;
            targetLon = 8.547045;
        }
        double userLat = Input.location.lastData.latitude;
        double userLon = Input.location.lastData.longitude;

        // Build a Google Maps directions URL (walking mode)
        string url =
            $"https://www.google.com/maps/dir/?api=1" +
            $"&origin={userLat},{userLon}" +
            $"&destination={targetLat},{targetLon}" +
            $"&travelmode=walking";

        
        webView.LoadURL(url);
        webViewLoaded = true;
    }
}