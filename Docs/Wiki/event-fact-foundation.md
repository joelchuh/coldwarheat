# 已完成影响力事件成功账本

先读 [模块JSON](../Descriptors/event-fact-foundation.module.json)，再按 entrypoints 读取对应函数。来源与正式策略仍放在 treaty_protection 数据的 event_fact_policy；不在每张牌或脚本重复常量。

EventFactDefinitionLoader.Load 校验既有合同、版本与来源，绑定同一地图/卡牌目录及明确覆盖的效果实例。EventFactLedger.Create 要求显式 historyKnown；旧存档缺历史不能默认已知空。完整结算的 InfluenceEventTransition 才能经 RecordCompleted 产生不可变事实，等待选择、拒绝或其他牌区操作均不能冒充成功。

每条事实记录 resolutionId、卡/效果、受益方/行动责任方、成功结果与版本/序号。零变化成功同样有效；精确重复返回原事实与当前账本，不重复增加；同 ID 不同证据或新事实水位倒退会拒绝。查询先验证全部 ID，再判 ANY：见到成功即可满足，否则只有已知历史且覆盖完整才是否定，其余保持 RequiresRuling。

流程：共享策略 → 绑定定义 → 引擎完成结果 → 验证/幂等/不可变追加 → 前置查询。记录不会改动 GameState、OPS、随机数或牌区；正式宿主必须后续把账本与父事件原子发布。

当前仅为基础模块，不意味着马歇尔已可出牌或北约已实现。跨回合状态、存档导入和玩家界面属于后续接线。测试/审查证据见 [验证报告](../Testing/event-fact-foundation.validation.json) 与 [工作记录](../Worklogs/2026-10-17-event-fact-foundation.md)。
