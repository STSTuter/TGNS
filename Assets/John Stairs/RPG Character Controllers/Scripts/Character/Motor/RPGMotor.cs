using System.Collections;
using JohnStairs.RPG.Character.Motor.Enums;
using JohnStairs.RPG.Character.Motor.Subcomponents;
using UnityEngine;

namespace JohnStairs.RPG.Character.Motor {
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(CharacterController))]
    public class RPGMotor : MonoBehaviour, IMotor, ITransportable {
        /// <summary>
        /// Layers that are considered walkable for ground checks and movement
        /// </summary>
        [Tooltip("Layers that are considered walkable for ground checks and movement")]
        public LayerMask WalkableLayers = 195; // Default + TransparentFX + Ground + Climbable
        /// <summary>
        /// Tolerance distance for determining if the character is grounded
        /// </summary>
        [Tooltip("Tolerance distance for determining if the character is grounded")]
        public float GroundedTolerance = 0.16f;
        /// <summary>
        /// Default movement speed when not walking, crouching, or sprinting
        /// </summary>
        [Tooltip("Default movement speed when not walking, crouching, or sprinting")]
        public float DefaultSpeed = 4.0f;
        /// <summary>
        /// Movement speed when strafing sideways
        /// </summary>
        [Tooltip("Movement speed when strafing sideways")]
        public float StrafeSpeed = 4.0f;
        /// <summary>
        /// Multiplier applied to speed when moving backwards
        /// </summary>
        [Tooltip("Multiplier applied to speed when moving backwards")]
        public float BackwardsSpeedMultiplier = 0.5f;
        /// <summary>
        /// Multiplier applied to speed when sprinting
        /// </summary>
        [Tooltip("Multiplier applied to speed when sprinting")]
        public float SprintSpeedMultiplier = 2.0f;
        /// <summary>
        /// Movement speed when walking
        /// </summary>
        [Tooltip("Movement speed when walking")]
        public float WalkSpeed = 2.0f;
        /// <summary>
        /// Movement speed when crouching
        /// </summary>
        [Tooltip("Movement speed when crouching")]
        public float CrouchSpeed = 1.5f;
        /// <summary>
        /// Movement speed when flying
        /// </summary>
        [Tooltip("Movement speed when flying")]
        public float FlyingSpeed = 10.0f;
        /// <summary>
        /// Movement speed change per second
        /// </summary>
        [Tooltip("Movement speed change per second")]
        public float MovementSpeedChangeRate = 20.0f;
        /// <summary>
        /// Speed of turning (horizontal rotation) in degrees per second
        /// </summary>
        [Tooltip("Speed of turning (horizontal rotation) in degrees per second")]
        public float RotationSpeed = 180.0f;
        /// <summary>
        /// Smooth time for rotating the character to the facing direction
        /// </summary>
        [Tooltip("Smooth time for rotating the character to the facing direction")]
        public float AlignmentTime = 0.01f;
        /// <summary>
        /// Height of the jump in world units
        /// </summary>
        [Tooltip("Height of the jump in world units")]
        public float JumpHeight = 1.0f;
        /// <summary>
        /// If set to true, jumps while the character is in midair are allowed. Used in combination with variable "AllowedMidairJumps"
        /// </summary>
        [Tooltip("If set to true, jumps while the character is in midair are allowed. Used in combination with variable \"Allowed Midair Jumps\"")]
        public bool EnableMidairJumps = true;
        /// <summary>
        /// The number of allowed jumps while the character is in midair/airborne, e.g. use value 1 for allowing double jumps
        /// </summary>
        [Tooltip("The number of allowed jumps while the character is in midair/airborne, e.g. use value 1 for allowing double jumps")]
        public int AllowedMidairJumps = 1;
        /// <summary>
        /// If true, allows movement while in midair
        /// </summary>
        [Tooltip("If true, allows movement while in midair")]
        public bool AllowMidairMovement = true;
        /// <summary>
        /// If set to true, the jump direction is affected by the current carrier object, i.e. performing a standing jump on a moving carrier lets the character always land on the same spot
        /// </summary>
        [Tooltip("If set to true, the jump direction is affected by the current carrier object, i.e. performing a standing jump on a moving carrier lets the character always land on the same spot")]
        public bool CarrierAffectsJumps = true;
        /// <summary>
        /// Gravity applied to the character
        /// </summary>
        [Tooltip("Gravity applied to the character")]
        public float Gravity = -9.81f;
        /// <summary>
        /// Strength of downward force to keep character stuck to the ground
        /// </summary>
        [Tooltip("Strength of downward force to keep character stuck to the ground")]
        public float GroundStickiness = 6.0f;

        /// <summary>
        /// Reference to the CharacterController component
        /// </summary>
        protected CharacterController _characterController;
        /// <summary>
        /// Handler for sliding mechanics
        /// </summary>
        protected ISlidingHandler _slidingHandler;
        /// <summary>
        /// Handler for swimming mechanics
        /// </summary>
        protected ISwimmingHandler _swimmingHandler;
        /// <summary>
        /// Handler for climbing mechanics
        /// </summary>
        protected IClimbingHandler _climbingHandler;
        /// <summary>
        /// Handler for animation control
        /// </summary>
        protected IAnimationHandler _animationHandler;
        /// <summary>
        /// Current type of movement (e.g., grounded, midair)
        /// </summary>
        protected MovementType _movementType;
        /// <summary>
        /// Input movement vector in world coordinates
        /// </summary>
        protected Vector3 _inputMovementVector;
        /// <summary>
        /// Current movement vector applied to the character
        /// </summary>
        protected Vector3 _movementVector;
        /// <summary>
        /// Current horizontal movement speed
        /// </summary>
        protected float _movementSpeed;
        /// <summary>
        /// External multiplier for movement speed
        /// </summary>
        protected float _externalMovementSpeedMultiplier;
        /// <summary>
        /// Desired facing direction from input
        /// </summary>
        protected FacingDirection _inputFacingDirection;
        /// <summary>
        /// Target rotation for the character
        /// </summary>
        protected Quaternion _targetRotation;
        /// <summary>
        /// Direction and amount of turning in the current frame
        /// </summary>
        protected float _turningDirection;
        /// <summary>
        /// Current rotation velocity for turning the character
        /// </summary>
        protected float _rotationVelocity;
        /// <summary>
        /// True if the character is currently grounded
        /// </summary>
        protected bool _grounded;
        /// <summary>
        /// True if the character should walk
        /// </summary>
        protected bool _walking;
        /// <summary>
        /// True if the character should sprint
        /// </summary>
        protected bool _sprinting;
        /// <summary>
        /// True if the character should crouch
        /// </summary>
        protected bool _crouching;
        /// <summary>
        /// True if the character could swim
        /// </summary>
        protected bool _couldSwim;
        /// <summary>
        /// True if the character is currently not allowed to swim, e.g. because of a recent jump near the water surface
        /// </summary>
        protected bool _swimmingTimeout;
        /// <summary>
        /// Climbing state of the character
        /// </summary>
        protected ClimbingState _climbingState;
        /// <summary>
        /// True if autorunning is enabled
        /// </summary>
        protected bool _autorunning;
        /// <summary>
        /// True if jump input was received this frame
        /// </summary>
        protected bool _inputJump;
        /// <summary>
        /// Number of already performed jumps during the current midair period
        /// </summary>
        protected int _midairJumpsCount = 0;
        /// <summary>
        /// If true, the character is able to fly (e.g. has flying ability enabled)
        /// </summary>
        protected bool _canFly = false;
        /// <summary>
        /// True if the character is sliding
        /// </summary>
        protected bool _sliding = false;

        protected virtual void Awake() {
            _characterController = GetComponent<CharacterController>();
            _slidingHandler = GetComponent<ISlidingHandler>();
            _swimmingHandler = GetComponent<ISwimmingHandler>();
            _climbingHandler = GetComponent<IClimbingHandler>();
            _animationHandler = GetComponent<IAnimationHandler>();
        }

        protected virtual void Start() {
            _inputFacingDirection = new FacingDirection(transform.forward, Vector3.up);
            _targetRotation = transform.rotation;
            _slidingHandler?.Init(WalkableLayers, GroundedTolerance);
            ResetMovementSpeedMultiplier();
        }

        protected virtual void Update() {
            _couldSwim = CheckSwimming(_characterController.GetBottomSphereCenter());
            _climbingState = GetClimbingState();
            _grounded = !IsSwimming() && CheckGrounded();

            _movementType = DetermineMovementType();

            switch (_movementType) {
                case MovementType.Grounded:
                    GroundedMovement();
                    break;
                case MovementType.Midair:
                    MidairMovement();
                    break;
                case MovementType.Flying:
                    Flying();
                    break;
                case MovementType.Swimming:
                    Swimming();
                    break;
                case MovementType.Climbing:
                    Climbing();
                    break;
                default:
                    break;
            }

            _characterController.Move(_movementVector * Time.deltaTime);

            RotateToTargetRotation();

            Animate();

            _inputJump = false;
            _turningDirection = 0;
        }

        protected virtual void GroundedMovement() {
            _midairJumpsCount = 0;
            _sliding = CheckSliding(out Vector3 slidingVector);

            if (_sliding) {
                _movementVector = slidingVector;
            } else {
                float verticalVelocity = _movementVector.y;

                if (verticalVelocity < 0) {
                    verticalVelocity = -GroundStickiness;
                }

                if (_inputJump) {
                    verticalVelocity = PerformJump();
                }

                _movementVector = GetGroundedMovementVector(verticalVelocity);
            }

            ApplyGravity(ref _movementVector);

            _targetRotation = GetRotationFromFacingDirection(_inputFacingDirection, Projection.OnHorizontalPlane);
        }

        protected virtual void MidairMovement() {
            if (AllowMidairMovement) {
                _movementVector = GetGroundedMovementVector(_movementVector.y);
            }

            if (_inputJump && EnableMidairJumps && _midairJumpsCount < AllowedMidairJumps) {
                _movementVector.y = PerformJump();
                _midairJumpsCount++;
            }

            ApplyGravity(ref _movementVector);

            _targetRotation = GetRotationFromFacingDirection(_inputFacingDirection, Projection.OnHorizontalPlane);
        }

        protected virtual void Flying() {
            ThreeDimensionalMovement(FlyingSpeed);
        }

        protected virtual void Swimming() {
            ThreeDimensionalMovement(_swimmingHandler?.GetSwimSpeed() ?? DefaultSpeed);

            if (IsNearWaterSurface()) {
                _movementVector = PreventUpwardMovement(_movementVector);

                if (_inputJump) {
                    float verticalVelocity = PerformJump();
                    _movementVector.y = verticalVelocity;
                    StartCoroutine(SwimmingTimeout());
                }
            }
        }

        protected virtual void ThreeDimensionalMovement(float maxSpeed) {
            float currentSpeed = _characterController.velocity.magnitude;
            float targetMovementSpeed = maxSpeed * Mathf.Clamp01(_inputMovementVector.magnitude);

            float movementSpeed = Mathf.Lerp(currentSpeed, targetMovementSpeed, Time.deltaTime * MovementSpeedChangeRate);

            _movementVector = _inputMovementVector.normalized * movementSpeed;

            Projection appliedProjection = Projection.None;
            if (_inputMovementVector == Vector3.zero
                || IsSwimming() && IsNearWaterSurface() && _movementVector.y > 0) {
                appliedProjection = Projection.OnHorizontalPlane;
            }
            _targetRotation = GetRotationFromFacingDirection(_inputFacingDirection, appliedProjection);
        }

        protected virtual void Climbing() {
            if (_climbingHandler != null) {
                _movementVector = _climbingHandler.GetMovementVector(_inputMovementVector, IsGrounded() || IsSwimming());
                _targetRotation = GetRotationFromFacingDirection(_climbingHandler.GetFacingDirection(), Projection.None);
            }
        }

        protected virtual MovementType DetermineMovementType() {
            if (IsClimbing()) {
                return MovementType.Climbing;
            } else if (IsSwimming()) {
                return MovementType.Swimming;
            } else if (IsFlying()) {
                return MovementType.Flying;
            } else if (IsGrounded()) {
                return MovementType.Grounded;
            } else {
                return MovementType.Midair;
            }
        }

        public virtual MovementType GetMovementType() {
            return _movementType;
        }

        protected virtual bool CheckGrounded() {
            if (_movementVector.y < 0) {
                Vector3 sphereCenter = _characterController.GetBottomSphereCenter();
                return Physics.CheckSphere(sphereCenter + Vector3.down * GroundedTolerance, _characterController.GetActualRadius(), WalkableLayers, QueryTriggerInteraction.Ignore);
            } else {
                return false;
            }
        }

        protected virtual bool IsGrounded() {
            return _grounded;
        }

        protected virtual bool IsSliding() {
            return _sliding;
        }

        protected virtual bool IsFalling() {
            return !IsGrounded() && !IsSwimming() && !IsFlying() && !IsClimbing() && _characterController.velocity.y < 0;
        }

        protected virtual bool IsFlying() {
            return !IsGrounded() && _canFly;
        }

        protected virtual bool IsSwimming() {
            return _couldSwim && !_swimmingTimeout;
        }

        protected virtual bool IsClimbing() {
            return _climbingState != ClimbingState.None;
        }

        protected virtual bool CheckSliding(out Vector3 slidingVector) {
            slidingVector = Vector3.zero;
            return _slidingHandler?.CheckSliding(out slidingVector) ?? false;
        }

        protected virtual bool CheckSwimming(Vector3 colliderPosition) {
            return _swimmingHandler?.CheckSwimming(colliderPosition) ?? false;
        }

        protected virtual bool IsNearWaterSurface() {
            return _swimmingHandler?.IsNearWaterSurface() ?? false;
        }

        protected virtual Vector3 PreventUpwardMovement(Vector3 movementVector) {
            float magnitude = movementVector.magnitude;
            movementVector.y = Mathf.Min(0, movementVector.y);
            return movementVector.normalized * magnitude;
        }

        protected virtual IEnumerator SwimmingTimeout() {
            _swimmingTimeout = true;
            yield return new WaitForSeconds(0.5f);
            _swimmingTimeout = false;
        }

        protected virtual ClimbingState GetClimbingState() {
            return _climbingHandler == null ? ClimbingState.None : _climbingHandler.GetClimbingState();
        }

        public virtual bool IsInMotion() {
            return _characterController.velocity != Vector3.zero;
        }

        protected virtual Quaternion GetRotationFromFacingDirection(FacingDirection facingDirection, Projection projection) {
            Vector3 forward = facingDirection.Forward;
            Vector3 up;

            if (projection == Projection.OnHorizontalPlane) {
                forward.y = 0;
                up = Vector3.up;
            } else {
                up = Vector3.Normalize(facingDirection.Up);
            }

            if (forward == Vector3.zero) {
                return Quaternion.LookRotation(Vector3.Cross(transform.right, up), up);
            } else {
                return Quaternion.LookRotation(forward, up);
            }
        }

        protected virtual float GetHorizontalVelocity() {
            return new Vector2(_characterController.velocity.x, _characterController.velocity.z).magnitude;
        }

        protected virtual float GetGroundedTargetMovementSpeed() {
            float movementSpeed;
            if (_walking) {
                movementSpeed = WalkSpeed;
            } else if (_crouching) {
                movementSpeed = CrouchSpeed;
            } else {
                Vector3 localMovementVector = transform.InverseTransformDirection(_inputMovementVector);
                float speed = DefaultSpeed;
                float xWeight = Mathf.Abs(localMovementVector.x);
                float zWeight = Mathf.Abs(localMovementVector.z);
                float totalWeight = xWeight + zWeight;

                if (totalWeight > 0) {
                    speed = (StrafeSpeed * xWeight + DefaultSpeed * zWeight) / totalWeight;
                }

                if (localMovementVector.z < -0.05f) {
                    speed *= BackwardsSpeedMultiplier;
                } else if (_sprinting) {
                    speed *= SprintSpeedMultiplier;
                }
                movementSpeed = speed;
            }
            return movementSpeed * _externalMovementSpeedMultiplier;
        }

        protected virtual void StopClimbing() {
            _climbingHandler?.StopClimbing();
            _movementVector = Vector3.zero;
        }

        protected virtual float LerpMovementSpeed(float targetMovementSpeed) {
            return Mathf.Lerp(GetHorizontalVelocity(), targetMovementSpeed * Mathf.Clamp01(_inputMovementVector.magnitude), Time.deltaTime * MovementSpeedChangeRate);
        }

        protected virtual Vector3 GetGroundedMovementVector(float verticalVelocity) {
            float targetMovementSpeed = GetGroundedTargetMovementSpeed();
            _movementSpeed = LerpMovementSpeed(targetMovementSpeed);
            return new Vector3(_inputMovementVector.x, 0, _inputMovementVector.z).normalized * _movementSpeed + Vector3.up * verticalVelocity;
        }

        protected virtual float PerformJump() {
            _animationHandler?.Jump();
            return CalculateJumpHeight();
        }

        protected virtual float CalculateJumpHeight() {
            return Mathf.Sqrt(-2.0f * JumpHeight * Gravity);
        }

        protected virtual void ApplyGravity(ref Vector3 movementVector) {
            movementVector.y += Gravity * Time.deltaTime;
        }

        public virtual void Move(Vector3 movementVector) {
            _inputMovementVector = movementVector;
        }

        public virtual void TurnInDirection(Vector3 forward, Vector3 up) {
            _inputFacingDirection = new FacingDirection(forward, up);
        }

        public virtual float Turn(float degrees) {
            _turningDirection = degrees;
            float rotatedDegrees = degrees * RotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up, rotatedDegrees, Space.World);
            return rotatedDegrees;
        }

        public virtual void Jump() {
            _inputJump = true;
        }

        public virtual void Sprint(bool on) {
            _sprinting = on;
        }

        public virtual void ToggleCrouching() {
            if (IsGrounded()) {
                _crouching = !_crouching;
            }
        }

        public virtual void ToggleWalking() {
            if (IsGrounded()) {
                _walking = !_walking;
            }
        }

        public virtual void ToggleFlyingAbility() {
            _canFly = !_canFly;
        }

        protected virtual void RotateToTargetRotation() {
            if (!HasTargetRotation()) {
                float turningDirection = transform.rotation.eulerAngles.y;

                // Smoothly rotate to the target rotation
                float delta = Quaternion.Angle(transform.rotation, _targetRotation);
                if (delta >= 180.0f) {
                    // Decrease delta under 180 so that SmoothDampAngle decreases
                    delta = 179.0f;
                }
                float t = Mathf.SmoothDampAngle(delta, 0, ref _rotationVelocity, AlignmentTime);
                t = 1.0f - t / delta;
                transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, t);

                _turningDirection = Mathf.DeltaAngle(turningDirection, transform.rotation.eulerAngles.y);
            }
        }

        protected virtual bool HasTargetRotation() {
            return Quaternion.Angle(transform.rotation, _targetRotation) < 0.1f;
        }

        protected virtual void Animate() {
            if (_animationHandler != null) {
                Vector3 movementVectorLocal = transform.InverseTransformDirection(_movementVector);
                movementVectorLocal = NormalizeVectorOnHorizontalPlane(movementVectorLocal);
                _animationHandler.MovingForward(movementVectorLocal.z);
                _animationHandler.Strafing(movementVectorLocal.x);
                _animationHandler.Turning(_turningDirection);
                _animationHandler.SetMovementSpeed(_movementSpeed);
                _animationHandler.Grounded(IsGrounded());
                _animationHandler.Sliding(IsGrounded() && IsSliding());
                _animationHandler.Falling(IsFalling());
                _animationHandler.Flying(IsFlying());
                _animationHandler.Swimming(IsSwimming());
                _animationHandler.ClimbingState(_climbingState);
                _animationHandler.ClimbingUp(_movementVector.y);
                _animationHandler.Crouching(_crouching);
            }
        }

        protected virtual Vector3 NormalizeVectorOnHorizontalPlane(Vector3 vector) {
            vector.x = Mathf.Abs(vector.x) < 0.05f ? 0f : vector.x;
            vector.y = 0;
            vector.z = Mathf.Abs(vector.z) < 0.05f ? 0f : vector.z;
            vector.Normalize();
            return vector;
        }

        public virtual void Teleport(Vector3 position, Quaternion rotation) {
            if (!Time.inFixedTimeStep) {
                Debug.LogWarning("Method \"Teleport\" was called outside of FixedUpdate! This can lead to unexpected side effects (double-click here for more information)");
                // Not called from within FixedUpdate => disable the character controller component so that it does not overwrite the target position
                // The disadvantage is that collision detection callbacks are called again (OnTriggerEnter) or never (OnTriggerExit)
                _characterController.enabled = false;
            }

            transform.SetPositionAndRotation(position, rotation);

            if (!Time.inFixedTimeStep) {
                // Enable the character controller again
                _characterController.enabled = true;
            }
        }

        public virtual void SetMovementSpeedMultiplier(float multiplier) {
            _externalMovementSpeedMultiplier = multiplier;
        }

        public virtual void ResetMovementSpeedMultiplier() {
            _externalMovementSpeedMultiplier = 1.0f;
        }

        public virtual Transform GetTransform() {
            return transform;
        }

        public virtual float GetColliderRadius() {
            return _characterController.GetActualRadius();
        }

        public virtual bool JumpsAffectedByCarrier() {
            return CarrierAffectsJumps;
        }

        public virtual void Reset() {
            _inputMovementVector = Vector3.zero;
            _movementVector = Vector3.zero;
            _movementSpeed = 0f;
            ResetMovementSpeedMultiplier();
            _inputFacingDirection = new FacingDirection(transform.forward, Vector3.up);
            _targetRotation = transform.rotation;
            _turningDirection = 0f;
            _rotationVelocity = 0f;
            _grounded = false;
            _walking = false;
            _sprinting = false;
            _crouching = false;
            _couldSwim = false;
            _swimmingHandler?.Reset();
            _swimmingTimeout = false;
            _autorunning = false;
            _inputJump = false;
            _midairJumpsCount = 0;
            _canFly = false;
            _sliding = false;
            _movementType = MovementType.Grounded;
            _climbingState = ClimbingState.None;
            StopClimbing();
        }

        public virtual bool IsEnabled() {
            return enabled;
        }

        public virtual void Destroy() {
            foreach (var subcomponent in GetComponents<IMotorSubcomponent>()) {
                if (subcomponent is Component component) {
                    Destroy(component);
                }
            }
            Destroy(this);
        }

        /// <summary>
        /// If Gizmos are enabled, this method draws some debugging gizmos
        /// </summary>
        protected virtual void OnDrawGizmosSelected() {
            Color green = new(0.0f, 1.0f, 0.0f, 0.35f);
            Color yellow = new(1.0f, 1.0f, 0.0f, 0.35f);
            Color red = new(1.0f, 0.0f, 0.0f, 0.35f);
            Color blue = new(0.0f, 0.0f, 1.0f, 0.55f);
            Color gray = new(0.2f, 0.2f, 0.2f, 0.55f);

            if (!_characterController) {
                _characterController = GetComponent<CharacterController>();
            }

            // Draw sphere for the grounded check area
            if (IsGrounded()) {
                Gizmos.color = green;
            } else {
                Gizmos.color = red;
            }
            Gizmos.DrawSphere(_characterController.GetBottomSphereCenter() + Vector3.down * GroundedTolerance, _characterController.GetActualRadius());
        }
    }
}
