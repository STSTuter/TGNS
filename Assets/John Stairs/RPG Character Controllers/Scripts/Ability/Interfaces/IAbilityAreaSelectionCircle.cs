using UnityEngine;

namespace JohnStairs.RPG.Combat.Abilities {
    public interface IAbilityAreaSelectionCircle {
        /// <summary>
        /// Returns true if the selection circle is currently visible in the scene
        /// </summary>
        bool IsShown();

        /// <summary>
        /// Move the circle to the specified world position
        /// </summary>
        /// <param name="position">Target world coordinates</param>
        void SetPosition(Vector3 position);

        /// <summary>
        /// Position the circle by casting a vertical ray through the given point
        /// </summary>
        /// <param name="pointOnRay">Point to cast a vertical raycast through</param>
        void SetPositionByRaycast(Vector3 pointOnRay);

        /// <summary>
        /// Get the current position of the circle in world coordinates
        /// </summary>
        Vector3 GetPosition();

        /// <summary>
        /// Show the circle for the given caster and ability
        /// </summary>
        void Show(Transform caster, IAbility ability);

        /// <summary>
        /// Hide the circle so it is no longer rendered
        /// </summary>
        void Hide();
    }
}
