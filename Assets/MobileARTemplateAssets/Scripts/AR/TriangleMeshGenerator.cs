using UnityEngine;

namespace ARJourneyIntoMovies.AR
{
    /// <summary>
    /// Utility script to generate isosceles triangle mesh for arrow
    /// Attach to GameObject, run in Editor, then remove
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class TriangleMeshGenerator : MonoBehaviour
    {
        [Header("Triangle Parameters")]
        [Tooltip("Width of triangle base")]
        public float baseWidth = 0.5f;

        [Tooltip("Height of triangle (from base to apex)")]
        public float height = 1.0f;

        [Tooltip("Direction the triangle points")]
        public Vector3 pointDirection = Vector3.forward;

        [Header("Actions")]
        [Tooltip("Click to generate mesh")]
        public bool generateMesh = false;

        private void Update()
        {
            if (generateMesh)
            {
                generateMesh = false;
                GenerateTriangleMesh();
            }
        }

        /// <summary>
        /// Generate isosceles triangle mesh pointing in specified direction
        /// </summary>
        [ContextMenu("Generate Triangle Mesh")]
        public void GenerateTriangleMesh()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                Debug.LogError("[TriangleMeshGenerator] MeshFilter component not found!");
                return;
            }

            Mesh mesh = new Mesh();
            mesh.name = "TriangleMesh";

            // Normalize direction
            Vector3 forward = pointDirection.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            if (right.magnitude < 0.01f) // If forward is vertical, use different perpendicular
            {
                right = Vector3.Cross(Vector3.right, forward).normalized;
            }

            // Define vertices (isosceles triangle on XZ plane, pointing in Z+ direction)
            // Vertex 0: Apex (front point)
            // Vertex 1: Base left
            // Vertex 2: Base right
            Vector3[] vertices = new Vector3[6]; // Double-sided triangle

            // Front face
            vertices[0] = forward * height;                              // Apex
            vertices[1] = -right * (baseWidth / 2f);                    // Base left
            vertices[2] = right * (baseWidth / 2f);                     // Base right

            // Back face (same vertices, reverse order for back-face culling)
            vertices[3] = vertices[0];
            vertices[4] = vertices[2];
            vertices[5] = vertices[1];

            // Define triangles (counter-clockwise winding)
            int[] triangles = new int[]
            {
                // Front face
                0, 1, 2,
                // Back face
                3, 4, 5
            };

            // Assign to mesh
            mesh.vertices = vertices;
            mesh.triangles = triangles;

            // Calculate normals
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Apply to MeshFilter
            meshFilter.mesh = mesh;

            Debug.Log($"[TriangleMeshGenerator] Triangle mesh generated - Base: {baseWidth}m, Height: {height}m");
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw direction arrow in editor
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, pointDirection.normalized * height);
        }
#endif
    }
}
