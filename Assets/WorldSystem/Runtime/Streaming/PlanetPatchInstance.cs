using UnityEngine;

namespace ProceduralPlanet
{
    public sealed class PlanetPatchInstance
    {
        public PlanetPatchId Id;
        public GameObject GameObject;
        public Mesh Mesh;
        public MeshRenderer Renderer;
        public Bounds ApproximateBounds;
        public Vector3 Center;
        public Vector3 SurfaceNormal;
    }
}
