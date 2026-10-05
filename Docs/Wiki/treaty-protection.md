# 北约、美日安保与行动限制

description：查询条约的前置事件成功记录、跨回合保护、当前控制权、法国／西德例外、日本补至控制，以及与基础合法性和DEFCON门禁的衔接。

状态：2026-10-05规则设计与2牌参数绑定，C#待实现。入口：[模块描述](../Descriptors/action-restrictions.module.json) → [数据描述](../../Data/Descriptors/treaty-protection.data.json) → [唯一参数](../../Data/Cards/deluxe-2015/treaty-protection.parameters.json)。复用[持续效果生命周期](persistent-effects.md)，增加永久条约模板；UI通过稳定ID展示说明、保护标记及阻止原因。

## 两张牌及核对范围

| 项目 | 北约 #21 | 美日安保 #27 |
|---|---|---|
| 前置 | 马歇尔计划或华沙条约的事件已成功发生 | 无 |
| 立即影响力 | 无 | 美国增加至控制日本，已有量够则不减也不再加 |
| 保护目标 | 当前美国控制的规则地图欧洲国家 | 日本，不附加当前控制方条件 |
| 禁止操作 | 苏联政变／调整；任何玩家用Brush War攻击保护目标 | 苏联政变／调整 |
| 时限 | 永久，直到获授权取消或游戏结束 | 永久，直到获授权取消或游戏结束 |
| 局部例外 | 戴高乐使法国失去北约保护；勃兰特使西德失去保护 | 本批未发现卡面取消关系 |

来源：[GMT英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf) p4 #21，p5 #27；[2015规则](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf) §2.1.1–2、§2.1.7、§5.2、§7.3、§8.1；[FAQ v5](https://www.gmtgames.com/nnts/FAQv5.pdf) p3/p5 #21确认Brush War保护是第二版起的条款。卡图p4 #17/#23、p3 #16、p9 #55、p14 #96仅用来核对接缝。条约首批只登记这些来源牌的接缝；目前马歇尔与华沙已有参数设计，拆墙完整参数已在[事件行动授权](event-operations.md)绑定，Brush War参数已在第二批绑定，处理器待实现，戴高乐/勃兰特参数见[局部例外](nato-exceptions.md)，所有C#处理器待实现。详见[前置影响力事件](alliance-prerequisites.md)。

## 前置事件成功事实

`EventFactLedger`保存已提交事件的sourceResolutionId、CardId、EffectId、outcome、commitSequence与版本摘要。成功事件包括 `ResolvedChanged` 和 `ResolvedNoChange`；`NotTriggered`、`BlockedPrerequisite`、`Suppressed`、`Cancelled`不算成功。事实记录必须由结算事务生成，不能让UI写一个“已打出马歇尔”的布尔值。

北约在其 `EventAttempt` 实际执行时查询两张前置的成功事实，任一满足即可。只拿来做己方OPS、太空或被迫弃牌而未触发事件均不能解锁；对手做OPS而触发了该事件可以解锁。牌在Removed区不单独证明前置已发生，也不把仍在Discard区视为肯定未发生。

- 尚无成功前置且权威历史完整：`BlockedPrerequisite`。按§5.2的苏联北约OPS例子继续OPS，北约进入弃牌区，不因星号移出。
- 历史不完整且没有已知成功前置：`RequiresRuling`／诊断故障；不能猜成Blocked，也不能放行。一个已核验成功前置已足够满足OR条件，但相互冲突或损坏的账本仍须拒绝。
- 已成功的事实与当前活动状态分离，取消持续部分不会回滚过去的影响力，也不删除已提交成功记录。需要“仍生效”的其他牌条件必须另设模板，不能复用本条件。
- 同一结算ID重试检查完整请求摘要，返回原结果；同ID不同内容报冲突。技术失败回滚当前事务和拟写事实，不生成虚假的前置。

头条先后、子出牌继续位置、卡牌去向由既有TurnFlow编排；本服务不重新排序头条。玩家直接选择无法满足前置的事件用途是否允许，由出牌用途策略验证；本批只冻结EventAttempt的结果与§5.2已明确的敌方OPS路径，不增设特殊跳过或偷换用途。

## 激活与永久生命周期

计划 `TreatyEventResolver.BuildActivationPlan` 生成立即变化和 `ActiveEffectInstance`。两张牌固定事件受益方美国，禁止行动者条件另按参数查询。`duration=UntilCancelledOrGameEnd`，`expiresAt=null`，与回合OPS模板显式区分；`ExpireAndReset`不会删除条约。星号卡的实体移出与持续状态独立保存。

日本立即变化复用 `AddToControl`：从地图取得stability，增加量 `max(0, USSRInfluence + stability - USInfluence)`。不移除苏联影响力，不降低已超额的美国影响力，OPS修正不乘这次直接增加。影响力计划与条约实例必须在同一事务提交，即使增加量为0仍需激活保护。

两个永久实例均保存sourceResolutionId、源卡、控制者、activationSequence、期限类型、版本摘要和状态。不同事件ID对同一已存在条约再次激活没有本批专门裁定，返回RequiresRuling，不默许重复建实例。重试同一已提交事件走幂等收据。游戏结束保留审计状态，停止新的行动。

## 动态保护与局部例外

每次候选操作查询权威当前影响力，按 `own − opponent >= stability` 派生控制；不缓存“北约生效当时的保护国家名单”。欧洲成员来自world.json，包括加拿大、土耳其与东西欧子区，不能按现实北约成员名单或画面位置决定。

- 北约激活但美国暂时无欧洲控制国：实例仍成功建立；以后取得控制即受保护。失去控制不再保护，恢复控制重新保护。
- 美日安保在美国失去日本控制后仍禁止苏联政变／调整；条约不永久锁定美国控制权，苏联仍可通过合法的影响力路径改变控制。
- 戴高乐／勃兰特活动状态分别构成法国／西德的局部例外，适用于北约全部保护条款。它们不取消其他国家的保护，不终结北约实例，且先于北约发生的活动例外也会被查询。
- 卡图#55/#96的取消关系已核对：拆墙取消／禁止勃兰特。**TP-INTERPRETATION-01**：勃兰特活动状态被合法取消后，不再提供西德例外，北约若仍活动且美国控制西德，其保护自然恢复。这是按持续状态与取消卡文推导的接缝解释，未声称有专门FAQ。不回滚勃兰特已结算的VP或影响力。拆墙完整行动与时序见[事件行动授权](event-operations.md)；未知相关干预仍暂停。
- 相关例外状态未知时返回RequiresRuling，不能当作没发生；已验证的权威“未激活”状态可用。未来未知取消或豁免不使用静默默认值。

Brush War分支使用 `EventAttack + sourceCardId=card.brush_war`，不把北约扩大为“所有战争免疫”。美日安保也没有本批战争免疫条款。此服务返回的 `NoTreatyBlock`只说明条约未阻止；战争卡自己的国家稳定度、地区、目标、骰子与VP资格仍由对应处理器验证，当前Brush War参数已Bound，处理器仍planned。

## 行动限制组合与事务边界

计划 `ActionRestrictionService.Evaluate` 的输入为候选action、Actor、CountryId、sourceCardId、stateRevision、已验证权限/豁免、当前活动实例和事件事实。输出具名原因，包含来源效果ID及命中的国家条件。

```mermaid
flowchart LR
    A[验证上下文与状态版本] --> B[基础目标资格]
    B --> C[DEFCON地区检查]
    C --> D[条约保护及具名例外]
    D --> E[合并门禁结果]
    E --> F[预算报价与事务提交]
```

条约查询是纯计算，返回 `BlockedByTreaty`、`NoTreatyBlock`、`RequiresRuling`。聚合服务另返回Allowed／Denied／InvalidContext／RequiresRuling；未知身份、国家、版本或权限属于InvalidContext。存在明确不合法条件即可Denied并记录所有已知理由；若没有已知拒绝但必要规则尚未核实则RequiresRuling。只有全部必要门禁明确通过才Allowed。

DEFCON地区豁免仅跳过该检查，不绕过北约／美日安保、敌方影响力要求或其他事件限制。免费动作不天然免条约。国家例外移除的也只是北约对应国家的保护：如DEFCON4下法国失去北约保护，普通苏联法国政变仍因DEFCON被拒绝。

命令提交前重查当前状态版本与目标控制；同次调整的前一轮可以改变下轮目标合法性，不能把一次UI高亮保持到全部骰子结束。所有门禁和预算校验通过后才消耗OPS、随机数，或生成军事信用/DEFCON变化；被保护阻止的尝试没有这些副作用。新子行动接自己的Actor，即使PhasingPlayer不同，也不能用后者判断条约是否禁止。

## 验收、复用与下一步

[验收案例](../Design/treaty-protection.cases.json)覆盖前置历史、美国控制变化、日本增加量、国家例外、跨回合与DEFCON组合；[执行证据](../Design/treaty-protection.validation.json)区分离线模型和运行验收。C#事务、状态竞争、存档恢复、隐藏信息及完整来源牌处理器仍未实现。

共享数据通过稳定ID查询；模块描述只列必要输入、依赖和计划符号，不复制完整规则进脚本。表现层可自由更换图标、材质、布局与动效。马歇尔计划和华沙条约的影响力效果与选择分支现已在[前置事件设计](alliance-prerequisites.md)补齐；后续已设计[戴高乐／勃兰特](nato-exceptions.md)的具体效果和北约局部例外。马歇尔短缺按本次用户确认处理，经互会短缺仍待裁定。

## 前置影响力补充（2026-10-05）

[马歇尔计划与华沙条约](alliance-prerequisites.md)已绑定完整基础参数和选择流程；成功提交后沿用本页EventFactLedger。戴高乐/勃兰特已在[局部例外批](nato-exceptions.md)绑定；拆墙完整参数见[事件行动授权](event-operations.md)，Brush War参数与选择设计已在第二批补齐。

## 北约局部例外批（2026-10-05，待实现）

[戴高乐与勃兰特](nato-exceptions.md)复用固定影响力变更、永久活动实例及VP批次；取消只结束持续例外并登记未来阻止，不逆转历史收益，不删除北约。勃兰特唯一VP批次在卡文首步关闭，终局停止后续步骤。拆墙完整参数已在事件行动授权批绑定，不能以参数或取消接缝代替运行处理器。

## 拆墙与事件行动授权（2026-10-05，待实现）

见[事件行动授权](event-operations.md)：强制取消/固定收益先提交至可选行动等待点；额外行动采用单独预算，US为Actor，原PhasingPlayer承担普通核战责任。欧洲政变/调整豁免只覆盖DEFCON地理门禁；战场降级、其他事件拦截与终局仍生效。免费政变不计军事；每次调整后重查局面，余量不可转成投放、太空、政变或根OPS。

## 首批战争事件接入（2026-10-05，待实现）

[首批战争](war-events.md)复用成功事件事实契约来判断戴维营对阿以战争的阻止。北约与美日条约不提供这三张战争的全局免疫；Brush War另按其专门保护关系设计。

[战争第二批](war-events-batch2.md)已把Brush War的EventAttack接入北约查询，任意Actor均按当前美国控制过滤；国家例外仍不能绕过战争自身稳定度限制。
