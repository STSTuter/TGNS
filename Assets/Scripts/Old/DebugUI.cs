using UnityEngine;

/// <summary>
/// Deliberately ugly IMGUI developer UI for the Steam P2P prototype. Function over form:
/// HOST / JOIN / DISCONNECT / COPY LOBBY ID plus a status block covering everything Step 8 asks for.
/// </summary>
public class DebugUI : MonoBehaviour
{
    private string lobbyIdInput = string.Empty;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 420, 400), GUI.skin.box);

        SteamBootstrap steam = SteamBootstrap.Instance;
        SteamLobbyManager lobby = SteamLobbyManager.Instance;

        GUILayout.Label("STEAM P2P PROTOTYPE (DEBUG UI)", EditorBoldLabelStyle());

        bool steamOk = steam != null && steam.Initialized;
        GUILayout.Label($"Steam initialized: {(steamOk ? "Yes" : "No")}");

        if (!steamOk)
        {
            string err = steam != null ? steam.InitError : "SteamBootstrap not present in scene.";
            GUILayout.Label($"Steam error: {err}");
        }
        else
        {
            GUILayout.Label($"User: {steam.UserName}");
            GUILayout.Label($"SteamID: {steam.LocalSteamId}");
        }

        GUILayout.Space(8);

        if (lobby != null)
        {
            GUILayout.Label($"Current Lobby ID: {(lobby.CurrentLobbyId.IsValid() ? lobby.CurrentLobbyId.ToString() : "-")}");
            GUILayout.Label($"Lobby owner SteamID: {(lobby.LobbyOwnerId.IsValid() ? lobby.LobbyOwnerId.ToString() : "-")}");
            GUILayout.Label($"NGO mode: {lobby.Mode}");
            GUILayout.Label($"NGO clients: {lobby.ConnectedClientCount}");
            GUILayout.Label($"Last status: {lobby.LastStatusMessage}");
        }

        GUILayout.Space(8);

        bool canHostOrJoin = steamOk && lobby != null && lobby.Mode == NgoMode.Offline;

        GUI.enabled = canHostOrJoin;
        if (GUILayout.Button("HOST"))
        {
            lobby.HostGame();
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Lobby ID:", GUILayout.Width(60));
        lobbyIdInput = GUILayout.TextField(lobbyIdInput);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("JOIN"))
        {
            lobby.JoinGame(lobbyIdInput);
        }
        GUI.enabled = true;

        if (lobby != null && lobby.CurrentLobbyId.IsValid())
        {
            if (GUILayout.Button("COPY LOBBY ID"))
            {
                GUIUtility.systemCopyBuffer = lobby.CurrentLobbyId.m_SteamID.ToString();
            }
        }

        GUI.enabled = lobby != null && lobby.Mode != NgoMode.Offline;
        if (GUILayout.Button("DISCONNECT"))
        {
            lobby.Disconnect();
        }
        GUI.enabled = true;

        GUILayout.EndArea();
    }

    private static GUIStyle boldLabelStyle;

    private static GUIStyle EditorBoldLabelStyle()
    {
        if (boldLabelStyle == null)
        {
            boldLabelStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        }

        return boldLabelStyle;
    }
}
