# TGNS — Steam P2P 2-Player Prototype

A minimal Unity multiplayer prototype: two players, each controlling a capsule with WASD,
connected **peer-to-peer over Steam** — no dedicated server, no Unity Relay/Lobby, no manual
IP entry, no port forwarding.

**Stack:** Unity 6000.6.0f1 · Netcode for GameObjects (NGO) 2.13.2 · Steamworks.NET ·
community SteamNetworkingSockets NGO transport · Steam AppID 480 (Valve's public "Spacewar" test AppID).

This document is the entry point for a human or an agent picking up this project cold. For the
full technical rationale and API-verification trail, see [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## TL;DR — how it works

1. **PC A** launches the game (Steam must already be running) → clicks **HOST** → the game
   creates a Steam Lobby via `SteamMatchmaking.CreateLobby` → starts NGO as **Host**.
2. **PC A** copies the numeric **Lobby ID** and sends it to PC B out-of-band (Discord, chat, etc).
3. **PC B** launches the same build, logged into a *different* Steam account → pastes the Lobby
   ID → clicks **JOIN** → the game joins the Steam lobby, reads the lobby owner's SteamID, points
   the transport at that SteamID → starts NGO as **Client**.
4. NGO spawns one Player prefab per connected client. All transform traffic rides over Steam's
   `SteamNetworkingSockets` P2P connection (NAT punch-through / Steam Datagram Relay) — there is
   no IP address anywhere in this flow.
5. Each client only reads WASD input for **its own** capsule (owner-authoritative
   `NetworkTransform`); the other capsule updates purely from network replication.

## Project layout

```
Assets/
  PlayerMovement.cs          Owner-authoritative WASD movement (NetworkBehaviour)
  Scripts/
    SteamBootstrap.cs        Steam API init / RunCallbacks / shutdown, exposes local user info
    SteamLobbyManager.cs     Steam lobby create/join/leave + NGO StartHost/StartClient/Shutdown
    DebugUI.cs                Ugly-but-functional IMGUI dev UI (HOST/JOIN/DISCONNECT/status)
  Editor/
    SteamAppIdPostBuild.cs   Post-build step: writes steam_appid.txt beside the built .exe
  Prefabs/
    Player.prefab            Capsule + NetworkObject + NetworkTransform(Owner) + PlayerMovement
  Scenes/
    SampleScene.unity        The only scene; contains Floor, Light, static camera, NetworkManager,
                              SteamNetwork (bootstrap+lobby+UI) GameObjects
steam_appid.txt               "480" — read by Steamworks.NET when running from the Editor
docs/
  ARCHITECTURE.md            Deep dive: why these APIs, what was verified, known limitations
```

## Scene object reference

| GameObject | Components | Purpose |
|---|---|---|
| `NetworkManager` | `NetworkManager`, `SteamNetworkingSocketsTransport` | NGO session; `NetworkConfig.NetworkTransport` and `NetworkConfig.PlayerPrefab` are wired to this transport and to `Assets/Prefabs/Player.prefab` |
| `SteamNetwork` | `SteamBootstrap`, `SteamLobbyManager`, `DebugUI` | All Steam plumbing + the dev UI |
| `Main Camera` | static, positioned at `(0, 12, -12)` pitched 45° down | Frames both spawn points; **not** networked, no player-follow logic |
| `Floor`, `Directional Light` | — | Static scene dressing |

No player capsules are hand-placed in the scene — NGO spawns `Player.prefab` automatically per
connected client (`OwnerClientId == 0` spawns at `(-2,1,0)`, all other clients at `(2,1,0)`).

## Building

```powershell
$env:UNITY_CLI_CHANNEL='beta'; irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex
```

Then, with the project open in the Editor (the CLI drives a live Editor via the
`com.unity.pipeline` package rather than spawning a second batch instance):

```bash
unity command build -- StandaloneWindows64 Builds/TGNS.exe Windows '' '' true false
unity command build_status
```

Output lands in `Builds/` (gitignored). `steam_appid.txt` (contents `480`) is copied next to
`TGNS.exe` automatically by `SteamAppIdPostBuild.cs`.

## Testing (two PCs, two Steam accounts)

1. Both PCs: Steam client running and logged into **different** accounts. **The two accounts
   must be Steam friends** — the lobby is created as `k_ELobbyTypeFriendsOnly`
   (see [SteamLobbyManager.cs](Assets/Scripts/SteamLobbyManager.cs), `LobbyVisibility` constant;
   flip to `k_ELobbyTypePublic` to lift this requirement).
2. PC A: run `TGNS.exe` → confirm the debug UI shows `Steam initialized: Yes` → click **HOST** →
   click **COPY LOBBY ID** → send the ID to PC B.
3. PC B: run the same build → paste the Lobby ID → click **JOIN**.
4. Expected on both machines: `NGO clients: 2`, two capsules visible, each machine's WASD moves
   only its own capsule.
5. Diagnostics: every step logs to the Unity Console and to
   `%USERPROFILE%\AppData\LocalLow\<CompanyName>\TGNS\Player.log` on each machine.

## Known limitations

- AppID 480 (Spacewar) is Valve's shared public test AppID — fine for P2P networking tests, not
  for shipping (no overlay branding, no achievements/stats, shared namespace with every other
  developer testing against 480).
- `FriendsOnly` lobby visibility means the two test accounts must already be Steam friends.
- No reconnect/host-migration logic, no lag compensation/prediction — this is intentionally the
  smallest possible working prototype (see the constraints list in `docs/ARCHITECTURE.md`).

## What NOT to add without discussion

This prototype is deliberately minimal. Do not add inventory, health, jumping, physics
networking, prediction/reconciliation, animation, voice chat, a matchmaking browser, a dedicated
server, production UI, or scene transitions unless the user asks — see `docs/ARCHITECTURE.md`
for the full rationale.
