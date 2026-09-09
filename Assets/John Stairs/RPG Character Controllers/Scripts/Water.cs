using UnityEngine;
using System.Collections.Generic;

namespace JohnStairs.RPG {
    [RequireComponent(typeof(BoxCollider))]
    public class Water : MonoBehaviour {
        /// <summary>
        /// Water level in world coordinates, set once on start
        /// </summary>
        protected float _globalWaterLevel = 0;
        /// <summary>
        /// Box collider defining the bounds of the water volume
        /// </summary>
        protected BoxCollider _boxCollider;

        public class WaterComparer : Comparer<Water> {
            /// <summary>
            /// Compares two waters by their water level
            /// </summary>
            /// <param name="a">Left-side water script</param>
            /// <param name="b">Right-side water script</param>
            /// <returns>A signed number indicating the relative values of a and b</returns>
            public override int Compare(Water a, Water b) {
                return a.GetLevel().CompareTo(b.GetLevel());
            }
        }

        protected virtual void Start() {
            _boxCollider = GetComponent<BoxCollider>();
            if (!_boxCollider.isTrigger) {
                Debug.LogWarning("Box Collider assigned to Water component on game object " + name + " is not set up as trigger! However, this is needed for the underwater handler to work", _boxCollider);
            }
            _globalWaterLevel = transform.position.y + (_boxCollider.center.y + _boxCollider.size.y * 0.5f) * transform.localScale.y;
        }

        /// <summary>
        /// Gets the water level in world coordinates
        /// </summary>
        /// <returns>Water level in world coordinates</returns>
        public virtual float GetLevel() {
            return _globalWaterLevel;
        }

        /// <summary>
        /// Checks if the given point is inside this water object
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <returns>True if inside, otherwise false</returns>
        public virtual bool Contains(Vector3 point) {
            Vector3 direction = _boxCollider.transform.TransformPoint(_boxCollider.center) - point;
            return !_boxCollider.Raycast(new Ray(point, direction), out _, direction.magnitude);
        }
    }
}
