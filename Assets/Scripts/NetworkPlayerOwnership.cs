using JohnStairs.RPG;
using JohnStairs.RPG.Character.Cam;
using JohnStairs.RPG.Character.Controller;
using JohnStairs.RPG.Character.Controller.Subcomponents;
using JohnStairs.RPG.Character.Motor;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gates the John Stairs MMO control stack to the owning client.
///
/// The vendor scripts (<see cref="MMORPGController"/>, <see cref="RPGCamera"/>, <see cref="InputHandler"/>,
/// <see cref="CursorHandler"/>, <see cref="RPGMotor"/>) have no concept of a local player - left to themselves
/// every replica in the session would read the same keyboard, run its own motor and fight over Camera.main.
/// So they are authored disabled on the prefab and only ever switched on here, for the owner.
///
/// Authority model is client-authoritative: the owner runs the controller and motor locally, writes its own
/// transform, and NetworkTransform (owner authority) plus NetworkAnimator (owner authority) replicate the
/// result. Remote replicas keep RPGMotor disabled so nothing fights the interpolated network position.
/// </summary>
public class NetworkPlayerOwnership : NetworkBehaviour
{
    [Header("Owner-only control stack (left disabled on the prefab)")]
    [SerializeField] private MMORPGController controller;
    [SerializeField] private RPGCamera rpgCamera;
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private CursorHandler cursorHandler;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private RPGMotor motor;
    [SerializeField] private GameObject cameraObject;

    [Header("Behaviour")]
    [Tooltip("Detach the camera from the character on spawn. RPGCamera writes world-space transforms in " +
             "LateUpdate, so parenting it to a moving character only means the parent's motion is overwritten " +
             "every frame.")]
    [SerializeField] private bool detachCamera = true;
    [Tooltip("Disable the scene's MainCamera-tagged camera while this player owns one, so the session never " +
             "has two active cameras or two AudioListeners.")]
    [SerializeField] private bool takeOverSceneCamera = true;

    private GameObject _suppressedSceneCamera;
    private bool _cameraDetached;

    private void Awake()
    {
        // Fall back to GetComponent so the component still works if the prefab wiring is incomplete.
        if (controller == null) controller = GetComponent<MMORPGController>();
        if (rpgCamera == null) rpgCamera = GetComponent<RPGCamera>();
        if (inputHandler == null) inputHandler = GetComponent<InputHandler>();
        if (cursorHandler == null) cursorHandler = GetComponent<CursorHandler>();
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        if (motor == null) motor = GetComponent<RPGMotor>();

        if (cameraObject == null && rpgCamera != null && rpgCamera.UsedCamera != null)
        {
            cameraObject = rpgCamera.UsedCamera.gameObject;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            DisableLocalStack();
            enabled = false;
            return;
        }

        // 1. Position first. RPGCamera.Start() runs the first time the component is enabled and seeds its yaw
        //    from transform.forward via ResetView(), so enabling it before the character has been moved to its
        //    spawn point would leave the camera framed from the prefab origin.
        if (PlayerSpawnPoints.Instance != null)
        {
            transform.position = PlayerSpawnPoints.Instance.GetSpawnPosition(OwnerClientId);
        }

        // 2. Claim the scene camera before activating our own, so the tag lookup cannot find ours.
        if (takeOverSceneCamera)
        {
            SuppressSceneCamera();
        }

        // 3. Bring up this player's camera.
        if (cameraObject != null)
        {
            cameraObject.SetActive(true);

            if (detachCamera && cameraObject.transform.parent != null)
            {
                cameraObject.transform.SetParent(null, true);
                _cameraDetached = true;
            }
        }

        // 4. Enable in dependency order: input source, then the readers, then the camera, then the controller,
        //    then the motor the controller drives.
        SetEnabled(playerInput, true);
        SetEnabled(cursorHandler, true);
        SetEnabled(inputHandler, true);
        SetEnabled(rpgCamera, true);
        SetEnabled(controller, true);
        SetEnabled(motor, true);

        // 5. MMO convention: cursor free by default. CursorHandler hides and warps it while orbiting.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
        {
            return;
        }

        RestoreSceneCamera();
        DestroyDetachedCamera();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public override void OnDestroy()
    {
        // OnNetworkDespawn does not run on a hard scene teardown or an unspawned destroy; without this the
        // detached camera would outlive its player.
        RestoreSceneCamera();
        DestroyDetachedCamera();

        base.OnDestroy();
    }

    /// <summary>
    /// Everything a remote replica must not run. Replicas are driven purely by NetworkTransform and
    /// NetworkAnimator - running RPGMotor here would fight the replicated position.
    /// </summary>
    private void DisableLocalStack()
    {
        SetEnabled(controller, false);
        SetEnabled(rpgCamera, false);
        SetEnabled(inputHandler, false);
        SetEnabled(cursorHandler, false);
        SetEnabled(playerInput, false);
        SetEnabled(motor, false);

        if (cameraObject != null)
        {
            cameraObject.SetActive(false);
        }
    }

    private void SuppressSceneCamera()
    {
        if (_suppressedSceneCamera != null)
        {
            return;
        }

        Camera sceneCamera = Camera.main;
        if (sceneCamera == null || (cameraObject != null && sceneCamera.gameObject == cameraObject))
        {
            return;
        }

        _suppressedSceneCamera = sceneCamera.gameObject;
        _suppressedSceneCamera.SetActive(false);
    }

    private void RestoreSceneCamera()
    {
        if (_suppressedSceneCamera == null)
        {
            return;
        }

        _suppressedSceneCamera.SetActive(true);
        _suppressedSceneCamera = null;
    }

    private void DestroyDetachedCamera()
    {
        if (!_cameraDetached || cameraObject == null)
        {
            return;
        }

        Destroy(cameraObject);
        _cameraDetached = false;
    }

    private static void SetEnabled(Behaviour behaviour, bool value)
    {
        if (behaviour != null)
        {
            behaviour.enabled = value;
        }
    }
}
