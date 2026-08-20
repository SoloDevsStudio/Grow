using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProceduralPlanet
{
    [ExecuteAlways]
    public class ProceduralPlanetRoot : MonoBehaviour
    {
        [SerializeField] private PlanetSettings settings;
        [SerializeField] private PlanetPatchStreamer streamer;

        public PlanetSettings Settings
        {
            get => settings;
            set
            {
                settings = value;

                if (streamer != null)
                    streamer.Settings = value;
            }
        }

        public PlanetPatchStreamer Streamer => streamer;

        public void PreparePhase2Streaming()
        {
            ClearChildren();

            if (streamer == null)
                streamer = GetComponent<PlanetPatchStreamer>();

            if (streamer == null)
                streamer = gameObject.AddComponent<PlanetPatchStreamer>();

            streamer.Settings = settings;

            Camera cam = Camera.main;
            if (cam != null)
                streamer.TargetCamera = cam;

            streamer.RebuildStreamingState();

            if (settings != null)
                name = $"Planet_{settings.WorldName}";

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                EditorUtility.SetDirty(gameObject);
                SceneView.RepaintAll();
            }
#endif
        }

        public void ClearChildren()
        {
            if (streamer != null)
                streamer.ClearAllPatches();

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
    }
}
