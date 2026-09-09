using UnityEngine;

namespace JohnStairs.RPG.Character.Cam.Subcomponents {
    public class LookUpBehavior : MonoBehaviour, ILookUpBehavior {
        /// <summary>
        /// Layers to be checked for objects with the "Causing Tag"
        /// </summary>
        [Tooltip("Layers to be checked for objects with the \"Causing Tag\".")]
        public LayerMask CheckedLayers = 64;
        /// <summary>
        /// Tag that causes a look up once the camera touches the object's collider
        /// </summary>
        [Tooltip("Tag that causes a look up once the camera touches the object's collider.")]
        public string CausingTag = "Terrain";
        /// <summary>
        /// The maximum pitch possible during look up
        /// </summary>
        [Tooltip("The maximum pitch possible during look up.")]
        public float MaxPitch = 90.0f;
        /// <summary>
        /// The time needed for the camera to look up. The higher the smoother the look up
        /// </summary>
        [Tooltip("The time needed for the camera to look up. The higher the smoother the look up.")]
        public float RotationSmoothTime = 0.1f;

        /// <summary>
        /// Current look up degrees of the camera
        /// </summary>
        protected float _lookUpDegrees;
        /// <summary>
        /// Target look up degrees
        /// </summary>
        protected float _targetLookUpDegrees;
        /// <summary>
        /// Current velocity of the _lookUpDegrees smoothing
        /// </summary>
        protected float _lookUpVelocity;

        protected virtual void Awake() {
        }

        protected virtual void Start() {
        }

        public virtual void ConstrainPitch(ref float pitch, Vector3 pivotPosition, Vector3 cameraPosition, Vector3 viewport) {
            bool lookUpInProgress = false;
            float cameraDistance = (cameraPosition - transform.position).magnitude;
            if (Utils.TagBasedRaycast(cameraPosition, Vector3.down, out RaycastHit hit, 2.0f * cameraDistance, CheckedLayers, QueryTriggerInteraction.Ignore, CausingTag)) {
                float groundToPivotHeight = pivotPosition.y - hit.point.y - viewport.y;

                Vector3 groundToPivot = pivotPosition - hit.point;
                groundToPivot.y = 0;
                float groundToPivotDistance = groundToPivot.magnitude;

                float lookUpThresholdAngle = -Mathf.Atan(groundToPivotHeight / groundToPivotDistance) * Mathf.Rad2Deg;

                if (pitch <= lookUpThresholdAngle || _lookUpDegrees > 0) {
                    lookUpInProgress = true;
                    _targetLookUpDegrees -= pitch - lookUpThresholdAngle;
                    _targetLookUpDegrees = Mathf.Min(_targetLookUpDegrees, MaxPitch + lookUpThresholdAngle);
                    _lookUpDegrees = Mathf.SmoothDamp(_lookUpDegrees, _targetLookUpDegrees, ref _lookUpVelocity, RotationSmoothTime);
                    pitch = lookUpThresholdAngle;
                }
            }

            if (!lookUpInProgress) {
                _lookUpDegrees = 0;
                _targetLookUpDegrees = 0;
                _lookUpVelocity = 0;
            }
        }

        public virtual float GetLookUpDegrees() {
            return _lookUpDegrees;
        }
    }
}
