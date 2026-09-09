using JohnStairs.RPG.Character.Motor.Enums;
using UnityEngine;

namespace JohnStairs.RPG.Character.Controller {
    public class ARPGController : RPGController {
        /// <summary>
        /// If true, the camera will always orbit around the character, i.e. without holding the "Activate Orbiting" input. Use input action "Toggle Cursor Visibility" to pause camera orbiting
        /// </summary>
        [Tooltip("If true, the camera will always orbit around the character, i.e. without holding the \"Activate Orbiting\" input. Use input action \"Toggle Cursor Visibility\" to pause camera orbiting")]
        public bool AlwaysOrbitCamera = true;

        protected Vector3 _inputMovementVector;
        protected bool _makeCursorVisible;

        protected override void Awake() {
            base.Awake();
        }

        protected override void Update() {
            base.Update();

            HandleCursorVisibility();
        }

        protected override void HandleCharacterMovement() {
            Vector3 right;
            Vector3 forward;

            if (_motor?.GetMovementType() == MovementType.Climbing) {
                right = transform.right;
                forward = transform.forward;

                if (CameraIsBehindCharacter()) {
                    right = -right;
                    forward = -forward;
                }
            } else if (_lockedOnTarget) {
                right = transform.right;
                forward = transform.forward;
            } else {
                right = GetCamera().transform.right;
                forward = Vector3.Cross(right, Vector3.up);

            }
            _inputMovementVector = GetCombinedStrafeInput() * right + (GetCombinedForwardInput() + AutorunningForward()) * forward;
            ApplyAscendAndDescend(ref _inputMovementVector);
            _motor?.Move(_inputMovementVector);
        }

        protected override void HandleCharacterTurning() {
            if (AllowCharacterTurning()) {
                _motor?.Turn(_inputHandler.Rotate());
            }
        }

        protected override void HandleCharacterFacingDirection() {
            if (_lockedOnTarget) {
                _motor?.TurnInDirection(GetCharacterTargetDirection(), Vector3.up);
            } else if (_inputHandler.AlignWithCamera()) {
                _motor?.TurnInDirection(GetCamera().transform.forward, GetCamera().transform.up);
            } else if (_inputMovementVector.magnitude > 0) {
                _motor?.TurnInDirection(_inputMovementVector, GetUpDirectionWhileMoving(_inputMovementVector));
            } else {
                _motor?.TurnInDirection(transform.forward, Vector3.up);
            }
        }

        protected override void HandleCameraAlignment() {
            if (_lockedOnTarget) {
                _camera?.AlignWithTransform(transform, false);
            }
        }

        protected virtual Vector3 GetUpDirectionWhileMoving(Vector3 movementDirection) {
            Vector3 up = Vector3.Cross(movementDirection, transform.right);
            if (up.y < 0) {
                up.y = -up.y;
            }
            return up;
        }

        protected virtual void HandleCursorVisibility() {
            if (AlwaysOrbitCamera && _inputHandler.ToggleCursorVisibility()) {
                _makeCursorVisible = !_makeCursorVisible;
            }
        }

        protected override void SetCameraOrbitingActive() {
            if (AlwaysOrbitCamera) {
                HandleCameraOrbitingBasedOnToggle();
            } else {
                base.SetCameraOrbitingActive();
            }
        }

        protected virtual void HandleCameraOrbitingBasedOnToggle() {
            if (_makeCursorVisible) {
                _cameraOrbitingActivation = false;
                _cameraOrbitingActive = false;
            } else {
                _cameraOrbitingActivation = true;
                _cameraOrbitingActive = true;
            }
        }
    }
}