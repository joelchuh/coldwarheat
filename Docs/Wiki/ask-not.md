# Ask Not（AN1）

verified-subset 正式设计参数；无C#/Unity或真实随机实现。稳定CardId为 card.ask_not_what_your_country_can_do_for_you，共享卡表读取#77 US3 Mid星号。

固定US选择普通Hand的完整子集，零至全部、包括Scoring。排除China、已保留Headline、其他人的Hand、源InResolution、Deck和Removed。整个集合先锁定并一次弃置，弃牌无事件；然后只抽同样数量，不补到8/9，不允许看一张再追加弃牌。零选择按may/up-to及§5.2作为事件成功无变化推导，仍完成源星号去向；没有Draw调用或随机推进。

正数选择提交后是WaitingDraw：源与未完成FYP父仍InResolution，根未完成。权威DrawProvider先按已有Deck顺序抽取，再按R2015§4.3/4.3.1空堆触发重洗全部当时Discard。恰好抽足同时耗尽旧Deck也重洗，但不额外抽；这是通则组合推导，不冒称专门FAQ。本次弃牌已入Discard，可在洗后重新抽到。AskNot源和仍在解析的FYP父不在洗牌池；父收尾在完整Draw接受之后。EraReserve不在池中，不提前插入Mid/Late。

有限模型只核实一个绑定request/state/pool的全排列及前后RNG令牌；排列来源、均匀性、真实随机算法、存储和事务均planned。请求数恰等于已锁集合大小；原Deck不足时本次弃牌加入池，使合法完整牌区有足够候选。未知牌区完整性/提供者容量在弃前停止；回调证据无效则保留已提交的锁和弃牌，不重复弃、不私自生成牌。相同集合不按任何桌游8/9上限截断。

支持核实的RootEvent、OpponentCardEvent、HeadlineEvent及具独立父实体/帧/选择凭据的FYP US子事件。FYP父controller固定US且不等于任意根Phasing；源AskNot从USSR被弃后InResolution，US仍交换自己的Hand。父已完成、源实体/CardId/帧/resolution重复、未知StarWars/GrainSales或剩余父工作停下。Opponent剩余OPS时保留源到根收尾；FYP子牌成功后移出，父剩余OPS则保留父，父无剩余才按印刷去向收尾并只完成一个根槽。

当前私有Hand/选择视图与历史结果分离。US可看自己的候选和补牌；USSR/旁观者只能见已提交公开弃牌及阶段，不能见未选Hand、未公开Deck、排列或完整内部收据。新得到的牌可以由已建立的Ames当前视图消费者另行查询；旧Ames快照永不刷新。本模块不凭Ames字符串自行开放USSR视图。抽到Scoring不产生扣留豁免，继续用既有回合末胜负规则。

Begin前序必须为空，后续命令连接自己已锚定的上一步完整状态；任何成功写入必须有可验证收据。相同命令历史重放返回原结果且不覆盖后来live/Finished。变化的选择、状态、来源、排列或重算hash不能代替独立历史锚。未知provider保守停下；有限proof ID不是生产身份认证。

来源：[英文卡图p12#77](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)、[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf) §§4.3–4.4、5.2/5.4、9、[FAQv5 p15#77](https://www.gmtgames.com/nnts/FAQv5.pdf)。旧#98排序问答明确不适用Deluxe Remix，不用于本设计。PROJECT-AN1工程与派生标记单列，未来全局保留Pending。

文件：[参数](../../Data/Cards/deluxe-2015/ask-not.parameters.json)、[Schema](../Contracts/ask-not.schema.json)、[案例](../Design/ask-not.cases.json)、[数据描述](../../Data/Descriptors/ask-not.data.json)。复用[手牌事件](hand-events-batch1.md)、[开局牌区](setup.md)、[后期手牌当前视图](late-hand-batch1.md)。10运行验收UNRUN；离线作者验证不等于独立审阅或完整Deck引擎。

全局状态快照接受 Finished / winner=Draw，以兼容已经裁定的平局终局；相同历史命令仍返回原收据结果并保留后来live平局状态。Draw不是本事件新增结算分支：新执行和当前私有查询仍拒绝Finished，Active下任何非null赢家仍非法，历史收据完整校验不变。

整合主验4760项通过：4605既有＋155本牌（8数据、28契约、119参考）。99 Bound /3 Unresolved，102处理器planned；10运行验收未执行。独立整合审阅与发布状态见[验证报告](../Design/ask-not.validation.json)。
