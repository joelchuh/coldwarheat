# Japan pact play implementation plan

> REQUIRED SUB-SKILL: executing-plans inline; one fresh whole-branch review. User has authorized autonomous development and routine Git sync.

Goal: real direct/opponent/headline/Five Year Plan Japan event plus permanent target protection and a playable fixture.
Architecture: GameState permanent state, shared grant+activation preparation and centralized target gates; public projection and replaceable map UI.
Tech stack: existing C#, Unity6000.6.0f1, EditMode/Player verifier, canonical JSON.
Spec: Docs/Architecture/japan-pact-play.md.

## Global constraints
- Base0e26bbdaf5a64fffb5908a4d089854d3c6604dfc, branch feat/japan-pact-play; reuse user's Unity checkout and preserve two untracked user files.
- All states immutable, source IDs stable; reject unknown registration/context before RNG; actual Actor separate from event/root side.
- AddToControl and JapanPactState from foundation remain shared rule implementation; no copied influence or stability constants.
- No NATO/cancellation/save import, final map art or networking. No automatic change to main or existing builds.
- Native PowerShell worklog is the durable ledger; no new workflow script for this single batch.

## Review Focus
1. Every central state copy preserves active pact through ordinary, opponent, nested, headline, turn-end and next-turn paths; chronological/source/map/game binding rejects torn aggregates.
2. Direct/opponent orders/headline/Five Year Plan grant+activation+disposal+parent completion commit once; no partial state on overflow or duplicate activation and host replay retains both receipts.
3. Ordinary/China/opponent/Olympic coup and realignment all reject protected Japan before RNG/OPS/risk updates, while US/other targets/influence stay governed by their usual gates.
4. Optional registrations and all definition clones preserve exact bindings and uniqueness; legacy unregistered catalogs do not pretend Japan works. Explicit inactive state cannot silently replace an active state.
5. Public permanent indicator and combined receipts agree with authoritative eligibility; fixture data, Player payloads and build paths match Editor; no hidden-information exposure.

## Task1 — Core, host and explicit catalog integration

Files: Core/State/GameState.cs; Core/Rules/ActionPracticeState.cs; new Core/Rules/ActionPracticeRules.JapanPact.cs; existing direct/opponent/FiveYearPlan/headline/coup/realignment routes and transition carriers. Infrastructure/DataLoading/DemoCatalog.JapanPact.cs plus exact registration-preserving clone sites and two initial-state factories. Application public snapshot/result/headline contracts and host carrier. Tests/EditMode/JapanPactPlayTests.cs plus necessary fixture-copy helpers.
Interfaces: consumes JapanPactDefinitionLoader.Load, JapanPactState.Create/Activate, InfluenceEventFlow. Produces optional ActionPracticeDefinition.JapanPact and GameState.JapanPact; CardActionTransition.ActivatedJapanPact; PublicJapanPactEffect in view/result; WithJapanPact(parameters,schema) explicit factory.

- [x] Write compile-only API additions and tests first: direct both actors and zero grant; grant/final overflow; wrong/stale/repeat/replay; both opponent orders (including pre-event Japan coup); headlines and FiveYearPlan child; protected ordinary/China/opponent/Olympic coup/adjustment and unchanged RNG/risk; legal influence/US/elsewhere; copies through next turn; invalid temporal and definition binding.
- [x] Run native japan-play-core-red. Expected new assertion failures, no old regression.
- [x] Implement private grant→activation→parent publication; pass pact through central constructors; add target checks and public receipt carriers. Example invariant: `active.SourceVersion < version && active.ActivationSequence <= eventSequence`. `Commit(..., japanPact:activation.After)` is the only new publication path.
- [x] Run full native japan-play-core-green; metadata/diff check, update docs, commit tested Core/Host.

## Task2 — fixture, presentation and delivery

Files: Data/Demo/japan-pact-scenario.json + Data/Descriptors/japan-pact-scenario.data.json; DemoCatalog.JapanPact.cs fixture loader; Presentation bootstrap/build/action/feedback; Application projection as needed; Tests/EditMode/JapanPactDemoTests.cs and JapanPactPresentationTests.cs; module/Wiki/catalog/worklog/report.
Interfaces: consumes Task1 public active pact/activation receipt and authoritative target eligibility; produces explicit fixture, independent JapanPactPlaytest build and readable permanent/target feedback.

- [ ] Tests first for explicit scenario registration, public permanent status, zero-change combined receipt, headline/FiveYearPlan/coup propagation and fixture payload list. Run japan-play-demo-red expecting new display/registration assertions.
- [ ] Implement fixture data and strict parser, shared loader registration, UI explanations and build entry. UI uses public target checks; no local Japanese protection rule.
- [ ] Full native japan-play-demo-green, metadata/diff; commit. One fresh whole-branch review; fix Critical/Important in one RED→GREEN pass with full suite; ledger Minor.
- [ ] Build independent Player, verify manifest/payload digests, perform limited actual interaction when available; record exact exercised paths/limits. Push feature branch and verify remote SHA.

Self-review: two tasks share only named public state/receipt/catalog APIs; state ownership and permanent lifecycle agree; five review classes map to tests. No claim of full event coverage or final visual design.
