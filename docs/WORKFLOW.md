# Discussing and developing the game

Use plain language. The agent should translate a rough idea into player choices, concrete rules, and an experiment. You do not need to know component names or provide a large prompt.

## A useful conversation

1. Describe the desired experience or problem: “I want trading to feel like a group adventure” or “the passenger has nothing to do.” Reference another game only for the specific quality you mean.
2. Explore two or three options. Ask how they change player decisions, cooperation, journey pacing, and implementation effort. Choose a direction or continue exploring.
3. Save a short mechanic brief when the idea is worth retaining. Accepting the design and asking to implement it can happen in the same message; no extra ceremony is required.
4. Build the smallest playable test of the uncertain part. Agree what observations would support keeping, changing, or dropping it.
5. Record what happened, update the decision, and extend only the parts that earned their complexity.

For idea discussions, start from [Game design](GAME_DESIGN.md). Inspect code when evaluating feasibility or reusing an existing system. For implementation, also read the relevant [Architecture](ARCHITECTURE.md) and [Testing](TESTING.md) sections. Avoid loading unrelated vendor assets or all skill manuals.

## Prompts you can reuse

**Explore:** “Let's explore caravan trading. I want both players involved in deciding what to buy. Give me three approaches, their tradeoffs, and your recommendation. Discussion only.”

**Critique:** “Challenge this idea against our game direction. Where does it create meaningful cooperation, waiting, or a dominant strategy? Suggest one cheap way to test the largest uncertainty.”

**Specify:** “Turn the approach we chose into a proposed mechanic brief. Separate agreed rules from assumptions and open questions. Include one example journey.”

**Prototype:** “Implement the smallest prototype of this mechanic. Use the existing Steam/NGO stack, define who owns shared state, and tell me how to test it with two players.”

**Playtest:** “Here is what happened in our session: [observations]. Separate bugs, unclear feedback, and design problems. Recommend the next single change to test.”

These are examples, not required commands. Normal conversation should work too.

## Mechanic brief

Keep simple proposals under roughly one page. Save a larger one as `docs/mechanics/<short-name>.md` and link it from the game design register only when useful.

```markdown
# <Mechanic name>
Design status: proposed | accepted | deferred | rejected | superseded
Delivery status: not started | in progress | implemented
Updated: YYYY-MM-DD

## Player experience
What should feel enjoyable, tense, or satisfying? How does this serve caravan trade/exploration?

## Rules and example
Trigger → player actions and meaningful choice → cost/risk → outcome and feedback.
One concrete journey or interaction. Label invented tuning numbers as proposed.

## Cooperation
What can each player contribute? Who decides? What happens during disagreement or waiting?

## Multiplayer behavior
Who requests/validates/writes shared state? Can two players act on the same cargo or funds?
What should late joiners and disconnecting players see? Mark technical choices as open if undecided.

## Smallest experiment
One hypothesis; minimum content; scope exclusions; observations that mean keep/change/drop.
Separate functional acceptance checks from evidence that the mechanic is enjoyable.

## Open questions and evidence
Only unresolved questions that matter next. Dated playtest observations, decisions, and links.
```

For trading, check concurrent purchases, shared versus individual money/cargo, and whether the authority validates prices, funds, capacity, and stock. These are questions to resolve when implementing trading, not a prescribed economy.

## Short handoff

Use this when work moves to another task or agent. Prefer links over copying whole docs.

```text
Goal / current request:
Accepted decisions and relevant brief:
What changed / working-tree state:
Checks actually run and result:
Remaining uncertainty or blocker:
Next concrete step:
Relevant file paths:
```

Update canonical docs when facts or decisions change. Do not create a new permanent status file for every chat. When an agent cannot access this repository, supply the game design and relevant mechanic brief along with the handoff; a file path alone cannot provide context to that agent.
