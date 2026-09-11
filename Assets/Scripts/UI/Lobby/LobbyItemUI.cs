using Steamworks;
using TMPro;
using UnityEngine;
using FYP.PlayFabIntegration;

namespace FYP.UI
{
    public class LobbyItemUI : MonoBehaviour
    {
        [SerializeField]
        TMP_Text lobbyName;
        [SerializeField]
        TMP_Text players;
        [SerializeField]
        TMP_Text outputText;

        private CSteamID _lobbyId;
        private LobbyList _lobbyList;

        public void SetValues(CSteamID lobbyId, string displayName, string curPlayer, string maxPlayers, LobbyList lobbyList)
        {
            _lobbyId = lobbyId;
            _lobbyList = lobbyList;
            lobbyName.text = displayName;
            players.text = curPlayer + " / " + maxPlayers;
        }

        public void FindAndJoinLobby()
        {
            if (_lobbyList == null)
            {
                if (outputText != null)
                {
                    outputText.text = "Lobby row is missing its LobbyList reference.";
                }
                return;
            }

            _lobbyList.JoinLobby(_lobbyId);
        }
    }
}
