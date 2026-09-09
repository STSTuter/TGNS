using UnityEngine;

namespace JohnStairs.RPG.Character.Controller {
    public class IsoRPGController : RPGController {
        protected Vector3 _inputMovementVector;

        protected override void Awake() {
            base.Awake();
        }

        protected override void Update() {
            base.Update();
        }

        protected override void HandleCharacterMovement() {
            Vector3 right = GetCamera().transform.right;
            Vector3 forward = Vector3.Cross(right, Vector3.up);
            _inputMovementVector = _inputHandler.Movement().x * right + (GetCombinedForwardInput() + AutorunningForward()) * forward;
            ApplyAscendAndDescend(ref _inputMovementVector);
            _motor?.Move(_inputMovementVector);
        }

        protected override void HandleCharacterTurning() {
        }

        protected override void HandleCharacterFacingDirection() {
            if (_lockedOnTarget) {
                _motor?.TurnInDirection(GetCharacterTargetDirection(), Vector3.up);
            } else if (CursorRaycast(out RaycastHit hit)) {
                _motor?.TurnInDirection(hit.point - transform.position, Vector3.up);
            } else if (_inputMovementVector.magnitude > 0) {
                _motor?.TurnInDirection(_inputMovementVector, Vector3.up);
            } else {
                _motor?.TurnInDirection(transform.forward, Vector3.up);
            }
        }

        protected override void HandleCameraAlignment() {
        }

        protected virtual bool CursorRaycast(out RaycastHit hit) {
            Ray ray = GetCamera().ScreenPointToRay(_inputHandler.CursorPosition());
            return Physics.Raycast(ray, out hit, 100.0f, SceneManager.GetInstance().GroundLayers, QueryTriggerInteraction.Ignore);
        }
    }
}