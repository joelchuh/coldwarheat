# 奥运会基础 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use executing-plans inline; one fresh read-only reviewer for the whole branch at the end. User-authorized autonomy replaces routine review questions.

**Goal:** 正式参数驱动的比赛双骰与抵制核战原子基础，供下一批权威事件编排复用。
**Architecture:** Core只读定义和不可变计划；Infrastructure严格JSON加载。既有共享随机/DEFCON/胜负服务，不改Unity、Host或玩法入口。
**Tech Stack:** Unity6000.6.0f1 C#、NUnit EditMode、Newtonsoft仅Infrastructure。
**Spec:** Docs/Architecture/olympic-contest-foundation.md

## Global Constraints

- 使用既有Data/Cards/deluxe-2015/olympic-games.parameters.json和Data/Rulesets/deluxe-2015/standard-dice.json及对应Schema。
- Core无Unity/JSON。数据值加载、ID稳定，不复制规则到UI。保留用户两项未跟踪文件。
- 来源授权/分支/收据/模式/OPS执行不在本批；Offer不是授权。未知干预拒绝。
- 每对US→USSR独立fork；原RNG不可变；无重掷循环/上限。原子范围每对骰，不吞异常或伪造结果。
- 无新工作脚本；临时编辑工具不成为正式workflow。

## Review Focus

1. 弱化schema后数值/字符串/布尔混型不能被强制转换接受；Task1形状与语义测试。
2. 真实卡牌绑定和共享骰必须对齐；Task1正式数据及错误印刷/绑定测试。
3. 第二骰失败/游标不动/换stream不能暴露半结果或消耗源随机；Task2故障提供者测试。
4. 对手赢比赛与Sponsor/root核战角色不同；Task2双方VP与四角色组合测试。
5. 平手继续不是事件完成，不通过基础服务授予OPS或实现命令幂等；Task2连续计划与明确边界文档。

## Task 1 — 正式参数与比赛计算

**Create:** Assets/ColdWarStruggle/Scripts/Core/Rules/StandardDieDefinition.cs、OlympicGamesDefinition.cs、OlympicContestRules.cs；Infrastructure/DataLoading/StandardDieDefinitionLoader.cs、OlympicGamesDefinitionLoader.cs（同Scripts根）；Assets/ColdWarStruggle/Tests/EditMode/OlympicDefinitionTests.cs、OlympicContestTests.cs。
**Interfaces:** `StandardDieDefinitionLoader.Load(string,string)`；`OlympicGamesDefinitionLoader.Load(string,string,CardCatalog,StandardDieDefinition,IReadOnlyCollection<string>)`；`OlympicContestRules.Calculate(OlympicGamesDefinition,PlayerSide,int,int)`。

- [ ] 编写可编译接口stub与真实测试，跑原生RED：正式加载、schema弱化后值/类型错误、空引用、双方所有骰组合、原骰范围、总数不封顶。
```csharp
var result=OlympicContestRules.Calculate(definition,PlayerSide.US,6,6);
Assert.That(result.UsTotal,Is.EqualTo(8));
Assert.That(result.Winner,Is.EqualTo(PlayerSide.US));
```
- [ ] 实现只读定义、严格加载与数学比较。数据事实保持共享；比较对US/SU分别加SponsorModifier，Tie时Winner空、Award为TieVp。
```csharp
var us=checked(usRoll+(sponsor==PlayerSide.US?definition.SponsorModifier:0));
var su=checked(ussrRoll+(sponsor==PlayerSide.USSR?definition.SponsorModifier:0));
```
- [ ] 完整原生GREEN后记录工作日志/验证报告、源码SHA并提交。

## Task 2 — 双骰与抵制原子计划

**Create:** Assets/ColdWarStruggle/Scripts/Core/Rules/OlympicEventService.cs；Assets/ColdWarStruggle/Tests/EditMode/OlympicEventServiceTests.cs。
**Interfaces:** `OlympicEventState`、`OlympicPairPlan`、`OlympicBoycottPlan`、`OlympicOperationsOffer`；`PlanPair(state,sponsor,resolutionId,OlympicInterferencePolicy,IRandomProvider)`和`PlanBoycott(state,sponsor,rootPhasing,resolutionId,OlympicInterferencePolicy)`。

- [ ] 先写RED：双方主办/赢家/终局/平手、完整双骰提交、每次游标验证、第二骰故障源RNG不动、PCG重建、抵制四角色核战和存活Offer、无抽骰、未知干预/终局/溢出拒绝。
```csharp
var plan=OlympicEventService.PlanPair(state,PlayerSide.US,"pair",OlympicInterferencePolicy.NoActiveInterference,random);
Pcg32RandomTests.Same(state.Random,random.Capture());
Assert.That(plan.After.Defcon,Is.SameAs(state.Defcon));
```
- [ ] 实现预检、独立fork两次取样、Calculate与ClosedVpBatchEvidence；PlanBoycott复用DefconService和NuclearWarEvidence，Offer只携带参数。
```csharp
var change=DefconService.PlanChange(state.Defcon,DefconChangeKind.DegradeOne,rootPhasing);
var decision=change.NuclearWar?VictoryService.Evaluate(state.Victory,new NuclearWarEvidence(resolutionId,definition.EffectId,state.VpForUS,change)):live;
```
- [ ] 跑完整原生GREEN；同步Docs/Descriptors/olympic-contest-foundation.module.json、Wiki、Worklogs、Testing报告，注册目录；更新原奥运会数据描述中实现范围，不冒称完整卡牌已接。
- [ ] 元数据与diff检查通过、源码提交后按executing-plans进行一次fresh只读整批审查，修必要项；推送feat/olympic-contest-foundation并核验远端SHA。Player沿用SpaceEventPlaytest。

原生命令：`Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId <本步骤唯一ID> -TimeoutSeconds 600`。每轮结果如实保留，不用托管替代原生证据。计划自审已核对依赖、原子边界及未实现项；实现前确认NuclearWarEvidence构造签名。
