using System.Collections;
using JohnStairs.RPG.Character.Motor.Enums;
using UnityEngine;

namespace JohnStairs.RPG.Character.Motor.Subcomponents {
    public class ClimbingHandler : MonoBehaviour, IClimbingHandler {
        /// <summary>
        /// Objects of these layers can be climbed on
        /// </summary>
        [Tooltip("Objects of these layers can be climbed on")]
        public LayerMask ClimbableLayers = 128;
        /// <summary>         
        /// Movement speed when climbing
        /// </summary>        
        [Tooltip("Movement speed when climbing")]
        public float ClimbingSpeed = 3.0f;
        /// <summary>
        /// Height of the hands during climbing
        /// </summary>
        [Tooltip("Height of the hands during climbing")]
        public float HandsGrabHeight = 1.6f;
        /// <summary>
        /// Height of the feet during climbing
        /// </summary>
        [Tooltip("Height of the feet during climbing")]
        public float FeetGrabHeight = 0.3f;
        /// <summary>
        /// The distance between the left and the right arm or leg
        /// </summary>
        [Tooltip("The distance between the left and the right arm or leg")]
        public float GrabRange = 0.4f;
        /// <summary>
        /// Diameter of hand and foot checks
        /// </summary>
        [Tooltip("Diameter of hand and foot checks")]
        public float GrabDiameter = 0.1f;
        /// <summary>
        /// The maximum distance the character can grab a climbable surface to start climbing
        /// </summary>
        [Tooltip("The maximum distance the character can grab a climbable surface to start climbing")]
        public float MaxGrabDistance = 0.5f;
        /// <summary>
        /// The time in seconds which has to pass before the next ledge grab is possible
        /// </summary>
        [Tooltip("The time in seconds which has to pass before the next ledge grab is possible")]
        public float LedgeGrabCooldown = 0.5f;
        /// <summary>
        /// The speed the character pulls up ledges
        /// </summary>
        [Tooltip("The speed the character pulls up ledges")]
        public float LedgePullUpSpeed = 3.0f;
        /// <summary>
        /// Vertical tolerance for ledge pull-up checks
        /// </summary>
        [Tooltip("Vertical tolerance for ledge pull-up checks")]
        public float LedgePullUpCheckHeight = 0.2f;
        /// <summary>
        /// Draw gizmos for ledge grab detection
        /// </summary>
        [Tooltip("Draw gizmos for ledge grab detection")]
        public bool DrawLedgeGrabGizmos = false;
        /// <summary>
        /// Draw gizmos for general climbing checks
        /// </summary>
        [Tooltip("Draw gizmos for general climbing checks")]
        public bool DrawClimbingGizmos = false;
        /// <summary>
        /// Draw gizmos for ledge pull-up checks
        /// </summary>
        [Tooltip("Draw gizmos for ledge pull-up checks")]
        public bool DrawLedgePullUpGizmos = false;

        /// <summary>
        /// Reference to the character controller component
        /// </summary>
        protected CharacterController _characterController;
        /// <summary>
        /// Current climbing state of the character
        /// </summary>
        protected ClimbingState _climbingState;
        /// <summary>
        /// The current surface hit used for climb surface calculations
        /// </summary>
        protected RaycastHit _climbSurfaceHit;
        /// <summary>
        /// The current ledge hit used for ledge grab movement
        /// </summary>
        protected RaycastHit _ledgeHit;
        /// <summary>
        /// Duration of the current ledge grab
        /// </summary>
        protected float _ledgeGrabDuration;
        /// <summary>
        /// If true, the next ledge grab is on cooldown
        /// </summary>
        protected bool _ledgeGrabOnCooldown;
        /// <summary>
        /// Timeout used to prevent getting stuck while climbing
        /// </summary>
        protected float _antiStuckTimeout = 1.0f;
        /// <summary>
        /// Buffer for measuring how long climbing is no longer possible
        /// </summary>
        protected float _stuckBuffer;

        protected virtual void Awake() {
            _characterController = GetComponent<CharacterController>();
        }

        protected virtual void FixedUpdate() {
            if (CanGrabLedge()) {
                _climbingState = ClimbingState.GrabbingLedge;
            }
        }

        public virtual ClimbingState GetClimbingState() {
            return _climbingState;
        }

        public virtual Vector3 GetMovementVector(Vector3 inputMovementVector, bool cannotFall) {
            inputMovementVector = TransformToClimbingMovementInput(inputMovementVector);

            if (cannotFall && IsClimbingDownwards(inputMovementVector)
                || StuckWhileClimbing()) {
                StopClimbing();
                return Vector3.zero;
            }

            return _climbingState switch {
                ClimbingState.GrabbingLedge => GetLedgeMovementVector(inputMovementVector),
                ClimbingState.PullingUp => GetPullingUpMovementVector(inputMovementVector),
                ClimbingState.Climbing => GetClimbingMovementVector(inputMovementVector),
                _ => Vector3.zero,
            };
        }

        public virtual FacingDirection GetFacingDirection() {
            Vector3 forward = -_climbSurfaceHit.normal;
            Vector3 up = Vector3.up;
            Vector3.OrthoNormalize(ref forward, ref up);
            return new FacingDirection(forward, up);
        }

        protected virtual bool StuckWhileClimbing() {
            if ((CheckClimbingPossibility(transform.position) || CheckLedgeGrabPossibility(transform.position, out _))
                && !StuckWhilePullingUp()) {
                _stuckBuffer = _antiStuckTimeout;
                return false;
            } else {
                _stuckBuffer -= Time.deltaTime;
                return _stuckBuffer < 0;
            }
        }

        protected virtual bool StuckWhilePullingUp() {
            return _climbingState == ClimbingState.PullingUp && _characterController.velocity.magnitude < 0.05f;
        }

        protected virtual Vector3 GetLedgeMovementVector(Vector3 inputMovementVector) {
            _ledgeGrabDuration += Time.deltaTime;

            if (IsClimbingDownwards(inputMovementVector)) {
                _ledgeGrabDuration = 0;
                StartCoroutine(LedgeGrabCooldownCoroutine());
                _climbingState = ClimbingState.Climbing;
                return Vector3.zero;
            } else if (IsClimbingUpwards(inputMovementVector)
                        && _ledgeGrabDuration > 0.5f
                        && CheckPullUpPossibility()) {
                _climbingState = ClimbingState.PullingUp;
                return Vector3.zero;
            }

            _climbSurfaceHit = GetClimbSurfaceHit(transform.position);
            if (_climbSurfaceHit.normal.y < 0) {
                _climbSurfaceHit.normal = new Vector3(_climbSurfaceHit.normal.x, 0, _climbSurfaceHit.normal.z).normalized; // let the character hang down the ledge
            }

            inputMovementVector.y = 0; // only allow horizontal movement
            Vector3 movementVector = ProjectOnLedgePlane(inputMovementVector, _ledgeHit.normal) * ClimbingSpeed;
            Vector3 desiredPosition = transform.position + movementVector * Time.deltaTime;
            if (CheckLedgeGrabPossibility(desiredPosition, out _ledgeHit)) {
                movementVector = ProjectOnLedgePlane(movementVector, _ledgeHit.normal) * ClimbingSpeed;
                return movementVector + (GetHeightDeltaToGrabbedLedge(_ledgeHit.point) + GetDragToClimbSurface(_climbSurfaceHit)) * 20.0f; // drag towards the ledge
            } else {
                return Vector3.zero;
            }
        }

        protected virtual Vector3 GetPullingUpMovementVector(Vector3 inputMovementVector) {
            Vector3 movementDirection = Vector3.zero;
            if (transform.position.y < _ledgeHit.point.y + LedgePullUpCheckHeight) {
                movementDirection = Vector3.up;
            } else if (transform.InverseTransformDirection(_ledgeHit.point - transform.position).z > 0) {
                movementDirection = transform.forward;
            } else {
                StopClimbing();
            }

            return movementDirection * LedgePullUpSpeed;
        }

        protected virtual Vector3 GetClimbingMovementVector(Vector3 inputMovementVector) {
            _climbSurfaceHit = GetClimbSurfaceHit(transform.position);

            Vector3 movementDirection = Vector3.ProjectOnPlane(inputMovementVector, _climbSurfaceHit.normal).normalized;
            Vector3 desiredPosition = transform.position + movementDirection * GrabDiameter;
            if (CheckClimbingPossibility(desiredPosition)) {
                return movementDirection * ClimbingSpeed + GetDragToClimbSurface(_climbSurfaceHit); // drag towards the climbing surface
            } else {
                return Vector3.zero;
            }
        }

        protected virtual Vector3 TransformToClimbingMovementInput(Vector3 inputMovementVector) {
            inputMovementVector.y = 0;
            inputMovementVector = transform.InverseTransformVector(inputMovementVector);
            inputMovementVector = new Vector3(inputMovementVector.x, inputMovementVector.z, 0);
            return transform.TransformVector(inputMovementVector);
        }

        protected virtual Vector3 GetHeightDeltaToGrabbedLedge(Vector3 ledgeHitPoint) {
            return new Vector3(0, ledgeHitPoint.y - (transform.position.y + HandsGrabHeight + GrabDiameter + 0.1f), 0);
        }

        protected virtual Vector3 GetDragToClimbSurface(RaycastHit climbSurfaceHit) {
            Vector3 dragDirection = Vector3.ProjectOnPlane(climbSurfaceHit.normal, Vector3.up).normalized;
            return (_characterController.GetActualRadius() - climbSurfaceHit.distance) * dragDirection;
        }

        protected virtual Vector3 ProjectOnLedgePlane(Vector3 inputMovementVector, Vector3 ledgeNormal) {
            return Vector3.ProjectOnPlane(inputMovementVector, ledgeNormal).normalized;
        }

        protected virtual bool CanGrabLedge() {
            return _climbingState != ClimbingState.GrabbingLedge
                    && _climbingState != ClimbingState.PullingUp
                    && !_ledgeGrabOnCooldown
                    && CheckLedgeGrabPossibility(transform.position, out _);
        }

        protected virtual bool CheckLedgeGrabPossibility(Vector3 atPosition, out RaycastHit ledgeHit, bool drawGizmos = false) {
            Vector3 handsPosition = atPosition + transform.up * HandsGrabHeight;
            Vector3 right = transform.right * 0.5f * GrabRange;
            Vector3 forward = transform.forward;
            float actualGrabDistance = GetActualGrabDistance();

            Vector3 origin = handsPosition + transform.up * (actualGrabDistance * 0.5f + GrabDiameter) + forward * (_characterController.GetActualRadius() + GrabDiameter);
            bool result = LedgeGrabSphereCast(origin - right, out RaycastHit ledgeHitLeft, drawGizmos)
                    & LedgeGrabSphereCast(origin + right, out RaycastHit ledgeHitRight, drawGizmos);
            //&& ClimbingSphereCast(handsPosition + transform.up * GrabDiameter, out _, true);

            ledgeHit = new RaycastHit {
                point = (ledgeHitLeft.point + ledgeHitRight.point) * 0.5f,
                normal = (ledgeHitLeft.normal + ledgeHitRight.normal).normalized
            };

            return result;
        }

        protected virtual bool CheckPullUpPossibility(bool drawGizmos = false) {
            float colliderRadius = _characterController.GetActualRadius();
            Vector3 start = _ledgeHit.point + Vector3.up * (colliderRadius + LedgePullUpCheckHeight);
            Vector3 end = _ledgeHit.point + Vector3.up * (_characterController.GetActualHeight() - colliderRadius + LedgePullUpCheckHeight);
            bool result = !Physics.CheckCapsule(start, end, colliderRadius);

            if (drawGizmos) {
                DrawSphereCastGizmo(start, end - start, colliderRadius, result);
            }

            return result;
        }

        protected virtual bool CheckClimbingPossibility(Vector3 atPosition, bool drawGizmos = false) {
            Vector3 up = transform.up;
            Vector3 right = transform.right * 0.5f * GrabRange;

            // Check both hands and feet for climbing possibility
            return ClimbingSphereCast(atPosition + up * HandsGrabHeight - right, out _, drawGizmos)
                & ClimbingSphereCast(atPosition + up * HandsGrabHeight + right, out _, drawGizmos)
                & ClimbingSphereCast(atPosition + up * FeetGrabHeight - right, out _, drawGizmos)
                & ClimbingSphereCast(atPosition + up * FeetGrabHeight + right, out _, drawGizmos);
        }

        protected virtual bool LedgeGrabSphereCast(Vector3 origin, out RaycastHit surfaceHit, bool drawGizmos = false) {
            return SphereCast(origin, Vector3.down, out surfaceHit, drawGizmos);
        }

        protected virtual bool ClimbingSphereCast(Vector3 origin, out RaycastHit surfaceHit, bool drawGizmos = false) {
            return SphereCast(origin, transform.forward, out surfaceHit, drawGizmos);
        }

        protected virtual bool SphereCast(Vector3 origin, Vector3 direction, out RaycastHit surfaceHit, bool drawGizmos = false) {
            bool result = false;
            surfaceHit = new RaycastHit {
                normal = -direction,
                distance = Mathf.Infinity
            };

            float actualGrabDistance = GetActualGrabDistance();
            Ray ray = new(origin, direction);
            float radius = GrabDiameter * 0.5f;

            RaycastHit[] surfaceHits = Physics.SphereCastAll(ray, radius, actualGrabDistance, ClimbableLayers, QueryTriggerInteraction.Ignore);
            if (surfaceHits.Length == 0) {
                surfaceHit.distance = 0;
            } else {
                foreach (RaycastHit hit in surfaceHits) {
                    if (hit.point == Vector3.zero) {
                        // Collider overlaps at the start
                        surfaceHit = hit;
                        result = false;
                        break;
                    }

                    // Find closest hit
                    if (hit.distance < surfaceHit.distance) {
                        surfaceHit = hit;
                        result = true;
                    }
                }
            }

            if (drawGizmos) {
                DrawSphereCastGizmo(ray.origin, ray.direction * actualGrabDistance, radius, result);
            }

            return result;
        }

        protected virtual float GetActualGrabDistance() {
            return _characterController.GetActualRadius() + MaxGrabDistance;
        }

        protected virtual RaycastHit GetClimbSurfaceHit(Vector3 atPosition, bool drawGizmos = false) {
            RaycastHit climbSurfaceHit;
            Vector3 handsPosition = atPosition + transform.up * HandsGrabHeight;

            if (_climbingState == ClimbingState.GrabbingLedge) {
                ClimbingSphereCast(handsPosition, out RaycastHit surfaceHit);
                climbSurfaceHit = surfaceHit;
            } else {
                Vector3 right = transform.right * 0.5f * GrabRange;
                ClimbingSphereCast(handsPosition - right, out RaycastHit surfaceHitLeft);
                ClimbingSphereCast(handsPosition + right, out RaycastHit surfaceHitRight);

                climbSurfaceHit = new RaycastHit {
                    point = handsPosition,
                    normal = (surfaceHitLeft.normal + surfaceHitRight.normal).normalized,
                    distance = surfaceHitRight.distance
                };
            }

            if (drawGizmos) {
                DrawRayCastGizmo(climbSurfaceHit.point, climbSurfaceHit.normal);
            }

            return climbSurfaceHit;
        }

        protected virtual bool IsClimbingUpwards(Vector3 inputMovementVector) {
            return inputMovementVector.y > 0.5f;
        }

        protected virtual bool IsClimbingDownwards(Vector3 inputMovementVector) {
            return inputMovementVector.y < -0.5f;
        }

        public virtual void StopClimbing() {
            _ledgeGrabDuration = 0;
            if (_climbingState == ClimbingState.GrabbingLedge) {
                StartCoroutine(LedgeGrabCooldownCoroutine());
            }
            _climbingState = ClimbingState.None;
        }

        protected virtual IEnumerator LedgeGrabCooldownCoroutine() {
            _ledgeGrabOnCooldown = true;
            yield return new WaitForSeconds(LedgeGrabCooldown);
            _ledgeGrabOnCooldown = false;
        }

        protected virtual bool IsBeingTransported() {
            return transform.root != transform;
        }

        /// <summary>
        /// "OnControllerColliderHit is called when the controller hits a collider while performing a Move" - Unity Documentation
        /// </summary>
        public virtual void OnControllerColliderHit(ControllerColliderHit hit) {
            if (Utils.LayerInLayerMask(hit.gameObject.layer, ClimbableLayers)
                && _climbingState == ClimbingState.None
                && !IsBeingTransported()
                && CheckClimbingPossibility(transform.position)) {
                _climbingState = ClimbingState.Climbing;
            }
        }

        protected virtual void DrawRayCastGizmo(Vector3 from, Vector3 direction) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(from, direction);
        }

        protected virtual void DrawSphereCastGizmo(Vector3 origin, Vector3 direction, float radius, bool result) {
            Gizmos.color = result ? Color.green : Color.red;
            Gizmos.DrawWireSphere(origin, radius);
            Gizmos.DrawRay(origin + Vector3.forward * radius, direction);
            Gizmos.DrawRay(origin - Vector3.forward * radius, direction);
            Gizmos.DrawRay(origin + Vector3.right * radius, direction);
            Gizmos.DrawRay(origin - Vector3.right * radius, direction);
            Gizmos.DrawRay(origin + Vector3.up * radius, direction);
            Gizmos.DrawRay(origin - Vector3.up * radius, direction);
            Gizmos.DrawWireSphere(origin + direction, radius);
        }

        protected virtual void OnDrawGizmosSelected() {
            if (!_characterController) {
                _characterController = GetComponent<CharacterController>();
            }
            CheckLedgeGrabPossibility(transform.position, out _, DrawLedgeGrabGizmos);
            if (_climbingState == ClimbingState.GrabbingLedge) {
                CheckPullUpPossibility(DrawLedgePullUpGizmos);
            }
            CheckClimbingPossibility(transform.position, DrawClimbingGizmos);
        }
    }
}
