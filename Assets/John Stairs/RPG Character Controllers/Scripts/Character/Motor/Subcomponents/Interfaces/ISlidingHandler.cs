using UnityEngine;

namespace JohnStairs.RPG.Character.Motor.Subcomponents {
    public interface ISlidingHandler : IMotorSubcomponent {
        /// <summary>
        /// Initialize the sliding handler with the given parameters
        /// </summary>
        /// <param name="walkableLayers">Layers that are walkable</param>
        /// <param name="groundedTolerance">Tolerance for being grounded</param>
        void Init(LayerMask walkableLayers, float groundedTolerance);

        /// <summary>
        /// Checks if the character should slide, and if so outputs the sliding vector
        /// </summary>
        /// <param name="slidingVector">Vector describing the sliding vector</param>
        /// <returns>True if the character should slide during this frame, otherwise false</returns>
        bool CheckSliding(out Vector3 slidingVector);
    }
}
