using System.Collections.Generic;
using JohnStairs.RPG.Character.Cam.Subcomponents.Enums;
using UnityEngine;

namespace JohnStairs.RPG.Character.Cam.Subcomponents {
    public class ViewFrustum : MonoBehaviour, IViewFrustum {
        /// <summary>
        /// Controls which shape the view frustum has
        /// </summary>
        [Tooltip("Controls which shape the view frustum has.")]
        public FrustumShape Shape = FrustumShape.Cuboid;
        /// <summary>
        /// Number of evenly distributed rays per near frustum plane edge, exclusive the plane corners (pyramid shape only)
        /// </summary>
        [Tooltip("Number of evenly distributed rays per near frustum plane edge, exclusive the plane corners.")]
        public int RaysPerEdge = 3;
        /// <summary>
        /// Only objects in these layers are detected by the view frustum
        /// </summary>
        [Tooltip("Only objects in these layers are detected by the view frustum.")]
        public LayerMask CheckedLayers = 195; // Default + TransparentFX + Ground + Climbable
        /// <summary>
        /// Game objects with one of these tags are ignored by the view frustum
        /// </summary>
        [Tooltip("Game objects with one of these tags are ignored by the view frustum.")]
        public List<string> IgnoredTags = new() { "Player" };

        protected class ViewportCornerVectors {
            public Vector3 ShiftTopLeft;
            public Vector3 ShiftTopRight;
            public Vector3 ShiftBottomLeft;
            public Vector3 ShiftBottomRight;
        }

        protected virtual void Awake() {
        }

        protected virtual void Start() {
        }

        public virtual RaycastHit[] GetObjectHitsInFrustum(Vector3 from, Vector3 to, Vector3 viewport) {
            if (Vector3.Distance(from, to) <= viewport.z) {
                return new RaycastHit[0];
            }

            List<RaycastHit> objectHits;
            if (Shape == FrustumShape.Pyramid) {
                objectHits = CheckForPyramidFrustumOcclusion(from, to, viewport, CheckedLayers);
            } else {
                objectHits = CheckForCuboidFrustumOcclusion(from, to, viewport, CheckedLayers);
            }

            return FilterOutIgnoredTags(objectHits).ToArray();
        }

        protected virtual List<RaycastHit> CheckForPyramidFrustumOcclusion(Vector3 from, Vector3 to, Vector3 viewport, LayerMask layerMask) {
            int maxIndex = RaysPerEdge + 1;
            Vector3[,] rayOrigins = GetPyramidRayOrigins(from, to, viewport, maxIndex);

            Vector3 direction = (to - from).normalized;
            List<RaycastHit> objectHits = new();
            RaycastHit[] hitArray;
            Vector3 rayDirection;
            // Loop over all rays in the matrix and cast them
            for (int i = 0; i <= maxIndex; i++) {
                for (int j = 0; j <= maxIndex; j++) {
                    rayDirection = rayOrigins[i, j] - from;
                    hitArray = Physics.RaycastAll(from, rayDirection, rayDirection.magnitude, layerMask, QueryTriggerInteraction.Ignore);

                    for (int k = 0; k < hitArray.Length; k++) {
                        // Project the distance from the frustum edge onto the camera direction
                        hitArray[k].distance = Vector3.Project(rayDirection.normalized * hitArray[k].distance, direction).magnitude;
                        objectHits.Add(hitArray[k]);
                    }
                }
            }
            return objectHits;
        }

        /// <summary>
        /// Gets a matrix storing the ray origins to be cast from the near frustum plane (pyramid shape only)
        /// </summary>
        /// <param name="from">Frustum start</param>
        /// <param name="to">Frustum end</param>
        /// <param name="viewport">Camera viewport</param>
        /// <param name="maxIndex">Maximum index of the resulting array in both dimensions</param>
        /// <returns>Matrix with ray origins on the near frustum plane</returns>
        protected virtual Vector3[,] GetPyramidRayOrigins(Vector3 from, Vector3 to, Vector3 viewport, int maxIndex) {
            ViewportCornerVectors cornerVectors = GetViewportCornerVectors(from, to, viewport);
            Vector3[,] origins = new Vector3[maxIndex + 1, maxIndex + 1];
            // Corners
            origins[0, 0] = to + cornerVectors.ShiftTopLeft;
            origins[0, maxIndex] = to + cornerVectors.ShiftTopRight;
            origins[maxIndex, 0] = to + cornerVectors.ShiftBottomLeft;
            origins[maxIndex, maxIndex] = to + cornerVectors.ShiftBottomRight;

            Vector3 down = cornerVectors.ShiftBottomLeft - cornerVectors.ShiftTopLeft;
            Vector3 right = cornerVectors.ShiftTopRight - cornerVectors.ShiftTopLeft;
            // Borders
            for (int i = 1; i < maxIndex; i++) {
                origins[i, 0] = origins[0, 0] + down * (i / (float)maxIndex);
                origins[i, maxIndex] = origins[i, 0] + right;
            }
            // Inside
            for (int i = 0; i <= maxIndex; i++) {
                for (int j = 1; j < maxIndex; j++) {
                    origins[i, j] = origins[i, 0] + right * (j / (float)maxIndex);
                }
            }
            return origins;
        }

        /// <summary>
        /// Gets the vectors from a (camera) position, to each viewport/frustum plane corner
        /// </summary>
        /// <param name="from">Frustum start</param>
        /// <param name="to">Frustum end</param>
        /// <param name="viewport">Camera viewport</param>
        /// <returns>Vectors pointing from a point to a viewport corner each</returns>
        protected virtual ViewportCornerVectors GetViewportCornerVectors(Vector3 from, Vector3 to, Vector3 viewport) {
            Vector3 targetDirection = (from - to).normalized;

            Vector3 up = Vector3.up;
            Vector3.OrthoNormalize(ref targetDirection, ref up);
            Vector3 right = Vector3.Cross(up, targetDirection);

            Vector3 offset = targetDirection * viewport.z;

            return new ViewportCornerVectors {
                ShiftTopLeft = -right * viewport.x + up * viewport.y + offset,
                ShiftTopRight = right * viewport.x + up * viewport.y + offset,
                ShiftBottomLeft = -right * viewport.x - up * viewport.y + offset,
                ShiftBottomRight = right * viewport.x - up * viewport.y + offset
            };
        }

        protected virtual List<RaycastHit> CheckForCuboidFrustumOcclusion(Vector3 from, Vector3 to, Vector3 viewport, LayerMask layerMask) {
            // Cast the box which acts as the cuboid view frustum
            RaycastHit[] hitArray = BoxCastAll(from, to, viewport, layerMask);
            List<RaycastHit> validHits = new();
            RaycastHit hit;
            for (int i = 0; i < hitArray.Length; i++) {
                hit = hitArray[i];

                if (hit.point != Vector3.zero) {
                    validHits.Add(hit);
                } else {
                    // Most likely due to the note described on https://docs.unity3d.com/ScriptReference/Physics.BoxCastAll.html
                    //Debug.LogWarning("There is a collider overlapping the box at the start of the sweep!", gameObject);
                    // Skip this case
                    continue;
                }
            }
            return validHits;
        }

        /// <summary>
        /// Casts a box such that all collisions between from and to are detected 
        /// </summary>
        /// <param name="from">Beginning of the detecting box</param>
        /// <param name="to">End of the detecting box</param>
        /// <returns>All ray cast hits between from and to</returns>
        protected virtual RaycastHit[] BoxCastAll(Vector3 from, Vector3 to, Vector3 viewport, LayerMask layerMask) {
            Vector3 direction = to - from;
            float maxDistance = direction.magnitude - viewport.z;
            direction.Normalize();
            // Set the box dimensions
            Vector3 boxHalfExtents = new(viewport.x, viewport.y, viewport.z * 0.5f);
            // Set the box center before the beginning of the view frustum to bypass colliders that overlap the box at the start of the sweep
            Vector3 boxCenter = from - direction * boxHalfExtents.z;
            Quaternion boxOrientation = Quaternion.LookRotation(direction);
            // Draw debug ray for the box cast, i.e. from the first box center to the last box center (end box position)
            //Debug.DrawRay(boxCenter, direction * maxDistance, Color.magenta);
            // Cast the box
            return Physics.BoxCastAll(boxCenter, boxHalfExtents, direction, boxOrientation, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
        }

        protected virtual List<RaycastHit> FilterOutIgnoredTags(List<RaycastHit> objectHits) {
            List<RaycastHit> filteredObjectHits = new();
            foreach (RaycastHit objectHit in objectHits) {
                if (!IgnoredTags.Contains(objectHit.collider.tag)) {
                    filteredObjectHits.Add(objectHit);
                }
            }
            return filteredObjectHits;
        }

        public virtual void DrawFrustum(Vector3 from, Vector3 to, Vector3 viewport, Vector3 cameraPosition) {
            if (from == to) {
                return;
            }

            ViewportCornerVectors cornerVectors = GetViewportCornerVectors(from, to, viewport);
            Color frustumPlaneColor = Color.gray;
            Color cameraPlaneColor = Color.yellow;
            Color frustumEdgeColor = Color.white;

            DrawFrustumPlane(to, cornerVectors, frustumPlaneColor);

            if (Shape == FrustumShape.Pyramid) {
                DrawPyramidFrustumEdges(to, from, cornerVectors, frustumEdgeColor);
            } else {
                // Draw second plane
                Vector3 offset = (to - from).normalized * viewport.z;
                DrawFrustumPlane(from + offset, cornerVectors, frustumPlaneColor);
                DrawCuboidFrustumEdges(from + offset, to, cornerVectors, frustumEdgeColor);
            }

            if (cameraPosition != Vector3.zero) {
                DrawFrustumPlane(cameraPosition, cornerVectors, cameraPlaneColor);
            }
        }

        protected virtual void DrawFrustumPlane(Vector3 position, ViewportCornerVectors cornerVectors, Color color) {
            // Calculate the near frustum plane at the end position (e.g. desired camera position)
            Vector3 upperLeft = position + cornerVectors.ShiftTopLeft;
            Vector3 upperRight = position + cornerVectors.ShiftTopRight;
            Vector3 lowerLeft = position + cornerVectors.ShiftBottomLeft;
            Vector3 lowerRight = position + cornerVectors.ShiftBottomRight;
            // Draw the frustum plane at the end
            Debug.DrawLine(upperLeft, upperRight, color);
            Debug.DrawLine(upperLeft, lowerLeft, color);
            Debug.DrawLine(upperRight, lowerRight, color);
            Debug.DrawLine(lowerLeft, lowerRight, color);
        }

        protected virtual void DrawPyramidFrustumEdges(Vector3 baseCenter, Vector3 apex, ViewportCornerVectors cornerVectors, Color color) {
            Vector3 upperLeft = baseCenter + cornerVectors.ShiftTopLeft;
            Vector3 upperRight = baseCenter + cornerVectors.ShiftTopRight;
            Vector3 lowerLeft = baseCenter + cornerVectors.ShiftBottomLeft;
            Vector3 lowerRight = baseCenter + cornerVectors.ShiftBottomRight;

            Debug.DrawLine(upperLeft, apex, color);
            Debug.DrawLine(upperRight, apex, color);
            Debug.DrawLine(lowerLeft, apex, color);
            Debug.DrawLine(lowerRight, apex, color);
        }

        protected virtual void DrawCuboidFrustumEdges(Vector3 from, Vector3 to, ViewportCornerVectors cornerVectors, Color color) {
            Vector3 upperLeft = from + cornerVectors.ShiftTopLeft;
            Vector3 upperRight = from + cornerVectors.ShiftTopRight;
            Vector3 lowerLeft = from + cornerVectors.ShiftBottomLeft;
            Vector3 lowerRight = from + cornerVectors.ShiftBottomRight;

            Vector3 direction = to - from;
            Debug.DrawRay(upperLeft, direction, color);
            Debug.DrawRay(upperRight, direction, color);
            Debug.DrawRay(lowerLeft, direction, color);
            Debug.DrawRay(lowerRight, direction, color);
        }
    }
}
