# 美日安保条约运行基础

description：定位补足控制的通用影响力效果、美日条约严格装载、永久实例候选与纯保护查询；出牌和地图展示尚未接入。

状态：3902项原生测试通过，独立审查无阻塞项，1项Minor测试覆盖建议已记录；不能作为试玩功能。入口：[模块描述](../Descriptors/japan-pact-foundation.module.json)；完整两牌设计仍见 [treaty-protection.md](treaty-protection.md)。

## 数据与职责

正式条约参数的日本分支 → JapanPactDefinitionLoader → JapanPactDefinition → AddToControlInfluenceEffect + JapanPactState → JapanPactRules.Evaluate。规则只依赖 MapDefinition 的稳定度和国家 ID；地图画风、坐标、图标都不参与裁决。

AddToControlInfluenceEffect 复用 InfluenceEventRules/Flow：只补受益方至控制，不删除对手、不减少超额值；0 增加仍成功。任何算术、状态版本或事件序号溢出都不返回部分候选。

JapanPactState 是已知无条约／已激活两种不可变权威状态；禁止以 null 表示“假定没发生”。Create 绑定定义和 gameId，Activate 只生成候选并记录源版本、序号、控制者、结算 ID；它不会直接修改地图或牌区。上层必须将加点、激活和卡牌移出一次性提交。相同请求重试返回同一候选；同 ID 不同请求冲突，活动条约收到新 ID 重复激活则 RequiresRuling。

Evaluate 返回 NoTreatyBlock、BlockedByTreaty 或 InvalidContext；NoTreatyBlock 只说明本条约不阻止，不能跳过 DEFCON、预算和基础资格。美国失去日本控制或回合号变化不会取消保护；苏联合法投放、其他国家及战争事件不受本条约全局禁止。该查询使用实际行动者而非事件根责任方。

## 验证与范围

测试：`JapanPactFoundationTests`。状态与证据见 [验证报告](../Testing/japan-pact-foundation.validation.json)；实现路径和 flow 见模块 JSON。

本批不支持北约成功事实／法国西德例外、不实现取消或存档恢复、不注册 Demo 事件；这些边界不能因正式参数存在而默认启用。下一批将组合实际出牌事务，给现有政变／调整查询加入保护门禁，并保持条约跨回合。
