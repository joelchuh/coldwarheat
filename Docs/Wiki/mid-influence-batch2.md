# 中期影响力第二批

description：查Liberation Theology中美额度、Muslim Revolution两国清除与South African Unrest分支邻国分配。

状态2026-10-06：#75 USSR2、#56 USSR4、#53 USSR2，均Mid非星号，参数Bound，C#待实现。[描述](../../Data/Descriptors/mid-influence-batch2.data.json)→[参数](../../Data/Cards/deluxe-2015/mid-influence-batch2.parameters.json)、[Schema](../Contracts/mid-influence-batch2.schema.json)、[案例](../Design/mid-influence-batch2.cases.json)、[验证](../Design/mid-influence-batch2.validation.json)。

沿用InfluenceMutation.BuildPlan、CountrySelectionRules.ValidateSelection、RemovalAllocationRules.ValidateAndBuildPlan与InfluenceBranchRules.ValidateBranchAndBuildDecision，具名处理器选择封闭模板，没有逐卡UI或JSON脚本引擎。固定受益方与决策Owner=USSR，即使US出牌也不改归属或根行动者；事件不花OPS、无普通投放邻接/敌控费用，不把印刷4误作移除上限。

| 牌 | 目标与分配 | 完成方案 |
|---|---|---|
| 解放神学 | 中美地区任意国家，加总3USSR，每国最多2 | 同国3非法；2＋1或三个国家各1，必须总3，不放弃余额 |
| 穆斯林革命 | 卡面8国中选2个不同国家，移除全部US | Sudan/Iran/Iraq/Egypt/Libya/SaudiArabia/Syria/Jordan；不限控制，USSR保留 |
| 南非动荡 | 二选一：SouthAfrica加2；或SouthAfrica加1并邻国合计2USSR | 邻国可同国2或两国各1，分支及全量方案一起原子提交 |

解放神学的目标仅为world.json正式region.central_america的10个成员国，不要求已有USSR影响或排除US控制。响应CountryId唯一、正整数金额≤每国上限、总额恰好预算；非法/少量/额外量整案拒绝，不部分加点。

穆斯林名单不是“整个中东”：Sudan属于Africa仍在名单；Israel/Lebanon/Gulf States不在名单。必须两国不同，允许选择现有US为0的名单国家，因卡文没有正影响过滤，清零是幂等操作；MI2具名文本推导，非官方专门FAQ或扩张以前短缺裁定。一个/两个零量国家可合法，仍完整执行两国选择并记录成功Changed/NoChange。没有分配缺额，不将清除解释为转换USSR，控制变化只由数值重新查询，非自动地区计分。

南非动荡以分支ID选择完整策略，SouthAfricaOnly无需邻国额度；SouthAfricaAndNeighbors必须先完成总2分配再提交固定1与邻国变化。在等待或非法响应时SouthAfrica仍未变化，避免半执行后再撤回；本批是无中途交互的完整方案事务约定。邻国使用正式AdjacentCountryIds查询，不把视觉距离、非洲同区或OPS的可达性当邻接。当前world图返回Angola与Botswana；卡牌参数只记录SouthAfrica/查询，不复制邻国名单。FAQv5 PDFp12 #53明确可各1。两分支总增量不同（2对3），不能统一误设同一总预算；也不能固定SouthAfrica先加1再每国自动2。重复行、名单外国、wrong branch或多余不适用字段拒绝，不忽略额外payload。

事件授权/版本/活动限制→建立分支/国家/额度PendingDecision→USSR响应→Owner/Revision/完整方案验证→同事务提交影响力、完成/成功事实与去重收据→既有牌区与父帧收尾。真实鉴权/事务/存档尚未运行。直接响应含稳定DecisionId、branch_id与完整country_ids或allocation，不接受玩家自报阵营/来源/增量；保存源ResolutionId、定义版本、数据摘要、候选快照、DecisionRevision与继续位置，恢复不依赖弹窗或动画。

无随机/VP/DEFCON/军事或额外根名额。三牌均非星号，成功后普通弃牌、以后可重入牌库，不自行移出；去向仍读cards.json单一字段。重复同ResolutionId返原收据，同ID不同请求冲突；过期/未知干预停止且当前方案零变化。地图缺定义不是无候选玩法，不能用默认空列表完成。可选#110等来源不在本包启用；未知其他影响力限制不猜缺省合法。

依据[英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)p9 #53/#56、p11 #75、[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)§5.2/6.1.1及[FAQ](https://www.gmtgames.com/nnts/FAQv5.pdf)PDFp12 #53。表现层读取公开增量/候选，主题和动画可更换。

本批主离线16数据＋53参考＝69新项，1396旧回归，共1465通过；29Schema/32数据/19模块、96参数指针。6runtime未运行，38Tools另层，独立只读审阅Ready Yes，无Critical/Important；16有限结构＋53参考独立复算，meta/1396旧回归未独立运行，目标范围措辞Minor已主側修正。
