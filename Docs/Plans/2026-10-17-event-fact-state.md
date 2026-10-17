# Event fact state integration plan

REQUIRED SUB-SKILL: executing-plans inline, one fresh whole-branch review. User delegated routine decisions.
Base d7c20ae252476b8c19daac2882b2d0c56663a2b7; branch feat/event-fact-state; reuse Unity checkout; preserve two user files.
Spec: Docs/Architecture/event-fact-state.md. Pure state binding only; no Host/card/Player registration or new workflow scripts.

## Review Focus
1. A ledger must match game/map/real card catalog and cannot contain evidence from a future version or sequence.
2. Every GameState copy path retains the same ledger; ordinary actions and next-turn copy preserve immutable history.
3. WithEventFacts accepts only identical ledger or one appended fact with the exact old prefix, same definition/game/history; no reset, rewrite, batch append or unknown-to-known laundering.
4. Missing legacy ledger remains null; no automatic completeness claim or Marshall/NATO runtime exposure.

## Task1 — bind and preserve
Interfaces: GameState.EventFacts readonly EventFactLedger; optional final constructor eventFacts=null; internal WithEventFacts(EventFactLedger) returns same state for identity retry, otherwise copy with no new version/sequence and only validated one-append history. All six existing copy methods preserve EventFacts.
Files: Core/State/GameState.cs; Tests/EditMode/EventFactStateTests.cs +meta; module JSON, Wiki/catalog, report and ledger.
- [ ] Write genuine-flow fixture plus invalid-binding/timeline/replacement tests and six copy-path tests; compile-only property/optional parameter/WithEventFacts stub.
- [ ] Run native event-fact-state-red, expect only new failures, old4050 pass.
- [ ] Implement constructor invariants, append-only helper and all copy preservation.
- [ ] Native event-fact-state-green passes; metadata/diff, evidence, scoped commit.
- [ ] One whole-branch read-only review; significant fixes one RED→GREEN pass if needed, minors ledger; push feature branch and verify remote SHA.

Tests directly exercise internal copy helpers via bounded reflection; production contains no reflection. Use public InfluenceEventFlow to generate actual fact receipts, not synthetic successes. No new implicit known-empty migration.
