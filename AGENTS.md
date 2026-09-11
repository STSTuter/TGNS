# TGNS agent guide

## Read only what the task needs

Start with [README.md](README.md), then use this routing table. Do not scan all assets, package caches, or archived notes for routine onboarding.

| Task | Read next |
|---|---|
| Ideas, mechanics, game direction, prioritization | [Game design](docs/GAME_DESIGN.md), [Workflow](docs/WORKFLOW.md) |
| Implementation or debugging | [Architecture](docs/ARCHITECTURE.md), relevant scripts/assets, relevant [testing steps](docs/TESTING.md) |
| Build or multiplayer verification | [Testing](docs/TESTING.md), relevant architecture section |
| Docs maintenance | The affected canonical doc and its inbound links |
| Skill selection | [Skills shortlist](docs/SKILLS.md) |

## Discuss and decide

- Treat brainstorming and critique as discussion. Do not turn tentative ideas into accepted mechanics, a roadmap, or code changes. Explicit requests to implement, fix, or prototype authorize that work; do not request a second approval for the same scope.
- Ground recommendations in the confirmed game direction. Label assumptions and proposals. Existing combat code does not establish combat as a design pillar.
- Explain the player's action, choice, feedback, and reason to repeat before discussing architecture. Consider what every co-op player does, including players who are not driving or trading.
- Offer a small number of meaningfully different options, recommend one with a tradeoff, and identify the smallest useful playtest. Ask only questions that materially change the next step.
- Use the brief in `docs/WORKFLOW.md` when an idea warrants a saved proposal. Do not demand a complete design document for a small change.
- Record explicit design decisions in `docs/GAME_DESIGN.md` with date and rationale; never invent acceptance or playtest results. Larger mechanics can live under `docs/mechanics/` when needed, linked from the design doc.

## Implement and verify

- Inspect `git status --short` before edits. Preserve existing staged, unstaged, and untracked work. Do not revert unrelated assets or re-run prefab migration tools as part of a docs task.
- Inspect the scene's actual player-prefab reference and that prefab's serialized components. A setup script describes a possible migration, not proof of the current asset state.
- Use `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, and `Packages/packages-lock.json` for versions. Check installed package source for version-sensitive APIs; do not assume tutorial code matches this project.
- Preserve the Steam/NGO approach for ordinary changes. A different backend or authority model is a separate design choice when required by the requested feature.
- For networked mechanics, identify who may request an action, who validates it, who writes state, and what late joiners receive. Local responsiveness and authoritative outcomes are separate concerns.
- Prefer project-owned adapters under `Assets/Scripts/`. Preserve imported vendor assets and `.meta` GUIDs. Use existing Editor helpers or Unity serialization APIs for structural scene/prefab changes, inspect their scope first, and review the resulting asset diff.
- Use appropriate checks from `docs/TESTING.md`; report exactly what ran and what remains unverified. Compilation does not prove multiplayer or game feel. Documentation-only edits need link/content checks, not a game build.
- Keep changes focused. Historical capsule-only feature exclusions are superseded; add the features the user requests without unrelated systems.

## Keep context durable and small

Update the document that owns a changed fact, then link to it. Do not duplicate project state into separate agent memories or giant onboarding prompts. Read archived notes only when investigating their historical topic. End substantial work with the result, validation, unresolved issues, and next useful step; use the short handoff template when another task needs to continue.
