using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Per-player carrying strength. Everything the carry system is allowed to do to a grabbed rigidbody is
/// derived from one number, <see cref="LiftCapacity"/>: the mass in kilograms the player can hold up
/// against gravity.
///
/// Because the hold is force limited rather than mass gated, the "struggle" is emergent. An object at
/// half the lift capacity snaps to the aim point; one just under it can be held but accelerates slowly;
/// one above it cannot be lifted off the ground at all and is dragged instead.
///
/// The capacity is a server-written network variable so progression, buffs or equipment can raise it
/// later without touching the carry code. The server is the one that validates grab requests, so it must
/// be the writer.
/// </summary>
public class PlayerStrength : NetworkBehaviour
{
    [Header("Strength")]
    [Tooltip("Mass in kilograms this player can hold up against gravity. Objects at or below this are carried at the aim point; heavier ones sag and drag.")]
    [SerializeField] private float liftCapacity = 40f;

    [Tooltip("Multiple of the lift capacity for the heaviest object that can still be gripped at all. Between lift capacity and this the object can be dragged but not lifted.")]
    [SerializeField] private float dragCapacityMultiplier = 3f;

    [Tooltip("Force available on top of the dead weight of an object at exactly lift capacity. 1 means the player can just barely hold it still; higher values make carrying snappier.")]
    [SerializeField] private float forceHeadroom = 2.5f;

    [Tooltip("Angular acceleration in rad/s^2 available for turning an object at exactly lift capacity. Lighter objects turn proportionally faster.")]
    [SerializeField] private float turnAcceleration = 30f;

    [Tooltip("Throw impulse budget in kg*m/s per kilogram of lift capacity. The throw speed is this budget divided by the object's mass, so heavy objects barely leave the hands.")]
    [SerializeField] private float throwImpulsePerCapacity = 0.6f;

    private readonly NetworkVariable<float> m_LiftCapacity = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// Mass in kilograms the player can hold up against gravity.
    /// </summary>
    public float LiftCapacity => m_LiftCapacity.Value > 0f ? m_LiftCapacity.Value : liftCapacity;

    /// <summary>
    /// Heaviest object that can be gripped at all. Anything above this refuses the grab outright.
    /// </summary>
    public float MaxGrabMass => LiftCapacity * Mathf.Max(1f, dragCapacityMultiplier);

    /// <summary>
    /// Newtons available to move a held object, including the force spent holding it up.
    /// </summary>
    public float MaxHoldForce => LiftCapacity * Physics.gravity.magnitude * Mathf.Max(1f, forceHeadroom);

    /// <summary>
    /// Angular acceleration available for turning an object of the given mass, in rad/s^2.
    /// </summary>
    /// <param name="mass">Mass of the held object in kilograms</param>
    /// <returns>Angular acceleration budget for that mass</returns>
    public float GetMaxTurnAcceleration(float mass)
    {
        return turnAcceleration * Mathf.Clamp01(LiftCapacity / Mathf.Max(mass, 0.01f));
    }

    /// <summary>
    /// Fastest the player can throw an object of the given mass, in m/s.
    /// </summary>
    /// <param name="mass">Mass of the thrown object in kilograms</param>
    /// <returns>Maximum throw speed for that mass</returns>
    public float GetMaxThrowSpeed(float mass)
    {
        return LiftCapacity * throwImpulsePerCapacity / Mathf.Max(mass, 0.01f);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && m_LiftCapacity.Value <= 0f)
        {
            m_LiftCapacity.Value = liftCapacity;
        }
    }

    /// <summary>
    /// Sets the player's lift capacity. Server only; clients receive it through the network variable.
    /// </summary>
    /// <param name="kilograms">New lift capacity in kilograms</param>
    public void ServerSetLiftCapacity(float kilograms)
    {
        if (!IsServer)
        {
            Debug.LogWarning($"[{nameof(PlayerStrength)}] Only the server may change lift capacity.");
            return;
        }

        m_LiftCapacity.Value = Mathf.Max(0.01f, kilograms);
    }
}
