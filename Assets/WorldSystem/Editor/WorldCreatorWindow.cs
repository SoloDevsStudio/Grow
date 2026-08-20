#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProceduralPlanet.Editor
{
    public class WorldCreatorWindow : EditorWindow
    {
        private enum Tab
        {
            Setup,
            World,
            LOD,
            Camera,
            Debug
        }

        private Tab tab;

        private PlanetSettings settings;
        private ProceduralPlanetRoot planetRoot;
        private PlanetNavigationRig navigationRig;

        private UnityEditor.Editor settingsEditor;

        private Vector2 scroll;

        [MenuItem(
            "Tools/Procedural Planet/World Creator")]
        public static void Open()
        {
            WorldCreatorWindow window =
                GetWindow<WorldCreatorWindow>();

            window.titleContent =
                new GUIContent(
                    "World Creator");

            window.minSize =
                new Vector2(
                    520f,
                    650f);

            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawTabs();

            scroll =
                EditorGUILayout
                    .BeginScrollView(scroll);

            switch (tab)
            {
                case Tab.Setup:
                    DrawSetup();
                    break;

                case Tab.World:
                    DrawWorld();
                    break;

                case Tab.LOD:
                    DrawLod();
                    break;

                case Tab.Camera:
                    DrawCamera();
                    break;

                case Tab.Debug:
                    DrawDebug();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField(
                "PROCEDURAL PLANET — WORLD CREATOR",
                EditorStyles.boldLabel);

            EditorGUILayout.LabelField(
                $"Generator Version: {WorldGenerationVersion.Current}",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(4);
        }

        private void DrawTabs()
        {
            tab =
                (Tab)GUILayout.Toolbar(
                    (int)tab,
                    new[]
                    {
                        "SETUP",
                        "WORLD",
                        "LOD",
                        "CAMERA",
                        "DEBUG"
                    });

            EditorGUILayout.Space(8);
        }

        private void DrawSetup()
        {
            EditorGUILayout.LabelField(
                "PROJECT SETUP",
                EditorStyles.boldLabel);

            settings =
                (PlanetSettings)
                EditorGUILayout.ObjectField(
                    "Planet Settings",
                    settings,
                    typeof(PlanetSettings),
                    false);

            if (GUILayout.Button(
                    "Create New Planet Settings Asset",
                    GUILayout.Height(34)))
            {
                CreateSettingsAsset();
            }

            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "Create or assign PlanetSettings first.",
                    MessageType.Info);

                return;
            }

            if (GUILayout.Button(
                    "Find Existing World Objects",
                    GUILayout.Height(28)))
            {
                FindExistingObjects();
            }

            EditorGUILayout.Space(8);

            if (GUILayout.Button(
                    "PREPARE WORLD",
                    GUILayout.Height(44)))
            {
                PrepareWorld();
                FitPlanetToSceneView();
            }

            EditorGUILayout.HelpBox(
                "Prepare World creates/repairs the planet streamer, enables editor preview, rebuilds the quadtree and frames the complete planet.",
                MessageType.None);
        }

        private void DrawWorld()
        {
            if (!RequireSettings())
                return;

            EnsureSettingsEditor();

            SerializedObject so =
                new SerializedObject(settings);

            so.Update();

            EditorGUILayout.LabelField(
                "WORLD",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                so.FindProperty("worldName"));

            EditorGUILayout.PropertyField(
                so.FindProperty("seed"));

            EditorGUILayout.PropertyField(
                so.FindProperty("radius"));

            EditorGUILayout.PropertyField(
                so.FindProperty("maxTerrainHeight"));

            EditorGUILayout.PropertyField(
                so.FindProperty("previewMaterial"));

            EditorGUILayout.Space(8);

            EditorGUILayout.PropertyField(
                so.FindProperty("autoScaleStreaming"));

            if (!so.FindProperty(
                    "autoScaleStreaming")
                .boolValue)
            {
                EditorGUILayout.PropertyField(
                    so.FindProperty("visibleDistance"));

                EditorGUILayout.PropertyField(
                    so.FindProperty("preloadDistance"));

                EditorGUILayout.PropertyField(
                    so.FindProperty("unloadDistance"));
            }

            EditorGUILayout.PropertyField(
                so.FindProperty(
                    "generatePatchColliders"));

            so.ApplyModifiedProperties();

            EditorGUILayout.Space(10);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        "Rebuild World",
                        GUILayout.Height(34)))
                {
                    PrepareWorld();

                    if (planetRoot != null &&
                        planetRoot.Streamer != null)
                    {
                        planetRoot.Streamer
                            .RebuildStreamingState();
                    }

                    FitPlanetToSceneView();
                }

                if (GUILayout.Button(
                        "Fit Planet To View",
                        GUILayout.Height(34)))
                {
                    FitPlanetToSceneView();
                }
            }

            if (settings.AutoScaleStreaming)
            {
                EditorGUILayout.HelpBox(
                    $"Auto Stream Ranges\n" +
                    $"Visible: {settings.VisibleDistance:0}\n" +
                    $"Preload: {settings.PreloadDistance:0}\n" +
                    $"Unload: {settings.UnloadDistance:0}",
                    MessageType.Info);
            }
        }

        private void DrawLod()
        {
            if (!RequireSettings())
                return;

            SerializedObject so =
                new SerializedObject(settings);

            so.Update();

            EditorGUILayout.LabelField(
                "QUADTREE LOD",
                EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                so.FindProperty("maxLodLevel"));

            EditorGUILayout.PropertyField(
                so.FindProperty("leafPatchResolution"));

            EditorGUILayout.PropertyField(
                so.FindProperty(
                    "splitDistanceMultiplier"));

            EditorGUILayout.PropertyField(
                so.FindProperty(
                    "mergeDistanceMultiplier"));

            EditorGUILayout.PropertyField(
                so.FindProperty("skirtDepth"));

            EditorGUILayout.PropertyField(
                so.FindProperty(
                    "maxActiveLeafPatches"));

            EditorGUILayout.Space(8);

            EditorGUILayout.PropertyField(
                so.FindProperty(
                    "streamUpdateInterval"));

            EditorGUILayout.PropertyField(
                so.FindProperty(
                    "maxPatchCreatesPerUpdate"));

            so.ApplyModifiedProperties();

            EditorGUILayout.Space(10);

            if (GUILayout.Button(
                    "Rebuild Quadtree",
                    GUILayout.Height(36)))
            {
                PrepareWorld();

                if (planetRoot != null &&
                    planetRoot.Streamer != null)
                {
                    planetRoot.Streamer
                        .RebuildStreamingState();
                }

                FitPlanetToSceneView();
            }

            if (planetRoot != null &&
                planetRoot.Streamer != null)
            {
                PlanetPatchStreamer s =
                    planetRoot.Streamer;

                EditorGUILayout.Space(10);

                EditorGUILayout.LabelField(
                    "LIVE QUADTREE",
                    EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField(
                        "Root Nodes",
                        s.RootNodeCount);

                    EditorGUILayout.IntField(
                        "Evaluated Nodes",
                        s.EvaluatedNodeCount);

                    EditorGUILayout.IntField(
                        "Desired Leaves",
                        s.DesiredLeafCount);

                    EditorGUILayout.IntField(
                        "Active Leaves",
                        s.ActiveLeafCount);

                    EditorGUILayout.IntField(
                        "Visible Renderers",
                        s.VisibleRendererCount);

                    EditorGUILayout.IntField(
                        "Highest LOD",
                        s.HighestActiveLod);
                }
            }
        }

        private void DrawCamera()
        {
            if (!RequireSettings())
                return;

            EditorGUILayout.LabelField(
                "CAMERA",
                EditorStyles.boldLabel);

            navigationRig =
                (PlanetNavigationRig)
                EditorGUILayout.ObjectField(
                    "Navigation Rig",
                    navigationRig,
                    typeof(PlanetNavigationRig),
                    true);

            if (GUILayout.Button(
                    "Create / Reset Navigation Rig",
                    GUILayout.Height(38)))
            {
                PrepareWorld();

                navigationRig =
                    PlanetNavigationRigFactory
                        .CreateOrReset(
                            settings,
                            planetRoot);

                FitRuntimeCamera();
            }

            if (navigationRig != null)
            {
                EditorGUILayout.Space(8);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(
                            "FREE CAMERA",
                            GUILayout.Height(36)))
                    {
                        navigationRig.NavigationMode =
                            PlanetNavigationMode.FreeCamera;
                    }

                    if (GUILayout.Button(
                            "FIRST PERSON",
                            GUILayout.Height(36)))
                    {
                        navigationRig.NavigationMode =
                            PlanetNavigationMode
                                .FirstPersonPlanet;
                    }
                }

                if (GUILayout.Button(
                        "Fit Runtime Camera To Planet",
                        GUILayout.Height(30)))
                {
                    FitRuntimeCamera();
                }

                if (navigationRig.NavigationMode ==
                    PlanetNavigationMode
                        .FirstPersonPlanet)
                {
                    if (GUILayout.Button(
                            "Snap First Person To Surface",
                            GUILayout.Height(30)))
                    {
                        navigationRig
                            .SnapToSurface();
                    }
                }
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField(
                "SCENE VIEW",
                EditorStyles.boldLabel);

            SceneViewFlyNavigator.Enabled =
                EditorGUILayout.Toggle(
                    "Enable Editor Fly Controls",
                    SceneViewFlyNavigator.Enabled);

            SceneViewFlyNavigator.Speed =
                EditorGUILayout.FloatField(
                    "Editor Move Speed",
                    SceneViewFlyNavigator.Speed);

            SceneViewFlyNavigator.Boost =
                EditorGUILayout.FloatField(
                    "Shift Boost",
                    SceneViewFlyNavigator.Boost);

            if (GUILayout.Button(
                    "Fit Scene View To Planet",
                    GUILayout.Height(30)))
            {
                FitPlanetToSceneView();
            }
        }

        private void DrawDebug()
        {
            if (!RequireSettings())
                return;

            SerializedObject so =
                new SerializedObject(settings);

            so.Update();

            EditorGUILayout.LabelField(
                "VISUAL DEBUG",
                EditorStyles.boldLabel);

            SerializedProperty master =
                so.FindProperty("debugEnabled");

            EditorGUILayout.PropertyField(
                master,
                new GUIContent(
                    "MASTER DEBUG"));

            if (master.boolValue)
            {
                EditorGUILayout.Space(6);

                EditorGUILayout.PropertyField(
                    so.FindProperty(
                        "showPlanetOutline"),
                    new GUIContent(
                        "Planet Circle / Sphere Outline"));

                EditorGUILayout.PropertyField(
                    so.FindProperty(
                        "showRootFaceGuides"),
                    new GUIContent(
                        "6 Root Face Guides"));

                EditorGUILayout.PropertyField(
                    so.FindProperty(
                        "showActivePatchBorders"),
                    new GUIContent(
                        "Active Streamed Patch Borders"));

                EditorGUILayout.PropertyField(
                    so.FindProperty(
                        "showLodColors"),
                    new GUIContent(
                        "LOD Colors"));

                EditorGUILayout.PropertyField(
                    so.FindProperty(
                        "showLodLabels"),
                    new GUIContent(
                        "LOD Labels"));

                EditorGUILayout.PropertyField(
                    so.FindProperty(
                        "showPatchIds"),
                    new GUIContent(
                        "Patch IDs"));

                EditorGUILayout.PropertyField(
                    so.FindProperty(
                        "showPatchCenters"),
                    new GUIContent(
                        "Patch Center Markers"));

                EditorGUILayout.PropertyField(
                    so.FindProperty(
                        "showCameraSurfaceGuide"),
                    new GUIContent(
                        "Camera → Surface Guide"));

                EditorGUILayout.PropertyField(
                    so.FindProperty(
                        "showStreamingRangeGuides"),
                    new GUIContent(
                        "Visible / Preload / Unload Guides"));

                EditorGUILayout.PropertyField(
                    so.FindProperty(
                        "debugEdgeSegments"),
                    new GUIContent(
                        "Guide Smoothness"));
            }

            so.ApplyModifiedProperties();

            EditorGUILayout.Space(10);

            FindExistingObjects();

            if (planetRoot != null &&
                planetRoot.Streamer != null)
            {
                PlanetPatchStreamer s =
                    planetRoot.Streamer;

                EditorGUILayout.LabelField(
                    "STREAMING STATE",
                    EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField(
                        "Evaluated Nodes",
                        s.EvaluatedNodeCount);

                    EditorGUILayout.IntField(
                        "Desired Leaves",
                        s.DesiredLeafCount);

                    EditorGUILayout.IntField(
                        "Active Leaf Meshes",
                        s.ActiveLeafCount);

                    EditorGUILayout.IntField(
                        "Visible Leaf Renderers",
                        s.VisibleRendererCount);

                    EditorGUILayout.IntField(
                        "Highest LOD",
                        s.HighestActiveLod);
                }

                EditorGUILayout.Space(6);

                s.EditorStreamingPreview =
                    EditorGUILayout.Toggle(
                        "Editor Streaming Preview",
                        s.EditorStreamingPreview);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(
                            "Rebuild Preview"))
                    {
                        s.RebuildStreamingState();
                    }

                    if (GUILayout.Button(
                            "Clear Preview"))
                    {
                        s.ClearAllPatches();
                    }
                }
            }

            EditorGUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "The white great-circle guides show the planet boundary. Yellow lines show the six original cube-sphere faces. Colored lines show only the active streamed quadtree leaf patches. Move toward the surface and the colored regions should subdivide.",
                MessageType.Info);
        }

        private bool RequireSettings()
        {
            if (settings != null)
                return true;

            EditorGUILayout.HelpBox(
                "Go to SETUP and create/assign PlanetSettings first.",
                MessageType.Warning);

            return false;
        }

        private void PrepareWorld()
        {
            if (settings == null)
                return;

            if (planetRoot == null)
            {
                planetRoot =
                    FindFirstObjectByType<
                        ProceduralPlanetRoot>();
            }

            if (planetRoot == null)
            {
                GameObject go =
                    new GameObject(
                        "Procedural Planet");

                Undo.RegisterCreatedObjectUndo(
                    go,
                    "Create Procedural Planet");

                planetRoot =
                    go.AddComponent<
                        ProceduralPlanetRoot>();
            }

            planetRoot.Settings =
                settings;

            planetRoot
                .PreparePhase2Streaming();

            if (planetRoot.Streamer != null)
            {
                planetRoot.Streamer
                    .EditorStreamingPreview = true;

                planetRoot.Streamer
                    .RebuildStreamingState();
            }

            Selection.activeGameObject =
                planetRoot.gameObject;
        }

        private void FindExistingObjects()
        {
            if (planetRoot == null)
            {
                planetRoot =
                    FindFirstObjectByType<
                        ProceduralPlanetRoot>();
            }

            if (navigationRig == null)
            {
                navigationRig =
                    FindFirstObjectByType<
                        PlanetNavigationRig>();
            }
        }

        private void FitPlanetToSceneView()
        {
            if (settings == null)
                return;

            SceneView view =
                SceneView.lastActiveSceneView;

            if (view == null ||
                view.camera == null)
                return;

            Vector3 center =
                planetRoot != null
                    ? planetRoot
                        .transform.position
                    : Vector3.zero;

            float fovRad =
                view.camera.fieldOfView *
                Mathf.Deg2Rad;

            float distance =
                (settings.Radius /
                 Mathf.Sin(
                     fovRad * 0.5f)) *
                settings.CameraFitMargin;

            view.LookAt(
                center,
                Quaternion.identity,
                distance);

            view.Repaint();
        }

        private void FitRuntimeCamera()
        {
            if (settings == null ||
                navigationRig == null)
                return;

            Camera cam =
                navigationRig
                    .GetComponentInChildren<
                        Camera>(true);

            if (cam == null)
                return;

            PlanetNavigationRigFactory
                .PositionRigForPlanetFit(
                    navigationRig.transform,
                    cam,
                    settings);

            if (planetRoot != null &&
                planetRoot.Streamer != null)
            {
                planetRoot.Streamer
                    .TargetCamera = cam;
            }
        }

        private void EnsureSettingsEditor()
        {
            if (settingsEditor == null ||
                settingsEditor.target != settings)
            {
                if (settingsEditor != null)
                    DestroyImmediate(
                        settingsEditor);

                settingsEditor =
                    UnityEditor.Editor
                        .CreateEditor(settings);
            }
        }

        private void CreateSettingsAsset()
        {
            string path =
                EditorUtility
                    .SaveFilePanelInProject(
                        "Create Planet Settings",
                        "PlanetSettings",
                        "asset",
                        "Choose where to save PlanetSettings.");

            if (string.IsNullOrEmpty(path))
                return;

            PlanetSettings asset =
                CreateInstance<
                    PlanetSettings>();

            AssetDatabase.CreateAsset(
                asset,
                path);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            settings = asset;

            Selection.activeObject =
                asset;

            EditorGUIUtility.PingObject(
                asset);
        }

        private void OnDisable()
        {
            if (settingsEditor != null)
            {
                DestroyImmediate(
                    settingsEditor);

                settingsEditor = null;
            }
        }
    }
}
#endif
