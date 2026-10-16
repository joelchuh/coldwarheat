# Final Scoring Playtest Implementation Plan

> **For agentic workers:** Use executing-plans inline, test-driven-development and one fresh final whole-branch reviewer.

**Goal:** 可操作的第10回合最终计分Demo，来源清晰的分项报告。
**Architecture:** Core原子规则→既有固定身份Host→公开投影→Presentation；Infrastructure装配正式数据与显式fixture。
**Tech Stack:** Unity6000.6.0f1/C#/NUnit/现有JSON工具，无新依赖。
**Spec:** Docs/Design/2026-10-16-final-scoring-playtest.md

## Global Constraints

纯Core；共享数据唯一；保留原15场景/2个用户文件；固定状态栏/IMapView可换；默认观察关闭；旧Player不覆盖。原生测试基线2386。

## Review Focus

- 最终状态构造与原军事审核不同VP仍严格关联，零分终局不能当继续。
- 直接构造坏状态/跨地图/非10回合/特殊太空能力不得绕过资格。
- 同ID重试与旧版本/溢出不得二次计分或发布部分状态。
- 公开报告不携带规则/随机数/牌库，历史收据不覆盖当前视图。
- Fixture不伪造审核结果，所有构建载荷齐全，旧场景与地图切换兼容。

## Task 1: 权威最终命令与公开投影

Files: Core/Rules/PracticeFinalScoringRules.cs（新），ActionPracticeState.cs、ActionPracticeRules.cs、PracticeTurnEndRules.cs；Application/Sessions/FinalScoringView.cs（新）、CardActionContracts.cs、SessionContracts.cs；Tests/EditMode/FinalScoringPlayTests.cs、FinalScoringSessionTests.cs（新）。
Interfaces: ActionPracticeRules.CanCompleteFinalScoring(GameState,PlayerSide)→RuleError；CardActionKind.FinalScoring；ActionPracticeState.CreateAtTurn(definition,int,int)；PracticeFinalScoringReceipt(Result,Decision,BeforeVp)；PublicPracticeView.FinalScoring/Error/Available。
- [x] 写可编译入口和真实行为测试，fixture以CreateAtTurn+真实14行动建立，不反射修改状态。
- [x] 原生RunId final-play-core-red。Expected:新行为失败，旧2386保持。
- [x] 实现资格、完整计划、receipt不变量、投影；检查终局/重发/回滚。
- [x] RunId final-play-core-green。Expected:全套0失败0跳过。
- [x] 补模块描述与记录，提交Task1。

## Task 2: 共享数据与独立练习入口

Files: Infrastructure/DataLoading/DemoCatalog.FinalScoring.cs（新）和DemoCatalog.cs、必要FinalScoringDefinitionLoader绑定重载；Data/Demo/final-scoring-scenario.json与Data/Descriptors；Tests/EditMode/FinalScoringDemoEntryTests.cs。
Interfaces: WithFinalScoringScenario(fixture,regionJson,regionSchema,chinaJson,chinaSchema)→新DemoCatalog；DemoScenario显式fixture配置；CreateHost准备14合法行动后待审核。
- [x] 先写正式载荷/保留15场景/定义共享/fixture非法输入/可到终局测试。
- [x] RunId final-play-data-red。Expected:新增失败。
- [x] 实现最小装配与fixture，数字从JSON读取，校验规则和场景身份。
- [x] RunId final-play-data-green。Expected:全套通过；元数据验证通过；提交。

## Task 3: 控制器、固定面板、构建与交互

Files: Presentation/DemoController.FinalScoring.cs、DemoView.FinalScoring.cs（新）；现有Cards/TurnEnd面板接线；Bootstrap/DemoBootstrap.cs、Editor/DemoBuild.cs；Tests/EditMode/FinalScoringPresentationTests.cs；Docs各描述/Wiki/报告。
Interfaces: CompleteFinalScoringAsync()复用SendCard；FinalScoringPanelFrame或公开结果到展示文本；DemoBuild.BuildFinalScoringPlaytest。
- [ ] 写终局正负零/欧洲、按钮资格、重发/切身份、场景入口和构建载荷测试。
- [ ] RunId final-play-ui-red。Expected:新增失败。
- [ ] 实现、RunId final-play-ui-green；Expected:全套通过。
- [ ] 一次独立完整审查，重要项一轮RED/GREEN修复；完成元数据与源码哈希。
- [ ] 构建独立Player并验证载荷，执行真实点击审核→最终计分→重发→切图/切名。不能控制窗口时保留构建并如实标未验收，不重复问用户常规批准。
- [ ] Wiki/JSON/worklog与Git推送/远端SHA核对，保留已知用户文件。

每次原生命令：& Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId <上述> -TimeoutSeconds600。每项GREEN均对照XML/退出码，不重复无意义全套。
