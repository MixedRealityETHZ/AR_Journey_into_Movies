using UnityEngine;
using System.Collections;

public class MapViewController : MonoBehaviour
{
    private WebViewObject webView;

    // 目标：46°N, 8°E
    private const double targetLat = 47.378352; 
    private const double targetLon = 8.548224;

    public void ShowMap()
    {
        StartCoroutine(InitializeAndShow());
    }

    private IEnumerator InitializeAndShow()
    {
        // 1) 请求 GPS 权限
        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("[MapView] GPS not enabled by user.");
            yield break;
        }

        Input.location.Start();

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            Debug.Log("[MapView] GPS failed.");
            yield break;
        }

        double userLat = Input.location.lastData.latitude;
        double userLon = Input.location.lastData.longitude;

        Debug.Log($"[MapView] User GPS: {userLat}, {userLon}");

        // 2) 构造 Google Maps 路线 URL
        string url =
            $"https://www.google.com/maps/dir/?api=1" +
            $"&origin={userLat},{userLon}" +
            $"&destination={targetLat},{targetLon}" +
            $"&travelmode=walking";

        Debug.Log("[MapView] Load URL: " + url);

        // 3) 初始化 WebView
        webView = gameObject.AddComponent<WebViewObject>();
        webView.Init(enableWKWebView: true);

        // 设置 WebView 区域 = Panel 全屏
        webView.SetMargins(0, 0, 0, 0);

        webView.SetVisibility(true);
        webView.LoadURL(url);
    }

    public void HideMap()
    {
        if (webView != null)
            webView.SetVisibility(false);
        gameObject.SetActive(false);
    }
}