# 中国牌事件转交：文化大革命、尼克松与乌苏里江

description：查持有者分支、独立China状态、面向、VP/亚洲完整分配及原收据重试。

状态：verified-subset参数已绑定，运行服务planned。[描述](../../Data/Descriptors/china-transfer-batch1.data.json) → [参数](../../Data/Cards/deluxe-2015/china-transfer-batch1.parameters.json)、[契约](../Contracts/china-transfer-batch1.schema.json)、[案例](../Design/china-transfer-batch1.cases.json)。静态dataset入口的symbol:null用于检索，不代表运行函数；整合主验见[验证报告](../Design/china-transfer-batch1.validation.json)：233新项＋2829旧项＝3062离线检查；83 Bound/19 Unresolved，102处理器均planned。10项本批运行验收未跑。

| 卡牌 | 对手持有中国牌 | 受益方已经持有 |
|---|---|---|
| #58 Cultural Revolution | USSR取得FaceUp | USSR得1VP |
| #71 Nixon Plays the China Card | US取得FaceDown | US得2VP |
| #76 Ussuri River Skirmish | US取得FaceUp | US在亚洲加总4影响，每国最多2 |

三牌均Mid、星号；印刷OPS/阵营/去向从cards.json读取，事件奖励是英文卡文独立事实。判定只看当前holder，不看FaceUp。自己持FaceDown仍走奖励分支且不翻面，这是卡文条件直接推导，无专门FAQ。分支在真正事件步骤采样，不在选牌/头条揭示时冻结，也不让玩家任选。转交、VP和加点互斥。

中国牌继续使用turn-flow的独立ChinaState，保留唯一实体/holder/face/revision，不进入普通Hand、牌库、弃牌或Removed。FaceUp只恢复可用资格，不立即插入出牌、不强迫使用、不新增根行动/OPS。正常回合末ChinaReady、使用后FaceDown转交及头条/计分义务/弃牌限制复用既有中国规则。事件明文FaceUp可覆盖先前正常使用造成的FaceDown；不另加once-per-turn限制。自己已有分支不修改China最近转交事实。

Ussuri加点由US完整选择，根出牌者即使USSR也不能代选。CountryId唯一、正整数、亚洲总4、每国≤2；亚洲成员取world.json，包括东南亚。不是OPS投放：无需RootActionRoundStart或GrantCreation邻接快照，不受敌控双倍费用、遏制/清洗/中国亚洲奖励影响。不能Decline、少放或把同国拆行绕过cap。全零US影响、敌控国都可加；缺地图是数据错误。未知未来影响力提供者先RequiresRuling，不能借用其他牌短缺裁定。

来源以内部接受的frame ledger与独立实体状态逐项绑定：CardId/InstanceId、InResolution、frame/resolution、root action/phase/phasing、controller、剩余源步骤及父链。NormalEvent/OpponentCardEvent只在ActionRound；HeadlineEvent只在Headline且无后续OPS。FYP父controller依据印刷US关联固定为US，不等于RootPhasing。US根ActionRound父provider为NormalEvent，USSR根为OpponentCardEvent，两方根Headline均为HeadlineEvent；父牌实际owner仍是RootPhasing。FYP仅支持已验证直系五年计划的US关联子事件Nixon/Ussuri，独立随机收据/选中实体/US关联匹配，Cultural不合法。普通实体InstanceId与CardId均唯一且定义已知；每个已知source/parent活动帧只能关联其本身一张已接受InResolution源牌，不能让另一实体复用这个frame_id，合法FYP父子各一张仍允许。已完成去向的帧必须不再关联任何实体，自己的源牌在按印刷星号决定的去向；保留根OPS的延迟帧仍只关联自己的源牌。此跨字段领域检查统一用于首次结算、Ussuri新决定和历史收据的原结果快照，Schema形状本身不是证明。父子frame_id和resolution_id必须各不相同，父子resolution不得已在成功历史中；单独event_completed:false不能覆盖已有成功事实。未知父provider/收尾步骤RequiresRuling；不能用trusted=true或自报hash授权任意来源。模型传入的ledger和caller是可信合成前提，未实现鉴权。

Active状态已有winner、达到共享VP自动终局阈值或到达共享DEFCON nuclear_level均InvalidState；已提交Finished则FinishedState，不能再转交China或等待分配。活动Formosan等实例必须匹配已接受成功事件的来源、card/effect/controller/turn。已识别的Formosan和OPS修正在本事件中保持原样；未知相关provider停止。转交不生成China CardUse，三牌标题/holder/root身份均不取消台湾决议。共享PlanChinaUseCancellation只响应实际已接受的US中国OPS或Space使用；不弃手中未发生的Formosan。中国地区奖励和VP终局阈值只读共享引用，不另存副本。

转交/VP分支一次提交完整结果。Ussuri先提交只含PendingDecision的等待状态；合法答复才同事务提交全部4点和事件完成。拒绝不撤销此前接受的等待点。正常事件完成同时收尾可完成的源牌去向；对手牌先事件后OPS时源牌仍InResolution，保存RemoveAfterRemainingSteps，直到原根剩余步骤完成。不得提前移出。VP终局使用未提交草稿，截断余下OPS并同事务完成已接受源牌、已验证FYP父牌及当前根slot的必要收尾后写Finished；已提交Finished不允许后来Finalize改变状态。FYP父链区分FinalizeAfterChild与具名OperationsAfterChild，后一种只允许USSR根ActionRound的OpponentCardEvent，表示父事件返回后还有原根OPS；它不能用于NormalEvent或Headline。子事件非终局时保留原父帧及父牌，由父流程继续，不替父记录已成功或提前弃牌；终局草稿才截断这些余下步骤并以US控制者写一次父成功事实。未知链不推断收尾。

收据保存原完整命令摘要、完整结果、after摘要和独立可信存储的接受记录锚点。重试返回原receipt副本，后来的China转交、翻面、影响力变化或Finished均不要求等于原after；不能覆盖后来局面。真正请求冲突、损坏原状态/trace/摘要或重算自报摘要替换原锚点仍拒绝。新Ussuri决定须匹配当前等待收据与版本；该新决定的快照校验不是旧命令重放的live-state要求。历史帧检查不读取后来live状态。匹配锚点但含重复帧实体的合成损坏收据仍拒绝；该反例不假定真实客户端能修改独立锚点，也不声称完整历史重执行。离线深拷贝/摘要不证明生产存储、并发或持久化。

范围外：可选Chinese Civil War未启用，基础状态必须一个US/USSR holder；缺holder不是默认US。未知嵌套、未来影响力干预及实际存档/China使用/父帧收尾仍待对应实现。有限投影覆盖具名来源与终局草稿，未执行真实生产事务。无RNG、军事、DEFCON或额外根名额由这三牌事件产生。

来源：[英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf) p2 #5、p9 #58、p11 #71、p12 #76（目视英文）；[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf) §§2.1.2/5.2/6.1.1/8.1.3/9/10.3；[FAQv5](https://www.gmtgames.com/nnts/FAQv5.pdf) p4 #6、p8 #35、p19手牌。FAQ未发现这三牌专门问答，不把工程事务/收据说成官方规则。

全局快照结果域现允许 Finished/Draw；本事件不因此新增平局分支。主入口的新行动仍拒绝终局或活动局面的非空赢家，历史重放保持原结果与当前状态。范围与实际证据见[全局结果契约](draw-global-contracts.md)。
