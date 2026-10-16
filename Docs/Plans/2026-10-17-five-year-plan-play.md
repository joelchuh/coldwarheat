# Five Year Plan Play Implementation Plan

> Execute inline with executing-plans; fresh whole-branch read-only review at end. User autonomy authorizes ordinary implementation decisions. Project E:/TwilightStruggle remains the intended Unity checkout.

**Goal:** 第24练习可发动五年计划，抽样一次，US子事件完成后正确返回直接/对手OPS/头条父根。
**Spec:** Docs/Architecture/five-year-plan-play.md。
**Architecture:** immutable parent/child continuation, registered service capabilities, transactional disposition, narrow public result, replaceable map unchanged.
**Tech:** pure C# Core/Application, Unity6000.6.0f1, formal JSON loader, NUnit native EditMode, existing build/payload workflow.

## Global constraints / review focus
- 单个父子结构；所有能力预检在随机之前；未知处理器不跳过、不重抽。
- 根Actor/原牌/原OPS顺序/头条游标保持，ChildOwnerUS；子ID不与父冲突。
- 只接受绑定的牌区与RNG；父子实体唯一；子完成先处置子再处理父；终局保留审计根。
- 固定身份、幂等、版本及公开投影；客户端不见内部Selection或未选手牌。
- 每个包装保留组合结果；同次后续失败不发布任何草稿；已有提交不回滚。
- 不编辑正在测试/构建的C#；保留两项用户未跟踪文件；无额外手工规则常量副本。

### Task 1: 直接根和三类US子事件
Files: Core/Rules/FiveYearPlanExecution.cs, ActionPracticeRules.FiveYearPlan.cs, ActionPracticeState.cs, ActionPracticeRules.cs, ActionPracticeRules.Events.cs; Core/State/GameState.cs, Core/Effects/InfluenceEventFlow.cs; Tests/EditMode/FiveYearPlanPlayTests.cs (+meta).
Interfaces: optional ActionPracticeDefinition.fiveYearPlan; optional ActionPracticeState.fiveYearPlan; FiveYearPlanExecution authoritative immutable root/selection; FiveYearPlanResult narrow selected/outcome; CardActionTransition.FiveYearPlanResult and InfluenceEventTransition.FiveYearPlanResult. Existing Plan(Event) enters BeginFiveYearPlan; CompleteEventAction routes pending child into CompleteFiveYearPlanInfluence.
- [x] Stubs + true state fixtures; native RED for direct roots, discards, US waiting/no-change, turn-OPS, DEFCON/VP, terminal, preflight, stale/overflow boundaries.
- [x] Implement capability derivation, start/context binding, child adapter dispatch, atomic completion/terminal; do not overwrite parent Actor/ActiveCard.
- [x] Native GREEN whole suite; evidence and ledger.

### Task 2: 对手序列与头条
Files: Core/Rules/ActionPracticeRules.OpponentOps.cs, ActionPracticeRules.Coup.cs, HeadlineResolutionRules.cs; Tests/EditMode/FiveYearPlanFlowTests.cs (+meta), prior task entry guards if required.
Interfaces: BeginFiveYearPlan(state, alreadyResolving) used by both ordinary and opponent entry; FinishFiveYearPlan returns existing Next/AdvanceOpponentStep; Headline CompleteChoice forwards child result; SupportedCards includes configured parent.
- [ ] Native RED covering event-first/OPS-first, Influence/Realignment/Coup wrappers, terminal cutoff, headline current/second slot and next-failure rollback, returned receipts.
- [ ] Integrate existing source guards and result wrappers without relaxing unrelated one-card guards.
- [ ] Native GREEN; record root/zone/random assertions.

### Task 3: 会话、数据与Demo
Files: Application/Sessions/CardActionContracts.cs, EventSessionContracts.cs, HeadlineSessionContracts.cs, LocalGameHost.Cards.cs, LocalGameHost.Headlines.cs, public view wiring; Infrastructure/DataLoading/DemoCatalog.FiveYearPlan.cs; Presentation/DemoController*, DemoView* relevant result functions; existing Editor/DemoBuild.cs and Bootstrap payload assembly; Data/Demo/five-year-plan-scenario.json, Data/Descriptors/five-year-plan-scenario.data.json, Data/catalog.json; Tests/EditMode/FiveYearPlanSessionTests.cs, FiveYearPlanScenarioTests.cs (+meta).
Interfaces: narrow PublicFiveYearPlanResult and pending view; WithFiveYearPlan / WithFiveYearPlanScenario using existing hand-event JSON/Schema; isolated BuildFiveYearPlanPlaytest, scenario demo.practice.five_year_plan.
- [ ] Locate exact current filenames/functions through descriptors and rg before edits; register exact paths in ledger.
- [ ] Native RED for fixed identity, stale/duplicate/conflict and no second random, public data, result wrappers, strict artificial scenario, preservation through definition factories.
- [ ] Implement session projection/summary, canonical scenario loader, scenario picker, child/root feedback and payload/build binding; retain old 23 entries.
- [ ] Native GREEN + metadata/diff checks; update module/data JSON, Wiki/flow/worklog/report.
- [ ] Commit source; fresh read-only whole-branch review, fix any Important with regression RED→GREEN.
- [ ] Build Player from checked source, validate byte-exact payloads; real UI exercise random-child choice, side switch, retry, root return, map switch, reset. Report limits precisely.
- [ ] Commit evidence, push feature branch and verify remote SHA. Continue next useful batch while user allowance remains.

Native: Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId unique-five-year-play-id -TimeoutSeconds 600.
