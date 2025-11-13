using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
[Serializable] public class ImageSize { public int width; public int height; }
[Serializable] public class Intrinsics { public float fx, fy, cx, cy; public string model; }
[Serializable] public class Notes { public bool mirrorY_applied; public string coord; }
[Serializable]
public class Payload
{
    public long timestamp_ms;
    public float[] rotation_xyzw;   // Rwc: [x,y,z,w]
    public float[] translation_m;   // camera center C: [tx,ty,tz]
    public ImageSize image_size;
    public Intrinsics intrinsics;
    public Notes notes;
}



[RequireComponent(typeof(ARCameraManager))]
public class ARFrameUploaderV2 : MonoBehaviour
{
    [SerializeField] private ARCameraManager cameraManager;
    [SerializeField] private float captureInterval = 3f;
    [SerializeField] private string serverUrl = "http://192.168.8.109:5000/upload";

    // down stream (server / SfM）use OpenCV/+Z coordinate
    [SerializeField] private bool convertToOpenCVCamera = false;

    private float timer = 0f;

    void Awake()
    {
        if (cameraManager == null)
            cameraManager = GetComponent<ARCameraManager>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= captureInterval)
        {
            timer = 0f;
            if (verboseLog) Debug.Log($"[ARFU] tick — try capture (interval={captureInterval}s)");
            StartCoroutine(CaptureAndUpload());
        }
    }

    [SerializeField] private bool verboseLog = true; // 调试日志开关
    void OnEnable()
    {
        // 订阅不到 frameReceived 也没关系（你当前是用 Update 定时器），这里只做网络连通自检
        try
        {
            var baseUrl = serverUrl;
            var i = baseUrl.LastIndexOf("/upload");
            if (i > 0) baseUrl = baseUrl.Substring(0, i);
            StartCoroutine(PingServer(baseUrl + "/ping"));
        }
        catch (System.Exception e)
        {
            if (verboseLog) Debug.LogWarning("[ARFU] Build ping URL failed: " + e.Message);
        }
    }

    private IEnumerator PingServer(string url)
    {
        if (verboseLog) Debug.Log("[ARFU] Pinging " + url);
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogError("[ARFU] Ping failed: " + www.error);
            else if (verboseLog)
                Debug.Log("[ARFU] Ping ok: " + www.downloadHandler.text);
        }
    }

    private IEnumerator CaptureAndUpload()
    {
        // 0) ARSession 状态（如果还没初始化，直接返回）
        if (ARSession.state <= ARSessionState.Ready)
        {
            if (verboseLog) Debug.LogWarning($"[ARFU] ARSession not tracking yet: {ARSession.state}");
            yield break;
        }

        // 1) CPU 图像
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            Debug.LogWarning("[ARFU] TryAcquireLatestCpuImage = false (no CPU image available)");
            yield break;
        }

        if (verboseLog) Debug.Log($"[ARFU] Got CPU image: {image.width}x{image.height}");

        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.RGBA32,
            //transformation = XRCpuImage.Transformation.MirrorY // 垂直翻转
            transformation = XRCpuImage.Transformation.None
        };

        int size = image.GetConvertedDataSize(conversionParams);
        var buffer = new NativeArray<byte>(size, Allocator.Temp);
        image.Convert(conversionParams, buffer);
        int width = image.width;
        int height = image.height;
        image.Dispose();

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(buffer);
        texture.Apply();
        buffer.Dispose();

        // 2) 位姿（camera→world）
        var cam = cameraManager.GetComponent<Camera>().transform;
        Quaternion qUnity = cam.rotation;
        Vector3 tUnity = cam.position;

        if (convertToOpenCVCamera)
            qUnity = qUnity * Quaternion.Euler(0f, 180f, 0f);

        float[] rot_xyzw = new float[] { qUnity.x, qUnity.y, qUnity.z, qUnity.w };
        float[] pos_m = new float[] { tUnity.x, tUnity.y, tUnity.z };

        // 3) 内参
        float fx = 0, fy = 0, cx = 0, cy = 0;
        int intrW = width, intrH = height;
        if (cameraManager.TryGetIntrinsics(out XRCameraIntrinsics intr))
        {
            fx = intr.focalLength.x;
            fy = intr.focalLength.y;
            cx = intr.principalPoint.x;
            cy = intr.principalPoint.y;
            intrW = intr.resolution.x;
            intrH = intr.resolution.y;
        }

        if (intrW != width || intrH != height)
        {
            float sx = (float)width / intrW;
            float sy = (float)height / intrH;
            fx *= sx; fy *= sy;
            cx *= sx; cy *= sy;
        }

        float cy_flipped = (height - 1) - cy;

        // 4) 组 JSON
        long ts_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var payload = new Payload
        {
            timestamp_ms = ts_ms,
            rotation_xyzw = rot_xyzw,
            translation_m = pos_m,
            image_size = new ImageSize { width = width, height = height },
            intrinsics = new Intrinsics { fx = fx, fy = fy, cx = cx, cy = cy_flipped, model = "pinhole" },
            notes = new Notes { mirrorY_applied = true, coord = convertToOpenCVCamera ? "OpenCV(+Z forward)" : "Unity(+Z forward)" }
        };
        string json = JsonUtility.ToJson(payload);

        // 5) 上传
        byte[] imageBytes = texture.EncodeToPNG();
        Destroy(texture);

        var form = new WWWForm();
        form.AddBinaryData("image", imageBytes, $"frame_{payload.timestamp_ms}.png", "image/png");
        form.AddField("meta_json", json);

        if (verboseLog) Debug.Log("[ARFU] POST " + serverUrl);
        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
        {
            //www.chunkedTransfer = false;  
            www.timeout = 10;              
            www.SetRequestHeader("Connection", "close"); 

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogError("[ARFU] Upload failed: " + www.error + " (code=" + www.responseCode + ")");
            else
                Debug.Log($"[ARFU] Uploaded #{ts_ms}, size=({width}x{height})");
        }
    }



}
