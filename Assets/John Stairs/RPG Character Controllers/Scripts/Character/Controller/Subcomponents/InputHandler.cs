using System.Collections.Generic;
using JohnStairs.RPG.Character.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JohnStairs.RPG.Character.Controller.Subcomponents {
    [RequireComponent(typeof(PlayerInput))]
    public class InputHandler : MonoBehaviour, IInputHandler {
        // Camera
        public bool _ActivateOrbitingStart = false;
        public bool _ActivateOrbitingStop = false;
        public Vector2 _OrbitingAmount;
        public float _ZoomAmount = 0;
        public bool _MinDistanceZoom = false;
        public bool _MaxDistanceZoom = false;
        public Vector2 _CursorPosition;
        public bool _ToggleCursorVisibility = false;
        // Motor
        public Vector2 _Movement;
        public float _MoveForward;
        public float _Rotate = 0;
        public bool _Jump = false;
        public bool _Sprint = false;
        public bool _ToggleWalking = false;
        public bool _ToggleCrouching = false;
        public bool _ToggleAutorunning = false;
        public bool _AlignWithCamera = false;
        public bool _ToggleFlyingAbility = false;
        public bool _Ascend = false;
        public bool _Descend = false;
        // Combat
        public bool _Select = false;
        public bool _Cancel = false;
        public InputByPhase[] _ActionBarSlots;
        public bool _LockOnTarget = false;

        // General
        protected PlayerInput _playerInput;
        protected bool characterScriptFound = false;
        // Camera
        protected InputAction _activateOrbitingAction;
        protected InputAction _orbitingAmountAction;
        protected InputAction _zoomAction;
        protected InputAction _zoomToMinDistanceAction;
        protected InputAction _zoomToMaxDistanceAction;
        protected InputAction _cursorPositionAction;
        protected InputAction _toggleCursorVisibilityAction;
        // Motor
        protected InputAction _movementAction;
        protected InputAction _moveForwardAction;
        protected InputAction _rotateAction;
        protected InputAction _jumpAction;
        protected InputAction _sprintAction;
        protected InputAction _toggleWalkingAction;
        protected InputAction _toggleCrouchingAction;
        protected InputAction _toggleAutorunningAction;
        protected InputAction _alignWithCameraAction;
        protected InputAction _toggleFlyingAbilityAction;
        protected InputAction _ascendAction;
        protected InputAction _descendAction;
        // Combat
        protected InputAction _selectAction;
        protected InputAction _cancelAction;
        protected InputAction[] _actionBarSlotActions;
        protected InputAction _lockOnTargetAction;

        protected virtual void Start() {
            characterScriptFound = GetComponent<ICharacter>() != null;
            InitializeInputActions();
        }

        /// <summary>
        /// Tries to get the input values used by this script
        /// </summary>
        protected virtual void Update() {
            // Camera
            _ActivateOrbitingStart = _activateOrbitingAction?.WasPressedThisFrame() ?? false;
            _ActivateOrbitingStop = _activateOrbitingAction?.WasReleasedThisFrame() ?? false;
            _OrbitingAmount = _orbitingAmountAction?.ReadValue<Vector2>() ?? Vector2.zero;
            _ZoomAmount = _zoomAction?.ReadValue<float>() ?? 0;
            _MinDistanceZoom = _zoomToMinDistanceAction?.WasPressedThisFrame() ?? false;
            _MaxDistanceZoom = _zoomToMaxDistanceAction?.WasPressedThisFrame() ?? false;
            _CursorPosition = _cursorPositionAction?.ReadValue<Vector2>() ?? Vector2.zero;
            _ToggleCursorVisibility = _toggleCursorVisibilityAction?.WasPressedThisFrame() ?? false;

            // Motor
            _Movement = _movementAction?.ReadValue<Vector2>() ?? Vector2.zero;
            _MoveForward = _moveForwardAction?.ReadValue<float>() ?? 0;
            _Rotate = _rotateAction?.ReadValue<float>() ?? 0;
            _Jump = _jumpAction?.WasPressedThisFrame() ?? false;
            _Sprint = _sprintAction?.IsPressed() ?? false;
            _ToggleWalking = _toggleWalkingAction?.WasPressedThisFrame() ?? false;
            _ToggleCrouching = _toggleCrouchingAction?.WasPressedThisFrame() ?? false;
            _ToggleAutorunning = _toggleAutorunningAction?.WasPressedThisFrame() ?? false;
            _AlignWithCamera = _alignWithCameraAction?.IsPressed() ?? false;
            _ToggleFlyingAbility = _toggleFlyingAbilityAction?.WasPressedThisFrame() ?? false;
            _Ascend = _ascendAction?.IsPressed() ?? false;
            _Descend = _descendAction?.IsPressed() ?? false;

            if (characterScriptFound) {
                // Combat
                _Select = _selectAction?.WasReleasedThisFrame() ?? false;
                _Cancel = _cancelAction?.WasPressedThisFrame() ?? false;
                for (int i = 0; i < _actionBarSlotActions.Length; i++) {
                    InputAction inputAction = _actionBarSlotActions[i];
                    InputByPhase inputByPhase = new() {
                        Start = inputAction?.WasPressedThisFrame() ?? false,
                        Stop = inputAction?.WasReleasedThisFrame() ?? false
                    };
                    _ActionBarSlots[i] = inputByPhase;
                }
                _LockOnTarget = _lockOnTargetAction?.WasPressedThisFrame() ?? false;
            }
        }

        /// <summary>
        /// Initializes the internal input action variables from the PlayerInput component
        /// </summary>
        public virtual void InitializeInputActions(bool logWarnings = false) {
            if (_playerInput == null) {
                _playerInput = GetComponent<PlayerInput>();
            }
            if (_playerInput.actions == null) {
                Debug.LogWarning("There is no Input Action Asset assigned to the Player Input component on game object " + gameObject.name + "! This is a requirement for the RPG Controller to work", gameObject);
                return;
            }
            // Camera
            _activateOrbitingAction = GetInputAction("Activate Orbiting", logWarnings);
            _orbitingAmountAction = GetInputAction("Orbiting Amount", logWarnings);
            _zoomAction = GetInputAction("Zoom", logWarnings);
            _zoomToMinDistanceAction = GetInputAction("Zoom To Min Distance", logWarnings);
            _zoomToMaxDistanceAction = GetInputAction("Zoom To Max Distance", logWarnings);
            _cursorPositionAction = GetInputAction("Cursor Position", logWarnings);
            _toggleCursorVisibilityAction = GetInputAction("Toggle Cursor Visibility", logWarnings);

            // Motor
            _movementAction = GetInputAction("Movement", logWarnings);
            _moveForwardAction = GetInputAction("Move Forward", logWarnings);
            _rotateAction = GetInputAction("Rotate", logWarnings);
            _jumpAction = GetInputAction("Jump", logWarnings);
            _sprintAction = GetInputAction("Sprint", logWarnings);
            _toggleWalkingAction = GetInputAction("Toggle Walking", logWarnings);
            _toggleCrouchingAction = GetInputAction("Toggle Crouching", logWarnings);
            _toggleAutorunningAction = GetInputAction("Toggle Autorunning", logWarnings);
            _alignWithCameraAction = GetInputAction("Align With Camera", logWarnings);
            _toggleFlyingAbilityAction = GetInputAction("Toggle Flying Ability", logWarnings);
            _ascendAction = GetInputAction("Ascend", logWarnings);
            _descendAction = GetInputAction("Descend", logWarnings);

            if (characterScriptFound) {
                // Combat
                _selectAction = GetInputAction("Select", logWarnings);
                _cancelAction = GetInputAction("Cancel", logWarnings);
                _actionBarSlotActions = GetActionBarInputActions(logWarnings);
                _lockOnTargetAction = GetInputAction("Lock On Target", logWarnings);
            }
        }

        protected virtual InputAction GetInputAction(string actionName, bool logWarnings = false) {
            try {
                return _playerInput.actions[actionName];
            } catch (KeyNotFoundException) {
                if (logWarnings) {
                    Debug.LogWarning("Input action " + actionName + " not found in " + _playerInput.actions.name, _playerInput);
                }
                return null;
            }
        }

        protected virtual InputAction[] GetActionBarInputActions(bool logWarnings = false) {
            InputAction[] actionBarInputActions;
            Dictionary<InputAction, int> inputActionMap = new();
            int maximumSlotNumber = -1;
            foreach (InputAction action in _playerInput.actions) {
                if (action.name.Contains("Action Bar Slot")) {
                    string[] parts = action.name.Split(' ');
                    int slotNumber;
                    if (int.TryParse(parts[parts.Length - 1], out slotNumber)) {
                        if (slotNumber > maximumSlotNumber)
                            maximumSlotNumber = slotNumber;
                    }
                    inputActionMap.Add(action, slotNumber);
                }
            }

            if (maximumSlotNumber < 0 && logWarnings) {
                Debug.LogWarning("No action bar slot input actions found in " + _playerInput.actions.name, _playerInput);
            }

            actionBarInputActions = new InputAction[maximumSlotNumber + 1];
            foreach (KeyValuePair<InputAction, int> inputToSlotNumber in inputActionMap) {
                actionBarInputActions[inputToSlotNumber.Value] = inputToSlotNumber.Key;
            }
            _ActionBarSlots = new InputByPhase[actionBarInputActions.Length];
            return actionBarInputActions;
        }

        public virtual bool ActivateOrbitingStart() {
            return _ActivateOrbitingStart;
        }

        public virtual bool ActivateOrbitingStop() {
            return _ActivateOrbitingStop;
        }

        public virtual Vector2 OrbitingAmount() {
            return _OrbitingAmount;
        }

        public virtual float ZoomAmount() {
            return _ZoomAmount;
        }

        public virtual bool MinDistanceZoom() {
            return _MinDistanceZoom;
        }

        public virtual bool MaxDistanceZoom() {
            return _MaxDistanceZoom;
        }

        public virtual Vector2 CursorPosition() {
            return _CursorPosition;
        }

        public virtual Vector2 Movement() {
            return _Movement;
        }

        public virtual float MoveForward() {
            return _MoveForward;
        }

        public virtual float Rotate() {
            return _Rotate;
        }

        public virtual bool Jump() {
            return _Jump;
        }

        public virtual bool AlignWithCamera() {
            return _AlignWithCamera;
        }

        public virtual bool Sprint() {
            return _Sprint;
        }

        public virtual bool ToggleWalking() {
            return _ToggleWalking;
        }

        public virtual bool ToggleCrouching() {
            return _ToggleCrouching;
        }

        public virtual bool ToggleAutorunning() {
            return _ToggleAutorunning;
        }

        public virtual bool ToggleFlyingAbility() {
            return _ToggleFlyingAbility;
        }

        public virtual bool Ascend() {
            return _Ascend;
        }

        public virtual bool Descend() {
            return _Descend;
        }

        public virtual bool Select() {
            return _Select;
        }

        public virtual bool Cancel() {
            return _Cancel;
        }

        public virtual InputByPhase[] ActionBarSlots() {
            return _ActionBarSlots;
        }

        public virtual bool LockOnTarget() {
            return _LockOnTarget;
        }

        public virtual bool ToggleCursorVisibility() {
            return _ToggleCursorVisibility;
        }

        public virtual void Destroy() {
            Destroy(this);
        }
    }
}