using UnityEngine;

namespace ProceduralPlanet
{
    public static class CubeSphereUtility
    {
        public static Vector3 CubeToSphere(Vector3 p)
        {
            float x2 = p.x * p.x;
            float y2 = p.y * p.y;
            float z2 = p.z * p.z;

            return new Vector3(
                p.x * Mathf.Sqrt(Mathf.Max(
                    0f,
                    1f - y2 * 0.5f - z2 * 0.5f + y2 * z2 / 3f)),

                p.y * Mathf.Sqrt(Mathf.Max(
                    0f,
                    1f - z2 * 0.5f - x2 * 0.5f + z2 * x2 / 3f)),

                p.z * Mathf.Sqrt(Mathf.Max(
                    0f,
                    1f - x2 * 0.5f - y2 * 0.5f + x2 * y2 / 3f))
            ).normalized;
        }

        public static Vector3 GetCubePoint(
            PlanetFace face,
            float a,
            float b)
        {
            switch (face)
            {
                case PlanetFace.PositiveX:
                    return new Vector3(1f, b, -a);

                case PlanetFace.NegativeX:
                    return new Vector3(-1f, b, a);

                case PlanetFace.PositiveY:
                    return new Vector3(a, 1f, -b);

                case PlanetFace.NegativeY:
                    return new Vector3(a, -1f, b);

                case PlanetFace.PositiveZ:
                    return new Vector3(a, b, 1f);

                case PlanetFace.NegativeZ:
                    return new Vector3(-a, b, -1f);

                default:
                    return Vector3.forward;
            }
        }

        public static int DivisionsAtLevel(int level)
        {
            return 1 << Mathf.Max(0, level);
        }

        public static void GetPatchCubeRange(
            PlanetPatchId id,
            out float minA,
            out float maxA,
            out float minB,
            out float maxB)
        {
            int divisions = DivisionsAtLevel(id.Level);
            float size = 2f / divisions;

            minA = -1f + id.X * size;
            maxA = minA + size;

            minB = -1f + id.Y * size;
            maxB = minB + size;
        }

        public static Vector3 GetPatchCenterDirection(
            PlanetPatchId id)
        {
            GetPatchCubeRange(
                id,
                out float minA,
                out float maxA,
                out float minB,
                out float maxB);

            float a = (minA + maxA) * 0.5f;
            float b = (minB + maxB) * 0.5f;

            return CubeToSphere(
                GetCubePoint(id.Face, a, b));
        }

        public static Vector3 EvaluatePatchDirection(
            PlanetPatchId id,
            float u,
            float v)
        {
            GetPatchCubeRange(
                id,
                out float minA,
                out float maxA,
                out float minB,
                out float maxB);

            float a = Mathf.Lerp(minA, maxA, u);
            float b = Mathf.Lerp(minB, maxB, v);

            return CubeToSphere(
                GetCubePoint(id.Face, a, b));
        }

        public static float ApproximatePatchAngularSizeRadians(
            int level)
        {
            return (Mathf.PI * 0.5f) / DivisionsAtLevel(level);
        }

        public static float ApproximatePatchWorldSize(
            float radius,
            int level)
        {
            return radius *
                   ApproximatePatchAngularSizeRadians(level);
        }
    }
}
