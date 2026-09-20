# Physics carry (pick up, carry, load)
Design status: accepted
Delivery status: in progress
Updated: 2026-09-20

The first piece of the physics-based caravan handling the project owner asked for on 2026-09-20: pick an
object up in first person, feel its weight, and put it in the carriage. Moving the carriage and fixing the
player/carriage collision are separate, later steps — see [Out of scope](#out-of-scope).

## Player experience

Cargo should feel like an object, not an inventory row. Picking up a sack is instant and weightless;
wrestling a 90 kg crate across camp and dropping it into the carriage should be a small physical effort you
can see going wrong. Loading the caravan then becomes something players do with their hands and argue about,
rather than a menu one player operates while the others wait.

The intended read, at a glance, without any UI:

| Object mass vs. the player's strength | What the player sees |
|---|---|
| Well under lift capacity | Snaps to the crosshair, swings freely, throws well |
| Near lift capacity | Lifts, but lags behind the aim point and wallows when you turn |
| Above lift capacity | Will not rise; hangs low and is dragged along the ground, player slows right down |
| Above the drag limit | Refuses the grab entirely |

## Rules and example

Aim at a grabbable prop within **3.5 m** and press **E**. The server checks reach and mass against the
player's strength; on success the object is held in front of the camera by a force, not by parenting.
**E** drops it, **left mouse** throws it, **scroll** moves it nearer or further, **R + mouse** turns it in
your hands.

Every behaviour above comes from one cap. The hold applies the force needed to move the object to the hold
point *plus* the force needed to carry its weight, then clamps the total to the player's force budget. An
object whose dead weight already exceeds the budget therefore cannot be raised at all.

Proposed tuning, on the player prefab (`PlayerStrength`), all invented and expected to change:

| Value | Default | Meaning |
|---|---|---|
| Lift capacity | 40 kg | Heaviest object that can be held up against gravity |
| Drag multiplier | 3 | Grab limit is 120 kg; between 40 and 120 kg the object drags |
| Force headroom | 2.5 | Force available on top of a 40 kg object's dead weight |
| Throw budget | 0.6 | 24 kg·m/s, so a 5 kg crate throws at ~4.8 m/s and a 40 kg one barely leaves the hands |
| Movement penalty | ×0.35 at 120 kg | Carrying speed scales linearly with carried mass |

**Example.** Two players load the carriage. One shuttles 5 kg sacks at a jog. The other needs both the slow
walk and a clear run-up for the 90 kg crate, drags it up to the tailboard, and has to aim the drop. The
250 kg block on the ground refuses every grab, which is the game saying *this one needs two of you, or the
carriage brought to it* — an open design question, not a solved one.

## Cooperation

Today: any player can carry any object they are strong enough for, and only one player can hold a given
object at a time. That already creates the useful co-op shape of "you fetch, I stack".

Unresolved and worth designing next: whether two players can lift one object together, whether strength is
a per-character stat players choose between, and what the players who are not carrying do while loading.

## Multiplayer behavior

- **Requests** come from the carrying client. **Validation** is the server's: it re-checks reach against its
  own copy of both transforms and re-checks mass against the player's server-held strength, so a client
  cannot grab through a wall or lift an anvil by asking nicely.
- **Writes** follow the project's existing owner-authoritative movement model. At rest a prop is owned by
  the server. On an accepted grab the server hands the prop's `NetworkObject` to the grabbing client, which
  simulates it locally — that is what removes the round trip from the carry. On release ownership returns to
  the server, which also applies the throw velocity, because the releasing client no longer has authority.
- **Two players on one object** cannot happen: the holder is a server-written network variable, and the
  second request is refused with "someone else is carrying that".
- **Late joiners** receive the holder variable and the replicated transform, so a prop already in someone's
  hands arrives in their hands.
- **Disconnecting players** drop their cargo where it is. The prop's `NetworkObject` must have
  *Don't Destroy With Owner* enabled or it would be destroyed with the carrier; the setup menu sets it and
  `Grabbable` warns at runtime if it is missing.

This is deliberately not server-authoritative carrying. It is the right trade for a four-player friends
lobby where the host is a player, and it is the wrong trade if cargo ever becomes worth cheating for.
Revisit it when trading has real value attached.

## Smallest experiment

**Hypothesis:** weight that the player can feel makes loading the carriage worth doing by hand, instead of
feeling like a slow inventory screen.

Minimum content: the four test props next to the carriage (5, 35, 90, 250 kg) and two players in one lobby.

Functional acceptance — separate from whether it is enjoyable:

1. Each player can pick up, carry, drop and throw the 5 kg and 35 kg props.
2. The 90 kg prop cannot be raised, can be dragged, and visibly slows the carrier.
3. The 250 kg prop refuses the grab with feedback.
4. A second player cannot grab a prop the first is holding.
5. Both players see the same prop position while it is carried, and where it lands after a drop.
6. A prop dropped by a disconnecting player stays in the world.

Evidence that it is enjoyable is separate and comes from watching a loading session: do players hand objects
to each other, complain about weight in a good way, and choose what to carry? Or do they route around the
mechanic and ask for a button?

## Out of scope

Deliberately not part of this step, all still open:

- **Player/carriage collision.** Bumping the carriage launches it. The likely cause is the player's
  `CharacterController` versus the carriage's 1000 kg wheel-collider rigidbody, which PhysX resolves by
  depenetration rather than contact. Needs its own pass.
- **Moving the carriage** and cargo that stays put while it moves. A prop resting in the bed of a moving
  carriage is not yet attached to it in any way.
- **The carriage is not a `NetworkObject`.** It is local physics only, so nothing about it is replicated.
- **Cargo capacity, value, or any trade meaning.** These props are weight and nothing else.
- **Two-player lifts** and strength as a character stat players invest in.

## Open questions and evidence

1. Is refusing heavy objects outright the right answer, or should every object be draggable and only lifting
   be gated? The current split is a guess.
2. Should carrying block sprinting and jumping outright rather than only scaling speed?
3. Does the 30 Hz tick make a carried prop look acceptable on the *other* player's screen? Untested.

| Date | Evidence | Result |
|---|---|---|
| 2026-09-20 | Headless Editor runs; see the ledger in [Testing](../TESTING.md#verification-ledger) | Compiles, prefab and scene wiring applied. **No Play Mode or multiplayer test run** — nothing above is playtested. |
