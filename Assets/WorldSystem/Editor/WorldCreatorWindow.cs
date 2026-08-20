#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProceduralPlanet.Editor
{
    public class WorldCreatorWindow : EditorWindow
    {
        private PlanetSettings settings;
        private ProceduralPlanetRoot planetRoot;
        private UnityEditor.Editor settingsEditor;

        [MenuItem("Tools/Procedural Planet/World Creator")]
        public static void Open()
        {
            var window = GetWindow<WorldCreatorWindow>();
            window.titleContent = new GUIContent("World Creator");
            window.minSize = new Vector2(440f, 620f);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();

            EditorGUILayout.Space(8);

            settings = (PlanetSettings)EditorGUILayout.ObjectField(
                "Planet Settings",
                settings,
                typeof(PlanetSettings),
                false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Settings Asset", GUILayout.Height(28)))
                    CreateSettingsAsset();

                GUI.enabled = settings != null;
                if (GUILayout.Button("Ping Settings", GUILayout.Height(28)))
                {
                    Selection.activeObject = settings;
                    EditorGUIUtility.PingObject(settings);
                }
                GUI.enabled = true;
            }

            EditorGUILayout.Space(10);

            if (settings != null)
                DrawSettingsInspector();
            else
                EditorGUILayout.HelpBox("Create or assign PlanetSettings first.", MessageType.Info);

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Planet Scene Object", EditorStyles.boldLabel);

            planetRoot = (ProceduralPlanetRoot)EditorGUILayout.ObjectField(
                "Planet Root",
                planetRoot,
                typeof(ProceduralPlanetRoot),
                true);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create / Find Planet Root", GUILayout.Height(30)))
                    CreateOrFindPlanetRoot();

                GUI.enabled = planetRoot != null;
                if (GUILayout.Button("Frame Planet", GUILayout.Height(30)))
                    FramePlanet();
                GUI.enabled = true;
            }

            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = settings != null;

                if (GUILayout.Button("Generate Planet", GUILayout.Height(38)))
                {
                    CreateOrFindPlanetRoot();
                    planetRoot.Settings = settings;
                    planetRoot.Generate();
                    Selection.activeGameObject = planetRoot.gameObject;
                }

                GUI.enabled = planetRoot != null;

                if (GUILayout.Button("Clear Planet", GUILayout.Height(38)))
                    planetRoot.ClearGeneratedFaces();

                GUI.enabled = true;
            }

            EditorGUILayout.Space(14);
            EditorGUILayout.LabelField("Phase Progress", EditorStyles.boldLabel);

            DrawStatus("Phase 0 — Foundation", true);
            DrawStatus("Phase 1 — Cube-Sphere", planetRoot != null && planetRoot.transform.childCount == 6);
            DrawStatus("Phase 2 — Fly Camera + Streaming", false);

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "Phase 1 is greybox only. Terrain shaping, stylized biomes, forests and Albion-like readability arrive in later approved phases.",
                MessageType.None);
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("PROCEDURAL PLANET — WORLD CREATOR", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"Generator Version: {WorldGenerationVersion.Current}",
                EditorStyles.miniLabel);
        }

        private void DrawSettingsInspector()
        {
            if (settingsEditor == null || settingsEditor.target != settings)
            {
                if (settingsEditor != null)
                    DestroyImmediate(settingsEditor);

                settingsEditor = UnityEditor.Editor.CreateEditor(settings);
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            settingsEditor.OnInspectorGUI();
            EditorGUILayout.EndVertical();
        }

        private void CreateOrFindPlanetRoot()
        {
            if (planetRoot == null)
                planetRoot = FindFirstObjectByType<ProceduralPlanetRoot>();

            if (planetRoot == null)
            {
                var go = new GameObject("Procedural Planet");
                Undo.RegisterCreatedObjectUndo(go, "Create Procedural Planet");
                planetRoot = go.AddComponent<ProceduralPlanetRoot>();
            }

            if (settings != null)
                planetRoot.Settings = settings;

            Selection.activeGameObject = planetRoot.gameObject;
        }

        private void FramePlanet()
        {
            if (planetRoot == null)
                return;

            Selection.activeGameObject = planetRoot.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        private static void DrawStatus(string label, bool complete)
        {
            EditorGUILayout.LabelField($"{(complete ? "✓" : "○")} {label}");
        }

        private void CreateSettingsAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Planet Settings",
                "PlanetSettings",
                "asset",
                "Choose where to save PlanetSettings.");

            if (string.IsNullOrEmpty(path))
                return;

            var asset = CreateInstance<PlanetSettings>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            settings = asset;
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void OnDisable()
        {
            if (settingsEditor != null)
            {
                DestroyImmediate(settingsEditor);
                settingsEditor = null;
            }
        }
    }
}
#endif
