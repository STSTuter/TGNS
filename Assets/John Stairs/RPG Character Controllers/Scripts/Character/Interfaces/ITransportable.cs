using UnityEngine;

namespace JohnStairs.RPG.Character {
    public interface ITransportable {
        /// <summary>
        /// Gets the transform of the transportable object
        /// </summary>
        /// <returns>Unity transform component</returns>
        Transform GetTransform();

        /// <summary>
        /// Gets the collider radius
        /// </summary>
        /// <returns>Collider radius in world space</returns>
        float GetColliderRadius();

        /// <summary>
        /// Checks if the object's jumps are influenced by its carrier
        /// </summary>
        /// <returns>True if jumps are influenced</returns>
        bool JumpsAffectedByCarrier();
    }
}
