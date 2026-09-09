using JohnStairs.RPG.Character.Motor.Enums;
using UnityEngine;

namespace JohnStairs.RPG.Character.Controller {
    public class MMORPGController : RPGController {
        /// <summary>
        /// If true, the camera will align with the character's forward direction when it is moving
        /// </summary>
        [Tooltip("If true, the camera will align with the character's forward direction when it is moving")]
        public bool AlignCameraOnCharacterMovement = true;

        protected override void Awake() {
            base.Awake();
        }

        protected override void Update() {
            base.Update();
        }

        protected override void HandleCharacterMovement() {
            Vector3 inputMovementVector = GetCombinedStrafeInput() * transform.right + (GetCombinedForwardInput() + AutorunningForward()) * transform.forward;
            ApplyAscendAndDescend(ref inputMovementVector);
            _motor?.Move(inputMovementVector);
        }

        protected override void HandleCharacterTurning() {
            if (AllowCharacterTurning()) {
                float characterRotation = _motor?.Turn(_inputHandler.Rotate()) ?? 0;
                if (SyncRotations == RotationSync.Always
                    || SyncRotations == RotationSync.PauseOnOrbiting && !_cameraOrbitingActive) {
                    _camera?.Yaw(characterRotation, true);
                }
            }
        }

        protected override void HandleCharacterFacingDirection() {
            if (_lockedOnTarget) {
                _motor?.TurnInDirection(GetCharacterTargetDirection(), Vector3.up);
            } else if (_inputHandler.AlignWithCamera()) {
                _motor?.TurnInDirection(GetCamera().transform.forward, GetCamera().transform.up);
            } else {
                _motor?.TurnInDirection(transform.forward, Vector3.up);
            }
        }

        protected override void HandleCameraAlignment() {
            if (_lockedOnTarget && !_cameraOrbitingActive) {
                _camera?.AlignWithTransform(transform, false);
            } else if (AlignCameraWithCharacterOnMovement(out bool opposed)) {
                _camera?.AlignWithTransform(transform, opposed);
            }
        }

        protected virtual bool AlignCameraWithCharacterOnMovement(out bool opposed) {
            opposed = _inputHandler.Movement().y < 0 && !PreventOpposedCameraAlignment();
            return AlignCameraOnCharacterMovement
                    && _inputHandler.Movement().y != 0
                    && !_cameraOrbitingActive;
        }

        protected virtual bool PreventOpposedCameraAlignment() {
            return _inputHandler.AlignWithCamera() || _motor?.GetMovementType() == MovementType.Climbing;
        }
    }
}