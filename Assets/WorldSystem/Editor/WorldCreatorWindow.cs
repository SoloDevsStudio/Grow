#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProceduralPlanet.Editor
{
    public class WorldCreatorWindow : EditorWindow
    {
        private PlanetSettings settings;
        private UnityEditor.Editor settingsEditor;

        [MenuItem("Tools/Procedural Planet/World Creator")]
        public static void Open()
        {
            var window = GetWindow<WorldCreatorWindow>();
            window.titleContent = new GUIContent("World Creator");
            window.minSize = new Vector2(420f, 500f);
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

            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Settings Asset", GUILayout.Height(30)))
                {
                    CreateSettingsAsset();
                }

                GUI.enabled = settings != null;
                if (GUILayout.Button("Ping Asset", GUILayout.Height(30)))
                {
                    EditorGUIUtility.PingObject(settings);
                    Selection.activeObject = settings;
                }
                GUI.enabled = true;
            }

            EditorGUILayout.Space(12);

            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "Create or assign a PlanetSettings asset. Phase 1 generation controls will appear here later.",
                    MessageType.Info);
                return;
            }

            DrawSettingsInspector();

            EditorGUILayout.Space(16);
            EditorGUILayout.LabelField("Phase 0 Status", EditorStyles.boldLabel);

            DrawStatus("Settings Asset", true);
            DrawStatus("Custom World Creator Window", true);
            DrawStatus("Planet Mesh Generator", false);
            DrawStatus("Streaming", false);

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Phase 0 intentionally does not generate terrain. Approve the foundation first, then Phase 1 adds the cube-sphere generator.",
                MessageType.None);
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField(
                "PROCEDURAL PLANET — WORLD CREATOR",
                EditorStyles.boldLabel);

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

        private static void DrawStatus(string label, bool complete)
        {
            var icon = complete ? "✓" : "○";
            EditorGUILayout.LabelField($"{icon} {label}");
        }

        private void CreateSettingsAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Planet Settings",
                "PlanetSettings",
                "asset",
                "Choose where to save the PlanetSettings asset.");

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
