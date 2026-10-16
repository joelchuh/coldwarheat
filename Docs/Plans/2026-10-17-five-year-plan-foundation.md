# Five Year Plan Foundation Implementation Plan

> **For agentic workers:** Use executing-plans inline, one fresh whole-branch review. 用户已授权自主决定并持续开发；不重复要求常规方案批准。

**Goal:** 在已验证牌区与PCG基础上实现确定性五年计划选牌和US子事件交接计划。
**Architecture:** immutable definition + authority-only selection plan + shared strict data contract. Caller preserves root and commits the plan; this batch does not run child events.
**Tech Stack:** pure C# Core, Unity6000.6.0f1 EditMode NUnit, canonical JSON/Newtonsoft bounded schema.
**Spec:** Docs/Architecture/five-year-plan-foundation.md；Docs/Wiki/hand-events-batch1.md。

## Global constraints
- 不改变已核对参数或增加规则解释；不把未实现子事件当无效果。
- US能力完整预检先于随机，随机只使用独立PCG实例；所有错误不改变输入。
- 同一实体唯一牌区，父五年计划仍Resolving；US子牌不自动发OPS/占行动。
- 计划仅权威可读，无新客户端API；本批没有存档/完整幂等/Player入口。
- 保留用户两项未跟踪文件；复用用户指定E:/TwilightStruggle目录。

## Review focus
1. 无US能力的候选即使此次随机本来会选其他牌，也必须抽样前拒绝。
2. 中国、预留头条、父源、他方手牌绝不能随机弃；计分牌必须合法无计分弃置。
3. 空手不消耗，单张也消耗一次bounded调用；无外部随机状态副作用。
4. US结果仅交接而非已完成，父牌不移动/重复，返回只读牌区不暴露客户端。
5. 抽取共同装载验证不能放松Blockade既有profile/来源/引用检查。

### Task 1: 选择计划与定义
Files under Assets/ColdWarStruggle: Scripts/Core/Rules/FiveYearPlanDefinition.cs, Scripts/Core/Cards/FiveYearPlanRules.cs, Tests/EditMode/FiveYearPlanRulesTests.cs (+meta).
Interfaces:
```csharp
FiveYearPlanDefinition(CardCatalog cards,CardId card,string effectId,PlayerSide handOwner,CardAffiliation triggeredAffiliation,int discardCount)
FiveYearPlanSelection FiveYearPlanRules.Plan(FiveYearPlanDefinition definition,CardZones cards,RandomSnapshot random,IReadOnlyCollection<CardId> supportedUsEvents)
enum FiveYearPlanRoute { EmptyHand, Discarded, AwaitingUsEvent }
// selection: Before, Cards, RandomBefore, RandomAfter, SelectedCard?, Route
```
- [x] 写最小编译接口与真实全牌区/PCG用例，原生红灯。
- [x] 实现严格定义绑定、唯一父源、支持US集合预检、稳定排序和独立抽样、只移动选中牌；输出只读计划。
- [x] 全原生绿灯：断言空/单/三阵营/计分、两种手牌顺序相同选牌、所有非候选区域保持、拒绝/溢出不变。

### Task 2: 正式装载与复用
Files: Scripts/Infrastructure/DataLoading/HandEventContract.cs, FiveYearPlanDefinitionLoader.cs, existing BlockadeDefinitionLoader.cs; Tests/EditMode/FiveYearPlanDefinitionTests.cs.
Interfaces: FiveYearPlanDefinitionLoader.Load(string json,string schema,CardCatalog cards,IReadOnlyCollection<string> knownEffectIds). Internal HandEventContract.Load(json,schema,branch) returns validated JObject branch, including exact profile and evidence-set checks.
- [x] 写正式数据正例及身份/语义/来源/重复/未知/未核实/错误schema和印刷绑定变异红灯。
- [x] 提取共同装载，保持Blockade行为和异常类别；五年计划逐项约束语义后构建定义。
- [x] 全原生绿灯并跑元数据，不改正式参数的历史说明。

### Task 3: 索引、证据与交付
Files: Docs/Descriptors/five-year-plan-foundation.module.json, Docs/Wiki/five-year-plan-foundation.md, Docs/Worklogs/2026-10-17-five-year-plan-foundation.md, Docs/Testing/five-year-plan-foundation.validation.json and catalog/Wiki links.
- [x] 写description/精确入口/数据依赖/flow/未实现边界，保存红绿证据。
- [x] 原生与元数据/diff checks通过，提交，fresh只读整批审查并修复。
- [ ] 推送feat/five-year-plan-foundation并核对远程；再继续实际子事件续接，不把此基础当可玩五年计划。

Native command: Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId unique-five-year-id -TimeoutSeconds600. 只在无Unity进程时修改C#；测试运行时不编辑编译输入。
