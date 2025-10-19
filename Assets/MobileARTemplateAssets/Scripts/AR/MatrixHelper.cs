using UnityEngine;

namespace ARJourneyIntoMovies.AR
{
    /// <summary>
    /// Utility class for matrix and quaternion operations
    /// Provides helper functions for coordinate transformations
    /// </summary>
    public static class MatrixHelper
    {
        /// <summary>
        /// Invert a transformation matrix (TRS matrix)
        /// Uses Unity's built-in inverse function
        /// </summary>
        public static Matrix4x4 InverseTransform(Matrix4x4 matrix)
        {
            return matrix.inverse;
        }

        /// <summary>
        /// Create TRS matrix from position and rotation
        /// </summary>
        public static Matrix4x4 TRS(Vector3 position, Quaternion rotation)
        {
            return Matrix4x4.TRS(position, rotation, Vector3.one);
        }

        /// <summary>
        /// Extract position from transformation matrix
        /// Uses proper TRS extraction
        /// </summary>
        public static Vector3 GetPosition(Matrix4x4 matrix)
        {
            // Extract position from 4th column (index 3)
            return new Vector3(matrix.m03, matrix.m13, matrix.m23);
        }

        /// <summary>
        /// Extract rotation from transformation matrix
        /// Uses Unity's built-in rotation property
        /// </summary>
        public static Quaternion GetRotation(Matrix4x4 matrix)
        {
            return matrix.rotation;
        }

        /// <summary>
        /// Decompose matrix into TRS components
        /// </summary>
        public static void Decompose(Matrix4x4 matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            position = GetPosition(matrix);
            rotation = GetRotation(matrix);
            scale = matrix.lossyScale;
        }

        /// <summary>
        /// Multiply two transformation matrices
        /// Result = A * B (applies B first, then A)
        /// </summary>
        public static Matrix4x4 Multiply(Matrix4x4 a, Matrix4x4 b)
        {
            return a * b;
        }

        /// <summary>
        /// Convert quaternion (w, x, y, z) array to Unity Quaternion
        /// Server format: [w, x, y, z]
        /// </summary>
        public static Quaternion ArrayToQuaternion(float[] arr)
        {
            if (arr == null || arr.Length != 4)
            {
                Debug.LogWarning("[MatrixHelper] Invalid quaternion array, returning identity");
                return Quaternion.identity;
            }
            // Server: [w, x, y, z] → Unity: (x, y, z, w)
            return new Quaternion(arr[1], arr[2], arr[3], arr[0]);
        }

        /// <summary>
        /// Convert Unity Quaternion to array (w, x, y, z)
        /// </summary>
        public static float[] QuaternionToArray(Quaternion q)
        {
            return new float[] { q.w, q.x, q.y, q.z };
        }

        /// <summary>
        /// Log matrix for debugging (formatted output)
        /// </summary>
        public static void LogMatrix(string name, Matrix4x4 matrix)
        {
            Vector3 pos = GetPosition(matrix);
            Quaternion rot = GetRotation(matrix);
            Debug.Log($"[MatrixHelper] {name}:");
            Debug.Log($"  Position: {pos}");
            Debug.Log($"  Rotation: {rot.eulerAngles} (Euler)");
            Debug.Log($"  Quaternion: (w={rot.w:F3}, x={rot.x:F3}, y={rot.y:F3}, z={rot.z:F3})");
        }

        /// <summary>
        /// Check if matrix is valid (not NaN, not infinite)
        /// </summary>
        public static bool IsValid(Matrix4x4 matrix)
        {
            for (int i = 0; i < 16; i++)
            {
                float value = matrix[i];
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
