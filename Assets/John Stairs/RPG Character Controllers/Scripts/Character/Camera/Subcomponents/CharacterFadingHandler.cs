using System.Collections.Generic;
using UnityEngine;

namespace JohnStairs.RPG.Character.Cam.Subcomponents {
    public class CharacterFadingHandler : MonoBehaviour, ICharacterFadingHandler {
        /// <summary>
        /// The alpha value of the character when the "Fading End Distance" has been reached
        /// </summary>
        [Tooltip("The alpha value of the character when the \"Fading End Distance\" has been reached.")]
        public float FadeOutAlpha = 0;
        /// <summary>
        /// The distance between the camera and its pivot where the character starts to fade out
        /// </summary>
        [Tooltip("The distance between the camera and its pivot where the character starts to fade out.")]
        public float FadingStartDistance = 1.0f;
        /// <summary>
        /// The distance between the camera and its pivot where the character is faded out to "Fade Out Alpha"
        /// </summary>
        [Tooltip("The distance between the camera and its pivot where the character is faded out to \"Fade Out Alpha\".")]
        public float FadingEndDistance = 0.1f;
        /// <summary>
        /// Color property references of material shaders whose alpha value is used for fading
        /// </summary>
        [Tooltip("Color property references of material shaders whose alpha value is used for fading.")]
        public string[] ShaderColorProperties = { "_BaseColor", "_Color" }; // _BaseColor (URP), _Color (BRP)

        /// <summary>
        /// Stores the default alpha values of materials before fading them
        /// </summary>
        protected Dictionary<EntityId, float> _defaultMaterialAlphas = new();

        protected virtual void Awake() {
        }

        protected virtual void Start() {
            Utils.GetMaterialsToFade(gameObject, ShaderColorProperties, ref _defaultMaterialAlphas);
        }

        public virtual void HandleCharacterVisibility(Camera camera, Vector3 viewport) {
            Vector3 viewportCenter = camera.transform.position + camera.transform.forward * viewport.z;
            Vector3 closestPointToCharacter = transform.position;
            Collider collider = GetComponent<Collider>();
            if (collider) {
                closestPointToCharacter = collider.ClosestPointOnBounds(viewportCenter);
            }

            // Get the actual distance between the used camera's viewport and the character
            float actualDistance = Vector3.Distance(closestPointToCharacter, viewportCenter);

            // Compute the new alpha value depending on the fading start and end distance
            float t = Mathf.Clamp01((actualDistance - FadingEndDistance) / (FadingStartDistance - FadingEndDistance));

            float newAlpha = Mathf.SmoothStep(FadeOutAlpha, 1.0f, t);
            List<Material> materialsToFade = Utils.GetMaterialsToFade(gameObject, ShaderColorProperties, ref _defaultMaterialAlphas);
            foreach (Material material in materialsToFade) {
                // Adjust their color's alpha value accordingly
                Utils.HasExpectedProperty(material, ShaderColorProperties, out string property);
                Color newColor = material.GetColor(property);
                newColor.a = Mathf.Min(newAlpha, _defaultMaterialAlphas[material.GetEntityId()]);
                material.SetColor(property, newColor);
            }
        }
    }
}
