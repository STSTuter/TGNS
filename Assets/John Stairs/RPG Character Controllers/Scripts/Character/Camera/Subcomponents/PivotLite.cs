using System;
using UnityEngine;

namespace JohnStairs.RPG.Character.Cam.Subcomponents {
    public class PivotLite : MonoBehaviour, IPivot {
        /// <summary>
        /// Position of the pivot in local character coordinates. Turn on Gizmos to display it as a small cyan sphere
        /// </summary>
        [Tooltip("Position of the pivot in local character coordinates. Turn on Gizmos to display it as a small cyan sphere.")]
        public Vector3 LocalPosition = new(0, 1.2f, 0);
        /// <summary>
        /// The time needed to reach the new pivot position while the character is moving
        /// </summary>
        [Tooltip("The time needed to reach the new pivot position while the character is moving.")]
        public float PositionSmoothTime = 0.1f;

        /// <summary>
        /// Current pivot movement smoothing velocity
        /// </summary>
        protected Vector3 _velocity;
        /// <summary>
        /// Current pivot position in world coordinates
        /// </summary>
        protected Vector3 _position;

        protected virtual void Awake() {
        }

        protected virtual void Start() {
        }

        public virtual void Init(float yaw) {
            _position = GetDesiredPivotPosition(transform.position, yaw);
        }

        public virtual Vector3 GetPosition(Vector3 anchor, float yaw, Vector3 viewport, LayerMask obstacleLayers) {
            Vector3 desiredPosition = GetDesiredPivotPosition(anchor, yaw);
            _position = Vector3.SmoothDamp(_position, desiredPosition, ref _velocity, GetPositionSmoothTime());
            return _position;
        }

        protected virtual Vector3 GetDesiredPivotPosition(Vector3 anchor, float yawDegrees) {
            Quaternion yawRotation = Quaternion.AngleAxis(yawDegrees, Vector3.up);
            return anchor + yawRotation * LocalPosition;
        }

        protected virtual Vector3 GetPivotAnchorPosition() {
            return GetComponent<CharacterController>()?.GetBottomSphereCenter() ?? transform.position;
        }

        protected float GetPositionSmoothTime() {
            if (IsBeingTransported()) {
                return 0;
            } else {
                return PositionSmoothTime;
            }
        }

        public virtual void SetLocalPosition(Vector3 localPosition) {
            LocalPosition = localPosition;
        }
        protected virtual bool IsBeingTransported() {
            return transform.root != transform;
        }

        /// <summary>
        /// If Gizmos are enabled, this method draws some utility/debugging spheres
        /// </summary>
        protected virtual void OnDrawGizmos() {
            Color cyan = new(0.0f, 1.0f, 1.0f, 0.65f);
            Gizmos.color = cyan;

            if (Application.isPlaying) {
                Gizmos.DrawSphere(_position, 0.1f);
            } else {
                Gizmos.DrawSphere(GetPivotAnchorPosition() + transform.TransformDirection(LocalPosition), 0.1f);
            }
        }
    }
}
