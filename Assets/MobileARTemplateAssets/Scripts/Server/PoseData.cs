using System;
using UnityEngine;

namespace ARJourneyIntoMovies.Server
{
    /// <summary>
    /// Data Transfer Object for server response from /localize endpoint
    /// </summary>
    [Serializable]
    public class PoseData
    {
        public bool success;

        // Quaternion: w, x, y, z
        public float[] rotation;

        // Position: x, y, z
        public float[] translation;

        public float fov;
        public float aspect;

        public string movie_frame_id;
        public float confidence;

        /// <summary>
        /// Convert rotation array to Unity Quaternion
        /// </summary>
        public Quaternion GetRotation()
        {
            if (rotation == null || rotation.Length != 4)
            {
                Debug.LogWarning("Invalid rotation data, returning identity");
                return Quaternion.identity;
            }
            // Server format: [w, x, y, z]
            return new Quaternion(rotation[1], rotation[2], rotation[3], rotation[0]);
        }

        /// <summary>
        /// Convert translation array to Unity Vector3
        /// </summary>
        public Vector3 GetTranslation()
        {
            if (translation == null || translation.Length != 3)
            {
                Debug.LogWarning("Invalid translation data, returning zero");
                return Vector3.zero;
            }
            return new Vector3(translation[0], translation[1], translation[2]);
        }

        /// <summary>
        /// Get camera pose as Matrix4x4 (map space)
        /// </summary>
        public Matrix4x4 GetPoseMatrix()
        {
            return Matrix4x4.TRS(GetTranslation(), GetRotation(), Vector3.one);
        }
    }
}
