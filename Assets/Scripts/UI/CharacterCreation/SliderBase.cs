using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FYP.UI
{
    public class SliderBase : MonoBehaviour
    {
        [SerializeField]
        CustomizeCost customizeCost;
        protected Slider sliderComp;
        [SerializeField]
        TMP_Text textComponent;
        [Header("Values")]
        public int newCharacterValue;
        [SerializeField]
        int baseCost = 50;
        public int modifyCharacterValue;
        [SerializeField]
        int extraCost = 100;
        public int initialValue;

        int currentCost;

        private void OnEnable()
        {
            sliderComp = GetComponent<Slider>();
            SliderValueChange();
        }

        public virtual void SliderValueChange()
        {
            textComponent.text = sliderComp.value.ToString("N0");
        }

        public void SetSliderValueTo(int value)
        {
            sliderComp.value = value;
        }

        public void SetMaxValueForNewCharacter()
        {
            CheckForSliderComp();
            sliderComp.maxValue = newCharacterValue;
        }

        public void SetMaxValueForExistingCharacter()
        {
            CheckForSliderComp();
            sliderComp.maxValue = modifyCharacterValue;
        }

        void CheckForSliderComp()
        {
            if (sliderComp == null)
                sliderComp = GetComponent<Slider>();
        }

        public void CalculateCost()
        {
            int sliderValue = (int)sliderComp.value;
            if (sliderValue == initialValue)
            {
                customizeCost.ChangeCost(-currentCost);
                currentCost = 0;
                return;
            }
            if (sliderValue <= newCharacterValue)
            {
                customizeCost.ChangeCost(-currentCost);
                customizeCost.ChangeCost(baseCost);
                currentCost = baseCost;
            }
            else
            {
                customizeCost.ChangeCost(-currentCost);
                customizeCost.ChangeCost(extraCost);
                currentCost = extraCost;
            }
        }
    }
}