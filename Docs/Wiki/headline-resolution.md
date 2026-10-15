# 有限头条事件结算

> description: 查冻结队列、头条根责任与事件收尾的运行边界；游标和有限事件编排已实现，选择继续入口及Host/UI尚未接。

先读[模块摘要](../Descriptors/headline-resolution.module.json)。CreateOrdinary要求双方已保留且无人正在Resolving，复核目录后冻结普通印刷值顺序；AdvanceAfterResolution仅接受当前牌与出牌方一次，不改变原快照。CurrentResolutionId按回合及出牌方区分，避免两张头条复用普通action0身份。

游标推进不等于事件执行；编排仍须完成当前实体去向、等待选择和终局判断后才能调用。Defectors等特殊优先不能直接用普通候选顺序替代。18个新增RED→GREEN及79个既有头条检查通过，共97；Unity原生/Host/UI未验。[实施计划](../Plans/2026-10-15-headline-event-resolution.md)记录后续根接线与选择恢复。

Begin只接受双方已保留、普通行动尚未开始的稳定权威状态，支持集合由已装配服务推导（当前真实配置12事件+7计分）。Advance至多逐张处理到下一个选择/终局/两张完成；即时分支复用原事件与计分计算，Next依据头条根推进游标而不增普通行动。所有同回合Practice副本携带队列，下一回合显式重置。

Actor在头条期间读取Current.PlayedBy；影响力Owner仍从事件定义读取，核战责任取出牌方。手牌、HeadlineReserved、Resolving三个实体来源在共享Move中区分，不把头条塞回手牌。当前步骤失败不返回任何同批成功草稿；等待前已发布状态的处理将在CompleteChoice子批验证。正常两张结束后苏联行动1，预算0；首张终局不执行后张。

Task2新增31项先RED后GREEN，全部可用托管1982项通过。无Unity、构建或鼠标验收。源码含完整权威结果，只能由未来Host过滤投影，不作为可直接传输的响应。
