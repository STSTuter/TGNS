using UnityEngine;

namespace JohnStairs.RPG.Character.Cam.Subcomponents {
    public interface IViewFrustum : ICameraSubcomponent {
        /// <summary>
        /// Gets all object hits inside the frustum
        /// </summary>
        /// <param name="from">Frustum start</param>
        /// <param name="to">Frustum end</param>
        /// <param name="viewport">Camera viewport</param>
        /// <returns>Hits of objects inside the frustum</returns>
        RaycastHit[] GetObjectHitsInFrustum(Vector3 from, Vector3 to, Vector3 viewport);

        /// <summary>
        /// Draws the frustum
        /// </summary>
        /// <param name="from">Frustum start</param>
        /// <param name="to">Frustum end</param>
        /// <param name="viewport">Camera viewport to use</param>
        /// <param name="cameraPosition">Additional camera position</param>
        void DrawFrustum(Vector3 from, Vector3 to, Vector3 viewport, Vector3 cameraPosition);
    }
}
