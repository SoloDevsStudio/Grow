#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProceduralPlanet.Editor
{
    [InitializeOnLoad]
    public static class SceneViewFlyNavigator
    {
        private const string EnabledKey = "ProceduralPlanet.SceneFly.Enabled";
        private const string SpeedKey = "ProceduralPlanet.SceneFly.Speed";
        private const string BoostKey = "ProceduralPlanet.SceneFly.Boost";

        static SceneViewFlyNavigator()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        public static float Speed
        {
            get => EditorPrefs.GetFloat(SpeedKey, 20f);
            set => EditorPrefs.SetFloat(SpeedKey, Mathf.Max(0.1f, value));
        }

        public static float Boost
        {
            get => EditorPrefs.GetFloat(BoostKey, 4f);
            set => EditorPrefs.SetFloat(BoostKey, Mathf.Max(1f, value));
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!Enabled || sceneView == null || sceneView.camera == null)
                return;

            Event e = Event.current;
            if (e == null || !(EditorWindow.focusedWindow is SceneView))
                return;

            Transform cam = sceneView.camera.transform;
            float multiplier = e.shift ? Boost : 1f;

            if (e.type == EventType.KeyDown)
            {
                Vector3 move = Vector3.zero;

                if (e.keyCode == KeyCode.UpArrow) move += cam.forward;
                if (e.keyCode == KeyCode.DownArrow) move -= cam.forward;
                if (e.keyCode == KeyCode.LeftArrow) move -= cam.right;
                if (e.keyCode == KeyCode.RightArrow) move += cam.right;
                if (e.keyCode == KeyCode.PageUp) move += cam.up;
                if (e.keyCode == KeyCode.PageDown) move -= cam.up;

                if (move.sqrMagnitude > 0f)
                {
                    sceneView.pivot += move.normalized * Speed * 0.15f * multiplier;
                    e.Use();
                    sceneView.Repaint();
                    return;
                }
            }

            if (e.type == EventType.ScrollWheel)
            {
                float amount = -e.delta.y * Speed * 0.10f * multiplier;
                sceneView.pivot += cam.forward * amount;
                e.Use();
                sceneView.Repaint();
            }
        }
    }
}
#endif
