# Development and testing

This document owns setup/build steps and verification evidence. Commands inherited from the original notes are labeled below; they were not re-run during the documentation pass on 2026-09-11.

## Open the project

1. Use the Unity version in `ProjectSettings/ProjectVersion.txt` (currently 6000.6.0f1). Let Unity resolve `Packages/manifest.json` with `Packages/packages-lock.json`.
2. Open `Assets/Scenes/SampleScene.unity`, the enabled scene in `ProjectSettings/EditorBuildSettings.asset`.
3. Check the Console for compile/import errors and inspect the scene's NGO player prefab for missing scripts. Verify the [controller migration](ARCHITECTURE.md#current-integration-gaps) before claiming a playable build.
4. For Steam tests, run the Steam client and sign in. The root `steam_appid.txt` contains the prototype AppID 480.

Editor migration helpers under `Assets/Scripts/Editor/` modify assets. Read the relevant helper and review existing changes before invoking it. Applying all helpers is not a routine onboarding step.

## Windows build

The project includes `com.unity.pipeline` and has historically used the Unity CLI against a live Editor. Check the installed CLI's help and command schema first:

```powershell
unity --help
unity command --json
```

The following build commands are preserved from the original workflow; confirm their positional parameters against the installed schema before execution:

```powershell
unity command build -- StandaloneWindows64 Builds/TGNS.exe Windows '' '' true false
unity command build_status
```

Use the live Editor build route when this project is already open. A second batch Editor can contend for the same project lock. Alternatively, use the Editor's Windows build workflow with `SampleScene` included and output to `Builds/TGNS.exe`.

The post-build hook `Assets/Editor/SteamAppIdPostBuild.cs` writes `steam_appid.txt` beside the executable. Check that it exists. `Builds/` is ignored by Git. The old report noted a Pipeline configuration warning in player builds; inspect actual output before attributing any new warning to that historical message.

## Checks appropriate to the change

| Change | Useful validation |
|---|---|
| Documentation only | Local links/paths, contradictory statements, diff review; no game build needed |
| C# gameplay logic | Unity compilation plus focused EditMode/PlayMode tests where they check meaningful behavior |
| Controller, camera, prefab, animation | Inspect missing scripts/references, Play Mode owner controls and remote replicas, relevant two-PC steps |
| Networked state, lobby, interaction | Host/client test with matching builds; test both players as initiator, simultaneous requests, relevant late-join/disconnect cases |
| Tuning or game feel | Functional checks plus an observed playtest against the mechanic's hypothesis |

The Unity Test Framework dependency is installed. No project-owned automated test suite was identified in this inspection. Do not report tests as passed simply because the package exists. Use the Editor Test Runner for tests added or found for the specific change.

## Two-PC smoke test

Preconditions: same build on two Windows PCs; separate Steam accounts that are friends; Steam running on both. Record the build/revision and any uncommitted asset changes.

1. PC A: launch the game; confirm Steam initialized in the debug UI; choose **HOST**, then **COPY LOBBY ID**.
2. PC B: paste the shared ID and choose **JOIN**. Confirm the host sees two connections and both machines see both players; corroborate with logs rather than relying solely on the client's debug count.
3. On each machine, move the local player. Only its input should drive that player; the other player should move through replication.
4. Verify local camera/orbit behavior, cursor interaction, and one active gameplay camera/audio listener. Remote players must not take over local input or the camera.
5. Observe both characters' locomotion, jumps where supported, and appearance from each machine. Record any animation difference between owner and observer.
6. If combat is part of the change and wired in the test scene, attack a health-bearing target from each player. Check visible animation separately from server hit detection and replicated health.
7. Disconnect the client and check player/camera cleanup. End the host session and record client behavior. Do not assume automatic reconnection or host migration.
8. For changes involving shared state, separately check a late join and simultaneous actions. A two-player pass does not validate the four-player cap; test additional players when changing capacity.

Logs: Editor Console and `%LOCALAPPDATA%/Unity/Editor/Editor.log`; player logs under `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/Player.log`. Resolve company/product names from Player Settings. Record expected versus actual behavior and the relevant error excerpt.

## Verification ledger

| Date / scope | Evidence | Result / limitation |
|---|---|---|
| Original networking baseline; date/revision absent | [Original notes](archive/NETWORKING_BASELINE.md) | Historical local compile/host/build claims retained. README and architecture conflict on whether cross-machine joining was tested; unresolved. |
| 2026-09-11 documentation standardization | Read project-owned source, manifest/lockfile, scene/prefab references, existing Git status | Documentation inspection only. Observed the controller prefab migration land during the pass; runtime verification remains pending, along with the combat-animation integration gap. No Editor, build, or live multiplayer test performed. |

Add a row after a meaningful verification run with date, revision/build, scenario, observed result, and evidence location. Keep failures and incomplete checks visible. A result applies to the tested build and scenario, not every subsequent change.
