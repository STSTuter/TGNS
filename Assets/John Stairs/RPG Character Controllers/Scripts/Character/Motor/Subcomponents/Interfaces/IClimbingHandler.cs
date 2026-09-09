using JohnStairs.RPG.Character.Motor.Enums;
using UnityEngine;

namespace JohnStairs.RPG.Character.Motor.Subcomponents {
    public interface IClimbingHandler : IMotorSubcomponent {
        /// <summary>
        /// Gets the current climbing state
        /// </summary>
        ClimbingState GetClimbingState();

        /// <summary>
        /// Calculates movement while climbing
        /// </summary>
        /// <param name="inputMovementVector">The movement input vector in world coordinates</param>
        /// <param name="grounded">Whether the character is currently grounded</param>
        Vector3 GetMovementVector(Vector3 inputMovementVector, bool grounded);

        /// <summary>
        /// Gets the facing direction used while climbing
        /// </summary>
        FacingDirection GetFacingDirection();

        /// <summary>
        /// Stops climbing and resets climbing state
        /// </summary>
        void StopClimbing();
    }
}
