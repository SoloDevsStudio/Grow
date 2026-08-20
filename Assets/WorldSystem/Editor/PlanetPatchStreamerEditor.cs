#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProceduralPlanet.Editor
{
    [CustomEditor(typeof(PlanetPatchStreamer))]
    public class PlanetPatchStreamerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            PlanetPatchStreamer streamer =
                (PlanetPatchStreamer)target;

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField(
                "QUADTREE MONITOR",
                EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField(
                    "Root Nodes",
                    streamer.RootNodeCount);

                EditorGUILayout.IntField(
                    "Evaluated Nodes",
                    streamer.EvaluatedNodeCount);

                EditorGUILayout.IntField(
                    "Desired Leaf Nodes",
                    streamer.DesiredLeafCount);

                EditorGUILayout.IntField(
                    "Active Leaf Meshes",
                    streamer.ActiveLeafCount);

                EditorGUILayout.IntField(
                    "Visible Renderers",
                    streamer.VisibleRendererCount);

                EditorGUILayout.IntField(
                    "Highest Active LOD",
                    streamer.HighestActiveLod);
            }

            EditorGUILayout.Space(8);

            if (GUILayout.Button(
                    "Rebuild Quadtree Streaming"))
            {
                streamer.RebuildStreamingState();
            }

            if (GUILayout.Button(
                    "Clear Active Leaf Meshes"))
            {
                streamer.ClearAllPatches();
            }
        }
    }
}
#endif
