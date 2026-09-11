using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FYP.UI
{
    public class HostUI : MonoBehaviour
    {
        [Header("Lobby Settings")]
        public TMP_Dropdown playerDropdown;
        public TMP_InputField lobbyCode;
        public Button createLobbyButton;

        [Header("Dungeon Settings")]
        public TMP_Dropdown dungeonMapDropdown;
        public TMP_Dropdown diffcultyDropdown;
        public Button createDungeonButton;

        public void CanAccess(bool isHost)
        {
            CanAccessLobby(isHost);
            CanAccesssDungeon(isHost);
        }

        public void CanAccessLobby(bool canAccess)
        {
            playerDropdown.interactable = canAccess;
            createLobbyButton.interactable = canAccess;
        }

        public void CanAccesssDungeon(bool canAccess)
        {
            dungeonMapDropdown.interactable = canAccess;
            diffcultyDropdown.interactable = canAccess;
            createDungeonButton.interactable = canAccess;
        }
    }
}