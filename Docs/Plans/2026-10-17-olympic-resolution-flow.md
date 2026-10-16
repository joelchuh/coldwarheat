# 奥运会选择与续跑 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: executing-plans inline; one fresh read-only whole-branch review at the end. 用户自主执行授权替代常规确认。

**Goal:** 可验证的对手分支锁与逐对惰性续跑，供下一批真实 GameState 接线。
**Architecture:** 三个 Core 小文件分别定义不可变状态/收据、状态转换、有限惰性片段；复用 OlympicEventService。无 UI/IO/新业务数据。
**Tech Stack:** Unity 6000.6.0f1 / C# / NUnit EditMode。
**Spec:** Docs/Architecture/olympic-resolution-flow.md

## Global Constraints

- 原有 E:/TwilightStruggle checkout + 新 feat 分支；保留两项用户未跟踪文件，原 Player 不变。
- 用户已授权自主编码/日常 Git；不合并 main；无新脚本。
- 只接受真实目录一致且唯一 Resolving 的奥运牌；可信父编排仍负责回合/来源授权。
- 首对与分支一起产出；平手永久锁分支；一对一个快照；续跑不由玩家调用。
- 纯内存子集：无持久化摘要链、无 Host 幂等、无 OPS 执行/源牌收尾，不冒称完整可玩。

## Review Focus

1. CardZones 完整分区仍不能证明触发授权；API/文档必须清楚标记可信边界；Task1 错区/多Resolving/目录实例测试。
2. 版本溢出必须在 fork/工厂前拒绝；Task1 maxversion 测试，Task2 惰性失败测试。
3. 第二骰失败不得锁定初始分支；Task1 后可抵制测试。
4. 片段不能提前取样或丢失已输出对；Task2 枚举分步/提前dispose/后对异常测试。
5. 已完成、抵制、外事件令牌均不能重掷；Task1/2 吸收边界与错误令牌测试。

## Task 1 — 不可变选择与单对继续

**Files:** 新建 Scripts/Core/Rules/OlympicResolution.cs、OlympicResolutionRules.cs（位于 Assets/ColdWarStruggle）；Tests/EditMode/OlympicResolutionTests.cs（同根）。
**Interfaces:** `Begin(OlympicEventState,CardZones,string gameId,string resolutionId,long revision,OlympicSourceKind,PlayerSide sponsor,PlayerSide rootPhasing)`；`Respond(OlympicResolution,PlayerSide,string decisionId,long expectedRevision,OlympicBranch,IRandomProvider)`；`ContinueOne(OlympicResolution,OlympicContinuation,IRandomProvider)`；`DeclineOperations(OlympicResolution,PlayerSide,long)`。

- [x] 写 stub 和真实测试；正式定义/完整牌区初始化，测试状态/错误输入/两角色/首对故障/连续平手/核战和 VP/放弃/溢出。
```csharp
var first=OlympicResolutionRules.Respond(start,PlayerSide.USSR,start.DecisionId,7,OlympicBranch.Participate,random);
Assert.That(first.Stage,Is.EqualTo(OlympicResolutionStage.Rolling));
Assert.That(first.PendingOwner,Is.Null);
Assert.That(first.LastPair.RoundIndex,Is.EqualTo(1));
```
- [x] 运行原生 RED，Expected：新测试因 NotImplemented 失败，旧3299通过。
- [x] 实现内部构造只读链与令牌，Begin 门禁，Respond/ContinueOne/DeclineOperations 纯计划。checked(revision+1) 必须先于取样。
```csharp
var revision=checked(state.Revision+1);
var pair=OlympicEventService.PlanPair(state.Current,state.Sponsor,state.ResolutionId,OlympicInterferencePolicy.NoActiveInterference,random);
```
- [x] 完整原生 GREEN，Expected：零失败/跳过；记录证据并提交 Task1。

## Task 2 — 有限惰性继续片段

**Files:** 新建 Scripts/Core/Rules/OlympicContinuationRunner.cs、Tests/EditMode/OlympicContinuationTests.cs；Docs/Descriptors/olympic-resolution-flow.module.json、Wiki/olympic-resolution-flow.md、Testing/olympic-resolution-flow.validation.json、Worklogs/2026-10-17-olympic-resolution-flow.md，更新两个目录入口。
**Interfaces:** `PlanSlice(OlympicResolution,int maxPairs,Func<RandomSnapshot,IRandomProvider>) : IEnumerable<OlympicResolution>`。

- [x] stub + 测试片段1/3一致、工厂输入逐次游标、无提前取样/提前dispose、预算零/负、完成后不续跑、错误工厂/后对异常保留前对。
```csharp
using(var e=OlympicContinuationRunner.PlanSlice(tie,3,factory).GetEnumerator()) {
 Assert.That(factoryCalls,Is.Zero);
 Assert.That(e.MoveNext(),Is.True); var committed=e.Current;
 Assert.That(factoryCalls,Is.EqualTo(1));
}
```
- [x] 原生 RED 后实现显式参数预检和私有 iterator，每次 yield 一个 ContinueOne；核对不暗中吞异常。
```csharp
for(var i=0;i<maxPairs && current.Stage==OlympicResolutionStage.Rolling;i++) {
 var next=OlympicResolutionRules.ContinueOne(current,current.Continuation,factory(current.Current.Random));
 yield return next; current=next;
}
```
- [ ] 原生 GREEN、元数据验证及diff检查；记录源码/输入/XML SHA，提交；一次 fresh 只读整批审查；修必要项后推送并核验远端。

验证命令：Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId <唯一ID> -TimeoutSeconds 600。沿用既有原生运行器，不另造测试工作流。自审：Task2只消费Task1定义和ContinueOne；未发现接口冲突。
