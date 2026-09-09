using UnityEngine;

namespace JohnStairs.RPG.Character.Cam.Subcomponents {
    public interface ICharacterFadingHandler : ICameraSubcomponent {
        /// <summary>
        /// Handles the visibility of the character based on the given camera and viewport
        /// </summary>
        /// <param name="camera">Camera object</param>
        /// <param name="viewport">Camera viewport</param>
        void HandleCharacterVisibility(Camera camera, Vector3 viewport);
    }
}
