using System;
using UnityEngine;

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
        public string reason;
        public bool isFromAlbum;
        public bool firstAlbumFrame;
    }
    /// <summary>
    /// Client for communicating with HLoc server (localization service)
    /// Sends camera images and intrinsics via HTTP POST and receives pose data
    /// </summary>
    public class ServerClient : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject hudPanel;
        public ARFrameUploader uploader;

        // Events (cannot use [Header] attribute on events)
        public event Action<PoseData> OnPoseReceived;
        public event Action<string> OnError;

        private void Awake()
        {
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
                OnError?.Invoke(raw.reason);
                return;
            }

            // Build PoseData for your project
            PoseData pose = new PoseData();
            pose.success = true;

            // ----- rotation: convert xyzw → wxyz -----
            float[] r = raw.session_pose.rotation_xyzw;
            pose.rotation = new float[] { r[3], r[0], r[1], r[2] }; 
            // w = r[3]

            // ----- translation: copy -----
            pose.translation = raw.session_pose.translation;

            // Optional: export confidence/fov if needed
            pose.confidence = 1.0f;
            pose.fov = 60;
            pose.aspect = 9f / 16f;
            pose.movie_frame_id = "film";

            Debug.Log($"[ServerClient] Final PoseData → T={pose.GetTranslation()}, R={pose.GetRotation().eulerAngles}");

            uploader.enabled = false;
            OnPoseReceived?.Invoke(pose);
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
                translation = new float[] { 0f, 0f, 0.1f },   // x, y, z (2m forward)
                fov = 60f,
                aspect = 9f / 16f,
                movie_frame_id = "mock_frame_001",
                confidence = 0.95f
            };

            OnPoseReceived?.Invoke(mockData);
        }

        
    }
}
