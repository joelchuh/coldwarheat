# 奥运会正常手牌试玩 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: executing-plans inline；完成整批后一次fresh只读审查。用户自主授权替代常规确认。

**Goal:** 真实手牌奥运会分支、自动平手续跑与抵制后普通三OPS进入单机试玩。
**Architecture:** 复用纯OlympicResolution与现有OPS/PCG/VP/DEFCON；GameState持有父执行上下文，Host固定身份/幂等，Presentation只消费公开DTO。
**Tech Stack:** Unity6000.6.0f1、C#、NUnit、共享JSON/descriptor。
**Spec:** Docs/Architecture/olympic-play.md

## Global Constraints

- 正常ActionRound手牌首批；本牌Headline/特殊嵌套未注册，OG1-OPEN-01不自行裁定。
- GameState来源/牌区/回合/版本/根必须绑定；SourceCards只是物理证据，不代替会话授权。
- 每帧/内部调用最多一对，平手不可改分支；旧Command回执幂等，故障只取消当前未提交对。
- 三种OPS预算取QuoteAsIfCard+Olympic基值；不用印刷OPS，不借中国奖励，不再耗一张牌。
- 影响力可达性保存本根起点，费用用当前局面；终局沿用Resolving审计，不后补根完成。
- 保留用户未跟踪文件与旧Player；新feat分支、独立Builds/OlympicPlaytest。
- 不新增工作脚本；只新增必要业务fixture及JSON摘要。

## Review Focus

1. 跨根/跨局/换目录/错物理区不能消费Pending，Mode前后的允许变化必须明确；Task1/2伪造上下文测试。
2. 普通手牌Olympic仍可用印刷OPS/太空，只有抵制授权使用事件基值；Task2和会话query比较测试。
3. 内部续跑不得将原响应重发再次执行，公共DTO无完整状态/隐藏牌；Task3幂等/投影测试。
4. 一次Finish/Coup只完成一个根，核战/VP终局不能移动牌或继续；Task1/2源牌及计数测试。
5. 生产Definition复制不能丢失Olympic或旧SpaceEvent/War等，fixture不能悄悄修改正式数值；Task3装配与旧入口回归。

## Task 1 — 真实根与参赛接线

**Create:** Assets/ColdWarStruggle/Scripts/Core/Rules/OlympicPracticeExecution.cs、ActionPracticeRules.Olympic.cs；Assets/ColdWarStruggle/Tests/EditMode/OlympicPlayTests.cs。
**Modify:** Core/Rules/ActionPracticeState.cs、ActionPracticeRules.cs、ActionPracticeRules.Events.cs、PracticeRiskState.cs（同Scripts根）。
**Interfaces:** ActionPracticeDefinition追加`OlympicGamesDefinition olympic=null`；State追加`OlympicPracticeExecution olympic=null`；新增命令Kind。`CanRespondToOlympic(GameState,PlayerSide)`与`PlanOlympicContinuation(GameState)`；CardActionTransition追加OlympicPracticeResult。

- [x] 编写接口stub及测试：双方真实手牌开始、对手身份、错kind/额外payload、源牌唯一、跨局/错VP/随机/根/版本、比赛正常/VP终局、平手续跑不能重选、源状态不变；正常根和头条拒绝边界。
```csharp
var begin=ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Event,"play",Card);
Assert.That(begin.NextState.Cards.ResolvingCards.Single(),Is.EqualTo(Card));
Assert.That(begin.NextState.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));
```
- [x] 原生RED（唯一RunId），Expected：新功能用例失败，原3362不回退。
- [x] 实现真实绑定wrapper、开始/回应/内部一对、风险更新和正常完成；待OPS保留选择Stage，具体用途Task2实现；头条SupportedCards不添加Olympic。
```csharp
var nextFlow=OlympicResolutionRules.Respond(flow,side,flow.DecisionId,flow.Revision,branch,new Pcg32Random(state.RandomState));
// Build complete immutable next state and result before exposing any draft.
```
- [x] 原生GREEN，零失败/跳过，记录测试/XML/源码并提交。

## Task 2 — 抵制三种OPS与单根收尾

**Create:** Core/Rules/ActionPracticeRules.OlympicOps.cs；Tests/EditMode/OlympicOperationsTests.cs。
**Modify:** ActionPracticeRules.cs、ActionPracticeRules.Coup.cs、ActionPracticeRules.Realignment.cs、OlympicPracticeExecution.cs、InfluenceRules.cs（同路径根）。
**Interfaces:** `CanChooseOlympicOperations(GameState,PlayerSide,OperationsMode)`、`QuoteOlympicOperations(GameState,PlayerSide,OpsValuePurpose)`、`CanContinueOlympicOperations(GameState,PlayerSide,OperationsMode)`；新增UseOlympicInfluence/UseOlympicRealignment/UseOlympicCoup/DeclineOlympicOperations；FinishInfluence/FinishRealignment优先完成Olympic父事件。

- [x] RED测试：抵制DEFCON2核战/存活、错误方/模式重复/陈旧状态、基值4与印刷2区分、清洗及正修正封顶、无中国bonus、固定根可达性/当前费用、投放与调整多次/Finish、政变军事与核战、无额外手牌/槽。
```csharp
var ops=ActionPracticeRules.QuoteOlympicOperations(boycott,actor,OpsValuePurpose.Influence);
Assert.That(ops.GeneralValue,Is.EqualTo(4));
Assert.That(ops.Card,Is.Null);
```
- [x] 原生RED后实现模式锁与共享算法调用；增加Olympic上下文门禁但保留已有普通/对手OPS行为。
```csharp
var quote=OpsGeneralRules.QuoteAsIfCard(d.Ops,d.Olympic.EffectId,d.Olympic.BoycottBaseOps,actor,purpose,p.OpsEffects.GetModifiers(state.Turn));
```
- [x] 原生GREEN并提交；纯组件仍只用于真实父编排，不从UI直接调用。

## Task 3 — 宿主、界面与独立试玩

**Create:** Application/Sessions/OlympicContracts.cs、LocalGameHost.Olympic.cs；Presentation/DemoController.Olympic.cs、DemoView.Olympic.cs；Infrastructure/DataLoading/DemoCatalog.Olympic.cs；Tests/EditMode/OlympicSessionTests.cs、OlympicPresentationTests.cs、OlympicDemoTests.cs；Data/Demo/olympic-scenarios.json、Data/Descriptors/olympic-scenarios.data.json；Docs/Descriptors/olympic-play.module.json、Wiki/olympic-play.md、Testing/olympic-play.validation.json。
**Modify:** SessionContracts.cs、CardActionContracts.cs、LocalGameHost.Cards.cs；DemoController.Cards.cs、DemoView.Actions.cs；各DemoCatalog定义复制入口；Editor/DemoBuild.cs、Bootstrap/DemoBootstrap.cs；Data/catalog.json、Docs/Descriptors/catalog.json、Docs/Wiki/index.md、Docs/Wiki/prototype-roadmap.md（以实际存在入口定位）。
**Interfaces:** `LocalGameHost.AdvanceInternalOlympic(PlayerSide viewer)`只在可信Host提供，不进入IGameSession；公开OlympicView/Result；控制器`RespondOlympicAsync(bool participate)`/`ChooseOlympicOperationsAsync(OperationsMode)`/`DeclineOlympicOperationsAsync()`/`AdvanceInternalOlympic()`；装配`WithOlympicEvents(parameters,schema,die,dieSchema,cardReferences)`与`WithOlympicScenarios(fixture)`；构建`BuildOlympicPlaytest`。

- [x] 写真实Host/控制器/装配行为测试与stub：身份切换、重复/冲突/旧版本、先前回执在内部继续后不重掷、DTO不泄漏、OPSquery预算一致、两方响应、错误fixture、所有复制路径保留定义。
```csharp
var first=await opponent.SubmitCardActionAsync(command);
host.AdvanceInternalOlympic(PlayerSide.US);
var retry=await opponent.SubmitCardActionAsync(command);
Assert.That(retry,Is.SameAs(first));
```
- [x] 原生RED后接公共DTO、可信单对pump、UI分支/用途/结果说明。新增fixture只含人工局面与稳定ID，正式数值从共享定义获取。
- [x] 原生GREEN；元数据与diff检查，更新报告/JSON/Wiki；提交后一次fresh整批只读审查。
- [x] 构建Builds/OlympicPlaytest；比较全部StreamingAssets源字节及Managed程序集SHA，记录构建log；工具可用时实际检查参赛/抵制核战及三OPS、重发/身份切换，保存实际截图与限制。必要修复后相关回归。
- [ ] 常规精确暂存提交/push并ls-remote核验，旧Player保留；如未完成UI验收不得标为全部通过。

Native run: Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId <unique> -TimeoutSeconds 600。运行Unity时不编辑C#/输入。每任务记录工作日志，后续发现的签名/边界差异以Ruling明确更新。
