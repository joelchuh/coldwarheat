# Scientist可玩接线 Implementation Plan

> **For agentic workers:** 使用executing-plans内联逐项执行；TDD、verification-before-completion；整批一次fresh只读审查，不派实现子代理。

**Goal:** 用户可从真实手牌/头条执行Scientist事件并查看公共推进/奖励与能力结果。
**Architecture:** 可选定义注册 → 原有Event命令/头条 → 纯推进计划+VP胜负 → 原子状态/牌去向 → 公共DTO → 可替换地图旁的结果显示。
**Tech Stack:** Unity6000.6.0f1、纯C#Core、固定LocalGameSession、IMGUI、共享JSON、WindowsPlayer。
**Spec:** Docs/Architecture/space-event-play.md。

## Global Constraints

- 终点新事件EventRequiresRuling，不采用新裁定；中立牌受益者由可信PlayedBy决定。
- 不掷骰、不计算事件OPS、不改普通次数、DEFCON、军事或地图；普通用牌路径保持。
- 正常星号移出；终局按现有审计约定留Resolving、停止根与后续头条。
- 真实身份/版本/牌区和重复命令验证保持；UI不持有GameState/随机流/完整牌库。
- 用户授权自主持续、普通Git提交推送；保存用户两项未跟踪文件。Unity运行时不编辑C#与加载输入。

## Review Focus

1. 获得太空4或8能力时已有头条/行动编排：不重开选择，新增额度由现有调度器查询；实际流程测试。
2. 终点牌仍在手：只阻止未知Event/Headline，合法OPS不受牵连；零状态/RNG与私有候选测试。
3. 与其它头条同收据完成或终局：摘要都保留，失败步骤不能部分提交；混合与终局测试。
4. 工厂在不同顺序克隆Definition：SpaceEvent注册不丢失，Cards/Space绑定保持；实际DemoCatalog方法链测试。
5. 重试原事件收据：保留当前版本/随机状态，无第二次奖励或牌去向；固定会话及UI版本保护测试。

## Task 1 — 权威即时事件和头条

**Create:** Assets/ColdWarStruggle/Scripts/Core/Rules/ActionPracticeRules.SpaceEvent.cs；Tests/EditMode/SpaceEventPlayTests.cs、SpaceEventHeadlineTests.cs（Tests均在Assets/ColdWarStruggle下）。
**Modify:** Core/Rules/ActionPracticeState.cs（Definition可选spaceEvent参数/SpaceEvent属性及Transition可选spaceEvent结果）；ActionPracticeRules.cs的CanPlay；ActionPracticeRules.Events.cs的派发；HeadlineResolutionRules.cs支持表；HeadlineFlowRules.cs选择前过滤终点实体牌。全部路径前缀Assets/ColdWarStruggle/Scripts/。
**Interfaces:** `CheckSpaceEvent(GameState, PlayerSide, CardId)`返回RuleError；`PlanSpaceEvent(GameState,PlayerSide)`返回CardActionTransition；`SpaceEventHeadlineCandidates(GameState,IReadOnlyList<CardId>)`返回经过权威持牌/终点检查的支持集合。

- [x] 写可编译接口与真实Core失败测试并运行原生RED：双方首/后奖励、普通次数已用、无随机、星号、终局、终点、正常OPS/Space、ID/回合、头条/取消/继续。
```csharp
var result=ActionPracticeRules.Plan(state,side,CardActionKind.Event,"event",scientist);
Assert.That(result.NextState.Practice.Space.AttemptsFor(side),Is.EqualTo(beforeCount));
Assert.That(result.NextState.Cards.RemovedCards,Has.Member(scientist));
Pcg32RandomTests.Same(state.RandomState,result.NextState.RandomState);
```
- [x] 实现基础计划和现有胜负/Next/Move组合，来源ID使用头条CurrentResolutionId或带Turn的PracticeSlotId。终点从CanPlay和运行时都返回EventRequiresRuling；候选过滤只看权威真实持牌者。
```csharp
var plan=SpaceEventRules.PlanAdvance(p.Space,request);
var award=plan.Receipt.AwardVp;
var decision=VictoryService.Evaluate(p.Risk.Definition.Victory,
    new ClosedVpBatchEvidence(request.Id,definition.EffectId,p.VpForUS,
        side==PlayerSide.US?award:0,side==PlayerSide.USSR?award:0));
```
- [x] 全部原生通过后记录结果并提交Task1。

## Task 2 — 固定会话与结果显示

**Create:** Application/Sessions/SpaceEventContracts.cs、Presentation/DemoView.SpaceEvent.cs；Tests/EditMode/SpaceEventSessionTests.cs、SpaceEventPresentationTests.cs。
**Modify:** Application/Sessions/CardActionContracts.cs、LocalGameHost.Cards.cs、HeadlineSessionContracts.cs；Presentation/DemoController.HeadlineFeedback.cs、DemoView.HeadlineFeedback.cs、DemoView.Actions.cs。均沿现有Assets/ColdWarStruggle根。
**Interfaces:** `PublicSpaceEventResult(SpaceEventPlan)`内部投影，公开Card/Beneficiary/From/To/FirstArrival/Award/VpForUS/双方BeforeAbilities与AfterAbilities；CardActionResult.SpaceEvent和PublicHeadlineStep.SpaceEvent；`DemoView.SpaceEventResultMessage`内部纯文本。

- [x] 真实Host/Controller测试先RED：固定双方、重复原收据、冲突/过期、两种头条混合、旧版本摘要、展开不执行。不能以纯DTO构造替代命令验证。
```csharp
var first=await session.SubmitCardActionAsync(command);
var retry=await session.SubmitCardActionAsync(command);
Assert.That(retry,Is.SameAs(first));
Assert.That(first.SpaceEvent.AwardVp,Is.EqualTo(2));
```
已核对 Application/Sessions/SessionContracts.cs 的 IGameSession.SubmitCardActionAsync 契约，沿用已有会话。
- [x] 从CardActionTransition和HeadlineResolutionStep传递公共结果。头条集合包含SpaceEvent；结果描述使用已有本地化名称/Side与结果页，不增加规则计算。
- [x] 完整回归通过后记录与提交Task2。

## Task 3 — 人工练习、描述与Player

**Create:** Infrastructure/DataLoading/DemoCatalog.SpaceEvent.cs；Data/Demo/space-event-scenarios.json及Data/Descriptors/space-event-scenarios.data.json；Tests/EditMode/SpaceEventDemoTests.cs；Docs/Descriptors/space-event-play.module.json、Docs/Wiki/space-event-play.md、Docs/Worklogs/2026-10-17-space-event-play.md、Docs/Testing/space-event-play.validation.json。
**Modify:** Infrastructure/DataLoading下所有复制ActionPracticeDefinition的DemoCatalog.*.cs保留SpaceEvent；Editor/DemoBuild.cs、Bootstrap/DemoBootstrap.cs；Docs与Data目录索引。C#路径统一Assets/ColdWarStruggle/Scripts。
**Interfaces:** `DemoCatalog.WithSpaceEvents(parameters,schema,cardReferences)`、`WithSpaceEventScenarios(fixture)`、`DemoBuild.BuildSpaceEventPlaytest()`。

- [x] 写严格fixture/方法链与真实事件测试RED：首到、后到、能力格、终点，未知字段/非法位置/重复牌/错误来源/错误Actor预备动作拒绝。
- [x] 加参数注册与四场景；源Data取得规则数据不复制奖励。所有已有复制点新增spaceEvent:d.SpaceEvent或相应位置参数。Builder和Bootstrap同时加载三新载荷。
```csharp
catalog.WithSpaceEvents(parameters,schema,cardReferences).WithSpaceEventScenarios(fixtures);
```
- [x] 新模块/数据JSON、Wiki/工作日志与验证报告完成；完整Unity回归、元数据与diff验证通过，提交后fresh只读整批审查，修必要问题。
- [x] 构建独立SpaceEventPlaytest，逐项对照所有Copy载荷；记录源提交与Managed DLL SHA。用computer-use操作首到/后到/能力/终点，保存F12图和运行日志。未验收项目按实际记录。
- [ ] 提交证据，推送feature分支并ls-remote核对；继续下一项已授权规则工作。
