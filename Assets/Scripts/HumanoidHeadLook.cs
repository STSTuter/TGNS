using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Project-owned head-look driven by Unity's Humanoid Animator IK (the project does not include
/// the Animation Rigging package).
///
/// The owner computes camera-relative <c>HeadYaw</c> / <c>HeadPitch</c> and writes them as Animator
/// float parameters; <see cref="Unity.Netcode.Components.NetworkAnimator"/> replicates them, and every
/// client (owner and remote alike) applies them in <see cref="OnAnimatorIK"/> without touching local
/// input. Body influence is kept at (or near) zero so the neck-down body stays owned by locomotion;
/// the yaw is clamped relative to the body so the head never twists past the configured limit.
/// </summary>
[RequireComponent(typeof(Animator))]
public class HumanoidHeadLook : NetworkBehaviour
{
    [Header("IK weights")]
    [Range(0f, 1f)][SerializeField] private float overallWeight = 1f;
    [Range(0f, 1f)][SerializeField] private float bodyWeight = 0f;
    [Range(0f, 1f)][SerializeField] private float headWeight = 1f;
    [Range(0f, 1f)][SerializeField] private float eyesWeight = 0.4f;
    [Range(0f, 1f)][SerializeField] private float clampWeight = 0.5f;
    [Tooltip("Extra weight blended toward the neck by nudging the look target down the spine. Cosmetic only.")]
    [Range(0f, 1f)][SerializeField] private float neckWeight = 0.3f;

    [Header("Angle limits (degrees, relative to the body)")]
    [Tooltip("Head never yaws more than this away from the torso. Keep below 90.")]
    [SerializeField] private float maxHeadYaw = 85f;
    [SerializeField] private float maxHeadPitch = 60f;
    [SerializeField] private float minHeadPitch = -50f;

    [Header("Smoothing")]
    [Tooltip("SmoothDamp time applied to the applied head rotation to prevent jitter.")]
    [SerializeField] private float lookSmoothTime = 0.12f;
    [Tooltip("Distance the virtual look target is projected in front of the head.")]
    [SerializeField] private float lookDistance = 12f;

    private static readonly int HeadYawId = Animator.StringToHash("HeadYaw");
    private static readonly int HeadPitchId = Animator.StringToHash("HeadPitch");

    private Animator _animator;
    private NetworkedFirstPersonController _controller;

    private float _smoothYaw;
    private float _smoothPitch;
    private float _yawVel;
    private float _pitchVel;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<NetworkedFirstPersonController>();
    }

    private void LateUpdate()
    {
        if (!IsOwner || _controller == null)
        {
            return;
        }

        // Camera-relative head aim. Yaw is measured against the current body facing so the value
        // written here already respects the head limit; torso correction in the controller keeps
        // the raw camera yaw from ever needing more than this.
        float yaw = Mathf.Clamp(
            Mathf.DeltaAngle(transform.eulerAngles.y, _controller.CameraYaw),
            -maxHeadYaw, maxHeadYaw);
        float pitch = Mathf.Clamp(_controller.CameraPitch, minHeadPitch, maxHeadPitch);

        _animator.SetFloat(HeadYawId, yaw);
        _animator.SetFloat(HeadPitchId, pitch);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (_animator == null)
        {
            return;
        }

        // Owner: values were set this frame in LateUpdate. Remote: values arrive via NetworkAnimator.
        float targetYaw = Mathf.Clamp(_animator.GetFloat(HeadYawId), -maxHeadYaw, maxHeadYaw);
        float targetPitch = Mathf.Clamp(_animator.GetFloat(HeadPitchId), minHeadPitch, maxHeadPitch);

        _smoothYaw = Mathf.SmoothDampAngle(_smoothYaw, targetYaw, ref _yawVel, lookSmoothTime);
        _smoothPitch = Mathf.SmoothDampAngle(_smoothPitch, targetPitch, ref _pitchVel, lookSmoothTime);

        Transform head = _animator.GetBoneTransform(HumanBodyBones.Head);
        if (head == null)
        {
            return;
        }

        // Direction built relative to the body, so it can never exceed the clamped yaw above.
        Quaternion rot = Quaternion.AngleAxis(_smoothYaw, Vector3.up)
                         * Quaternion.AngleAxis(-_smoothPitch, transform.right);
        Vector3 dir = rot * FlatForward();
        Vector3 lookTarget = head.position + dir * lookDistance;

        _animator.SetLookAtWeight(overallWeight, bodyWeight, Mathf.Max(headWeight, neckWeight * 0.5f), eyesWeight, clampWeight);
        _animator.SetLookAtPosition(lookTarget);
    }

    private Vector3 FlatForward()
    {
        Vector3 f = transform.forward;
        f.y = 0f;
        return f.sqrMagnitude > 0.0001f ? f.normalized : transform.forward;
    }
}
