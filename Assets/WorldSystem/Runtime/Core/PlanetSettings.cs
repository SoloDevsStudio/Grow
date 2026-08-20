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

        [Header("Planet")]
        [Min(10f)]
        [SerializeField] private float radius = 500f;

        [Range(4, 256)]
        [SerializeField] private int baseResolution = 32;

        [Min(0f)]
        [SerializeField] private float maxTerrainHeight = 25f;

        [Header("Streaming")]
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
        public int PatchResolution => patchResolution;
        public int PreloadRing => preloadRing;
        public float StreamDistance => streamDistance;
        public bool ShowPatchBounds => showPatchBounds;
        public bool ShowPatchIds => showPatchIds;
    }
}
