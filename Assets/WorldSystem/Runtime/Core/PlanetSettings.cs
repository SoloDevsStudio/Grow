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
        [SerializeField] private float radius = 100f;

        [Min(0f)]
        [SerializeField] private float maxTerrainHeight = 25f;

        [SerializeField] private Material previewMaterial;

        [Header("Phase 3 - Quadtree LOD")]
        [Range(0, 12)]
        [SerializeField] private int maxLodLevel = 6;

        [Range(4, 64)]
        [SerializeField] private int leafPatchResolution = 12;

        [Range(0.5f, 10f)]
        [SerializeField] private float splitDistanceMultiplier = 3.0f;

        [Range(0.5f, 12f)]
        [SerializeField] private float mergeDistanceMultiplier = 4.0f;

        [Min(0f)]
        [SerializeField] private float skirtDepth = 2f;

        [Range(24, 2000)]
        [SerializeField] private int maxActiveLeafPatches = 180;

        [Header("Streaming Range")]
        [SerializeField] private bool autoScaleStreaming = true;

        [Range(1.5f, 8f)]
        [SerializeField] private float autoVisibleRadiusMultiplier = 3.2f;

        [Range(1.5f, 10f)]
        [SerializeField] private float autoPreloadRadiusMultiplier = 3.8f;

        [Range(2f, 12f)]
        [SerializeField] private float autoUnloadRadiusMultiplier = 4.5f;

        [Min(10f)]
        [SerializeField] private float visibleDistance = 250f;

        [Min(10f)]
        [SerializeField] private float preloadDistance = 330f;

        [Min(10f)]
        [SerializeField] private float unloadDistance = 380f;

        [Range(0.02f, 1f)]
        [SerializeField] private float streamUpdateInterval = 0.10f;

        [Range(1, 128)]
        [SerializeField] private int maxPatchCreatesPerUpdate = 16;

        [Header("Editor Streaming Performance")]
        [Range(1, 32)]
        [SerializeField] private int editorMaxPatchCreatesPerUpdate = 4;

        [Tooltip("Scene camera must move this fraction of planet radius before the quadtree reevaluates.")]
        [Range(0.00001f, 0.02f)]
        [SerializeField] private float editorCameraMoveThresholdRatio = 0.0005f;

        [Range(0.05f, 5f)]
        [SerializeField] private float editorCameraRotationThreshold = 0.5f;

        [Tooltip("Keep OFF normally. Edit-mode MeshCollider cooking is expensive.")]
        [SerializeField] private bool generateEditorPreviewColliders = false;

        [SerializeField] private bool generatePatchColliders = false;

        [Header("Camera")]
        [Range(1.05f, 5f)]
        [SerializeField] private float cameraFitMargin = 1.15f;

        [Min(1f)]
        [SerializeField] private float flyMoveSpeed = 80f;

        [Min(1f)]
        [SerializeField] private float flyBoostMultiplier = 4f;

        [Range(0.01f, 1f)]
        [SerializeField] private float flyLookSensitivity = 0.12f;

        [Header("Visual Debug - Master")]
        [SerializeField] private bool debugEnabled = true;

        [Header("Visual Debug - Guides")]
        [SerializeField] private bool showPlanetOutline = true;
        [SerializeField] private bool showRootFaceGuides = true;
        [SerializeField] private bool showActivePatchBorders = true;
        [SerializeField] private bool showLodColors = true;
        [SerializeField] private bool showLodLabels = false;
        [SerializeField] private bool showPatchIds = false;
        [SerializeField] private bool showPatchCenters = false;
        [SerializeField] private bool showCameraSurfaceGuide = true;
        [SerializeField] private bool showStreamingRangeGuides = false;

        [Range(1, 12)]
        [SerializeField] private int debugEdgeSegments = 3;

        [Range(20, 1000)]
        [SerializeField] private int debugMaxPatchGuides = 120;

        [Range(5, 200)]
        [SerializeField] private int debugMaxLabels = 40;

        public string WorldName => worldName;
        public int Seed => seed;
        public float Radius => radius;
        public float MaxTerrainHeight => maxTerrainHeight;
        public Material PreviewMaterial => previewMaterial;

        public int MaxLodLevel => maxLodLevel;
        public int LeafPatchResolution => leafPatchResolution;
        public float SplitDistanceMultiplier => splitDistanceMultiplier;
        public float MergeDistanceMultiplier => mergeDistanceMultiplier;
        public float SkirtDepth => skirtDepth;
        public int MaxActiveLeafPatches => maxActiveLeafPatches;

        public bool AutoScaleStreaming => autoScaleStreaming;

        public float VisibleDistance =>
            autoScaleStreaming
                ? Mathf.Max(visibleDistance, radius * autoVisibleRadiusMultiplier)
                : visibleDistance;

        public float PreloadDistance =>
            autoScaleStreaming
                ? Mathf.Max(preloadDistance, radius * autoPreloadRadiusMultiplier)
                : preloadDistance;

        public float UnloadDistance =>
            autoScaleStreaming
                ? Mathf.Max(unloadDistance, radius * autoUnloadRadiusMultiplier)
                : unloadDistance;

        public float StreamUpdateInterval => streamUpdateInterval;
        public int MaxPatchCreatesPerUpdate => maxPatchCreatesPerUpdate;
        public int EditorMaxPatchCreatesPerUpdate => editorMaxPatchCreatesPerUpdate;

        public float EditorCameraMoveThreshold =>
            Mathf.Max(0.01f, radius * editorCameraMoveThresholdRatio);

        public float EditorCameraRotationThreshold => editorCameraRotationThreshold;
        public bool GenerateEditorPreviewColliders => generateEditorPreviewColliders;
        public bool GeneratePatchColliders => generatePatchColliders;

        public float CameraFitMargin => cameraFitMargin;
        public float FlyMoveSpeed => flyMoveSpeed;
        public float FlyBoostMultiplier => flyBoostMultiplier;
        public float FlyLookSensitivity => flyLookSensitivity;

        public bool DebugEnabled => debugEnabled;
        public bool ShowPlanetOutline => showPlanetOutline;
        public bool ShowRootFaceGuides => showRootFaceGuides;
        public bool ShowActivePatchBorders => showActivePatchBorders;
        public bool ShowLodColors => showLodColors;
        public bool ShowLodLabels => showLodLabels;
        public bool ShowPatchIds => showPatchIds;
        public bool ShowPatchCenters => showPatchCenters;
        public bool ShowCameraSurfaceGuide => showCameraSurfaceGuide;
        public bool ShowStreamingRangeGuides => showStreamingRangeGuides;
        public int DebugEdgeSegments => debugEdgeSegments;
        public int DebugMaxPatchGuides => debugMaxPatchGuides;
        public int DebugMaxLabels => debugMaxLabels;

#if UNITY_EDITOR
        private void OnValidate()
        {
            radius = Mathf.Max(10f, radius);
            maxLodLevel = Mathf.Clamp(maxLodLevel, 0, 12);
            leafPatchResolution = Mathf.Clamp(leafPatchResolution, 4, 64);

            splitDistanceMultiplier = Mathf.Max(0.5f, splitDistanceMultiplier);
            mergeDistanceMultiplier =
                Mathf.Max(splitDistanceMultiplier + 0.1f, mergeDistanceMultiplier);

            skirtDepth = Mathf.Max(0f, skirtDepth);
            maxActiveLeafPatches = Mathf.Max(24, maxActiveLeafPatches);

            autoPreloadRadiusMultiplier =
                Mathf.Max(autoVisibleRadiusMultiplier, autoPreloadRadiusMultiplier);

            autoUnloadRadiusMultiplier =
                Mathf.Max(autoPreloadRadiusMultiplier, autoUnloadRadiusMultiplier);

            preloadDistance = Mathf.Max(visibleDistance, preloadDistance);
            unloadDistance = Mathf.Max(preloadDistance, unloadDistance);

            streamUpdateInterval = Mathf.Clamp(streamUpdateInterval, 0.02f, 1f);
            maxPatchCreatesPerUpdate = Mathf.Clamp(maxPatchCreatesPerUpdate, 1, 128);
            editorMaxPatchCreatesPerUpdate =
                Mathf.Clamp(editorMaxPatchCreatesPerUpdate, 1, 32);

            cameraFitMargin = Mathf.Max(1.05f, cameraFitMargin);
            debugEdgeSegments = Mathf.Clamp(debugEdgeSegments, 1, 12);
            debugMaxPatchGuides = Mathf.Clamp(debugMaxPatchGuides, 20, 1000);
            debugMaxLabels = Mathf.Clamp(debugMaxLabels, 5, 200);
        }
#endif
    }
}
