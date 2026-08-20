using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralPlanet
{
    public static class PlanetPatchMeshBuilder
    {
        public static Mesh Build(
            PlanetPatchId id,
            int resolution,
            float radius,
            float skirtDepth)
        {
            resolution = Mathf.Max(2, resolution);

            var vertices = new List<Vector3>(
                resolution * resolution + resolution * 8);

            var normals = new List<Vector3>(
                resolution * resolution + resolution * 8);

            var uvs = new List<Vector2>(
                resolution * resolution + resolution * 8);

            var triangles = new List<int>();

            BuildSurface(
                id,
                resolution,
                radius,
                vertices,
                normals,
                uvs,
                triangles);

            if (skirtDepth > 0f)
            {
                BuildSkirt(
                    id,
                    resolution,
                    radius,
                    skirtDepth,
                    vertices,
                    normals,
                    uvs,
                    triangles);
            }

            Mesh mesh = new Mesh
            {
                name = $"Patch_{id}"
            };

            if (vertices.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();

            return mesh;
        }

        private static void BuildSurface(
            PlanetPatchId id,
            int resolution,
            float radius,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<int> triangles)
        {
            int start = vertices.Count;

            for (int y = 0; y < resolution; y++)
            {
                float v = y / (float)(resolution - 1);

                for (int x = 0; x < resolution; x++)
                {
                    float u = x / (float)(resolution - 1);

                    Vector3 dir =
                        CubeSphereUtility.EvaluatePatchDirection(
                            id,
                            u,
                            v);

                    vertices.Add(dir * radius);
                    normals.Add(dir);
                    uvs.Add(new Vector2(u, v));
                }
            }

            for (int y = 0; y < resolution - 1; y++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int i = start + x + y * resolution;

                    int a = i;
                    int b = i + resolution;
                    int c = i + resolution + 1;
                    int d = i + 1;

                    AddOutwardTriangle(
                        vertices,
                        triangles,
                        a,
                        b,
                        c);

                    AddOutwardTriangle(
                        vertices,
                        triangles,
                        a,
                        c,
                        d);
                }
            }
        }

        private static void BuildSkirt(
            PlanetPatchId id,
            int resolution,
            float radius,
            float skirtDepth,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<int> triangles)
        {
            AddSkirtEdge(
                id,
                resolution,
                radius,
                skirtDepth,
                0,
                vertices,
                normals,
                uvs,
                triangles);

            AddSkirtEdge(
                id,
                resolution,
                radius,
                skirtDepth,
                1,
                vertices,
                normals,
                uvs,
                triangles);

            AddSkirtEdge(
                id,
                resolution,
                radius,
                skirtDepth,
                2,
                vertices,
                normals,
                uvs,
                triangles);

            AddSkirtEdge(
                id,
                resolution,
                radius,
                skirtDepth,
                3,
                vertices,
                normals,
                uvs,
                triangles);
        }

        // edge:
        // 0 bottom v=0
        // 1 right  u=1
        // 2 top    v=1
        // 3 left   u=0
        private static void AddSkirtEdge(
            PlanetPatchId id,
            int resolution,
            float radius,
            float skirtDepth,
            int edge,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<int> triangles)
        {
            int start = vertices.Count;

            for (int i = 0; i < resolution; i++)
            {
                float t = i / (float)(resolution - 1);

                float u = 0f;
                float v = 0f;

                switch (edge)
                {
                    case 0:
                        u = t;
                        v = 0f;
                        break;

                    case 1:
                        u = 1f;
                        v = t;
                        break;

                    case 2:
                        u = 1f - t;
                        v = 1f;
                        break;

                    case 3:
                        u = 0f;
                        v = 1f - t;
                        break;
                }

                Vector3 dir =
                    CubeSphereUtility.EvaluatePatchDirection(
                        id,
                        u,
                        v);

                vertices.Add(dir * radius);
                normals.Add(dir);
                uvs.Add(new Vector2(u, v));

                vertices.Add(dir * (radius - skirtDepth));
                normals.Add(dir);
                uvs.Add(new Vector2(u, v));
            }

            for (int i = 0; i < resolution - 1; i++)
            {
                int topA = start + i * 2;
                int bottomA = topA + 1;
                int topB = topA + 2;
                int bottomB = topA + 3;

                // Double-sided skirt.
                triangles.Add(topA);
                triangles.Add(bottomA);
                triangles.Add(bottomB);

                triangles.Add(topA);
                triangles.Add(bottomB);
                triangles.Add(topB);

                triangles.Add(bottomB);
                triangles.Add(bottomA);
                triangles.Add(topA);

                triangles.Add(topB);
                triangles.Add(bottomB);
                triangles.Add(topA);
            }
        }

        private static void AddOutwardTriangle(
            List<Vector3> vertices,
            List<int> triangles,
            int a,
            int b,
            int c)
        {
            Vector3 va = vertices[a];
            Vector3 vb = vertices[b];
            Vector3 vc = vertices[c];

            Vector3 normal =
                Vector3.Cross(vb - va, vc - va);

            Vector3 outward =
                (va + vb + vc).normalized;

            if (Vector3.Dot(normal, outward) >= 0f)
            {
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
            }
            else
            {
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);
            }
        }
    }
}
