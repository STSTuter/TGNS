using UnityEngine;

namespace JohnStairs.RPG.Character.Cam.Subcomponents {
    public interface IPivot : ICameraSubcomponent {
        /// <summary>
        /// Initializes the pivot's internal variables
        /// </summary>
        /// <param name="yaw">Degrees the pivot should be rotated horizontally</param>
        void Init(float yaw);

        /// <summary>
        /// Computes the pivot position based on the given anchor, yaw and viewport
        /// </summary>
        /// <param name="anchor">Anchor position in world coordinates</param>
        /// <param name="yaw">Degrees the pivot needs to be rotated horizontally</param>
        /// <param name="viewport">Viewport of the camera</param>
        /// <param name="obstacleLayers">Layer mask of objects that make the pivot evade</param>
        /// <returns>Pivot position in world coordinates</returns>
        Vector3 GetPosition(Vector3 anchor, float yaw, Vector3 viewport, LayerMask obstacleLayers);

        /// <summary>
        /// Sets the pivot position in local space coordinates
        /// </summary>
        /// <param name="localPosition">New local pivot position</param>
        void SetLocalPosition(Vector3 localPosition);
    }
}
