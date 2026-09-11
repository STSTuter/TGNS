using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Menu
{
    public class MenuDate : MonoBehaviour
    {
        [SerializeField]
        TMP_Text text;
        void Start()
        {
            DateTime dateTime = DateTime.Now;
            text.text = dateTime.ToString("dd MMMM yyyy");
        }
    }
}