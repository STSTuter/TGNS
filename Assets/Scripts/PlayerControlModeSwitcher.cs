using JohnStairs.RPG.Character.Cam;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owner-only toggle between the project's first-person controller and the
/// vendor's third-person orbit camera/controller (MMORPGController + RPGCamera
/// + InputHandler + CursorHandler). The two never run at the same time - both
/// ultimately drive the same RPGMotor, so only one may be enabled or their
/// outputs would fight. For non-owner instances (remote replicas), every
/// input/camera-driving component listed here is disabled outright - only
/// the owning client's local player should ever read input or move a camera.
/// </summary>
public class PlayerControlModeSwitcher : NetworkBehaviour
{
    [SerializeField] private Key toggleKey = Key.V;
    [SerializeField] private bool startInOrbitMode = true;

    [Header("First-person mode")]
    [SerializeField] private NetworkedFirstPersonController firstPersonController;
    [SerializeField] private GameObject firstPersonCameraObject;

    [Header("Orbit mode")]
    [SerializeField] private Behaviour orbitController; // MMORPGController
    [SerializeField] private RPGCamera orbitCamera;
    [SerializeField] private Behaviour inputHandler; // InputHandler
    [SerializeField] private Behaviour cursorHandler; // CursorHandler
    [SerializeField] private PlayerInput playerInput;

    private bool _orbitMode;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            DisableAll();
            enabled = false;
            return;
        }

        _orbitMode = startInOrbitMode;
        ApplyMode();
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            _orbitMode = !_orbitMode;
            ApplyMode();
        }
    }

    private void ApplyMode()
    {
        if (firstPersonController != null)
        {
            firstPersonController.enabled = !_orbitMode;
        }

        if (firstPersonCameraObject != null)
        {
            firstPersonCameraObject.SetActive(!_orbitMode);
        }

        if (orbitController != null)
        {
            orbitController.enabled = _orbitMode;
        }

        if (orbitCamera != null)
        {
            orbitCamera.enabled = _orbitMode;
        }

        if (inputHandler != null)
        {
            inputHandler.enabled = _orbitMode;
        }

        if (cursorHandler != null)
        {
            cursorHandler.enabled = _orbitMode;
        }

        if (playerInput != null)
        {
            playerInput.enabled = _orbitMode;
        }
    }

    private void DisableAll()
    {
        if (firstPersonController != null) firstPersonController.enabled = false;
        if (firstPersonCameraObject != null) firstPersonCameraObject.SetActive(false);
        if (orbitController != null) orbitController.enabled = false;
        if (orbitCamera != null) orbitCamera.enabled = false;
        if (inputHandler != null) inputHandler.enabled = false;
        if (cursorHandler != null) cursorHandler.enabled = false;
        if (playerInput != null) playerInput.enabled = false;
    }
}
