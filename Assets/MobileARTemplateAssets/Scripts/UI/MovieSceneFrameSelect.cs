using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;


public class MovieSceneFrameController : MonoBehaviour
{
    // =========================================================
    //  数据结构
    // =========================================================
    [System.Serializable]
    public class MovieData
    {
        public string name;
        public Sprite filmecover;
        public List<SceneData> scenes = new List<SceneData>();
    }

    [System.Serializable]
    public class SceneData
    {
        public string name;
        public Sprite scenecover;
        public List<FrameData> frames = new List<FrameData>();
    }

    [System.Serializable]
    public class FrameData
    {
        public string id;
        public Sprite framesprite;
    }
    [Header("Navigation Buttons")]
    public Button backFromSceneButton;
    public Button backFromFrameButton;
    public Button confirmFrameButton;

    // =========================================================
    //  Inspector 拖拽项
    // =========================================================
    [Header("Panels")]
    public GameObject moviePanel;
    public GameObject scenePanel;
    public GameObject framePanel;
    public GameObject arPanel;

    [Header("Contents")]
    public Transform movieContent;
    public Transform sceneContent;
    public Transform frameContent;

    [Header("Prefabs")]
    public GameObject movieItemPrefab;
    public GameObject sceneItemPrefab;
    public GameObject frameItemPrefab;

    [Header("Next Buttons")]
    public Button nextFromMovieButton;
    public Button nextFromSceneButton;

    // =========================================================
    // 内部数据
    // =========================================================
    private List<MovieData> movies = new List<MovieData>();
    private MovieData currentMovie;
    private SceneData currentScene;
    private FrameData currentFrame;

    private GameObject currentMovieItem;
    private GameObject currentSceneItem;
    private GameObject currentFrameItem;


    // =========================================================
    //  启动逻辑
    // =========================================================
    void Start()
    {
        LoadAllMoviesFromResources();
        ShowMoviePanel();

        backFromSceneButton.onClick.AddListener(GoToMoviePanel);
        backFromFrameButton.onClick.AddListener(GoToScenePanel);
        confirmFrameButton.onClick.AddListener(OnConfirmFrameSelected);
        nextFromMovieButton.onClick.AddListener(GoToScenePanel);
        nextFromSceneButton.onClick.AddListener(GoToFramePanel);
    }


    // =========================================================
    //  自动加载 Resources/Movies 下的所有图片
    // =========================================================
    void LoadAllMoviesFromResources()
    {
        movies.Clear();

        string basePath = Path.Combine(Application.dataPath, "Resources/Movies");
        DirectoryInfo movieDir = new DirectoryInfo(basePath);

        foreach (DirectoryInfo movieFolder in movieDir.GetDirectories())
        {
            MovieData movie = new MovieData();
            movie.name = movieFolder.Name;

            // 电影封面
            movie.filmecover = Resources.Load<Sprite>($"Movies/{movie.name}/cover");

            // 场景目录
            string scenePath = Path.Combine(movieFolder.FullName, "Scenes");
            DirectoryInfo sceneDir = new DirectoryInfo(scenePath);

            foreach (DirectoryInfo sceneFolder in sceneDir.GetDirectories())
            {
                SceneData scene = new SceneData();
                scene.name = sceneFolder.Name;

                // Scene 的封面（thumbnail.png）
                scene.scenecover = Resources.Load<Sprite>(
                    $"Movies/{movie.name}/Scenes/{scene.name}/thumbnail"
                );

                // 加载所有 frame 图
                FileInfo[] frameFiles = sceneFolder.GetFiles("*.png");

                foreach (FileInfo frameFile in frameFiles)
                {
                    if (frameFile.Name == "thumbnail.png") continue;

                    FrameData f = new FrameData();
                    f.id = Path.GetFileNameWithoutExtension(frameFile.Name);

                    f.framesprite = Resources.Load<Sprite>(
                        $"Movies/{movie.name}/Scenes/{scene.name}/{f.id}"
                    );

                    scene.frames.Add(f);
                }

                movie.scenes.Add(scene);
            }

            movies.Add(movie);
        }
    }

    // =========================================================
    // UI 切换：Movie → Scene → Frame → AR
    // =========================================================
    public void ShowMoviePanel()
    {
        moviePanel.SetActive(true);
        scenePanel.SetActive(false);
        framePanel.SetActive(false);
        arPanel.SetActive(false);

        PopulateMovies();
    }

    // ------------------- MOVIE -------------------
    void PopulateMovies()
    {
        ClearContent(movieContent);

        foreach (var movie in movies)
        {
            GameObject item = Instantiate(movieItemPrefab, movieContent);

            item.transform.Find("CoverImage").GetComponent<Image>().sprite = movie.filmecover;
            item.transform.Find("MovieName").GetComponent<TMP_Text>().text = movie.name;

            Button btn = item.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                HighlightItem(item, ref currentMovieItem);
                currentMovie = movie;
            });
        }
    }

    // ------------------- SCENE -------------------
    void PopulateScenes(MovieData movie)
    {
        ClearContent(sceneContent);

        foreach (var scene in movie.scenes)
        {
            GameObject item = Instantiate(sceneItemPrefab, sceneContent);

            item.transform.Find("SceneImage").GetComponent<Image>().sprite = scene.scenecover;
            item.transform.Find("SceneName").GetComponent<TMP_Text>().text = scene.name;

            Button btn = item.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                HighlightItem(item, ref currentSceneItem);
                currentScene = scene;
            });
        }
    }

    // ------------------- FRAME -------------------
    void PopulateFrames(SceneData scene)
    {
        ClearContent(frameContent);

        foreach (var frame in scene.frames)
        {
            GameObject item = Instantiate(frameItemPrefab, frameContent);

            item.transform.Find("FrameImage").GetComponent<Image>().sprite = frame.framesprite;
            item.transform.Find("FrameName").GetComponent<TMP_Text>().text = frame.id;

            Button btn = item.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                HighlightItem(item, ref currentFrameItem);
                currentFrame = frame;
            });
        }
    }



    void OnConfirmFrameSelected()
    {
        arPanel.SetActive(true);
        moviePanel.SetActive(false);
        scenePanel.SetActive(false);
        framePanel.SetActive(false);

        if (currentMovie == null || currentScene == null || currentFrame == null)
        {
            Debug.LogError("Selection incomplete!");
            return;
        }

        string movieName = currentMovie.name;
        string sceneName = currentScene.name;
        string frameId = currentFrame.id;

        Debug.Log($"CONFIRMED:\nMovie = {movieName}\nScene = {sceneName}\nFrame = {frameId}");

        // TODO: 在这里把三个值传给你的 AR 功能
    }


    // =========================================================
    // 工具函数
    // =========================================================
    void ClearContent(Transform content)
    {
        foreach (Transform c in content)
            Destroy(c.gameObject);
    }

    void HighlightItem(GameObject item, ref GameObject currentItem)
    {
        if (currentItem != null)
            currentItem.transform.Find("Highlight").gameObject.SetActive(false);
        item.transform.Find("Highlight").gameObject.SetActive(true);
        currentItem = item;
    }


    void GoToMoviePanel()
    {
        scenePanel.SetActive(false);
        framePanel.SetActive(false);
        moviePanel.SetActive(true);
        PopulateMovies();
    }

    void GoToScenePanel()
    {
        framePanel.SetActive(false);
        moviePanel.SetActive(false);
        scenePanel.SetActive(true);
        PopulateScenes(currentMovie);
    }

    void GoToFramePanel()
    {
        framePanel.SetActive(true);
        moviePanel.SetActive(false);
        scenePanel.SetActive(false);
        PopulateFrames(currentScene);
    }



}


