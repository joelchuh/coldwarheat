# 五年计划可玩续接

> description: 父根与US子事件的权威返回点、一次抽样与终局截断；当前分批实现中。

先查[模块JSON](../Descriptors/five-year-plan-play.module.json)，再查[方案](../Architecture/five-year-plan-play.md)和[计划](../Plans/2026-10-17-five-year-plan-play.md)。基础选牌与正式装载见[基础模块](five-year-plan-foundation.md)。

直接Event入口2856项原生回归已通过：FiveYearPlanExecution保留真实父根与已经接受的Selection，ActiveCard与Actor保持父根，子事件ID独立。纯影响力/DEFCON/单回合OPS复用既有服务；空手与非US/计分普通弃置，US事件等待选择或结算；正常先处理子牌再处理父牌，终局保留父子实体作审计。内部Selection含隐藏牌区，绝不能直接投影给玩家。

尚未完成对手OPS入口、头条能力注册/摘要、会话公共摘要、构建与第24练习。此前封锁Player不包含本牌的新流程。实际阶段见[验证报告](../Testing/five-year-plan-play.validation.json)。
