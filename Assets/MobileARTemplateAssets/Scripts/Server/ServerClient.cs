using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.UI;
using ARJourneyIntoMovies.AR;

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
    }
    /// <summary>
    /// Client for communicating with HLoc server (localization service)
    /// Sends camera images and intrinsics via HTTP POST and receives pose data
    /// </summary>
    public class ServerClient : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject hudPanel;

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
                string reason = string.IsNullOrEmpty(raw.reason)
                    ? "Localization not ready. Please continue capturing."
                    : raw.reason;

                Debug.LogWarning("[ServerClient] Server returned success=false: " + reason);

                OnError?.Invoke(reason);
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

            OnPoseReceived?.Invoke(pose);
        }
    }
}
