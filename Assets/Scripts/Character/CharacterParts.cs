using System;
using UnityEditor;
using UnityEngine;

namespace FYP.Character
{
    public enum BodyPartIndex
    {
        Hair = 0,
        Face = 1,
        Eyebrows = 2,
        Torso = 3,
        Gauntlets = 4,
        Leggings = 5,
        Boots = 6,
        Helmet = 7,
        Weapon = 8,
        Cape = 9,
    }

    public enum BodyColorIndex
    {
        _Color_Skin = 0,
        _Color_Hair = 1,
        _Color_Eyes = 2,
        _Color_BodyArt = 3,
        _Color_Primary = 4,
        _Color_Secondary = 5,
        _Color_Leather_Primary = 6,
        _Color_Leather_Secondary = 7,
        _Color_Metal_Primary = 8,
        _Color_Metal_Secondary = 9,
        _Color_Metal_Dark = 10,
    }


    [Serializable]
    public class BodyPart
    {
        public GameObject parent;
        public BodyPartIndex bodyPartEnum;
        public int currentBodyPartId;
    }

    public class CharacterParts : MonoBehaviour
    {
        [Header("Info")]
        public string characterName;
        [Header("Body Parts")]
        public BodyPart[] bodyParts;
        [Header("Color")]
        [SerializeField]
        Material defaultMaterial;
        public Material currentMaterial;
        [SerializeField]
        GameObject colorParent;
        private void Start()
        {
            currentMaterial = Instantiate(defaultMaterial);
            ChangeMaterial(transform);
        }

        public void SetAllBodyParts()
        {
            foreach (var bodyPart in bodyParts)
            {
                ChangeCurrentBodyPart(bodyPart.currentBodyPartId, bodyPart);
            }
        }

        public void ChangeBodyPart(BodyPartIndex bodyPart, int value)
        {
            foreach (var bp in bodyParts)
            {
                if (bp.bodyPartEnum == bodyPart)
                {
                    if (bodyPart == BodyPartIndex.Helmet)
                        ChangeCurrentHelmet(value, bp);
                    else if (bodyPart == BodyPartIndex.Hair)
                        ChangeCurrentHair(value, bp);
                    else if (bodyPart == BodyPartIndex.Face)
                        ChangeCurrentHead(value, bp);
                    else
                        ChangeCurrentBodyPart(value, bp);
                }
            }
        }

        public void ChangeCurrentHelmet(int value, BodyPart bp)
        {
            DissableAllOtherMeshes(bp);
            var currentHelmet = EnableRightMesh(value, bp);
            //Helmets that Start with X, will Delete the Hair
            foreach (var bpp in bodyParts)
            {
                if (bpp.bodyPartEnum == BodyPartIndex.Hair)
                    if (currentHelmet.name[0] == 'X' || currentHelmet.name[0] == 'Y')
                        DissableAllOtherMeshes(bpp);
                    else
                        ChangeCurrentBodyPart(bpp.currentBodyPartId, bpp);

                if (bpp.bodyPartEnum == BodyPartIndex.Face)
                    if (currentHelmet.name[0] == 'Y')
                        DissableAllOtherMeshes(bpp);
                    else
                        ChangeCurrentBodyPart(bpp.currentBodyPartId, bpp);
            }
        }

        void ChangeCurrentHair(int value, BodyPart bp)
        {
            DissableAllOtherMeshes(bp);
            foreach (var bpp in bodyParts)
                if (bpp.bodyPartEnum == BodyPartIndex.Helmet)
                    if (GetCurrentMesh(bpp).name[0] == 'Y' || GetCurrentMesh(bpp).name[0] == 'X')
                        bp.currentBodyPartId = value;
                    else
                        EnableRightMesh(value, bp);
        }

        void ChangeCurrentHead(int value, BodyPart bp)
        {
            DissableAllOtherMeshes(bp);
            foreach (var bpp in bodyParts)
                if (bpp.bodyPartEnum == BodyPartIndex.Helmet && GetCurrentMesh(bpp).name[0] != 'Y')
                    EnableRightMesh(value, bp);
                else
                    bp.currentBodyPartId = value;
        }

        GameObject GetCurrentMesh(BodyPart bp)
        {
            return bp.parent.transform.GetChild(bp.currentBodyPartId).gameObject;
        }

        void ChangeCurrentBodyPart(int value, BodyPart bp)
        {
            DissableAllOtherMeshes(bp);
            EnableRightMesh(value, bp);
        }

        void DissableAllOtherMeshes(BodyPart bp)
        {
            foreach (var bodyPart in bp.parent.GetComponentsInChildren<Transform>(true))
                bodyPart.gameObject.SetActive(false);
            bp.parent.SetActive(true);
        }

        GameObject EnableRightMesh(int value, BodyPart bp)
        {
            var currentBP = bp.parent.transform.GetChild(value).gameObject;
            currentBP.SetActive(true);
            foreach (var subPart in currentBP.GetComponentsInChildren<Transform>(true))
                subPart.gameObject.SetActive(true);

            bp.currentBodyPartId = value;
            return currentBP;
        }

        public void CreateMaterial(Color skinColor, Color hairColor, Color eyesColor, Color bodyArtColor,
            Color primary, Color secondary, Color leatherPrimary, Color leatherSecondary, Color metalPrimary, Color metalSecondary, Color metalDark)
        {
            currentMaterial = Instantiate(defaultMaterial);
            currentMaterial.SetColor(Enum.GetName(typeof(BodyColorIndex), BodyColorIndex._Color_Skin), skinColor);
            currentMaterial.SetColor(Enum.GetName(typeof(BodyColorIndex), BodyColorIndex._Color_Hair), hairColor);
            currentMaterial.SetColor(Enum.GetName(typeof(BodyColorIndex), BodyColorIndex._Color_Eyes), eyesColor);
            currentMaterial.SetColor(Enum.GetName(typeof(BodyColorIndex), BodyColorIndex._Color_BodyArt), bodyArtColor);
            currentMaterial.SetColor(Enum.GetName(typeof(BodyColorIndex), BodyColorIndex._Color_Primary), primary);
            currentMaterial.SetColor(Enum.GetName(typeof(BodyColorIndex), BodyColorIndex._Color_Secondary), secondary);
            currentMaterial.SetColor(Enum.GetName(typeof(BodyColorIndex), BodyColorIndex._Color_Leather_Primary), leatherPrimary);
            currentMaterial.SetColor(Enum.GetName(typeof(BodyColorIndex), BodyColorIndex._Color_Leather_Secondary), leatherSecondary);
            currentMaterial.SetColor(Enum.GetName(typeof(BodyColorIndex), BodyColorIndex._Color_Metal_Primary), metalPrimary);
            currentMaterial.SetColor(Enum.GetName(typeof(BodyColorIndex), BodyColorIndex._Color_Metal_Secondary), metalSecondary);
            currentMaterial.SetColor(Enum.GetName(typeof(BodyColorIndex), BodyColorIndex._Color_Metal_Dark), metalDark);
            ChangeMaterial(transform);
        }

        public void SetColorOnMaterial(BodyColorIndex index, Color Color)
        {
            currentMaterial.SetColor(Enum.GetName(typeof(BodyColorIndex), index), Color);
        }

        void ChangeMaterial(Transform currentTransform, int currentDepth = 0, int maxDepth = 4)
        {
            if (maxDepth < currentDepth) return;
            Renderer renderer;
            currentTransform.TryGetComponent(out renderer);
            if (renderer != null)
            {
                renderer.material = currentMaterial;
            }

            foreach (var child in currentTransform.GetComponentsInChildren<Transform>(true))
            {
                ChangeMaterial(child, currentDepth + 1, maxDepth);
            }
        }
    }
}