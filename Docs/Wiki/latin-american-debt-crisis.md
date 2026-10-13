# 拉美债务危机（LADC1）

本页是 verified-subset 设计候选；没有 C#/Unity 处理器。英文卡图 PDF p14 #95 为 USSR、2 OPS、Late、非星号。印刷事实读取共享 cards；规则版本为 Deluxe2015。

美国先私下获得有效 OPS≥3 的普通 Hand 候选，可弃一张或拒绝。阈值与 Hand/弃牌规则复用已核实的 Blockade 参数；本牌英文也独立核对为相同阈值。报价的实际玩家恒为 US，使用共享 OpsThreshold 聚合：Containment 可使2达标，作用于US的Purge可使3不达标；USSR的Brezhnev/Vietnam不会转移给美国，地区奖励不用于阈值。未知相关提供者、冲突/重复激活或不完整报价先 RequiresRuling。Scoring没有OPS，China独立牌区、保留头条、源InResolution和对手手牌均非候选。

弃置牌进入公开弃牌堆，即使它有星号，也不移出游戏且不触发事件。美国拒绝后苏联选择两个不同的共享地图 SouthAmerica 国家，一次原子接受，将两国当前USSR影响力各乘2；US影响力不动，不做OPS/控制/邻接限制。卡文未要求已有正影响力，因此0×2仍0；这属于明确数学/文本推导，不冒称专门FAQ。空手仍能提交拒绝，不能借此向对手泄露手牌或候选数量。两阶段没有随机、额外实体牌使用或额外根行动槽。

来源只支持核实的 RootEvent、OpponentCardEvent、HeadlineEvent。事件控制者恒USSR；根Phasing保持原值，美国响应者与苏联目标选择者分开。FYP仅触发US关联事件，本牌不符合，明确拒绝；未知嵌套与父方待完成义务停在变更前。当前源必须是牌组唯一CardId/instance、独立InResolution实体，frame/resolution/root与独立dispatch一致；实体、帧或历史成功resolution不可复用。

Begin提交只等待US；拒绝只推进到等待USSR；完整合法弃牌或两国计划才完成事件。非星号源正常进DiscardPile。OpponentCardEvent若还有已核实RootOperations，则事件完成但源仍InResolution、根未完成，不替父层执行OPS；实际根回调仍planned。Headline不增加行动轮名额。当前Finished、DEFCON1、VP终局或winner异常不得开始/继续。

每步的完整内部收据保存原请求、权威源、完整此前局面和结果，并用独立锚绑定。新步骤必须与上个已接受结果完全一致；同命令重试先验证历史并返回原结果，后来live/Finished不被覆盖。不同新命令不能重新使用已接受的dispatch/frame/resolution；未来正常再抽到非星号牌须具备新源帧/新resolution。自改结果后重算本地hash不能代替独立锚，历史结果仍须重新推导。收据与候选只在权威域；对手和旁观者投影不含Hand、候选、报价或候选数。新的当前私有视图也必须核对活跃决策的完整当前局面与已锚定历史；不能把旧收据重放等同后来新增手牌的查看许可。身份、完整提供者、存储、事务和权限均是有限模型的可信前提，不宣称真实认证。

来源：[GMT英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)；[2015规则](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf) §§5.2–5.5、7.4–7.4.2、9.5/9.8；[FAQv5](https://www.gmtgames.com/nnts/FAQv5.pdf) p19公开弃牌。未找到#95专门FAQ。PROJECT-LADC1的选择/零值推导及工程边界与官方事实分列；未来全局登记必须保留Pending而不升级官方裁定。

文件：[参数](../../Data/Cards/deluxe-2015/latin-american-debt-crisis.parameters.json)、[契约](../Contracts/latin-american-debt-crisis.schema.json)、[案例](../Design/latin-american-debt-crisis.cases.json)、[数据描述](../../Data/Descriptors/latin-american-debt-crisis.data.json)。复用[手牌事件](hand-events-batch1.md)和[事件行动](event-operations.md)。10项运行验收均UNRUN；作者离线检查与后续独立审阅分开，不报告累计通过量。

Begin是新历史链的首步，前序命令必须为空；任何存在或不存在的非空前序ID都在提交前拒绝，当前局面、收据与独立锚均保持原样。新正常非星号牌来源在具备新dispatch/frame/resolution/root证据后仍可从空前序开始；后续美国/苏联选择只能连接它自己已接受的链。契约 execution_request 显式描述此要求，历史 receipt_valid 的严格规则保留。

全局状态快照接受 Finished / winner=Draw，以兼容已经裁定的平局终局；相同历史命令仍返回原收据结果并保留后来live平局状态。Draw不是本事件新增结算分支：新执行和当前私有查询仍拒绝Finished，Active下任何非null赢家仍非法，历史收据完整校验不变。

整合主验4605项通过：4454既有＋151本牌（9数据、36契约、106参考）。98 Bound/4 Unresolved、102处理器planned；10运行验收未执行。独立复审与发布状态见[验证报告](../Design/latin-american-debt-crisis.validation.json)。
