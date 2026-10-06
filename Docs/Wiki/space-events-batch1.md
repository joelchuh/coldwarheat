# 事件太空推进首批

description：查捕获纳粹科学家的一格事件推进、普通尝试隔离、正常奖励/领先能力和终点待裁定。

状态2026-10-06：#18参数Bound，C#待实现。[描述](../../Data/Descriptors/space-events-batch1.data.json)→[参数](../../Data/Cards/deluxe-2015/space-events-batch1.parameters.json)、[Schema](../Contracts/space-events-batch1.schema.json)、[案例](../Design/space-solidarity-events.cases.json)、[验证](../Design/space-solidarity-events.validation.json)。

采用既有SpaceRaceService.PlanEventAdvance，经可信ResolutionFrame取EffectController作为受益方，不接受客户端自报轨道/奖励；中立事件控制方也不能一律替换为根行动者。事件推进一格，不查普通OPS门槛、不掷骰，不增加或归零普通AttemptsUsedThisTurn；即使本回合普通额度已用完也可事件推进。一般EventAttempt/Headline合法性仍由出牌编排验证。

轨道终点、目标格、先到/后到VP及能力只查rules.deluxe2015.space-race，参数不复制8格数据。位置0至倒数第1格：计算下一格、按对手到达顺序发正常目标奖励、根据双方新位置刷新领先能力。获得第二次尝试不是把已有次数清零；新头条策略只影响未建立的选择组；第8格追加名额仍由原调度器决定，事件本身不插入立即根行动。双方相同格按实际先后记录，不发两个先到奖。

英文Deluxe p4 #18是一格。FAQ PDFp4明确事件不算普通尝试；紧接p5旧二格问答不可改写本版步数。共同轨道/奖励入口生成AdvanceId与VP批次，RuleEngine原子提交位置、奖励收据、能力投影及事件完成/星号去向，然后胜负检查；终局不再开始后续根行动。没有实际事务或CardDisposition运行验证。

已在轨道终点：不越界、不重发奖励，事件可否合法无变化完成/移出的细节尚未找到专门官方裁定。SS1-OPEN-01当前RequiresRuling，整次事件零修改、零随机，建议“允许无变化完成并移出”尚未获用户确认。普通Space用牌终点明确拒绝，与此待裁定事件路径分开。普通Space使用#18须按实际有效OPS资格，通常印刷1不足；修正后能合法时只走普通尝试，事件不发生，星号牌弃置而不移出。

重复AdvanceId绑定完整请求摘要与来源帧，返原收据，不再给VP；同ID不同请求冲突。损坏位置、负次数、到达记录/版本冲突、不可信授权与未知相关干预拒绝/RequiresRuling；内部失败回滚本步。表现层只读轨道投影，更换布局或画风不改推进。

依据[英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)p4、[规则2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)§6.4、[FAQ](https://www.gmtgames.com/nnts/FAQv5.pdf)PDFp4。基础接缝见[太空服务](space-race.md)。

本批共同主离线检查22数据＋48参考＝70新项，1265旧回归，共1335通过；27 Schema/30数据/19模块、90参数指针。8runtime未运行，38工作工具实际检查另层，独立只读审阅未发现Critical/Important；22数据子集与48参考均独立复算，meta/1265旧回归未独立运行，两项Minor摘要已主侧修正。
