# Architecture

Source/asset inspection: 2026-09-11. This describes the working tree, including unfinished changes; it is not a runtime verification report. Game direction and design decisions live in [Game design](GAME_DESIGN.md), validation in [Testing](TESTING.md).

## Stack and sources of truth

| Layer | Current configuration | Source |
|---|---|---|
| Editor | Unity 6000.6.0f1 | `ProjectSettings/ProjectVersion.txt` |
| Netcode | Netcode for GameObjects 2.13.2 | `Packages/manifest.json` |
| Steam API | Steamworks.NET git dependency; resolved hash `ba71581f1ed7349e8d0f17ddc6f135dd3bc8a6a3` | `Packages/packages-lock.json` |
| Transport | Community SteamNetworkingSockets; resolved hash `0fab638470379ace12b0149dfb41c043d23dbce5` | `Packages/packages-lock.json` |
| Input / rendering | Input System 1.20.0 / URP 17.7.0 | `Packages/manifest.json` |
| Editor automation | Pipeline 0.6.0-exp.1 | `Packages/manifest.json` |

Git dependencies are requested by repository URL without a commit fragment; the lockfile records their resolved commits. Do not describe them as immutable manifest pins. Consult installed `Library/PackageCache` sources when working with package APIs, particularly the NGO transport interface and animation authority configuration.

## Connection flow

1. `SteamBootstrap` initializes Steam, checks packing, warms relay access, pumps callbacks, and shuts down the API.
2. `SteamLobbyManager.HostGame()` creates a friends-only Steam lobby with `PlayerSpawnPoints.MaxPlayers` members, stores host SteamID metadata, and starts NGO as host.
3. A joining player enters a lobby ID. `JoinLobby` resolves asynchronously; `GetLobbyOwner` supplies the SteamID assigned to `SteamNetworkingSocketsTransport.ConnectToSteamID` before NGO starts as a client.
4. NGO spawns its configured player prefab and exchanges game state through the Steam transport. The host is also the NGO server; there is no dedicated server or Unity Relay/Lobby service in this implementation.
5. Disconnect shuts down NGO and leaves the Steam lobby. Reconnect recovery and host migration are not implemented by this glue.

`PlayerSpawnPoints.MaxPlayers` is currently **4** and also feeds the Steam lobby member limit. Spawn positions use client ID modulo the configured spawn-point count. This is not robust seat assignment for repeated departures/rejoins, and the lobby cap is not an NGO connection-approval rule.

## Runtime and authoring map

| Area | Files | Responsibility |
|---|---|---|
| Session and diagnostics | `Assets/Scripts/SteamBootstrap.cs`, `SteamLobbyManager.cs`, `DebugUI.cs` | Steam lifecycle, host/join/disconnect, IMGUI debug controls |
| Spawn positions | `Assets/Scripts/PlayerSpawnPoints.cs` | Inspector-assigned points or direct-child collection |
| Active scene/player | `Assets/Scenes/SampleScene.unity`, `Assets/Prefabs/NetworkPlayer.prefab` | Scene's NGO player reference resolves to the humanoid prefab |
| Ownership migration | `Assets/Scripts/NetworkPlayerOwnership.cs`, `Assets/Scripts/Editor/OrbitControllerSetup.cs` | Owner-only vendor input/motor/camera stack; migration helper |
| Locomotion/animation | `Assets/Scripts/NetworkPlayerAnimationDriver.cs`, `HumanoidHeadLook.cs` | Motor-driven animation parameters and head look |
| Appearance | `Assets/Scripts/Character/CharacterParts.cs`, `MultiplayerParts.cs` | Modular appearance and network synchronization |
| Combat | `Assets/Scripts/Combat/PlayerCombat.cs`, `MeleeWeapon.cs`, `Health.cs` | Attack requests, server hit detection, replicated health |
| Prefab/animation authoring | `Assets/Scripts/Editor/` | Menu-driven player, animator, combat, and test-dummy setup |
| Build support | `Assets/Editor/SteamAppIdPostBuild.cs` | Writes AppID file beside Windows build |
| Legacy baseline | `Assets/PlayerMovement.cs`, `Assets/Prefabs/Player.prefab` | Earlier capsule movement; not the scene's current NGO player prefab |

Paths in cells after the first are relative to the first file's directory. Imported John Stairs controllers and Synty assets are dependencies of the current character/environment work. Preserve their assets and GUIDs; prefer project-owned integration components. Environment art in the scene is intentional project content.

## Authority model

- The current humanoid prefab serializes owner authority for `NetworkTransform` and `NetworkAnimator`. The ownership integration enables the local input, controller, motor, and camera only for the owner; remote replicas receive network state.
- `NetworkPlayerAnimationDriver` writes locomotion parameters for the owner and sends jump triggers through `NetworkAnimator`. Its `Cast` and `FinishCast` methods are no-ops, so the older combat path's animation calls do not establish working melee visuals.
- `MultiplayerParts` submits owner-selected appearance to the server and replicates the appearance through a network variable. This does not provide persistent character storage.
- `PlayerCombat` reads owner attack input and requests animation/hitbox changes through RPCs. Attack timing/cooldown is checked locally. `MeleeWeapon` applies damage on the server; `Health` uses a server-write network variable. This combination is not proof of fully server-validated combat.
- Authority for future cargo, prices, money, caravan control, and discovered locations has not been designed. Define it per mechanic rather than inheriting movement's owner authority for shared economic state.

## Current integration gaps

These are inspection findings, not a request to repair unrelated work during documentation maintenance.

1. **Controller migration needs runtime verification.** `SampleScene` references `NetworkPlayer.prefab` (GUID `f0991c084c801dc4294cd617f48e6b9f`). During this documentation pass the prefab changed from the first-person stack to serialized `NetworkPlayerOwnership` and `PlayerCamera`; the latest inspected asset no longer contains the old controller class name. The ownership script and its `.meta` are untracked, the prefab/setup helper are modified, and the old controller sources are staged for deletion. This is ongoing working-tree work. Inspect the current Editor state and references, then verify local/remote control and one owner camera/audio listener before treating the migration as tested.
2. **Combat/animation integration needs a playtest.** `PlayerCombat` calls `IAnimationHandler.Cast/FinishCast`, which the project locomotion driver currently leaves empty. The presence of melee scripts is not evidence of visible attack animation or a complete combat loop.
3. **Current multiplayer evidence is missing.** The original README says two-PC tests passed; the original architecture report says the cross-machine join was not observed. Neither establishes current humanoid/controller behavior. See the verification ledger in [Testing](TESTING.md).

## Scope and historical rationale

The prototype uses Steam AppID 480, owner-authoritative movement, a friends-only lobby, and debug UI as implementation choices. These are a starting point for the caravan game, not a complete shipping architecture. Trading, caravan gameplay, and an exploration loop have not been established by the inspected project-owned scripts.

The old capsule-only feature exclusions are superseded by the game direction and explicit user requests. Do not infer an inventory, combat, persistence, or backend roadmap from those exclusions or from imported assets.

The [archived baseline](archive/NETWORKING_BASELINE.md) preserves both original documents, including the installed-source API investigation, transport compatibility checklist, original test claims, and CLI troubleshooting. Read it only when that history is needed; re-verify version-sensitive advice against installed sources.
