# 一小步：有条件的两格事件推进

description：查#80落后门禁、两格推进、仅第二格VP、领先能力与越终点停止。

状态2026-10-12：参数契约已Bound；C#、Unity及真实事务均待实现。[数据描述](../../Data/Descriptors/one-small-step.data.json)→[参数](../../Data/Cards/deluxe-2015/one-small-step.parameters.json)、[Schema](../Contracts/one-small-step.schema.json)、[案例](../Design/one-small-step.cases.json)、[整合验证报告](../Design/one-small-step.validation.json)。

## 门禁与共享入口

复用既有SpaceRaceService.PlanEventAdvance，模板ConditionalTwoStepSpaceAdvance只封装卡牌策略。规则和轨道终点来自rules.deluxe2015.space-race，VP符号/胜利阈值来自rules.deluxe2015.victory；本参数不复制8格表、奖励数值或胜利阈值。

先验证完整权威状态与来源，再于EventResolution比较受益方和对手位置。受益方为可信ResolutionFrame.EffectController，中立事件也不一定等于RootPhasingPlayer。严格落后才推进；相同或领先返回BlockedPrerequisite，没有推进、奖励或成功事件事实。它不是通用ValidateIntent拒绝：头条/普通用牌资格和此后非星号弃置仍由原编排处理。门禁按事件真正开始时的局面重查，不缓存头条选择时的位置。

合法路径固定两格，无普通OPS门槛、骰子或普通尝试消耗/重置；普通尝试次数已用尽不妨碍事件推进。本牌普通Space用途走既有普通用牌服务并抑制事件，不由此参数发起两次尝试。

## 到达、奖励与能力

计划保存两次到达，但第一格VP明确抑制，只按第二格的先到/后到奖励生成一次VP批次。不能把第一格奖励算入中间胜利检查，不能总发先到分。差一格可反超；差两格可到相同位置，均符合卡文。

最终双方位置交SpaceAbilityRules.Query，包含获得与取消。落后p<q保证第一格p+1<=q：该格不能给移动方新的独占能力，但可能取消对手能力。能力由位置推导，不是“一小步永久效果”。继续使用既有头条选择、回合末弃牌和额外行动的调度边界；不重开已锁定头条、不重置次数、不立即插入行动、不新增根slot。

## 越终点与验证边界

OSS-OPEN-01：自己7、对手8满足落后，但两格请求的第二格超出终点。没有核实专门官方裁定，当前overflow_policy=RequiresRulingBeforeMutation；不夹到8、不先移动一格、不改能力/VP/次数/牌区/收据。完整有效位置下这是唯一落后越终点组合。自己已8时不可能落后，门禁失败；这不能解决或覆盖Scientist已8的SS1-OPEN-01，后者保持待用户裁定。

状态预检要求双方且仅双方位置为轨道定义范围内整数；次数为非负整数且所属TurnNumber匹配。计数超过当前能力上限不必损坏，因为对手追到里程碑可令上限下降；已有尝试不回滚。到达记录必须完整、来源/版本一致、每方路径与当前位置一致、没有重复先到奖或冲突奖励收据；每条抑制记录不能伪装未到达。缺失/冲突到达证据停为InvalidState，不按默认值补齐。未知相关干预停RequiresRuling，缺失运行处理器为Faulted。

## 事务、恢复与终局

入口只接受经注册来源验证的帧，校验CardId/EffectId/规则版本/状态版本及EffectController；客户端不能自报步数、奖励、能力、骰点、根玩家或“已验证”标志。先构造不可变完整计划，再由RuleEngine原子提交轨道、到达/抑制收据、VP批次、能力投影和源事件完成/非星号去向；技术错误回滚本次接受步骤。

AdvanceId绑定完整请求摘要与来源帧。已接受同请求返回原收据，不重新算奖；同ID不同请求冲突。存档保存收据、完整摘要和父返回点，不因后续局面变化重算旧请求。重试已完成终局请求可读取既有收据，但不能修改Finished。

第二格奖励产生胜利时，在同一TerminalCandidateDraft收尾已经接受的本事件、源卡及根slot收据后提交Finished；不运行父帧剩余OPS、兄弟事件或新决策，不在已提交Finished后再做Finalize。根行动者/核战责任不因EffectController改变。界面动画不驱动第二次推进，也没有待选隐藏目标；只公开既有允许的轨道与结果，不附带手牌/头条内容。

## 来源与有限验证

[英文Deluxe卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)PDFp12 #80已目视；[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)§5.2/5.5/6.4支持门禁、卡文优先、奖励与能力。FAQ旧Scientist标题下二格问答不当作本牌专门终点裁定。

本批临时检查器只检查新Schema/参数和手写预期的有限状态投影，读取既有轨道/胜负单源数据。其可信帧与到达证据布尔/状态标签是合成输入，不证明真实来源鉴权、完整到达账本验证、事务回滚、保存/重放或网络可见性。实际结果见本批报告；所有runtime案例均planned-runtime/UNRUN。正式库存71Bound31Unresolved；102处理器仍planned，整合离线验证见本批报告，不是生产实现通过声明。


整合主验29结构＋54有限参考＝83新项，1801既有回归，共1884通过。36Schema/39data/21模块、117指针。10runtime未运行，13note38Tools另层；独立完成稿Ready Yes，无Critical/Important/Minor；独立83新项、309基线SHA/8报告SHA、214JSON/36Schema、39数据登记、21模块DAG及117指针和链接核对；未独立运行1801历史/10runtime/13note38Tools。
