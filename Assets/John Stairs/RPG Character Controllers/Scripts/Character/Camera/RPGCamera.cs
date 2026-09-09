using UnityEngine;

namespace JohnStairs.RPG.Character.Cam {
    public class RPGCamera : RPGCameraLite, ICamera {
        protected override void Awake() {
            base.Awake();
        }

        protected override void Start() {
            base.Start();
        }

        protected override void LateUpdate() {
            base.LateUpdate();
        }

        public virtual void MoveToPosition(Vector3 position, bool instant = false) {
            Vector3 lookDirection = _pivotPosition - position;
            float yaw = Utils.SignedAngle(Vector3.forward, lookDirection, Vector3.up);
            float pitch = Vector3.Angle(Vector3.up, lookDirection) - 90.0f;
            float distance = lookDirection.magnitude;
            SetPositionParameters(new Vector3(yaw, pitch, distance), instant);
        }

        public virtual Vector3 GetPositionParameters() {
            return new Vector3(_yaw, _pitch, _distance);
        }

        public virtual void SetPositionParameters(Vector3 parameters, bool instant = false) {
            SetYaw(parameters.x, instant);
            SetPitch(parameters.y, instant);
            SetDistance(parameters.z, instant);
        }
    }
}
