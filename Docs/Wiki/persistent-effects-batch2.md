# 持续修正第二批：OPS与地区政变骰

description：查Brezhnev固定USSR的实际OPS报价、LADS中南美双方政变骰、精确到期及重复待裁定边界。

状态2026-10-06：#51 USSR3Mid星号、#69 Neutral2Mid非星号，参数Bound；62 Bound /40 Unresolved、102处理器planned，C#/Unity未实现。[描述](../../Data/Descriptors/persistent-effects-batch2.data.json)→[参数](../../Data/Cards/deluxe-2015/persistent-effects-batch2.parameters.json)、[Schema](../Contracts/persistent-effects-batch2.schema.json)、[案例](../Design/persistent-effects-batch2.cases.json)、[验证](../Design/persistent-effects-batch2.validation.json)。先前[接口蓝图](turn-modifiers-next.md)是历史笔记，不能替代本批契约或实际运行。

Brezhnev复用ActivateTurnOpsModifier：affectedSide=USSR、Flat+1、本回合后续OPS，普通上限/下限只查persistent_batch1 /ops_policy。合并Flat得到R，先普通上限，再合法地区奖励B，最后共享下限；effective=max(minimum,min(ordinary_maximum,R)+B)。Actor归属不随实体卡牌转交：US偷到苏联牌无USSR加值；USSR做自己的OPS时仍获自己的加值。Space/OpsThreshold没有地区奖励，头条排序按印刷OPS；事件视同OPS受R2015 §7.4.3修正，直接影响力、VP和战争固定军事不加1。只改后续新报价，不追溯已完成预算。

LADS复用活动实例＋政变干预的独立骰渠道：可信ResolutionFrame.EffectController在中/南美政变+1，实际对方CoupActor同区-1，其他地区0。正式地图地区查询，不读视觉位置；普通/已核实事件授予政变均适用，战争、调整、Space、OPS与军事信用不使用此渠道。FAQp14 #69明确对方处罚也仅中/南美。根PhasingPlayer不替代实际Actor。基本合法性/危机致命检查在前，真正接受致命政变立即结束，不进入骰修正或随机后续。无其他合法性/DEFCON豁免。

SALT与LADS骰项按同一渠道合并，+1与-1可抵消；普通OPS数与军事信用独立。提供者只返回已核实delta/来源，不自行掷骰。原始骰仍普通政变管线验证，修正后结果不夹到原始骰面范围；不因此给LADS另建一份骰子常量。

生命周期RemainderOfActivationTurn/ExpireAndReset：激活序列/来源帧/回合/版本入权威实例。报价查询当前位置与当前活动状态，错位的上回合Active实例InvalidState，不静默当未生效。到期、牌区和行动计数各由原边界处理；#51星号事件实际发生移出，#69非星号弃置仍可本回合生效。模块只生成计划，由编排事务统一提交，不回调可变服务或等待动画。

重复边界：同ResolutionId且完整请求匹配只返原收据；同ID不同请求Conflict。Brezhnev同方新ID重复沿既有RequiresRuling。LADS同方、反方新ID重复均RequiresRuling，整次事件不修改实例。PE2-OPEN-01建议反方覆盖旧配对，old Cancelled记来源、new Active，只有一个活动pair，但用户未答前不采纳。proposed_resolution是记录候选，不是可执行默认分支；英文卡面没有专门的反转/覆盖句，旁列译文不可补成规则。未知相关效果/未核实OPS中断恢复停止，不改旧预算或叠加。

未来接口仍ActiveEffectService.Activate/ExpireAtBoundary、OpsModifierService.Quote与CoupInterventionService.BeforeAcceptedCoup，同一shared policy/地图，两个修正通道分离。事件激活无玩家选择/随机/VP/影响力/军事/DEFCON或额外根名额；实际政变后续走既有管线。Save/replay应保存实例状态、来源与完成/Quote证据，不把布尔fixture当鉴权实现。画风只决定如何显示公开修正说明。

依据[英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)p8 #51/p11 #69、[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)§7.4.1–3/6.3与[FAQ](https://www.gmtgames.com/nnts/FAQv5.pdf)PDFp14 #69；PROJECT-PE2只工程边界和待裁定记录，未冒称官方覆盖规则。

主18数据＋56参考＝74新项，1465旧回归，共1539通过；30Schema/33数据/19模块、99参数指针。7runtime未运行，13笔记与38Tools检查另层；独立完成契约审阅Ready Yes，无Critical/Important；新Schema meta及18数据/56参考独立通过，旧1465未独立运行；模块边界与历史索引P3已主侧修正。
