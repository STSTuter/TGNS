using System;
using System.Collections.Generic;
using JohnStairs.RPG.Character.Cam.Subcomponents;
using JohnStairs.RPG.Character.Cam.Subcomponents.Enums;
using UnityEngine;

namespace JohnStairs.RPG.Character.Cam {
    public class RPGCameraLite : MonoBehaviour, ICameraLite {
        /// <summary>
        /// Camera component that is controlled by this script. If no camera object is assigned, the scene's Main Camera is used
        /// </summary>
        [Tooltip("Camera component that is controlled by this script. If no camera object is assigned, the scene's Main Camera is used.")]
        public Camera UsedCamera;
        /// <summary>
        /// Defines margins for the viewport in x and y direction
        /// </summary>
        [Tooltip("Defines margins for the viewport in x and y direction.")]
        public Vector2 ViewportMargin = new(0.2f, 0.2f);
        /// <summary>
        /// The time needed for the camera to orbit around its pivot. The higher the smoother the orbiting
        /// </summary>
        [Tooltip("The time needed for the camera to orbit around its pivot. The higher the smoother the orbiting.")]
        public float RotationSmoothTime = 0.1f;
        /// <summary>
        /// Controls how the camera distance/zoom should be smoothed
        /// </summary>
        [Tooltip("Controls how the camera distance/zoom should be smoothed.")]
        public Smoothing DistanceSmoothing = Smoothing.SmoothDamp;
        /// <summary>
        /// The time needed to zoom in and out a step
        /// </summary>
        [Tooltip("The time needed to zoom in and out a step.")]
        public float DistanceSmoothTime = 0.4f;
        /// <summary>
        /// Minimum pitch angle in degrees
        /// </summary>
        [Tooltip("Minimum pitch angle in degrees.")]
        public float MinPitch = -89.9f;
        /// <summary>
        /// Maximum pitch angle in degrees
        /// </summary>
        [Tooltip("Maximum pitch angle in degrees.")]
        public float MaxPitch = 89.9f;
        /// <summary>
        /// Minimum distance to the character
        /// </summary>
        [Tooltip("Minimum distance to the character.")]
        public float MinDistance = 0;
        /// <summary>
        /// Maximummum distance to the character
        /// </summary>
        [Tooltip("Maximum distance to the character.")]
        public float MaxDistance = 20.0f;
        /// <summary>
        /// If set to true, yawing the camera is not possible
        /// </summary>
        [Tooltip("If set to true, yawing the camera is not possible.")]
        public bool LockYaw;
        /// <summary>
        /// If set to true, pitching the camera is not possible
        /// </summary>
        [Tooltip("If set to true, pitching the camera is not possible.")]
        public bool LockPitch;
        /// <summary>
        /// If set to true, zooming in/out is not possible
        /// </summary>
        [Tooltip("If set to true, zooming in/out is not possible.")]
        public bool LockDistance;
        /// <summary>
        /// The camera's starting degrees on the horizontal axis
        /// </summary>
        [Tooltip("The camera's starting degrees on the horizontal axis.")]
        public float StartYaw = 0;
        /// <summary>
        /// If set to true, the start rotation will be relative to the character's start rotation (transform.forward)
        /// </summary>
        [Tooltip("If set to true, the start rotation will be relative to the character's start rotation (transform.forward).")]
        public bool StartYawRelativeToCharacterRotation = true;
        /// <summary>
        /// The camera's starting degrees on the vertical axis
        /// </summary>
        [Tooltip("The camera's starting degrees on the vertical axis.")]
        public float StartPitch = 15.0f;
        /// <summary>
        /// The camera's starting distance
        /// </summary>
        [Tooltip("The camera's starting distance.")]
        public float StartDistance = 7.5f;

        /// <summary>
        /// Reference to a pivot subcomponent
        /// </summary>
        protected IPivot _pivot;
        /// <summary>
        /// Reference to a view frustum subcomponent
        /// </summary>
        protected IViewFrustum _viewFrustum;
        /// <summary>
        /// Reference to an occlusion handler subcomponent
        /// </summary>
        protected IOcclusionHandler _occlusionHandler;
        /// <summary>
        /// Reference to a character fading handler subcomponent
        /// </summary>
        protected ICharacterFadingHandler _characterFadingHandler;
        /// <summary>
        /// Reference to an underwater handler subcomponent
        /// </summary>
        protected IUnderwaterHandler _underwaterHandler;
        /// <summary>
        /// Reference to a look up behavior subcomponent
        /// </summary>
        protected ILookUpBehavior _lookUpBehavior;
        /// <summary>
        /// Reference to an optional Character Controller component for better pivot position calculation
        /// </summary>
        protected CharacterController _characterController;
        /// <summary>
        /// Current yaw angle in degrees
        /// </summary>
        protected float _yaw;
        /// <summary>
        /// Targeted yaw angle in degrees
        /// </summary>
        protected float _targetYaw;
        /// <summary>
        /// Current yaw smoothing velocity
        /// </summary>
        protected float _yawVelocity;
        /// <summary>
        /// Current pitch angle in degrees
        /// </summary>
        protected float _pitch;
        /// <summary>
        /// Targeted pitch angle in degrees
        /// </summary>
        protected float _targetPitch;
        /// <summary>
        /// Current pitch smoothing velocity
        /// </summary>
        protected float _pitchVelocity;
        /// <summary>
        /// Current distance in world units
        /// </summary>
        protected float _distance;
        /// <summary>
        /// Targeted distance in world units
        /// </summary>
        protected float _targetDistance;
        /// <summary>
        /// Used rotation smooth time for orbiting
        /// </summary>
        protected float _rotationSmoothTime;
        /// <summary>
        /// Current distance smoothing velocity
        /// </summary>
        protected float _distanceVelocity;
        /// <summary>
        /// Maximum number of occlusion checks (usually only relevant for a non-cuboid view frustum)
        /// </summary>
        protected int _maxOcclusionChecks = 5;
        /// <summary>
        /// Current position of the pivot
        /// </summary>
        protected Vector3 _pivotPosition;
        /// <summary>
        /// If true, the look up behavior will be prevented from applying any adjustments to the camera rotation
        /// </summary>
        protected bool _preventLookUpBehavior;

        protected virtual void Awake() {
            _pivot = GetComponent<IPivot>();
            _viewFrustum = GetComponent<IViewFrustum>();
            _occlusionHandler = GetComponent<IOcclusionHandler>();
            _characterFadingHandler = GetComponent<ICharacterFadingHandler>();
            _underwaterHandler = GetComponent<IUnderwaterHandler>();
            _lookUpBehavior = GetComponent<ILookUpBehavior>();
            _characterController = GetComponent<CharacterController>();
        }

        protected virtual void Start() {
            UsedCamera = DetermineUsedCamera();
            ResetRotationSmoothTime();
            ResetView(true);
            _pivot?.Init(_yaw);
        }

        protected virtual void LateUpdate() {
            UsedCamera = DetermineUsedCamera();

            if (UsedCamera == null) {
                return;
            }

            Vector3 viewport = GetViewport(UsedCamera);

            _targetPitch = Mathf.Clamp(_targetPitch, MinPitch, MaxPitch);
            _targetDistance = Mathf.Clamp(_targetDistance, MinDistance, MaxDistance);

            if (!_preventLookUpBehavior) {
                _lookUpBehavior?.ConstrainPitch(ref _targetPitch, _pivotPosition, UsedCamera.transform.position, viewport);
            }

            _yaw = Mathf.SmoothDamp(_yaw, _targetYaw, ref _yawVelocity, _rotationSmoothTime);
            _pitch = Mathf.SmoothDamp(_pitch, _targetPitch, ref _pitchVelocity, _rotationSmoothTime);

            LayerMask zoomConditionsLayers = _occlusionHandler?.GetZoomConditionsLayerMask() ?? 1;
            _pivotPosition = _pivot?.GetPosition(GetPivotAnchorPosition(), _yaw, viewport, zoomConditionsLayers) ?? transform.position;

            _distance = ComputeDistance(_pivotPosition, viewport, out List<GameObject> objectsInBetween);
            _occlusionHandler?.HandleObjectVisibility(objectsInBetween);

            float usedPitch = _underwaterHandler?.ApplyWaterLevelSkip(_pivotPosition, _distance, viewport, _pitch) ?? _pitch;

            UsedCamera.transform.position = ComputeCameraPosition(_pivotPosition, usedPitch, _yaw, _distance);

            _characterFadingHandler?.HandleCharacterVisibility(UsedCamera, viewport);

            if (_underwaterHandler != null) {
                if (_underwaterHandler.IsUnderwater(UsedCamera, viewport)) {
                    _underwaterHandler.EnableEffects();
                } else {
                    _underwaterHandler.DisableEffects();
                }
            }

            // Check if we are in third or first person and adjust the camera rotation behavior
            if (EnableFirstPersonMode()) {
                // Normal camera rotation
                UsedCamera.transform.rotation = Quaternion.Euler(new Vector3(_pitch, _yaw, 0));
            } else {
                // Orbit camera
                UsedCamera.transform.LookAt(_pivotPosition);
                // Look up
                UsedCamera.transform.Rotate(Vector3.left, GetLookUpDegrees());
            }
        }

        protected virtual bool EnableFirstPersonMode() {
            return _distance < 0.1f;
        }

        /// <summary>
        /// Determines and returns the camera component that should be used controlled by this script. If no camera was manually assigned, then the scene's Main Camera is used
        /// </summary>
        /// <returns>Camera component to be controlled</returns>
        protected virtual Camera DetermineUsedCamera() {
            if (UsedCamera) {
                return UsedCamera;
            } else {
                return Camera.main;
            }
        }

        public virtual Camera GetUsedCamera() {
            return UsedCamera;
        }

        protected virtual float SmoothDistance(float from, float to, ref float velocity, float duration) {
            if (DistanceSmoothing == Smoothing.Linear) {
                return Utils.LinearTransition(from, to, duration);
            } else {
                return Mathf.SmoothDamp(from, to, ref velocity, duration);
            }
        }

        protected virtual Vector3 GetPivotAnchorPosition() {
            return _characterController?.GetBottomSphereCenter() ?? transform.position;
        }

        /// <summary>
        /// Computes the camera position based on the given parameters as if there were no obstacles
        /// </summary>
        /// <param name="pivotPosition">Position of the camera pivot, i.e. its anchor point</param>
        /// <param name="pitchDegrees">Orbital rotation degrees around the X axis (up/down)</param>
        /// <param name="yawDegrees">Orbital rotation degrees around the Y axis (left/right)</param>
        /// <param name="distance">Distance to the character</param>
        /// <returns>Computed orbital camera position</returns>
        protected virtual Vector3 ComputeCameraPosition(Vector3 pivotPosition, float pitchDegrees, float yawDegrees, float distance) {
            Vector3 offset = -Vector3.forward * distance;
            // Create the combined rotation of X and Y axis rotation
            Quaternion pitchRotation = Quaternion.AngleAxis(pitchDegrees, Vector3.right);
            Quaternion yawRotation = Quaternion.AngleAxis(yawDegrees, Vector3.up);
            Quaternion rotation = yawRotation * pitchRotation;

            return pivotPosition + rotation * offset;
        }

        protected virtual float ComputeDistance(Vector3 pivotPosition, Vector3 viewport, out List<GameObject> objectsInBetween) {
            Vector3 desiredPosition = ComputeCameraPosition(pivotPosition, _pitch, _yaw, _targetDistance);
            _viewFrustum?.DrawFrustum(pivotPosition, desiredPosition, viewport, UsedCamera.transform.position);

            float closestDistance = ComputeClosestDistance(pivotPosition, desiredPosition, viewport, out objectsInBetween);

            bool desiredViewIsClear = closestDistance == Mathf.Infinity;
            if (desiredViewIsClear) {
                closestDistance = _targetDistance;
            }

            if (desiredViewIsClear || _distance < closestDistance) {
                return SmoothDistance(_distance, closestDistance, ref _distanceVelocity, DistanceSmoothTime);
            } else {
                return closestDistance;
            }
        }

        /// <summary>
        /// Computes the closest possible distance to the desired position (without occlusion that causes the camera to zoom in)
        /// </summary>
        /// <param name="pivotPosition">Position of the camera pivot</param>
        /// <param name="desiredPosition">Position to compute the distance to</param>
        /// <param name="viewport">Camera viewport</param>
        /// <param name="objectsInBetween">Objects along the closest possible distance</param>
        /// <returns>Closest distance on the way to the given desired position</returns>
        protected virtual float ComputeClosestDistance(Vector3 pivotPosition, Vector3 desiredPosition, Vector3 viewport, out List<GameObject> objectsInBetween) {
            float closestDistance;
            float tempHitDistance = Mathf.Infinity;
            Vector3 tempPosition = desiredPosition;
            Vector3 frustumDirection = (desiredPosition - pivotPosition).normalized;
            int occlusionChecks = 0;
            do {
                closestDistance = tempHitDistance;
                RaycastHit[] objectHits = GetObjectHitsInFrustum(pivotPosition, tempPosition, viewport);
                tempHitDistance = GetClosestHitDistance(objectHits, out objectsInBetween) + viewport.z - 0.1f;
                occlusionChecks++;
                tempPosition = pivotPosition + frustumDirection * tempHitDistance;
            } while (tempHitDistance != Mathf.Infinity && occlusionChecks < _maxOcclusionChecks);
            return closestDistance;
        }

        protected virtual RaycastHit[] GetObjectHitsInFrustum(Vector3 from, Vector3 to, Vector3 viewport) {
            if (_viewFrustum == null) {
                return Array.Empty<RaycastHit>();
            } else {
                return _viewFrustum?.GetObjectHitsInFrustum(from, to, viewport);
            }
        }

        protected virtual float GetClosestHitDistance(RaycastHit[] objectHits, out List<GameObject> objectsInBetween) {
            if (_occlusionHandler == null) {
                objectsInBetween = new List<GameObject>();
                return Mathf.Infinity;
            } else {
                return _occlusionHandler.GetClosestHitDistance(objectHits, out objectsInBetween);
            }
        }

        public virtual void Yaw(float degrees, bool instant = false) {
            if (!LockYaw) {
                _targetYaw += degrees;

                if (instant) {
                    _yaw += degrees;
                }
            }
        }

        public virtual void Pitch(float degrees, bool instant = false) {
            if (!LockPitch) {
                _targetPitch += degrees;

                if (instant) {
                    _pitch += degrees;
                }
            }
        }

        public virtual void Zoom(float units, bool instant = false) {
            if (!LockDistance) {
                _targetDistance += units;

                if (instant) {
                    _distance += units;
                }
            }
        }

        protected virtual Vector3 GetViewport(Camera camera) {
            Vector3 result;
            float halfFieldOfView = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            result.y = camera.nearClipPlane * Mathf.Tan(halfFieldOfView);
            result.x = result.y * camera.aspect + ViewportMargin.x;
            result.y += ViewportMargin.y;
            result.z = camera.nearClipPlane;
            return result;
        }

        /// <summary>
        /// Gets the camera's start rotation on the horizontal axis, i.e. left/right rotation 
        /// </summary>
        /// <returns>Start rotation on the horizontal axis in degrees</returns>
        protected virtual float GetStartYaw() {
            return StartYaw + (StartYawRelativeToCharacterRotation ? transform.eulerAngles.y : 0);
        }

        public virtual void ResetView(bool instant = true) {
            SetYaw(GetStartYaw(), instant);
            SetPitch(StartPitch, instant);
            SetDistance(StartDistance, instant);
        }

        /// <summary>
        /// Sets the yaw rotation to the given angle in degrees
        /// </summary>
        /// <param name="degrees">Yaw angle in degrees</param>
        /// <param name="instant">If true, the change will not be smoothed and instead applied immediately</param>
        public virtual void SetYaw(float degrees, bool instant = true) {
            _targetYaw += Mathf.DeltaAngle(_targetYaw, degrees);

            if (instant) {
                _yaw = _targetYaw;
            }
        }

        /// <summary>
        /// Sets the pitch rotation to the given angle in degrees
        /// </summary>
        /// <param name="degrees">Pitch angle in degrees</param>
        /// <param name="instant">If true, the change will not be smoothed and instead applied immediately</param>
        public virtual void SetPitch(float degrees, bool instant = true) {
            _targetPitch = degrees;

            if (instant) {
                _pitch = _targetPitch;
            }
        }

        /// <summary>
        /// Sets the camera distance to the given units in world coordinate
        /// </summary>
        /// <param name="units">New camera distance</param>
        /// <param name="instant">If true, the change will not be smoothed and instead applied immediately</param>
        public virtual void SetDistance(float units, bool instant = true) {
            _targetDistance = units;

            if (instant) {
                _distance = units;
            }
        }

        public virtual void ZoomToMinDistance() {
            SetDistance(MinDistance, false);
        }

        public virtual void ZoomToMaxDistance() {
            SetDistance(MaxDistance, false);
        }

        public virtual void AlignWithTransform(Transform transform, bool opposed) {
            float targetYaw = opposed ? transform.eulerAngles.y - 180.0f : transform.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(new Vector3(0, targetYaw, 0));
            // Compute the delta between the current rotation and the target
            Quaternion delta = targetRotation * Quaternion.Inverse(Quaternion.Euler(new Vector3(0, _targetYaw, 0)));
            float deltaEuler = delta.eulerAngles.y;

            if (Utils.IsAlmostEqual(deltaEuler, 0, 0.5f)) {
                // There is no offset to the camera rotation => no alignment computation required
                return;
            }

            if (deltaEuler > 180.0f) {
                deltaEuler = -(360.0f - deltaEuler);
            }

            _targetYaw += deltaEuler * Time.deltaTime / RotationSmoothTime;
        }

        public virtual void SetRotationSmoothTime(float rotationSmoothTime) {
            _rotationSmoothTime = rotationSmoothTime;
        }

        public virtual void ResetRotationSmoothTime() {
            _rotationSmoothTime = RotationSmoothTime;
        }

        public virtual void PreventLookUpBehavior(bool prevent) {
            _preventLookUpBehavior = prevent;
        }

        public virtual bool IsOrbitingLocked() {
            return LockYaw && LockYaw;
        }

        public virtual void Destroy() {
            foreach (var subcomponent in GetComponents<ICameraSubcomponent>()) {
                if (subcomponent is Component component) {
                    Destroy(component);
                }
            }
            Destroy(this);
        }

        protected virtual float GetLookUpDegrees() {
            if (_preventLookUpBehavior) {
                return 0;
            } else {
                return _lookUpBehavior?.GetLookUpDegrees() ?? 0;
            }
        }

        /// <summary>
        /// If Gizmos are enabled, this method draws some utility/debugging spheres
        /// </summary>
        protected virtual void OnDrawGizmos() {
            if (!Application.isPlaying) {
                if (!_characterController) {
                    _characterController = GetComponent<CharacterController>();
                }
                // Draw the currently set up start position of the CameraToUse in yellow
                Gizmos.color = Color.yellow;
                _pivotPosition = GetPivotAnchorPosition() + transform.TransformDirection(GetComponent<PivotLite>()?.LocalPosition ?? Vector3.zero);
                Vector3 cameraStartPosition = ComputeCameraPosition(_pivotPosition, StartPitch, GetStartYaw(), StartDistance);
                Gizmos.DrawSphere(cameraStartPosition, 0.3f);
            }
        }
    }
}
