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
| Active scene/player | `Assets/Scenes/SampleScene.unity`, `Assets/Prefabs/Character.prefab` | Scene's `NetworkManager.PlayerPrefab` (GUID `b4ca84e0d54203d4586d2b3b28857e58`) |
| First person view | `Assets/Scripts/Player/FirstPersonCamera.cs`, `FirstPersonController.cs`, `Assets/Scripts/Editor/FirstPersonPlayerSetup.cs` | Owner-only look/input on top of the vendor `RPGMotor`; prefab conversion helper |
| Locomotion/animation | `Assets/Scripts/NetworkPlayerAnimationDriver.cs`, `HumanoidHeadLook.cs` | Motor-driven animation parameters and head look |
| Appearance | `Assets/Scripts/Character/CharacterParts.cs`, `MultiplayerParts.cs` | Modular appearance and network synchronization |
| Combat | `Assets/Scripts/Combat/PlayerCombat.cs`, `MeleeWeapon.cs`, `Health.cs` | Attack requests, server hit detection, replicated health |
| Physics carry | `Assets/Scripts/Interaction/PlayerCarry.cs`, `PlayerStrength.cs`, `Grabbable.cs`, `GrabbableNetworkTransform.cs`, `CarryHud.cs`, `Assets/Scripts/Editor/PhysicsGrabSetup.cs` | First person pick up/carry/throw driven by a strength-capped force, ownership transfer on grab, setup menus |
| Prefab/animation authoring | `Assets/Scripts/Editor/` | Menu-driven player, animator, combat, and test-dummy setup |
| Build support | `Assets/Editor/SteamAppIdPostBuild.cs` | Writes AppID file beside Windows build |
| Legacy baseline | `Assets/PlayerMovement.cs`, `Assets/Prefabs/Player.prefab` | Earlier capsule movement; not the scene's current NGO player prefab |

Paths in cells after the first are relative to the first file's directory. Imported John Stairs controllers and Synty assets are dependencies of the current character/environment work. Preserve their assets and GUIDs; prefer project-owned integration components. Environment art in the scene is intentional project content.

## Authority model

- The current humanoid prefab serializes owner authority for `NetworkTransform` and `NetworkAnimator`. The ownership integration enables the local input, controller, motor, and camera only for the owner; remote replicas receive network state.
- `NetworkPlayerAnimationDriver` writes locomotion parameters for the owner and sends jump triggers through `NetworkAnimator`. Its `Cast` and `FinishCast` methods are no-ops, so the older combat path's animation calls do not establish working melee visuals.
- `MultiplayerParts` submits owner-selected appearance to the server and replicates the appearance through a network variable. This does not provide persistent character storage.
- `PlayerCombat` reads owner attack input and requests animation/hitbox changes through RPCs. Attack timing/cooldown is checked locally. `MeleeWeapon` applies damage on the server; `Health` uses a server-write network variable. This combination is not proof of fully server-validated combat.
- The physics carry system requests a grab from the server, which re-checks reach against its own copy of
  both transforms and mass against the player's server-written `PlayerStrength`. On acceptance the server
  calls `NetworkObject.ChangeOwnership` so the grabbing client simulates the prop locally, and
  `RemoveOwnership` on release, which is also where the clamped throw velocity is applied. Props therefore
  use an owner-authoritative `GrabbableNetworkTransform` plus `NetworkRigidbody`, and are server-owned at
  rest. The holder is a server-written network variable, so a second player cannot take a held prop and
  late joiners see it in the carrier's hands. This is not proof against a cheating client that owns a prop.
- Authority for future cargo, prices, money, caravan control, and discovered locations has not been designed. Define it per mechanic rather than inheriting movement's owner authority for shared economic state.

## Current integration gaps

These are inspection findings, not a request to repair unrelated work during documentation maintenance. Parts of this document still name files from before the scene moved to `Character.prefab` (`NetworkPlayerOwnership.cs`, `NetworkPlayerAnimationDriver.cs`, `HumanoidHeadLook.cs`, `PlayerCamera`); those rows were not re-audited during the first person change and should be checked against the working tree before they are relied on.

1. **First person view needs a playtest.** `Character.prefab` no longer carries `RPGCamera`, `RPGViewFrustum` or `RPGController`; `FirstPersonCamera` and `FirstPersonController` replace them and both self-enable for the owner in `OnNetworkSpawn`. `RPGMotorMMO` is kept, so locomotion tuning and the animator parameters remote replicas play are unchanged, and its `AlignWithCamera`/`AlsoRotateCamera` are now `Never` because the camera owns body rotation. Mouse X is routed through `RPGMotor.SetRotation` rather than applied to the transform, because `StartMotor` resets `Turning Direction` at the top of its own update and any rotation applied outside that window never reaches the animator. Compilation and the prefab conversion are verified; owner/remote control, the single owner camera and audio listener, and the turning animation remote players see are not. See [Testing](TESTING.md).
2. **Combat/animation integration needs a playtest.** `PlayerCombat` calls `IAnimationHandler.Cast/FinishCast`, which the project locomotion driver currently leaves empty. The presence of melee scripts is not evidence of visible attack animation or a complete combat loop.
3. **The carriage is local physics only, and collides badly with the player.** `Assets/Prefabs/Carriage.prefab`
   is a 1000 kg `Rigidbody` with two `WheelCollider`s and `CarriageStabilizer`, and has no `NetworkObject`, so
   nothing about it replicates. The reported launch when a player walks into it is consistent with PhysX
   depenetrating the player's `CharacterController` against that body rather than resolving a contact; the
   `CharacterController` cannot receive or exchange impulses. This has not been reproduced or measured during
   this pass and is not addressed by the carry system.
4. **Current multiplayer evidence is missing.** The original README says two-PC tests passed; the original architecture report says the cross-machine join was not observed. Neither establishes current humanoid/controller behavior. See the verification ledger in [Testing](TESTING.md).

## Scope and historical rationale

The prototype uses Steam AppID 480, owner-authoritative movement, a friends-only lobby, and debug UI as implementation choices. These are a starting point for the caravan game, not a complete shipping architecture. Trading, caravan gameplay, and an exploration loop have not been established by the inspected project-owned scripts.

The old capsule-only feature exclusions are superseded by the game direction and explicit user requests. Do not infer an inventory, combat, persistence, or backend roadmap from those exclusions or from imported assets.

The [archived baseline](archive/NETWORKING_BASELINE.md) preserves both original documents, including the installed-source API investigation, transport compatibility checklist, original test claims, and CLI troubleshooting. Read it only when that history is needed; re-verify version-sensitive advice against installed sources.
