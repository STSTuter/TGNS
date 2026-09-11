using System;
using UnityEngine;

namespace TGNS.Character
{
    /// <summary>
    /// Slot-based body-part swapping: each slot is a parent Transform whose
    /// children are the interchangeable mesh variants for that slot (index 0,
    /// 1, 2, ...). Only one child is enabled at a time.
    ///
    /// This assumes the character hierarchy under each slot's parent is set
    /// up with one child GameObject per variant, in a fixed order - build
    /// that hierarchy in the prefab (drag the modular part meshes from
    /// Assets/Assets/PolygonFantasyHeroCharacters under the matching slot
    /// parent) before this component has anything to switch between.
    /// </summary>
    public class CharacterParts : MonoBehaviour
    {
        public enum BodyPartIndex
        {
            Hair,
            Face,
            Eyebrows,
            Torso,
            Gauntlets,
            Leggings,
            Boots,
            Helmet,
            Weapon,
            Cape
        }

        public enum BodyColorIndex
        {
            _Color_Skin,
            _Color_Hair,
            _Color_Eyes,
            _Color_BodyArt,
            _Color_Primary,
            _Color_Secondary,
            _Color_Leather_Primary,
            _Color_Leather_Secondary,
            _Color_Metal_Primary,
            _Color_Metal_Secondary,
            _Color_Metal_Dark
        }

        [Serializable]
        public class BodyPart
        {
            public BodyPartIndex bodyPartEnum;
            public GameObject parent;
            public int currentBodyPartId;
        }

        [SerializeField] private BodyPart[] bodyParts = Array.Empty<BodyPart>();
        [SerializeField] private Material defaultMaterial;

        public Material CurrentMaterial { get; private set; }

        private void Start()
        {
            if (defaultMaterial != null)
            {
                CurrentMaterial = Instantiate(defaultMaterial);
                ApplyMaterialRecursively(transform);
            }

            ApplyAllBodyParts();
        }

        public void ApplyAllBodyParts()
        {
            foreach (BodyPart part in bodyParts)
            {
                SelectVariant(part, part.currentBodyPartId);
            }
        }

        public void ChangeBodyPart(BodyPartIndex slot, int variantIndex)
        {
            BodyPart part = FindSlot(slot);
            if (part == null)
            {
                return;
            }

            SelectVariant(part, variantIndex);

            // Helmets can hide hair/face depending on the variant's naming
            // convention (mirrors the source project: a variant whose name
            // starts with 'X' hides hair, 'Y' hides hair and face).
            if (slot == BodyPartIndex.Helmet)
            {
                GameObject selected = GetVariantObject(part, variantIndex);
                bool hidesHair = selected != null && (selected.name.StartsWith("X") || selected.name.StartsWith("Y"));
                bool hidesFace = selected != null && selected.name.StartsWith("Y");

                SetSlotVisible(BodyPartIndex.Hair, !hidesHair);
                SetSlotVisible(BodyPartIndex.Face, !hidesFace);
            }
        }

        public void SetColor(BodyColorIndex colorSlot, Color color)
        {
            if (CurrentMaterial == null)
            {
                return;
            }

            CurrentMaterial.SetColor(colorSlot.ToString(), color);
        }

        public int GetCurrentVariant(BodyPartIndex slot)
        {
            BodyPart part = FindSlot(slot);
            return part?.currentBodyPartId ?? 0;
        }

        public Color GetColor(BodyColorIndex colorSlot)
        {
            if (CurrentMaterial == null || !CurrentMaterial.HasProperty(colorSlot.ToString()))
            {
                return Color.white;
            }

            return CurrentMaterial.GetColor(colorSlot.ToString());
        }

        private void SetSlotVisible(BodyPartIndex slot, bool visible)
        {
            BodyPart part = FindSlot(slot);
            if (part == null)
            {
                return;
            }

            GameObject current = GetVariantObject(part, part.currentBodyPartId);
            if (current != null)
            {
                current.SetActive(visible);
            }
        }

        private BodyPart FindSlot(BodyPartIndex slot)
        {
            foreach (BodyPart part in bodyParts)
            {
                if (part.bodyPartEnum == slot)
                {
                    return part;
                }
            }

            return null;
        }

        private static GameObject GetVariantObject(BodyPart part, int variantIndex)
        {
            if (part.parent == null || variantIndex < 0 || variantIndex >= part.parent.transform.childCount)
            {
                return null;
            }

            return part.parent.transform.GetChild(variantIndex).gameObject;
        }

        private static void SelectVariant(BodyPart part, int variantIndex)
        {
            if (part.parent == null)
            {
                return;
            }

            Transform parentTransform = part.parent.transform;
            for (int i = 0; i < parentTransform.childCount; i++)
            {
                parentTransform.GetChild(i).gameObject.SetActive(i == variantIndex);
            }

            part.currentBodyPartId = variantIndex;
        }

        private void ApplyMaterialRecursively(Transform current, int depth = 0, int maxDepth = 4)
        {
            if (depth > maxDepth)
            {
                return;
            }

            if (current.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = CurrentMaterial;
            }

            for (int i = 0; i < current.childCount; i++)
            {
                ApplyMaterialRecursively(current.GetChild(i), depth + 1, maxDepth);
            }
        }
    }
}
