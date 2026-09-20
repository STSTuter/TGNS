using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Marks a networked rigidbody as something a player can pick up, and owns the authoritative answer to
/// "who is holding this right now".
///
/// Authority follows the project's owner-authoritative movement model. At rest the object is owned by the
/// server, so the server simulates it and every client receives the result. On a successful grab the
/// server hands ownership to the grabbing client, which then simulates the object locally while it is
/// carried - that is what makes the carry feel immediate. On release ownership goes back to the server.
///
/// The NetworkObject must have Don't Destroy With Owner enabled, otherwise a carrier who disconnects takes
/// the cargo with them. <see cref="PhysicsGrabSetup"/> sets that; this component warns if it is missing.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Grabbable : NetworkBehaviour
{
    /// <summary>
    /// Value of <see cref="HeldByClientId"/> while nobody is holding the object.
    /// </summary>
    public const ulong NoHolder = ulong.MaxValue;

    [Tooltip("Refuse grabs entirely, e.g. for scenery that happens to be a rigidbody.")]
    [SerializeField] private bool canBeGrabbed = true;

    [Tooltip("Collision detection mode used while the object is being carried, so a fast swing does not tunnel it through a wall. Restored on release.")]
    [SerializeField] private CollisionDetectionMode carriedCollisionDetection = CollisionDetectionMode.ContinuousDynamic;

    private readonly NetworkVariable<ulong> m_HeldBy = new NetworkVariable<ulong>(
        NoHolder, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Rigidbody m_Rigidbody;
    private CollisionDetectionMode m_RestingCollisionDetection;
    private bool m_CarryRequested;
    private bool m_CarryTweaksApplied;
    private bool m_HasPendingThrow;
    private Vector3 m_PendingThrowVelocity;

    /// <summary>The rigidbody the carry system pushes around.</summary>
    public Rigidbody Body => m_Rigidbody;

    /// <summary>Mass in kilograms, used for every strength comparison.</summary>
    public float Mass => m_Rigidbody != null ? m_Rigidbody.mass : 0f;

    /// <summary>False for objects that are deliberately not pickable.</summary>
    public bool CanBeGrabbed => canBeGrabbed;

    /// <summary>True while any player is holding the object.</summary>
    public bool IsHeld => m_HeldBy.Value != NoHolder;

    /// <summary>Client id of the holder, or <see cref="NoHolder"/>.</summary>
    public ulong HeldByClientId => m_HeldBy.Value;

    /// <summary>
    /// Raised on every peer when the holder changes, with the previous and the new holder.
    /// </summary>
    public event Action<ulong, ulong> HolderChanged;

    protected virtual void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_RestingCollisionDetection = m_Rigidbody.collisionDetectionMode;
    }

    public override void OnNetworkSpawn()
    {
        m_HeldBy.OnValueChanged += OnHeldByChanged;

        if (IsServer)
        {
            if (!NetworkObject.DontDestroyWithOwner)
            {
                Debug.LogWarning($"[{nameof(Grabbable)}] {name} has Don't Destroy With Owner disabled. " +
                                 "If the carrying client disconnects the object is destroyed instead of dropped. " +
                                 "Run TGNS/Setup/Make Selection Grabbable to fix it.");
            }

            NetworkManager.OnClientDisconnectCallback += OnClientDisconnect;
        }
    }

    public override void OnNetworkDespawn()
    {
        m_HeldBy.OnValueChanged -= OnHeldByChanged;

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }

        RestoreRestingPhysics();
    }

    /// <summary>
    /// Server side grab. Fails if the object is already held or not grabbable at all; mass is checked by
    /// the caller against the requesting player's strength.
    /// </summary>
    /// <param name="clientId">Client that asked to hold the object</param>
    /// <returns>True if the object is now held by that client</returns>
    internal bool ServerTryHold(ulong clientId)
    {
        if (!IsServer || !canBeGrabbed || IsHeld)
        {
            return false;
        }

        m_HeldBy.Value = clientId;
        NetworkObject.ChangeOwnership(clientId);
        return true;
    }

    /// <summary>
    /// Server side release. Ownership returns to the server so a dropped object is simulated in one place,
    /// and the throw velocity is applied there too - the releasing client is about to lose authority.
    /// </summary>
    /// <param name="throwVelocity">World space velocity to leave the object with, already clamped by the caller</param>
    internal void ServerRelease(Vector3 throwVelocity)
    {
        if (!IsServer || !IsHeld)
        {
            return;
        }

        m_HeldBy.Value = NoHolder;
        NetworkObject.RemoveOwnership();

        if (throwVelocity == Vector3.zero)
        {
            return;
        }

        if (m_Rigidbody.isKinematic)
        {
            // Ownership has moved but NetworkRigidbody has not re-enabled simulation yet; apply on the next step
            m_HasPendingThrow = true;
            m_PendingThrowVelocity = throwVelocity;
            return;
        }

        m_Rigidbody.linearVelocity = throwVelocity;
    }

    /// <summary>
    /// Physics tuning applied on the client that is actually simulating the carry. Call on grab.
    /// </summary>
    internal void BeginCarryPhysics()
    {
        // Applied from FixedUpdate rather than here: ownership arrives a round trip after the grab is
        // accepted, and until it does the body is still kinematic, where a continuous mode does not apply
        m_CarryRequested = true;
    }

    /// <summary>
    /// Undoes <see cref="BeginCarryPhysics"/>. Call on release, despawn, or when the hold is taken away.
    /// </summary>
    internal void EndCarryPhysics()
    {
        RestoreRestingPhysics();
    }

    private void FixedUpdate()
    {
        if (m_Rigidbody.isKinematic)
        {
            return;
        }

        if (m_CarryRequested && !m_CarryTweaksApplied)
        {
            m_RestingCollisionDetection = m_Rigidbody.collisionDetectionMode;
            m_Rigidbody.collisionDetectionMode = carriedCollisionDetection;
            m_CarryTweaksApplied = true;
        }

        if (m_HasPendingThrow)
        {
            m_Rigidbody.linearVelocity = m_PendingThrowVelocity;
            m_HasPendingThrow = false;
            m_PendingThrowVelocity = Vector3.zero;
        }
    }

    private void OnHeldByChanged(ulong previous, ulong current)
    {
        if (current == NoHolder)
        {
            RestoreRestingPhysics();
        }

        HolderChanged?.Invoke(previous, current);
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (!IsServer || m_HeldBy.Value != clientId)
        {
            return;
        }

        // The carrier is gone; drop the cargo where it is rather than leaving it flagged as held forever
        ServerRelease(Vector3.zero);
    }

    private void RestoreRestingPhysics()
    {
        m_CarryRequested = false;

        if (!m_CarryTweaksApplied || m_Rigidbody == null)
        {
            return;
        }

        if (!m_Rigidbody.isKinematic)
        {
            m_Rigidbody.collisionDetectionMode = m_RestingCollisionDetection;
        }

        m_CarryTweaksApplied = false;
    }
}
