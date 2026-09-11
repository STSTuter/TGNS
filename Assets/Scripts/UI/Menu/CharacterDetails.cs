using FYP.Character;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FYP.UI
{
    public class CharacterDetails : MonoBehaviour
    {
        [SerializeField]
        CharacterParts character;

        [SerializeField]
        TMP_Text characterName;

        private void OnEnable()
        {
            characterName.text = character.characterName;
        }
    }
}