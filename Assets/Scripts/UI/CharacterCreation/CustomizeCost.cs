using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FYP.UI
{
    public class CustomizeCost : MonoBehaviour
    {
        [SerializeField]
        TMP_Text costText;
        public int currentCost;

        public void ChangeCost(int value)
        {
            currentCost += value;
            costText.text = currentCost.ToString();
        }
    }
}