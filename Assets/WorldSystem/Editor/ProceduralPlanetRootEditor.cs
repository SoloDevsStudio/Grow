#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProceduralPlanet.Editor
{
    [CustomEditor(typeof(ProceduralPlanetRoot))]
    public class ProceduralPlanetRootEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var planet = (ProceduralPlanetRoot)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Phase 1 Controls", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = planet.Settings != null;

                if (GUILayout.Button("Generate / Regenerate", GUILayout.Height(32)))
                    planet.Generate();

                if (GUILayout.Button("Clear", GUILayout.Height(32)))
                    planet.ClearGeneratedFaces();

                GUI.enabled = true;
            }

            if (planet.Settings == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a PlanetSettings asset before generating.",
                    MessageType.Warning);
            }
        }
    }
}
#endif
