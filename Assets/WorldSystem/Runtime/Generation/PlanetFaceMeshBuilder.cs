using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralPlanet
{
    public static class PlanetFaceMeshBuilder
    {
        public static Mesh Build(PlanetFace face, int resolution, float radius)
        {
            resolution = Mathf.Max(2, resolution);

            int vertexCount = resolution * resolution;
            int quadCount = (resolution - 1) * (resolution - 1);
            int indexCount = quadCount * 6;

            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var triangles = new int[indexCount];

            for (int y = 0; y < resolution; y++)
            {
                float v = y / (float)(resolution - 1);
                float cubeY = Mathf.Lerp(-1f, 1f, v);

                for (int x = 0; x < resolution; x++)
                {
                    float u = x / (float)(resolution - 1);
                    float cubeX = Mathf.Lerp(-1f, 1f, u);

                    int index = x + y * resolution;

                    Vector3 cubePoint = GetCubePoint(face, cubeX, cubeY);
                    Vector3 direction = CubeSphereUtility.CubeToSphere(cubePoint);

                    vertices[index] = direction * radius;
                    normals[index] = direction;
                    uvs[index] = new Vector2(u, v);
                }
            }

            int tri = 0;
            for (int y = 0; y < resolution - 1; y++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int i = x + y * resolution;

                    int a = i;
                    int b = i + resolution;
                    int c = i + resolution + 1;
                    int d = i + 1;

                    // Winding is corrected after construction if necessary.
                    triangles[tri++] = a;
                    triangles[tri++] = b;
                    triangles[tri++] = c;

                    triangles[tri++] = a;
                    triangles[tri++] = c;
                    triangles[tri++] = d;
                }
            }

            EnsureOutwardWinding(vertices, triangles);

            var mesh = new Mesh
            {
                name = $"PlanetFace_{face}"
            };

            if (vertexCount > 65535)
                mesh.indexFormat = IndexFormat.UInt32;

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            return mesh;
        }

        private static Vector3 GetCubePoint(PlanetFace face, float a, float b)
        {
            switch (face)
            {
                case PlanetFace.PositiveX: return new Vector3(1f, b, -a);
                case PlanetFace.NegativeX: return new Vector3(-1f, b, a);
                case PlanetFace.PositiveY: return new Vector3(a, 1f, -b);
                case PlanetFace.NegativeY: return new Vector3(a, -1f, b);
                case PlanetFace.PositiveZ: return new Vector3(a, b, 1f);
                case PlanetFace.NegativeZ: return new Vector3(-a, b, -1f);
                default: return Vector3.forward;
            }
        }

        private static void EnsureOutwardWinding(Vector3[] vertices, int[] triangles)
        {
            if (triangles.Length < 3)
                return;

            Vector3 a = vertices[triangles[0]];
            Vector3 b = vertices[triangles[1]];
            Vector3 c = vertices[triangles[2]];

            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            Vector3 outward = ((a + b + c) / 3f).normalized;

            if (Vector3.Dot(normal, outward) >= 0f)
                return;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int temp = triangles[i + 1];
                triangles[i + 1] = triangles[i + 2];
                triangles[i + 2] = temp;
            }
        }
    }
}
