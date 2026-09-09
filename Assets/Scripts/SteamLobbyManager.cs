using Netcode.Transports;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Minimal Steam lobby + NGO session glue for a 2-player prototype:
/// HOST creates a friends-only Steam lobby and starts NGO as Host;
/// JOIN enters a pasted lobby ID, resolves the lobby owner's SteamID, points the
/// SteamNetworkingSocketsTransport at it, and starts NGO as Client.
/// No IP addresses, no Unity Relay/Lobby service - Steam is the only backend used.
/// </summary>
public enum NgoMode
{
    Offline,
    Host,
    Client
}

public class SteamLobbyManager : MonoBehaviour
{
    public static SteamLobbyManager Instance { get; private set; }

    // Flip to k_ELobbyTypePublic if the two test accounts aren't Steam friends.
    private const ELobbyType LobbyVisibility = ELobbyType.k_ELobbyTypeFriendsOnly;
    private const int MaxPlayers = 2;
    private const string HostSteamIdLobbyKey = "HostSteamID";

    public CSteamID CurrentLobbyId { get; private set; } = CSteamID.Nil;
    public CSteamID LobbyOwnerId { get; private set; } = CSteamID.Nil;
    public NgoMode Mode { get; private set; } = NgoMode.Offline;
    public string LastStatusMessage { get; private set; } = "Idle.";

    private SteamNetworkingSocketsTransport transport;

    private CallResult<LobbyCreated_t> callResultLobbyCreated;
    private CallResult<LobbyEnter_t> callResultLobbyEntered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        callResultLobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
        callResultLobbyEntered = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    private void Start()
    {
        transport = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.NetworkConfig.NetworkTransport as SteamNetworkingSocketsTransport
            : null;

        if (transport == null)
        {
            Debug.LogError("[SteamLobbyManager] NetworkManager.Singleton has no SteamNetworkingSocketsTransport assigned.");
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnNgoClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnNgoClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnNgoClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnNgoClientDisconnected;
        }
    }

    public int ConnectedClientCount =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening
            ? NetworkManager.Singleton.ConnectedClientsList.Count
            : 0;

    // ---------------------------------------------------------------- HOST

    public void HostGame()
    {
        if (SteamBootstrap.Instance == null || !SteamBootstrap.Instance.Initialized)
        {
            SetStatus("Cannot host: Steam is not initialized.");
            Debug.LogError("[SteamLobbyManager] " + LastStatusMessage);
            return;
        }

        SetStatus("Requesting Steam lobby creation...");
        Debug.Log("[SteamLobbyManager] Lobby creation requested.");

        SteamAPICall_t handle = SteamMatchmaking.CreateLobby(LobbyVisibility, MaxPlayers);
        callResultLobbyCreated.Set(handle);
    }

    private void OnLobbyCreated(LobbyCreated_t callback, bool ioFailure)
    {
        if (ioFailure || callback.m_eResult != EResult.k_EResultOK)
        {
            SetStatus($"Lobby creation failed: {callback.m_eResult} (ioFailure={ioFailure})");
            Debug.LogError("[SteamLobbyManager] " + LastStatusMessage);
            return;
        }

        CurrentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        LobbyOwnerId = SteamUser.GetSteamID();
        SteamMatchmaking.SetLobbyData(CurrentLobbyId, HostSteamIdLobbyKey, LobbyOwnerId.m_SteamID.ToString());

        Debug.Log($"[SteamLobbyManager] Lobby created. LobbyID={CurrentLobbyId}, HostSteamID={LobbyOwnerId}");

        bool started = NetworkManager.Singleton.StartHost();
        Debug.Log($"[SteamLobbyManager] NGO StartHost result: {started}");

        if (!started)
        {
            SetStatus("NGO StartHost failed.");
            Debug.LogError("[SteamLobbyManager] " + LastStatusMessage);
            return;
        }

        Mode = NgoMode.Host;
        SetStatus($"Hosting. Lobby ID: {CurrentLobbyId}");
    }

    // ---------------------------------------------------------------- JOIN

    public void JoinGame(string lobbyIdText)
    {
        if (SteamBootstrap.Instance == null || !SteamBootstrap.Instance.Initialized)
        {
            SetStatus("Cannot join: Steam is not initialized.");
            Debug.LogError("[SteamLobbyManager] " + LastStatusMessage);
            return;
        }

        if (!ulong.TryParse(lobbyIdText, out ulong lobbyIdValue) || lobbyIdValue == 0)
        {
            SetStatus("Invalid Lobby ID - must be a numeric SteamID64.");
            Debug.LogError("[SteamLobbyManager] " + LastStatusMessage);
            return;
        }

        CSteamID lobbyId = new CSteamID(lobbyIdValue);
        SetStatus($"Joining lobby {lobbyId}...");
        Debug.Log("[SteamLobbyManager] Lobby joining: " + lobbyId);

        SteamAPICall_t handle = SteamMatchmaking.JoinLobby(lobbyId);
        callResultLobbyEntered.Set(handle);
    }

    private void OnLobbyEntered(LobbyEnter_t callback, bool ioFailure)
    {
        var response = (EChatRoomEnterResponse)callback.m_EChatRoomEnterResponse;

        if (ioFailure || response != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            SetStatus($"Lobby join failed: {response} (ioFailure={ioFailure})");
            Debug.LogError("[SteamLobbyManager] " + LastStatusMessage);
            return;
        }

        CurrentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        LobbyOwnerId = SteamMatchmaking.GetLobbyOwner(CurrentLobbyId);

        Debug.Log($"[SteamLobbyManager] Lobby joined: {CurrentLobbyId}. Owner SteamID: {LobbyOwnerId}");

        if (transport == null)
        {
            SetStatus("Joined lobby but no SteamNetworkingSocketsTransport is configured.");
            Debug.LogError("[SteamLobbyManager] " + LastStatusMessage);
            return;
        }

        transport.ConnectToSteamID = LobbyOwnerId.m_SteamID;

        bool started = NetworkManager.Singleton.StartClient();
        Debug.Log($"[SteamLobbyManager] NGO StartClient result: {started}");

        if (!started)
        {
            SetStatus("NGO StartClient failed.");
            Debug.LogError("[SteamLobbyManager] " + LastStatusMessage);
            return;
        }

        Mode = NgoMode.Client;
        SetStatus($"Connecting to host {LobbyOwnerId}...");
    }

    // ---------------------------------------------------------- DISCONNECT

    public void Disconnect()
    {
        Debug.Log("[SteamLobbyManager] Disconnect requested.");

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        if (CurrentLobbyId.IsValid())
        {
            SteamMatchmaking.LeaveLobby(CurrentLobbyId);
        }

        CurrentLobbyId = CSteamID.Nil;
        LobbyOwnerId = CSteamID.Nil;
        Mode = NgoMode.Offline;
        SetStatus("Disconnected.");
    }

    private void OnApplicationQuit()
    {
        if (CurrentLobbyId.IsValid())
        {
            SteamMatchmaking.LeaveLobby(CurrentLobbyId);
        }
    }

    // -------------------------------------------------------------- NGO events

    private void OnNgoClientConnected(ulong clientId)
    {
        Debug.Log($"[SteamLobbyManager] NGO client connected: {clientId}");
        SetStatus($"Client connected: {clientId}");
    }

    private void OnNgoClientDisconnected(ulong clientId)
    {
        Debug.Log($"[SteamLobbyManager] NGO client disconnected: {clientId}");
        SetStatus($"Client disconnected: {clientId}");
    }

    private void SetStatus(string message)
    {
        LastStatusMessage = message;
    }
}
