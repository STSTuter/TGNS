using System.Collections.Generic;
using UnityEngine;

namespace JohnStairs.RPG.Character.Cam.Subcomponents {
    public class UnderwaterHandlerLite : MonoBehaviour, IUnderwaterHandler {
        /// <summary>
        /// Fog mode when the camera is underwater
        /// </summary>
        [Tooltip("Fog mode when the camera is underwater.")]
        public FogMode UnderwaterFogMode = FogMode.ExponentialSquared;
        /// <summary>
        /// Fog color when the camera is underwater
        /// </summary>
        [Tooltip("Fog color when the camera is underwater.")]
        public Color UnderwaterFogColor = new(0, 0.46f, 1.0f);
        /// <summary>
        /// Fog density when the camera is underwater
        /// </summary>
        [Tooltip("Fog density when the camera is underwater.")]
        public float UnderwaterFogDensity = 0.1f;

        /// <summary>
        /// True if underwater render effects are enabled, otherwise false
        /// </summary>
        protected bool _effectsEnabled;
        /// <summary>
        /// Fog mode before apply underwater effects
        /// </summary>
        protected FogMode _previousFogMode;
        /// <summary>
        /// Fog color before apply underwater effects
        /// </summary>
        protected Color _previousFogColor;
        /// <summary>
        /// Fog density before apply underwater effects
        /// </summary>
        protected float _previousFogDensity;
        /// <summary>
        /// Stores all scripts of touched waters, sorted by water level in world coordinates
        /// </summary>
        protected SortedSet<Water> _touchedWaters;

        protected virtual void Awake() {
            _touchedWaters = new SortedSet<Water>(new Water.WaterComparer());
        }

        protected virtual void Start() {
            RenderSettings.fog = true;
        }

        public virtual bool IsUnderwater(Camera camera, Vector3 viewport) {
            Vector3 viewportCenter = camera.transform.position + camera.transform.forward * viewport.z;
            foreach (Water water in _touchedWaters) {
                if (water.Contains(viewportCenter)) {
                    return true;
                }
            }
            return false;
        }

        public virtual void EnableEffects() {
            if (!_effectsEnabled) {
                _previousFogMode = RenderSettings.fogMode;
                _previousFogColor = RenderSettings.fogColor;
                _previousFogDensity = RenderSettings.fogDensity;
                RenderSettings.fogMode = UnderwaterFogMode;
                RenderSettings.fogColor = UnderwaterFogColor;
                RenderSettings.fogDensity = UnderwaterFogDensity;
                _effectsEnabled = true;
            }
        }

        public virtual void DisableEffects() {
            if (_effectsEnabled) {
                RenderSettings.fogMode = _previousFogMode;
                RenderSettings.fogColor = _previousFogColor;
                RenderSettings.fogDensity = _previousFogDensity;
                _effectsEnabled = false;
            }
        }

        /// <summary>
        /// Gets the current water height
        /// </summary>
        /// /// <returns>Current water height if touching water, otherwise -infinity</returns>
        protected virtual float GetCurrentWaterHeight() {
            return _touchedWaters.Max?.GetLevel() ?? -Mathf.Infinity;
        }

        public virtual float ApplyWaterLevelSkip(Vector3 pivotPosition, float cameraDistance, Vector3 viewport, float pitch) {
            // Not implemented with Lite
            return pitch;
        }

        /// <summary>
        /// "OnTriggerEnter happens on the FixedUpdate function when two GameObjects collide" - Unity Documentation
        /// </summary>
        /// <param name="other">Collider that entered the trigger collider</param>
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
    }
}
