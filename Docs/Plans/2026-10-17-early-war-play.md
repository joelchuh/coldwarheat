# 早期战争可玩接线 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: executing-plans在当前会话执行；收尾使用requesting-code-review一次fresh-context只读整分支审查。

**Goal:** 三张战争能从真实手牌/头条使用，在可替换地图确认印巴目标，完整显示结算。
**Architecture:** 会话命令→现有出牌状态机→WarEventService不可变计划→宿主统一提交→公开DTO/地图。共享参数不在UI重写。
**Tech Stack:** Unity6000.6.0f1、纯C# Core、NUnit、现有IMGUI及IMapView。
**Spec:** Docs/Architecture/early-war-play.md。

## Global Constraints

- Core无Unity；无骰点/奖励/policy客户端参数；封闭支持集合不产生未实现持续事实，不导入外部存档。
- 沿用E:/TwilightStruggle及feat/early-war-play分支；用户两项未跟踪文件保留。常规Git提交/推送已授权。
- Unity测试/构建期间不改C#/共享输入；玩家版本只在实际构建/核验后宣称可用。
- 新模块/数据维护JSON description、Wiki符号索引和工作记录。

## Review Focus

1. 第二回合同一action index仍可给军事：Task2带历史信用的跨回合测试。
2. 事件先终局不可再进入OPS；政变核战不可再战争：Task2分别覆盖。
3. pending选择不能让替换后的board/random或别人的会话通过：Task1上下文负例，Task3会话错身份和DTO序列化。
4. 后续头条失败不能留下已经移动的影响力/军事/骰游标：Task2溢出整事务测试。
5. 旧回执不能覆盖战争头条或当前选择结果；更换地图不能提交旧输入：Task3真实反馈组合与MapPresenter测试。

## Task 1 — 战争直接出牌和目标决策

**Files:** 新增Core/Rules/WarDecision.cs、ActionPracticeRules.War.cs、Tests/EditMode/WarPlayTests.cs及.meta；修改ActionPracticeState.cs、ActionPracticeRules.cs/Events.cs、PracticeRiskState.cs。
**Interfaces:** Definition.Wars、State.War、CardActionKind.ResolveWar、Transition.WarResult；CanResolveWar(GameState,PlayerSide)→RuleError，BeginWar(GameState,CardId,bool)→CardActionTransition，WarEventService.Plan已有签名不变。Commit追加可选countries，确保OPS可达性用更新后局面。

- [x] 写可编译接口和测试，真实RED：
```csharp
var before=Ready("indo_pakistani_war");
var waiting=Play(before);
Assert.That(waiting.NextState.Practice.CompletedActions,Is.Zero);
Pcg32RandomTests.Same(before.RandomState,waiting.NextState.RandomState);
var result=ActionPracticeRules.Plan(waiting.NextState,PlayerSide.USSR,CardActionKind.ResolveWar,"target",target:new CountryId("country.india"));
Assert.That(result.NextState.Practice.Risk.Military.UssrEarned,Is.EqualTo(2));
```
覆盖固定牌胜败/星号、双方印巴双目标、DEFCON2空白、终局±19、溢出及错目标/身份/版本/牌引用/board/random，验证输入不变。
- [x] 实现定义绑定、pending上下文、原子结算；先后OP/头条接线留给Task2但不注册未实现入口。
- [x] 完整GREEN、元数据、diff；更新module JSON/Wiki/worklog/report后提交。

## Task 2 — 对手OPS与头条延续

**Files:** 修改ActionPracticeRules.OpponentOps.cs/Coup.cs/War.cs、HeadlineResolutionRules.cs；新增Tests/EditMode/WarContinuationTests.cs及.meta。
**Interfaces:** BeginOpponentEvent分派Wars；SupportedCards加入Wars；Advance在War待选停下；ResolveWar响应返回HeadlineSteps并一次性继续后续头条；Coup回执携带WarResult。

- [ ] 写RED：US打苏联战争先事件或先OPS，固定军事归SU，根只完成一次；先政变核战0战争；战争VP终局停止OPS。头条印巴待选→后续影响力待选→两个根完成且行动数0；Defectors取消战争0骰。普通/头条ID含turn，跨回合同槽不冲突。后续头条溢出拒绝且原state/random/信用不变。
```csharp
var result=ActionPracticeRules.Plan(ready,PlayerSide.US,CardActionKind.OpponentEventFirst,"root",Id("korean_war"));
Assert.That(result.WarResult.Calculation.Beneficiary,Is.EqualTo(PlayerSide.USSR));
Assert.That(result.NextState.Practice.CompletedActions,Is.EqualTo(ready.Practice.CompletedActions));
Assert.That(result.NextState.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChoosingOperations));
```
- [ ] 实现上述路径，保持共享WarService唯一计算；GREEN、记录并提交。

## Task 3 — 会话、地图与可试玩构建

**Files:** 新增Application/Sessions/WarContracts.cs、Presentation/DemoController.War.cs和DemoView.War.cs、Infrastructure/DataLoading/DemoCatalog.War.cs及Tests/EditMode/WarSessionTests.cs、WarPresentationTests.cs、WarDemoTests.cs/.meta。修改SessionContracts/CardActionContracts/HeadlineSessionContracts/LocalGameHost.Cards、Map/MapPresenter、DemoView控制分派与反馈、DemoController.HeadlineFeedback、各DemoCatalog定义复制工厂、Bootstrap/DemoBootstrap、Editor/DemoBuild。
新增Data/Demo/early-war-scenarios.json及Data/Descriptors对应description与catalog。使用既有最终计分场景的可信规则和随机快照构造人工turn2局面，三张战争各一个场景；所有战争数值仍读正式参数。

- [ ] 先写可编译接口和RED：会话重复命令same receipt、冲突/旧版/错身份零变更，公开战争数据不含随机/牌库，Coup+War两回执、头条等待Owner；地图仅当前owner两目标高亮，替换视图旧intent无效，确认用真实命令；反馈读取实际绘制helper且版本绑定。Demo装配三场景、定义引用保留、非法fixture拒绝、每场景能真实完成战争。
```csharp
var once=await session.SubmitCardActionAsync(command);
Assert.That(await session.SubmitCardActionAsync(command),Is.SameAs(once));
Assert.That(session.ReadView().Version,Is.EqualTo(once.View.Version));
Assert.That(JsonConvert.SerializeObject(once.War),Does.Not.Contain("Random"));
```
- [ ] 实现公开DTO、命令按钮/地图选择、中文反馈、严格fixture及构建载荷；GREEN后补元数据验证。
- [ ] 构建WarPlaytest；核对Copy载荷与Managed hash，真实鼠标逐场景运行、日志无新异常、F12留证据。实际未测路径在report标明。
- [ ] 更新描述/Wiki/工作记录、diff检查、提交；fresh整分支只读审查，必要修复RED/GREEN；推送并ls-remote核对。

原生命令：`Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId <独立阶段名> -TimeoutSeconds 600`。元数据使用Tools/Scripts/validate-project-metadata.py。
