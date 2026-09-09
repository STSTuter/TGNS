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

    private RPGMotor _motor;
    private float _yaw;
    private float _pitch;
    private bool _cursorLocked;

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
            cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        Vector3 flatForward = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
        _motor.TurnInDirection(flatForward, Vector3.up);
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

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            _motor.Jump();
        }

        _motor.Sprint(keyboard.leftShiftKey.isPressed);

        if (keyboard.leftCtrlKey.wasPressedThisFrame)
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
