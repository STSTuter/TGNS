using Steamworks;
using UnityEngine;

namespace FYP.Steam
{
    /// <summary>
    /// Initializes Steamworks.NET on startup, pumps SteamAPI callbacks every frame, and shuts down
    /// cleanly on exit. Exposes the minimal state the rest of the game (SteamLobby, LobbyList,
    /// account/login UI) needs: whether init succeeded, the local player's display name and SteamID.
    /// Never silently swallows a Steam init failure - it logs loudly and exposes InitError for UI.
    /// </summary>
    public class SteamBootstrap : MonoBehaviour
    {
        public static SteamBootstrap Instance { get; private set; }

        public bool Initialized { get; private set; }
        public string InitError { get; private set; } = string.Empty;

        public string UserName => Initialized ? SteamFriends.GetPersonaName() : string.Empty;
        public CSteamID LocalSteamId => Initialized ? SteamUser.GetSteamID() : CSteamID.Nil;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSteam();
        }

        private void InitializeSteam()
        {
            if (!Packsize.Test())
            {
                InitError = "Steamworks.NET Packsize.Test() failed - native/managed struct packing mismatch.";
                Debug.LogError("[SteamBootstrap] " + InitError);
                return;
            }

            ESteamAPIInitResult result = SteamAPI.InitEx(out string steamErrMsg);
            if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                Initialized = false;
                InitError = $"SteamAPI.InitEx failed ({result}): {steamErrMsg}";
                Debug.LogError("[SteamBootstrap] Steam initialization failed: " + InitError +
                                " Make sure Steam is running and steam_appid.txt (480) sits beside the executable.");
                return;
            }

            Initialized = true;
            InitError = string.Empty;
            Debug.Log($"[SteamBootstrap] Steam initialized. User: {SteamFriends.GetPersonaName()}, SteamID: {SteamUser.GetSteamID()}");

            // Warm up Steam Datagram Relay access now so the host's first connection isn't delayed;
            // the community transport also calls this on the client path before ConnectP2P.
            SteamNetworkingUtils.InitRelayNetworkAccess();
        }

        private void Update()
        {
            if (Initialized)
            {
                SteamAPI.RunCallbacks();
            }
        }

        private void OnApplicationQuit()
        {
            ShutdownSteam();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ShutdownSteam();
            }
        }

        private void ShutdownSteam()
        {
            if (Initialized)
            {
                Debug.Log("[SteamBootstrap] Shutting down SteamAPI.");
                SteamAPI.Shutdown();
                Initialized = false;
            }
        }
    }
}
