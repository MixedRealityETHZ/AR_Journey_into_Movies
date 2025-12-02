using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using ARJourneyIntoMovies.AR;
using ARJourneyIntoMovies.Server;


public class MovieSceneFrameController : MonoBehaviour
{
    // =========================================================
    //  ���ݽṹ
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
    //  Inspector ��ק��
    // =========================================================
    [Header("Panels")]
    public GameObject moviePanel;
    public GameObject scenePanel;
    public GameObject framePanel;
    public GameObject ChooseFramePanel;
    public OverlayManager overlayManager;
    // public GameObject arPanel;

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
    public Button ButtonAlbum;
    public Button ChooseBuiltinImageButton;
    public Button ChooseAlbumImageButton;

    // =========================================================
    // �ڲ�����
    // =========================================================
    private List<MovieData> movies = new List<MovieData>();
    private MovieData currentMovie;
    private SceneData currentScene;
    private FrameData currentFrame;

    private GameObject currentMovieItem;
    private GameObject currentSceneItem;
    private GameObject currentFrameItem;
    private string movieName;
    private string sceneName;
    private string frameId;
    private Texture2D MovieFrameTexture;
    private bool isFromAlbum = false;

    public (string movie, string scene, string frame, Texture2D frameTexture, bool fromAlbum) GetSelectedFrameInfo()
    {
        return (movieName, sceneName, frameId, MovieFrameTexture, isFromAlbum);
    }


    // =========================================================
    //  �����߼�
    // =========================================================
    void Start()
    {
        LoadAllMoviesFromResources();
        ShowMoviePanel();

        backFromSceneButton.onClick.AddListener(GoToMoviePanel);
        backFromFrameButton.onClick.AddListener(GoToFrameSelectionPanel);
        confirmFrameButton.onClick.AddListener(OnConfirmFrameSelected);
        nextFromMovieButton.onClick.AddListener(GoToScenePanel);
        nextFromSceneButton.onClick.AddListener(GoToFrameSelectionPanel);
        ButtonAlbum.onClick.AddListener(GoToMoviePanel);
        ChooseBuiltinImageButton.onClick.AddListener(GoToFramePanel);
        ChooseAlbumImageButton.onClick.AddListener(OnOpenChooseAlbumPanel);
    }

    void GetFrameInfo()
    {
        
    }

    // =========================================================
    //  �Զ����� Resources/Movies �µ�����ͼƬ
    // =========================================================
    void LoadAllMoviesFromResources()
    {
        movies.Clear();

        TextAsset movieListFile = Resources.Load<TextAsset>("movies");
        string[] lines = movieListFile.text.Split('\n');
        List<string> movieNames = new List<string>();
        List<string> sceneNames = new List<string>();

        foreach(string rawLine in lines)
        {
            string line = rawLine.Trim();
            string[] parts = line.Split('/');
            
            string movieName = parts[0].Trim();   // '/' 前

            if(!movieNames.Contains(movieName))
            {
                movieNames.Add(movieName);
            }
            sceneNames.Add(line);  
        }

        foreach (string movieFolder in movieNames)
        {
            MovieData movie = new MovieData();
            movie.name = movieFolder;
            movie.filmecover = Resources.Load<Sprite>($"Movies/{movie.name}/cover");

            foreach (string sceneFolder in sceneNames)
            {
                if (!sceneFolder.StartsWith(movieFolder + "/")) continue;
                string[] parts = sceneFolder.Split('/');
                string sceneName = parts[1].Trim();

                SceneData scene = new SceneData();
                scene.name = sceneName;

                scene.scenecover = Resources.Load<Sprite>(
                    $"Movies/{movie.name}/Scenes/{scene.name}/thumbnail"
                );

                Sprite[] frames = Resources.LoadAll<Sprite>($"Movies/{movie.name}/Scenes/{scene.name}");
                foreach (var f in frames)
                {
                    if (f.name.Contains("thumbnail")) continue;

                    FrameData fd = new FrameData();
                    fd.id = f.name;
                    fd.framesprite = f;
                    scene.frames.Add(fd);
                }

                movie.scenes.Add(scene);
            }
            movies.Add(movie);
        }
    }

    // =========================================================
    // UI �л���Movie �� Scene �� Frame �� AR
    // =========================================================
    public void ShowMoviePanel()
    {
        moviePanel.SetActive(false);
        scenePanel.SetActive(false);
        framePanel.SetActive(false);
        ChooseFramePanel.SetActive(false);

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
        // arPanel.SetActive(true);
        moviePanel.SetActive(false);
        scenePanel.SetActive(false);
        framePanel.SetActive(false);

        if (currentMovie == null || currentScene == null || currentFrame == null)
        {
            Debug.LogError("Selection incomplete!");
            return;
        }

        movieName = currentMovie.name;
        sceneName = currentScene.name;
        frameId = currentFrame.id;
        MovieFrameTexture = currentFrame.framesprite.texture;
        isFromAlbum = false;
        overlayManager.setMovieFrame();

        Debug.Log($"CONFIRMED:\nMovie = {movieName}\nScene = {sceneName}\nFrame = {frameId}");
    }


    // =========================================================
    // ���ߺ���
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
        if(moviePanel.activeSelf)
        {
            moviePanel.SetActive(false);
        }
        else
        {
            scenePanel.SetActive(false);
            framePanel.SetActive(false);
            moviePanel.SetActive(true);
            ChooseFramePanel.SetActive(false);
            PopulateMovies();
        }
    }

    void GoToScenePanel()
    {
        framePanel.SetActive(false);
        moviePanel.SetActive(false);
        scenePanel.SetActive(true);
        ChooseFramePanel.SetActive(false);
        PopulateScenes(currentMovie);
    }

    void GoToFramePanel()
    {
        framePanel.SetActive(true);
        moviePanel.SetActive(false);
        scenePanel.SetActive(false);
        ChooseFramePanel.SetActive(false);
        PopulateFrames(currentScene);
    }

    void GoToFrameSelectionPanel()
    {
        ChooseFramePanel.SetActive(true);
        framePanel.SetActive(false);
        moviePanel.SetActive(false);
        scenePanel.SetActive(false);
    }

    private void OnOpenChooseAlbumPanel()
    {
        ChooseFramePanel.SetActive(false);
        PickImageFromGallery();
    }

    private void PickImageFromGallery()
    {
        // 调用 NativeGallery
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (path == null)
            {
                Debug.Log("User cancelled picking image");
                return;
            }

            // 加载图片为 Texture2D
            Texture2D texture = NativeGallery.LoadImageAtPath(path, 2048);
            if (texture == null)
            {
                Debug.LogError("Failed to load texture");
                return;
            }
            MovieFrameTexture = texture;
            movieName = currentMovie.name;
            sceneName = currentScene.name;
            frameId = "FromAlbum";
            isFromAlbum = true;

            overlayManager.setMovieFrame();
        }, "Select a photo");
    }
}


