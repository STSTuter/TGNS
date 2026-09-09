using System.Collections.Generic;
using UnityEngine;

namespace JohnStairs.RPG.Character.Motor.Subcomponents {
    public class SwimmingHandler : MonoBehaviour, ISwimmingHandler {
        /// <summary>
        /// Personal water level at which the character should start to swim. Enable Gizmos for easier setup
        /// </summary>
        [Tooltip("Personal water level at which the character should start to swim. Enable Gizmos for easier setup")]
        public float PersonalStartLevel = 0.8f;
        /// <summary>
        /// Movement speed when swimming
        /// </summary>
        [Tooltip("Movement speed when swimming")]
        public float SwimSpeed = 4.0f;

        /// <summary>
        /// Level in world coordinates below which the character starts swimming
        /// </summary>
        protected float _globalStartLevel = -Mathf.Infinity;
        /// <summary>
        /// Stores all scripts of touched waters, sorted by water level
        /// </summary>
        protected SortedSet<Water> _touchedWaters;
        /// <summary>
        /// Water level on the world Y axis
        /// </summary>
        protected float _currentWaterLevel = -Mathf.Infinity;


        protected virtual void Awake() {
            _touchedWaters = new SortedSet<Water>(new Water.WaterComparer());
        }

        protected virtual void Update() {
            _currentWaterLevel = GetCurrentWaterLevel();
        }

        public bool CheckSwimming(Vector3 colliderPosition) {
            _globalStartLevel = colliderPosition.y + PersonalStartLevel;
            return _globalStartLevel <= _currentWaterLevel;
        }

        public virtual bool IsNearWaterSurface() {
            return Mathf.Abs(_currentWaterLevel - _globalStartLevel) < 0.1f;
        }

        /// <summary>
        /// Gets the current water level based on all touched waters
        /// </summary>
        /// <returns>Maximum water level of all touched waters, -Infinity if no water is touched</returns>
        protected virtual float GetCurrentWaterLevel() {
            return _touchedWaters.Max?.GetLevel() ?? -Mathf.Infinity;
        }

        public virtual float GetSwimSpeed() {
            return SwimSpeed;
        }

        public virtual void Reset() {
            _touchedWaters?.Clear();
        }

        /// <summary>
        /// "OnTriggerEnter happens on the FixedUpdate function when two GameObjects collide" - Unity Documentation
        /// </summary>
        /// <param name="other">Triggering collider</param>
        protected virtual void OnTriggerEnter(Collider other) {
            Water water = other.GetComponent<Water>();
            if (water) {
                // Store the water script for getting the right water level later
                _touchedWaters.Add(water);
            }
        }

        /// <summary>
        /// "OnTriggerExit is called when the Collider other has stopped touching the trigger" - Unity Documentation
        /// </summary>
        /// <param name="other">Left trigger collider</param>
        protected virtual void OnTriggerExit(Collider other) {
            Water water = other.GetComponent<Water>();
            if (water) {
                // Remove the water again since we left it
                _touchedWaters.Remove(water);
            }
        }

        protected virtual void OnDrawGizmosSelected() {
            Gizmos.color = new Color(0.0f, 0.0f, 1.0f, 0.55f);
            // Draw Personal Start Level
            Gizmos.DrawCube(GetComponent<CharacterController>().GetBottomSphereCenter() + Vector3.up * PersonalStartLevel, new Vector3(0.7f, 0.01f, 0.7f));
        }
    }
}
