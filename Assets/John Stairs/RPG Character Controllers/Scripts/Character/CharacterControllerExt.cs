using UnityEngine;

namespace JohnStairs.RPG.Character {
    public static class CharacterControllerExt {
        /// <summary>
        /// Gets the radius of the character controller collider in world scale. Uses transform.lossyScale
        /// </summary>
        /// <param name="characterController">Character controller component</param>
        /// <returns>Global collider radius</returns>
        public static float GetActualRadius(this CharacterController characterController) {
            return characterController.radius * Mathf.Max(characterController.transform.lossyScale.x, characterController.transform.lossyScale.z);
        }

        /// <summary>
        /// Gets the height of the character controller collider in world scale. Uses transform.lossyScale
        /// </summary>
        /// <param name="characterController">Character controller component</param>
        /// <returns>Global collider height</returns>
        public static float GetActualHeight(this CharacterController characterController) {
            return characterController.height * characterController.transform.lossyScale.y;
        }

        /// <summary>
        /// Gets the center of the character controller collider in world coordinates
        /// </summary>
        /// <param name="characterController">Character controller component</param>
        /// <returns>Collider center in world coordinates</returns>
        public static Vector3 GetActualCenter(this CharacterController characterController) {
            return characterController.transform.TransformPoint(characterController.center);
        }

        /// <summary>
        /// Gets the center of the bottom sphere of the character controller collider in world coordinates
        /// </summary>
        /// <param name="characterController">Character controller component</param>
        /// <returns>Bottom sphere center in world coordinates</returns>
        public static Vector3 GetBottomSphereCenter(this CharacterController characterController) {
            return characterController.GetActualCenter() + Vector3.down * (characterController.GetActualHeight() * 0.5f - characterController.GetActualRadius());
        }
    }
}
