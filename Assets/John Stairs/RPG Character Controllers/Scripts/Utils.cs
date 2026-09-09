using System;
using System.Collections.Generic;
using UnityEngine;

namespace JohnStairs.RPG {
    public static class Utils {
        /// <summary>
        /// Converts a bool to a sign
        /// </summary>
        /// <param name="value">Bool to convert</param>
        /// <returns>1 if value is "true", otherwise -1</returns>
        public static int BoolToSign(bool value) {
            return value ? 1 : -1;
        }

        /// <summary>
        /// Linearly transitions from one to another value
        /// </summary>
        /// <param name="from">Start value</param>
        /// <param name="to">Target value</param>
        /// <param name="delta">Delta to add per second</param>
        /// <returns>Value between "from" and "to"</returns>
        public static float LinearTransition(float from, float to, float delta) {
            if (from < to) {
                return Mathf.Min(from + Time.deltaTime * delta, to);
            } else {
                return Mathf.Max(from - Time.deltaTime * delta, to);
            }
        }

        /// <summary>
        /// Returns the signed angle between vector a and b on the plane with normal normal. The result range is in (-180, 180]
        /// </summary>
        /// <param name="a">First vector</param>
        /// <param name="b">Second vector</param>
        /// <param name="normal">Plane normal for projecting vector a and b</param>
        /// <returns>Signed angle on plane with normal normal</returns>
        public static float SignedAngle(Vector3 a, Vector3 b, Vector3 normal) {
            // Project a and b onto the plane with normal normal
            a = Vector3.ProjectOnPlane(a, normal);
            b = Vector3.ProjectOnPlane(b, normal);
            // Calculate the signed angle between them
            return Vector3.SignedAngle(a, b, normal);
        }

        /// <summary>
        /// Checks if the given layer is part of the given layer mask
        /// </summary>
        /// <param name="layer">Layer to check</param>
        /// <param name="layerMask">Layer mask to look in for layer</param>
        /// <returns>True if layer is in layerMask, otherwise false</returns>
        public static bool LayerInLayerMask(int layer, LayerMask layerMask) {
            return (layerMask.value & (1 << layer)) > 0;
        }

        /// <summary>
        /// Checks if two floats are considered equal according to the given epsilon. Returns true if the distance between a and b is smaller than epsilon
        /// </summary>
        /// <param name="a">Left-side float</param>
        /// <param name="b">Right-side float</param>
        /// <param name="epsilon">Minimum value for inequality</param>
        /// <returns>True if the distance between a and b is smaller than epsilon</returns>
        public static bool IsAlmostEqual(float a, float b, float epsilon) {
            return Mathf.Abs(a - b) < epsilon;
        }

        /// <summary>
        /// Enables the ZWrite property of the given material
        /// </summary>
        /// <param name="material">Material whose shader ZWrite property should change</param>
        public static void EnableZWrite(Material material) {
            if (material.HasProperty("_ZWrite")) {
                material.SetFloat("_ZWrite", 1.0f);
            }
        }

        /// <summary>
        /// Sets the rendering order (position) of the given materials
        /// </summary>
        /// <param name="materials">Materials to change the rendering order</param>
        /// <param name="position">Value added to the "Transparent" render queue value</param>
        public static void SetRenderOrder(List<Material> materials, int position) {
            foreach (Material material in materials) {
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + position;
            }
        }

        /// <summary>
        /// Checks if the given material has one of the given expected properties
        /// </summary>
        /// <param name="material">Material to check for properties</param>
        /// <param name="exptectedProperties">Properties that are tested</param>
        /// <param name="property">Name of the first matching property</param>
        /// <returns>True if the material has one of the expected properties, otherwise false</returns>
        public static bool HasExpectedProperty(Material material, string[] exptectedProperties, out string property) {
            for (int i = 0; i < exptectedProperties.Length; i++) {
                if (material.HasProperty(exptectedProperties[i])) {
                    property = exptectedProperties[i];
                    return true;
                }
            }
            property = null;
            return false;
        }

        /// <summary>
        /// Gets all materials from the given game object that qualify for fading
        /// </summary>
        /// <param name="gameObject">Object to get the materials from</param>
        /// <param name="filterProperties">Material properties to filter for</param>
        /// <param name="alphaValueCache">Cache for storing the default alpha values of the materials</param>
        /// <returns>List of fadeable materials</returns>
        public static List<Material> GetMaterialsToFade(GameObject gameObject, string[] filterProperties, ref Dictionary<EntityId, float> alphaValueCache) {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            List<Material> materialsToFade = new();
            foreach (Renderer renderer in renderers) {
                foreach (Material material in renderer.materials) {
                    if (HasExpectedProperty(material, filterProperties, out string property)) {
                        materialsToFade.Add(material);
                        EnableZWrite(material);
                        if (!alphaValueCache.ContainsKey(material.GetEntityId())) {
                            alphaValueCache.Add(material.GetEntityId(), material.GetColor(property).a);
                        }
                    }
                }
            }
            return materialsToFade;
        }

        /// <summary>
        /// Comparator for comparing two RaycastHits by their distance
        /// </summary>
        /// <param name="a">Left-side RaycastHit</param>
        /// <param name="b">Right-side RaycastHit</param>
        /// <returns>A signed number indicating the relative values of a and b</returns>
        public static int RaycastHitComparator(RaycastHit a, RaycastHit b) {
            return a.distance.CompareTo(b.distance);
        }

        /// <summary>
        /// Standard physics raycast that only considers objects with the given tag
        /// </summary>
        /// <param name="origin">The starting point of the ray in world coordinates</param>
        /// <param name="direction">The direction of the ray</param>
        /// <param name="hitInfo">If true is returned, hitInfo will contain more information about where the closest collider was hit</param>
        /// <param name="maxDistance">The max distance the ray should check for collisions</param>
        /// <param name="layerMask">A Layer mask that is used to selectively ignore colliders when casting a ray</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit Triggers</param>
        /// <param name="wantedTag">Tag that hit objects should have</param>
        /// <returns>Returns true when the ray intersects any collider, otherwise false</returns>
        public static bool TagBasedRaycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, string wantedTag) {
            RaycastHit[] raycastHits = Physics.RaycastAll(origin, direction, maxDistance, layerMask, queryTriggerInteraction);
            Array.Sort(raycastHits, RaycastHitComparator);
            for (int i = 0; i < raycastHits.Length; i++) {
                if (raycastHits[i].transform.CompareTag(wantedTag)) {
                    hitInfo = raycastHits[i];
                    return true;
                }
            }
            hitInfo = default;
            return false;
        }
    }
}
