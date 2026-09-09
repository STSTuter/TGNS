using UnityEngine;

namespace JohnStairs.RPG.Character.Cam {
    public interface ICamera : ICameraLite {
        /// <summary>
        /// Moves the camera to the given world-space position
        /// </summary>
        /// <param name="position">Target world-space position for the camera</param>
        /// <param name="instant">If true, teleport the camera instead of doing a smooth transition</param>
        void MoveToPosition(Vector3 position, bool instant = false);

        /// <summary>
        /// Gets the current position-related camera parameters. Complement to method "SetPositionParameters"
        /// </summary>
        /// <returns>A Vector3 containing the camera position parameters used by the implementation</returns>
        Vector3 GetPositionParameters();

        /// <summary>
        /// Sets the camera position-related parameters. Complement to method "GetPositionParameters"
        /// </summary>
        /// <param name="parameters">Position parameters to apply to the camera implementation</param>
        /// <param name="instant">If true, teleport the camera instead of doing a smooth transition</param>
        void SetPositionParameters(Vector3 parameters, bool instant = false);
    }
}
