# 封锁手牌基础

> description: 先按有效OPS找当前普通手牌，再做无事件弃置；参数绑定与后续私有回应的边界。

状态：纯规则基础已实现，2708项原生 EditMode 全通过；尚未接封锁对局流程。先读 [模块JSON](../Descriptors/blockade-hand-discard.module.json)。完整设计见 [封锁与五年计划](hand-events-batch1.md)。

## 接口与流程

CardZones.Catalog 保留创建牌区时验证过的不可变目录。HandDiscardRules.QualifyingCards 使用同一目录，先验证 OPS 上下文，再只对 Owner 的 Hand 中 Event 牌按 OpsThreshold 报价并筛选。结果按稳定 CardId 排序，只读，不会改变手牌或随机状态。空手也先验证输入。普通修正仍由 OpsGeneralRules 汇总后保底/封顶，不传入地区奖励。

DiscardEventCard 是权威内部物理事务：当前 Hand 的一张 Event 移入 DiscardPile，其他区域顺序/内容保留；新 CardZones 再检查完整唯一分区。它不决定门槛、不执行被弃牌事件、不调整地图、胜负、行动或随机。调用者必须先验证当前决定、身份、版本和候选；错误不得当作 Decline。

BlockadeDefinitionLoader 将共享手牌参数中的 blockade 子项绑定 Map/Cards/Ops。五年计划、私有回应状态、固定身份命令、头条/对手事件续接和 UI 尚待接入；不得从本模块的纯函数推断这些能力已经存在。未知相关修正仍须由权威上游拒绝，不应制造一个不完整的修正列表。

```mermaid
flowchart LR
  A[已验证牌区和OPS定义] --> B[验证完整修正/用途/门槛]
  B --> C[Owner当前普通Hand事件牌]
  C --> D[有效OPS报价与阈值筛选]
  D --> E[权威内部只读候选]
  E --> F[后续会话验证决定与选择]
  F --> G[无事件弃置 新牌区]
```

本批不新增工作脚本；测试路径和真实验证报告由模块 JSON 定位。

实际证据：[验证报告](../Testing/blockade-hand-discard.validation.json)；新增21项手牌资格/弃置案例及31项封锁定义案例。不把自动测试当作界面验收。

2026-10-17装载复用：BlockadeDefinitionLoader与FiveYearPlanDefinitionLoader共用内部HandEventContract的profile/来源/Verified引用验证；两者继续分别约束分支业务语义。后续回归见[五年计划基础](five-year-plan-foundation.md)。
