using UnityEngine;

namespace Game.Systems.Mining
{
    /// <summary>
    /// Samples the topmost walkable ground Y of the existing terrain mesh without modifying
    /// the terrain GameObject or adding colliders to the map.
    /// </summary>
    [DisallowMultipleComponent]
    public class TerrainHeightSampler : MonoBehaviour
    {
        [Header("Terrain Mesh Reference (Read-Only)")]
        [SerializeField] private MeshFilter terrainMeshFilter;
        [SerializeField] private float fallbackGroundY = 0f;
        [SerializeField, Range(0.25f, 2f)] private float gridCellSize = 0.5f;

        private float[,] heightGrid;
        private int gridWidth;
        private int gridDepth;
        private float minX;
        private float minZ;
        private bool isBuilt;

        private void Awake()
        {
            BuildHeightCache();
        }

        public void BuildHeightCache()
        {
            isBuilt = false;

            if (terrainMeshFilter == null)
            {
                GameObject ter = GameObject.Find("TER_MainGround_Continuous");
                if (ter != null)
                {
                    terrainMeshFilter = ter.GetComponent<MeshFilter>();
                }
            }

            if (terrainMeshFilter == null || terrainMeshFilter.sharedMesh == null)
            {
                return;
            }

            Mesh mesh = terrainMeshFilter.sharedMesh;
            Transform tf = terrainMeshFilter.transform;
            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.triangles;

            Bounds worldBounds = terrainMeshFilter.GetComponent<Renderer>() != null
                ? terrainMeshFilter.GetComponent<Renderer>().bounds
                : new Bounds(Vector3.zero, new Vector3(130f, 12f, 30f));

            minX = worldBounds.min.x;
            minZ = worldBounds.min.z;
            gridWidth = Mathf.Max(2, Mathf.CeilToInt(worldBounds.size.x / gridCellSize) + 1);
            gridDepth = Mathf.Max(2, Mathf.CeilToInt(worldBounds.size.z / gridCellSize) + 1);

            heightGrid = new float[gridWidth, gridDepth];
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    heightGrid[x, z] = float.MinValue;
                }
            }

            // Rasterize upward-facing triangles into the height grid (topmost surface wins).
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 a = tf.TransformPoint(verts[tris[i]]);
                Vector3 b = tf.TransformPoint(verts[tris[i + 1]]);
                Vector3 c = tf.TransformPoint(verts[tris[i + 2]]);

                Vector3 normal = Vector3.Cross(b - a, c - a);
                if (normal.sqrMagnitude < 1e-6f)
                {
                    continue;
                }

                normal.Normalize();
                if (Mathf.Abs(normal.y) < 0.45f)
                {
                    continue;
                }

                float triMinX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
                float triMaxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
                float triMinZ = Mathf.Min(a.z, Mathf.Min(b.z, c.z));
                float triMaxZ = Mathf.Max(a.z, Mathf.Max(b.z, c.z));

                int gx0 = Mathf.Clamp(Mathf.FloorToInt((triMinX - minX) / gridCellSize), 0, gridWidth - 1);
                int gx1 = Mathf.Clamp(Mathf.CeilToInt((triMaxX - minX) / gridCellSize), 0, gridWidth - 1);
                int gz0 = Mathf.Clamp(Mathf.FloorToInt((triMinZ - minZ) / gridCellSize), 0, gridDepth - 1);
                int gz1 = Mathf.Clamp(Mathf.CeilToInt((triMaxZ - minZ) / gridCellSize), 0, gridDepth - 1);

                for (int gx = gx0; gx <= gx1; gx++)
                {
                    float wx = minX + gx * gridCellSize;
                    for (int gz = gz0; gz <= gz1; gz++)
                    {
                        float wz = minZ + gz * gridCellSize;
                        if (TryBarycentricY(wx, wz, a, b, c, out float y))
                        {
                            if (y > heightGrid[gx, gz])
                            {
                                heightGrid[gx, gz] = y;
                            }
                        }
                    }
                }
            }

            isBuilt = true;
        }

        public float SampleHeight(Vector3 worldPos)
        {
            if (!isBuilt || heightGrid == null)
            {
                return fallbackGroundY;
            }

            float fx = (worldPos.x - minX) / gridCellSize;
            float fz = (worldPos.z - minZ) / gridCellSize;

            int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, gridWidth - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt(fz), 0, gridDepth - 1);
            int x1 = Mathf.Clamp(x0 + 1, 0, gridWidth - 1);
            int z1 = Mathf.Clamp(z0 + 1, 0, gridDepth - 1);

            float h00 = heightGrid[x0, z0];
            float h10 = heightGrid[x1, z0];
            float h01 = heightGrid[x0, z1];
            float h11 = heightGrid[x1, z1];

            if (h00 <= float.MinValue * 0.5f && h10 <= float.MinValue * 0.5f &&
                h01 <= float.MinValue * 0.5f && h11 <= float.MinValue * 0.5f)
            {
                return fallbackGroundY;
            }

            float validDefault = h00 > float.MinValue * 0.5f ? h00 :
                                 h10 > float.MinValue * 0.5f ? h10 :
                                 h01 > float.MinValue * 0.5f ? h01 : h11;

            if (h00 <= float.MinValue * 0.5f) h00 = validDefault;
            if (h10 <= float.MinValue * 0.5f) h10 = validDefault;
            if (h01 <= float.MinValue * 0.5f) h01 = validDefault;
            if (h11 <= float.MinValue * 0.5f) h11 = validDefault;

            float tx = Mathf.Clamp01(fx - x0);
            float tz = Mathf.Clamp01(fz - z0);

            float hx0 = Mathf.Lerp(h00, h10, tx);
            float hx1 = Mathf.Lerp(h01, h11, tx);
            return Mathf.Lerp(hx0, hx1, tz);
        }

        private static bool TryBarycentricY(float px, float pz, Vector3 a, Vector3 b, Vector3 c, out float y)
        {
            float denom = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
            if (Mathf.Abs(denom) < 1e-6f)
            {
                y = 0f;
                return false;
            }

            float w1 = ((b.z - c.z) * (px - c.x) + (c.x - b.x) * (pz - c.z)) / denom;
            float w2 = ((c.z - a.z) * (px - c.x) + (a.x - c.x) * (pz - c.z)) / denom;
            float w3 = 1f - w1 - w2;

            const float eps = -0.02f;
            if (w1 >= eps && w2 >= eps && w3 >= eps)
            {
                y = w1 * a.y + w2 * b.y + w3 * c.y;
                return true;
            }

            y = 0f;
            return false;
        }

        public void ConfigureReference(MeshFilter terrainMesh)
        {
            terrainMeshFilter = terrainMesh;
        }
    }
}
