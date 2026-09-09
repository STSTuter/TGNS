using System;
using System.Collections.Generic;
using JohnStairs.RPG.Character.Cam.Subcomponents.Enums;
using UnityEngine;

namespace JohnStairs.RPG.Character.Cam.Subcomponents {
    public class OcclusionHandlerLite : MonoBehaviour, IOcclusionHandler {
        /// <summary>
        /// Objects fulfilling at least one of these conditions cause the camera to zoom in
        /// </summary>
        [Tooltip("Objects fulfilling at least one of these conditions cause the camera to zoom in.")]
        public List<Condition> ZoomConditions = new() { new(ConditionType.Component, "JohnStairs.RPG.Character.Cam.Subcomponents.ZoomInCondition"), new(ConditionType.Layer, "Default"), new(ConditionType.Layer, "Ground"), new(ConditionType.Layer, "Climbable") };

        [Serializable]
        public struct Condition {
            [SerializeField]
            public ConditionType Type;
            [SerializeField]
            public string Name;

            public Condition(ConditionType type, string name) {
                Type = type;
                Name = name;
            }
        }

        protected enum Fade {
            Out,
            In
        }

        protected virtual void Awake() {
        }

        protected virtual void Start() {
            // Consistency checks
            CheckConditionConsistency(ZoomConditions);
        }

        protected virtual void CheckConditionConsistency(List<Condition> conditions) {
            ViewFrustum viewFrustum = GetComponent<ViewFrustum>();
            if (viewFrustum != null) {
                foreach (Condition condition in conditions) {
                    if (condition.Type == ConditionType.Layer) {
                        int layer = LayerMask.NameToLayer(condition.Name);
                        if (!Utils.LayerInLayerMask(layer, viewFrustum.CheckedLayers)) {
                            Debug.LogWarning("A layer was set up as condition for occlusion handling which is not part of the \"Checked Layers\" layer mask of the View Frustum! As a result, the condition can never be fulfilled", viewFrustum);
                        }
                    } else if (condition.Type == ConditionType.Tag) {
                        if (viewFrustum.IgnoredTags.Contains(condition.Name)) {
                            Debug.LogWarning("A tag was set up as condition for occlusion handling which is ignored by the View Frustum! As a result, the condition can never be fulfilled", viewFrustum);
                        }
                    }
                }
            }
        }

        public virtual float GetClosestHitDistance(RaycastHit[] objectHits, out List<GameObject> objectsInBetween) {
            objectsInBetween = new List<GameObject>();
            Array.Sort(objectHits, Utils.RaycastHitComparator);

            for (int i = 0; i < objectHits.Length; i++) {
                RaycastHit hit = objectHits[i];
                if (ObjectCausesZoomIn(hit.transform.gameObject)) {
                    Debug.DrawRay(hit.point, hit.normal, Color.red);
                    return hit.distance;
                } else {
                    objectsInBetween.Add(hit.transform.gameObject);
                }
            }
            return Mathf.Infinity;
        }

        public virtual void HandleObjectVisibility(List<GameObject> objects) {
            // Not implemented with Lite
        }

        protected virtual bool ObjectCausesZoomIn(GameObject objectToCheck) {
            return ObjectFulfillsCondition(objectToCheck, ZoomConditions);
        }

        protected virtual bool ObjectFulfillsCondition(GameObject objectToCheck, List<Condition> conditions) {
            foreach (Condition condition in conditions) {
                switch (condition.Type) {
                    case ConditionType.Layer:
                        if (objectToCheck.layer == LayerMask.NameToLayer(condition.Name)) {
                            return true;
                        }
                        break;
                    case ConditionType.Tag:
                        if (objectToCheck.CompareTag(condition.Name)) {
                            return true;
                        }
                        break;
                    case ConditionType.Component:
                        try {
                            if (objectToCheck.GetComponent(Type.GetType(condition.Name))) {
                                return true;
                            }
                        } catch (ArgumentException) {
                            Debug.LogWarning("Component " + condition.Name + " is not defined in this project!", gameObject);
                        }
                        break;
                    default: break;
                }
            }
            return false;
        }

        public virtual LayerMask GetZoomConditionsLayerMask() {
            int mask = 0;
            foreach (Condition condition in ZoomConditions) {
                if (condition.Type == ConditionType.Layer) {
                    int layer = LayerMask.NameToLayer(condition.Name);
                    if (layer >= 0 && layer <= 31) {
                        mask |= 1 << layer;
                    } else {
                        Debug.LogWarning("Layer '" + condition.Name + "' used in zoom conditions is not a valid layer name.", this);
                    }
                }
            }
            return mask;
        }
    }
}
