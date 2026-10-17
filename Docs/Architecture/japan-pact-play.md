# Playable Japan pact

Goal: add the already verified Japan pact foundation to the actual one-window tester's card lifecycle. Maintain replaceable map rendering, authoritative commands, separate data and descriptions. User requests autonomous continuous development and Git delivery.

## Architecture

Store JapanPactState on GameState, outside transient ActionPracticeState and remainder-of-turn OPS effects. The six central GameState copies preserve it. Explicit optional registration on ActionPracticeDefinition shares exactly the map/card definitions and rejects duplicate event handlers. A registered practice must have an explicit inactive or active state; null is only allowed for old unregistered practices. Demo initialization creates the known inactive state. Definition clones carry registration rather than silently dropping it.

The GameState aggregate validates same game/map/registered definition. Active instance requires Version > SourceVersion and EventSequence >= ActivationSequence, closing the chronological candidate-preimage boundary recorded in the foundation review. No independent boolean can activate protection. The internal WithPractice publication accepts an optional newly activated candidate and advances the counters atomically; no temporary invalid GameState with future activation is constructed. Existing copies carry it unchanged, including next-turn and terminal states. Game end retains audit, commands still stop.

Shared PrepareJapanPactEvent runs the existing AddToControl flow into an unpublished candidate, clears its temporary execution, then creates activation. Direct event and headline complete one root/disposal; opponent event executes at its selected order and continues its OPS step; Five Year Plan registers this US child, resolves grant/activation, and finishes its existing parent. Every error publishes nothing, including grant, activation or final-parent overflow. No new resolution stack or dice source. Exact command replay remains the host receipt's responsibility.

Coup and Realignment target checks compose treaty query with existing DEFCON, influence, actor and budget gates before random use. Central target checks are shared by ordinary, China, opponent and Olympic routes; audit Olympic custom coup path explicitly. Rejected attempts change neither board, OPS, RNG, military nor DEFCON. Japan control may later change through legal influence; protection stays active. No generic War immunity is added.

Public snapshot exposes the active source card/effect, target, blocked actor and permanent duration; no hidden hands or internal source snapshot. CardActionTransition/result and headline/Five Year Plan/coup continuations retain both influence grant and activation receipt, including zero grant. UI renders a permanent effect explanation and specific protected-target feedback using public state, never calculates permission itself.

## Demo and limits

Add one explicit Japan fixture in shared Data/Demo with its descriptor, plus a distinct JapanPactPlaytest build. Formal influence/control amounts remain in rule/map data; fixture hands/board values are clearly artificial. Register the same loader on Editor/bootstrap and player paths; build verifier must include canonical treaty data/schema and fixture. A limited real-click route should play the event, see US control and permanent notice, reject Soviet Japan coup/realignment, and still act elsewhere. Full UI and whole-game completeness are not inferred from unit tests.

NATO, cancellation, persistence import and later-card protections stay outside this slice. No new rules interpretation. Foundation Minor printed-binding coverage is carried as a follow-up and may be covered while extending definition registration tests; it is not a blocker.
