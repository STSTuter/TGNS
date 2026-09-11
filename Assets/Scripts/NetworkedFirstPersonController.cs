using JohnStairs.RPG.Character.Motor;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owner-authoritative first-person driver for the John Stairs RPGMotor/AnimationHandler.
/// Only the owning client reads mouse/keyboard input and runs RPGMotor's Update loop;
/// everyone else sees the result through NetworkTransform (position/rotation) and
/// NetworkAnimator (animation parameters), same replication pattern as PlayerMovement.
/// The package's RPGController/RPGCamera/InputHandler/CursorHandler/SceneManager are not
/// used here - none of them have a local-player concept and they assume a single Camera.main.
/// </summary>
[RequireComponent(typeof(RPGMotor))]
public class NetworkedFirstPersonController : NetworkBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera fpsCamera;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("First-person view")]
    [Tooltip("Renderers hidden for the owner only (e.g. the head mesh) to avoid camera clipping into the character's own model.")]
    [SerializeField] private Renderer[] firstPersonHiddenRenderers = System.Array.Empty<Renderer>();

    [Header("Body facing")]
    [Tooltip("SmoothDamp time for rotating the body toward its target facing. Higher = slower, no snapping.")]
    [SerializeField] private float bodyRotationSmoothTime = 0.15f;
    [Tooltip("Once the camera yaw is more than this far from the body, the torso is pulled toward the camera so the head stays under its own limit.")]
    [SerializeField] private float beginTorsoCorrection = 75f;
    [Tooltip("Minimum planar input magnitude before the body re-aims toward the movement direction.")]
    [SerializeField] private float moveFacingThreshold = 0.1f;

    private RPGMotor _motor;
    private float _yaw;
    private float _pitch;
    private float _bodyYaw;
    private float _bodyYawVel;
    private bool _cursorLocked;

    /// <summary>Owner-only camera yaw in degrees (world space). Consumed by HumanoidHeadLook.</summary>
    public float CameraYaw => _yaw;
    /// <summary>Owner-only camera pitch in degrees. Consumed by HumanoidHeadLook.</summary>
    public float CameraPitch => _pitch;

    private void Awake()
    {
        _motor = GetComponent<RPGMotor>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            if (PlayerSpawnPoints.Instance != null)
            {
                transform.position = PlayerSpawnPoints.Instance.GetSpawnPosition(OwnerClientId);
            }

            _yaw = transform.eulerAngles.y;
            _pitch = 0f;
            _bodyYaw = transform.eulerAngles.y;

            if (fpsCamera != null)
            {
                fpsCamera.gameObject.SetActive(true);
            }

            foreach (Renderer renderer in firstPersonHiddenRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }

            SetCursorLocked(true);
        }
        else
        {
            if (fpsCamera != null)
            {
                fpsCamera.gameObject.SetActive(false);
            }

            // Remote replicas are driven purely by NetworkTransform/NetworkAnimator;
            // running RPGMotor's own Update() here would fight that replicated position.
            _motor.enabled = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            SetCursorLocked(false);
        }
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursorLocked(!_cursorLocked);
        }

        if (_cursorLocked)
        {
            HandleLook();
            HandleMovement();
        }
    }

    private void HandleLook()
    {
        Vector2 delta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;

        _yaw += delta.x * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch - delta.y * mouseSensitivity, minPitch, maxPitch);

        if (cameraPivot != null)
        {
            // The camera now yaws independently of the body: the pivot carries the yaw offset
            // between the body facing and the look direction, plus the pitch.
            float relativeYaw = Mathf.DeltaAngle(transform.eulerAngles.y, _yaw);
            cameraPivot.localRotation = Quaternion.Euler(_pitch, relativeYaw, 0f);
        }
    }

    /// <summary>
    /// Body facing is decoupled from the camera. When moving, the body smoothly turns toward the
    /// camera-relative movement vector; when stationary it holds its heading. If the camera yaw
    /// runs past <see cref="beginTorsoCorrection"/> the torso is pulled toward the camera so the
    /// head-look component never has to exceed its own yaw limit. Rotation is still handed to
    /// RPGMotor.TurnInDirection so it stays inside the network-authoritative movement flow.
    /// </summary>
    private void UpdateBodyFacing(Vector3 planarMoveDirection)
    {
        float targetYaw = _bodyYaw;

        if (planarMoveDirection.sqrMagnitude > moveFacingThreshold * moveFacingThreshold)
        {
            targetYaw = Mathf.Atan2(planarMoveDirection.x, planarMoveDirection.z) * Mathf.Rad2Deg;
        }

        float headOffset = Mathf.DeltaAngle(targetYaw, _yaw);
        if (Mathf.Abs(headOffset) > beginTorsoCorrection)
        {
            targetYaw = _yaw - Mathf.Sign(headOffset) * beginTorsoCorrection;
        }

        _bodyYaw = Mathf.SmoothDampAngle(_bodyYaw, targetYaw, ref _bodyYawVel, bodyRotationSmoothTime);

        Vector3 bodyForward = Quaternion.Euler(0f, _bodyYaw, 0f) * Vector3.forward;
        _motor.TurnInDirection(bodyForward, Vector3.up);
    }

    private void HandleMovement()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        Vector2 input = Vector2.zero;
        if (keyboard.aKey.isPressed) input.x -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.wKey.isPressed) input.y += 1f;
        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector3 yawForward = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
        Vector3 yawRight = Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;
        Vector3 moveDirection = yawForward * input.y + yawRight * input.x;
        _motor.Move(moveDirection);

        UpdateBodyFacing(moveDirection);

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            _motor.Jump();
        }

        _motor.Sprint(keyboard.leftShiftKey.isPressed);

        if (keyboard.cKey.wasPressedThisFrame)
        {
            _motor.ToggleCrouching();
        }
    }

    private void SetCursorLocked(bool locked)
    {
        _cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
