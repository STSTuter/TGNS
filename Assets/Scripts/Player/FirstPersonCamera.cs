using JohnStairs.RCC.Character.Cam;
using JohnStairs.RCC.Character.Motor;
using JohnStairs.RCC.Enums;
using JohnStairs.RCC.Inputs;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Owner-only first person camera. The camera is parented to an eye pivot on the character, so turning the
/// body also turns the view; mouse Y only pitches the camera.
///
/// Mouse X is not applied here. It is accumulated and handed to FirstPersonController, which feeds it to
/// the RPGMotor as rotation input so the turn happens inside the motor's own update - the only window in
/// which the "Turning Direction" animator parameter that remote replicas play gets written.
///
/// Implements IRPGCamera because RPGMotor resolves its camera through that interface; this replaces
/// RPGCamera + RPGViewFrustum on the player prefab.
/// </summary>
[DefaultExecutionOrder(-50)]
public class FirstPersonCamera : NetworkBehaviour, IRPGCamera
{
    [Header("Eye")]
    [Tooltip("Eye position in character local space. Y is eye height above the character's feet.")]
    [SerializeField] private Vector3 eyeLocalPosition = new Vector3(0f, 1.65f, 0.1f);

    [Header("Look")]
    [Tooltip("Degrees of rotation per unit of mouse delta.")]
    [SerializeField] private float lookSensitivity = 0.12f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private float pitchMin = -85f;
    [SerializeField] private float pitchMax = 85f;

    [Header("Cursor")]
    [Tooltip("Lock and hide the cursor while the first person view is active. Toggled with the \"Toggle Menu Cursor\" binding (F1).")]
    [SerializeField] private bool lockCursor = true;

    [Header("Own character")]
    [Tooltip("Render the owner's own body as shadows only, so the head does not block the first person view.")]
    [SerializeField] private bool hideOwnCharacter = true;

    private RPGInputActions _inputActions;
    private RPGMotor _rpgMotor;
    private Camera _usedCamera;
    private Transform _eyePivot;
    private float _pitch;
    private float _pendingYawDegrees;
    private bool _cursorLocked;

    /// <summary>
    /// Pivot the camera is parented to. Use it as the origin for interaction rays.
    /// </summary>
    public Transform EyePivot => _eyePivot;

    protected virtual void Awake()
    {
        _rpgMotor = GetComponent<RPGMotor>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsClient || !IsOwner)
        {
            return;
        }

        _inputActions = RPGInputManager.GetInputActions();

        CreateEyePivot();
        AcquireCamera();

        if (hideOwnCharacter)
        {
            RefreshOwnCharacterVisibility();
        }

        SetCursorLocked(lockCursor);
        enabled = true;
    }

    public override void OnNetworkDespawn()
    {
        ReleaseCamera();
        SetCursorLocked(false);
    }

    public override void OnDestroy()
    {
        // Backstop for teardown paths that do not run OnNetworkDespawn, e.g. a scene unload
        ReleaseCamera();
        base.OnDestroy();
    }

    protected virtual void Update()
    {
        if (!IsSpawned || !IsOwner || _usedCamera == null)
        {
            return;
        }

        if (_inputActions.Character.ToggleMenuCursor.WasPressedThisFrame())
        {
            SetCursorLocked(!_cursorLocked);
        }

        if (!_cursorLocked)
        {
            return;
        }

        Vector2 lookInput = _inputActions.Character.RotationAmount.ReadValue<Vector2>();

        float yawDegrees = lookInput.x * lookSensitivity;
        if (yawDegrees != 0)
        {
            if (_rpgMotor != null && _rpgMotor.enabled)
            {
                // Consumed by FirstPersonController later this frame
                _pendingYawDegrees += yawDegrees;
            }
            else
            {
                transform.Rotate(Vector3.up, yawDegrees, Space.World);
            }
        }

        float pitchDegrees = lookInput.y * lookSensitivity * (invertY ? 1f : -1f);
        _pitch = Mathf.Clamp(_pitch + pitchDegrees, pitchMin, pitchMax);
        _usedCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    /// <summary>
    /// Returns the horizontal look input accumulated since the last call, in degrees, and clears it
    /// </summary>
    /// <returns>Degrees the character body should turn this frame</returns>
    public virtual float ConsumeYawDegrees()
    {
        float degrees = _pendingYawDegrees;
        _pendingYawDegrees = 0f;
        return degrees;
    }

    /// <summary>
    /// Locks/unlocks the cursor and stops/resumes look input
    /// </summary>
    /// <param name="locked">If true, the cursor is locked to the screen center and hidden</param>
    public virtual void SetCursorLocked(bool locked)
    {
        _cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;

        if (!locked)
        {
            _pendingYawDegrees = 0f;
        }
    }

    /// <summary>
    /// Re-applies the shadows-only setting to the owner's renderers. Call this after swapping body parts
    /// so newly shown meshes do not pop back into the first person view.
    /// </summary>
    public virtual void RefreshOwnCharacterVisibility()
    {
        if (!IsOwner || !hideOwnCharacter)
        {
            return;
        }

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }

    protected virtual void CreateEyePivot()
    {
        _eyePivot = new GameObject("Eye Pivot").transform;
        _eyePivot.SetParent(transform, false);
        _eyePivot.localPosition = eyeLocalPosition;
        _eyePivot.localRotation = Quaternion.identity;
    }

    protected virtual void AcquireCamera()
    {
        _usedCamera = Camera.main;

        if (_usedCamera == null)
        {
            GameObject cameraObject = new GameObject("First Person Camera")
            {
                tag = "MainCamera"
            };
            _usedCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            Debug.LogWarning("[FirstPersonCamera] No camera tagged \"MainCamera\" in the scene; spawned a plain one without render pipeline settings.");
        }

        Transform cameraTransform = _usedCamera.transform;
        cameraTransform.SetParent(_eyePivot, false);
        cameraTransform.localPosition = Vector3.zero;
        cameraTransform.localRotation = Quaternion.identity;
    }

    protected virtual void ReleaseCamera()
    {
        if (_usedCamera != null)
        {
            // Detach so the scene camera is not destroyed together with the player object
            _usedCamera.transform.SetParent(null, true);
            _usedCamera = null;
        }

        if (_eyePivot != null)
        {
            Destroy(_eyePivot.gameObject);
            _eyePivot = null;
        }
    }

    /* IRPGCamera interface */

    public Camera GetUsedCamera()
    {
        return _usedCamera;
    }

    public Vector2 GetViewportExtentsWithMargin()
    {
        if (_usedCamera == null)
        {
            return Vector2.zero;
        }

        Vector2 result;
        float halfFieldOfView = _usedCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        result.y = _usedCamera.nearClipPlane * Mathf.Tan(halfFieldOfView);
        result.x = result.y * _usedCamera.aspect;
        return result;
    }

    public void Rotate(Axis axis, float degrees, bool immediately)
    {
        // Horizontal rotation needs no work here: the camera is parented to the character and follows its yaw
        if (axis == Axis.Y)
        {
            _pitch = Mathf.Clamp(_pitch + degrees, pitchMin, pitchMax);
        }
    }

    public bool IsOrbitingWithCharacterRotation()
    {
        // In first person the body always turns with the view
        return true;
    }
}
