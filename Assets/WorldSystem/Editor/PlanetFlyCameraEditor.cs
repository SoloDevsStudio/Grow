#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProceduralPlanet.Editor
{
    [CustomEditor(typeof(PlanetFlyCamera))]
    public class PlanetFlyCameraEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            PlanetFlyCamera controller =
                (PlanetFlyCamera)target;

            EditorGUILayout.LabelField(
                "Runtime Navigation Mode",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                    "FREE CAMERA",
                    GUILayout.Height(34)))
                {
                    Undo.RecordObject(
                        controller,
                        "Set Free Camera Mode");

                    controller.NavigationMode =
                        PlanetCameraNavigationMode.FreeFly;

                    EditorUtility.SetDirty(controller);
                }

                if (GUILayout.Button(
                    "FIRST PERSON",
                    GUILayout.Height(34)))
                {
                    Undo.RecordObject(
                        controller,
                        "Set First Person Planet Mode");

                    controller.NavigationMode =
                        PlanetCameraNavigationMode.FirstPersonPlanet;

                    EditorUtility.SetDirty(controller);
                }
            }

            EditorGUILayout.Space(6);

            if (controller.NavigationMode ==
                PlanetCameraNavigationMode.FreeFly)
            {
                EditorGUILayout.HelpBox(
                    "FREE CAMERA: flies around the world but is prevented from entering the base planet. Streamed MeshColliders provide real terrain collision when enabled.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "FIRST PERSON: movement follows the local tangent of the planet and gravity always points toward the planet center.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(8);

            DrawDefaultInspector();
        }
    }
}
#endif
