using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProceduralPlanet
{
    [ExecuteAlways]
    public class PlanetPatchStreamer : MonoBehaviour
    {
        [SerializeField] private PlanetSettings settings;
        [SerializeField] private Camera targetCamera;

        [Header("Editor Preview")]
        [SerializeField] private bool editorStreamingPreview = true;

        [Header("Quadtree Monitor")]
        [SerializeField] private int rootNodeCount = 6;
        [SerializeField] private int evaluatedNodeCount;
        [SerializeField] private int desiredLeafCount;
        [SerializeField] private int activeLeafCount;
        [SerializeField] private int visibleRendererCount;
        [SerializeField] private int highestActiveLod;

        private readonly Dictionary<PlanetPatchId, PlanetPatchInstance> active =
            new Dictionary<PlanetPatchId, PlanetPatchInstance>();

        private readonly HashSet<PlanetPatchId> desiredLeaves =
            new HashSet<PlanetPatchId>();

        private readonly HashSet<PlanetPatchId> activeAncestors =
            new HashSet<PlanetPatchId>();

        private readonly List<PlanetPatchId> leafWorkList =
            new List<PlanetPatchId>();

        private readonly List<PlanetPatchId> createBuffer =
            new List<PlanetPatchId>();

        private readonly List<PlanetPatchId> removeBuffer =
            new List<PlanetPatchId>();

        private readonly Plane[] frustumPlanes = new Plane[6];

        private float nextRuntimeUpdateTime;
        private double nextEditorUpdateTime;

        private Vector3 lastEditorCameraPosition;
        private Quaternion lastEditorCameraRotation;
        private bool hasEditorCameraSample;
        private bool forceEditorEvaluation = true;

        private Material fallbackMaterial;
        private Camera lastEvaluationCamera;

        public PlanetSettings Settings
        {
            get => settings;
            set
            {
                if (settings == value)
                    return;

                settings = value;
                forceEditorEvaluation = true;
            }
        }

        public Camera TargetCamera
        {
            get => targetCamera;
            set => targetCamera = value;
        }

        public bool EditorStreamingPreview
        {
            get => editorStreamingPreview;
            set => editorStreamingPreview = value;
        }

        public int RootNodeCount => rootNodeCount;
        public int EvaluatedNodeCount => evaluatedNodeCount;
        public int DesiredLeafCount => desiredLeafCount;
        public int ActiveLeafCount => activeLeafCount;
        public int VisibleRendererCount => visibleRendererCount;
        public int HighestActiveLod => highestActiveLod;

        public IEnumerable<PlanetPatchInstance> ActivePatches => active.Values;

        private void OnEnable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorTick;
            EditorApplication.update += EditorTick;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorTick;
#endif
        }

        private void Update()
        {
            if (!Application.isPlaying || settings == null)
                return;

            if (targetCamera == null)
                targetCamera = Camera.main;

            if (targetCamera == null)
                return;

            if (Time.unscaledTime < nextRuntimeUpdateTime)
                return;

            nextRuntimeUpdateTime =
                Time.unscaledTime + settings.StreamUpdateInterval;

            UpdateStreaming(
                targetCamera,
                settings.MaxPatchCreatesPerUpdate);
        }

#if UNITY_EDITOR
        private void EditorTick()
        {
            if (Application.isPlaying ||
                !editorStreamingPreview ||
                settings == null)
                return;

            if (EditorApplication.timeSinceStartup < nextEditorUpdateTime)
                return;

            SceneView view = SceneView.lastActiveSceneView;

            if (view == null || view.camera == null)
                return;

            Camera cam = view.camera;
            bool moved = HasEditorCameraMovedEnough(cam);

            if (!forceEditorEvaluation && !moved && createBuffer.Count == 0)
            {
                nextEditorUpdateTime =
                    EditorApplication.timeSinceStartup +
                    Mathf.Max(0.12f, settings.StreamUpdateInterval);

                return;
            }

            nextEditorUpdateTime =
                EditorApplication.timeSinceStartup +
                Mathf.Max(0.08f, settings.StreamUpdateInterval);

            bool changed =
                UpdateStreaming(
                    cam,
                    settings.EditorMaxPatchCreatesPerUpdate);

            forceEditorEvaluation = false;

            lastEditorCameraPosition = cam.transform.position;
            lastEditorCameraRotation = cam.transform.rotation;
            hasEditorCameraSample = true;

            if (changed)
                view.Repaint();
        }

        private bool HasEditorCameraMovedEnough(Camera cam)
        {
            if (!hasEditorCameraSample)
                return true;

            if (Vector3.Distance(
                    cam.transform.position,
                    lastEditorCameraPosition) >=
                settings.EditorCameraMoveThreshold)
                return true;

            return Quaternion.Angle(
                       cam.transform.rotation,
                       lastEditorCameraRotation) >=
                   settings.EditorCameraRotationThreshold;
        }
#endif

        public void RebuildStreamingState()
        {
            ClearAllPatches();

            nextRuntimeUpdateTime = 0f;
            nextEditorUpdateTime = 0d;
            forceEditorEvaluation = true;
            hasEditorCameraSample = false;

            if (settings == null)
                return;

            if (Application.isPlaying)
            {
                Camera cam = targetCamera != null ? targetCamera : Camera.main;

                if (cam != null)
                    UpdateStreaming(cam, settings.MaxPatchCreatesPerUpdate);
            }
#if UNITY_EDITOR
            else
            {
                SceneView view = SceneView.lastActiveSceneView;

                if (view != null && view.camera != null)
                    UpdateStreaming(
                        view.camera,
                        settings.EditorMaxPatchCreatesPerUpdate);
            }
#endif
        }

        public void ClearAllPatches()
        {
            foreach (var pair in active)
                DestroyPatch(pair.Value);

            active.Clear();
            desiredLeaves.Clear();
            activeAncestors.Clear();
            leafWorkList.Clear();
            createBuffer.Clear();
            removeBuffer.Clear();

            activeLeafCount = 0;
            desiredLeafCount = 0;
            visibleRendererCount = 0;
            highestActiveLod = 0;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(child);
                else
                    Destroy(child);
#else
                Destroy(child);
#endif
            }
        }

        private bool UpdateStreaming(
            Camera cameraToUse,
            int createBudget)
        {
            if (settings == null || cameraToUse == null)
                return false;

            lastEvaluationCamera = cameraToUse;

            GeometryUtility.CalculateFrustumPlanes(
                cameraToUse,
                frustumPlanes);

            BuildDesiredCoverage(cameraToUse.transform.position);
            BuildActiveAncestorCache();

            createBuffer.Clear();

            foreach (PlanetPatchId id in desiredLeaves)
            {
                if (!active.ContainsKey(id))
                    createBuffer.Add(id);
            }

            Vector3 cameraPosition = cameraToUse.transform.position;

            createBuffer.Sort(
                (a, b) =>
                    DistanceToPatch(a, cameraPosition)
                    .CompareTo(DistanceToPatch(b, cameraPosition)));

            // Transition headroom avoids deadlock when replacing parents with children.
            int transitionHeadroom =
                Mathf.Max(
                    16,
                    createBudget * 8);

            int hardActiveLimit =
                settings.MaxActiveLeafPatches +
                transitionHeadroom;

            int room =
                Mathf.Max(
                    0,
                    hardActiveLimit - active.Count);

            int createCount =
                Mathf.Min(
                    createBudget,
                    createBuffer.Count,
                    room);

            bool changed = false;

            // IMPORTANT: create replacements first.
            for (int i = 0; i < createCount; i++)
            {
                CreatePatch(createBuffer[i]);
                changed = true;
            }

            // Only now consider removal, and only if replacement coverage exists.
            removeBuffer.Clear();

            foreach (var pair in active)
            {
                if (desiredLeaves.Contains(pair.Key))
                    continue;

                if (CanSafelyRemoveObsoletePatch(pair.Key))
                    removeBuffer.Add(pair.Key);
            }

            for (int i = 0; i < removeBuffer.Count; i++)
            {
                RemovePatch(removeBuffer[i]);
                changed = true;
            }

            UpdateRendererVisibility(cameraToUse);

            activeLeafCount = active.Count;
            highestActiveLod = 0;

            foreach (var pair in active)
                highestActiveLod =
                    Mathf.Max(
                        highestActiveLod,
                        pair.Key.Level);

            return changed;
        }

        private void BuildDesiredCoverage(Vector3 cameraPosition)
        {
            desiredLeaves.Clear();
            leafWorkList.Clear();

            evaluatedNodeCount = 0;

            // Coverage comes first: ALWAYS begin with all six cube faces.
            foreach (PlanetFace face in Enum.GetValues(typeof(PlanetFace)))
            {
                leafWorkList.Add(
                    new PlanetPatchId(
                        face,
                        0,
                        0,
                        0));
            }

            int maxLeaves =
                Mathf.Max(
                    6,
                    settings.MaxActiveLeafPatches);

            // Each quadtree split replaces 1 leaf with 4 children: net +3.
            while (leafWorkList.Count + 3 <= maxLeaves)
            {
                int bestIndex = FindBestLeafToSplit(cameraPosition);

                if (bestIndex < 0)
                    break;

                PlanetPatchId parent = leafWorkList[bestIndex];

                leafWorkList.RemoveAt(bestIndex);

                int childLevel = parent.Level + 1;
                int baseX = parent.X * 2;
                int baseY = parent.Y * 2;

                leafWorkList.Add(
                    new PlanetPatchId(
                        parent.Face,
                        childLevel,
                        baseX,
                        baseY));

                leafWorkList.Add(
                    new PlanetPatchId(
                        parent.Face,
                        childLevel,
                        baseX + 1,
                        baseY));

                leafWorkList.Add(
                    new PlanetPatchId(
                        parent.Face,
                        childLevel,
                        baseX,
                        baseY + 1));

                leafWorkList.Add(
                    new PlanetPatchId(
                        parent.Face,
                        childLevel,
                        baseX + 1,
                        baseY + 1));
            }

            for (int i = 0; i < leafWorkList.Count; i++)
                desiredLeaves.Add(leafWorkList[i]);

            desiredLeafCount = desiredLeaves.Count;
        }

        private int FindBestLeafToSplit(Vector3 cameraPosition)
        {
            int bestIndex = -1;
            float bestScore = float.PositiveInfinity;

            for (int i = 0; i < leafWorkList.Count; i++)
            {
                PlanetPatchId id = leafWorkList[i];

                evaluatedNodeCount++;

                if (id.Level >= settings.MaxLodLevel)
                    continue;

                Vector3 localDirection =
                    CubeSphereUtility.GetPatchCenterDirection(id);

                Vector3 worldCenter =
                    transform.TransformPoint(
                        localDirection * settings.Radius);

                float distance =
                    Vector3.Distance(
                        cameraPosition,
                        worldCenter);

                if (distance > settings.PreloadDistance)
                    continue;

                if (!IsHorizonRelevant(
                        id,
                        localDirection,
                        cameraPosition))
                    continue;

                float patchSize =
                    CubeSphereUtility.ApproximatePatchWorldSize(
                        settings.Radius,
                        id.Level);

                bool wasSplit =
                    IsCurrentlySplit(id);

                float thresholdMultiplier =
                    wasSplit
                        ? settings.MergeDistanceMultiplier
                        : settings.SplitDistanceMultiplier;

                float splitDistance =
                    patchSize * thresholdMultiplier;

                if (distance >= splitDistance)
                    continue;

                float normalizedDistance =
                    distance /
                    Mathf.Max(0.001f, splitDistance);

                // Prefer finer detail directly under / near the camera.
                float cameraFacing =
                    Vector3.Dot(
                        localDirection,
                        transform
                            .InverseTransformPoint(cameraPosition)
                            .normalized);

                float facingPenalty =
                    Mathf.Lerp(
                        0.35f,
                        1f,
                        Mathf.InverseLerp(
                            -0.1f,
                            1f,
                            cameraFacing));

                float score =
                    normalizedDistance /
                    Mathf.Max(0.1f, facingPenalty);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private void BuildActiveAncestorCache()
        {
            activeAncestors.Clear();

            foreach (PlanetPatchId id in active.Keys)
            {
                int x = id.X;
                int y = id.Y;

                for (int level = id.Level - 1; level >= 0; level--)
                {
                    x >>= 1;
                    y >>= 1;

                    activeAncestors.Add(
                        new PlanetPatchId(
                            id.Face,
                            level,
                            x,
                            y));
                }
            }
        }

        private bool IsCurrentlySplit(PlanetPatchId id)
        {
            return activeAncestors.Contains(id);
        }

        private bool CanSafelyRemoveObsoletePatch(PlanetPatchId oldId)
        {
            // Merge case:
            // if a desired ancestor is already active, it covers this old child.
            PlanetPatchId ancestor = oldId;

            int ax = oldId.X;
            int ay = oldId.Y;

            for (int level = oldId.Level - 1; level >= 0; level--)
            {
                ax >>= 1;
                ay >>= 1;

                ancestor =
                    new PlanetPatchId(
                        oldId.Face,
                        level,
                        ax,
                        ay);

                if (desiredLeaves.Contains(ancestor) &&
                    active.ContainsKey(ancestor))
                    return true;
            }

            // Split case:
            // every desired descendant that replaces this old parent
            // must already exist before the old parent is removed.
            bool foundReplacementDescendant = false;

            foreach (PlanetPatchId desired in desiredLeaves)
            {
                if (desired.Face != oldId.Face ||
                    desired.Level <= oldId.Level)
                    continue;

                int levelDifference =
                    desired.Level - oldId.Level;

                int ancestorX =
                    desired.X >> levelDifference;

                int ancestorY =
                    desired.Y >> levelDifference;

                if (ancestorX != oldId.X ||
                    ancestorY != oldId.Y)
                    continue;

                foundReplacementDescendant = true;

                if (!active.ContainsKey(desired))
                    return false;
            }

            return foundReplacementDescendant;
        }

        private bool IsHorizonRelevant(
            PlanetPatchId id,
            Vector3 patchCenterDirection,
            Vector3 cameraPosition)
        {
            Vector3 cameraFromCenter =
                transform.InverseTransformPoint(cameraPosition);

            if (cameraFromCenter.sqrMagnitude < 0.0001f)
                return true;

            Vector3 cameraDirection =
                cameraFromCenter.normalized;

            float angularSize =
                CubeSphereUtility.ApproximatePatchAngularSizeRadians(
                    id.Level);

            float threshold =
                -Mathf.Sin(
                    Mathf.Min(
                        Mathf.PI * 0.49f,
                        angularSize * 1.35f));

            return Vector3.Dot(
                       patchCenterDirection,
                       cameraDirection) >=
                   threshold;
        }

        private float DistanceToPatch(
            PlanetPatchId id,
            Vector3 cameraPosition)
        {
            Vector3 normal =
                CubeSphereUtility.GetPatchCenterDirection(id);

            Vector3 center =
                transform.TransformPoint(
                    normal * settings.Radius);

            return Vector3.Distance(
                cameraPosition,
                center);
        }

        private void CreatePatch(PlanetPatchId id)
        {
            if (active.ContainsKey(id))
                return;

            Mesh mesh =
                PlanetPatchMeshBuilder.Build(
                    id,
                    settings.LeafPatchResolution,
                    settings.Radius,
                    settings.SkirtDepth);

            GameObject go = new GameObject(id.ToString());

            go.hideFlags = HideFlags.DontSaveInBuild;
            go.transform.SetParent(transform, false);

            MeshFilter filter = go.AddComponent<MeshFilter>();
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();

            filter.sharedMesh = mesh;

            renderer.sharedMaterial =
                settings.PreviewMaterial != null
                    ? settings.PreviewMaterial
                    : GetFallbackMaterial();

            bool createCollider =
                settings.GeneratePatchColliders &&
                (Application.isPlaying ||
                 settings.GenerateEditorPreviewColliders);

            if (createCollider)
            {
                MeshCollider collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
            }

            Vector3 normal =
                CubeSphereUtility.GetPatchCenterDirection(id);

            Vector3 center =
                transform.TransformPoint(
                    normal * settings.Radius);

            float patchSize =
                CubeSphereUtility.ApproximatePatchWorldSize(
                    settings.Radius,
                    id.Level);

            Bounds bounds =
                new Bounds(
                    center,
                    Vector3.one * patchSize * 1.9f);

            active.Add(
                id,
                new PlanetPatchInstance
                {
                    Id = id,
                    GameObject = go,
                    Mesh = mesh,
                    Renderer = renderer,
                    ApproximateBounds = bounds,
                    Center = center,
                    SurfaceNormal = normal
                });
        }

        private void UpdateRendererVisibility(Camera cameraToUse)
        {
            int visible = 0;
            Vector3 cameraPosition = cameraToUse.transform.position;

            foreach (PlanetPatchInstance patch in active.Values)
            {
                float distance =
                    Vector3.Distance(
                        cameraPosition,
                        patch.Center);

                bool inDistance =
                    distance <= settings.VisibleDistance;

                bool inFrustum =
                    GeometryUtility.TestPlanesAABB(
                        frustumPlanes,
                        patch.ApproximateBounds);

                bool horizonRelevant =
                    IsHorizonRelevant(
                        patch.Id,
                        patch.SurfaceNormal,
                        cameraPosition);

                bool shouldRender =
                    inDistance &&
                    inFrustum &&
                    horizonRelevant;

                if (patch.Renderer.enabled != shouldRender)
                    patch.Renderer.enabled = shouldRender;

                if (shouldRender)
                    visible++;
            }

            visibleRendererCount = visible;
        }

        private void RemovePatch(PlanetPatchId id)
        {
            if (!active.TryGetValue(
                    id,
                    out PlanetPatchInstance patch))
                return;

            active.Remove(id);
            DestroyPatch(patch);
        }

        private void DestroyPatch(PlanetPatchInstance patch)
        {
            if (patch == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (patch.GameObject != null)
                    DestroyImmediate(patch.GameObject);

                if (patch.Mesh != null)
                    DestroyImmediate(patch.Mesh);
            }
            else
            {
                if (patch.GameObject != null)
                    Destroy(patch.GameObject);

                if (patch.Mesh != null)
                    Destroy(patch.Mesh);
            }
#else
            if (patch.GameObject != null)
                Destroy(patch.GameObject);

            if (patch.Mesh != null)
                Destroy(patch.Mesh);
#endif
        }

        private Material GetFallbackMaterial()
        {
            if (fallbackMaterial != null)
                return fallbackMaterial;

            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
                shader = Shader.Find("Standard");

            if (shader == null)
                return null;

            fallbackMaterial =
                new Material(shader)
                {
                    name = "Planet_Greybox_Fallback"
                };

            Color grey =
                new Color(
                    0.52f,
                    0.54f,
                    0.55f,
                    1f);

            if (fallbackMaterial.HasProperty("_BaseColor"))
                fallbackMaterial.SetColor("_BaseColor", grey);
            else if (fallbackMaterial.HasProperty("_Color"))
                fallbackMaterial.SetColor("_Color", grey);

            return fallbackMaterial;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (settings == null || !settings.DebugEnabled)
                return;

            DrawPlanetOutline();

            if (settings.ShowRootFaceGuides)
                DrawRootFaceGuides();

            if (settings.ShowActivePatchBorders)
                DrawActivePatchGuides();

            if (settings.ShowCameraSurfaceGuide)
                DrawCameraSurfaceGuide();

            if (settings.ShowStreamingRangeGuides)
                DrawStreamingRangeGuides();
        }

        private void DrawPlanetOutline()
        {
            if (!settings.ShowPlanetOutline)
                return;

            Handles.color =
                new Color(
                    0.95f,
                    0.95f,
                    0.95f,
                    0.85f);

            Vector3 center = transform.position;
            float radius = settings.Radius;

            Handles.DrawWireDisc(center, transform.right, radius);
            Handles.DrawWireDisc(center, transform.up, radius);
            Handles.DrawWireDisc(center, transform.forward, radius);
        }

        private void DrawRootFaceGuides()
        {
            foreach (PlanetFace face in Enum.GetValues(typeof(PlanetFace)))
            {
                DrawPatchBoundary(
                    new PlanetPatchId(face, 0, 0, 0),
                    new Color(1f, 0.8f, 0.15f, 0.85f),
                    Mathf.Max(4, settings.DebugEdgeSegments));
            }
        }

        private void DrawActivePatchGuides()
        {
            int guideCount = 0;
            int labelCount = 0;

            foreach (PlanetPatchInstance patch in active.Values)
            {
                if (guideCount >= settings.DebugMaxPatchGuides)
                    break;

                Color color =
                    settings.ShowLodColors
                        ? LodColor(patch.Id.Level)
                        : new Color(0.2f, 0.9f, 1f, 0.9f);

                DrawPatchBoundary(
                    patch.Id,
                    color,
                    settings.DebugEdgeSegments);

                guideCount++;

                if (settings.ShowPatchCenters)
                {
                    Handles.color = color;

                    float size =
                        Mathf.Max(
                            settings.Radius * 0.0015f,
                            0.05f);

                    Handles.DotHandleCap(
                        0,
                        patch.Center,
                        Quaternion.identity,
                        size,
                        EventType.Repaint);
                }

                if ((settings.ShowLodLabels ||
                     settings.ShowPatchIds) &&
                    labelCount < settings.DebugMaxLabels)
                {
                    string label = "";

                    if (settings.ShowLodLabels)
                        label = $"LOD {patch.Id.Level}";

                    if (settings.ShowPatchIds)
                    {
                        if (label.Length > 0)
                            label += "\n";

                        label += patch.Id.ToString();
                    }

                    Handles.Label(patch.Center, label);
                    labelCount++;
                }
            }
        }

        private void DrawPatchBoundary(
            PlanetPatchId id,
            Color color,
            int segments)
        {
            Handles.color = color;

            DrawPatchEdge(id, segments, 0);
            DrawPatchEdge(id, segments, 1);
            DrawPatchEdge(id, segments, 2);
            DrawPatchEdge(id, segments, 3);
        }

        private void DrawPatchEdge(
            PlanetPatchId id,
            int segments,
            int edge)
        {
            Vector3 previous =
                EvaluateEdgePoint(id, edge, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;

                Vector3 current =
                    EvaluateEdgePoint(id, edge, t);

                Handles.DrawLine(previous, current);
                previous = current;
            }
        }

        private Vector3 EvaluateEdgePoint(
            PlanetPatchId id,
            int edge,
            float t)
        {
            float u = 0f;
            float v = 0f;

            switch (edge)
            {
                case 0: u = t; v = 0f; break;
                case 1: u = 1f; v = t; break;
                case 2: u = 1f - t; v = 1f; break;
                case 3: u = 0f; v = 1f - t; break;
            }

            Vector3 local =
                CubeSphereUtility.EvaluatePatchDirection(
                    id,
                    u,
                    v) *
                settings.Radius;

            return transform.TransformPoint(local);
        }

        private void DrawCameraSurfaceGuide()
        {
            Camera cam =
                Application.isPlaying
                    ? targetCamera
                    : lastEvaluationCamera;

            if (cam == null)
                return;

            Vector3 center = transform.position;
            Vector3 fromCenter = cam.transform.position - center;

            if (fromCenter.sqrMagnitude < 0.001f)
                return;

            Vector3 surfacePoint =
                center +
                fromCenter.normalized * settings.Radius;

            Handles.color =
                new Color(
                    1f,
                    0.2f,
                    0.2f,
                    0.9f);

            Handles.DrawDottedLine(
                center,
                cam.transform.position,
                5f);

            float markerSize =
                Mathf.Max(
                    settings.Radius * 0.01f,
                    0.2f);

            Handles.DrawWireDisc(
                surfacePoint,
                fromCenter.normalized,
                markerSize);
        }

        private void DrawStreamingRangeGuides()
        {
            Camera cam =
                Application.isPlaying
                    ? targetCamera
                    : lastEvaluationCamera;

            if (cam == null)
                return;

            Vector3 p = cam.transform.position;

            Handles.color =
                new Color(0.25f, 1f, 0.25f, 0.28f);

            Handles.DrawWireDisc(
                p,
                cam.transform.up,
                settings.VisibleDistance);

            Handles.color =
                new Color(1f, 0.8f, 0.15f, 0.24f);

            Handles.DrawWireDisc(
                p,
                cam.transform.up,
                settings.PreloadDistance);

            Handles.color =
                new Color(1f, 0.25f, 0.25f, 0.20f);

            Handles.DrawWireDisc(
                p,
                cam.transform.up,
                settings.UnloadDistance);
        }

        private static Color LodColor(int level)
        {
            float hue =
                Mathf.Repeat(
                    level * 0.137f,
                    1f);

            Color c =
                Color.HSVToRGB(
                    hue,
                    0.78f,
                    1f);

            c.a = 0.95f;

            return c;
        }
#endif
    }
}
