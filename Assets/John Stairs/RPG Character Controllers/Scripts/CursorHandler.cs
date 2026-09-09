using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace JohnStairs.RPG {
    public class CursorHandler : MonoBehaviour, ICursorHandler {
        /// <summary>
        /// Layers to check for UI interaction
        /// </summary>
        [Tooltip("Layers to check for UI interaction")]
        public LayerMask UiLayers = 32;

        /// <summary>
        /// Stores the position the cursor is currently locked in
        /// </summary>
        protected Vector2? _lockedCursorPosition;

        protected virtual void Awake() {
        }

        protected virtual void Start() {
        }

        public virtual void ShowCursor() {
            Cursor.visible = true;
            _lockedCursorPosition = null;
        }

        public virtual void HideCursor() {
            Cursor.visible = false;

            if (_lockedCursorPosition == null) {
                _lockedCursorPosition = Mouse.current.position.ReadValue();
            }

            Mouse.current.WarpCursorPosition(_lockedCursorPosition.GetValueOrDefault());
        }

        /// <summary>
        /// Checks if the cursor is currently over an element which belongs to the specified UI layers
        /// </summary>
        /// <returns>True if the cursor is over UI, otherwise false</returns>
        public virtual bool IsCursorOverUI() {
            if (!EventSystem.current || !Cursor.visible) {
                return false;
            }
            PointerEventData eventData = new(EventSystem.current) {
                position = Mouse.current.position.ReadValue()
            };
            List<RaycastResult> raycastResults = new();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            foreach (RaycastResult raycastResult in raycastResults) {
                if (Utils.LayerInLayerMask(raycastResult.gameObject.layer, UiLayers)) {
                    return true;
                }
            }
            return false;
        }

        public virtual void Destroy() {
            Destroy(this);
        }
    }
}