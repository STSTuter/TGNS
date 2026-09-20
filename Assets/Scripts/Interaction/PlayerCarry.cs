using System;
using JohnStairs.RCC.Character.Motor;
using JohnStairs.RCC.Inputs;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owner-only first person pick up and carry. The held object is never parented or made kinematic: it stays
/// a fully simulated rigidbody and is pushed towards a hold point in front of the camera with a force that
/// is capped by <see cref="PlayerStrength"/>. Everything the design asks for falls out of that cap.
///
/// The force budget includes the force spent holding the object up, so an object heavier than the player's
/// lift capacity cannot be raised at all - it sags towards the ground and is dragged along. An object near
/// the capacity is liftable but accelerates slowly and swings behind the aim point. A light object tracks
/// the crosshair almost exactly.
///
/// Networking: the grab is a request. The server checks reach and mass against the player's strength, then
/// hands the object's NetworkObject to this client, so the carry is simulated locally with no round trip.
/// Release returns ownership to the server, which also applies the throw velocity, because by then this
/// client no longer has authority over the object.
/// </summary>
[RequireComponent(typeof(PlayerStrength))]
public class PlayerCarry : NetworkBehaviour
{
    /// <summary>
    /// Why a grab request was turned down. Sent back to the requesting client for feedback.
    /// </summary>
    public enum GrabRefusal
    {
        None,
        NothingInReach,
        AlreadyHeld,
        TooHeavy,
        OutOfReach
    }

    [Header("Reach")]
    [Tooltip("How far the player can reach, in metres, measured from the camera.")]
    [SerializeField] private float grabRange = 3.5f;

    [Tooltip("Radius of the aim cast. A small radius makes picking up small props far more forgiving than a thin ray.")]
    [SerializeField] private float grabRadius = 0.15f;

    [Tooltip("Layers that can be picked up or block the aim cast.")]
    [SerializeField] private LayerMask grabLayers = ~0;

    [Tooltip("Slack on the server reach check, to absorb the latency between aiming and the request arriving.")]
    [SerializeField] private float serverRangeTolerance = 2f;

    [Header("Hold")]
    [Tooltip("Distance in front of the camera the object is held at. Adjusted in play with the scroll wheel.")]
    [SerializeField] private float holdDistance = 2f;
    [SerializeField] private float holdDistanceMin = 1.2f;
    [SerializeField] private float holdDistanceMax = 3.5f;
    [SerializeField] private float holdDistanceStep = 0.25f;

    [Tooltip("How hard the hold pulls the object towards the hold point, in 1/s. Higher is stiffer and twitchier.")]
    [SerializeField] private float holdResponsiveness = 12f;

    [Tooltip("Fastest the hold will try to move an object, in m/s. Caps how far a light object can be flung by turning around.")]
    [SerializeField] private float maxHoldSpeed = 7f;

    [Tooltip("How hard the hold turns the object towards its carry orientation, in 1/s.")]
    [SerializeField] private float turnResponsiveness = 10f;

    [Tooltip("Fastest the hold will try to turn an object, in rad/s.")]
    [SerializeField] private float maxTurnSpeed = 6f;

    [Tooltip("How far below the aim point an object at the maximum grabbable mass hangs, in metres. This is what makes an overweight object drag along the floor.")]
    [SerializeField] private float overloadSag = 1.2f;

    [Tooltip("If the object ends up this far behind its hold point it is dropped, e.g. because it got stuck on geometry.")]
    [SerializeField] private float breakDistance = 2.5f;

    [Header("Carrying penalty")]
    [Tooltip("Movement speed multiplier when carrying an object at the maximum grabbable mass. 1 disables the penalty.")]
    [SerializeField] private float minSpeedFactor = 0.35f;

    [Header("Rotating a held object")]
    [Tooltip("Degrees of object rotation per unit of mouse delta while the rotate button is held.")]
    [SerializeField] private float rotateSensitivity = 0.35f;

    [Header("Input")]
    [SerializeField]
    private InputAction grabAction = new InputAction("Grab", InputActionType.Button, "<Keyboard>/e");

    [SerializeField]
    private InputAction throwAction = new InputAction("Throw", InputActionType.Button, "<Mouse>/leftButton");

    [SerializeField]
    private InputAction holdDistanceAction = new InputAction("Hold Distance", InputActionType.Value, "<Mouse>/scroll/y");

    [SerializeField]
    private InputAction rotateHeldAction = new InputAction("Rotate Held Object", InputActionType.Button, "<Keyboard>/r");

    private RPGInputActions m_RpgInput;
    private FirstPersonCamera m_Camera;
    private PlayerStrength m_Strength;
    private RPGMotor m_Motor;
    private Collider m_PlayerCollider;

    private Grabbable m_Carried;
    private Collider[] m_CarriedColliders = Array.Empty<Collider>();
    private Quaternion m_CarryLocalRotation = Quaternion.identity;
    private bool m_RotatingHeldObject;

    private float m_BaseRunSpeed;
    private float m_BaseWalkSpeed;
    private float m_BaseStrafeSpeed;
    private float m_BaseCrouchSpeed;
    private bool m_SpeedPenaltyApplied;

    /// <summary>The object this player is currently carrying, or null.</summary>
    public Grabbable Carried => m_Carried;

    /// <summary>True while an object is being carried. Read by systems that must not fire while the hands are full.</summary>
    public bool IsCarrying => m_Carried != null;

    /// <summary>The grabbable currently under the crosshair and in reach, or null. Owner only; for HUD use.</summary>
    public Grabbable Focused { get; private set; }

    /// <summary>Carried mass as a fraction of the heaviest object this player could grab. 0 when empty.</summary>
    public float LoadFactor { get; private set; }

    /// <summary>Why the last grab request was refused, for HUD feedback.</summary>
    public GrabRefusal LastRefusal { get; private set; }

    /// <summary>Time.time at which <see cref="LastRefusal"/> was set.</summary>
    public float LastRefusalTime { get; private set; }

    protected virtual void Awake()
    {
        m_Camera = GetComponent<FirstPersonCamera>();
        m_Strength = GetComponent<PlayerStrength>();
        m_Motor = GetComponent<RPGMotor>();
        m_PlayerCollider = GetComponent<CharacterController>();

        if (m_PlayerCollider == null)
        {
            m_PlayerCollider = GetComponent<Collider>();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsClient || !IsOwner)
        {
            return;
        }

        m_RpgInput = RPGInputManager.GetInputActions();

        grabAction.Enable();
        throwAction.Enable();
        holdDistanceAction.Enable();
        rotateHeldAction.Enable();

        enabled = true;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
        {
            return;
        }

        StopCarryingLocally();

        grabAction.Disable();
        throwAction.Disable();
        holdDistanceAction.Disable();
        rotateHeldAction.Disable();
    }

    protected virtual void Update()
    {
        if (!IsSpawned || !IsOwner)
        {
            return;
        }

        Transform aim = GetAimTransform();

        if (aim == null)
        {
            return;
        }

        UpdateRotateMode(aim);
        UpdateHoldDistance();

        Focused = m_Carried == null ? FindGrabbableInReach(aim) : null;

        if (grabAction.WasPressedThisFrame())
        {
            if (m_Carried != null)
            {
                RequestRelease(Vector3.zero);
            }
            else
            {
                RequestGrab(aim);
            }
        }

        if (m_Carried != null && throwAction.WasPressedThisFrame())
        {
            float speed = m_Strength.GetMaxThrowSpeed(m_Carried.Mass);
            RequestRelease(aim.forward * speed);
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!IsSpawned || !IsOwner || m_Carried == null)
        {
            return;
        }

        // Ownership arrives a round trip after the grab was accepted; until then the body is still kinematic
        if (!m_Carried.IsOwner)
        {
            return;
        }

        Rigidbody body = m_Carried.Body;
        Transform aim = GetAimTransform();

        if (body == null || aim == null)
        {
            StopCarryingLocally();
            return;
        }

        DriveHoldPosition(body, aim);

        // DriveHoldPosition can drop the object when it falls too far behind the hold point
        if (m_Carried != null)
        {
            DriveHoldRotation(body, aim);
        }
    }

    /* Hold simulation */

    /// <summary>
    /// Pushes the held body towards the hold point with a force capped by the player's strength. The cap
    /// includes gravity compensation, so an object the player is not strong enough to lift simply sinks.
    /// </summary>
    /// <param name="body">Rigidbody being carried</param>
    /// <param name="aim">Camera transform the hold point hangs in front of</param>
    protected virtual void DriveHoldPosition(Rigidbody body, Transform aim)
    {
        float mass = body.mass;
        Vector3 holdPoint = GetHoldPoint(aim, mass);
        Vector3 toTarget = holdPoint - body.worldCenterOfMass;

        if (toTarget.magnitude > breakDistance)
        {
            // Wedged behind geometry or yanked out of reach; let go rather than fight the solver
            RequestRelease(Vector3.zero);
            return;
        }

        Vector3 desiredVelocity = Vector3.ClampMagnitude(toTarget * holdResponsiveness, maxHoldSpeed);
        Vector3 velocityChange = desiredVelocity - body.linearVelocity;

        // The force for this step's velocity change, plus the force spent holding the object's weight up.
        // Clamping the sum is what stops a heavy object from ever leaving the ground.
        Vector3 force = velocityChange * mass / Time.fixedDeltaTime - Physics.gravity * mass;
        body.AddForce(Vector3.ClampMagnitude(force, m_Strength.MaxHoldForce));
    }

    /// <summary>
    /// Turns the held body towards its carry orientation with an angular acceleration budget that shrinks
    /// as the object gets heavier.
    /// </summary>
    /// <param name="body">Rigidbody being carried</param>
    /// <param name="aim">Camera transform the carry orientation is relative to</param>
    protected virtual void DriveHoldRotation(Rigidbody body, Transform aim)
    {
        Quaternion target = GetYaw(aim.rotation) * m_CarryLocalRotation;
        Quaternion delta = target * Quaternion.Inverse(body.rotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle == 0f || float.IsInfinity(axis.x) || float.IsNaN(axis.x))
        {
            return;
        }

        if (angle > 180f)
        {
            angle -= 360f;
        }

        Vector3 desiredAngularVelocity = Vector3.ClampMagnitude(
            axis.normalized * (angle * Mathf.Deg2Rad * turnResponsiveness), maxTurnSpeed);
        Vector3 angularAcceleration = (desiredAngularVelocity - body.angularVelocity) / Time.fixedDeltaTime;

        body.AddTorque(
            Vector3.ClampMagnitude(angularAcceleration, m_Strength.GetMaxTurnAcceleration(mass: body.mass)),
            ForceMode.Acceleration);
    }

    /// <summary>
    /// Hold point in front of the camera, pulled in when geometry is in the way and dropped towards the
    /// ground in proportion to how far over the lift capacity the object is.
    /// </summary>
    /// <param name="aim">Camera transform</param>
    /// <param name="mass">Mass of the held object</param>
    /// <returns>World space point the object is pulled towards</returns>
    protected virtual Vector3 GetHoldPoint(Transform aim, float mass)
    {
        float distance = holdDistance;
        RaycastHit[] hits = Physics.SphereCastAll(aim.position, grabRadius, aim.forward, holdDistance,
            grabLayers, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (IsOwnCollider(hit.collider) || IsCarriedCollider(hit.collider))
            {
                continue;
            }

            distance = Mathf.Min(distance, Mathf.Max(holdDistanceMin, hit.distance - grabRadius));
        }

        float overload = Mathf.InverseLerp(m_Strength.LiftCapacity, m_Strength.MaxGrabMass, mass);
        return aim.position + aim.forward * distance - Vector3.up * (overloadSag * overload);
    }

    /* Input */

    private void UpdateHoldDistance()
    {
        if (m_Carried == null || m_RotatingHeldObject)
        {
            return;
        }

        float scroll = holdDistanceAction.ReadValue<float>();

        if (Mathf.Approximately(scroll, 0f))
        {
            return;
        }

        holdDistance = Mathf.Clamp(holdDistance + Mathf.Sign(scroll) * holdDistanceStep,
            holdDistanceMin, holdDistanceMax);
    }

    private void UpdateRotateMode(Transform aim)
    {
        bool wantsRotate = m_Carried != null && rotateHeldAction.IsPressed();

        if (wantsRotate != m_RotatingHeldObject)
        {
            m_RotatingHeldObject = wantsRotate;

            // Borrow the mouse from the camera so turning the crate does not also spin the player
            if (m_Camera != null)
            {
                m_Camera.SuppressLook(wantsRotate);
            }
        }

        if (!m_RotatingHeldObject || m_RpgInput == null)
        {
            return;
        }

        Vector2 mouseDelta = m_RpgInput.Character.RotationAmount.ReadValue<Vector2>();

        if (mouseDelta == Vector2.zero)
        {
            return;
        }

        Quaternion yaw = GetYaw(aim.rotation);
        Quaternion spin = Quaternion.AngleAxis(mouseDelta.x * rotateSensitivity, Vector3.up)
                          * Quaternion.AngleAxis(-mouseDelta.y * rotateSensitivity, aim.right);
        m_CarryLocalRotation = Quaternion.Inverse(yaw) * spin * yaw * m_CarryLocalRotation;
    }

    /* Grab and release */

    private void RequestGrab(Transform aim)
    {
        Grabbable target = FindGrabbableInReach(aim);

        if (target == null)
        {
            SetRefusal(GrabRefusal.NothingInReach);
            return;
        }

        if (target.IsHeld)
        {
            SetRefusal(GrabRefusal.AlreadyHeld);
            return;
        }

        // Predicted locally so the "too heavy" feedback is immediate; the server checks it again
        if (target.Mass > m_Strength.MaxGrabMass)
        {
            SetRefusal(GrabRefusal.TooHeavy);
            return;
        }

        RequestGrabRpc(new NetworkObjectReference(target.NetworkObject));
    }

    private void RequestRelease(Vector3 throwVelocity)
    {
        if (m_Carried == null)
        {
            return;
        }

        NetworkObjectReference reference = new NetworkObjectReference(m_Carried.NetworkObject);
        StopCarryingLocally();
        RequestReleaseRpc(reference, throwVelocity);
    }

    [Rpc(SendTo.Server)]
    private void RequestGrabRpc(NetworkObjectReference target, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!target.TryGet(out NetworkObject networkObject) ||
            !networkObject.TryGetComponent(out Grabbable grabbable) ||
            !grabbable.CanBeGrabbed)
        {
            GrabResultRpc(target, false, GrabRefusal.NothingInReach);
            return;
        }

        if (grabbable.IsHeld)
        {
            GrabResultRpc(target, false, GrabRefusal.AlreadyHeld);
            return;
        }

        // Checked against the server's own copy of both transforms, never against anything the client claimed
        float reach = grabRange + serverRangeTolerance;

        if ((grabbable.transform.position - transform.position).sqrMagnitude > reach * reach)
        {
            GrabResultRpc(target, false, GrabRefusal.OutOfReach);
            return;
        }

        if (grabbable.Mass > m_Strength.MaxGrabMass)
        {
            GrabResultRpc(target, false, GrabRefusal.TooHeavy);
            return;
        }

        bool accepted = grabbable.ServerTryHold(clientId);
        GrabResultRpc(target, accepted, accepted ? GrabRefusal.None : GrabRefusal.AlreadyHeld);
    }

    [Rpc(SendTo.Owner)]
    private void GrabResultRpc(NetworkObjectReference target, bool accepted, GrabRefusal refusal)
    {
        if (!accepted)
        {
            SetRefusal(refusal);
            return;
        }

        if (target.TryGet(out NetworkObject networkObject) &&
            networkObject.TryGetComponent(out Grabbable grabbable))
        {
            StartCarryingLocally(grabbable);
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestReleaseRpc(NetworkObjectReference target, Vector3 throwVelocity, RpcParams rpcParams = default)
    {
        if (!target.TryGet(out NetworkObject networkObject) ||
            !networkObject.TryGetComponent(out Grabbable grabbable))
        {
            return;
        }

        // Only the client that is actually holding it may put it down
        if (grabbable.HeldByClientId != rpcParams.Receive.SenderClientId)
        {
            return;
        }

        float maxSpeed = m_Strength.GetMaxThrowSpeed(grabbable.Mass);
        grabbable.ServerRelease(Vector3.ClampMagnitude(throwVelocity, maxSpeed));
    }

    /* Local carry state */

    private void StartCarryingLocally(Grabbable grabbable)
    {
        if (m_Carried == grabbable)
        {
            return;
        }

        StopCarryingLocally();

        m_Carried = grabbable;
        m_Carried.HolderChanged += OnCarriedHolderChanged;
        m_Carried.BeginCarryPhysics();

        Transform aim = GetAimTransform();
        Quaternion yaw = GetYaw(aim != null ? aim.rotation : transform.rotation);
        m_CarryLocalRotation = Quaternion.Inverse(yaw) * grabbable.transform.rotation;

        SetCarriedCollisionWithPlayer(false);
        ApplySpeedPenalty(grabbable.Mass);

        LoadFactor = Mathf.Clamp01(grabbable.Mass / m_Strength.MaxGrabMass);
        LastRefusal = GrabRefusal.None;
    }

    private void StopCarryingLocally()
    {
        if (m_Carried == null)
        {
            return;
        }

        SetCarriedCollisionWithPlayer(true);
        m_Carried.HolderChanged -= OnCarriedHolderChanged;
        m_Carried.EndCarryPhysics();
        m_Carried = null;
        m_CarriedColliders = Array.Empty<Collider>();
        LoadFactor = 0f;

        RemoveSpeedPenalty();

        if (m_RotatingHeldObject && m_Camera != null)
        {
            m_Camera.SuppressLook(false);
        }

        m_RotatingHeldObject = false;
    }

    private void OnCarriedHolderChanged(ulong previous, ulong current)
    {
        // The server took the hold away, e.g. the object despawned or the hold was cleared on disconnect
        if (current != NetworkManager.LocalClientId)
        {
            StopCarryingLocally();
        }
    }

    /// <summary>
    /// Stops the carried object from shoving the player around. The CharacterController would otherwise
    /// fight the held body at close range, which is exactly the kind of jitter this system must avoid.
    /// </summary>
    /// <param name="collide">True to restore collision between the player and the carried object</param>
    private void SetCarriedCollisionWithPlayer(bool collide)
    {
        if (m_PlayerCollider == null)
        {
            return;
        }

        if (!collide && m_Carried != null)
        {
            m_CarriedColliders = m_Carried.GetComponentsInChildren<Collider>(true);
        }

        foreach (Collider collider in m_CarriedColliders)
        {
            if (collider != null)
            {
                Physics.IgnoreCollision(collider, m_PlayerCollider, !collide);
            }
        }
    }

    /* Movement penalty */

    private void ApplySpeedPenalty(float mass)
    {
        if (m_Motor == null || m_SpeedPenaltyApplied || minSpeedFactor >= 1f)
        {
            return;
        }

        m_BaseRunSpeed = m_Motor.RunSpeed;
        m_BaseWalkSpeed = m_Motor.WalkSpeed;
        m_BaseStrafeSpeed = m_Motor.StrafeSpeed;
        m_BaseCrouchSpeed = m_Motor.CrouchSpeed;

        float factor = Mathf.Lerp(1f, minSpeedFactor, Mathf.Clamp01(mass / m_Strength.MaxGrabMass));
        m_Motor.RunSpeed = m_BaseRunSpeed * factor;
        m_Motor.WalkSpeed = m_BaseWalkSpeed * factor;
        m_Motor.StrafeSpeed = m_BaseStrafeSpeed * factor;
        m_Motor.CrouchSpeed = m_BaseCrouchSpeed * factor;
        m_SpeedPenaltyApplied = true;
    }

    private void RemoveSpeedPenalty()
    {
        if (m_Motor == null || !m_SpeedPenaltyApplied)
        {
            return;
        }

        m_Motor.RunSpeed = m_BaseRunSpeed;
        m_Motor.WalkSpeed = m_BaseWalkSpeed;
        m_Motor.StrafeSpeed = m_BaseStrafeSpeed;
        m_Motor.CrouchSpeed = m_BaseCrouchSpeed;
        m_SpeedPenaltyApplied = false;
    }

    /* Helpers */

    private Transform GetAimTransform()
    {
        Camera usedCamera = m_Camera != null ? m_Camera.GetUsedCamera() : null;
        return usedCamera != null ? usedCamera.transform : null;
    }

    private Grabbable FindGrabbableInReach(Transform aim)
    {
        RaycastHit[] hits = Physics.SphereCastAll(aim.position, grabRadius, aim.forward, grabRange,
            grabLayers, QueryTriggerInteraction.Ignore);

        Grabbable best = null;
        float bestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.distance >= bestDistance || IsOwnCollider(hit.collider))
            {
                continue;
            }

            Grabbable grabbable = hit.collider.GetComponentInParent<Grabbable>();

            if (grabbable == null || !grabbable.CanBeGrabbed)
            {
                continue;
            }

            best = grabbable;
            bestDistance = hit.distance;
        }

        return best;
    }

    private bool IsOwnCollider(Collider collider)
    {
        return collider != null && collider.transform.IsChildOf(transform);
    }

    private bool IsCarriedCollider(Collider collider)
    {
        return m_Carried != null && collider != null && collider.transform.IsChildOf(m_Carried.transform);
    }

    private void SetRefusal(GrabRefusal refusal)
    {
        LastRefusal = refusal;
        LastRefusalTime = Time.time;
    }

    private static Quaternion GetYaw(Quaternion rotation)
    {
        // Only the body's heading should carry the object around; camera pitch must not tilt it
        return Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
    }
}
