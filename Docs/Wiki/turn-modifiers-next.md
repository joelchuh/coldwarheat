# 后续回合修正：接口与生命周期蓝图

description：查勃列日涅夫主义的OPS归属，以及拉美死亡小队的地区政变骰/对方重触发替换与验收计划。

状态2026-10-06：设计蓝图，尚无本批正式参数、Schema或运行处理器。#51/#69仍Unresolved，不增加60 Bound /42 Unresolved库存。[JSON摘要与契约意图](../Design/turn-modifiers-next.plan.json)→本页→既有[OPS与生命周期](persistent-effects.md)和[政变前干预](defcon-events-batch1.md)。[记录验证](../Design/turn-modifiers-next.note-validation.json)只验证设计笔记引用与状态，不重跑/增加1465游戏设计检查。

采用两类封闭修正提供者，共用ActiveEffectService生命周期，分别供OpsModifierService与CoupInterventionService查询。相比逐卡另写OPS/政变公式，提供者只返回具名delta与来源证据；相比一个通用数字加成通道，分开OPS和政变骰可避免把骰子加成算成军事信用、太空门槛或投放预算。全部入口尚待实现，文字蓝图不能调用。

勃列日涅夫主义#51固定USSR，当前回合后续有效OPS加1。读取现有persistent_batch1的ops_policy，不复制上限/下限。实际OpsActor决定受益：US偷到USSR牌不继承修正；USSR用US关联牌做自己的OPS仍按自己的已生效修正报价。合并Flat得到R，先应用普通上限，再加合法地区奖励B，最后应用共享下限；即effective=max(minimum,min(ordinary_maximum,R)+B)；Space与OpsThreshold不拿地区奖励，头条排序仍印刷OPS。事件视同OPS授权遵循R2015 §7.4.3；直接影响力、固定战争军事、VP不自动修改。已开始/完成的报价上下文不追溯重算，未核实中断恢复停止。

拉美死亡小队#69中立、非星号，可信EffectController在中/南美政变骰+1、对方在同区域-1，本回合余下时间有效。FAQ明确对方减值也仅这两区。使用实际CoupActor和world地区，根PhasingPlayer、卡牌归属或地图视觉位置不替代。包括合法事件授予政变；不影响战争、调整、Space、OPS预算或军事信用。先验证基础合法性，危机下真正接受的致命政变先结束游戏；没有机会靠骰加成挽救。

冻结英文卡面只明确双方±1，未发现对方再次触发的专门覆盖/叠加裁定；旁列译文不能补成英文规则。TN-OPEN-01当前RequiresRuling，已询问用户，未获答复。建议的PROJECT-TN工程方案为对方再次触发时生成完整替换计划：旧实例Cancelled并记明确替换原因，新实例Active且以新控制方给双方±delta。若方案获确认，则只允许一个活动配对，旧USSR-1不再与新USSR+1叠加。此为候选解释，不是当前已采用规则。取消历史审计保留；所有实例/角色/来源和当前回合在同事务提交。同方新结算ID重复仍沿既有RequiresRuling；反方路径亦等待确认，不以未核实反转规则放行；同ID真实重试只返原收据。

SALT的双方-1与地区骰修正合并，读取其coup_modifier渠道；不修改OPS或军事值。实际原始骰面仍由普通政变管线验证，修正后的计算结果不再强行夹到1–6。此处只计划返回delta，不新增另一份骰子范围常量或单独掷骰入口。

两类效果按既有ExpireAndReset到期；卡牌星号去向来自cards.json，与活动实例保存独立。#51星号、#69非星号事实保留；不能把牌已弃置当作回合效果已到期。保存sourceResolutionId/版本/摘要/激活序列与替换审计，UI只收只读修正说明。下一步制作persistent_batch2参数/闭合Schema、数据与有限参考案例，再全旧回归和独立完成契约审阅；本蓝图的审阅不代替那次审阅。

六项[验收计划](../Design/turn-modifiers-next.plan.json)仍planned-runtime。依据[卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)p8/p11、[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)§7.4.1–3与[FAQ](https://www.gmtgames.com/nnts/FAQv5.pdf)PDFp14#69。未知同方重复/其他交互保持停止；Pope/Scientist/Flower未答事项不受本蓝图影响。
