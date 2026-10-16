# 单格太空事件基础 Implementation Plan

> **For agentic workers:** 使用executing-plans在当前会话内联实施，TDD和verification-before-completion；整批完成后requesting-code-review做一次fresh只读审查。

**Goal:** #18可信单格事件推进与普通尝试共用奖励计算，产生可接入宿主的不可变计划。
**Architecture:** 正式参数严格加载 → Card/Track绑定定义 → 可信请求 → 去重/终点检查 → 共享单格计算 → 保留次数与两类收据的新状态。
**Tech Stack:** Unity6000.6.0f1、纯C#Core、Newtonsoft仅Infrastructure、EditMode/NUnit。
**Spec:** Docs/Architecture/space-event-foundation.md。

## Global Constraints

- 不改正式卡牌/轨道数值，不采纳终点未知解释；不接客户端命令、牌区、VP提交与终局。
- 同一轨道对象绑定、双方次数保持；普通尝试/下一回合不得丢事件收据。
- ID共用命名空间、同事实幂等/冲突拒绝、总4096上限；回合先验证，旧回合请求不能重放。
- 用户允许自主持续执行；复用用户checkout的feature分支，保留两项无关未跟踪文件。Unity运行时不改C#和共享输入。

## Review Focus

1. 两条推进路径先后到同一格：测试事件先到+普通后到及反向顺序，不能发两个首奖。
2. 已用普通次数后事件取得/失去第二次资格：真实普通尝试准备状态，验证次数不清空。
3. 普通尝试与跨回合丢事件收据：完成后再重试、冲突和下一回合保留集合测试。
4. 放宽schema不能偷换已实现政策：参数/来源/轨道引用/卡牌绑定负例在加载器拒绝。
5. 终点重试与新请求不同：到8的原ID可幂等，新IDRequiresRuling，无奖励与越界。

## Task 1 — 共享计算、事件计划、加载与可调用描述

**Create Core/Rules:** SpaceAdvanceRules.cs（SpaceAdvanceCalculation与CalculateSingleStep）、SpaceEventDefinition.cs、SpaceEventState.cs（Request/Receipt/Plan/Eligibility）、SpaceEventRules.cs。
**Modify Core/Rules:** SpaceAttemptState.cs新增EventReceipts与合计上限；SpaceAttemptRules.cs成功时使用共享计算、保留事件收据、拒绝跨类型ID。
**Create Infrastructure/DataLoading:** SpaceEventDefinitionLoader.cs：`Load(string json,string schema,CardCatalog cards,SpaceRaceDefinition track,IReadOnlyCollection<string> knownEffectIds)`。
**Tests:** 新Assets/ColdWarStruggle/Tests/EditMode/SpaceEventFoundationTests.cs、SpaceEventDefinitionTests.cs。沿用既有SpaceAttemptPlanTests回归。
**Docs:** 新Docs/Descriptors/space-event-foundation.module.json及目录条目、Docs/Wiki/space-event-foundation.md、Docs/Worklogs/2026-10-17-space-event-foundation.md、Docs/Testing/space-event-foundation.validation.json；更新space-events-batch1与space-race当前实现入口及Wiki索引。

**Interfaces:** `SpaceEventDefinition(cards,track,cardId,effectId,advanceSteps)`；`SpaceEventRequest(id,sourceActionId,turnNumber,beneficiary,definition)`；`SpaceEventRules.QueryEligibility(state,request)`；`SpaceEventRules.PlanAdvance(state,request)`；`SpaceAdvanceRules.CalculateSingleStep(track,beneficiary)`。

- [ ] 写最小可编译接口桩和真实测试，运行原生RED。测试用正式轨道、手核奖励表，不由被测函数生成预期；一组完整双方/先后/目标表及故障边界。
```csharp
var plan = SpaceEventRules.PlanAdvance(state, request);
Assert.That(plan.After.Track.US, Is.EqualTo(1));
Assert.That(plan.VpDeltaToApply, Is.EqualTo(2));
Assert.That(plan.After.UsAttempts, Is.Zero);
var retry = SpaceEventRules.PlanAdvance(plan.After, request);
Assert.That(retry.After, Is.SameAs(plan.After));
Assert.That(retry.VpDeltaToApply, Is.Zero);
```
- [ ] 实现共享计算、定义与严格加载、两类收据保留、事件幂等与终点边界。普通成功改调用共用计算，失败逻辑保持。
```csharp
if (state.EventReceipts.TryGetValue(request.Id, out var prior)) {
    if (!prior.Request.SameFacts(request)) throw new ArgumentException("Conflicting space event ID.");
    return new SpaceEventPlan(state, state, prior, true);
}
var advance = SpaceAdvanceRules.CalculateSingleStep(state.Track, request.Beneficiary);
```
- [ ] 运行Tools/Scripts/run-unity-editmode.ps1，实际全部通过后记录测试XML/SHA与源提交；修复真实失败，不把编译失败当规则RED。
- [ ] 更新模块JSON、Wiki/工作记录与验证报告；运行元数据与diff检查、提交；fresh只读审查并修必要问题；推送分支并ls-remote核对。基础层本批不重建未接入该功能的Player。
