using System.Collections.Generic;
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
        [SerializeField, HideInInspector] private List<Mesh> generatedMeshes = new List<Mesh>();

        private Material runtimeFallbackMaterial;

        public PlanetSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        public void Generate()
        {
            if (settings == null)
            {
                Debug.LogError("Procedural Planet: No PlanetSettings assigned.", this);
                return;
            }

            ClearGeneratedFaces();

            foreach (PlanetFace face in System.Enum.GetValues(typeof(PlanetFace)))
            {
                CreateFace(face);
            }

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

        public void ClearGeneratedFaces()
        {
            generatedMeshes.RemoveAll(m => m == null);

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(child.gameObject);
                else
                    Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }

            foreach (Mesh mesh in generatedMeshes)
            {
                if (mesh == null) continue;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(mesh);
                else
                    Destroy(mesh);
#else
                Destroy(mesh);
#endif
            }

            generatedMeshes.Clear();
        }

        private void CreateFace(PlanetFace face)
        {
            GameObject child = new GameObject(face.ToString());
            child.transform.SetParent(transform, false);

            var filter = child.AddComponent<MeshFilter>();
            var renderer = child.AddComponent<MeshRenderer>();

            Mesh mesh = PlanetFaceMeshBuilder.Build(
                face,
                settings.BaseResolution,
                settings.Radius);

            generatedMeshes.Add(mesh);
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = settings.PreviewMaterial != null
                ? settings.PreviewMaterial
                : GetFallbackMaterial();

            if (settings.GenerateColliders)
            {
                var collider = child.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
            }
        }

        private Material GetFallbackMaterial()
        {
            if (runtimeFallbackMaterial != null)
                return runtimeFallbackMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            if (shader == null)
            {
                Debug.LogWarning(
                    "Procedural Planet: Could not find URP/Lit or Standard shader. Assign a Preview Material in PlanetSettings.",
                    this);
                return null;
            }

            runtimeFallbackMaterial = new Material(shader)
            {
                name = "Planet_Greybox_Fallback"
            };

            if (runtimeFallbackMaterial.HasProperty("_BaseColor"))
                runtimeFallbackMaterial.SetColor("_BaseColor", new Color(0.52f, 0.54f, 0.55f, 1f));
            else if (runtimeFallbackMaterial.HasProperty("_Color"))
                runtimeFallbackMaterial.SetColor("_Color", new Color(0.52f, 0.54f, 0.55f, 1f));

            return runtimeFallbackMaterial;
        }

        private void OnDestroy()
        {
            if (runtimeFallbackMaterial == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(runtimeFallbackMaterial);
            else
                Destroy(runtimeFallbackMaterial);
#else
            Destroy(runtimeFallbackMaterial);
#endif
        }
    }
}
