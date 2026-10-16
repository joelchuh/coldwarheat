# 封锁手牌弃置基础 Implementation Plan

> **For agentic workers:** Use executing-plans inline; one fresh reviewer after this bounded batch. 用户已授权自行继续，无需重复设计审批。

**Goal:** 实现可复用的有效 OPS 手牌筛选与无事件弃置，并严格读取封锁正式定义，为下一批私有回应接线提供基础。

**Architecture:** CardZones 保留构造时的不可变 CardCatalog；HandDiscardRules 只查询普通手牌并返回不可变报价，弃牌返回新牌区。BlockadeDefinition 绑定 Map/Cards/OPS，Loader 读取既有手牌事件数据，只暴露已实现的封锁子集。本批没有会话/UI入口，不宣称封锁已可玩。

**Tech Stack:** Unity 6000.6.0f1 / pure C# / Newtonsoft JSON / NUnit EditMode.

**Spec:** Docs/Wiki/hand-events-batch1.md；Data/Cards/deluxe-2015/hand-events-batch1.parameters.json。

## Global Constraints

- Core 无 Unity 引用；候选仅权威内部使用，后续按 Owner 投影，不公开候选数量。
- 阈值从参数读取；US 的普通修正、OpsThreshold，不接受地区预算。
- 只普通 Hand 的 Event；弃牌不触发、星号仍弃置；其他区域保持唯一分区。
- 本批不改固定参数、不实现五年计划、不加入未定义的效果默认值。
- 沿用用户指定 E:/TwilightStruggle 和功能分支，保留两项用户未跟踪文件。

## Review Focus

1. 空手/仅计分牌仍拒绝无效修正和不支持的 OPS purpose，不能因为没有迭代而漏校验。
2. 苏联修正不可影响美国门槛，印刷2受遏制可达3，印刷3受清洗变2。
3. HeadlineReserved/Resolving/China/另一方/牌库不进入候选，星号牌弃置不移出。
4. 源牌区保持不变；重复弃同牌或选错牌失败，不能把错误解释为放弃。
5. Loader 拒绝坏引用、未知语义、未验证来源、缺 OPS 支持及错误印刷卡绑定。

### Task 1: 手牌阈值与弃置

Files: Core/Cards/CardZones.cs、Core/Rules/OpsGeneralRules.cs、新 Core/Cards/HandDiscardRules.cs、Tests/EditMode/HandDiscardRulesTests.cs（均在 Assets/ColdWarStruggle 下相应 Scripts/Tests）。

Interfaces: `CardZones.Catalog`；`HandDiscardRules.QualifyingCards(CardZones,PlayerSide,OpsGeneralDefinition,int,IEnumerable<OpsFlatModifier>) : IReadOnlyList<OpsGeneralQuote>`；`DiscardEventCard(CardZones,PlayerSide,CardId) : CardZones`。

- [x] 先写真实牌区测试：修正门槛/排序/不可变报价、非Hand隔离、空候选校验、弃置整分区守恒和拒绝错误选牌。
- [x] 最小编译接口后运行原生测试，观察新行为红灯。
- [x] 实现普通 OPS 共享验证与手牌查询/原子牌区复制，无全局状态修改。
- [x] 原生测试绿灯，记录 XML SHA/退出码。

### Task 2: 封锁定义装载

Files: 新 Core/Rules/BlockadeDefinition.cs、Infrastructure/DataLoading/BlockadeDefinitionLoader.cs、Tests/EditMode/BlockadeDefinitionTests.cs。

Interfaces: `Load(json,schema,map,cards,ops,knownEffectIds) : BlockadeDefinition`；定义持有实际 Map/Cards/Ops、Card、EffectId、Owner、MinimumEffectiveOps、FallbackCountry。

- [x] 先写真实数据装载和损坏数据/绑定矩阵测试，最小接口后跑红灯。
- [x] 完整 Schema 和已知版本/语义/引用校验；只绑定 blockade 参数；不接其旁边的五年计划。
- [x] 跑绿灯，写模块 JSON、Wiki、工作记录与验证报告；元数据/差异检查。
- [x] 新鲜只读独立审查，Critical/Important/Minor 均0。
- [ ] 提交/推送核对 SHA（以 Git 命令实际结果为准）。

下一批：显式手牌回应状态、固定身份命令、按玩家过滤的候选、头条/对手事件续接及 Demo。不得直接用公开国家选择状态装私有手牌。
