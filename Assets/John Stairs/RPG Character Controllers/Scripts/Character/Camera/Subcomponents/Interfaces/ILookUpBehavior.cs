using UnityEngine;

namespace JohnStairs.RPG.Character.Cam.Subcomponents {
    public interface ILookUpBehavior : ICameraSubcomponent {
        /// <summary>
        /// Constrains the given pitch during camera look up
        /// </summary>
        /// <param name="pitch">Pitch to constrain</param>
        /// <param name="pivotPosition">Position of the camera pivot</param>
        /// <param name="cameraPosition">Position of the camera</param>
        /// <param name="viewport">Camera viewport</param>
        void ConstrainPitch(ref float pitch, Vector3 pivotPosition, Vector3 cameraPosition, Vector3 viewport);

        /// <summary>
        /// Gets the degrees to look up
        /// </summary>
        /// <returns>Positive look up degrees</returns>
        float GetLookUpDegrees();
    }
}
