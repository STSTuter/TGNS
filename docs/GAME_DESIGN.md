# Game design

Last updated: 2026-09-11. This document owns intended player experience and design decisions. Implementation evidence lives in [Architecture](ARCHITECTURE.md).

## Confirmed direction

**A co-op trading and exploration game using a caravan.** Confirmed by the project owner on 2026-09-11.

Players cooperate; trading, exploring, and the caravan are central to the concept. Perspective, tone, session length, progression, setting, combat's role, and how players control the caravan are not yet settled. The current four-player lobby cap is an implementation limit, not a confirmed final party size.

## Candidate core loop — proposed, not accepted

Prepare cargo and choose a destination together → travel with the caravan → discover a place, opportunity, or complication → trade and decide what to carry next → plan the next journey.

This is a starting point for discussion. It does not commit the game to survival meters, combat encounters, procedural worlds, real-time driving, permanent progression, or a simulated economy.

## Questions that shape the first playable loop

1. **Cooperation:** is there one shared caravan, and what can each player do while someone else drives or trades? Look for shared decisions and useful actions without making one player the permanent passenger.
2. **Trading:** what makes choosing cargo interesting: price information, limited capacity, uncertain routes, local demand, or relationships? Start with one source of tension.
3. **Exploration:** what can players discover that changes their route or trade plan, rather than merely revealing scenery?
4. **Tone and stakes:** is travel relaxed, tense, or dangerous? What happens after a bad trade or failed journey? Existing melee code does not answer this.
5. **Scope:** what should one short session accomplish, and what (if anything) persists between sessions?

Resolve only the questions needed for the next experiment. Record further ideas as proposals instead of quietly expanding the baseline.

## Suggested first experiment — proposed, not scheduled

Test whether choosing cargo and a route together is enjoyable. Use two trading stops, two goods, a limited cargo capacity, and one optional detour with a discoverable trade opportunity. These quantities are test scaffolding, not accepted game rules.

Before implementation, choose the minimum caravan interaction needed for this test and one useful contribution for each player. A marker or simple moving object may answer the question before vehicle physics is justified.

Observe whether both players contribute to a cargo or route decision, whether the discovery changes their plan, and whether either player spends the trip waiting. Keep or revise the mechanic based on observed behavior and player feedback; do not treat an agent's opinion as a playtest.

## Decision record

| ID / date | Status | Decision | Basis / consequence |
|---|---|---|---|
| D-001 / 2026-09-11 | Accepted | Co-op trading and exploration using a caravan | Explicit project-owner direction. Evaluate proposed mechanics against this experience. |

For a new decision, record what was chosen, why, and a link to its mechanic brief or evidence if one exists. If it changes later, mark it superseded and link the replacement; preserve the reason without carrying obsolete instructions into the current brief.

## Mechanic register

No detailed mechanic briefs are accepted yet. Trading, caravan interaction, and exploration are areas to design; prototype combat and locomotion are implementation context, not approved gameplay specifications.

Use the [mechanic brief](WORKFLOW.md#mechanic-brief) when saving an idea. Track **design status** (`proposed`, `accepted`, `deferred`, `rejected`, `superseded`) separately from **delivery status** (`not started`, `in progress`, `implemented`) and dated verification evidence.
