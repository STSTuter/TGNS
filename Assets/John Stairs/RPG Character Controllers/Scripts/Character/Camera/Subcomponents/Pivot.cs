using System;
using UnityEngine;

namespace JohnStairs.RPG.Character.Cam.Subcomponents {
    public class Pivot : PivotLite, IPivot {
        /// <summary>
        /// Enables an evasive pivot that moves away from obstacles which the player could see through if zooming in enough (internal pivot only)
        /// </summary>
        [Tooltip("Enables an evasive pivot that moves away from obstacles which the player could see through if zooming in enough.")]
        public bool EnableEvasion = true;
        /// <summary>
        /// The time needed for moving the evasive pivot away from the obstacle (internal pivot only)
        /// </summary>
        [Tooltip("The time needed for moving the evasive pivot away from the obstacle.")]
        public float EvasionSmoothTime = 0.2f;

        /// <summary>
        /// Reference to a view frustum between the pivot and its retreat (external pivot only)
        /// </summary>
        protected IViewFrustum _viewFrustum;
        /// <summary>
        /// Reference to an occlusion handler for objects between the pivot and its retreat (external pivot only)
        /// </summary>
        protected IOcclusionHandler _occlusionHandler;
        /// <summary>
        /// Collider assigned to the character object for computing the pivot retreat position (external pivot only)
        /// </summary>
        protected Collider _collider;
        /// <summary>
        /// Computed once on start: If true, the pivot is internal, i.e. inside the character's collider. Controls the activation of needed functionality for each case
        /// </summary>
        protected bool _internal;
        /// <summary>
        /// Current pivot evasive movement direction (intelligent pivot only)
        /// </summary>
        protected Vector3 _evasionVector;
        /// <summary>
        /// Current pivot smoothing velocity during retreat (intelligent pivot only)
        /// </summary>
        protected Vector3 _evasionVelocity;

        protected override void Awake() {
            base.Awake();
            _viewFrustum = GetComponent<IViewFrustum>();
            _occlusionHandler = GetComponent<IOcclusionHandler>();
            _collider = GetComponent<Collider>();
        }

        protected override void Start() {
            base.Start();
        }

        public override void Init(float yaw) {
            base.Init(yaw);
            _internal = IsInternal(_position);
        }

        public virtual bool IsInternal() {
            return IsInternal(transform.TransformPoint(LocalPosition));
        }

        protected virtual bool IsInternal(Vector3 position) {
            // Check assignment since this method can also be called by an Editor script
            if (!_collider) {
                _collider = GetComponent<Collider>();

                // Check again
                if (!_collider) {
                    // No collider component found at all => assume an external pivot
                    return false;
                }
            }
            // Check if within the character collider or not
            return _collider.bounds.Contains(position);
        }

        public override Vector3 GetPosition(Vector3 anchor, float yaw, Vector3 viewport, LayerMask obstacleLayers) {
            Vector3 desiredPosition = GetDesiredPivotPosition(anchor, yaw);
            Vector3 targetPosition;

            if (_internal) {
                targetPosition = GetInternalPosition(desiredPosition, viewport, obstacleLayers);
            } else {
                targetPosition = GetExternalPosition(desiredPosition, viewport);
            }

            _position = Vector3.SmoothDamp(_position, targetPosition, ref _velocity, GetPositionSmoothTime());
            return _position;
        }

        protected virtual Vector3 GetInternalPosition(Vector3 desiredPosition, Vector3 viewport, LayerMask obstacleLayers) {
            if (EnableEvasion) {
                return GetEvasivePivotPosition(desiredPosition, viewport, obstacleLayers);
            } else {
                return desiredPosition;
            }
        }

        protected virtual Vector3 GetExternalPosition(Vector3 desiredPosition, Vector3 viewport) {
            Vector3 colliderHead = _collider?.GetRetreatLocation() ?? transform.position;
            Vector3 retreat = transform.position;
            retreat.y = Mathf.Min(desiredPosition.y, colliderHead.y);

            float safetyDistance = Mathf.Max(viewport.x, viewport.y, viewport.z);
            Vector3 frustumDirection = (desiredPosition - retreat).normalized;
            // Add extra length to have the full distance checked (no near clip plane subtracted)
            Vector3 actualTo = desiredPosition + frustumDirection * (viewport.z + safetyDistance);
            _viewFrustum?.DrawFrustum(retreat, actualTo, viewport, Vector3.zero);
            // Check for occlusion between the pivot retreat and the desired pivot position
            RaycastHit[] objectHits = GetObjectHitsInFrustum(retreat, actualTo, viewport);
            float closestHitDistance = _occlusionHandler?.GetClosestHitDistance(objectHits, out _) ?? Mathf.Infinity;

            if (closestHitDistance == Mathf.Infinity) {
                return desiredPosition;
            } else {
                return retreat + frustumDirection * (closestHitDistance - safetyDistance);
            }
        }

        protected virtual RaycastHit[] GetObjectHitsInFrustum(Vector3 from, Vector3 to, Vector3 viewport) {
            if (_viewFrustum == null) {
                return Array.Empty<RaycastHit>();
            } else {
                return _viewFrustum?.GetObjectHitsInFrustum(from, to, viewport);
            }
        }

        protected virtual Vector3 GetEvasivePivotPosition(Vector3 desiredPosition, Vector3 viewport, LayerMask obstacleLayers) {
            Vector3[] checkVectors = GetEvasionCheckVectors(viewport);
            Vector3 targetEvasionVector = Vector3.zero;
            foreach (Vector3 checkVector in checkVectors) {
                // Cast the ray
                if (Physics.Raycast(desiredPosition, checkVector, out RaycastHit hit, checkVector.magnitude, obstacleLayers, QueryTriggerInteraction.Ignore)
                    && hit.transform.root != transform.root) {
                    // Process the hit
                    targetEvasionVector += (hit.point - desiredPosition).normalized * (hit.distance - checkVector.magnitude);
                }
            }
            // Smooth the resulting evasive movement 
            _evasionVector = Vector3.SmoothDamp(_evasionVector, targetEvasionVector, ref _evasionVelocity, EvasionSmoothTime);
            return desiredPosition + _evasionVector;
        }

        protected virtual Vector3[] GetEvasionCheckVectors(Vector3 viewport) {
            float halfDiagonal = Mathf.Sqrt(viewport.x * viewport.x + viewport.y * viewport.y);
            float horizontal = Mathf.Sqrt(viewport.x * viewport.x + viewport.z * viewport.z) + 0.1f;
            float vertical = Mathf.Sqrt(viewport.y * viewport.y + viewport.z * viewport.z) + 0.1f;
            float diagonal = Mathf.Sqrt(halfDiagonal * halfDiagonal + viewport.z * viewport.z) + 0.1f;
            return new Vector3[] {
                                    Vector3.forward * horizontal,
                                    -Vector3.forward * horizontal,
                                    Vector3.left * horizontal,
                                    Vector3.right * horizontal,
                                    Vector3.up * vertical,
                                    Vector3.down * vertical,
                                    (Vector3.forward + Vector3.up).normalized * diagonal,
                                    (-Vector3.forward + Vector3.up).normalized * diagonal,
                                    (Vector3.left + Vector3.up).normalized * diagonal,
                                    (Vector3.right + Vector3.up).normalized * diagonal,
                                    (Vector3.forward + Vector3.down).normalized * diagonal,
                                    (-Vector3.forward + Vector3.down).normalized * diagonal,
                                    (Vector3.left + Vector3.down).normalized * diagonal,
                                    (Vector3.right + Vector3.down).normalized * diagonal
                                };
        }
    }
}
