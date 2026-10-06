# 陷阱根行动：泥潭与捕熊陷阱

description：查Quagmire/BearTrap激活范围、有效OPS阈值、强迫弃骰/计分/内部跳过、跨回合持续和共享标准D6。

状态2026-10-06：#42 USSR3Mid星号/下划线影响US；#44 US3Mid星号/下划线影响USSR，参数Bound，C#未实现。[模块](../Descriptors/trap-actions.module.json)／[参数描述](../../Data/Descriptors/trap-events.data.json)→[参数](../../Data/Cards/deluxe-2015/trap-events.parameters.json)、[Schema](../Contracts/trap-events.schema.json)、[案例](../Design/trap-events.cases.json)、[验证](../Design/trap-events.validation.json)。65Bound37Unresolved、102处理器planned。

ActiveEffectService激活固定AffectedSide的UntilEscapeOrGameEnd实例，保存SourceResolution/ActivationRootId/激活序列/版本。TurnFlow在下一新受影响根slot调用TrapActionService，不回调可变服务：排除激活事件所在当前根的余下OPS-before/after；Headline完全不受限，不能按实际OPS Actor全局禁止。普通Root范围按next ActionRound及R2015§4.2推导；其他授权子来源、Space8额外根、跨中断恢复需要可信具体scope，否则RequiresRuling，不猜全局禁令或额外slot数。

BuildEligibilityPlan先接收可信ScoringObligation。MustScore优先，允许/要求一张正常计分而不弃骰，陷阱保留；没有义务则有合格牌必须弃，只有无合格牌才可选Scoring。没有合格弃牌也无计分，内部SkipOneRootSlot，零随机且陷阱保留。义务/剩余名额由原TurnFlow与计分资格提供，不能在陷阱里私算可放弃/尚未取得额外行动；未知义务停止。普通一根只能一张计分，计分去向/胜利仍旧服务，不能补救已扣留审计失败。

候选只本方普通Hand Event，报价Actor=AffectedSide、Purpose=OpsThreshold、有效OPS≥2，所有Flat修正按共享政策，不用地区奖励。Brezhnev可令USSR1变2，清洗可令2变1不合格；不因牌关联对方而排除。China/Scoring/HeadlineReserved/InResolution不是弃牌候选。非法/旧Revision/错误Owner在消费卡或随机前拒绝。私有候选只给Owner，其余玩家只看到实际弃牌与骰结果。

PlanForcedDiscardAndEscape完整验证后单事务：手牌→Discard、来源弃牌证据、一次RawD6、成功取消此实例或失败保留、完成当前根slot/去重收据。弃牌不执行事件、不创ActualCardUse、不产生OPS或军事，即使星号也不移出。Raw1–4成功，5–6失败，不加coup/war/OPS修正；成功也只能下一本方根行动恢复正常，当前无补行动。无候选与计分分支不掷骰，读档/重试不再弃/再掷/再完成slot。

新消费者查[共享标准骰描述](../../Data/Descriptors/standard-dice.data.json)→[标准D6](../../Data/Rulesets/deluxe-2015/standard-dice.json)，而非把SpaceRace模块当骰子服务。旧Space /die和war die_sides本阶段留作兼容数值，离线与新D6断言一致；没有声称旧服务/参数已全面迁移。注入随机源按定义请求范围，逻辑不依赖3D骰子动画、UI时间或画风。

有经验证来源的Missile Envy义务且该牌仍合格时，候选收窄为#49；仅手里有#49不是义务证明。低于阈值不能弃它，本模块保持来源义务，发本次弃牌证据给已注册来源消费者，不自行实现49后续强制OPS。义务来源不完整停RequiresRuling；计分义务仍优先强迫弃牌。

陷阱跨TurnEnd与补牌保留，Headline不受限；不能由ExpireAndReset取消。source星号实际事件发生后Removed与实例独立，逃脱只终结实例。新ID重复激活沿RequiresRuling，同ID完整请求一致只返原收据。基础包1–103禁用optional104–110：NORAD取消关系写在optional106英文卡面，不在42；只记录范围外已核实交互，无基础运行依赖/未知实例。含禁用NORAD的存档应拒绝载入，不静默取消后继续。

已提交Finished保持吸收；骰子与弃牌事务不等待动画。真实鉴权、私有候选、RNG/牌区/实例/根收据原子提交、计分义务、SourceSlot授权和保存未运行。依据[英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)p7/p8/p16、[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)§2/4.2/5.3–4/7.4.1–2/9.5/10.1.5、[FAQ](https://www.gmtgames.com/nnts/FAQv5.pdf)PDFp9–10/p12/p19–20。

主20结构＋53有限参考＝73新项，1599旧回归，共1672通过；33Schema/36data/21模块、110参数指针。8runtime未运行，13note/38Tools另层；独立完成契约审阅Ready Yes，无Critical/Important；20结构＋53参考=73及33Schema meta独立通过，工作记录字段名Minor已主側修正。
