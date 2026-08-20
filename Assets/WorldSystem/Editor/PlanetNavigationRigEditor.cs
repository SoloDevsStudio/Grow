#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProceduralPlanet.Editor
{
    [CustomEditor(typeof(PlanetNavigationRig))]
    public class PlanetNavigationRigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            PlanetNavigationRig rig =
                (PlanetNavigationRig)target;

            EditorGUILayout.LabelField(
                "Navigation Mode",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("FREE CAMERA", GUILayout.Height(36)))
                {
                    Undo.RecordObject(rig, "Set Free Camera");
                    rig.NavigationMode = PlanetNavigationMode.FreeCamera;
                    EditorUtility.SetDirty(rig);
                }

                if (GUILayout.Button("FIRST PERSON", GUILayout.Height(36)))
                {
                    Undo.RecordObject(rig, "Set First Person");
                    rig.NavigationMode = PlanetNavigationMode.FirstPersonPlanet;
                    EditorUtility.SetDirty(rig);
                }
            }

            if (rig.NavigationMode == PlanetNavigationMode.FirstPersonPlanet)
            {
                if (GUILayout.Button("Snap To Planet Surface", GUILayout.Height(28)))
                {
                    Undo.RecordObject(rig.transform, "Snap To Planet Surface");
                    rig.SnapToSurface();
                    EditorUtility.SetDirty(rig.transform);
                }
            }

            EditorGUILayout.Space(8);
            DrawDefaultInspector();
        }
    }
}
#endif
