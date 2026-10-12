# 晚期手牌事件：伊朗人质、恐怖主义与Ames Remix

description：查询固定伊朗影响力与永久加倍、整批随机弃牌、Ames本回合当前手牌视图及不可变历史。

2026-10-13 verified-subset正式设计。按[描述](../../Data/Descriptors/late-hand-batch1.data.json)→[参数](../../Data/Cards/deluxe-2015/late-hand-batch1.parameters.json)、[契约](../Contracts/late-hand-batch1.schema.json)、[案例](../Design/late-hand-batch1.cases.json)检索。入口静态path/symbol:null仅为数据检索；所有运行服务planned，无C#/Unity。

#82 Iranian Hostage Crisis按顺序清除伊朗全部US影响，再给USSR加2，已有USSR影响保留。零US不跳过后续。影响修改、成功事实和永久Hostage实例一个草稿提交。它仅使对US的Terrorism弃牌数加倍；不是所有弃牌加倍。新ResolutionId重复激活尚未核实，初始停止RequiresRuling；旧命令重放独立处理。

#92 Terrorism由可信普通中立来源的控制者使对手随机弃一张；USSR控制且核实Hostage生效时US随机弃两张，US控制时USSR仍仅一张。FAQ p16确认额外牌也随机。候选是当前普通Hand，包括计分牌；China、源InResolution、HeadlineReserved和其他区排除。稳定CardId排序，每次权威均匀NextInt后移除所选候选，无放回；一次逻辑抽样/单候选及顺序为工程约定。完整数量、候选、所有抽样及最终游标预检后整批提交。第二抽样非法不保留第一张、不耗已提交RNG。客户端不能指定种子、索引或牌。

被弃牌无论阵营/星号/计分，均Hand→公开DiscardPile；不触发事件、计分、OPS、CardUse或新根名额。本批不复制FYP的US子事件例外，也不继承其空手完成规则。Terrorism空手或两弃只剩一张、Ames初始空手尚无专门裁定，均在任何随机、揭示或激活前RequiresRuling，不按最大可完成量默认执行。

#98 Aldrich Ames Remix先建立本回合US→USSR手牌揭示权限、提交当前Hand快照与USSR强制选一弃牌决定，再等待合法答复。接收者和选择者固定USSR，即使根玩家US。旧版排序、Quagmire顺序、旧Ask Not保留牌排序及UN限制全部不导入；FAQ p16明确旧问答不适用Remix。

Ames期限作用于手牌集合；同回合后来进入普通Hand的牌进入独立当前视图查询，是卡文期限直接派生，非专门FAQ。历史收据快照绝不追加/刷新。查询检查权威版本、活动实例、来源事实、激活回合和USSR身份；下一回合、Expired或Finished不给新当前视图。历史合法看到的内容不会因权限结束被当作未曾看到。真实回合边界撤销、存档、网络与生产访问服务待实现。等待决定时非法/陈旧选择不撤销已提交揭示；完成只弃选中Hand一张，不能Decline或重排未来出牌。

来源保留EffectController、RootPhasing、固定手牌Owner和DecisionOwner。仅RootEvent、OpponentCardEvent、HeadlineEvent：前两者只ActionRound，Headline只Headline且无OPS；关联牌controller来自印刷，Terrorism中立controller对应根玩家。源实体独立核对InResolution、CardId、owner和唯一frame；InstanceId/CardId均唯一，China独立牌区。未知父链/其他接受收尾义务/提供者在披露前停止；三牌均非US关联，FYP子路线无效。已有Finished、DEFCON核战水平或胜利VP状态拒绝新命令。正常完成源去向及本根槽；对手先事件后OPS保留源InResolution与延迟移出，不能提前消耗根槽。本批无VP/DEFCON/军事原语，未知终局提供者不臆造父帧收尾。

活动Hostage须绑定独立已接受EventCompleted事实；Ames在等待弃牌时可绑定已提交DisclosureCommitted事实，不能只用一个active=true。重复/损坏/未知相关激活停止。权威完整手牌与来源证明是有限模型的可信合成前提，字符串proof或布尔值不是鉴权实现。

同一ResolutionId在所有激活事实与活动实例中必须绑定同一CardId、EffectId、controller和激活回合。同牌同身份可有DisclosureCommitted、EventCompleted等不同阶段；StepId为不透明标识，不凭命名推导合法性。跨牌共用结算编号或身份矛盾在披露、弃牌和RNG推进前InvalidState。

新的CurrentView查询与新事件执行共用合法活动局面约束：已有winner、共享核战DEFCON水平或任一方向达到共享自动胜利VP阈值的Active状态均InvalidState，且无私人输出；Finished仍Unavailable。此检查仅针对新的当前查询，不把后来终局状态施加到合法历史收据重放。

历史收据保存原完整内部输入、结果与不可变摘要，原接受索引独立锚定。重放验证原历史而不重算live手牌/RNG，不覆盖后来局面；选择历史同时绑定原等待收据。内部权威记录包含私有牌，不直接作为玩家响应；ProjectReceipt只输出公开接受弃牌和获授权的Ames历史快照。CurrentView是另一个新查询，不能借旧receipt刷新。拒绝与未提交计划没有私人输出。摘要、内存复制和离线序列不证明生产鉴权、均匀随机、持久性或原子事务。

共享地图、印刷属性、China、VP/DEFCON、Hand/随机模式和TurnFlow/ActiveEffects接口通过参数引用；不在此复制共享常量或历史全局计数。[整合验证报告](../Design/late-hand-batch1.validation.json)：173新＋3446旧＝3619离线检查，89 Bound/13 Unresolved，102处理器planned；12个runtime案例保留UNRUN。

来源：[英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)p13 #82、p14 #92、p15 #98；[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)§§5.2/5.4/5.5/7.3/9；[FAQv5](https://www.gmtgames.com/nnts/FAQv5.pdf)p16 #92及#98版本声明、p19。来源备忘work/late-hand-research.json只作为研究入口，不是运行规则。
