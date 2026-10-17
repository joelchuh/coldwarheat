# Comecon Implementation Plan

> REQUIRED SUB-SKILL: executing-plans inline；完成代码后一次fresh整批审查。用户明确自主开发，沿用Unity工作目录与专用feat分支。

**Goal:** 将经互会接入真实出牌及现有地图选择，保留未裁定短缺边界。
**Spec:** Docs/Architecture/comecon-play.md
**Base:** a4920a1faaab11ff22dc6d4eb7c856b6209a1dc0
**Branch:** feat/comecon-play
**Stack:** pure C#, existing InfluenceEvent CountrySet, NUnit, Unity6000.6.0f1.

## Global constraints
- No new card MonoBehaviour/host/DSL; old AddToDistinct defaults Any.
- Caps and regions from shared formal data; runtime country control from immutable state.
- Candidate shortage/unknown interference remain RequiresRuling; no cross-card interpretation.
- Preserve user untracked files and old Players. Do not edit C#/loaded data during Unity runs.
- Prior relocation mouse acceptance and collapsed-feedback Minor remain pending; this batch does not mark them done.

## Review Focus
1. Opponent is relative to beneficiary; US influence alone must not exclude an uncontrolled country.
2. Shared AddToDistinct default/unknown filters, region duplicates and double-subregion membership cannot alter old events.
3. Candidate shortage must stop before card disposition/root completion, including OPS-before state changes.
4. Direct/headline/opponent OPS choice identity, atomic overflow, zero/duplicate/illegal targets and replay conflicts.
5. Definition copying retains all previously supported features; fixture and actual Player evidence remain distinct.

## Task1 — reusable filtered placement and strict data binding
Modify Assets/ColdWarStruggle/Scripts/Core/Effects/InfluenceEffectDefinition.cs (InfluenceControlFilter, AddToDistinctCountriesEffect.ControlFilter optional final constructor arg), InfluenceEventRules.cs (query filter, deterministic filtered changes). Create Infrastructure/DataLoading/ComeconDefinitionLoader.cs and Tests/EditMode/ComeconRulesTests.cs, ComeconDefinitionTests.cs plus .meta.
- [x] Tests: non-US control eligibility, dual subregions, no OPS/DEFCON/adjacency costs, count/distinctness, conservation of US, fixed benefit/owner/version, shortage0–3, unknown interference, setup/ended, default Any and unknown filter, duplicate regions, numeric/version/sequence overflow, stable input order; RED.
- [x] Loader rejects altered identity/profile/filter/shortage/region/refs/unknown keys and accepts real data. Implement minimal behavior; native full GREEN.
- [x] Record hashes/ledger, metadata, commit and push verified checkpoint.

## Task2 — card lifecycle and playable fixture
Create Infrastructure/DataLoading/DemoCatalog.Comecon.cs; Tests/EditMode/ComeconPlayTests.cs and ComeconDemoTests.cs; Data/Demo/comecon-scenario.json and description. Modify Bootstrap/DemoBootstrap.cs, Editor/DemoBuild.cs, shared Tools/Configuration/player-payloads.json. Reuse existing controller CountrySet; no new UI layout.
- [x] Tests: direct actors/US choice identity, OPS orders/modes, headlines, star/root once, shortage unchanged zones/rollback, stale/different-command replay, factory preserve prior features, strict fixture, map/controller four-country confirmation; RED/GREEN.
- [x] Register WithComecon/WithComeconScenario; BuildComeconPlaytest,70 payloads; Wiki/module/data/catalog/report update.
- [ ] Fresh whole-branch review; Important one RED/GREEN fix pass, record Minor. Build from source commit, verify payloads, real mouse if available; record OS blockage honestly.
- [ ] Commit/push/ls-remote. Continue next independent useful batch while retaining incomplete UI checks.

Native command: Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId <unique> -TimeoutSeconds600; expected full XML passing with no skipped/inconclusive. Review package uses one-off git log/stat/diff if Bash remains unavailable.
