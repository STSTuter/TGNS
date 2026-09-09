using JohnStairs.RPG.Character.Motor.Enums;
using JohnStairs.RPG.Combat.Abilities.Enums;
using UnityEngine;

namespace JohnStairs.RPG.Character {
    [RequireComponent(typeof(Animator))]
    public class AnimationHandler : MonoBehaviour, IAnimationHandler {
        public string MeleeCastTrigger = "Melee Cast";
        public string MeleeCastFinishedTrigger = "Melee Cast Finished";
        public string RangedWeaponCastTrigger = "Ranged Weapon Cast";
        public string RangedWeaponCastFinishedTrigger = "Ranged Weapon Cast Finished";
        public string CastAggressiveTrigger = "Cast Aggressive";
        public string CastAggressiveFinishedTrigger = "Cast Aggressive Finished";
        public string CastDefensiveTrigger = "Cast Defensive";
        public string CastDefensiveFinishedTrigger = "Cast Defensive Finished";

        protected Animator _animator;
        protected IMountAnimationHandler _mountAnimationHandler;
        protected float _upperBodyLayerWeight;
        protected float _upperBodyLayerWeightVelocity;

        protected static readonly int _movingForwardId = Animator.StringToHash("Moving Forward");
        protected static readonly int _strafingId = Animator.StringToHash("Strafing");
        protected static readonly int _turningId = Animator.StringToHash("Turning");
        protected static readonly int _movementSpeedId = Animator.StringToHash("Movement Speed");
        protected static readonly int _jumpingId = Animator.StringToHash("Jumping");
        protected static readonly int _groundedId = Animator.StringToHash("Grounded");
        protected static readonly int _slidingId = Animator.StringToHash("Sliding");
        protected static readonly int _fallingId = Animator.StringToHash("Falling");
        protected static readonly int _flyingId = Animator.StringToHash("Flying");
        protected static readonly int _swimmingId = Animator.StringToHash("Swimming");
        protected static readonly int _climbingStateId = Animator.StringToHash("Climbing State");
        protected static readonly int _climbingUpId = Animator.StringToHash("Climbing Up");
        protected static readonly int _crouchingId = Animator.StringToHash("Crouching");

        protected virtual void Awake() {
            _animator = GetComponent<Animator>();
        }

        protected virtual void Start() {
        }

        protected virtual void Update() {
            _mountAnimationHandler = GetComponentInChildren<IMountAnimationHandler>();

            _upperBodyLayerWeight = Mathf.SmoothDamp(_upperBodyLayerWeight, IsInMotion() || HasThreeDimensionalMovement() ? 1.0f : 0, ref _upperBodyLayerWeightVelocity, 0.1f);

            _animator.SetLayerWeight(1, 1.0f - _upperBodyLayerWeight);
            _animator.SetLayerWeight(2, _upperBodyLayerWeight);
        }

        protected virtual bool IsInMotion() {
            return !Utils.IsAlmostEqual(_animator.GetFloat(_movingForwardId), 0, 0.1f) ||
                   !Utils.IsAlmostEqual(_animator.GetFloat(_strafingId), 0, 0.1f);
        }

        protected virtual bool HasThreeDimensionalMovement() {
            return _animator.GetBool(_flyingId) || _animator.GetBool(_swimmingId);
        }

        public virtual void Sliding(bool sliding) {
            _animator.SetBool(_slidingId, sliding);
            _mountAnimationHandler?.Sliding(sliding);
        }

        public virtual void Falling(bool falling) {
            _animator.SetBool(_fallingId, falling);
            _mountAnimationHandler?.Falling(falling);
        }

        public virtual void Flying(bool flying) {
            _animator.SetBool(_flyingId, flying);
            _mountAnimationHandler?.Flying(flying);
        }

        public virtual void Swimming(bool swimming) {
            _animator.SetBool(_swimmingId, swimming);
            _mountAnimationHandler?.Swimming(swimming);
        }

        public virtual void ClimbingState(ClimbingState climbingState) {
            _animator.SetInteger(_climbingStateId, (int)climbingState);
        }

        public virtual void ClimbingUp(float up) {
            up = Mathf.Clamp(up, -1.0f, 1.0f);
            _animator.SetFloat(_climbingUpId, up, 0.1f, Time.deltaTime);
        }

        public virtual void Crouching(bool crouching) {
            _animator.SetBool(_crouchingId, crouching);
        }

        public virtual void Grounded(bool grounded) {
            _animator.SetBool(_groundedId, grounded);
            _mountAnimationHandler?.Grounded(grounded);
        }

        public virtual void Jump() {
            _animator.SetTrigger(_jumpingId);
            _mountAnimationHandler?.Jump();
        }

        public virtual void MovingForward(float forward) {
            _animator.SetFloat(_movingForwardId, forward, 0.1f, Time.deltaTime);
            _mountAnimationHandler?.MovingForward(forward);
        }

        public virtual void Strafing(float strafing) {
            _animator.SetFloat(_strafingId, strafing, 0.1f, Time.deltaTime);
            _mountAnimationHandler?.Strafing(strafing);
        }

        public virtual void Turning(float turning) {
            _animator.SetFloat(_turningId, turning, 0.1f, Time.deltaTime);
            _mountAnimationHandler?.Turning(turning);
        }

        public virtual void SetMovementSpeed(float speed) {
            _animator.SetFloat(_movementSpeedId, speed);
            _mountAnimationHandler?.SetMovementSpeed(speed);
        }

        public virtual void Cast(AbilityAnimationFlow animationFlow) {
            switch (animationFlow) {
                case AbilityAnimationFlow.Melee:
                    _animator.SetTrigger(MeleeCastTrigger);
                    break;
                case AbilityAnimationFlow.RangedWeapon:
                    _animator.SetTrigger(RangedWeaponCastTrigger);
                    break;
                case AbilityAnimationFlow.CastAggressive:
                    _animator.SetTrigger(CastAggressiveTrigger);
                    break;
                case AbilityAnimationFlow.CastDefensive:
                    _animator.SetTrigger(CastDefensiveTrigger);
                    break;
            }
        }

        public virtual void FinishCast(AbilityAnimationFlow animationFlow) {
            switch (animationFlow) {
                case AbilityAnimationFlow.Melee:
                    _animator.SetTrigger(MeleeCastFinishedTrigger);
                    break;
                case AbilityAnimationFlow.RangedWeapon:
                    _animator.SetTrigger(RangedWeaponCastFinishedTrigger);
                    break;
                case AbilityAnimationFlow.CastAggressive:
                    _animator.SetTrigger(CastAggressiveFinishedTrigger);
                    break;
                case AbilityAnimationFlow.CastDefensive:
                    _animator.SetTrigger(CastDefensiveFinishedTrigger);
                    break;
            }
        }

        public virtual void Cancel() {
            _animator.SetTrigger("Cancel");
        }

        public virtual void Die() {
            _animator.SetBool("Dead", true);
        }
    }
}
