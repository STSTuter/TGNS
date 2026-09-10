using JohnStairs.RPG.Character;
using JohnStairs.RPG.Character.Motor;
using JohnStairs.RPG.Character.Motor.Enums;
using JohnStairs.RPG.Combat.Abilities.Enums;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Project-owned <see cref="IAnimationHandler"/> that feeds the simplified NetworkPlayer
/// Animator (Grounded / Crouched / Jump / Falling with nested blend trees).
///
/// It only consumes the callbacks <see cref="RPGMotor"/> already pushes every frame - it does
/// not run its own ground checks, does not use OnCollisionEnter, and does not add a Rigidbody.
/// The vendor motor stays the single source of truth for collision and grounded detection.
///
/// Only the owner writes parameters. Remote replicas receive them through <see cref="NetworkAnimator"/>
/// (RPGMotor is disabled on replicas, so the setters below never even run there).
/// </summary>
[RequireComponent(typeof(Animator))]
public class NetworkPlayerAnimationDriver : NetworkBehaviour, IAnimationHandler
{
    [Header("Smoothing")]
    [Tooltip("Damp time for the MoveX / MoveY directional parameters.")]
    [SerializeField] private float moveDamp = 0.1f;
    [Tooltip("Damp time for the normalized MovementSpeed parameter.")]
    [SerializeField] private float speedDamp = 0.12f;

    [Header("Speed normalization (falls back to RPGMotor fields when zero)")]
    [Tooltip("Speed mapped to MovementSpeed = 0.5 (walk). 0 = read RPGMotor.WalkSpeed.")]
    [SerializeField] private float walkSpeedOverride = 0f;
    [Tooltip("Speed mapped to MovementSpeed = 1 (run). 0 = read RPGMotor.DefaultSpeed * RPGMotor.SprintSpeedMultiplier.")]
    [SerializeField] private float runSpeedOverride = 0f;

    private static readonly int MoveXId = Animator.StringToHash("MoveX");
    private static readonly int MoveYId = Animator.StringToHash("MoveY");
    private static readonly int MovementSpeedId = Animator.StringToHash("MovementSpeed");
    private static readonly int GroundedId = Animator.StringToHash("Grounded");
    private static readonly int CrouchedId = Animator.StringToHash("Crouched");
    private static readonly int FallingId = Animator.StringToHash("Falling");
    private static readonly int JumpingId = Animator.StringToHash("Jumping");

    private Animator _animator;
    private NetworkAnimator _networkAnimator;
    private RPGMotor _motor;

    private float _moveX;
    private float _moveY;
    private float _rawSpeed;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _networkAnimator = GetComponent<NetworkAnimator>();
        _motor = GetComponent<RPGMotor>();
    }

    // LateUpdate so RPGMotor (execution order 100) has already pushed this frame's callbacks.
    private void LateUpdate()
    {
        if (!IsOwner)
        {
            return;
        }

        _animator.SetFloat(MoveXId, _moveX, moveDamp, Time.deltaTime);
        _animator.SetFloat(MoveYId, _moveY, moveDamp, Time.deltaTime);
        _animator.SetFloat(MovementSpeedId, NormalizeSpeed(_rawSpeed), speedDamp, Time.deltaTime);
    }

    private float NormalizeSpeed(float speed)
    {
        float walk = walkSpeedOverride > 0f ? walkSpeedOverride
            : _motor != null ? _motor.WalkSpeed : 2f;
        float run = runSpeedOverride > 0f ? runSpeedOverride
            : _motor != null ? _motor.DefaultSpeed * _motor.SprintSpeedMultiplier : 8f;

        if (run <= walk)
        {
            run = walk + 1f;
        }

        if (speed <= walk)
        {
            return Mathf.InverseLerp(0f, walk, speed) * 0.5f;
        }

        return 0.5f + 0.5f * Mathf.Clamp01(Mathf.InverseLerp(walk, run, speed));
    }

    // ----- IAnimationHandler: locomotion -----

    public void MovingForward(float forward)
    {
        _moveY = forward;
    }

    public void Strafing(float strafing)
    {
        _moveX = strafing;
    }

    public void SetMovementSpeed(float speed)
    {
        _rawSpeed = speed;
    }

    public void Grounded(bool grounded)
    {
        if (IsOwner)
        {
            _animator.SetBool(GroundedId, grounded);
        }
    }

    public void Falling(bool falling)
    {
        if (IsOwner)
        {
            _animator.SetBool(FallingId, falling);
        }
    }

    public void Crouching(bool crouching)
    {
        if (IsOwner)
        {
            _animator.SetBool(CrouchedId, crouching);
        }
    }

    public void Jump()
    {
        if (IsOwner)
        {
            // Triggers must go through NetworkAnimator so remote clients receive them.
            _networkAnimator.SetTrigger(JumpingId);
        }
    }

    // ----- IAnimationHandler: out of scope for the locomotion pass (intentionally no-ops) -----

    public void Turning(float turning) { }
    public void Sliding(bool sliding) { }
    public void Flying(bool flying) { }
    public void Swimming(bool swimming) { }
    public void ClimbingState(ClimbingState climbingState) { }
    public void ClimbingUp(float up) { }
    public void Cast(AbilityAnimationFlow animationFlow) { }
    public void FinishCast(AbilityAnimationFlow animationFlow) { }
    public void Cancel() { }
    public void Die() { }
}
