using UnityEngine;

namespace JohnStairs.RPG.Character.Motor.Subcomponents {
    public class SlidingHandler : MonoBehaviour, ISlidingHandler {
        /// <summary>
        /// If true, the character will slide down slopes that are too steep
        /// </summary>
        [Tooltip("If true, the character will slide down slopes that are too steep")]
        public bool EnableSliding = true;
        /// <summary>
        /// Time in seconds which has to pass before sliding logic is applied
        /// </summary>
        [Tooltip("Time in seconds which has to pass before sliding logic is applied")]
        public float SlidingTimeout = 0.1f;
        /// <summary>
        /// Sliding time in seconds which has to pass before the anti-stuck mechanic enables
        /// </summary>
        [Tooltip("Sliding time in seconds which has to pass before the anti-stuck mechanic enables")]
        public float AntiStuckTimeout = 0.1f;

        /// <summary>
        /// Reference to the character controller component
        /// </summary>
        protected CharacterController _characterController;
        /// <summary>
        /// Walkable layers for the character
        /// </summary>
        protected LayerMask _walkableLayers;
        /// <summary>
        /// Tolerance for being grounded
        /// </summary>
        protected float _groundedTolerance;
        /// <summary>
        /// Buffer for measuring how long consecutive sliding events occured
        /// </summary>
        protected float _slidingBuffer;
        /// <summary>
        /// True if the anti-stuck mechanic is enabled, i.e. the anti-stuck timeout occurred
        /// </summary>
        protected bool _antiStuckEnabled;

        protected virtual void Awake() {
            _characterController = GetComponent<CharacterController>();
        }

        protected virtual void Update() {
        }

        public virtual void Init(LayerMask walkableLayers, float groundedTolerance) {
            _walkableLayers = walkableLayers;
            _groundedTolerance = groundedTolerance;
        }

        public virtual bool CheckSliding(out Vector3 slidingVector) {
            bool slide = false;
            slidingVector = Vector3.zero;

            if (!EnableSliding) {
                return false;
            }

            if (GroundTooSteep(out Vector3 summedNormals)) {
                _slidingBuffer -= Time.deltaTime;

                if (StuckWhileSliding() || _antiStuckEnabled) {
                    _antiStuckEnabled = true;
                    _slidingBuffer = SlidingTimeout;
                } else if (_slidingBuffer < 0) {
                    slide = true;
                    Vector3 slidingDirection = new(summedNormals.x, -summedNormals.y, summedNormals.z);
                    // Normalize the sliding direction and make it orthogonal to the hit normal
                    Vector3.OrthoNormalize(ref summedNormals, ref slidingDirection);

                    slidingVector = slidingDirection * SlopeDegree(summedNormals) * 0.2f;
                    Debug.DrawRay(transform.position + Vector3.up * _characterController.GetActualHeight(), slidingVector, Color.magenta);
                }
            } else {
                _slidingBuffer = SlidingTimeout;
                _antiStuckEnabled = false;
            }

            return slide;
        }

        protected virtual bool GroundTooSteep(out Vector3 summedNormals) {
            bool slide = true;
            summedNormals = Vector3.zero;

            RaycastHit[] groundHits = Physics.SphereCastAll(_characterController.GetBottomSphereCenter(), _characterController.GetActualRadius(), Vector3.down, _groundedTolerance, _walkableLayers, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit groundHit in groundHits) {
                if (SlopeDegree(groundHit.normal) > _characterController.slopeLimit) {
                    summedNormals += groundHit.normal;
                } else {
                    slide = false;
                }
            }
            return slide;
        }

        protected virtual float SlopeDegree(Vector3 slopeNormal) {
            return Mathf.Round(Vector3.Angle(slopeNormal, Vector3.up));
        }

        protected virtual bool StuckWhileSliding() {
            return _slidingBuffer < -AntiStuckTimeout && _characterController.velocity.magnitude < 0.05f;
        }
    }
}
