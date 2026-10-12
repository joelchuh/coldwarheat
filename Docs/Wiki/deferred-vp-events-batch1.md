# WWBY 与 U2：即时前缀和延后 VP 检查点

description：查 #50 的即时 DEFCON、下一美国行动轮 UN 豁免与 VP 优先，以及 #60 的本回合稍后合法 UN 额外 VP。

2026-10-13 verified-subset 设计。按[数据描述](../../Data/Descriptors/deferred-vp-events-batch1.data.json)→[参数](../../Data/Cards/deluxe-2015/deferred-vp-events-batch1.parameters.json)、[契约](../Contracts/deferred-vp-events-batch1.schema.json)、[案例](../Design/deferred-vp-events-batch1.cases.json)检索。静态 path/symbol:null 为数据入口，所有生产服务 planned，无 C#/Unity。

WWBY #50：先降 DEFCON 一档，立即按根 PhasingPlayer 判核战；发生核战则必要源牌/本根收尾后 Finished，不安装延后 3VP。非终局保存成功事件事实与待检查效果，不立即给 3VP。美国下一行动轮若没有合法使用 UN 事件，苏方先得完整 3VP；该批胜利检查先于 Duck and Cover 等美国事件的 VP，不能两牌净额合并。仅源事件前缀已知时可先提交；未来时点未核实不撤销已提交 DEFCON。

有限已知时点是同回合普通苏方第 n 槽→美方第 n 槽，或头条→本回合美国第一个普通行动槽。读取共享普通先行方及时代槽数，并绑定独立接受根/槽/事件序号。Headline 本身和苏方行动不是目标。美国自身在行动轮触发 WWBY 后的目标、Trap/跳过/额外行动、跨回合或遗漏目标检查点不猜测，依赖这些信息的新 checkpoint 返回 RequiresRuling，保持此前 prefix。它并非声称整个事件从未改过局面。FAQ 的无下一轮分支以最终回合美方最后普通槽的实际来源及已接受 FinalScoring 边界共同证明；不把更早漏执行的待办静默清掉。FinalBoundary 只关闭待检查效果，不代替最终计分；额外槽来源仍待裁定。

U2 #60：苏方立即 1VP，先做独立事件批次胜利检查；终局不再安装效果。否则本回合稍后任一方合法使用 UN 事件时，再给苏方 1VP。此前 UN、普通 UN OPS/Space、下一回合不满足。第一次额外奖励后，另一个本回合新合法 UN 的次数问题尚无具名官方裁定，保留效果与首次证据，新的检查点 RequiresRuling；既有同一 UN 使用的重复通知不再次计分。本批不默认为无限重复或自动取消永久余下资格。

UN 接入的是现有[双牌设计](un-intervention.md)的 LegalUNEventUseEvidence：NormalActionOwnerHand、两个独立先前 Hand 实体、当前恰好 UN 与仅对手关联 Event 配牌、两条 ActualUse（UN Event／配牌 Operations）、同 PairId/Root/玩家/序号和 UNIntervention 压制；独立 ledger 必须一致。无需配牌事件本身可触发。配牌不写成功事实。没有 UN 卡名或一个 verified=true 就能授权的通道。FAQ 确认 Missile Envy、Star Wars、Grain Sales 取得 UN 并有合法苏方配牌可豁免 WWBY；这些特殊来源尚未注册完整契约，当前仍 RequiresRuling，不否认已核实桌游规则。

WWBY 无 UN 的有限观察来源包括直接 Duck and Cover 事件开始之前、已接受本人/中立单卡 OPS 与已接受单卡 Space 用途。两种单卡用途都要求共享卡表 kind=Event；Scoring 和 China 不合法，frame/ledger 一致也不能授权。Space 的目标格、有效 OPS、次数及其他限制由独立已合法验证的上游提供者负责，当前模型不实现完整太空资格验证。它们只投影 WWBY 检查点，不运行后续事件、OPS 或 Space。未知取牌/嵌套来源不能过早断言本轮绝无 UN。合法 UN 同时触及两效果时，先记录 WWBY 豁免，再给 U2 独立奖励；若胜利，已接受双牌按 UN 契约弃置、当前根槽只收尾一次，最后 Finished。其他相关钩子覆盖未知时当前步骤不提交。

即时来源只接普通根事件、对手事件与头条；关联控制者固定 USSR，根玩家独立。两牌非 US 关联，FYP 子事件拒绝；未知父链不推断终结。所有牌 CardId/InstanceId 唯一；单卡 frame 恰有自己，已接受 UN pair frame 恰有两张具名实体。新执行拒绝 Finished、Active winner、核战等级或已达共享 VP 胜利阈值。即时成功星牌正常移出；对手先事件后 OPS 非终局只记录 DeferredRemove，不早移牌。终局先完成已接受源/单卡或双牌的必要去向与本根，再提交 Finished，不结束无关根。

活动效果必须和独立已接受 EventCompleted 事实绑定 ResolutionId、CardId、EffectId、controller、turn、序号和来源根；不能凭 active 布尔、请求自报摘要或当前卡名证明历史。后续 Awarded/Cancelled/Expired 同时匹配独立接受的 hook 结果日志；由 UN 触发的日志保留完整原双牌证据，同 EvidenceId 不同完整事实拒绝。具体槽调度、真实合法双牌/ActualUse 提供者、网络存档和事务未实现。有限参考模型把独立 ledger、接受事实与原收据锚点列为受信合成前提。特别是非 UN 的历史 Awarded/Expired 日志不含当时目标检查点或 FinalBoundary 完整来源，必须假定独立提供者已验证其合法性；本投影只能检查身份、状态、数额和序号一致性，不能据此重建或鉴别原始目标时机。生产适配必须独立建立该前提，任意导入的同形日志不获授权。

原收据保存完整内部 input/result 及参数版本，独立原接受锚点先验。重放在上述受信前提下重验完整原结果及来源用途，不声称验证未保存的更早非 UN 目标检查点；返回原完整副本，不用后来 current 当作原 after，也不覆盖后来 Finished/牌区/VP。新 checkpoint 拒绝只回滚当前未提交草稿；早已接受的即时前缀或首次 U2 奖励仍保留。数字摘要与深拷贝不等于生产鉴权、持久化或事务实现。详细离线检查数与冻结 SHA 在外置 new-only 报告及 handoff，12 个 runtime 场景未执行。

来源：[英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf) p8 #50、p10 #60；[FAQv5](https://www.gmtgames.com/nnts/FAQv5.pdf) pp11–12；[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf) §§4.5C–D/5.2/5.4/5.5/6.4.1/8.1.3/10.2.3/10.3.1。英文图已亲视，邻近译文不用作依据。U2 重复使用及 WWBY 未定时点无新增官方裁定，保留待裁定。

整合状态：[验证报告](../Design/deferred-vp-events-batch1.validation.json)记录160新＋3973旧＝4133离线检查，95 Bound/7 Unresolved，102处理器planned；12运行验收未执行。独立探针另外计数，不重复累计。
