# Historical networking baseline

Archived on 2026-09-11 during documentation standardization. Original dates/build revisions were not recorded.

This is historical evidence, not current instructions or a current test report. The original README claims two-PC verification while the original architecture notes say it was not observed. That discrepancy is unresolved. The capsule-only scope and feature exclusions below are superseded by [Game design](../GAME_DESIGN.md) and [Architecture](../ARCHITECTURE.md). Code and prefab wiring have changed since these notes.

Original path text below is relative to the repository root; Markdown hyperlinks have been rebased for this archive. Read this archive only for the original API investigation or historical rationale; recheck installed sources before relying on it.

## Original README

# TGNS — Steam P2P 2-Player Prototype

A minimal Unity multiplayer prototype: players each control a capsule with WASD, connected
**peer-to-peer over Steam** — no dedicated server, no Unity Relay/Lobby, no manual IP entry, no
port forwarding. The lobby/spawn system supports up to **4** players (`PlayerSpawnPoints.MaxPlayers`);
the tested and verified end-to-end flow so far is 2 PCs / 2 Steam accounts (see Testing below).

**Stack:** Unity 6000.6.0f1 · Netcode for GameObjects (NGO) 2.13.2 · Steamworks.NET ·
community SteamNetworkingSockets NGO transport · Steam AppID 480 (Valve's public "Spacewar" test AppID).

This document is the entry point for a human or an agent picking up this project cold. For the
full technical rationale and API-verification trail, see [`docs/ARCHITECTURE.md`](../../docs/ARCHITECTURE.md).

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
    PlayerSpawnPoints.cs     Registry of world-placed spawn point Transforms (up to MaxPlayers)
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
| `Main Camera` | static, positioned at `(0, 12, -12)` pitched 45° down | Frames the spawn area; **not** networked, no player-follow logic |
| `Floor`, `Directional Light` | — | Static scene dressing |
| `SpawnPoints` | `PlayerSpawnPoints` | Parent of up to `PlayerSpawnPoints.MaxPlayers` (4) empty child GameObjects — the actual spawn locations, placed in the world |
| `SpawnPoints/SpawnPoint_0..3` | `Transform` only | Plain empty GameObjects. Move these around in the Editor to change where players spawn — no code changes needed |

No player capsules are hand-placed in the scene — NGO spawns `Player.prefab` automatically per
connected client. Each client positions its own capsule on spawn by asking
`PlayerSpawnPoints.Instance` for the point at index `OwnerClientId % spawnPoints.Length` (host is
always client 0 → `SpawnPoint_0`). To add/remove/move spawn points: edit the `SpawnPoints`
GameObject's children and re-wire the `spawnPoints` array on its `PlayerSpawnPoints` component (or
leave the array empty and it auto-collects its own direct children in hierarchy order). Max
player count is a single constant, `PlayerSpawnPoints.MaxPlayers` (currently 4) — it also drives
the Steam lobby's `cMaxMembers` in `SteamLobbyManager`, so the two stay in sync automatically.

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
   (see [SteamLobbyManager.cs](../../Assets/Scripts/SteamLobbyManager.cs), `LobbyVisibility` constant;
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


---

## Original architecture notes

# Architecture & Implementation Notes

This document exists so a future agent (or human) does not have to re-derive package
compatibility, API surfaces, or design decisions from scratch. Everything under "Verified APIs"
below was checked against the *actually installed* package source in this project — not assumed
from tutorials or older SDK versions. When you touch this code, re-verify against installed
`Library/PackageCache` sources the same way if you upgrade a package.

## Why this stack

Requirement was: 2-player Internet multiplayer, no Unity Relay, no Unity Lobby, no Photon, no
Mirror/FishNet, no port forwarding, no direct IP entry, tested across two Windows PCs on two
separate Steam accounts. That rules out Unity's own transport (needs Relay or a reachable IP) and
points at Steam's own P2P networking (`SteamNetworkingSockets`, i.e. Steam Datagram Relay + NAT
punch-through) as the transport, with a Steam Lobby only as the out-of-band signalling channel to
exchange the host's SteamID.

## Package versions (pinned)

| Package | Version | Why pinned this way |
|---|---|---|
| `com.unity.netcode.gameobjects` | **2.13.2** (exact, in `Packages/manifest.json`) | Latest stable NGO 2.x at the time of writing. Its `NetworkTransport` abstract surface was confirmed to match exactly what the community Steam transport implements (see below) — no adapter code needed. |
| `com.unity.transport` | 6.6.0 | Pulled in transitively by NGO; not used directly (we use the Steam transport, not `UnityTransport`). |
| `com.rlabrecque.steamworks.net` | git `main` @ commit `ba71581f1ed7349e8d0f17ddc6f135dd3bc8a6a3` | Only distributed via git URL; no separate version tags to pin to, so the resolved commit hash is recorded here for reproducibility. Resolved package version metadata reports `2025.165.0`. |
| `com.community.netcode.transport.steamnetworkingsockets` | git `main` @ commit `0fab638470379ace12b0149dfb41c043d23dbce5` | Same — git-only package (Heathen Group / Unity multiplayer-community-contributions), version `1.0.1` per its `package.json`. |
| `com.unity.pipeline` | 0.6.0-exp.1 | **Dev/tooling only.** Lets the Unity CLI drive a live Editor (create GameObjects, run eval, trigger builds) instead of hand-editing `.unity`/`.prefab` YAML. Not required for the game to run; produces one harmless build warning ("No RuntimePipelineConfig asset found... Pipeline will be disabled in Player builds") which is expected and safe to ignore. Feel free to remove it if the CLI workflow is no longer needed, but there's no harm in leaving it.

If you upgrade NGO past 2.13.2, **re-check the Steam transport's overrides against the new
`NetworkTransport` abstract class** before assuming it still compiles — see the compatibility
section below for exactly what to diff.

## Compatibility: NGO 2.13.2 ↔ SteamNetworkingSocketsTransport 1.0.1

This is the trickiest part of the setup and the reason the plan for this project explicitly said
"inspect installed sources, don't copy from tutorials." NGO's `NetworkTransport` abstract API has
changed across major versions; the community Steam transport was last updated for an older NGO
generation. As of this pairing they still line up exactly:

`NetworkTransport` (NGO 2.13.2) abstract/required members:
`ServerClientId`, `IsSupported`, `Initialize(NetworkManager=null)`, `Send`, `PollEvent`,
`StartClient`, `StartServer`, `Shutdown`, `DisconnectLocalClient`, `DisconnectRemoteClient`,
`GetCurrentRtt`. (`OnEarlyUpdate`/`OnPostLateUpdate`/`OnCurrentTopology` are *virtual*, not
required.)

`SteamNetworkingSocketsTransport` (namespace `Netcode.Transports`) implements exactly that set —
confirmed by reading
`Library/PackageCache/com.community.netcode.transport.steamnetworkingsockets@*/Runtime/SteamNetworkingSocketsTransport.cs`
directly. **No patch/adapter was needed.** If a future NGO version adds new abstract members, the
transport will fail to compile and Unity will boot into Safe Mode — see the Troubleshooting
section.

The transport's only configuration surface: a public field `ulong ConnectToSteamID` (set before
`StartClient()`) and an optional `SteamNetworkingConfigValue_t[] options`. Host mode
(`StartServer()`) needs no configuration at all — it just calls
`SteamNetworkingSockets.CreateListenSocketP2P(...)`.

The transport calls `SteamNetworkingUtils.InitRelayNetworkAccess()` itself inside `StartClient()`
(client-side SDR warm-up). `SteamBootstrap.cs` also calls it once at Steam-init time so the
**host** warms SDR too, since the transport never does that for `StartServer()`.

## Steamworks.NET API surface actually used

Verified against `Library/PackageCache/com.rlabrecque.steamworks.net@*/Runtime/`:

- `SteamAPI.InitEx(out string)` → returns `ESteamAPIInitResult` (`k_ESteamAPIInitResult_OK` /
  `_FailedGeneric` / `_NoSteamClient` / `_VersionMismatch`), with a human-readable error string.
  Used instead of the older bool-returning `SteamAPI.Init()` specifically because it gives a
  readable failure reason for the debug UI (Step 3 of the original spec required *no silent
  failures*).
- `Packsize.Test()` — struct-packing sanity check, called before `InitEx`. **Note:** an earlier
  draft of `SteamBootstrap.cs` also called a `DllCheck.Test()` that does **not exist** in this
  Steamworks.NET version — it was removed after the compiler caught it. If you see that class
  referenced anywhere (old notes, a different tutorial), it's stale advice for this package
  version.
- `SteamAPI.RunCallbacks()` — pumped every `Update()`.
- `SteamAPI.Shutdown()` — called from both `OnApplicationQuit()` and `OnDestroy()`.
- `SteamFriends.GetPersonaName()`, `SteamUser.GetSteamID()` — local user display info.
- `SteamMatchmaking.CreateLobby(ELobbyType, int)` → async, resolved via
  `CallResult<LobbyCreated_t>` (pattern: `CallResult<T>.Create(callback)` then `.Set(handle)`).
- `SteamMatchmaking.JoinLobby(CSteamID)` → async, resolved via `CallResult<LobbyEnter_t>`; check
  `(EChatRoomEnterResponse)callback.m_EChatRoomEnterResponse ==
  k_EChatRoomEnterResponseSuccess`.
- `SteamMatchmaking.GetLobbyOwner(CSteamID)` → `CSteamID` of the host, used to set
  `transport.ConnectToSteamID`.
- `SteamMatchmaking.LeaveLobby(CSteamID)` — called on Disconnect and on quit if still in a lobby.
- `CSteamID.Nil` / `.IsValid()` — used throughout for "no lobby" sentinel state.

## NGO API surface actually used

Verified against `Library/PackageCache/com.unity.netcode.gameobjects@*/Runtime/`:

- `NetworkManager.Singleton.StartHost()` / `.StartClient()` / `.Shutdown(bool discardMessageQueue = false)`.
- `NetworkManager.Singleton.NetworkConfig.NetworkTransport` (public field, type `NetworkTransport`)
  and `.NetworkConfig.PlayerPrefab` (public field, type `GameObject`) — both wired via the Editor
  Inspector (or, in this project's case, the Unity CLI's `set_serialized_field` against the
  `NetworkConfig.NetworkTransport` / `NetworkConfig.PlayerPrefab` SerializedProperty paths — the
  top-level `NetworkConfig` field itself is a plain C# class, not directly settable as a single
  object reference).
- `NetworkManager.Singleton.OnClientConnectedCallback` / `OnClientDisconnectCallback` — logging
  hooks in `SteamLobbyManager`.
- `NetworkManager.Singleton.ConnectedClientsList` (`IReadOnlyList<NetworkClient>`) — used for the
  "NGO clients: N" debug UI counter.
- `NetworkBehaviour.IsOwner`, `.OwnerClientId`, `.OnNetworkSpawn()` — used in `PlayerMovement.cs`
  for input gating and initial spawn-position offset.
- `NetworkTransform.AuthorityMode` (enum `AuthorityModes { Server, Owner }`) — set to `Owner` on
  the Player prefab's `NetworkTransform` component. This is what makes movement **owner
  authoritative** without needing the older `ClientNetworkTransform` sample workaround; NGO 2.x
  supports owner authority natively via this field in client-server topology.

## Design decisions and why

- **Owner-authoritative movement, no Rigidbody.** `PlayerMovement.Update()` early-returns unless
  `IsOwner`, then does a plain `transform.position +=` each frame. `NetworkTransform` with
  `AuthorityMode = Owner` pushes that position to the server and all other clients. This is the
  simplest possible movement model and matches the "no physics networking, no
  prediction/reconciliation" constraint explicitly requested.
- **Spawn position applied by the owner, in `OnNetworkSpawn()`, sourced from a world-placed spawn
  point registry.** Because authority is owner-side, a server-set spawn position would never
  replicate outward under `AuthorityMode.Owner` — only the owner's own transform writes are
  synced. So each client still sets its own spawn position the moment it spawns, but now looks it
  up from `PlayerSpawnPoints.Instance.GetSpawnPosition(OwnerClientId)` (an earlier revision
  hardcoded `OwnerClientId == 0 ? (-2,1,0) : (2,1,0)` directly in `PlayerMovement`; that literal
  is now only a fallback used if no `PlayerSpawnPoints` exists in the scene, so the project still
  works if that GameObject is ever deleted).
  `PlayerSpawnPoints` (`Assets/Scripts/PlayerSpawnPoints.cs`) is a plain (non-networked)
  `MonoBehaviour` living on the scene's `SpawnPoints` GameObject. It holds a `Transform[]`
  (Inspector-editable, or auto-collected from its own children if left empty) and maps
  `OwnerClientId % spawnPoints.Length` to a position — i.e. **spawn points are ordinary empty
  GameObjects you drag around in the scene**, not code literals. `PlayerSpawnPoints.MaxPlayers`
  (currently `4`) is the single source of truth for the player cap: `SteamLobbyManager.MaxPlayers`
  references it directly so the Steam lobby's `cMaxMembers` and the spawn-point cap can never
  drift apart. There is no NGO-side connection-approval enforcement of this cap — Steam itself
  refuses a 5th lobby member once `CreateLobby`'s member limit is reached, which is sufficient for
  this prototype's scope.
  **Caveat:** clientId-based modulo indexing assumes clientIds stay low and contiguous (true for
  a session with no disconnect/reconnect churn, which matches this prototype's scope — see
  Non-goals). It is not a robust "first come, first served" seat assignment for a long-lived
  session with players leaving and rejoining.
- **`FriendsOnly` lobby visibility** (`SteamLobbyManager.LobbyVisibility`, a `const`). Chosen over
  `Public` to avoid the (admittedly minor, since nobody searches) risk of AppID-480 lobby
  namespace collisions with other developers' test sessions. Trade-off: the two test Steam
  accounts must already be Steam friends, or `JoinLobby` fails with
  `k_EChatRoomEnterResponseNotAllowed` or similar. This is a single `const` — change it to
  `ELobbyType.k_ELobbyTypePublic` if that's a blocker.
- **IMGUI (`OnGUI`) for the debug UI**, not a Canvas/uGUI hierarchy. Zero scene wiring, no
  Inspector references that can silently break, ~95 lines. This was an explicit trade-off
  approved by the user in favor of "smallest possible" over "looks nice."
- **Static overhead camera**, not a networked follow-camera. The spec explicitly said this was
  acceptable and a follow-camera adds complexity with no networking value in a 2-capsule
  prototype.
- **`steam_appid.txt` at the project root** (contents: exactly `480`, 3 bytes, no BOM) so
  Steamworks.NET initializes correctly when running **inside the Unity Editor** (Editor Play mode
  reads `steam_appid.txt` from the project root, not from a build output folder).
  `Assets/Editor/SteamAppIdPostBuild.cs` is an `IPostprocessBuildWithReport` that copies the same
  3-byte file next to the built `.exe` for `StandaloneWindows`/`StandaloneWindows64` targets, so
  standalone builds never need to be launched through Steam itself
  (`SteamAPI_RestartAppIfNecessary` is intentionally not called).

## Verification performed (this is not just "should work")

All of the following were actually run and observed, not assumed:

1. **Compile check** via the Unity CLI (`unity command recompile` / `recompile_status`) after
   every script was added — zero errors on first pass for all four scripts once the
   nonexistent-`DllCheck.Test()` line was caught and removed pre-emptively.
2. **In-Editor Play-mode smoke test**: `SteamBootstrap` logged a real Steam init
   (`Steam initialized. User: <real display name>, SteamID: <real SteamID64>`) using the actual
   Steam client running on the dev machine.
3. **Host flow smoke test** (via `unity command eval` calling `SteamLobbyManager.Instance.HostGame()`
   directly): a real Steam lobby was created (`LobbyID=109775245112709037`), `NetworkManager.StartHost()`
   returned `true`, `OnClientConnectedCallback` fired for client 0, and the spawned
   `Player(Clone)` GameObject's `transform.position` was confirmed at exactly `(-2, 1, 0)` — the
   host/owner-0 spawn offset (this was before the `PlayerSpawnPoints` registry existed; see item
   6 below for the re-test after that change).
4. **Windows build**: built via the live-Editor `build`/`build_status` pipeline commands (not a
   second batch-mode Editor instance, which would have deadlocked on the project file lock).
   Result: 0 errors, 1 expected/harmless warning about the dev-only Pipeline package, output at
   `Builds/TGNS.exe` (~117 MB total).
5. **Standalone launch test**: `Builds/TGNS.exe` was launched directly (not through Steam, not
   through the Editor) and its `Player.log`
   (`%USERPROFILE%\AppData\LocalLow\DefaultCompany\TGNS\Player.log`) showed a clean
   `[SteamBootstrap] Steam initialized...` line — confirming `steam_appid.txt` placement actually
   works for a real standalone build, not just in-Editor.
6. **Spawn-point registry re-test** (same `eval`-driven flow, after replacing the hardcoded
   offset with `PlayerSpawnPoints`): with `SpawnPoint_0..3` placed at `(-2,1,-2)`, `(2,1,-2)`,
   `(-2,1,2)`, `(2,1,2)`, hosting spawned the player capsule at exactly `(-2.00, 1.00, -2.00)` —
   `SpawnPoint_0`'s position, confirming the `OwnerClientId % spawnPoints.Length` lookup works and
   no `[PlayerSpawnPoints]`/`[PlayerMovement]` fallback warnings were logged (i.e. the registry
   was found and used, not the hardcoded fallback).

What was **not** verified in this session (needs a second physical/virtual PC): the actual
cross-machine JOIN flow, i.e. PC B resolving the lobby owner's SteamID and successfully connecting
over `SteamNetworkingSockets` to PC A across the real Internet. The code path is implemented and
the API calls are confirmed correct against the installed packages, but end-to-end two-PC
connectivity has not been physically observed.

## Note: the scene also contains unrelated environment art

`SampleScene.unity` currently has a large set of decorative root objects (`Plants`, `Rocks`,
`Trees`, `Background Trees`, `Decals`, `Particles`, `Bushes`, `Terrain`, `Water`, `Props`,
`Mushrooms`, `Flowers`) alongside the networking objects this document describes
(`NetworkManager`, `SteamNetwork`, `SpawnPoints`, `Floor`, `Main Camera`, `Directional Light`).
That art was added directly in the Editor by the project owner, separately from and unrelated to
the networking prototype work — it is not part of the multiplayer implementation and this
document does not cover it. Don't assume it was placed there by mistake or "clean it up" as part
of unrelated networking work; if it's ever in the way of something networking-related (e.g. it
occludes the camera or a spawn point), ask before touching it.

## Explicit non-goals (do not add without the user asking)

Per the original spec, this prototype intentionally excludes: inventory, enemies, health,
jumping, networked physics, client-side prediction/reconciliation, animation, voice chat, a
matchmaking/lobby browser, dedicated servers, production-quality UI, and scene transitions. If
asked to extend this project, treat "keep it minimal" as a standing constraint unless the user
says otherwise.

## Troubleshooting notes for future agents

- **Unity CLI `unity command <name> -- <args>` argument syntax**: positional arguments after `--`,
  in the exact order the command's `parameters` schema lists them (check with
  `unity command --json` and grep for the command name). `key=value` style does **not** work.
  Under Git Bash on Windows, set `MSYS_NO_PATHCONV=1` before any command containing a
  leading-slash hierarchy path (e.g. `/Player`) — otherwise MSYS silently mangles it into a
  Windows path and the call fails with a confusing "Empty object reference" error.
- **`set_serialized_field` / `get_serialized_fields` on a `GameObject` target**: you must pass the
  `component` argument (component type name) — pointing `target` straight at a GameObject without
  it fails. For a nested serialized field (e.g. `NetworkManager.NetworkConfig.NetworkTransport`),
  address it by dotted SerializedProperty path (`NetworkConfig.NetworkTransport`), not by trying
  to set the parent `NetworkConfig` object directly (it's a plain C# class, reported as
  `propertyType: "Generic"` / `"unsupported"` when read as a whole).
- **Object references in `set_serialized_field`**: pass either a bare `instanceId` (int, for a
  scene object/component) or an asset path string (for a project asset like a `.prefab`) — not a
  JSON object like `{"target": ..., "component": ...}`.
- **If the Editor won't connect to the Pipeline server** (`unity status` /
  `unity pipeline list` shows `isReachable: false` for a long time): the Editor process may be
  hung/windowless rather than genuinely busy. Check `Get-Process Unity` for a process with no
  `MainWindowTitle` and a stale `Editor.log` (`%LOCALAPPDATA%\Unity\Editor\Editor.log`); if so,
  kill it and reopen via `unity open <projectPath>` rather than waiting indefinitely.
- **`unity build` (the standalone CLI subcommand) vs. `unity command build` (via the live
  Editor)**: prefer the latter whenever an Editor is already open on the project — `unity build`
  spawns a *second* batch-mode Editor process, which will contend for the project's file lock
  with the already-open interactive Editor and can hang or fail.
