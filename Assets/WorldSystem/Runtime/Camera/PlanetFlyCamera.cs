using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProceduralPlanet
{
    public enum PlanetCameraNavigationMode
    {
        FreeFly,
        FirstPersonPlanet
    }

    [RequireComponent(typeof(CharacterController))]
    public class PlanetFlyCamera : MonoBehaviour
    {
        [SerializeField] private PlanetSettings settings;
        [SerializeField] private PlanetCameraNavigationMode navigationMode =
            PlanetCameraNavigationMode.FreeFly;

        [Header("Planet Reference")]
        [Tooltip("If empty, the controller finds ProceduralPlanetRoot automatically.")]
        [SerializeField] private Transform planetCenter;

        [Header("Shared Movement")]
        [SerializeField] private bool useSettingsValues = true;

        [Min(1f)]
        [SerializeField] private float moveSpeed = 80f;

        [Min(1f)]
        [SerializeField] private float boostMultiplier = 4f;

        [Range(0.01f, 1f)]
        [SerializeField] private float lookSensitivity = 0.12f;

        [Min(0.01f)]
        [SerializeField] private float panSensitivity = 0.08f;

        [Min(0.01f)]
        [SerializeField] private float wheelMoveSensitivity = 0.12f;

        [Header("Planet Collision")]
        [Tooltip("Minimum clearance above the mathematical base sphere.")]
        [Min(0.01f)]
        [SerializeField] private float freeCameraSurfaceClearance = 1.5f;

        [Header("First Person")]
        [Min(0.1f)]
        [SerializeField] private float gravityAcceleration = 30f;

        [Min(0.1f)]
        [SerializeField] private float firstPersonSurfaceClearance = 1.8f;

        [Tooltip("How quickly the controller aligns its up direction with the planet.")]
        [Min(0.1f)]
        [SerializeField] private float gravityAlignmentSpeed = 12f;

        [Tooltip("Maximum camera pitch up/down in First Person.")]
        [Range(10f, 89f)]
        [SerializeField] private float firstPersonPitchLimit = 85f;

        private CharacterController characterController;

        private float freeYaw;
        private float freePitch;

        private float localYaw;
        private float localPitch;

        private float verticalVelocity;

        public PlanetSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        public PlanetCameraNavigationMode NavigationMode
        {
            get => navigationMode;
            set
            {
                navigationMode = value;
                verticalVelocity = 0f;
                CaptureRotationForMode();
            }
        }

        public Transform PlanetCenter
        {
            get => planetCenter;
            set => planetCenter = value;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            ConfigureCharacterController();
            FindPlanetIfNeeded();
        }

        private void Start()
        {
            ApplySettings();
            FindPlanetIfNeeded();
            CaptureRotationForMode();
            EnforcePlanetSurfaceClearance(
                navigationMode == PlanetCameraNavigationMode.FreeFly
                    ? freeCameraSurfaceClearance
                    : firstPersonSurfaceClearance);
        }

        private void Update()
        {
            ApplySettings();
            FindPlanetIfNeeded();

            if (planetCenter == null || settings == null)
                return;

            if (navigationMode == PlanetCameraNavigationMode.FreeFly)
                UpdateFreeFly();
            else
                UpdateFirstPersonPlanet();
        }

        private void UpdateFreeFly()
        {
            float boost = BoostHeld() ? boostMultiplier : 1f;
            float frameSpeed = moveSpeed * boost * Time.unscaledDeltaTime;

            HandleFreeLook();
            HandleFreePan(boost);

            Vector3 input = ReadMovement();

            Vector3 move =
                transform.right * input.x +
                transform.up * input.y +
                transform.forward * input.z;

            if (move.sqrMagnitude > 1f)
                move.Normalize();

            MoveWithCollision(move * frameSpeed);

            float scroll = ReadScroll();

            if (Mathf.Abs(scroll) > 0.001f)
            {
                float amount =
                    scroll *
                    moveSpeed *
                    wheelMoveSensitivity *
                    boost;

                MoveWithCollision(transform.forward * amount);
            }

            EnforcePlanetSurfaceClearance(freeCameraSurfaceClearance);
        }

        private void UpdateFirstPersonPlanet()
        {
            Vector3 center = planetCenter.position;
            Vector3 radialUp = (transform.position - center).normalized;

            if (radialUp.sqrMagnitude < 0.5f)
                radialUp = Vector3.up;

            AlignToPlanet(radialUp);
            HandlePlanetLook(radialUp);

            float boost = BoostHeld() ? boostMultiplier : 1f;
            Vector3 input = ReadMovement();

            // First-person mode ignores explicit Q/E vertical flight.
            Vector3 tangentForward =
                Vector3.ProjectOnPlane(transform.forward, radialUp).normalized;

            if (tangentForward.sqrMagnitude < 0.001f)
                tangentForward =
                    Vector3.ProjectOnPlane(transform.up, radialUp).normalized;

            Vector3 tangentRight =
                Vector3.Cross(radialUp, tangentForward).normalized;

            Vector3 tangentMove =
                tangentRight * input.x +
                tangentForward * input.z;

            if (tangentMove.sqrMagnitude > 1f)
                tangentMove.Normalize();

            float horizontalSpeed =
                moveSpeed * boost * Time.unscaledDeltaTime;

            bool grounded =
                characterController != null &&
                characterController.isGrounded;

            if (grounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity -= gravityAcceleration * Time.unscaledDeltaTime;

            Vector3 gravityMove =
                -radialUp *
                verticalVelocity *
                -Time.unscaledDeltaTime;

            Vector3 totalMove =
                tangentMove * horizontalSpeed +
                gravityMove;

            MoveWithCollision(totalMove);

            EnforcePlanetSurfaceClearance(firstPersonSurfaceClearance);
        }

        private void HandleFreeLook()
        {
            if (!RightMouseHeld())
                return;

            Vector2 mouseDelta = ReadMouseDelta();

            freeYaw += mouseDelta.x * lookSensitivity;
            freePitch -= mouseDelta.y * lookSensitivity;
            freePitch = Mathf.Clamp(freePitch, -89f, 89f);

            transform.rotation =
                Quaternion.Euler(freePitch, freeYaw, 0f);
        }

        private void HandleFreePan(float boost)
        {
            if (!MiddleMouseHeld())
                return;

            Vector2 mouseDelta = ReadMouseDelta();

            Vector3 pan =
                (-transform.right * mouseDelta.x +
                  transform.up * mouseDelta.y)
                * panSensitivity
                * boost;

            MoveWithCollision(pan);
        }

        private void HandlePlanetLook(Vector3 radialUp)
        {
            if (RightMouseHeld())
            {
                Vector2 mouseDelta = ReadMouseDelta();

                localYaw += mouseDelta.x * lookSensitivity;
                localPitch -= mouseDelta.y * lookSensitivity;

                localPitch = Mathf.Clamp(
                    localPitch,
                    -firstPersonPitchLimit,
                    firstPersonPitchLimit);
            }

            Vector3 baseForward =
                Vector3.ProjectOnPlane(transform.forward, radialUp).normalized;

            if (baseForward.sqrMagnitude < 0.001f)
                baseForward =
                    Vector3.ProjectOnPlane(Vector3.forward, radialUp).normalized;

            if (baseForward.sqrMagnitude < 0.001f)
                baseForward =
                    Vector3.ProjectOnPlane(Vector3.right, radialUp).normalized;

            Quaternion yawRotation =
                Quaternion.AngleAxis(localYaw, radialUp);

            Vector3 yawForward =
                yawRotation * baseForward;

            Vector3 right =
                Vector3.Cross(radialUp, yawForward).normalized;

            Quaternion pitchRotation =
                Quaternion.AngleAxis(localPitch, right);

            Vector3 lookForward =
                pitchRotation * yawForward;

            transform.rotation =
                Quaternion.LookRotation(lookForward, radialUp);

            // The yaw is consumed into orientation so it does not accumulate
            // relative to a changing tangent frame.
            localYaw = 0f;
        }

        private void AlignToPlanet(Vector3 radialUp)
        {
            Vector3 forward =
                Vector3.ProjectOnPlane(transform.forward, radialUp).normalized;

            if (forward.sqrMagnitude < 0.001f)
                forward =
                    Vector3.ProjectOnPlane(Vector3.forward, radialUp).normalized;

            if (forward.sqrMagnitude < 0.001f)
                forward =
                    Vector3.ProjectOnPlane(Vector3.right, radialUp).normalized;

            Quaternion target =
                Quaternion.LookRotation(forward, radialUp);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    target,
                    1f - Mathf.Exp(
                        -gravityAlignmentSpeed *
                        Time.unscaledDeltaTime));
        }

        private void MoveWithCollision(Vector3 motion)
        {
            if (characterController != null &&
                characterController.enabled)
            {
                characterController.Move(motion);
            }
            else
            {
                transform.position += motion;
            }
        }

        private void EnforcePlanetSurfaceClearance(float clearance)
        {
            if (planetCenter == null || settings == null)
                return;

            Vector3 center = planetCenter.position;
            Vector3 offset = transform.position - center;

            float distance = offset.magnitude;
            float minimumDistance = settings.Radius + clearance;

            if (distance >= minimumDistance)
                return;

            Vector3 direction =
                distance > 0.0001f
                    ? offset / distance
                    : Vector3.up;

            bool wasEnabled =
                characterController != null &&
                characterController.enabled;

            if (wasEnabled)
                characterController.enabled = false;

            transform.position =
                center + direction * minimumDistance;

            if (wasEnabled)
                characterController.enabled = true;

            verticalVelocity = Mathf.Min(verticalVelocity, 0f);
        }

        private void CaptureRotationForMode()
        {
            if (navigationMode ==
                PlanetCameraNavigationMode.FreeFly)
            {
                Vector3 euler = transform.eulerAngles;
                freeYaw = euler.y;
                freePitch = NormalizePitch(euler.x);
            }
            else
            {
                localYaw = 0f;
                localPitch = 0f;
            }
        }

        private void ConfigureCharacterController()
        {
            if (characterController == null)
                return;

            characterController.radius = 0.35f;
            characterController.height = 1.8f;
            characterController.center = Vector3.zero;
            characterController.stepOffset = 0.3f;
            characterController.skinWidth = 0.05f;
            characterController.minMoveDistance = 0f;
            characterController.slopeLimit = 60f;
        }

        private void FindPlanetIfNeeded()
        {
            if (planetCenter != null)
                return;

            ProceduralPlanetRoot planet =
                FindFirstObjectByType<ProceduralPlanetRoot>();

            if (planet != null)
                planetCenter = planet.transform;
        }

        private void ApplySettings()
        {
            if (!useSettingsValues || settings == null)
                return;

            moveSpeed = settings.FlyMoveSpeed;
            boostMultiplier = settings.FlyBoostMultiplier;
            lookSensitivity = settings.FlyLookSensitivity;
        }

        private static float NormalizePitch(float angle)
        {
            if (angle > 180f)
                angle -= 360f;

            return angle;
        }

        private static Vector3 ReadMovement()
        {
            float x = 0f;
            float y = 0f;
            float z = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
                return Vector3.zero;

            if (Keyboard.current.aKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed) x -= 1f;

            if (Keyboard.current.dKey.isPressed ||
                Keyboard.current.rightArrowKey.isPressed) x += 1f;

            if (Keyboard.current.qKey.isPressed ||
                Keyboard.current.pageDownKey.isPressed) y -= 1f;

            if (Keyboard.current.eKey.isPressed ||
                Keyboard.current.pageUpKey.isPressed) y += 1f;

            if (Keyboard.current.sKey.isPressed ||
                Keyboard.current.downArrowKey.isPressed) z -= 1f;

            if (Keyboard.current.wKey.isPressed ||
                Keyboard.current.upArrowKey.isPressed) z += 1f;
#else
            if (Input.GetKey(KeyCode.A) ||
                Input.GetKey(KeyCode.LeftArrow)) x -= 1f;

            if (Input.GetKey(KeyCode.D) ||
                Input.GetKey(KeyCode.RightArrow)) x += 1f;

            if (Input.GetKey(KeyCode.Q) ||
                Input.GetKey(KeyCode.PageDown)) y -= 1f;

            if (Input.GetKey(KeyCode.E) ||
                Input.GetKey(KeyCode.PageUp)) y += 1f;

            if (Input.GetKey(KeyCode.S) ||
                Input.GetKey(KeyCode.DownArrow)) z -= 1f;

            if (Input.GetKey(KeyCode.W) ||
                Input.GetKey(KeyCode.UpArrow)) z += 1f;
#endif

            return new Vector3(x, y, z);
        }

        private static bool BoostHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null &&
                   (Keyboard.current.leftShiftKey.isPressed ||
                    Keyboard.current.rightShiftKey.isPressed);
#else
            return Input.GetKey(KeyCode.LeftShift) ||
                   Input.GetKey(KeyCode.RightShift);
#endif
        }

        private static bool RightMouseHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null &&
                   Mouse.current.rightButton.isPressed;
#else
            return Input.GetMouseButton(1);
#endif
        }

        private static bool MiddleMouseHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null &&
                   Mouse.current.middleButton.isPressed;
#else
            return Input.GetMouseButton(2);
#endif
        }

        private static Vector2 ReadMouseDelta()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null
                ? Mouse.current.delta.ReadValue()
                : Vector2.zero;
#else
            return new Vector2(
                Input.GetAxisRaw("Mouse X"),
                Input.GetAxisRaw("Mouse Y")) * 10f;
#endif
        }

        private static float ReadScroll()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null)
                return 0f;

            return Mathf.Clamp(
                Mouse.current.scroll.ReadValue().y / 120f,
                -1f,
                1f);
#else
            return Mathf.Clamp(
                Input.mouseScrollDelta.y,
                -1f,
                1f);
#endif
        }
    }
}
