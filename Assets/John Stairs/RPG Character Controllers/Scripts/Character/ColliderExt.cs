using UnityEngine;

namespace JohnStairs.RPG.Character {
    public static class ColliderExt {
        /// <summary>
        /// Gets the position of the collider head in world space. The collider head is expected to be a safe retreat for the external camera pivot
        /// </summary>
        /// <returns>Collider head position in world coordinates, transform.position if no collider is assigned</returns>
        public static Vector3 GetRetreatLocation(this Collider collider) {
            Transform transform = collider.transform;
            if (collider is CharacterController characterController) {
                return transform.TransformPoint(characterController.center) + Vector3.up * (characterController.GetActualHeight() * 0.5f - characterController.GetActualRadius());
            } else if (collider is CapsuleCollider capsuleCollider) {
                return transform.TransformPoint(capsuleCollider.center) + Vector3.up * (capsuleCollider.height * 0.5f - capsuleCollider.radius);
            } else if (collider is BoxCollider boxCollider) {
                return transform.TransformPoint(boxCollider.center);
            } else if (collider is SphereCollider sphereCollider) {
                return transform.TransformPoint(sphereCollider.center);
            }
            return transform.position;
        }
    }
}
