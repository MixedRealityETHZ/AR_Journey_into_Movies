using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;

namespace ARJourneyIntoMovies.Server
{
    [Serializable] public class ImageSize { public int width; public int height; }
    [Serializable] public class Intrinsics { public float fx, fy, cx, cy; public string model; }
    [Serializable] public class Notes { public bool mirrorY_applied; public string coord; }

    [Serializable]
    public class Payload
    {
        public long timestamp_ms;
        public float[] rotation_xyzw;   // Rwc: [x,y,z,w]
        public float[] translation_m;   // camera center C
        public ImageSize image_size;
        public Intrinsics intrinsics;
        public Notes notes;

        // 电影信息
        public string movieName;
        public string sceneName;
        public string frameId;
        public bool isFromAlbum;
    }

    [RequireComponent(typeof(ARCameraManager))]
    public class ARFrameUploader : MonoBehaviour
    {
        [Header("AR & Server")]
        [SerializeField] private ARCameraManager cameraManager;
        [SerializeField] public ServerClient serverClient;
        public string serverUrl;
        public MovieSceneFrameController movieSceneFrameController;

        [Header("Capture Settings")]
        [SerializeField] private float captureInterval = 3f;
        [SerializeField] private bool convertToOpenCVCamera = false;
        [SerializeField] private bool verboseLog = true;

        public Action OnCaptureStarted;

        private float timer = 0f;
        void Awake()
        {
            if (cameraManager == null)
                cameraManager = GetComponent<ARCameraManager>();
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer < captureInterval) return;
            timer = 0f;

            var info = movieSceneFrameController.GetSelectedFrameInfo();
            string movieName = info.movie;
            string sceneName = info.scene;
            string frameId = info.frame;
            Texture2D frameTexture = info.frameTexture;
            bool fromAlbum = info.fromAlbum;

            // 到这里说明本帧允许上传
            OnCaptureStarted?.Invoke();

            StartCoroutine(CaptureAndUpload(
                movieName,
                sceneName,
                frameId,
                frameTexture
                // isAlbumMode
            ));
        }

        // =====================================================
        //              Capture + Upload 主流程
        // =====================================================
        private IEnumerator CaptureAndUpload(
            string movieName,
            string sceneName,
            string frameId,
            Texture2D albumTexture)
        {
            // --- 1. ARSession 状态检查 ---
            // if (ARSession.state <= ARSessionState.Ready)
            // {
            //     if (verboseLog)
            //         Debug.LogWarning($"[ARFU] ARSession not tracking yet: {ARSession.state}");
            //     isUploadingNow = false;
            //     yield break;
            // }

            // --- 2. 获取 CPU 图像 ---
            if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                Debug.LogWarning("[ARFU] TryAcquireLatestCpuImage = false (no CPU image available)");
                // isUploadingNow = false;
                yield break;
            }

            if (verboseLog)
                Debug.Log($"[ARFU] Got CPU image: {image.width}x{image.height}");

            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, image.width, image.height),
                outputDimensions = new Vector2Int(image.width, image.height),
                outputFormat = TextureFormat.RGBA32,
                transformation = XRCpuImage.Transformation.MirrorY
            };

            int size = image.GetConvertedDataSize(conversionParams);
            var buffer = new NativeArray<byte>(size, Allocator.Temp);
            image.Convert(conversionParams, buffer);
            int width = image.width;
            int height = image.height;
            image.Dispose();

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.LoadRawTextureData(buffer);
            texture.Apply();
            buffer.Dispose();

            // --- 3. 相机位姿 ---
            var cam = cameraManager.GetComponent<Camera>().transform;
            Quaternion qUnity = cam.rotation;
            Vector3 tUnity = cam.position;

            if (convertToOpenCVCamera)
                qUnity = qUnity * Quaternion.Euler(0f, 180f, 0f);

            float[] rot_xyzw = { qUnity.x, qUnity.y, qUnity.z, qUnity.w };
            float[] pos_m = { tUnity.x, tUnity.y, tUnity.z };

            // --- 4. 相机内参 ---
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

            // --- 5. 构建 Payload JSON ---
            long ts_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var payload = new Payload
            {
                timestamp_ms = ts_ms,
                rotation_xyzw = rot_xyzw,
                translation_m = pos_m,
                image_size = new ImageSize { width = width, height = height },
                intrinsics = new Intrinsics { fx = fx, fy = fy, cx = cx, cy = cy_flipped, model = "pinhole" },
                notes = new Notes { mirrorY_applied = true, coord = convertToOpenCVCamera ? "OpenCV(+Z forward)" : "Unity(+Z forward)" },
                movieName = movieName,
                sceneName = sceneName,
                frameId = frameId,
                isFromAlbum = false
            };
            string json = JsonUtility.ToJson(payload);
            byte[] imageBytes = texture.EncodeToPNG();
            Destroy(texture);

            var form = new WWWForm();
            form.AddBinaryData("image", imageBytes, $"frame_{payload.timestamp_ms}.png", "image/png");
            form.AddField("meta_json", json);

            if (verboseLog) Debug.Log("[ARFU] POST " + serverUrl);
            using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
            {         
                www.SetRequestHeader("Connection", "close"); 

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                    Debug.LogError("[ARFU] Upload failed: " + www.error + " (code=" + www.responseCode + ")");
                else
                {
                    serverClient.ProcessServerResponse(www.downloadHandler.text);
                }
            }
        }
    }
}