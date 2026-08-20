#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProceduralPlanet.Editor
{
    public static class PlanetNavigationRigFactory
    {
        public static PlanetNavigationRig CreateOrReset(
            PlanetSettings settings,
            ProceduralPlanetRoot planet)
        {
            PlanetNavigationRig rig =
                Object.FindFirstObjectByType<PlanetNavigationRig>();

            if (rig == null)
            {
                GameObject root = new GameObject("Planet Navigation Rig");
                Undo.RegisterCreatedObjectUndo(root, "Create Planet Navigation Rig");

                root.AddComponent<CharacterController>();
                rig = root.AddComponent<PlanetNavigationRig>();

                GameObject pivot = new GameObject("Camera Pivot");
                pivot.transform.SetParent(root.transform, false);

                GameObject cameraGO = new GameObject("Main Camera");
                cameraGO.transform.SetParent(pivot.transform, false);
                cameraGO.tag = "MainCamera";

                Camera cam = cameraGO.AddComponent<Camera>();
                cameraGO.AddComponent<AudioListener>();

                rig.ViewCamera = cam;
                rig.CameraPivot = pivot.transform;
            }

            if (settings != null)
                rig.Settings = settings;

            if (planet != null)
                rig.PlanetCenter = planet.transform;

            Camera viewCamera = rig.GetComponentInChildren<Camera>(true);

            if (viewCamera != null && settings != null)
            {
                PositionRigForPlanetFit(rig.transform, viewCamera, settings);

                if (planet != null && planet.Streamer != null)
                    planet.Streamer.TargetCamera = viewCamera;
            }

            Selection.activeGameObject = rig.gameObject;
            return rig;
        }

        public static void PositionRigForPlanetFit(
            Transform rigTransform,
            Camera camera,
            PlanetSettings settings)
        {
            float verticalFovRad = camera.fieldOfView * Mathf.Deg2Rad;
            float halfFov = verticalFovRad * 0.5f;

            float distance =
                (settings.Radius / Mathf.Sin(halfFov)) *
                settings.CameraFitMargin;

            rigTransform.position = new Vector3(0f, 0f, -distance);
            rigTransform.rotation =
                Quaternion.LookRotation(-rigTransform.position.normalized, Vector3.up);

            camera.nearClipPlane =
                Mathf.Max(0.03f, settings.Radius * 0.0001f);

            camera.farClipPlane =
                Mathf.Max(
                    distance + settings.Radius * 3f,
                    settings.UnloadDistance * 1.25f);
        }
    }
}
#endif
