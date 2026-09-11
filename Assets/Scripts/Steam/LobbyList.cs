using Steamworks;
using TMPro;
using Unity.Netcode;
using Netcode.Transports;
using UnityEngine;
using FYP.UI;
using FYP.Steam;

namespace FYP.PlayFabIntegration
{
    /// <summary>
    /// Client-side lobby discovery/join. Lists public Steam lobbies filtered to this game
    /// (see PlayfabLobby.GameFilterKey) and joins either by clicking a row or by pasting the
    /// numeric Steam lobby ID. On join, points the SteamNetworkingSocketsTransport at the lobby
    /// owner's SteamID and starts the NGO client - no relay allocation involved.
    /// </summary>
    public class LobbyList : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        LobbyPanelsUI lobbyPanelUI;
        [SerializeField]
        TMP_Text outputText;
        [SerializeField]
        TMP_InputField lobbyCodeJoin;
        [SerializeField]
        PlayfabCharacter playfabCharacter;
        [SerializeField]
        GameObject lobbyListParent;
        [SerializeField]
        GameObject lobbyPrefab;
        [Header("Debug")]
        [SerializeField]
        string playerId;
        public string ownedLobbyId;

        private CallResult<LobbyMatchList_t> _callResultLobbyMatchList;
        private CallResult<LobbyEnter_t> _callResultLobbyEntered;

        private void Awake()
        {
            _callResultLobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
            _callResultLobbyEntered = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);
        }

        private void Start()
        {
            playerId = playfabCharacter.playerId;
            FindLobies();
        }

        public void FindLobies()
        {
            if (SteamBootstrap.Instance == null || !SteamBootstrap.Instance.Initialized)
            {
                ShowError("Steam is not running.");
                return;
            }

            SteamMatchmaking.AddRequestLobbyListStringFilter(PlayfabLobby.GameFilterKey, PlayfabLobby.GameFilterValue, ELobbyComparison.k_ELobbyComparisonEqual);
            SteamAPICall_t handle = SteamMatchmaking.RequestLobbyList();
            _callResultLobbyMatchList.Set(handle);
        }

        private void OnLobbyMatchList(LobbyMatchList_t result, bool ioFailure)
        {
            if (ioFailure)
            {
                ShowError("Failed to fetch the lobby list.");
                return;
            }

            for (int i = 1; i < lobbyListParent.transform.childCount; i++)
            {
                Destroy(lobbyListParent.transform.GetChild(i).gameObject);
            }

            for (int i = 0; i < result.m_nLobbiesMatching; i++)
            {
                CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
                string hostName = SteamMatchmaking.GetLobbyData(lobbyId, PlayfabLobby.HostNameLobbyKey);
                string displayName = string.IsNullOrEmpty(hostName) ? lobbyId.m_SteamID.ToString() : hostName;
                string current = SteamMatchmaking.GetNumLobbyMembers(lobbyId).ToString();
                string max = SteamMatchmaking.GetLobbyMemberLimit(lobbyId).ToString();

                var tempLobby = Instantiate(lobbyPrefab, lobbyListParent.transform);
                tempLobby.GetComponent<LobbyItemUI>().SetValues(lobbyId, displayName, current, max, this);
            }
        }

        public void FindAndJoinLobby()
        {
            if (!ulong.TryParse(lobbyCodeJoin.text, out ulong lobbyIdValue) || lobbyIdValue == 0)
            {
                ShowError("Invalid lobby code - it must be the numeric lobby ID.");
                return;
            }

            JoinLobby(new CSteamID(lobbyIdValue));
        }

        public void JoinLobby(CSteamID lobbyId)
        {
            SteamAPICall_t handle = SteamMatchmaking.JoinLobby(lobbyId);
            _callResultLobbyEntered.Set(handle);
        }

        private void OnLobbyEntered(LobbyEnter_t callback, bool ioFailure)
        {
            var response = (EChatRoomEnterResponse)callback.m_EChatRoomEnterResponse;
            if (ioFailure || response != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                ShowError($"Failed to join lobby: {response}");
                return;
            }

            CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            ownedLobbyId = lobbyId.m_SteamID.ToString();
            CSteamID hostSteamId = SteamMatchmaking.GetLobbyOwner(lobbyId);

            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as SteamNetworkingSocketsTransport;
            if (transport == null)
            {
                ShowError("No SteamNetworkingSocketsTransport is configured on the NetworkManager.");
                return;
            }

            transport.ConnectToSteamID = hostSteamId.m_SteamID;

            bool started = NetworkManager.Singleton.StartClient();
            if (!started)
            {
                ShowError("Failed to start the network client.");
                return;
            }

            lobbyPanelUI.JoinGame();
        }

        private void ShowError(string message)
        {
            Debug.LogError("[LobbyList] " + message);
            if (outputText != null)
            {
                outputText.text = message;
            }
        }
    }
}
