using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProceduralPlanet
{
    public enum PlanetNavigationMode
    {
        FreeCamera,
        FirstPersonPlanet
    }

    [RequireComponent(typeof(CharacterController))]
    public class PlanetNavigationRig : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlanetSettings settings;
        [SerializeField] private Transform planetCenter;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform cameraPivot;

        [Header("Mode")]
        [SerializeField] private PlanetNavigationMode navigationMode =
            PlanetNavigationMode.FreeCamera;

        [Header("Free Camera")]
        [SerializeField] private float freeMoveSpeed = 80f;
        [SerializeField] private float freeBoostMultiplier = 4f;
        [SerializeField] private float freeLookSensitivity = 0.12f;
        [SerializeField] private float freePanSensitivity = 0.08f;
        [SerializeField] private float freeWheelSensitivity = 0.12f;
        [SerializeField] private float freeSurfaceClearance = 1.5f;

        [Header("First Person")]
        [SerializeField] private float walkSpeed = 6f;
        [SerializeField] private float runMultiplier = 1.7f;
        [SerializeField] private float mouseSensitivity = 0.10f;
        [SerializeField] private float gravityAcceleration = 35f;
        [SerializeField] private float alignmentSpeed = 18f;
        [SerializeField] private float eyeHeight = 1.62f;
        [SerializeField] private float controllerHeight = 1.8f;
        [SerializeField] private float controllerRadius = 0.35f;
        [SerializeField] private float groundProbeDistance = 5f;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private bool lockCursorInFirstPerson = true;

        private CharacterController controller;

        private float freeYaw;
        private float freePitch;
        private float firstPersonPitch;
        private float radialVelocity;

        private PlanetNavigationMode previousMode;

        public PlanetSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        public Transform PlanetCenter
        {
            get => planetCenter;
            set => planetCenter = value;
        }

        public Camera ViewCamera
        {
            get => viewCamera;
            set => viewCamera = value;
        }

        public Transform CameraPivot
        {
            get => cameraPivot;
            set => cameraPivot = value;
        }

        public PlanetNavigationMode NavigationMode
        {
            get => navigationMode;
            set
            {
                if (navigationMode == value)
                    return;

                navigationMode = value;

                if (Application.isPlaying)
                    EnterMode(value);
            }
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            FindReferencesIfNeeded();
            ConfigureController();
        }

        private void Start()
        {
            FindReferencesIfNeeded();
            ConfigureController();

            previousMode = navigationMode;
            EnterMode(navigationMode);
        }

        private void Update()
        {
            FindReferencesIfNeeded();

            if (settings == null || planetCenter == null)
                return;

            if (previousMode != navigationMode)
            {
                previousMode = navigationMode;
                EnterMode(navigationMode);
            }

            if (navigationMode == PlanetNavigationMode.FreeCamera)
                UpdateFreeCamera();
            else
                UpdateFirstPerson();
        }

        private void EnterMode(PlanetNavigationMode mode)
        {
            radialVelocity = 0f;

            if (mode == PlanetNavigationMode.FreeCamera)
            {
                Vector3 euler = transform.eulerAngles;
                freeYaw = euler.y;
                freePitch = NormalizeAngle(euler.x);

                if (cameraPivot != null)
                    cameraPivot.localRotation = Quaternion.identity;

                SetCursorLocked(false);
            }
            else
            {
                SnapToSurface();
                AlignBodyImmediately();

                firstPersonPitch = 0f;

                if (cameraPivot != null)
                {
                    cameraPivot.localPosition = new Vector3(0f, eyeHeight, 0f);
                    cameraPivot.localRotation = Quaternion.identity;
                }

                SetCursorLocked(lockCursorInFirstPerson);
            }
        }

        private void UpdateFreeCamera()
        {
            float boost = BoostHeld() ? freeBoostMultiplier : 1f;
            float dt = Time.unscaledDeltaTime;

            if (RightMouseHeld())
            {
                Vector2 delta = ReadMouseDelta();
                freeYaw += delta.x * freeLookSensitivity;
                freePitch -= delta.y * freeLookSensitivity;
                freePitch = Mathf.Clamp(freePitch, -89f, 89f);
                transform.rotation = Quaternion.Euler(freePitch, freeYaw, 0f);
            }

            Vector3 input = ReadMovement(includeVertical: true);

            Vector3 move =
                transform.right * input.x +
                transform.up * input.y +
                transform.forward * input.z;

            if (move.sqrMagnitude > 1f)
                move.Normalize();

            controller.Move(move * freeMoveSpeed * boost * dt);

            if (MiddleMouseHeld())
            {
                Vector2 delta = ReadMouseDelta();

                Vector3 pan =
                    (-transform.right * delta.x +
                      transform.up * delta.y)
                    * freePanSensitivity
                    * boost;

                controller.Move(pan);
            }

            float scroll = ReadScroll();

            if (Mathf.Abs(scroll) > 0.001f)
            {
                controller.Move(
                    transform.forward *
                    scroll *
                    freeMoveSpeed *
                    freeWheelSensitivity *
                    boost);
            }

            EnforceBaseSphereClearance(freeSurfaceClearance);
        }

        private void UpdateFirstPerson()
        {
            HandleFirstPersonCursor();

            Vector3 radialUp = GetRadialUp();

            AlignBodySmooth(radialUp);

            if (CursorIsCaptured())
                HandleFirstPersonLook(radialUp);

            Vector3 input = ReadMovement(includeVertical: false);

            Vector3 forward =
                Vector3.ProjectOnPlane(transform.forward, radialUp).normalized;

            Vector3 right =
                Vector3.Cross(radialUp, forward).normalized;

            Vector3 tangentMove =
                forward * input.z +
                right * input.x;

            if (tangentMove.sqrMagnitude > 1f)
                tangentMove.Normalize();

            float speed =
                walkSpeed *
                (BoostHeld() ? runMultiplier : 1f);

            bool grounded = IsGrounded(radialUp);

            if (grounded && radialVelocity < 0f)
                radialVelocity = -2f;
            else
                radialVelocity -= gravityAcceleration * Time.deltaTime;

            Vector3 motion =
                tangentMove * speed +
                radialUp * radialVelocity;

            controller.Move(motion * Time.deltaTime);

            KeepOutsideBaseSphere();
        }

        private void HandleFirstPersonLook(Vector3 radialUp)
        {
            Vector2 delta = ReadMouseDelta();

            float yaw = delta.x * mouseSensitivity;
            firstPersonPitch -= delta.y * mouseSensitivity;
            firstPersonPitch = Mathf.Clamp(firstPersonPitch, -85f, 85f);

            Quaternion yawRotation = Quaternion.AngleAxis(yaw, radialUp);
            transform.rotation = yawRotation * transform.rotation;

            if (cameraPivot != null)
                cameraPivot.localRotation =
                    Quaternion.Euler(firstPersonPitch, 0f, 0f);
        }

        private void AlignBodySmooth(Vector3 radialUp)
        {
            Vector3 tangentForward =
                Vector3.ProjectOnPlane(transform.forward, radialUp).normalized;

            if (tangentForward.sqrMagnitude < 0.001f)
                tangentForward =
                    Vector3.ProjectOnPlane(Vector3.forward, radialUp).normalized;

            if (tangentForward.sqrMagnitude < 0.001f)
                tangentForward =
                    Vector3.ProjectOnPlane(Vector3.right, radialUp).normalized;

            Quaternion target =
                Quaternion.LookRotation(tangentForward, radialUp);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    target,
                    1f - Mathf.Exp(-alignmentSpeed * Time.deltaTime));
        }

        private void AlignBodyImmediately()
        {
            Vector3 radialUp = GetRadialUp();

            Vector3 tangentForward =
                Vector3.ProjectOnPlane(transform.forward, radialUp).normalized;

            if (tangentForward.sqrMagnitude < 0.001f)
                tangentForward =
                    Vector3.ProjectOnPlane(Vector3.forward, radialUp).normalized;

            if (tangentForward.sqrMagnitude < 0.001f)
                tangentForward =
                    Vector3.ProjectOnPlane(Vector3.right, radialUp).normalized;

            transform.rotation =
                Quaternion.LookRotation(tangentForward, radialUp);
        }

        private bool IsGrounded(Vector3 radialUp)
        {
            if (controller.isGrounded)
                return true;

            Vector3 origin =
                transform.position +
                radialUp * 0.15f;

            float probe =
                controllerHeight * 0.5f +
                groundProbeDistance;

            if (Physics.SphereCast(
                origin,
                controllerRadius * 0.8f,
                -radialUp,
                out RaycastHit hit,
                probe,
                groundMask,
                QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == transform ||
                    hit.transform.IsChildOf(transform))
                    return false;

                return true;
            }

            return false;
        }

        public void SnapToSurface()
        {
            if (planetCenter == null || settings == null)
                return;

            Vector3 radialUp = GetRadialUp();

            Vector3 rayOrigin =
                planetCenter.position +
                radialUp *
                (settings.Radius +
                 Mathf.Max(50f, groundProbeDistance * 4f));

            bool hitGround = Physics.Raycast(
                rayOrigin,
                -radialUp,
                out RaycastHit hit,
                Mathf.Infinity,
                groundMask,
                QueryTriggerInteraction.Ignore);

            float bodyHalfHeight =
                Mathf.Max(controllerHeight * 0.5f, controllerRadius);

            Vector3 targetPosition;

            if (hitGround &&
                hit.transform != transform &&
                !hit.transform.IsChildOf(transform))
            {
                targetPosition =
                    hit.point +
                    radialUp *
                    (bodyHalfHeight + controller.skinWidth);
            }
            else
            {
                targetPosition =
                    planetCenter.position +
                    radialUp *
                    (settings.Radius +
                     bodyHalfHeight +
                     controller.skinWidth);
            }

            bool enabled = controller.enabled;
            controller.enabled = false;
            transform.position = targetPosition;
            controller.enabled = enabled;
        }

        private void KeepOutsideBaseSphere()
        {
            float minimumRadius =
                settings.Radius +
                controllerHeight * 0.5f +
                controller.skinWidth;

            Vector3 offset =
                transform.position - planetCenter.position;

            float distance = offset.magnitude;

            if (distance >= minimumRadius)
                return;

            Vector3 direction =
                distance > 0.0001f
                    ? offset / distance
                    : Vector3.up;

            bool enabled = controller.enabled;
            controller.enabled = false;
            transform.position =
                planetCenter.position +
                direction * minimumRadius;
            controller.enabled = enabled;

            radialVelocity = Mathf.Min(radialVelocity, 0f);
        }

        private void EnforceBaseSphereClearance(float clearance)
        {
            Vector3 offset =
                transform.position - planetCenter.position;

            float distance = offset.magnitude;
            float minimum = settings.Radius + clearance;

            if (distance >= minimum)
                return;

            Vector3 direction =
                distance > 0.0001f
                    ? offset / distance
                    : Vector3.up;

            bool enabled = controller.enabled;
            controller.enabled = false;
            transform.position =
                planetCenter.position +
                direction * minimum;
            controller.enabled = enabled;
        }

        private Vector3 GetRadialUp()
        {
            Vector3 up =
                transform.position - planetCenter.position;

            return up.sqrMagnitude > 0.0001f
                ? up.normalized
                : Vector3.up;
        }

        private void ConfigureController()
        {
            controller.height = controllerHeight;
            controller.radius = controllerRadius;
            controller.center =
                new Vector3(0f, controllerHeight * 0.5f, 0f);

            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.05f;
            controller.minMoveDistance = 0f;
            controller.slopeLimit = 60f;

            if (cameraPivot != null)
                cameraPivot.localPosition =
                    new Vector3(0f, eyeHeight, 0f);
        }

        private void FindReferencesIfNeeded()
        {
            if (controller == null)
                controller = GetComponent<CharacterController>();

            if (planetCenter == null)
            {
                ProceduralPlanetRoot planet =
                    FindFirstObjectByType<ProceduralPlanetRoot>();

                if (planet != null)
                    planetCenter = planet.transform;
            }

            if (viewCamera == null)
                viewCamera = GetComponentInChildren<Camera>(true);

            if (viewCamera != null && cameraPivot == null)
                cameraPivot = viewCamera.transform.parent;
        }

        private void HandleFirstPersonCursor()
        {
            if (!lockCursorInFirstPerson)
                return;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(false);
            }

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame &&
                !CursorIsCaptured())
            {
                SetCursorLocked(true);
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape))
                SetCursorLocked(false);

            if (Input.GetMouseButtonDown(0) && !CursorIsCaptured())
                SetCursorLocked(true);
#endif
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState =
                locked
                    ? CursorLockMode.Locked
                    : CursorLockMode.None;

            Cursor.visible = !locked;
        }

        private static bool CursorIsCaptured()
        {
            return Cursor.lockState == CursorLockMode.Locked;
        }

        private static float NormalizeAngle(float angle)
        {
            if (angle > 180f)
                angle -= 360f;

            return angle;
        }

        private static Vector3 ReadMovement(bool includeVertical)
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

            if (Keyboard.current.sKey.isPressed ||
                Keyboard.current.downArrowKey.isPressed) z -= 1f;

            if (Keyboard.current.wKey.isPressed ||
                Keyboard.current.upArrowKey.isPressed) z += 1f;

            if (includeVertical)
            {
                if (Keyboard.current.qKey.isPressed ||
                    Keyboard.current.pageDownKey.isPressed) y -= 1f;

                if (Keyboard.current.eKey.isPressed ||
                    Keyboard.current.pageUpKey.isPressed) y += 1f;
            }
#else
            if (Input.GetKey(KeyCode.A) ||
                Input.GetKey(KeyCode.LeftArrow)) x -= 1f;

            if (Input.GetKey(KeyCode.D) ||
                Input.GetKey(KeyCode.RightArrow)) x += 1f;

            if (Input.GetKey(KeyCode.S) ||
                Input.GetKey(KeyCode.DownArrow)) z -= 1f;

            if (Input.GetKey(KeyCode.W) ||
                Input.GetKey(KeyCode.UpArrow)) z += 1f;

            if (includeVertical)
            {
                if (Input.GetKey(KeyCode.Q) ||
                    Input.GetKey(KeyCode.PageDown)) y -= 1f;

                if (Input.GetKey(KeyCode.E) ||
                    Input.GetKey(KeyCode.PageUp)) y += 1f;
            }
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
