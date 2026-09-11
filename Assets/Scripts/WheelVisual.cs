using UnityEngine;

/// <summary>
/// Drives a visual wheel mesh from a physics WheelCollider so it rolls, steers,
/// and follows suspension travel. Attach to the WheelCollider's GameObject and
/// assign the mesh root that should track it.
/// </summary>
[RequireComponent(typeof(WheelCollider))]
public class WheelVisual : MonoBehaviour
{
    [SerializeField] private Transform visualWheel;

    private WheelCollider wheelCollider;

    private void Awake()
    {
        wheelCollider = GetComponent<WheelCollider>();
    }

    private void FixedUpdate()
    {
        if (visualWheel == null) return;

        wheelCollider.GetWorldPose(out Vector3 position, out Quaternion rotation);
        visualWheel.SetPositionAndRotation(position, rotation);
    }
}
