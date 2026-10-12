# Olympic Games：参赛或抵制

description：查主办方与对手分支选择、平手强制重掷的双骰收据链、有限执行片段，以及抵制先降DEFCON再可选普通OPS的顺序。

状态2026-10-12：#20单牌参数设计，处理器planned，C#/Unity未实现。按[数据描述](../../Data/Descriptors/olympic-games.data.json)→[参数](../../Data/Cards/deluxe-2015/olympic-games.parameters.json)→[Schema](../Contracts/olympic-games.schema.json)→[案例](../Design/olympic-games.cases.json)检索。本批不依赖其他未发布批次的数据，也不报告全局绑定或旧回归数量。

## 来源与角色

Olympic Games是Neutral事件。Sponsor来自可信EffectController；Opponent和最初BranchDecisionOwner都是其对手。RootPhasingPlayer独立保留，OPS Actor则是Sponsor。正常根出牌/本牌头条由对应出牌者主办；特殊嵌套不能只因数据类型叫VerifiedNestedEvent就成立，必须另有已绑定提供者和可核实根身份继承。本批没有授权Grain Sales、Star Wars、Missile Envy等未绑定提供者，也不能用五年计划的“US关联事件”路线触发这张Neutral牌。

来源契约记录CardId、SourceInstanceId、EffectId、Resolution/Frame/Parent/Root、phase、provider、ruleset/version/data digest。独立权威CardInstanceLocation必须恰好匹配该实体/卡牌/本帧且实际在InResolution；不能只相信来源载荷自报的SourceZone。缺失、重复实体、Hand里的源牌、wrong frame或未知provider在选择、随机、轨道修改前停止。RootEvent只对应ActionRound，HeadlineEvent只对应Headline；已到核战等级、已有胜利VP或损坏运行状态不继续。

初始编排先保存由Opponent拥有的Participate/Boycott二选一决策。客户端只提交身份已绑定命令、DecisionId、Revision、选择及来源摘要，不能提交骰点、主办方、奖金、OPS或最大重掷次数。Schema负责形状，运行层还必须对照当前权威决策、source bundle、原始请求和已提交收据校验；可信布尔和proof ID只是离线前提，不构成真正鉴权。

## 参赛：每对骰独立原子提交，平手继续

双方各掷一颗共享标准D6，只有Sponsor的总点数增加卡文修正。比较修正后总数，较高者取得卡文VP，完整事件VP批次闭合后立即检查共享胜负阈值。对手可以赢。参赛本身没有OPS、军事或DEFCON变化。

平局必须重新掷双方骰，不能结束为无奖平局、让玩家改选抵制、只重掷一方、采用最后一方为胜者或设置最大重掷次数。US→USSR的固定取样顺序属于确定性工程约定，不影响规则胜负。

接受Participate与第一对完整骰、派生结果、RNG位置、VP/继续位置同一次提交。若第二骰非法或事务失败，第一骰也不提交，原分支决策仍保留；不能在没有完整首对结果时锁住一个半结算状态。第一对平手成功提交后，Participate永久锁定，只存在内部下一轮继续，不再有玩家分支选择。

每对保存不可变PairedRollReceipt：完整来源和原请求摘要、从1开始的RoundIndex、PreviousReceiptDigest、双方原骰/总数、Sponsor、Tie/Winner、Winner/VPDelta、RNG前后游标和CommittedRevision。两个原骰必须都在共享D6范围，数值为整数；卡文修正后的总数可以超过原骰上限。不能把修正总数夹回6。

摘要使用UTF-8、按键排序、紧凑JSON、数字仅整数的封闭表示计算SHA256。读取时重新计算摘要，也重新从原骰/主办方推导结果，并验证完整链的轮次、前收据、来源、原请求、RNG连续性和Revision。把错误赢家或跳轮后的内容重新哈希仍然无效。摘要仅用于一致性检测；它不是签名、权限证明或防篡改存储的实现。

若下一对输入或提交失败，只撤销这一对未提交结果。已提交平手收据及其RNG位置保持，禁止从最初种子重掷。相同已接受Command/Resolution的原语义请求重试，经过身份与来源绑定核对后返回原收据，不拿当前状态重算；同ID不同选择/原请求摘要为冲突。当前内部继续请求还必须匹配当前RoundIndex、PreviousReceiptDigest、RNG、Revision，不能拿旧继续点推进新轮。

## 连续平局与执行资源

规则允许任意多次平局；实现不能用一个永不返回的循环占住会话。调度器给一次执行片段分配正数的“最多处理几对”工作预算，这个预算不存作游戏的最大重掷数，也不影响概率或赢家。

片段耗尽时，在最后一对已提交平手之后保存InternalPairedRollContinuation并让出执行。继续记录含下轮索引、前收据摘要、RNG、Revision、原来源和原请求摘要，PlayerDecision固定为空。恢复后验证整条已提交链，从下一对开始；没有可点的“继续参赛/抵制”按钮。网络等待、保存和动画不决定是否重掷。

例如US主办，原骰依序(1,3)、(2,4)、(4,5)，总数为(3,3)、(4,4)、(6,5)：两次平手后US得2VP，总计六次逻辑取样。分三次各处理一对或一次处理三对，结果和游标相同。离线JSON往返仅演示值保存，不证明真实持久化或网络恢复。

## 抵制：先降级与终局，再给OPS

对手选择Boycott时，先按卡文降低DEFCON，然后立即执行NuclearWarReached检查。到核战值时RootPhasingPlayer输，无比赛骰、无VP、无后续OPS grant。R2015的直接例子是US打本牌且DEFCON2，USSR抵制，US立即输；不能先让US用OPS改善局势，也不能把责任转给选择抵制的对手。

未终局时，抵制选择、已降DEFCON与Sponsor的可选普通OPS授权一起提交到合法等待点。之后非法目标、越权、陈旧回复或Decline都不会撤回已提交降级。原分支不能改成Participate。

基值为卡文`VerifiedEventLiteral`，不能读取本牌印刷OPS来代替。Actor修正、一般上下限与条件区域奖励读取共享`ops_policy`：清洗后的预算为3，遏制/勃列日涅夫单独作用仍受普通上限；符合条件的越南地区奖励按共享历史政策处理。对手修正不转交Sponsor，不借中国牌专属源牌奖励。

允许Influence、Coup、Realignment或Decline，不消耗另一张卡或新根行动。首次合法操作锁模式；Coup一次花整份预算，Realignment逐次选目标，Influence沿用普通费用与可达性。有限参考只验证授权/预算/门禁计划，完整行动算法仍属于[module.event-operations](../Descriptors/event-operations.module.json)及[普通OPS](operations.md)。已接受操作之间Finish放弃余量沿用既有EO1工程推导，不冒称专门官方裁定。

这是`OperationsCoup`，非FreeCoup。按实际Actor的有效军事信用计入，保留普通DEFCON地区限制、条约、敌方影响力要求与已核实干预；古巴危机在基本合法企图被接受后、随机/军事/影响力/DEFCON之前判实际Actor输。普通核战仍归根Phasing负责。所有未识别的相关干预必须停止，不能因事件赠送行动而绕过。

不能太空。抵制没有取得另一张可弃实体，也没有Grain Sales明确允许play收到牌的例外；正在结算的本牌不能同时被弃作太空尝试。

## 影响力起点与完成边界

正常RootEvent/ActionRound引用RootActionRoundStart里的实际Actor快照；不因抵制和授权创建而重新拍当前局面，不因逐点新影响力滚动扩大可达性。Headline或特殊嵌套需要另行核实的具名baseline policy。OpsGrant保存来源上下文并校验phase/source_kind与baseline关系，Headline不能带RootActionRoundStartActor；缺少已核实起点则仅Influence查询/选择RequiresRuling，其他合法模式与Decline仍可用。该未核实分支标为OG1-OPEN-01，不默认GrantCreation。

事件完成或终局草稿中，由[TurnFlow](turn-flow.md)按正式卡牌印刷去向清理源牌、完成一次根slot并关闭可继续位置。卡牌定义是非星号，事件参数不复制移出标志。参赛/抵制/可选OPS都没有额外卡牌或根行动；核战或VP终局截断后续父OPS和另一头条。Finished提交后是吸收态，只允许经过授权过滤的原收据查询/重放，不做事后补写收尾。

## 来源、检查与未实现事项

已亲看[GMT英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf) PDF p4 #20，使用英文的主办修正、胜者VP、平手重掷和抵制顺序。[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf) §8.1.3提供直接抵制核战例；§2.0、§4.2/4.5C、§6.0/6.1.1、§7.4.1–3、§10.3.1提供共享规则依据。[FAQ v5](https://www.gmtgames.com/nnts/FAQv5.pdf) PDF p13 #62重申Olympic根责任例与CMC区别，p14 #67解释事件OPS的太空限制。旧版“事件OPS不受修正”的答复不覆盖本版as-if卡文。

每对骰原子提交、收据链、片段让出、身份/摘要绑定均是工程设计，非专门官方保存API。当前来源未解决Headline/特殊嵌套Influence起点；有限工作预算也不是新增规则。临时checker通过151项（25参数、65契约、61参考），另核对共享引用与印刷卡事实；参数已Bound；完整主验见[本批报告](../Design/olympic-games.validation.json)。10项runtime验收未运行，游戏测试执行数0，整合主验另复跑旧2678，共2829项；独立新151及18个额外探针分列，后者不计入总数。

[返回索引](index.md)

审阅补强：恢复每对骰前验证初始VP非终局、初始DEFCON合法且参赛期间未变，以及当前阶段的VP/胜者一致性。InternalRoll带赢家或终局分数一律拒绝且不耗随机；合法最终胜者收据仍可只读和重放，不能继续掷骰。正常RootEvent/ActionRound不能用其他具名快照代替实际Actor根起点；缺证仅Influence待裁定，头条仍可用独立具名证明。数据描述按目录惯例列静态路径与null symbol，不代表运行函数。
