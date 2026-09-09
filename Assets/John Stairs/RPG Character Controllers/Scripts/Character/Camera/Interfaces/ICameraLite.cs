using UnityEngine;

namespace JohnStairs.RPG.Character.Cam {
    public interface ICameraLite {
        /// <summary>
        /// Yaws the camera by the given angle in degrees
        /// </summary>
        /// <param name="degrees">Yaw angle in degrees</param>
        /// <param name="instant">If true, the change will not be smoothed and instead applied immediately</param>
        void Yaw(float degrees, bool instant = false);

        /// <summary>
        /// Pitches the camera by the given angle in degrees
        /// </summary>
        /// <param name="degrees">Pitch angle in degrees</param>
        /// <param name="instant">If true, the change will not be smoothed and instead applied immediately</param>
        void Pitch(float degrees, bool instant = false);

        /// <summary>
        /// Zooms the camera by the given units in world coordinates
        /// </summary>
        /// <param name="units">Units that are added to the current distance, negative values cause a zoom in</param>
        /// <param name="instant">If true, the change will not be smoothed and instead applied immediately</param>
        void Zoom(float units, bool instant = false);

        /// <summary>
        /// Lets the camera smoothly zoom to its minimum distance
        /// </summary>
        void ZoomToMinDistance();

        /// <summary>
        /// Lets the camera smoothly zoom to its maximum distance
        /// </summary>
        void ZoomToMaxDistance();

        /// <summary>
        /// Gets the camera component controlled by this script
        /// </summary>
        /// <returns>Used camera component</returns>
        Camera GetUsedCamera();

        /// <summary>
        /// Aligns the camera horizontally with the given transform (Y axis rotation)
        /// </summary>
        /// <param name="transform">Transform to align the camera with</param>
        /// <param name="opposed">If true, the camera will face the front of the transform. Otherwise, it will face the same direction</param>
        void AlignWithTransform(Transform transform, bool opposed);

        void SetRotationSmoothTime(float rotationSmoothTime);

        void ResetRotationSmoothTime();

        void PreventLookUpBehavior(bool prevent);

        /// <summary>
        /// Resets the camera view to its start values (yaw, pitch, distance)
        /// </summary>
        /// <param name="instant">If true, the transition to the default variable values will not be smoothed over time. Therefore, the camera is teleported immediately</param>        
        void ResetView(bool instant = true);

        bool IsOrbitingLocked();

        /// <summary>
        /// Destroys this component and all of its subcomponents
        /// </summary>
        void Destroy();
    }
}
