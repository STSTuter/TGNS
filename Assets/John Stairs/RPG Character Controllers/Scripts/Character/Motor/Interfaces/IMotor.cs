using JohnStairs.RPG.Character.Motor.Enums;
using UnityEngine;

namespace JohnStairs.RPG.Character.Motor {
    public interface IMotor {
        /// <summary>
        /// Move the character according to the supplied vector
        /// </summary>
        /// <param name="movementVector">World‑space movement delta</param>
        void Move(Vector3 movementVector);

        /// <summary>
        /// Lets the character rotate horizontally
        /// </summary>
        /// <param name="degrees">Degrees to rotate</param>
        /// <returns>Rotated angle in degrees</returns>
        float Turn(float degrees);

        /// <summary>
        /// Rotate the character to face the given forward direction
        /// </summary>
        /// <param name="forward">Desired forward vector</param>
        /// <param name="up">Up vector to maintain orientation</param>
        void TurnInDirection(Vector3 forward, Vector3 up);

        /// <summary>
        /// Lets the character jump if possible
        /// </summary>
        void Jump();

        /// <summary>
        /// Enable or disable sprinting
        /// </summary>
        /// <param name="on">True to sprint, false to stop</param>
        void Sprint(bool on);

        /// <summary>
        /// Toggle between walking and running states
        /// </summary>
        void ToggleWalking();

        /// <summary>
        /// Toggle between crouching and standing states
        /// </summary>
        void ToggleCrouching();

        /// <summary>
        /// Toggle the ability to fly on or off
        /// </summary>
        void ToggleFlyingAbility();

        /// <summary>
        /// Returns true if the character is moving during this frame
        /// </summary>
        bool IsInMotion();

        /// <summary>
        /// Returns the currently active type of character movement, e.g. walking, swimming, flying, climbing, etc.
        /// </summary>
        MovementType GetMovementType();

        /// <summary>
        /// Set movement speed multiplier to the given factor
        /// </summary>
        /// <param name="multiplier">Speed multiplier</param>
        void SetMovementSpeedMultiplier(float multiplier);

        /// <summary>
        /// Reset movement speed multiplier to default
        /// </summary>
        void ResetMovementSpeedMultiplier();

        /// <summary>
        /// Teleport the character to the given position and rotation
        /// </summary>
        /// <param name="position">Target position</param>
        /// <param name="rotation">Target rotation</param>
        void Teleport(Vector3 position, Quaternion rotation);

        /// <summary>
        /// Reset the motor to its default state, clearing transient movement and temporary flags so the character returns to a neutral state.
        /// </summary>
        void Reset();

        bool IsEnabled();

        /// <summary>
        /// Destroys this component and all of its subcomponents
        /// </summary>
        void Destroy();
    }
}
