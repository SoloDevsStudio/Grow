using UnityEngine;

namespace ProceduralPlanet
{
    [CreateAssetMenu(
        fileName = "PlanetSettings",
        menuName = "Procedural Planet/Planet Settings",
        order = 0)]
    public class PlanetSettings : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string worldName = "PrototypeWorld";
        [SerializeField] private int seed = 12345;

        [Header("Planet - Phase 1")]
        [Tooltip("Small prototype value for now. We scale up only after streaming is proven.")]
        [Min(10f)]
        [SerializeField] private float radius = 100f;

        [Tooltip("Vertices per edge of each cube face. 24-32 is ideal for Phase 1 testing.")]
        [Range(4, 256)]
        [SerializeField] private int baseResolution = 32;

        [Tooltip("Reserved for Phase 4 terrain shaping. Phase 1 remains a clean sphere.")]
        [Min(0f)]
        [SerializeField] private float maxTerrainHeight = 25f;

        [Header("Greybox Appearance")]
        [Tooltip("Optional material. If empty, the generator creates a simple grey fallback material.")]
        [SerializeField] private Material previewMaterial;

        [Tooltip("Creates a collider on each face. Useful for testing raycasts; disable if not needed.")]
        [SerializeField] private bool generateColliders = true;

        [Header("Streaming - Reserved For Phase 2")]
        [Min(1)]
        [SerializeField] private int patchResolution = 16;

        [Min(1)]
        [SerializeField] private int preloadRing = 1;

        [Min(10f)]
        [SerializeField] private float streamDistance = 500f;

        [Header("Debug")]
        [SerializeField] private bool showPatchBounds = true;
        [SerializeField] private bool showPatchIds = false;

        public string WorldName => worldName;
        public int Seed => seed;
        public float Radius => radius;
        public int BaseResolution => baseResolution;
        public float MaxTerrainHeight => maxTerrainHeight;
        public Material PreviewMaterial => previewMaterial;
        public bool GenerateColliders => generateColliders;
        public int PatchResolution => patchResolution;
        public int PreloadRing => preloadRing;
        public float StreamDistance => streamDistance;
        public bool ShowPatchBounds => showPatchBounds;
        public bool ShowPatchIds => showPatchIds;

#if UNITY_EDITOR
        private void OnValidate()
        {
            radius = Mathf.Max(10f, radius);
            baseResolution = Mathf.Clamp(baseResolution, 4, 256);
            patchResolution = Mathf.Max(1, patchResolution);
            preloadRing = Mathf.Max(1, preloadRing);
            streamDistance = Mathf.Max(10f, streamDistance);
        }
#endif
    }
}
