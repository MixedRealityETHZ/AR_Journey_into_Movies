using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ARJourneyIntoMovies.Server
{
    [Serializable]
    public class SessionPose
    {
        public float[] translation;
        public float[] rotation_xyzw;
    }

    [Serializable]
    public class LocalizeResponse
    {
        public bool success;
        public SessionPose session_pose;
    }

    /// <summary>
    /// Client for communicating with HLoc server (localization service)
    /// Sends camera images and intrinsics via HTTP POST and receives pose data
    /// </summary>
    public class ServerClient : MonoBehaviour
    {
        [Header("Server Configuration")]
        [Tooltip("URL of the localization server")]
        public string serverUrl = "http://10.5.111.194:5000/localize";

        [Header("Request Settings")]
        [Tooltip("JPEG quality for image compression (0-100)")]
        [Range(0, 100)]
        public int jpegQuality = 75;

        [Tooltip("Request timeout in seconds")]
        public float timeoutSeconds = 30f;

        // Events (cannot use [Header] attribute on events)
        public event Action<PoseData> OnPoseReceived;
        public event Action<string> OnError;

        private void Awake()
        {
            Debug.Log($"[ServerClient] Initialized with server URL: {serverUrl}");
        }

        
        public void ProcessServerResponse(string json)
        {
            Debug.Log("[ServerClient] Received response: " + json);

            LocalizeResponse raw;
            try
            {
                raw = JsonUtility.FromJson<LocalizeResponse>(json);
            }
            catch (Exception e)
            {
                OnError?.Invoke("JSON parse failed: " + e.Message);
                return;
            }

            if (!raw.success)
            {
                OnError?.Invoke("Server returned success=false");
                return;
            }

            // Build PoseData for your project
            PoseData pose = new PoseData();
            pose.success = true;

            // ----- rotation: convert xyzw → wxyz -----
            float[] r = raw.session_pose.rotation_xyzw;
            pose.rotation = new float[] { r[0], r[1], r[2], r[3] }; 
            // w = r[3]

            // ----- translation: copy -----
            pose.translation = raw.session_pose.translation;

            // Optional: export confidence/fov if needed
            pose.confidence = 1.0f;
            pose.fov = 60;
            pose.aspect = 16f / 9f;
            pose.movie_frame_id = "film";

            Debug.Log($"[ServerClient] Final PoseData → T={pose.GetTranslation()}, R={pose.GetRotation().eulerAngles}");

            OnPoseReceived?.Invoke(pose);
        }
        /// <summary>
        /// Send query image to server for localization
        /// </summary>
        /// <param name="queryImage">Camera image to localize</param>
        /// <param name="intrinsics">Camera intrinsics (fx, fy, cx, cy)</param>
        public IEnumerator SendQueryImage(Texture2D queryImage, Vector4 intrinsics)
        {
            Debug.Log($"[ServerClient] Sending query image - Size: {queryImage.width}x{queryImage.height}");
            Debug.Log($"[ServerClient] Camera intrinsics: fx={intrinsics.x:F2}, fy={intrinsics.y:F2}, cx={intrinsics.z:F2}, cy={intrinsics.w:F2}");

            // Encode image to JPEG
            byte[] imageBytes = queryImage.EncodeToJPG(jpegQuality);
            Debug.Log($"[ServerClient] Image encoded to JPEG - Size: {imageBytes.Length / 1024}KB");

            // Create form data
            WWWForm form = new WWWForm();
            form.AddBinaryData("image", imageBytes, "query.jpg", "image/jpeg");
            form.AddField("fx", intrinsics.x.ToString("F2"));
            form.AddField("fy", intrinsics.y.ToString("F2"));
            form.AddField("cx", intrinsics.z.ToString("F2"));
            form.AddField("cy", intrinsics.w.ToString("F2"));
            form.AddField("width", queryImage.width.ToString());
            form.AddField("height", queryImage.height.ToString());

            // Send POST request
            using (UnityWebRequest request = UnityWebRequest.Post(serverUrl, form))
            {
                request.timeout = (int)timeoutSeconds;

                Debug.Log($"[ServerClient] Sending POST request to {serverUrl}...");
                yield return request.SendWebRequest();

                // Handle response
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseJson = request.downloadHandler.text;
                    Debug.Log($"[ServerClient] Response received: {responseJson}");

                    try
                    {
                        // Parse JSON response
                        PoseData poseData = JsonUtility.FromJson<PoseData>(responseJson);

                        if (poseData.success)
                        {
                            Debug.Log($"[ServerClient] Localization SUCCESS - Confidence: {poseData.confidence:F2}");
                            Debug.Log($"[ServerClient] Position: {poseData.GetTranslation()}");
                            Debug.Log($"[ServerClient] Rotation: {poseData.GetRotation().eulerAngles}");
                            Debug.Log($"[ServerClient] FOV: {poseData.fov}°, Aspect: {poseData.aspect:F2}");
                            Debug.Log($"[ServerClient] Movie Frame ID: {poseData.movie_frame_id}");

                            // Trigger success event
                            OnPoseReceived?.Invoke(poseData);
                        }
                        else
                        {
                            string errorMsg = "Server returned success=false";
                            Debug.LogWarning($"[ServerClient] {errorMsg}");
                            OnError?.Invoke(errorMsg);
                        }
                    }
                    catch (Exception e)
                    {
                        string errorMsg = $"Failed to parse JSON response: {e.Message}";
                        Debug.LogError($"[ServerClient] {errorMsg}");
                        OnError?.Invoke(errorMsg);
                    }
                }
                else
                {
                    string errorMsg = $"Network error: {request.error} (Code: {request.responseCode})";
                    Debug.LogError($"[ServerClient] {errorMsg}");
                    OnError?.Invoke(errorMsg);
                }
            }
        }

        /// <summary>
        /// Mock response for testing (triggers event without network call)
        /// </summary>
        public void TriggerMockResponse()
        {
            Debug.Log("[ServerClient] Triggering mock response");

            PoseData mockData = new PoseData
            {
                success = true,
                rotation = new float[] { 1f, 0f, 0f, 0f }, // w, x, y, z (identity rotation)
                translation = new float[] { 0f, 0f, 2f },   // x, y, z (2m forward)
                fov = 60f,
                aspect = 16f / 9f,
                movie_frame_id = "mock_frame_001",
                confidence = 0.95f
            };

            OnPoseReceived?.Invoke(mockData);
        }
    }
}
