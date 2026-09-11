using FYP.PlayFabIntegration;
using FYP.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FYP.UI
{
    public class CharacterCreationRoom : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        TMP_InputField characterName;
        [SerializeField]
        TMP_Text outputText;
        [SerializeField]
        PlayfabCharacter playfabCharacter;
        [SerializeField]
        GameObject transition;
        [SerializeField]
        GameObject warning;
        [SerializeField]
        GameObject coins;
        [SerializeField]
        CustomizeCost cost;
        [SerializeField]
        TMP_Text creatButtonText;
        [SerializeField]
        GameObject armorColor;

        [Header("Sliders")]
        [Header("Body")]
        [SerializeField]
        SliderBodyPart[] bodyPartSliders;

        [Header("Color")]
        [SerializeField]
        SliderColor[] colorSliders;

        private void OnEnable()
        {
            Init();
        }

        public void Init()
        {
            SetSliderValues();
            SetExtraUI();
        }

        private void SetExtraUI()
        {
            if (playfabCharacter.cachedBodyData == null)
            {
                EnableUI(false);
                creatButtonText.text = "Create Character";
            }
            else
            {
                EnableUI(true);
                creatButtonText.text = "Modify Character";
            }
        }
        void EnableUI(bool existingCharacter)
        {
            warning.SetActive(!existingCharacter);
            coins.SetActive(existingCharacter);
            cost.gameObject.SetActive(existingCharacter);
            armorColor.SetActive(existingCharacter);
        }

        private void SetSliderValues()
        {
            SetSliderMaxValues();
            GetSliderValuesFromBodyData();
        }

        private void SetSliderMaxValues()
        {
            if (playfabCharacter.cachedBodyData == null)
            {
                foreach (var bodyPartSlider in bodyPartSliders)
                    bodyPartSlider.SetMaxValueForNewCharacter();
                foreach (var colorSlider in colorSliders)
                    colorSlider.SetMaxValueForNewCharacter();
            }
            else
            {
                foreach (var bodyPartSlider in bodyPartSliders)
                    bodyPartSlider.SetMaxValueForExistingCharacter();
                foreach (var colorSlider in colorSliders)
                    colorSlider.SetMaxValueForExistingCharacter();
            }
        }

        private void GetSliderValuesFromBodyData()
        {
            if (playfabCharacter.cachedBodyData == null) return;

            BodyData cachedBodyData = playfabCharacter.cachedBodyData;
            characterName.text = cachedBodyData.characterName;

            foreach (var bodyPart in cachedBodyData.bodyParts)
                foreach (var bodyPartSlider in bodyPartSliders)
                    if (bodyPartSlider.bodyPart == bodyPart.Key)
                    {
                        bodyPartSlider.initialValue = bodyPart.Value;
                        bodyPartSlider.SetSliderValueTo(bodyPart.Value);
                    }

            foreach (var color in cachedBodyData.bodyColors)
                foreach (var colorSlider in colorSliders)
                    if (colorSlider.colorIndex == color.Key)
                    {
                        int colorCode = colorSlider.GetColorCode(color.Value);
                        colorSlider.initialValue = colorCode;
                        colorSlider.SetSliderValueTo(colorCode);
                    }
        }

        public void Create()
        {
            string checkCharName = Validate.ValidateText("PASS", characterName.text, "Character Name");
            if (checkCharName != "PASS")
            {
                outputText.text = checkCharName;
                return;
            }
            if (cost.currentCost == 0)
            {
                ReturnToOMenu();
                return;
            }
            if (playfabCharacter.cachedBodyData != null)
            {
                RemoveGold();
                return;
            }

            ReturnToOMenu();
        }

        void ReturnToOMenu()
        {
            playfabCharacter.character.characterName = characterName.text;
            playfabCharacter.SetData();
            transition.SetActive(true);
        }

        void RemoveGold()
        {
            if (!playfabCharacter.TrySpendGold(cost.currentCost))
            {
                outputText.text = "Not enough gold.";
                return;
            }

            playfabCharacter.character.characterName = characterName.text;
            playfabCharacter.SetData();
            transition.SetActive(true);
        }
    }
}
