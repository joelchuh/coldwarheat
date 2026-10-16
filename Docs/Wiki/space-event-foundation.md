# 单格太空事件基础

> description: 事件直接推进与普通尝试共用奖励计算，保留次数、历史、幂等及终点待裁定。
> 2026-10-17：基础已实现，3142项原生测试通过；尚未接玩家命令。

边界与流程见[设计](../Architecture/space-event-foundation.md)、[计划](../Plans/2026-10-17-space-event-foundation.md)。正式参数沿用[事件太空](space-events-batch1.md)，轨道值沿用[太空竞赛](space-race.md)。

先读[模块描述](../Descriptors/space-event-foundation.module.json)，再按entrypoints定位函数。

当前调用：SpaceEventDefinitionLoader.Load绑定正式牌与同一轨道；SpaceEventRequest由可信事件编排生成；SpaceEventRules.PlanAdvance返回不可变Before/After/Receipt，普通尝试次数不变；宿主在后续接线时提交轨道、VP、事件牌去向及胜负。能力由SpaceAbilityRules.Query即时推导，UI只取公共投影。

事件服务没有随机源参数，不计算OPS。普通成功尝试和事件调用SpaceAdvanceRules.CalculateSingleStep；两个路径共同读取当前对手位置，避免重复首到奖励。EventReceipts与普通Receipts共用ID空间，旧请求不能在下一回合重新执行；同回合已成功请求返回原收据和当前状态、零待应用VP。将来的持久化必须保存并重绑两类收据，本批未实现存档。

已在终点的新事件保持TerminalRequiresRuling，零修改；到终点的原请求仍可幂等重试。此拒绝不是“无变化事件已完成”，不移出卡牌。完整玩家授权、牌区/分数提交、头条和界面将在下一批接入。

测试28项基础+27项加载，完整回归3142/3142，RED/GREEN和源SHA见[验证报告](../Testing/space-event-foundation.validation.json)。旧设计中的SpaceRaceService.PlanEventAdvance不是已有类，实际接口为本模块的SpaceEventRules.PlanAdvance。
