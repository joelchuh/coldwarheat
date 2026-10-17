# Influence Relocation Implementation Plan

> REQUIRED SUB-SKILL: executing-plans inline；末尾一次fresh整批审查。用户已授权自主推进。沿用专用feat分支和用户Unity工作目录，不开第二个编辑器。

**Goal:** De-Stalinization从既有参数/设计接到真实手牌、双向草稿和地图试玩。
**Spec:** Docs/Architecture/influence-relocation-play.md
**Base:** 04c892c2a79f86efb05c4bbd4248b5fe5457a8d7
**Branch:** feat/influence-relocation
**Stack:** pure C#, NUnit, Unity6000.6.0f1, bounded Newtonsoft contract loader, existing IMGUI map adapter.

## Global constraints
- Source/destination complete allocation atomic; preserve total USSR and every US value; no OPS adjacency/cost.
- Unknown DS1 overlap returns RequiresRuling, not invented ruling. Zero is supported; no implicit required four.
- Static values from existing parameter dataset/schema, no new rule constants in scripts/UI/scenarios.
- Keep two user untracked files and all old Players. No C#/loaded-data edits during Unity tests/build.
- Tests first, verify failures, implement, native full green. No new generic script or duplicate session host.

## Review Focus
1. Quantity conservation, duplicate/null/overflow/forged Owner/stale selection; rejected plans cannot publish earlier removals.
2. Source may be US-controlled, destinations may not; same-country overlap cannot be silently netted or allowed.
3. Root actor versus USSR decision owner during US OPS, headline continuation, single star disposition/root completion.
4. Same command ID changed source OR changed destination is not an idempotent replay; UI drafts don't mutate authority.
5. Queries/collections immutable, UI context reset and dynamic map highlights; zero choice must be deliberate and complete.

## Task1 — reusable core plan and strict binding
Create under Assets/ColdWarStruggle/Scripts: Core/Effects/RelocateOwnInfluenceEffect.cs, Core/Effects/InfluenceRelocationRules.cs, Infrastructure/DataLoading/InfluenceRelocationDefinitionLoader.cs; matching .meta. Tests/EditMode/InfluenceRelocationRulesTests.cs and InfluenceRelocationDefinitionTests.cs.
Modify Core/Effects/BudgetedRemovalEffect.cs(enum only), InfluenceEventPlan.cs(immutable selection/query), InfluenceEventRules.cs(dispatch and shared guard).
APIs: RelocateOwnInfluenceEffect.MaximumTotal/PerDestinationCap; InfluenceEventSelection.Relocate(owner,version,sources,destinations); Query.SourceCapacities/DestinationCapacities/MaximumAmount; use existing InfluenceEventPlan/InfluenceMutation.
- [x] Stub/new tests: legal 0/1/4, multi-source, US-controlled source, nonadjacent destination, caps, duplicate/null/negative/missing/US-control, overlap RequiresRuling, immutable input/query/result, deterministic sorted changes, unknown policy/ended/setup/owner/version, numeric/version/sequence overflow and zero-source.
- [x] Loader formal data, changed semantics/version/references/unknown keys; fixture values not copied to product.
- [x] Native RED; implement and native GREEN; source hashes/ledger, commit.

## Task2 — event/session integration
Modify InfluenceEventFlow.ResolveRelocation/shared resolve, Application/Sessions/EventSessionContracts.cs and LocalGameHost.cs, Core/Rules/PracticeInfluenceCatalog.cs. Add Tests/EditMode/InfluenceRelocationPlayTests.cs and InfluenceRelocationSessionTests.cs.
APIs: fixed-identity EventCommandEnvelope.Relocate with both copied lists, EventTargetsResult matching bounded projection.
- [ ] Direct USSR and US events, empty completion, both opponent-OPS orders/modes, headline, one star/root, final ordinary slot, atomic overflow; native RED/GREEN.
- [ ] Session wrong identity/version/execution/decision, same ID same/different source/destination, rejected choice keeps hand/resolving/countries, no observer private information leak; native RED/GREEN as one task batch.
- [ ] Commit verified task and ledger. Existing generic flow continues root/heads; no extra parallel context.

## Task3 — playable map draft and packaging
Create Infrastructure/DataLoading/DemoCatalog.Relocation.cs; Presentation/DemoController.Relocation.cs and DemoView.Relocation.cs; Data/Demo/relocation-scenarios.json; tests InfluenceRelocationDemoTests.cs. Modify DemoController.Events.cs, DemoView.Events.cs and existing map selection/highlight hooks by exact symbol discovery; existing Bootstrap/Editor registration. Build target RelocationPlaytest.
- [ ] Register bound effect in existing practice definitions, strict independent fixture scenarios; test copied configuration retains existing mechanics.
- [ ] Source/destination draft tabs, caps, balanced-confirm, explicit zero, no authority writes before confirm, context invalidation/clear/retry/map interface; RED/GREEN.
- [ ] Add three player payload mappings via shared manifest; keep old payloads; add module/data descriptors, three catalogs as applicable, Wiki/index/worklog/report. Existing parameter description status updated without rewriting historical rule claims.
- [ ] One fresh whole-branch review with focus above; Important one directed RED/GREEN fix pass. Record Minor decisions; no blind second review.
- [ ] Build unique Player after final source commit; readonly payload verifier, actual mouse full relocation and zero/replay; distinguish UI/native coverage. Metadata verification, commit/push/ls-remote, next task.
