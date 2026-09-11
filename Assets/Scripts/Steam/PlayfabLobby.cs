using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using FYP.UI;
using FYP.Steam;

namespace FYP.PlayFabIntegration
{
    /// <summary>
    /// Host-side lobby creation. Creates a public Steam lobby (filtered by GameFilterKey so this
    /// game's lobbies don't get lost in the noise of every other prototype sharing the 480 test
    /// AppID), then starts the NGO host - the SteamNetworkingSocketsTransport on NetworkManager
    /// handles the actual P2P connection, no relay allocation/join-code involved.
    /// </summary>
    public class PlayfabLobby : MonoBehaviour
    {
        internal const string HostSteamIdLobbyKey = "HostSteamID";
        internal const string HostNameLobbyKey = "HostName";
        internal const string GameFilterKey = "FYP_GAME";
        internal const string GameFilterValue = "1";

        [Header("Steam")]
        [SerializeField]
        PlayfabCharacter playfabCharacter;
        [SerializeField]
        string ownedLobbyId;

        [Header("UI References")]
        [SerializeField]
        TMP_Text outputText;
        public HostUI hostUI;
        [SerializeField]
        LobbyList lobbyList;

        private CSteamID _currentLobbyId = CSteamID.Nil;
        private CallResult<LobbyCreated_t> _callResultLobbyCreated;

        private void Awake()
        {
            _callResultLobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
        }

        private void OnEnable()
        {
            ownedLobbyId = lobbyList.ownedLobbyId;
        }

        public void CreateLobby()
        {
            if (SteamBootstrap.Instance == null || !SteamBootstrap.Instance.Initialized)
            {
                ShowError("Steam is not running - cannot host.");
                return;
            }

            uint maxPlayerNumbers = uint.Parse(hostUI.playerDropdown.options[hostUI.playerDropdown.value].text);
            SteamAPICall_t handle = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, (int)maxPlayerNumbers);
            _callResultLobbyCreated.Set(handle);
        }

        private void OnLobbyCreated(LobbyCreated_t callback, bool ioFailure)
        {
            if (ioFailure || callback.m_eResult != EResult.k_EResultOK)
            {
                ShowError($"Lobby creation failed: {callback.m_eResult}");
                return;
            }

            _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            ownedLobbyId = _currentLobbyId.m_SteamID.ToString();

            SteamMatchmaking.SetLobbyData(_currentLobbyId, HostSteamIdLobbyKey, SteamUser.GetSteamID().m_SteamID.ToString());
            SteamMatchmaking.SetLobbyData(_currentLobbyId, HostNameLobbyKey, SteamFriends.GetPersonaName());
            SteamMatchmaking.SetLobbyData(_currentLobbyId, GameFilterKey, GameFilterValue);

            bool started = NetworkManager.Singleton.StartHost();
            if (!started)
            {
                ShowError("Failed to start the network host.");
                return;
            }

            hostUI.CanAccesssDungeon(true);
            hostUI.CanAccessLobby(false);
            hostUI.lobbyCode.text = ownedLobbyId;
        }

        private void OnDisable()
        {
            LeaveLobby();
        }

        private void OnApplicationQuit()
        {
            LeaveLobby();
        }

        private void LeaveLobby()
        {
            if (_currentLobbyId == CSteamID.Nil)
            {
                return;
            }

            SteamMatchmaking.LeaveLobby(_currentLobbyId);
            _currentLobbyId = CSteamID.Nil;
        }

        private void ShowError(string message)
        {
            Debug.LogError("[PlayfabLobby] " + message);
            if (outputText != null)
            {
                outputText.text = message;
            }
        }
    }
}
