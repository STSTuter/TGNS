using UnityEngine;

namespace JohnStairs.RPG.Character.Motor {
    public struct FacingDirection {
        public Vector3 Forward;
        public Vector3 Up;

        public FacingDirection(Vector3 forward, Vector3 up) {
            Forward = forward;
            Up = up;
        }
    }
}
