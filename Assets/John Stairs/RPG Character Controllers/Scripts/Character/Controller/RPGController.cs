using System.Collections;
using JohnStairs.RPG.Character.Cam;
using JohnStairs.RPG.Character.Combat;
using JohnStairs.RPG.Character.Controller.Subcomponents;
using JohnStairs.RPG.Character.Motor;
using JohnStairs.RPG.Character.Motor.Enums;
using JohnStairs.RPG.Combat.Abilities;
using UnityEngine;

namespace JohnStairs.RPG.Character.Controller {
    [DefaultExecutionOrder(50)]
    public abstract class RPGController : MonoBehaviour {
        /// <summary>
        /// Enum for controlling if the camera should rotate together with the character
        /// </summary>
        public enum RotationSync {
            /// <summary>
            /// Never rotate with the character
            /// </summary>
            Never,
            /// <summary>
            /// The rotation stops while the camera is orbiting around the character
            /// </summary>
            PauseOnOrbiting,
            /// <summary>
            /// Always rotate together with the character
            /// </summary>
            Always
        }

        /// <summary>
        /// Inverts the horizontal orbit axis
        /// </summary>
        [Tooltip("Inverts the horizontal orbit axis")]
        public bool InvertYawAxis = true;
        /// <summary>
        /// Inverts the vertical orbit axis
        /// </summary>
        [Tooltip("Inverts the vertical orbit axis")]
        public bool InvertPitchAxis = true;
        /// <summary>
        /// The sensitivity of orbiting the camera on the horizontal axis
        /// </summary>
        [Tooltip("The sensitivity of orbiting the camera on the horizontal axis")]
        public float YawSensitivity = 0.25f;
        /// <summary>
        /// The sensitivity of orbiting the camera on the vertical axis
        /// </summary>
        [Tooltip("The sensitivity of orbiting the camera on the vertical axis")]
        public float PitchSensitivity = 0.25f;
        /// <summary>
        /// The sensitivity of the zooming input
        /// </summary>
        [Tooltip("The sensitivity of the zooming input")]
        public float ZoomSensitivity = 0.01f;
        /// <summary>
        /// Controls if the camera should rotate together with the character
        /// </summary>
        public RotationSync SyncRotations = RotationSync.PauseOnOrbiting;
        /// <summary>
        /// The number of shakes per second while the camera is shaking
        /// </summary>
        [Tooltip("The number of shakes per second")]
        public float ShakeFrequency = 5.0f;
        /// <summary>
        /// Maximum rotation change in degrees while the camera is shaking 
        /// </summary>
        [Tooltip("Maximum rotation change in degrees while the camera is shaking")]
        public float ShakeAmplitude = 10.0f;
        /// <summary>
        /// Maximum amplitude multiplier. E.g. with a value of 2, the amplitude can sometimes be twice as high
        /// </summary>
        [Tooltip("Maximum amplitude multiplier. E.g. with a value of 2, the amplitude can sometimes be twice as high")]
        public float ShakeAmplitudeVariance = 2.0f;
        /// <summary>
        /// If true, ability area selection uses the cursor position; otherwise, it uses the character's forward direction
        /// </summary>
        [Tooltip("If true, ability area selection uses the cursor position; otherwise, it uses the character's forward direction")]
        public bool AreaSelectionByCursor = true;

        /// <summary>
        /// Reference to the used input handler script
        /// </summary>
        protected IInputHandler _inputHandler;
        /// <summary>
        /// Reference to the used RPG camera script
        /// </summary>
        protected ICamera _camera;
        /// <summary>
        /// Reference to the used motor script
        /// </summary>
        protected IMotor _motor;
        /// <summary>
        /// Reference to the used cursor handler
        /// </summary>
        protected ICursorHandler _cursorHandler;
        /// <summary>
        /// True if the orbiting input is pressed (prerequisite for starting orbiting)
        /// </summary>
        protected bool _cameraOrbitingActivation;
        /// <summary>
        /// True if the camera started the orbiting
        /// </summary>
        protected bool _cameraOrbitingActive;
        /// <summary>
        /// Currently running shaking coroutine
        /// </summary>
        protected IEnumerator _shakingCoroutine;
        /// <summary>
        /// True if the character is currently autorunning
        /// </summary>
        protected bool _autorunning;
        /// <summary>
        /// The last recorded forward input value, used to detect changes for autorunning logic
        /// </summary>
        protected float _lastForwardInput;
        /// <summary>
        /// Reference to the used character script
        /// </summary>
        protected ICharacter _character;
        /// <summary>
        /// Reference to the used action bar script
        /// </summary>
        protected IActionBar _actionBar;
        /// <summary>
        /// Reference to the ability area selection circle script
        /// </summary>
        protected IAbilityAreaSelectionCircle _abilityAreaSelectionCircle;
        /// <summary>
        /// True if the camera is locked on to a target
        /// </summary>
        protected bool _lockedOnTarget;

        protected virtual void Awake() {
            _inputHandler = GetComponent<IInputHandler>();
            _camera = GetComponent<ICamera>();
            _cursorHandler = GetComponent<ICursorHandler>();
            _motor = GetComponent<IMotor>();
            _character = GetComponent<ICharacter>();
            _actionBar = GetComponent<IActionBar>();
        }

        protected virtual void Start() {
        }

        protected virtual void Update() {
            if (_inputHandler == null) {
                return;
            }

            HandleCancel();
            HandleTargetSelection();
            HandleLockOnTarget();
            HandleCharacterInMotion();
            HandleActionBar();
            HandleAbilityAreaSelection();

            HandlePreventLookUpBehavior();
            HandleCameraOrbiting();
            HandleCameraZoom();

            if (EnableMotorControl()) {
                HandleCharacterMovement();
                HandleCharacterTurning();
                HandleCharacterFacingDirection();
                HandleCameraAlignment();

                HandleJumping();
                HandleSprinting();
                HandleWalking();
                HandleCrouching();

                HandleFlyingAbility();
                HandleMovementImpairingEffects();
            }
        }

        protected virtual void HandleCancel() {
            if (_inputHandler.Cancel() && _character != null) {
                if (_character.IsUsingAbility()) {
                    _character.StopUsingAbility();
                } else if (_character.HasTarget()) {
                    _character.SetTarget(null);
                } else {
                    // open/close ui windows
                }
            }
        }

        protected virtual void HandleTargetSelection() {
            if (Select() && !AbilityAreaSelectionCircleShown()) {
                ICharacter target = null;
                if (CursorRaycast(SceneManager.GetInstance().CharacterLayers, out RaycastHit hit)) {
                    target = hit.collider.GetComponentInParent<ICharacter>();
                }
                _character?.SetTarget(target);
            }
        }

        protected virtual void HandleLockOnTarget() {
            if (!CharacterHasTarget()) {
                _lockedOnTarget = false;
            } else if (_inputHandler.LockOnTarget()) {
                _lockedOnTarget = !_lockedOnTarget;
            }
        }

        protected virtual void HandleCharacterInMotion() {
            _character?.SetInMotion(MotorIsInMotion());
        }

        protected virtual void HandleActionBar() {
            if (_actionBar == null) {
                return;
            }
            InputByPhase[] actionBarSlots = _inputHandler.ActionBarSlots();
            for (int i = 0; i < actionBarSlots.Length; i++) {
                if (actionBarSlots[i].Start) {
                    _actionBar.PressSlot(i);
                }

                if (actionBarSlots[i].Stop) {
                    _character?.UseAbility(_actionBar.GetAbilityInSlot(i));
                    _actionBar.ReleaseSlot(i);
                }
            }
        }

        protected virtual void HandleAbilityAreaSelection() {
            if (_abilityAreaSelectionCircle == null) {
                return;
            }

            if (_abilityAreaSelectionCircle.IsShown()) {
                if (AreaSelectionByCursor) {
                    if (CursorRaycast(SceneManager.GetInstance().GroundLayers, out RaycastHit hit)) {
                        _abilityAreaSelectionCircle.SetPosition(hit.point);
                    }
                } else {
                    Vector3 pointOnRay = transform.position + transform.forward;
                    _abilityAreaSelectionCircle.SetPositionByRaycast(pointOnRay);
                }

                if (Select()) {
                    _character?.SetSelectedAbilityArea(_abilityAreaSelectionCircle.GetPosition());
                    _abilityAreaSelectionCircle.Hide();
                }
            }
        }

        protected virtual void HandlePreventLookUpBehavior() {
            _camera?.PreventLookUpBehavior(_motor?.GetMovementType() == MovementType.Swimming);
        }

        protected virtual void HandleCameraOrbiting() {
            if (_camera?.IsOrbitingLocked() ?? false) {
                return;
            }

            SetCameraOrbitingActive();

            if (_cameraOrbitingActive) {
                _cursorHandler?.HideCursor();
                _camera?.Yaw(_inputHandler.OrbitingAmount().x * YawSensitivity * Utils.BoolToSign(InvertYawAxis));
                _camera?.Pitch(_inputHandler.OrbitingAmount().y * PitchSensitivity * Utils.BoolToSign(!InvertPitchAxis));
            } else {
                _cursorHandler?.ShowCursor();
            }
        }

        protected virtual void HandleCameraZoom() {
            if (_inputHandler.MinDistanceZoom()) {
                _camera?.ZoomToMinDistance();
            } else if (_inputHandler.MaxDistanceZoom()) {
                _camera?.ZoomToMaxDistance();
            } else {
                _camera?.Zoom(-_inputHandler.ZoomAmount() * ZoomSensitivity);
            }
        }

        protected abstract void HandleCharacterMovement();

        protected abstract void HandleCharacterTurning();

        protected abstract void HandleCharacterFacingDirection();

        protected abstract void HandleCameraAlignment();

        protected virtual void HandleJumping() {
            if (_inputHandler.Jump()) {
                _motor?.Jump();
            }
        }

        protected virtual void HandleSprinting() {
            _motor?.Sprint(_inputHandler.Sprint());
        }

        protected virtual void HandleWalking() {
            if (_inputHandler.ToggleWalking()) {
                _motor?.ToggleWalking();
            }
        }

        protected virtual void HandleCrouching() {
            if (_inputHandler.ToggleCrouching()) {
                _motor?.ToggleCrouching();
            }
        }

        protected virtual void HandleFlyingAbility() {
            if (_inputHandler.ToggleFlyingAbility()) {
                _motor?.ToggleFlyingAbility();
            }
        }

        protected virtual void HandleMovementImpairingEffects() {
            _motor?.SetMovementSpeedMultiplier(_character?.GetMovementSpeedMultiplier() ?? 1.0f);
        }

        protected virtual float GetCombinedStrafeInput() {
            return _inputHandler.Movement().x + (_lockedOnTarget || _inputHandler.AlignWithCamera() ? _inputHandler.Rotate() : 0);
        }

        protected virtual float GetCombinedForwardInput() {
            return _inputHandler.Movement().y + _inputHandler.MoveForward();
        }

        protected virtual float AutorunningForward() {
            if (_inputHandler.ToggleAutorunning()) {
                _autorunning = !_autorunning;
            }

            float forwardInput = GetCombinedForwardInput();
            if (_lastForwardInput == 0 && forwardInput != 0) {
                _autorunning = false;
            }
            _lastForwardInput = forwardInput;

            return _autorunning ? 1.0f : 0;
        }

        protected virtual void ApplyAscendAndDescend(ref Vector3 movementVector) {
            movementVector.y += (_inputHandler.Ascend() ? 1.0f : 0) - (_inputHandler.Descend() ? 1.0f : 0);
        }

        protected virtual void SetCameraOrbitingActive() {
            _cameraOrbitingActivation = _cameraOrbitingActivation || (_inputHandler.ActivateOrbitingStart() && !IsCursorOverUI());
            _cameraOrbitingActive = _cameraOrbitingActive || (_cameraOrbitingActivation && _inputHandler.OrbitingAmount().magnitude > 0);
            if (_inputHandler.ActivateOrbitingStop()) {
                _cameraOrbitingActivation = false;
                _cameraOrbitingActive = false;
            }
        }

        protected virtual bool IsCursorOverUI() {
            return _cursorHandler?.IsCursorOverUI() ?? false;
        }

        protected virtual Camera GetCamera() {
            return _camera?.GetUsedCamera() ?? Camera.main;
        }

        protected virtual bool CameraIsBehindCharacter() {
            return Vector3.Angle(GetCamera().transform.forward, transform.forward) > 90.0f;
        }

        protected virtual bool AllowCharacterTurning() {
            return !_lockedOnTarget && !_inputHandler.AlignWithCamera();
        }

        protected virtual Vector3 GetCharacterTargetDirection() {
            return (_character?.GetTargetPosition() ?? Vector3.zero) - transform.position;
        }

        /// <summary>
        /// Starts shaking the camera with the given parameters
        /// </summary>
        /// <param name="frequency">Number of shakes per second while the camera is shaking</param>
        /// <param name="amplitude">Maximum rotation change in degrees while the camera is shaking</param>
        /// <param name="variance">Maximum amplitude multiplier. E.g. with a value of 2, the amplitude can sometimes be twice as high</param>
        public virtual void StartShakingCamera(float frequency, float amplitude, float variance) {
            if (IsShakingCamera()) {
                StopShakingCamera();
            }

            _shakingCoroutine = ShakingCameraCoroutine(frequency, amplitude, variance);
            StartCoroutine(_shakingCoroutine);
            _camera?.SetRotationSmoothTime(1.0f / frequency);
        }

        /// <summary>
        /// Stops shaking the camera
        /// </summary>
        public virtual void StopShakingCamera() {
            if (IsShakingCamera()) {
                StopCoroutine(_shakingCoroutine);
                _shakingCoroutine = null;
                _camera?.ResetRotationSmoothTime();
            }
        }

        /// <summary>
        /// Checks if the camera is currently being shaked
        /// </summary>
        /// <returns>True if shaked, otherwise false</returns>
        public virtual bool IsShakingCamera() {
            return _shakingCoroutine != null;
        }

        /// <summary>
        /// Coroutine for shaking the camera
        /// </summary>
        protected virtual IEnumerator ShakingCameraCoroutine(float frequency, float amplitude, float variance) {
            Vector2 lastDelta;
            Vector2 _shakingDelta = Vector2.zero;
            while (true) {
                lastDelta = _shakingDelta;
                _shakingDelta = amplitude * Random.Range(1.0f / variance, variance) * Random.insideUnitCircle.normalized;

                if (Vector2.Angle(lastDelta, _shakingDelta) < 90.0f) {
                    // Make it more extreme
                    _shakingDelta *= -1.0f;
                }

                _camera?.Yaw(_shakingDelta.x);
                _camera?.Pitch(_shakingDelta.y);
                yield return new WaitForSeconds(1.0f / frequency * Random.Range(0.5f, 0.7f));
                _camera?.Yaw(-_shakingDelta.x);
                _camera?.Pitch(-_shakingDelta.y);
            }
        }

        protected virtual bool CursorRaycast(LayerMask layerMask, out RaycastHit hit) {
            if (_camera != null) {
                Ray ray = GetCamera().ScreenPointToRay(_inputHandler.CursorPosition());
                return Physics.Raycast(ray, out hit, 50.0f, layerMask, QueryTriggerInteraction.Ignore);
            } else {
                hit = new RaycastHit();
                return false;
            }
        }

        protected virtual bool Select() {
            return _inputHandler.Select() && !_cameraOrbitingActive && !IsCursorOverUI();
        }

        protected virtual bool AbilityAreaSelectionCircleShown() {
            return _abilityAreaSelectionCircle?.IsShown() ?? false;
        }

        protected virtual bool CharacterHasTarget() {
            return _character?.HasTarget() ?? false;
        }

        protected virtual bool MotorIsInMotion() {
            return _motor?.IsInMotion() ?? false;
        }

        protected virtual bool EnableMotorControl() {
            return MotorIsEnabled() && !CharacterIsDead();
        }

        protected virtual bool MotorIsEnabled() {
            return _motor?.IsEnabled() ?? false;
        }

        protected virtual bool CharacterIsDead() {
            return _character?.IsDead() ?? false;
        }

        public virtual void Destroy(bool cascade) {
            if (cascade) {
                _camera?.Destroy();
                _cursorHandler?.Destroy();
                _motor?.Destroy();
                _character?.Destroy();
                _actionBar?.Destroy();
                _inputHandler?.Destroy();
            }
            GameObject.Destroy(this);
        }
    }
}