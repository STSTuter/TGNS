using UnityEngine;

namespace JohnStairs.RPG.Character.Cam.Subcomponents {
    public interface IUnderwaterHandler : ICameraSubcomponent {
        /// <summary>
        /// Checks if the given camera is considered underwater
        /// </summary>
        /// <param name="camera">Camera to check</param>
        /// <param name="viewport">Camera viewport</param>
        /// <returns></returns>
        bool IsUnderwater(Camera camera, Vector3 viewport);

        /// <summary>
        /// Enables underwater camera effects
        /// </summary>
        void EnableEffects();

        /// <summary>
        /// Disables underwater camera effects
        /// </summary>
        void DisableEffects();

        /// <summary>
        /// Returns a pitch that skips the water level if required
        /// </summary>
        /// <param name="pivotPosition">Position of the camera pivot</param>
        /// <param name="cameraDistance">Distance from pivot to camera</param>
        /// <param name="viewport">Camera viewport</param>
        /// <param name="pitch">Camera pitch to check</param>
        /// <returns>Pitch to be used</returns>
        float ApplyWaterLevelSkip(Vector3 pivotPosition, float cameraDistance, Vector3 viewport, float pitch);
    }
}
