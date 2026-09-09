using UnityEngine;

namespace JohnStairs.RPG.Character.Motor.Subcomponents {
    public interface ISwimmingHandler : IMotorSubcomponent {
        /// <summary>
        /// Checks if the character could start swimming, i.e. is deep enough in water
        /// </summary>
        /// <param name="colliderPosition">Position of the character collider</param>
        /// <returns>True if the character could start swimming, otherwise false</returns>
        bool CheckSwimming(Vector3 colliderPosition);

        /// <summary>
        /// Checks if the character is near the water surface
        /// </summary>
        /// <returns>True if the character is near the water surface, otherwise false</returns>
        bool IsNearWaterSurface();

        /// <summary>
        /// Gets the swimming speed
        /// </summary>
        /// <returns>Swim speed of the character</returns>
        float GetSwimSpeed();

        /// <summary>
        /// Resets the swimming handler to its initial values
        /// </summary>
        void Reset();
    }
}
