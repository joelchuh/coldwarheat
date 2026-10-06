# 联合国干涉：双牌使用契约

description：查本人配牌资格、唯一双实体、配牌OPS与本次事件压制、实际使用证据、一根slot及未核实分支。

状态2026-10-06：#32 Neutral1Early非星号参数Bound，C#待实现。[模块描述](../Descriptors/paired-card-play.module.json)／[数据描述](../../Data/Descriptors/un-intervention.data.json)→[参数](../../Data/Cards/deluxe-2015/un-intervention.parameters.json)、[Schema](../Contracts/un-intervention.schema.json)、[案例](../Design/un-intervention.cases.json)、[验证](../Design/un-intervention.validation.json)。63Bound39Unresolved，102处理器planned。

采用UNPairService生成不可变PairPlan，配对是一个具名授权方式，不把两张牌各开一根行动，也不把配牌当强迫弃牌。规则正常分支从本人普通Hand取UN与不同的仅对方关联Event；自己阵营/中立/Scoring/China不能配。FAQ明确即使配牌事件受前置或禁止条件而不能发生，也可用，因此筛选不调用CanTriggerEvent。没有对方合格牌则事件用途无有效方案，不能白丢UN假装事件；普通单卡OPS用途仍交原CardPlayRules。

正常事件源为NormalActionOwnerHand。UN不是别人行动轮中的即时反应牌。FAQ承认Grain Sales等特定取牌来源，且Headline抽到UN必须返回；当前#67/#49/#85完整来源未设计，只有已注册且验证的来源契约才能进入，不能伪造一个source_verified布尔放行。Headline UN事件禁止；其普通OPS/Space用途是独立普通用牌策略，本文不因#67一般授权而放行未设计Headline子来源。

BuildPairDecision只输出Owner私有合法候选；PendingDecision保存UN意图、RootSlot、来源/版本与继续位置。BuildPairPlan在两实体持有权/当前版本/角色/用途和钩子齐备后，一次移动到InResolution并生成两个唯一ActualCardUse：UN Event，配牌Operations、带PairId/父Origin和UNIntervention抑制原因。实际Use与钩子在同一事务草稿中建立/查询；无法裁定时整个当前接受步骤回滚，不发布Use或半移牌。此牌的事件不入§5.2前后队列，故没有EventOrder选择。每次花OPS不再产生Use，两实体始终一物理位置，重复请求不重新用牌。

预算来源计划先保存CompanionCardId引用，真实有效预算在已核实ActualUse钩子处理后、OperationsStart按当前草稿查询cards.json.printed_ops及修正；OPS Actor从权威帧取；不加UN印刷1，不复制配牌数值到参数。复用共享OPS与普通地面行动，投放/政变/调整由既有模式锁/完整目标历史和报价规则处理；普通政变仍记有效OPS军事信用、受DEFCON/条约/危机，不是free coup，不赋予地理豁免。卡文may支持UN专用结束未花预算的具名分支，此为文本推导，不新增任意跳过根行动。

UN配对Space没有专门核实裁定，pair_space_policy=RequiresRuling；不把Conduct Operations泛化成任意Space许可。普通单卡UN若其有效OPS达到门槛，是否可Space仍按原正常卡用途验证，这不执行UN事件、不需配牌。借OPS不会触发配牌事件或其成功事实；压制只限本次，不永久取消未来效果。普通CardUse外部钩子仍要检查，不能因Suppress掉事件就直接跳过它们。

CardPlayHookService读取两Use与抑制证据：Flower的UN免罚/处罚先后仍按已有hook_policy RequiresRuling；本批不采纳用户未答建议。已核验不适用钩子可正常继续，无法裁决的钩子在当前配对提交前停止。LegalUNEventUseEvidence证明这次合法Event用途供已注册消费者查询；不把单卡OPS算作#50条件。#50本身仍Unresolved，不能借一条接口证明实现它的下一行动VP优先时序。

整个事件及OPS完毕由FinalizePair按既有CardDisposition分别处理：UN正常非星号弃置，配牌Suppressed→Discard，无论其星号；不新增本次配牌成功事实，也不删除已核实历史事实。在产生终局的内部TerminalCandidateDraft中，先收尾已经接受的pair与当前根slot/牌区收据，再与获胜效果同事务提交Finished；候选出现后不继续OPS或调度新行动。已提交Finished状态仍吸收，不允许后置Finalize改变规则状态，也不得接收新UN事件。根slot仅完成一次，服务不自己推进TurnFlow。状态/Use/预算/压制/去向/继续位置和同ID完整请求摘要由编排原子提交与持久化；技术失败回滚当前步骤，不留半移牌。真实事务/原收据/存档尚未运行。

~~~mermaid
flowchart LR
 A[本人UN事件意图] --> B[私有配牌资格与钩子]
 B --> C[双实体唯一InResolution与两Use]
 C --> D[配牌预算引用/本次Suppressed]
 D --> E[普通地面OPS与继续位置]
 E --> F[双牌去向与一次根slot]
~~~

依据[英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)p5 #32、[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)§2.2/5.2/7.4.1–3/7.5、[FAQv5](https://www.gmtgames.com/nnts/FAQv5.pdf)PDFp7 #32。共享政策及卡牌只读，角色/来源由Application和TurnFlow提供，显示主题不裁定配牌或预算。

主18结构＋42有限参考＝60新项，1539旧回归，共1599通过；31Schema/34数据/20模块、102参数指针。8runtime未执行，13note/38Tools另层；模型Authorize把已核实钩子与OPS开始报价合成一个有限步骤，不证明真实阶段/鉴权。独立复审Ready Yes，18数据＋42参考=60实际复跑；终局候选同事务与Finished吸收一致，全部反馈关闭，8runtime未运行。
