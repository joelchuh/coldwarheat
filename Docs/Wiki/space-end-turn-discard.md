# 太空回合末弃牌

第6格领先能力在普通行动、军事要求和扣留计分牌审核之后，给能力拥有者一次选择：弃一张普通手牌，或保留全部。候选不限本方事件，带星牌也仅进弃牌堆，不触发效果或移出游戏；中国牌独立保存，不进入候选。没有普通手牌时自动记 NoCards；对手到达同一里程碑后没有这项选择。

来源：[2015规则 4.5 / 6.4.4](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)、[官方 FAQ 6.4](https://www.gmtgames.com/nnts/FAQv5.pdf)，2026-10-16核对。FAQ这里只支持“多张中选一张”和“不能靠弃牌避免计分牌扣留判负”，旧FAQ其余胜负顺序不覆盖本项目已确认2015解释。

先读[模块description](../Descriptors/space-end-turn-discard.module.json)，再按入口读取具体符号。

## 权威流程

`AssessTurnEnd → ForAudit → Pending → DiscardHeldCard / KeepHeldCards → Discarded / Declined → BeginNextTurn / FinalScoring`。无牌为NoCards，终局或无能力时不建立选择。第8格额外行动仍拒绝，不能假定已完成。

SpaceEndTurnDiscardState绑定原审核、轨道和当前牌区实例；任何替换牌区都使旧决策失效。拥有者由能力确定，可以不同于普通Actor。下一回合清除旧决策，最终计分保留已完成收据供复盘。两种决策不动VP、地图、RNG、军事或动作数，弃牌一次只改变牌区，版本和序号各增加一次。

固定US/USSR会话沿用已有命令ID去重：同内容重发返回原结果，冲突ID、旧版本、非法候选和溢出不提交部分状态。公共投影只让待选择的拥有者读到Candidates，对手看到等待；弃牌后只公开实际弃掉的一张，保留不公开牌名。

## 初始化与边界

CreateAtTurn的可选initialTrack仅用于可信人工场景；必须使用同一个SpaceRaceDefinition。它不产生尝试、随机历史或太空奖励VP，也不是存档恢复。DemoCatalog初态使用Practice携带的回合对象，避免规则日历与场景日历独立加载造成不同引用。

当前Core/会话代码已接入，原生2468/2468通过，0失败/跳过（新25项）。练习数据、完整按钮与独立Player仍待后续任务；不能把旧Player当作本批已构建成果。验证记录见[报告](../Testing/space-end-turn-discard.validation.json)，[计划](../Plans/2026-10-16-space-end-turn-discard.md)记录剩余工作。

## 练习数据接线

[差异数据description](../../Data/Descriptors/space-end-turn-discard-scenarios.data.json)定位两个人工Late场景。WithSpaceDiscardScenarios在WithFinalScoringScenario后追加，不替换原16对象。共享CreateTurnEndFixture复用源地图、初始VP、中国牌和14张行动牌；新增普通保留牌只从源牌库移动，准备过程跳过HeldCards并通过真实Start/Influence/Finish命令到Version29。无太空尝试或随机数消耗，不表示已经获得对应奖励历史。

场景17为美国太空6/苏联4、回合9；场景18为美国4/苏联6、回合10。前者弃牌后可到第10回合，后者保留后可最终计分。源场景明确跳过头条，不能将这个专项练习当作完整对局。严格限制为Late时代、恰有一方弃牌能力、无额外行动能力，以及不同的普通保留牌；计分牌/中国牌/非源牌库牌拒绝。本批新增28项，原生2496/2496通过，0失败/跳过；界面及构建待Task3。

## 表现层

DemoController.SpaceDiscard将确认弃牌/保留发给现有固定身份会话，不持有或修改Core。CanSelectHandCard只准拥有者选择当前候选；旧版本选牌自动失效；选择已完成或终局后不可再选牌。中国牌选择资格抽到CanSelectChinaCard供同一按钮复用，保持普通阶段行为。选国、换地图和语言不提交规则状态。

右侧DrawSpaceDiscardControls显示审核通过、拥有者和两个明确选择；对方只有等待提示与切身份按钮。手牌窗口复用既有私有展示，观察模式默认关闭。Editor/Player加载链同步添加同一差异JSON；BuildSpaceDiscardPlaytest输出独立Builds/SpaceDiscardPlaytest，旧Player不覆盖。UI/控制器新增14项，原生2510/2510通过，0跳过；独立审查与构建/鼠标验收仍待完成。
