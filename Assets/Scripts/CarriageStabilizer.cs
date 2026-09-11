using UnityEngine;

/// <summary>
/// Freezes chassis pitch/roll so a single-axle cart with no front support doesn't
/// tip over while it's being pulled. Unlock before parking so it settles naturally
/// (e.g. tips forward to rest on its shaft) instead of hovering level.
/// </summary>
public class CarriageStabilizer : MonoBehaviour
{
    [SerializeField] private bool rotationLocked = true;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ApplyLockState();
    }

    public void SetRotationLocked(bool locked)
    {
        rotationLocked = locked;
        ApplyLockState();
    }

    public bool IsRotationLocked => rotationLocked;

    private void ApplyLockState()
    {
        rb.constraints = rotationLocked
            ? RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ
            : RigidbodyConstraints.None;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && rb != null) ApplyLockState();
    }
#endif
}
