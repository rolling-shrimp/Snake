# Session Record — 2026-11-02 22:48

## Context

- **Project:** Snake
- **Branch:** v2
- **Participants:** Scott Liu
- **Starting context:** Resumed from `SessionRecords/2026-10-25_14-14-compare-v1-v2-specification-gaps.md` and clarified the prior Q4, Q5, and Q7 conclusions.
- **Scope:** Read-only explanation and source comparison; no application code, tests, or protected specifications were changed.

## Summary

This session refined why v2 can pass its current specifications and tests while still showing incorrect snake-body behavior, accepting unconfirmed physical keys, or exposing orchestration and rendering problems. The main distinction is between values being individually valid and the whole workflow being semantically valid.

For snake movement, `body_cells` must be more than a collection of legal coordinates with the correct count: it must be an ordered, continuous head-to-tail path, with explicit movement, growth, tail-removal, and collision rules. For keyboard input, the logical `Direction` contract does not identify which WinForms `Keys` produced it, so the physical-key mapping needs its own adapter or presentation contract.

The session also corrected the shorthand that v1's `Advance...Step()` “completely owns” a tick. More precisely, v1 assigns one due 250ms movement event and its main game outcome to an `Advance...Step()` contract, while explicitly delegating display, replenishment, shield rescue, and result screens to named follow-up contracts. Its tests use larger, player-observable slices, and its centralized state plus double-buffered whole-form painting reduce coordination and control-lifecycle risks. These properties lower risk but do not prove that every omitted semantic rule was specified.

## Detailed Q&A Record

### Q1. Does Q4 mean v2 merely failed to specify where new `body_cells` are added?

Yes, but “where to add a cell” is only one part of the missing rule. The broader gap is the snake-body topology and transition contract.

The current v2 `Domains/Snake/domain_spec.yaml` requires that:

- `snake.length` equals the number of `body_cells`;
- every stored cell is a Board-approved coordinate;
- a positive length delta increases the body by the requested number of cells;
- a next-head position already present in the current body produces `SelfCollision`.

It does not fully require that:

- `body_cells` are ordered from head to tail;
- consecutive cells are orthogonally adjacent under the current Board boundary policy;
- ordinary movement produces `new head + old body without old tail`;
- growth preserves the tail or extends only from the legal tail history;
- newly added body cells cannot appear at unrelated distant positions;
- moving into a tail cell is distinguished between a tail that leaves during ordinary movement and a tail that remains during growth.

Therefore an implementation could add a legal but disconnected coordinate, keep the body count correct, and still satisfy the present count-and-coordinate invariants.

It was also clarified that several cells changing color during one move is not automatically a defect. A normal move changes the new head, the old head's role, and the departing tail. It is suspicious only when a changed cell cannot be explained as the new head, old head/body, old tail, contacted object, or another valid Domain transition.

### Q2. What does “snake-body topology” mean?

In this project, topology is practical terminology for the structure of the snake rather than an advanced mathematical topology concept. It means the order and connectivity of the stored cells and the roles of the head and tail.

A valid five-cell body might be:

```text
[(5,3), (4,3), (3,3), (2,3), (1,3)]
  head                                  tail
```

Each consecutive pair is adjacent, and the list has a defined head-to-tail order. The following has five legal Board coordinates but is not a valid connected snake:

```text
[(5,3), (4,3), (3,3), (8,3), (8,7)]
```

This structure is necessary to define ordinary movement, growth, shrinking, self-collision, and rendering consistently. A concise invariant is:

> `body_cells` must be an ordered, continuous head-to-tail path, not merely a correctly sized collection of legal coordinates.

### Q3. Why did v1 not show the same body-cell problem if its topology contract was also incomplete?

v1 did not mathematically specify every body transition either. Its implementation used the conventional snake algorithm: calculate the next head, exclude the departing tail from ordinary self-collision checks, insert the new head, and remove the old tail unless growth occurs. That implementation matched the intended behavior.

Accordingly, “the key contract and implementation happened to align” means the unspecified semantic choice was implemented correctly; it does not mean the behavior was guaranteed by a complete specification. v1's larger step contract, integration tests, and simpler state path protected surrounding behavior, but a different incorrect growth implementation could still have exposed the same specification gap.

“Happened to align” should not be read as accidental or careless coding. It identifies a guarantee boundary: the observed implementation was correct, but the specification alone could not force every conforming implementation to make the same choice.

### Q4. Is Q5 merely saying that v2 wrote “direction key” instead of explicitly writing “up/down/left/right keys”?

No. In normal Chinese UI usage, “鍵盤方向鍵” already strongly suggests the physical arrow keys. v2 also explicitly restricts the logical Domain direction to `Up`, `Down`, `Left`, and `Right`.

The missing contract is the translation between physical WinForms events and the logical command:

```text
Keys.Up     -> Direction.Up
Keys.Down   -> Direction.Down
Keys.Left   -> Direction.Left
Keys.Right  -> Direction.Right
Keys.W/A/S/D -> supported or ignored?
```

By the time `set-snake-direction` receives `SetSnakeDirectionCommand(Direction.Right)`, it cannot tell whether that value came from `Keys.Right`, `Keys.D`, another key, or another input device. Thus the Domain acceptance tests can all pass even when the WinForms adapter accepts WASD without an explicit product decision.

If the intended control scheme is arrow keys only, the adapter or presentation contract should list the four `Keys` mappings and require all other keys to produce no direction command. If WASD is supported, it should list those mappings explicitly as well.

### Q5. Did v1's Design by Contract explicitly make `Advance...Step()` the owner of a complete tick?

Largely yes at the use-case semantic level, but “complete owner” needs qualification.

The v1 `origin/master:design_by_contract.md` contracts define the input grain as one due 250ms movement event and the output grain as the level state after processing that movement and its main consequences:

- `AdvanceFirstLevelStep()` covers one-cell movement, wrapping, ordinary food, self-collision, and the 20-cell completion decision.
- `AdvanceSecondLevelStep()` additionally covers ordinary/special food, shield, fire, fatal reduction, self-collision, shield/death branching, and the 20-cell completion decision.
- `AdvanceThirdLevelStep()` additionally covers bombs, walls, open/closed doors, self-collision, shield/death branching, and the 50-cell game-completion decision.

The same document's “合約呼叫流程” specifies the follow-up relationships to `MaintainLevelObjects()`, `UpdateLevelStatus()`, `RescueWithShield()`, result screens, and next-level screens.

Therefore the accurate statement is:

> v1 has a step contract that owns the main decision and final semantic outcome of one movement event, plus an explicit contract-level call flow for delegated follow-ups.

It does not mean that rendering, replenishment, and every secondary effect must be implemented inside one method.

The phrase “one Timer tick” was also corrected. v1's UI timer checks the clock every 16ms; `Advance...Step()` represents a movement event that becomes due every 250ms.

### Q6. What does it mean that T-03, T-04, T-07, and T-10 focus on visible behavior?

“Visible” includes player-observable game state, not only rendered pixels.

- **T-03** queues a logical direction, proves the input itself does not move or resize the snake, advances the game, and verifies the next head coordinate. It also checks reverse, invalid, and non-playing input. It does not, however, verify physical WinForms key codes.
- **T-04** verifies 249ms versus 250ms, one-cell movement, wrapping at all four boundaries, ordinary-food growth, self-collision, and the first-level completion boundary.
- **T-07** verifies second-level item effects, fatal and non-fatal fire, body collision, shield rescue, unprotected death, and the 20-cell boundary.
- **T-10** verifies third-level items, bombs, outer and inner walls, open and closed doors, body collision, shield rescue, the 50-cell completion boundary, and door occupancy behavior.

These tests ask what the player-observable state is after a meaningful operation: snake position, length, phase, death cause, shield count, and level transition. They do not use method success, a DTO shape, a scenario ID, or a naming rule as a substitute for that result.

The v2 tests are not purely structural and do test behavior. The more accurate contrast is their acceptance slice:

```text
v1: one player operation / complete movement step -> final observable state
v2: one local use case -> local result or state fact
```

v2's `advance-snake-step` acceptance currently covers wrapping, third-level boundary blocking, and not-applicable states. Food, collisions, completion, replenishment, and rendering are verified separately. What is missing is an executable end-to-end tick acceptance that fixes the complete ordering and final published result.

### Q7. What does “v1's implementation architecture was simpler” mean?

The statement covered both game-state flow and UI rendering.

In v1, a single `SnakeGame` instance owns the snake, direction, objects, walls, doors, shield, level, phase, time, and terminal results. Its `AdvanceOneCell()` reads and changes this central state through one path: calculate the next head, check Board obstacles and collision, move or grow the body, apply the contacted object, and decide completion or death.

In v2, one tick crosses Entities, `SnakeGameSession`, multiple Use Cases, Commands/Results, multiple `ReadGameplay()` snapshots, the Presenter, View Models, and the WinForms View. `Snake.WinForms/GamePresenter.cs::OnTickRequested` currently determines this orchestration order in code:

```text
UpdateThirdLevelDoorState
-> AdvanceSnakeStep
-> read snapshot
-> handle collision or object contact
-> EvaluateLevelCompletion
-> optionally EndCurrentLevelPlay
-> optionally ReplenishTriggeredObject
-> RenderCurrentScreen
```

More layers are not inherently wrong, but they add more ordering, data-transfer, partial-failure, duplicate-processing, and intermediate-state boundaries that need an explicit top-level contract and integration acceptance.

For rendering, v1 uses `DoubleBuffered = true` and draws the entire board and sidebar through one `OnPaint()`/`Graphics` path before displaying the completed frame. v2 uses many WinForms child controls. `Snake.WinForms/GameForm.cs::Render()` updates the board, labels, progress, rules, result card, timer, and layout, while `RenderRules()` clears and recreates its rule controls on every render.

Although `SuspendLayout()` reduces layout churn, repeatedly detaching and adding controls can still cause layout and repaint transitions. This makes temporal UI requirements such as stable controls, atomic presentation, and flicker prevention more important in v2.

The conclusion is not that v1's concentrated design is universally better. It is that v1 has fewer coordination boundaries and a naturally atomic drawing path, so these particular omissions were less likely to become visible. Once v2 introduced more boundaries, the specifications and tests needed corresponding workflow and temporal contracts.

## Key Clarifications

- “Snake-body topology” means an ordered, continuous head-to-tail path with defined movement and growth transitions, not merely legal coordinates and the correct count.
- Multiple changed cells during movement can be normal; the relevant question is whether every change is explained by the new head, old head/body, departing tail, object contact, or another valid transition.
- v1's correct body behavior was not fully guaranteed by its specification; the implementation selected the expected semantics and the surrounding contracts/tests reduced exposure.
- “Direction key” already normally means the arrow keys. The missing v2 rule is the physical `Keys`-to-`Direction` mapping and the behavior of unlisted keys.
- v1's `Advance...Step()` owns the main result of one due movement event, not every rendering or follow-up operation.
- v2 tests are not merely structural; their main limitation is that local behaviors are not recomposed into a complete tick-level acceptance.
- Simpler architecture reduces coordination opportunities for failure but does not itself prove correctness or make all missing requirements unnecessary.

## Decisions Made

- Use “one due 250ms movement event” instead of the ambiguous phrase “one Timer tick” when describing v1.
- Describe v1 as owning the main semantic result of a step with explicit delegated follow-ups, rather than claiming every concern is performed by one contract or method.
- Describe the v1/v2 test difference primarily as complete-operation versus local-use-case acceptance grain, not behavior tests versus structural tests.
- Treat physical-key mapping as an adapter/presentation contract separate from the logical Domain `Direction` contract.
- No decision was made yet on whether the product should support arrow keys only, WASD only, or both.

## Code Changes

- Added `SessionRecords/2026-11-02_22-48-clarify-snake-contract-boundaries.md` containing this detailed Q&A record.
- No application code, tests, `Domains/**`, `Pattern Network/**`, or other specifications were modified.

## Follow-up Items

- Decide the supported physical keyboard scheme: arrow keys, WASD, or both.
- If specifications are to be changed, first request explicit authorization for the exact protected `Domains/**` or `Pattern Network/**` files.
- Define the ordered/connected `body_cells` invariant, movement/growth transition, and departing-tail collision rule.
- Define a top-level tick workflow contract that owns ordering, intermediate-state visibility, and the final published result across v2 Use Cases.
- Add complete-tick acceptance scenarios covering food growth, self-collision, departing-tail movement, shield rescue, death, completion, replenishment, and render timing.
- Define temporal presentation requirements for stable controls, atomic render, timer ownership, and flicker prevention.
