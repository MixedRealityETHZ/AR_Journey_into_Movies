using UnityEngine;

namespace ARJourneyIntoMovies.AR
{
    /// <summary>
    /// Builds frustum mesh for visualizing target camera pose
    /// Generates wireframe or solid frustum based on FOV and aspect ratio
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class FrustumBuilder : MonoBehaviour
    {
        [Header("Frustum Parameters")]
        [Tooltip("Length of frustum (depth)")]
        public float frustumLength = 2f;

        [Tooltip("Whether to build wireframe or solid frustum")]
        public bool wireframe = false;

        [Tooltip("Line thickness for wireframe mode")]
        public float lineThickness = 0.02f;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            Debug.Log("[FrustumBuilder] Initialized");
        }

        /// <summary>
        /// Build frustum mesh based on camera FOV and aspect ratio
        /// Creates a pyramid shape representing the camera's view volume
        /// </summary>
        /// <param name="fovDegrees">Vertical field of view in degrees</param>
        /// <param name="aspect">Aspect ratio (width/height)</param>
        /// <param name="length">Length of frustum</param>
        public Mesh BuildFrustumMesh(float fovDegrees, float aspect, float length)
        {
            Debug.Log($"[FrustumBuilder] Building frustum - FOV: {fovDegrees}°, Aspect: {aspect:F2}, Length: {length}m");

            Mesh mesh = new Mesh();
            mesh.name = "FrustumMesh";

            // Calculate half-height and half-width at far plane
            float halfHeight = length * Mathf.Tan(fovDegrees * 0.5f * Mathf.Deg2Rad);
            float halfWidth = halfHeight * aspect;

            // Define vertices
            // 0: Camera origin (apex)
            // 1-4: Far plane corners (top-left, top-right, bottom-right, bottom-left)
            Vector3[] vertices = new Vector3[5];
            vertices[0] = Vector3.zero; // Camera position (apex)
            vertices[1] = new Vector3(-halfWidth, halfHeight, length);   // Top-left
            vertices[2] = new Vector3(halfWidth, halfHeight, length);    // Top-right
            vertices[3] = new Vector3(halfWidth, -halfHeight, length);   // Bottom-right
            vertices[4] = new Vector3(-halfWidth, -halfHeight, length);  // Bottom-left

            if (wireframe)
            {
                // Wireframe mode: create thin lines
                BuildWireframeMesh(mesh, vertices);
            }
            else
            {
                // Solid mode: create pyramid faces
                BuildSolidMesh(mesh, vertices);
            }

            return mesh;
        }

        /// <summary>
        /// Build wireframe frustum (12 edges as thin quads)
        /// </summary>
        private void BuildWireframeMesh(Mesh mesh, Vector3[] frustumVertices)
        {
            // Wireframe consists of:
            // - 4 edges from apex to far plane corners (pyramid edges)
            // - 4 edges around far plane (rectangle)
            // Total: 8 edges

            int edgeCount = 8;
            Vector3[] vertices = new Vector3[edgeCount * 4]; // Each edge = 4 vertices (quad)
            int[] triangles = new int[edgeCount * 6]; // Each edge = 2 triangles = 6 indices

            int vIdx = 0;
            int tIdx = 0;

            // Helper function to add edge as quad
            void AddEdge(Vector3 start, Vector3 end)
            {
                Vector3 direction = (end - start).normalized;
                Vector3 offset = Vector3.Cross(direction, Vector3.up).normalized * lineThickness * 0.5f;
                if (offset.magnitude < 0.01f) // If direction is vertical, use different perpendicular
                {
                    offset = Vector3.Cross(direction, Vector3.right).normalized * lineThickness * 0.5f;
                }

                // Quad vertices
                vertices[vIdx + 0] = start - offset;
                vertices[vIdx + 1] = start + offset;
                vertices[vIdx + 2] = end + offset;
                vertices[vIdx + 3] = end - offset;

                // Triangles (CCW winding)
                triangles[tIdx + 0] = vIdx + 0;
                triangles[tIdx + 1] = vIdx + 1;
                triangles[tIdx + 2] = vIdx + 2;
                triangles[tIdx + 3] = vIdx + 0;
                triangles[tIdx + 4] = vIdx + 2;
                triangles[tIdx + 5] = vIdx + 3;

                vIdx += 4;
                tIdx += 6;
            }

            // Pyramid edges (apex to far corners)
            AddEdge(frustumVertices[0], frustumVertices[1]); // Apex to top-left
            AddEdge(frustumVertices[0], frustumVertices[2]); // Apex to top-right
            AddEdge(frustumVertices[0], frustumVertices[3]); // Apex to bottom-right
            AddEdge(frustumVertices[0], frustumVertices[4]); // Apex to bottom-left

            // Far plane rectangle edges
            AddEdge(frustumVertices[1], frustumVertices[2]); // Top edge
            AddEdge(frustumVertices[2], frustumVertices[3]); // Right edge
            AddEdge(frustumVertices[3], frustumVertices[4]); // Bottom edge
            AddEdge(frustumVertices[4], frustumVertices[1]); // Left edge

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Build solid frustum (pyramid with 5 faces)
        /// </summary>
        private void BuildSolidMesh(Mesh mesh, Vector3[] frustumVertices)
        {
            // 5 faces: 4 triangle sides + 1 quad far plane
            Vector3[] vertices = new Vector3[5]; // Duplicate vertices for proper normals
            int[] triangles = new int[18]; // 4 triangles (sides) + 2 triangles (far plane)

            // Copy vertices (duplicated for each face to have correct normals)
            for (int i = 0; i < 5; i++)
            {
                vertices[i] = frustumVertices[i];
            }

            // Triangle indices for 4 side faces
            // Face 1: Apex - Top-left - Top-right
            triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
            triangles[3] = 0; triangles[4] = 3; triangles[5] = 2;
            triangles[6] = 0; triangles[7] = 4; triangles[8] = 3;
            triangles[9] = 0; triangles[10] = 1; triangles[11] = 4;
            triangles[12] = 1; triangles[13] = 3; triangles[14] = 2;
            triangles[15] = 1; triangles[16] = 4; triangles[17] = 3;

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Update frustum visualization with new parameters
        /// </summary>
        public void UpdateFrustum(float fov, float aspect)
        {
            Mesh newMesh = BuildFrustumMesh(fov, aspect, frustumLength);
            meshFilter.mesh = newMesh;
        }

        /// <summary>
        /// Show/hide frustum
        /// </summary>
        public void SetVisible(bool visible)
        {
            meshRenderer.enabled = visible;
            Debug.Log($"[FrustumBuilder] Frustum visibility: {visible}");
        }

        /// <summary>
        /// Set material color (for runtime adjustments)
        /// </summary>
        public void SetColor(Color color)
        {
            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.color = color;
            }
        }

        /// <summary>
        /// Set material (for runtime material swaps)
        /// </summary>
        public void SetMaterial(Material material)
        {
            if (meshRenderer != null)
            {
                meshRenderer.material = material;
            }
        }
    }
}
