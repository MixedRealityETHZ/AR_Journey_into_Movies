using ARJourneyIntoMovies.AR;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SelectImageController : MonoBehaviour
{
    [Header("UI")]
    public GameObject ChooseImagePanel;
    public RectTransform MovieSelectPanel;  // 改为 RectTransform 才能动画
    public Button ButtonAlbum;
    public Button ChooseBuiltinImageButton;
    public Button ChooseAlbumImageButton;
    public OverlayManager OverlayManager;

    [Header("Slide Settings")]
    public float slideDuration = 0.35f;
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector2 panelHiddenPos;
    private Vector2 panelShownPos;

    private bool isPanelVisible = false;

    private void Start()
    {
        ChooseImagePanel.SetActive(false);

        ButtonAlbum.onClick.AddListener(OnOpenChooseImagePanel);
        ChooseBuiltinImageButton.onClick.AddListener(OnOpenChooseBuiltinImage);
        ChooseAlbumImageButton.onClick.AddListener(OnOpenChooseAlbumPanel);

        // 初始化位置
        panelShownPos = MovieSelectPanel.anchoredPosition;
        panelHiddenPos = panelShownPos + new Vector2(0, MovieSelectPanel.rect.height);

        // 初始放在屏幕外（上方）
        MovieSelectPanel.anchoredPosition = panelHiddenPos;
        MovieSelectPanel.gameObject.SetActive(false);
    }

    public void OnOpenChooseImagePanel()
    {
        ChooseImagePanel.SetActive(true);
    }

    private void OnOpenChooseBuiltinImage()
    {
        if (!isPanelVisible)
        {
            ChooseImagePanel.SetActive(false);
            StartCoroutine(SlideInMoviePanel());
        }
        else
            StartCoroutine(SlideOutMoviePanel());
    }

    private void OnOpenChooseAlbumPanel()
    {
        ChooseImagePanel.SetActive(false);
        OverlayManager.OnSelectPhoto();
    }

    // ============================
    //      动画：滑入
    // ============================
    private IEnumerator SlideInMoviePanel()
    {
        isPanelVisible = true;
        MovieSelectPanel.gameObject.SetActive(true);

        Vector2 start = panelHiddenPos;
        Vector2 end = panelShownPos;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            float v = slideCurve.Evaluate(t);

            MovieSelectPanel.anchoredPosition = Vector2.Lerp(start, end, v);
            yield return null;
        }

        MovieSelectPanel.anchoredPosition = end;
    }

    // ============================
    //      动画：滑出
    // ============================
    private IEnumerator SlideOutMoviePanel()
    {
        isPanelVisible = false;

        Vector2 start = panelShownPos;
        Vector2 end = panelHiddenPos;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            float v = slideCurve.Evaluate(t);

            MovieSelectPanel.anchoredPosition = Vector2.Lerp(start, end, v);
            yield return null;
        }

        MovieSelectPanel.anchoredPosition = end;
        MovieSelectPanel.gameObject.SetActive(false);
    }
}