# 晚期影响力第一批（LI1）

本次用户裁定的当前整合验证入口：[2026-10-13报告](../Design/confirmed-rulings-20261013.validation.json)。原批次报告保留为当时版本的历史记录，不代表修订后的最新验证。
description：查询四张晚期牌的VP先行、精确/可选分配、永久政变禁止及原收据重放。

本页四牌参数已Bound：#83 铁娘子、#87 改革者、#88 海军陆战队营房爆炸、#99 潘兴II部署。参数可读，处理器、事务、授权、持久回执和正式域证明均 **planned**。离线 Python 表例只验证有限参考投影；不表示已实现游戏行为；整合实际回归见[本批报告](../Design/late-influence-batch1.validation.json)。

## 文件与共享边界

- 参数：`Data/Cards/deluxe-2015/late-influence-batch1.parameters.json`
- 严格结构：`Docs/Contracts/late-influence-batch1.schema.json`，稳定具名 `$defs` 为四个 CardId 后缀及事件请求、状态、选择、查询。
- 表例：`Docs/Design/late-influence-batch1.cases.json`；完整初始双方84国快照加显式覆盖，遗漏国家不是默认零。预期覆盖独立手算，比较完整结果与输入不可变。
- 描述符：`Data/Descriptors/late-influence-batch1.data.json`；正式报告路径由集成阶段填充。新批次临时报告不宣称累计覆盖。

印刷OPS、阵营、星号、下划线继续以 `cards.json` 为准；国家/地区以 `world.json` 为准，VP方向与阈值引用 `rules.deluxe2015.victory`。四牌均星号移出；Iron Lady与Reformer长期标记。事件执行方按印刷阵营，不等于出牌者或根行动方。

复用计划接口 `InfluenceMutation.BuildPlan`、`RemovalAllocationRules.ValidateAndBuildPlan`、`CompositeEventResolver.ResolveOrderedSteps`、`ActiveEffectService.Activate`、`EventPreventionService.CheckCanTrigger` 与既有VP/终局草案协议。没有新增通用DSL、C#处理器或未来牌授权。

## 四牌行为

| 卡 | 有来源的基本行为 | 选择与长期边界 |
|---|---|---|
| Iron Lady | US得1VP；USSR在Argentina加1；清UK全部USSR影响；以后阻止Socialist Governments事件 | 先关闭唯一VP批次，终局则停止后三项。UK本为零仍执行其它项。禁止消费者从EI2 `socialist_governments` 解引用，匹配其 `blocked_by_effect_id`，不另复制目标表。|
| Reformer | USSR直接在欧洲加4，每国新加最多2；USSR当前VP领先时可改加6；以后禁止USSR在欧洲政变 | 欧洲全部21国，允许对手控制国，不用OPS邻接/敌控费用。已有影响不算新增上限。仅禁实际USSR actor的Coup，非US Coup、Realignment、WarEvent；免费政变也不能绕过独立禁止。|
| Marine Barracks Bombing | 清Lebanon全部US影响，再移除min(2,中东剩余US总量) | 在拟清空后核算额外容量；足2时可一国2或两国各1，不足时全部移除；Lebanon已清量不计额外量。完整方案先验证再提交。|
| Pershing II Deployed | USSR得1VP，再从至多3个不同西欧国家各移除1 US | 允许选0；少于3候选不需要短缺裁定。Austria、Finland、Canada均按地图合格；Poland不属西欧。0选择仍得VP并按星号移出。|

Reformer长期实例记录来源牌、成功结算ID与持续时间，复用 `UntilCancelledOrGameEnd` 及空到期边界；本批没有授权取消来源，未知取消仍须停止。`future_consumer_ref` 指向Glasnost的现有牌定义，只使未来消费者可查询成功活动事实；不执行Glasnost、不授予4OPS，也不把其OPS限制用于Reformer直接加点。回合变化不能自动重施影响或移除此永久事实。

## 具名推导与编排约定

**LI1-D01**：英文Reformer的“may … instead”按替代总量解释：当前共享US正向SignedVp小于零时，合法总量是4或6；零及正数仅4。不是4+6、任意5、up-to6或强制6。选项取实际事件分配边界的当前VP；对手OPS先结算造成领先变化时重新核验。此为卡文推导，未发现专门FAQ。

**LI1-D02 / PROJECT-LI1-USER-20261013**：Marine先在计划中清Lebanon，再从剩余中东US影响核算 `min(additional_total, capacity)`；清空量不计额外额度。2026-10-13用户明确确认不足量全部移除并完成；这是该牌的专属项目解释，不是借用别牌裁定或声称官方FAQ。容量0用selected_total=0及空列表，容量1必须恰移1；尝试移2或有量少移仍InvalidSelection，候选方案零提交。

**LI1-D03**：Pershing的up-to允许0至3个不同目标。规范化输入仅列有US影响的国家，0金额/空国家通过空列表表达；这是等效有意义选择的接口规范，并非声称官方禁止点选空国家。每目标固定1，不接受汇总3点投向一国。

**LI1-D04 / LI1-E01**：按印刷顺序先关闭Iron Lady/Pershing的唯一已知VP批次，再做即时终局检查。达到共享阈值时，只在同一终局草案事务完成已接受事件的成功事实、星号去向与回执，随后提交Finished；不执行尚未发生的加点、清除、禁止或选择。不得在已提交Finished之后另调finalize修改状态。

## 请求、计划与回执

域实现须验证来源帧/注册处理器、CardId/EffectId、owner、规则与参数版本、状态及决策版本、完整地图和所有相关provider。未知不能当作false或零。Reformer当前VP快照必须匹配权威状态；完整选择不得通过重复国家绕过上限。

参考请求的 `active_effects` 是本批两类相关活动事实的有限投影，并非替代全游戏活动效果集合；其余provider的完整性只用合成证据标记表达。正式运行必须从既有活动效果/预防服务生成此投影及完整性结果，不能让UI自报“完整”或把未建模效果丢掉。

参考模型投影**已提交候选完整方案**，无UI等待：先预检完整结构、权限、provider和非终局选择，再在候选副本按印刷步骤记录trace。无效选择不提交该候选。该投影不等于生产分步提交；生产Pershing须先保留唯一VP回执再等待目标，错误/过期响应保留已提交VP，重试/恢复不得重复奖分。这一继续位置与保存恢复仅列planned-runtime。

同一command/source/决策/完整请求的已接受回执重放返回原回执及其完整原结果，不变更当前状态。回执保存`accepted_result`（原outcome、state、trace）、其摘要、原after-state摘要和完整回执摘要；重放时验证结构、原请求绑定、这些摘要之间的一致性及可信账本原记录摘要。当前状态后来合法变化，不要求它仍等于原after-state：stateVersion推进、France影响变化甚至后来Finished，都不应导致原请求冲突，也不能被旧结果覆盖。真正修改请求、损坏原结果/trace/outcome或回执仍拒绝。返回副本，不允许接收方修改历史收据。

临时SHA内容绑定和独立原记录锚点只是在内存中模拟可信账本；重新计算受损回执的自带哈希不能替换这个原锚点，但这不验证真实认证、不可伪造凭证、持久账本或并发。首个已核实长期激活与新结算重复激活分开：后者停止RequiresRuling，不能刷新、叠加或重发影响/VP。

预防查询结果只表达此效果的否决或事实。`NoVetoFromThisEffect`不是完整行动合法性；仍须全部其它门禁。Iron Lady不反转过去Socialist Governments，不阻止其OPS；未来未实现来源继续被正式处理器门禁拒绝。

## 明确未决

- Reformer受未来干预而无法完成所选4/6、未知额外来源/取消或跨卡干预：`RequiresRuling`，不允许部分额度。
- Iron Lady/Reformer已活动，又出现不同可信结算ID的新激活：`RequiresRuling`。通常星号去向不建立重触发规则。
- 生产身份、终局原子提交、等待/恢复、去重、保存/加载、动作门禁整合和正式域实现：planned；本批无运行消费者测试。

## 来源

- [GMT英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)：PDF p13 #83/#87/#88、p15 #99，已目视；旁列土耳其译文不作为裁定来源。
- [R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)：§2.2.4–5、§5.2、§6.1.1、§6.3.5、§7.3/7.5/7.6、§10.2.2–3/10.3.1。
- [FAQ v5](https://www.gmtgames.com/nnts/FAQv5.pdf)：PDF p15 #90 Glasnost只说明未来额外OPS。限定检索未取得本四牌专门短缺/重复裁定；未命中不证明不存在裁定。

印刷事实、具名推导、项目编排与未决边界分开记录。正式绑定和共享注册表由集成阶段处理，本文件不声明整体牌数或回归累计数。

整合主验537新＋2141旧＝2678离线设计检查通过。独立审阅者另行重跑新537及14个重放探针（后者不计入2678），未重跑旧套件。12运行验收未执行，102处理器仍planned。这份0.1.0历史报告仅覆盖当时子集；Marine不足额现由下列用户确认修订取代，新来源重复仍待裁定。

2026-10-13修订验证：546项＝原537＋新增9（301数据形状、111输入形状、134有限参考）；2个旧短缺夹具仍试图移除不存在的2点，故期望改为InvalidSelection而不是合法短缺方案。新增0/1额度成功、少移拒绝和原完整短缺收据重放；后来地图或Finished不被旧结果覆盖。12运行验收未运行；未决重复、未知取消与来源边界保持。

全局快照结果域现允许 Finished/Draw；本事件不因此新增平局分支。主入口的新行动仍拒绝终局或活动局面的非空赢家，历史重放保持原结果与当前状态。范围与实际证据见[全局结果契约](draw-global-contracts.md)。
