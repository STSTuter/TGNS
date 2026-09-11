# TGNS

A Unity multiplayer prototype for a **co-op trading and exploration game using a caravan**. The game direction is confirmed; the trading, exploration, and caravan mechanics still need design and implementation.

## Start here

| Need | Canonical document |
|---|---|
| Understand the game, discuss mechanics, see decisions | [Game design](docs/GAME_DESIGN.md) |
| Work with an agent, turn an idea into a small experiment | [Collaboration workflow](docs/WORKFLOW.md) |
| Agent instructions and task-specific reading | [AGENTS.md](AGENTS.md) |
| Understand implementation and current integration gaps | [Architecture](docs/ARCHITECTURE.md) |
| Open, build, and verify the game | [Development and testing](docs/TESTING.md) |
| Choose additional game development skills | [Skills shortlist](docs/SKILLS.md) |

## Current implementation

Unity **6000.6.0f1**, Netcode for GameObjects **2.13.2**, Steamworks.NET, and the community SteamNetworkingSockets transport. See [package sources](Packages/manifest.json) and [resolved dependencies](Packages/packages-lock.json).

Steam friends host or join by sharing a numeric lobby ID. The host also runs the NGO server; Steam provides lobby discovery/signalling and P2P transport. The configured lobby cap is four players. The project contains a humanoid network player, locomotion/animation integration, appearance synchronization, and prototype melee/health code.

**Current verification gap (source inspection, 2026-09-11):** the scene uses `NetworkPlayer.prefab`, which now references the new ownership component and `PlayerCamera`. The orbit-controller migration is present in the working tree, but runtime behavior has not been verified in this documentation pass. See [Architecture](docs/ARCHITECTURE.md).

## Open and play

1. Open this folder with the editor version in [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt), and let packages import.
2. Open `Assets/Scenes/SampleScene.unity`. Check compilation and the player prefab's controller wiring before Play Mode.
3. With Steam running, use the debug UI to host or join. Cross-machine testing needs separate Steam accounts that are friends and matching builds.

The detailed [build and playtest checklist](docs/TESTING.md) distinguishes source inspection, local testing, and actual two-PC verification. The old documents disagree about whether the original two-PC test passed; current multiplayer verification remains unconfirmed.

## Documentation conventions

Each fact has one home; other documents link to it. `GAME_DESIGN.md` owns intended player experience and design decisions. `ARCHITECTURE.md` owns implementation facts. `TESTING.md` owns commands and test evidence. `AGENTS.md` owns agent working rules. `WORKFLOW.md` owns discussion and handoff templates.

Keep proposals, accepted decisions, implementation, and verification distinct. Update the relevant document with behavior changes. The [original networking notes](docs/archive/NETWORKING_BASELINE.md) are preserved as historical evidence, outside the normal reading path.
