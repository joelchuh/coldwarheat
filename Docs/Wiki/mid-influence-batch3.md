# 中期影响力第三批

本次用户裁定的当前整合验证入口：[2026-10-13报告](../Design/confirmed-rulings-20261013.validation.json)。原批次报告保留为当时版本的历史记录，不代表修订后的最新验证。
description：查殖民地后卫跨地区四国、运河固定三国、傀儡政府零影响筛选及美国之音非欧洲限额移除。

2026-10-12：四牌参数Bound，C#未实现。[数据描述](../../Data/Descriptors/mid-influence-batch3.data.json)→[参数](../../Data/Cards/deluxe-2015/mid-influence-batch3.parameters.json)、[Schema](../Contracts/mid-influence-batch3.schema.json)、[案例](../Design/mid-influence-batch3.cases.json)、[验证](../Design/mid-influence-batch3.validation.json)。

复用InfluenceMutation.BuildPlan、CountrySelectionRules.ValidateSelection及RemovalAllocationRules.ValidateAndBuildPlan；卡牌处理器选择封闭策略，数值只从数据集读取。四牌固定受益方为US，选择也归US；USSR打出事件不能变更选择权或根PhasingPlayer。直接事件无普通OPS邻接/费用，不消耗印刷OPS，敌方控制不额外计费。

| 卡牌 | 目标与结果 | 牌面去向 |
|---|---|---|
| #63 殖民地后卫 | 非洲和东南亚并集，4个不同国家各US1，可混选两区 | 非星号 |
| #64 巴拿马运河归还 | Panama、CostaRica、Venezuela各US1 | 星号 |
| #66 傀儡政府 | 可拒绝；Accept选min(3,合法空白国数)个各US1 | 星号 |
| #74 美国之音 | 非欧洲移除min(4,逐国限额容量)，每国最多2 | 非星号 |

殖民地后卫用地图主地区或子地区成员关系，东南亚并集不是整个Asia；Japan/India不合法。地图图形坐标不参与规则。运河三国同时固定加点，Venezuela虽属南美仍在具名名单；不允许改选其他国、遗漏一国或把总量1拆开。去向按cards.json读取星号字段，不在参数复制。事件实际成功才按既有流水线处理移出/弃牌，不能因为出作OPS/太空或仅弃牌就移出星号。

傀儡政府的“无影响”同时检查US=0及USSR=0，不等于无人控制或任一方没有影响；按事件解决时当前版本建立候选，在提交时重新验证。2026-10-13用户确认 `PROJECT-MI3-USER-20261013`：可以Decline整个加点；Accept必须选择 `min(target_count, 合法空白国数)` 个不同国家，各加参数中的1点。只有1或2个合法国时全部选取；足3国时仍必须选3国，不能任意少选。零候选的Accept无变化完成；合法Decline同样完成且不加点。固定US选择者、版本与完整计划约束仍在。

美国之音的必须移除额度改为 `min(total_amount, Σ min(max_per_country, 该国现有USSR影响))`，求和仅覆盖非欧洲合法国家。这里是用户确认的项目解释，非新增官方FAQ。每国仍最多2；单国有10点但其它国家均无影响时移除2并保留8，不能把“短缺全移”误作取消每国上限。额度为0时空分配完成；额度为1至3必须做满，不能Decline或少移。欧洲（包括Turkey、Finland等）不计容量，US影响保留，重复目标和负数仍拒绝。

流程：可信事件帧及完整状态→策略/候选查询→US的PendingDecision或固定计划→全量验证→同事务应用影响、完成事实与收据→既有父帧/牌区收尾。等待、非法、越权、过期、未核实限制时当前计划零变化，不部分先加/先扣；已完成同请求返旧收据，冲突重试拒绝，Finished不再变更。保存CardId、参数版本/摘要、DecisionId/Revision、候选状态版本、选择/额度和继续位置，不依赖弹窗/动画。普通UI只得到所属玩家可见选择与公开影响增量。

没有随机、VP、DEFCON、军事或额外根名额。未知跨卡干预必须停止，不把optional包自动接入。参考模型只验证影响力投影和上述条件；鉴权、真实事务/事件账本、牌区、存档、回放与画面接口仍需未来实现测试。

依据[官方英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)p10 #63/#64/#66及p11 #74；[Deluxe 2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)§2.1/5.2/6.1.1。Puppet/VOA短缺采用本次用户确认解释；原子完整计划仍为工程边界，均不声称官方专门裁定。既有Pope/Scientist/Flower/LADS等未答裁定全部保持。

原0.1.0历史主验17结构＋65有限参考＝82项，1719既有回归，共1801通过。35Schema/38data/21模块、116参数指针；6runtime未运行，13note/38Tools另层。独立审阅Ready Yes，无Critical/Important/Minor；82新项、302基线SHA/6报告SHA、209JSON/35Schema及116指针独立核验，1719历史与6runtime未独立运行。

2026-10-13修订验证：95项＝原82＋新增13（19数据形状、76有限参考），6运行验收未运行。原5个短缺结果按明确授权调整，其余历史期望保留；旧案例ID中的stop标签保留作追溯，不代表当前仍停止。新20文件裁定批总计954＝原908＋新增46；不重报其它历史回归或绑定总数。
