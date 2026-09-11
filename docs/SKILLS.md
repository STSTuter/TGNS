# Game design and development skills

Shortlist checked on 2026-09-11. These are recommendations based on the published skill files and this project's needs, not claims that the external skills have been run or benchmarked here. No external skill or plugin was installed during the documentation pass.

## Recommended starting point

Use [AGENTS.md](../AGENTS.md) for stable project rules and [Game design](GAME_DESIGN.md) for the caravan concept and decisions. Load specialized skills for the current task. Keeping agreed context accurate is more useful than repeatedly pasting the project history or installing overlapping skill packs.

Codex supports repository skills in `.agents/skills/`, and loads project guidance from `AGENTS.md`. Skills provide reusable procedures; they do not replace project facts or test evidence. See official [skill documentation](https://learn.chatgpt.com/docs/build-skills) and [AGENTS.md documentation](https://learn.chatgpt.com/docs/agent-configuration/agents-md).

## External options

| Source / skill | Best fit here | Qualification |
|---|---|---|
| [Unity official agent plugin](https://github.com/Unity-Technologies/unity-agent-plugin) | Unity development guidance; selected CLI, package management, physics, UI, audio, and navigation skills | Unity-maintained, targets Unity 6+, skills only; it does not itself provide an Editor MCP connection. Evaluate task-relevant skills rather than using every subsystem. |
| [prototype-fast](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/blob/main/skills/workflows/prototype-fast/SKILL.md) | Test whether a cargo choice, discovery, or caravan interaction is enjoyable with a small greybox experiment | Community skill; useful early. Its generic timebox should be adapted to the agreed test, not treated as a guarantee. |
| [level-design](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/blob/main/skills/disciplines/level-design/SKILL.md) | Route layouts, landmarks, detours, and encounter pacing | Community skill; a strong candidate once the journey experience is clearer. |
| [game-feel](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/blob/main/skills/disciplines/game-feel/SKILL.md) | Feedback for movement and interactions once the mechanic works | Focuses on audiovisual response and polish; it is not a complete economy or co-op design method. |
| [Unity skills collection](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/main/skills/unity) | C# lifecycle, input, animation, ScriptableObjects, physics, navigation, builds | Community collection states a Unity 6000.3 baseline; this project uses 6000.6. Check installed APIs before applying examples. |

The community [full catalog](https://github.com/gamedev-skills/awesome-gamedev-agent-skills) also covers cameras, AI, procedural generation, saving, UI, audio, and genre workflows. Those become useful when a chosen mechanic requires them. Avoid letting an RPG or survival template silently define this game's features.

My suggested order is **prototype-fast**, then **level-design**, with selected official Unity skills for implementation. Add game-feel after the trading/travel interaction is worth refining. A Steam/NGO-specific workflow will still need project context: Unity Services-oriented live-game or multiplayer skills are not a drop-in match for this project's Steam backend. The official [live-game skill](https://github.com/Unity-Technologies/unity-agent-plugin/blob/main/skills/build-live-game/SKILL.md) is explicitly oriented around Unity Gaming Services.

The OpenAI curated list was queried using the skill-installer helper. It did not contain a dedicated Unity/game-design skill at this check; this is separate from external repositories and already available local skills. The [curated directory](https://github.com/openai/skills/tree/main/skills/.curated) changes over time.

## Useful skills already available in this session

- **imagegen:** concept art, caravan silhouettes, settlement mood, and visual variants. A concept image is not automatically a production-ready game asset.
- **visualize:** route/trade models and small interactive explanations for comparing mechanics.
- **spreadsheets:** price, capacity, and journey-cost experiments when designing an economy; **presentations/documents** when a shareable pitch or formal brief is needed.
- **skill-creator:** capture a repeated design or verification procedure as a focused custom skill once the workflow has proven useful.

These are session capabilities, not repository dependencies; another agent may have a different installed set.

## Adding skills deliberately

Before installation, inspect the selected `SKILL.md`, its referenced files/scripts, license, engine assumptions, required tools, and whether it conflicts with project scope. Keep dependencies/references together and record the source revision if adopting a community skill. Prefer a small selection with clear triggers over overlapping general-purpose routers.

For example, ask an agent to install the exact `prototype-fast` skill from the linked repository after reviewing its references, or to set up the official Unity plugin. This shortlist is discovery, not installation authorization.

A future custom skill could be `co-op-mechanic-design`: turn an idea into a player decision, contribution for each player, explicit assumptions, a small experiment, and a saved decision. Keep the live caravan brief in `GAME_DESIGN.md` and link to it; do not embed another copy in the skill. The current [workflow](WORKFLOW.md) already provides that process without needing an installation.
