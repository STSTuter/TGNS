using System.Collections;
using System.Collections.Generic;
using JohnStairs.RPG.Character.Cam.Subcomponents.Enums;
using UnityEngine;

namespace JohnStairs.RPG.Character.Cam.Subcomponents {
    public class OcclusionHandler : OcclusionHandlerLite, IOcclusionHandler {
        /// <summary>
        /// Objects fulfilling at least one of these conditions are faded
        /// </summary>
        [Tooltip("Objects fulfilling at least one of these conditions are faded.")]
        public List<Condition> FadeConditions = new() { new(ConditionType.Layer, "TransparentFX") };
        /// <summary>
        /// The alpha to which objects fade out
        /// </summary>
        [Tooltip("The alpha to which objects fade out.")]
        public float FadeOutAlpha = 0.2f;
        /// <summary>
        /// The fade out duration of processed objects
        /// </summary>
        [Tooltip("The fade out duration of processed objects.")]
        public float FadeOutDuration = 0.2f;
        /// <summary>
        /// The fade in duration of processed objects
        /// </summary>
        [Tooltip("The fade in duration of processed objects.")]
        public float FadeInDuration = 0.2f;
        /// <summary>
        /// Color property references of material shaders whose alpha value is used for fading
        /// </summary>
        [Tooltip("Color property references of material shaders whose alpha value is used for fading.")]
        public string[] ShaderColorProperties = { "_BaseColor", "_Color" }; // _BaseColor (URP), _Color (BRP)

        /// <summary>
        /// Contains the objects to fade from the last frame
        /// </summary>
        protected SortedDictionary<EntityId, GameObject> _previousObjectsToFade = new();
        /// <summary>
        /// Contains all currently active fade out coroutines
        /// </summary>
        protected Dictionary<EntityId, IEnumerator> _fadeOutCoroutines = new();
        /// <summary>
        /// Contains all currently active fade in coroutines
        /// </summary>
        protected Dictionary<EntityId, IEnumerator> _fadeInCoroutines = new();
        /// <summary>
        /// Stores the default alpha values of materials before fading them
        /// </summary>
        protected Dictionary<EntityId, float> _defaultMaterialAlphas = new();

        protected override void Awake() {
            base.Awake();
        }

        protected override void Start() {
            base.Start();
            // Consistency checks
            CheckConditionConsistency(FadeConditions);
        }

        public override void HandleObjectVisibility(List<GameObject> objects) {
            SortedDictionary<EntityId, GameObject> objectsToFade = new();

            foreach (GameObject obj in objects) {
                if (ObjectShouldFade(obj)) {
                    EntityId hitObjectID = obj.GetEntityId();

                    if (!objectsToFade.ContainsKey(hitObjectID)) {
                        objectsToFade.Add(hitObjectID, obj);
                    }
                }
            }

            FadeObjects(objectsToFade);
        }

        protected virtual void FadeObjects(SortedDictionary<EntityId, GameObject> objectsToFade) {
            List<GameObject> fadeOut = new();
            List<GameObject> fadeIn = new();

            InitListsOfObjectsToFade(objectsToFade, ref fadeOut, ref fadeIn);

            StartFadingObjects(fadeOut, Fade.Out);
            StartFadingObjects(fadeIn, Fade.In);

            // Store the _previousObjectsToFade for the next frame occlusion computations
            _previousObjectsToFade = objectsToFade;
        }

        protected virtual void InitListsOfObjectsToFade(SortedDictionary<EntityId, GameObject> objectsToFade, ref List<GameObject> fadeOut, ref List<GameObject> fadeIn) {
            // Compare the objects to fade of the last frame and the objects hit in this frame:
            // - If an object is in _previousObjectsToFade but not in objectsToFade, fade it back in (as it is no longer inside the view frustum)
            // - If an object is not in _previousObjectsToFade but in objectsToFade, fade it out (as it entered the view frustum this frame)
            // - If an object is in both lists, do nothing and continue (as the object was already inside the view frustum and still is)
            SortedDictionary<EntityId, GameObject>.Enumerator i = _previousObjectsToFade.GetEnumerator();
            SortedDictionary<EntityId, GameObject>.Enumerator j = objectsToFade.GetEnumerator();

            bool iFinished = !i.MoveNext();
            bool jFinished = !j.MoveNext();
            bool aListFinished = iFinished || jFinished;

            while (!aListFinished) {
                EntityId iKey = i.Current.Key;
                EntityId jKey = j.Current.Key;

                if (iKey == jKey) {
                    iFinished = !i.MoveNext();
                    jFinished = !j.MoveNext();
                    aListFinished = iFinished || jFinished;
                } else if (iKey < jKey) {
                    if (i.Current.Value != null) {
                        fadeIn.Add(i.Current.Value);
                    }
                    aListFinished = !i.MoveNext();
                    iFinished = true;
                    jFinished = false;
                } else {
                    if (j.Current.Value != null) {
                        fadeOut.Add(j.Current.Value);
                    }
                    aListFinished = !j.MoveNext();
                    iFinished = false;
                    jFinished = true;
                }
            }

            if (iFinished && !jFinished) {
                AddRemainingObjects(j, ref fadeOut);
            } else if (!iFinished && jFinished) {
                AddRemainingObjects(i, ref fadeIn);
            }
        }

        protected virtual void AddRemainingObjects(SortedDictionary<EntityId, GameObject>.Enumerator iterator, ref List<GameObject> targetList) {
            do {
                if (iterator.Current.Value != null) {
                    targetList.Add(iterator.Current.Value);
                }
            } while (iterator.MoveNext());
        }

        protected virtual void StartFadingObjects(List<GameObject> objects, Fade fade) {
            Dictionary<EntityId, IEnumerator> oppositeCoroutines;
            Dictionary<EntityId, IEnumerator> targetCoroutines;
            if (fade == Fade.Out) {
                oppositeCoroutines = _fadeInCoroutines;
                targetCoroutines = _fadeOutCoroutines;
            } else {
                oppositeCoroutines = _fadeOutCoroutines;
                targetCoroutines = _fadeInCoroutines;
            }

            foreach (GameObject obj in objects) {
                EntityId objectId = obj.transform.GetEntityId();
                RemoveFadingCoroutine(objectId, oppositeCoroutines);
                // Create a new coroutine for fading out the object
                IEnumerator coroutine = FadeObjectCoroutine(obj, fade);
                // Add the new fade out coroutine to the list of fade out coroutines
                targetCoroutines.Add(objectId, coroutine);
                // Start the coroutine
                StartCoroutine(coroutine);
            }
        }

        protected virtual void RemoveFadingCoroutine(EntityId objectId, Dictionary<EntityId, IEnumerator> coroutines) {
            // Check if there is a running fade in coroutine for this object
            if (coroutines.TryGetValue(objectId, out IEnumerator runningCoroutine)) {
                // Stop the already running coroutine
                StopCoroutine(runningCoroutine);
                // Remove it from the fade in coroutines
                coroutines.Remove(objectId);
            }
        }

        protected virtual IEnumerator FadeObjectCoroutine(GameObject gameObject, Fade fade) {
            float newAlpha;
            float targetAlpha;
            float duration;
            Dictionary<EntityId, IEnumerator> coroutines;
            if (fade == Fade.Out) {
                newAlpha = 1.0f;
                targetAlpha = FadeOutAlpha;
                duration = FadeOutDuration;
                coroutines = _fadeOutCoroutines;
            } else {
                newAlpha = FadeOutAlpha;
                targetAlpha = 1.0f;
                duration = FadeInDuration;
                coroutines = _fadeInCoroutines;
            }

            List<Material> materialsToFade = Utils.GetMaterialsToFade(gameObject, ShaderColorProperties, ref _defaultMaterialAlphas);

            if (fade == Fade.Out) {
                Utils.SetRenderOrder(materialsToFade, 100);
            }

            float fadingVelocity = 0;
            bool fadingFinished = false;
            while (!fadingFinished) {
                if (gameObject == null) {
                    break;
                }

                if (Utils.IsAlmostEqual(newAlpha, targetAlpha, 0.01f)) {
                    newAlpha = targetAlpha;
                    fadingFinished = true;
                } else {
                    newAlpha = Mathf.SmoothDamp(newAlpha, targetAlpha, ref fadingVelocity, duration);
                }

                FadeMaterials(materialsToFade, newAlpha);

                yield return null;
            }

            if (fade == Fade.In) {
                Utils.SetRenderOrder(materialsToFade, 0);
            }

            coroutines.Remove(gameObject.transform.GetEntityId());
        }

        protected virtual void FadeMaterials(List<Material> materials, float newAlpha) {
            foreach (Material material in materials) {
                if (material == null || !Utils.HasExpectedProperty(material, ShaderColorProperties, out string property)) {
                    continue;
                }

                Color newColor = material.GetColor(property);
                newColor.a = Mathf.Min(newAlpha, _defaultMaterialAlphas[material.GetEntityId()]);
                material.SetColor(property, newColor);
            }
        }

        protected virtual bool ObjectShouldFade(GameObject objectToCheck) {
            return ObjectFulfillsCondition(objectToCheck, FadeConditions);
        }
    }
}
