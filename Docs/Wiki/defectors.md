# Defectors：头条取消与苏方用牌 VP

description：#103 的选中头条优先、苏方自取消、普通行动轮奖励及来源/去向/重放边界。

状态：verified-subset正式设计；已选头条有限运行时正在实现，进度见[运行时知识点](defectors-headline.md)。以下离线设计的普通VP/嵌套等范围仍planned。[描述](../../Data/Descriptors/defectors.data.json) → [参数](../../Data/Cards/deluxe-2015/defectors.parameters.json)、[契约](../Contracts/defectors.schema.json)、[案例](../Design/defectors.cases.json)。数据入口 symbol:null 是检索入口。[整合报告](../Design/defectors.validation.json)：169新＋3619旧＝3788离线检查；90 Bound/12 Unresolved，102处理器planned，游戏runtime0。

英文 #103 为 US 2 OPS、Early、非星号；印刷事实只读共享 cards。美国选作头条时，以具名 USSelectedDefectorsBeforeOrdinaryPrintedOpsOrder 策略先于普通 OPS 排序，取消仍待执行的苏方选中头条，包含计分牌。取消目标弃入 DiscardPile，即使原牌有星号；它没有成功事件事实。源 Defectors 本身成功、非星号弃牌。不能把取消当作目标事件成功后再移出。

苏方自己选 Defectors 作头条时自取消，0VP，源牌弃牌，不写成功事实，不取消美国独立头条。美国头条可能尚未执行或已经完成；本候选保留其原根及实体，不额外发明苏方自取消的全局排序优先规则。普通 US-selected 分支若收到苏方已完成的输入则拒绝：已违反其选中头条优先前提，不能倒转历史。

普通 USSR ActionRound 的 OpponentCardEvent 奖 US 1VP，来源的实际 CardUse 玩家是 USSR，事件控制者是印刷 US。事件先于 OPS 时保存 DeferredDiscard 和原根剩余 Operations；事件在 OPS 后完成则同次收尾源牌及根 slot。该 1VP 达共享自动胜利阈值时，在未提交终局草稿中截断剩余 OPS、弃源牌、完成当前根 slot，最后写 Finished。US 自己执行事件得 0VP，属于成功但无变化的事件。US 用作普通 OPS 与双方 Space 路径只是 Defectors hook 的 NotTriggered/0VP 投影，状态保持不变；实际 OPS、Space RNG/奖励/去向/根收尾由共享流程执行，不在这里伪实现。

FAQ 的实际顺序规则仍保留：Grain Sales 抽出 Defectors 并真正先于未执行 USSR 头条时可以取消；若 USSR 头条已经执行，则不能撤销。当前 GS/其他嵌套来源没有注册的独立来源契约，统一 RequiresRuling，任何 mutation 前停止。不能用 trusted=true、EventControllerUS、卡名、父 frame 自报或摘要冒充来源授权，也不能用普通 US-selected 的优先策略把已经在后面发生的嵌套事件提前。

设计入口消费[回合编排](turn-flow.md)的已接受 CardUse/选择队列/来源 ledger，与 current 源实体、根 action、phase、phasing、slot、frame、resolution 和剩余步骤逐项匹配。头条两边根、frame、resolution 各自唯一；源/目标 InstanceId 和基础 CardId 唯一，每个活动 frame 恰有本张实体。离开 InResolution 的牌不得还关联 frame。取消和成功历史不能被新的 command_id 绕过；目标已有成功或取消事实就不能再取消。独立 ledger、内部 caller、存储 anchor 是有限模型的明确可信前提，不是已实现的生产鉴权。

Active 已有 winner、VP 已达到自动胜利或 DEFCON 核战等级均拒绝。已提交 Finished 吸收新执行，仅可返回经独立原接受锚点校验的原命令收据。未知相关提供者先 RequiresRuling。源牌和被取消牌在同一计划内完成必要去向，不留下已完成 frame 的第二张实体；未知父链不推断收尾。

收据保存原完整 request、before、result、各摘要及参数版本摘要；重放依据原 before 重新验证历史转移，再核独立存储锚点，返回原完整收据副本。后来 current 的 VP、阶段、卡区或 Finished 不能被覆盖，也不要求等于原 after。修改旧 result/trace/source 或重算自报 hash 不替代旧锚点。离线深拷贝/摘要未验证真实并发、持久化或事务。

作者验证：169 个新离线检查（9 data、21 schema、139 reference）通过，另列 10 个 runtime 案例未执行。先记录未实现投影的 RED，再补一次普通手牌携带 foreign frame 的 RED 后收紧实体约束。历史结果的反例既测旧独立锚点，也用合成匹配锚点测重新投影的领域不变量。独立审阅复跑169并增加129探针全部通过；探针单列，不计入3788。10运行验收仍未执行。

来源：[英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf) p6 #103（目视英文）；[FAQ v5](https://www.gmtgames.com/nnts/FAQv5.pdf) p1 勘误、p14 #67、p18 #103；[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf) §§4.5C/5.2/6.4.5/10.2–10.3。队列、实体、收据和终局草稿是工程契约，不冒称专门 FAQ。

全局快照结果域现允许 Finished/Draw；本事件不因此新增平局分支。主入口的新行动仍拒绝终局或活动局面的非空赢家，历史重放保持原结果与当前状态。范围与实际证据见[全局结果契约](draw-global-contracts.md)。
