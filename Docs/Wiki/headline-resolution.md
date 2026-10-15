# 有限头条事件结算

> description: 查冻结队列、头条根责任与事件收尾的运行边界；当前游标已实现，真实事件执行/Host/UI尚未接。

先读[模块摘要](../Descriptors/headline-resolution.module.json)。CreateOrdinary要求双方已保留且无人正在Resolving，复核目录后冻结普通印刷值顺序；AdvanceAfterResolution仅接受当前牌与出牌方一次，不改变原快照。CurrentResolutionId按回合及出牌方区分，避免两张头条复用普通action0身份。

游标推进不等于事件执行；编排仍须完成当前实体去向、等待选择和终局判断后才能调用。Defectors等特殊优先不能直接用普通候选顺序替代。18个新增RED→GREEN及79个既有头条检查通过，共97；Unity原生/Host/UI未验。[实施计划](../Plans/2026-10-15-headline-event-resolution.md)记录后续根接线与选择恢复。
