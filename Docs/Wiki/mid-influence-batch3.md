# 中期影响力第三批

description：查殖民地后卫跨地区四国、运河固定三国、傀儡政府零影响筛选及美国之音非欧洲限额移除。

2026-10-12：四牌参数Bound，C#未实现。[数据描述](../../Data/Descriptors/mid-influence-batch3.data.json)→[参数](../../Data/Cards/deluxe-2015/mid-influence-batch3.parameters.json)、[Schema](../Contracts/mid-influence-batch3.schema.json)、[案例](../Design/mid-influence-batch3.cases.json)、[验证](../Design/mid-influence-batch3.validation.json)。

复用InfluenceMutation.BuildPlan、CountrySelectionRules.ValidateSelection及RemovalAllocationRules.ValidateAndBuildPlan；卡牌处理器选择封闭策略，数值只从数据集读取。四牌固定受益方为US，选择也归US；USSR打出事件不能变更选择权或根PhasingPlayer。直接事件无普通OPS邻接/费用，不消耗印刷OPS，敌方控制不额外计费。

| 卡牌 | 目标与结果 | 牌面去向 |
|---|---|---|
| #63 殖民地后卫 | 非洲和东南亚并集，4个不同国家各US1，可混选两区 | 非星号 |
| #64 巴拿马运河归还 | Panama、CostaRica、Venezuela各US1 | 星号 |
| #66 傀儡政府 | 可拒绝加点；或3个双方当前影响均为0的不同国家各US1 | 星号 |
| #74 美国之音 | 非欧洲国家移除USSR总4，每国最多2 | 非星号 |

殖民地后卫用地图主地区或子地区成员关系，东南亚并集不是整个Asia；Japan/India不合法。地图图形坐标不参与规则。运河三国同时固定加点，Venezuela虽属南美仍在具名名单；不允许改选其他国、遗漏一国或把总量1拆开。去向按cards.json读取星号字段，不在参数复制。事件实际成功才按既有流水线处理移出/弃牌，不能因为出作OPS/太空或仅弃牌就移出星号。

傀儡政府的“无影响”同时检查US=0及USSR=0，不等于无人控制、没有US影响或没有USSR影响；按事件解决时当前版本建立候选，在提交时重新验证。may在本设计解释为可Decline整个加点，合法拒绝产生ResolvedNoChange成功事实；不是可以任取1或2国。加点分支不足3合法国目前RequiresRuling，不能套用马歇尔的用户解释，也不自行把阻塞等同未发生事件。拒绝不需要凑满三国，仍遵守去重、Owner与Revision。合法候选不足的具体加点结果留待官方裁定或用户确认。

美国之音英文是移除4，不是up to4；每个国家实际USSR影响足够、分配合计必须4、每国1或2，完整方案一次提交。非欧洲按正式region.europe排除，包括Turkey/Finland等，不靠画面位置；US影响保留。可移除量按所有合法国家min(USSR影响,每国上限)求和，单国10点也只贡献2额度。小于4时RequiresRuling，不悄悄削减目标或形成负数；其他牌短缺解释不自动继承。已核实足够时少量/空方案非法。

流程：可信事件帧及完整状态→策略/候选查询→US的PendingDecision或固定计划→全量验证→同事务应用影响、完成事实与收据→既有父帧/牌区收尾。等待、非法、越权、过期、未核实限制时当前计划零变化，不部分先加/先扣；已完成同请求返旧收据，冲突重试拒绝，Finished不再变更。保存CardId、参数版本/摘要、DecisionId/Revision、候选状态版本、选择/额度和继续位置，不依赖弹窗/动画。普通UI只得到所属玩家可见选择与公开影响增量。

没有随机、VP、DEFCON、军事或额外根名额。未知跨卡干预必须停止，不把optional包自动接入。参考模型只验证影响力投影和上述条件；鉴权、真实事务/事件账本、牌区、存档、回放与画面接口仍需未来实现测试。

依据[官方英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)p10 #63/#64/#66及p11 #74；[Deluxe 2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)§2.1/5.2/6.1.1。短缺停止与原子完整计划为项目工程边界，非声称官方专门裁定。既有Pope/Scientist/Flower/LADS等未答裁定全部保持。

主验17结构＋65有限参考＝82新项，1719既有回归，共1801通过。35Schema/38data/21模块、116参数指针；6runtime未运行，13note/38Tools另层。独立审阅Ready Yes，无Critical/Important/Minor；82新项、302基线SHA/6报告SHA、209JSON/35Schema及116指针独立核验，1719历史与6runtime未独立运行。
