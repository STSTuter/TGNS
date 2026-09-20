using JohnStairs.RCC.Character.Motor;
using JohnStairs.RCC.Inputs;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Owner-only first person input for the RPGMotor: WASD moves forward/back and strafes relative to the
/// character's facing, and the mouse look FirstPersonCamera collected turns the body. Jump, sprint, crouch,
/// walk, swimming and climbing inputs are passed through unchanged.
///
/// This replaces RPGController on the player prefab, whose MMO mapping turns the character with A/D and
/// strafes with Q/E.
/// </summary>
public class FirstPersonController : NetworkBehaviour
{
    [Tooltip("If set to false, all character controls are disabled.")]
    public bool ActivateControl = true;

    private RPGInputActions _inputActions;
    private RPGMotor _rpgMotor;
    private FirstPersonCamera _firstPersonCamera;
    private bool _wasMoving;

    protected virtual void Awake()
    {
        _rpgMotor = GetComponent<RPGMotor>();
        _firstPersonCamera = GetComponent<FirstPersonCamera>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsClient || !IsOwner)
        {
            return;
        }

        _inputActions = RPGInputManager.GetInputActions();
        enabled = true;
    }

    protected virtual void Update()
    {
        if (!IsSpawned || !IsOwner || _rpgMotor == null)
        {
            return;
        }

        Vector2 movementInput = ActivateControl
            ? _inputActions.Character.Movement.ReadValue<Vector2>()
            : Vector2.zero;

        bool isMoving = movementInput != Vector2.zero;
        if (isMoving && !_wasMoving)
        {
            // Any movement input cancels autorunning and counts as a midair move attempt
            _rpgMotor.ToggleAutorunning(false);
            _rpgMotor.TryMidairMovement();
        }
        _wasMoving = isMoving;

        // The MMO motor takes sideward movement from the strafe input and forward movement from the
        // input direction's z component, but reads both components for the resulting movement speed
        _rpgMotor.SetInputDirection(new Vector3(movementInput.x, 0f, movementInput.y));
        _rpgMotor.SetStrafe(movementInput.x);
        _rpgMotor.SetRotation(GetRotationInput());
        _rpgMotor.SetRotationInputModification(false);
        _rpgMotor.SetAlignWithCamera(false);
        _rpgMotor.PauseCameraRotation(false);

        _rpgMotor.Sprint(ActivateControl && _inputActions.Character.Sprint.IsPressed());
        _rpgMotor.ToggleWalking(ActivateControl && _inputActions.Character.ToggleWalking.WasPressedThisFrame());
        _rpgMotor.ToggleCrouching(ActivateControl && _inputActions.Character.ToggleCrouching.WasPressedThisFrame());

        if (ActivateControl && _inputActions.Character.Jump.WasPressedThisFrame())
        {
            _rpgMotor.Jump();
        }

        if (ActivateControl && _inputActions.Character.CancelClimbing.WasPressedThisFrame())
        {
            _rpgMotor.CancelClimbing();
        }

        _rpgMotor.Surface(ActivateControl && _inputActions.Character.Surface.IsPressed());
        _rpgMotor.Dive(ActivateControl && _inputActions.Character.Dive.IsPressed());

        _rpgMotor.StartMotor();
    }

    /// <summary>
    /// Converts the mouse look that FirstPersonCamera collected this frame into the motor's rotation input.
    /// The motor turns by rotationInput * RotationSpeed * deltaTime inside its own update, so the degrees are
    /// scaled back into that input space instead of rotating the transform behind the motor's back - that way
    /// the turn also reaches the "Turning Direction" animator parameter remote replicas play.
    /// </summary>
    /// <returns>Rotation input for RPGMotor.SetRotation</returns>
    protected virtual float GetRotationInput()
    {
        if (_firstPersonCamera == null)
        {
            return 0f;
        }

        float yawDegrees = _firstPersonCamera.ConsumeYawDegrees();

        if (yawDegrees == 0f)
        {
            return 0f;
        }

        // While climbing, the MMO motor adds the rotation input to sideward climbing movement and refuses to
        // turn anyway, so the look is dropped rather than turned into a slide along the wall
        if (_rpgMotor.IsClimbing())
        {
            return 0f;
        }

        float degreesPerInputUnit = _rpgMotor.RotationSpeed * Time.deltaTime;

        if (degreesPerInputUnit <= 0f)
        {
            // Rotation speed of 0 disables the motor's own turning; fall back to turning the transform
            transform.Rotate(Vector3.up, yawDegrees, Space.World);
            return 0f;
        }

        return yawDegrees / degreesPerInputUnit;
    }
}
