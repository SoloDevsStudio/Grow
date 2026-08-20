using UnityEngine;

namespace ProceduralPlanet
{
    public static class CubeSphereUtility
    {
        // Maps a point on a unit cube to a sphere.
        // This "spherified cube" mapping distributes vertices more evenly than Normalize().
        public static Vector3 CubeToSphere(Vector3 p)
        {
            float x2 = p.x * p.x;
            float y2 = p.y * p.y;
            float z2 = p.z * p.z;

            return new Vector3(
                p.x * Mathf.Sqrt(Mathf.Max(0f, 1f - (y2 * 0.5f) - (z2 * 0.5f) + (y2 * z2 / 3f))),
                p.y * Mathf.Sqrt(Mathf.Max(0f, 1f - (z2 * 0.5f) - (x2 * 0.5f) + (z2 * x2 / 3f))),
                p.z * Mathf.Sqrt(Mathf.Max(0f, 1f - (x2 * 0.5f) - (y2 * 0.5f) + (x2 * y2 / 3f)))
            ).normalized;
        }
    }
}
