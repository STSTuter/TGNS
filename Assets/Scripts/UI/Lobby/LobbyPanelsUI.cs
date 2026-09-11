using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FYP.UI
{
    public class LobbyPanelsUI : MonoBehaviour
    {
        [Header("Host")]
        [SerializeField]
        GameObject hostQuit;
        [SerializeField]
        Toggle hostToggle;
        [SerializeField]
        HostUI hostPanel;
        [Header("Lobby")]
        [SerializeField]
        GameObject lobbyQuit;
        [SerializeField]
        Toggle lobbyToggle;
        [SerializeField]
        GameObject lobbyPanel;

        public void HostGame()
        {
            SwitchToPanel(false);
            hostPanel.CanAccessLobby(true);
            hostPanel.CanAccesssDungeon(false);
        }

        public void JoinGame()
        {
            SwitchToPanel(false);
            hostPanel.CanAccess(false);
        }

        public void ToLobby()
        {
            SwitchToPanel(true);
        }

        private void SwitchToPanel(bool lobby)
        {
            hostQuit.SetActive(!lobby);
            lobbyQuit.SetActive(lobby);

            hostToggle.gameObject.SetActive(!lobby);
            lobbyToggle.gameObject.SetActive(lobby);

            hostToggle.isOn = !lobby;
            lobbyToggle.isOn = lobby;

            hostPanel.gameObject.SetActive(!lobby);
            lobbyPanel.SetActive(lobby);
        }
    }
}
