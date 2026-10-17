# Japan pact foundation implementation plan

> REQUIRED SUB-SKILL: executing-plans inline; one fresh whole-branch review. User authorizes autonomous implementation and regular Git synchronization.

Goal: implement reusable add-to-control and a strict immutable Japan treaty foundation.
Architecture: pure Core definitions/state/query, Infrastructure loader, existing influence transaction engine; no Unity dependency or runtime card registration.
Tech stack: existing C#/Unity 6000.6.0f1 EditMode, Newtonsoft JSON in Infrastructure only.
Spec: Docs/Architecture/japan-pact-foundation.md.

## Global constraints
- Reuse E:/TwilightStruggle on feat/japan-pact-foundation from dfeee6ef925467b7adb3d868628d33617d0d2cb2; preserve the two user untracked files.
- Existing formal data owns values; no art, networking, save import, NATO or new rule interpretation.
- Reject unknown semantics and publish no partial state. Tests and docs distinguish standalone foundation from actual host integration.
- Use persistent worklog ledger instead of Unix skill helper scripts unavailable in this PowerShell environment; do not add a new automation script for this batch.

## Review Focus
1. Noncanonical but schema-valid Japan prohibitions/gate/target/source or printed binding cannot silently change supported behavior.
2. AddToControl preserves opposing/excess influence, uses map stability, and rolls back arithmetic/revision/sequence overflow.
3. Retry uses source identity, version, sequence, actor and game; same ID different request conflicts and different ID active repeats require ruling.
4. Active pact ignores later control/turn changes, but unknown actor/action/map/country and ended/setup do not return usable permission.
5. Definition and immutable state cannot be manufactured with unsupported semantics; metadata distinguishes no treaty block from full action permission and future integration.

## Task 1 — tested foundation and metadata

Files: new Core/Effects/AddToControlInfluenceEffect.cs; new Core/Rules/JapanPactDefinition.cs, JapanPactState.cs, JapanPactRules.cs; new Infrastructure/DataLoading/JapanPactDefinitionLoader.cs; extend Core/Effects/InfluenceEventRules.cs; new Tests/EditMode/JapanPactFoundationTests.cs. All paths under Assets/ColdWarStruggle/Scripts except Tests. Docs: module descriptor, Wiki and testing/worklog; catalog entry.

Interfaces: consumes InfluenceMutation.EnsureControlByAdding(CountryId,PlayerSide), InfluenceEventRules.Query/Plan, InfluenceEventFlow, ContractJson and CanonicalJsonDigest. Produces AddToControlInfluenceEffect(card,effect,side,country), JapanPactDefinitionLoader.Load(json,schema,map,cards), JapanPactState.Create(definition,gameId), Activate(GameState,PlayerSide,string), JapanPactRules.Evaluate(GameState,JapanPactState,TreatyAction,PlayerSide,CountryId).

- [x] Write behavior tests with compile-only API skeletons. E.g. `Plan(Board(1,3), Grant(US), null, NoActiveInterference)` yields US7/SU3, not US4/SU0; zero-change remains resolved. Parametric mutations reject unsupported JSON. Activate then query USSR Japan Coup => BlockedByTreaty; query Influence => NoTreatyBlock. Retry same preimage => same state, different revision => conflict. Invalid source/action and overflow have no candidate.
- [x] Run native full EditMode with run ID japan-foundation-red; expect assertion/NotImplemented failures for new behaviors, existing tests still pass.
- [x] Implement checked AddToControl route; strict loader; immutable activation with source receipt; pure restriction query. Key arithmetic: `needed=(long)opponent+stability`, `after=checked((int)Math.Max(current,needed))`; sequence `checked(board.EventSequence+1)`. Use existing private mutation, not duplicate these formulas in another service.
- [x] Run native full suite japan-foundation-green; expect all passed, no skipped/inconclusive. Validate project metadata and diff. Record exact evidence, update descriptors and Wiki, commit.
- [x] Request one fresh read-only whole-branch review; judge every declined item. Fix Critical/Important with reproducing RED→GREEN and full suite; ledger Minor without polishing. Commit/push and verify remote SHA. No build/UI claim because no Player consumer yet.

Plan self-review: all spec requirements assigned to Task1; one task has no inter-task shared interface. Five review classes covered in tests; later runtime atomic integration deliberately excluded.

Task1 complete: source51ae3f4; GREEN3902/3902; metadata187/89; whole-branch review0Critical/0Important/1Minor (deferred). Git delivery is verified by the subsequent push/ls-remote commands; no Player build in this foundation.
