using System.Collections.Generic;
using UnityEngine;

namespace JohnStairs.RPG.Character.Cam.Subcomponents {
    public class UnderwaterHandler : UnderwaterHandlerLite, IUnderwaterHandler {
        /// <summary>
        /// If set to true, the camera will skip the water surface instead of moving continuously through it
        /// </summary>
        [Tooltip("If set to true, the camera will skip the water surface instead of moving continuously through it.")]
        public bool PassThroughWaterLevel;


        protected override void Awake() {
            base.Awake();
        }

        protected override void Start() {
            base.Start();
        }

        public override float ApplyWaterLevelSkip(Vector3 pivotPosition, float cameraDistance, Vector3 viewport, float pitch) {
            if (PassThroughWaterLevel) {
                return pitch;
            }

            // Compute the degrees at which the viewport center intersects with the water surface
            float distanceToViewportCenter = cameraDistance - viewport.z;
            float waterSurfaceAngle = Mathf.Asin((GetCurrentWaterHeight() - pivotPosition.y) / distanceToViewportCenter) * Mathf.Rad2Deg;
            float deadZoneAngle = Mathf.Atan(viewport.y / distanceToViewportCenter) * Mathf.Rad2Deg;
            float degreesToWaterSurface = pitch - waterSurfaceAngle;

            if (Mathf.Abs(degreesToWaterSurface) < deadZoneAngle) {
                // Pitch is inside the dead zone => adjust it to dead zone border
                return waterSurfaceAngle + Mathf.Sign(degreesToWaterSurface) * deadZoneAngle;
            }

            return pitch;
        }
    }
}
