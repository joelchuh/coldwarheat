# Space End-Turn Discard Implementation Plan

> **For agentic workers:** Use executing-plans inline, test-driven-development, then one fresh whole-branch reviewer. User authorized autonomous continuation; no routine approval handoffs.

**Goal:** 第6格领先方真实回合末弃/留决策，私有候选和连续回合/最终计分衔接。
**Architecture:** Core不可变决策绑定审核/轨道/牌区；既有身份命令宿主；Application过滤；可替换地图旁独立面板。
**Tech Stack:** Unity6000.6.0f1、C#、NUnit、现有JSON装配，无新依赖。
**Spec:** Docs/Design/2026-10-16-space-end-turn-discard.md

## Global Constraints

基线e8f48fd，2443原生测试通过；工作目录E:/TwilightStruggle、新分支feat/space-end-turn-discard。保留两项用户未跟踪文件。Core无Unity引用。既有16场景/FinalScoringPlaytest不覆盖。共享space-race.json为数值源，新增fixture仅保存人工局面差异。普通手牌和秘密候选只向拥有者展示；默认观察关闭。不得跳过扣留计分牌判负、第8格额外行动仍拒绝。

## Review Focus

- 状态/卡区/轨道引用换掉后不能把旧审核或候选当成有效授权；Task1覆盖换牌区/参数/轨道。
- 不是当前普通Actor但是真正能力Owner时仍能决定；Task1双方和固定身份交叉验证。
- 已弃/保留重发不可重复移动实体；Task1同ID/冲突/旧版本/新ID/溢出覆盖。
- 对手查询/失败回复/身份切换不能泄露候选和保留的牌；Task1公开投影、Task3换身份覆盖。
- 旧第16场景及同源16对象/载荷不能因提取fixture而变化；Task2版本/得分/对象身份，Task3独立构建哈希和实际点击覆盖。

## Task 1: 权威决策和私有投影

Files: 新Core/Rules/SpaceEndTurnDiscardState.cs、PracticeSpaceDiscardRules.cs，Application/Sessions/SpaceEndTurnDiscardView.cs；修改ActionPracticeState.cs/Rules.cs、PracticeTurnEndRules.cs、PracticeNextTurnRules.cs、PracticeFinalScoringRules.cs、SessionContracts.cs；Tests/EditMode/SpaceEndTurnDiscardTests.cs、SpaceEndTurnDiscardSessionTests.cs。

Interfaces: SpaceEndTurnDiscardStatus{Pending,Declined,Discarded,NoCards}；ActionPracticeState.SpaceDiscard；ActionPracticeRules.CanResolveSpaceDiscard(GameState,PlayerSide)→RuleError；CardActionKind.DiscardHeldCard/KeepHeldCards；PlayerViewSnapshot.SpaceDiscard→PublicSpaceEndTurnDiscard(Owner,Status,CanChoose,Candidates,DiscardedCard,DecisionId)。

- [ ] 写行为测试和最小可编译接口，正式轨道6/4在可信测试内部初始化，完成真实14行动后审核。测试断言：
```csharp
var s=Assessed(PlayerSide.US); // 两张Event留手，正式space6领先
Assert.That(s.Practice.SpaceDiscard.Status,Is.EqualTo(SpaceEndTurnDiscardStatus.Pending));
Assert.That(ActionPracticeRules.CanBeginNextTurn(s,PlayerSide.US),Is.EqualTo(RuleError.ActionNotReady));
var r=ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.DiscardHeldCard,"discard",held);
Assert.That(r.Accepted,Is.True);
Assert.That(r.NextState.Cards.DiscardPile.Last(),Is.EqualTo(held));
Assert.That(r.NextState.Practice.TurnEnd,Is.SameAs(s.Practice.TurnEnd));
Assert.That(r.NextState.RandomState,Is.SameAs(s.RandomState));
```
覆盖双方/保留/无牌/失去领先/星号及对手事件无效果/计分先终局/国家Target与非法ID/候选陈旧/溢出/只允许一次；宿主重发与隐私。旧“不支持弃牌”测试改为“待选择不得越过”。
- [ ] RunId space-discard-core-red。Expected: 新行为断言失败，无编译错误；保存XML原因。
- [ ] 实现ForAudit与Bind校验；PlanDiscard检查所有事实后新CardZones或保留同实例，创建已决策状态并Commit；更新两个完成门禁，最终提交保留已完成决策。Public候选按viewer过滤：
```csharp
Candidates=Array.AsReadOnly(canChoose?state.Candidates.ToArray():Array.Empty<CardId>());
```
- [ ] RunId space-discard-core-green。Expected:全套0失败/跳过；检查隐私与幂等。
- [ ] 新增模块JSON/目录登记与Wiki、验证报告、日志；元数据通过，提交。

## Task 2: 共享初态与两条练习入口

Files: 提取Infrastructure/DataLoading/DemoCatalog.TurnEndFixture.cs；新增DemoCatalog.SpaceDiscard.cs；修改DemoCatalog.FinalScoring.cs、DemoCatalog.cs、ActionPracticeState.CreateAtTurn；Data/Demo/space-end-turn-discard-scenarios.json、Data/Descriptors；Tests/EditMode/SpaceDiscardDemoEntryTests.cs。

Interfaces: CreateAtTurn(definition,turnNumber,initialVp,SpaceTrackState initialTrack=null)；internal DemoTurnEndSetup(Turn,InitialVp,Cards,Random,InitialTrack,HeldCards)；WithSpaceDiscardScenarios(string fixture)→新DemoCatalog；共享CreateTurnEndFixture(scene)执行真实普通行动。

- [ ] 写可信初始轨道同定义/零历史、保留16对象及原第16v29、两新场景合法/保留牌不消耗、无随机消耗、双方路径与非法fixture测试。
```csharp
var c=FinalScoringDemoEntryTests.Catalog().WithSpaceDiscardScenarios(json);
Assert.That(c.Scenarios.Count,Is.EqualTo(18));
var h=c.CreateHost("demo.practice.space_discard.next");
Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(29));
Assert.That(h.UsSession.ReadView().Practice.UsSpace,Is.EqualTo(6));
```
- [ ] RunId space-discard-data-red。Expected: 新行为失败。
- [ ] 装配两条Late人工场景：引用原final场景地图/动作牌，只从DrawPile补ordinary保留牌。字段严格、重复/未知ID/错误时代/跨定义/能力不足/带计分或China/源不存在全部拒绝；原16不变。
- [ ] RunId space-discard-data-green。Expected:全套通过；数据/模块目录与Wiki同步，提交。

## Task 3: 选择面板与独立Player

Files: 新Presentation/DemoController.SpaceDiscard.cs、DemoView.SpaceDiscard.cs；修改Headlines/Cards/China/TurnEnd面板；Bootstrap/DemoBootstrap.cs、Editor/DemoBuild.cs；Tests/EditMode/SpaceDiscardPresentationTests.cs。

Interfaces: DiscardSelectedHeldCardAsync() / KeepHeldCardsAsync()→Task<CardActionResult>；DrawSpaceDiscardControls；DemoBuild.BuildSpaceDiscardPlaytest输出Builds/SpaceDiscardPlaytest。

- [ ] 写控制器两选择、选牌资格/身份切换/普通终局只读、next/final按钮边界、Editor/Player载荷测试。
```csharp
controller.SelectSide(owner);
controller.SelectCard(candidate);
var r=await controller.DiscardSelectedHeldCardAsync();
Assert.That(r.Accepted,Is.True);
Assert.That(controller.ReadView().SpaceDiscard.Status,Is.EqualTo(SpaceEndTurnDiscardStatus.Discarded));
Assert.That(await controller.RetryCardAsync(),Is.SameAs(r));
```
- [ ] RunId space-discard-ui-red。Expected: 新行为失败。
- [ ] 实现私有候选/非拥有者等待；手牌弹层仅候选可选；新按钮发既有命令；拒绝和保留不暴露手牌；对局阶段提示及未支持第8格原因明确。新增一份canonical载荷，Editor/Player同链。
- [ ] RunId space-discard-ui-green。Expected: 全套通过；元数据和diff检查。
- [ ] 一次全分支审查，重要项一轮修复与RED/GREEN；归档裁定。源码冻结后构建独立Player、载荷字节核验。
- [ ] 真鼠标：场景17审核→切拥有者→选弃→重发→下一回合；场景18审核→保留→最终。核验默认观察关闭、对手候选不显示、地图切换保留状态。游戏F12截图。
- [ ] Wiki/JSON/工作记录/源码及证据哈希；提交推送功能分支并ls-remote核对。

原生验证统一：`& ./Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId <above> -TimeoutSeconds 600`。每次新目录，核验XML与exit；仅源码变化/失败才重跑，不重复消耗。
