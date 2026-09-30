# 早期影响力事件：第三批

> description: 查询独立红色政权的名单与追平算法、经互会的敌控过滤与不同国家放置，以及零变化、选择归属和跨卡边界。
> 状态：2026-09-30，2牌基础设计；参数绑定不等于C#实现。截至本批三批共10牌Bound、92个非中国牌Unresolved；最新数量见[数据索引](map-card-data.md)，102个处理器均planned。
> 入口：[参数描述](../../Data/Descriptors/early-influence-batch3.data.json)、[参数](../../Data/Cards/deluxe-2015/early-influence-batch3.parameters.json)、[Schema](../Contracts/early-influence-batch3.schema.json)、[模块](../Descriptors/influence-events.module.json)、[案例](../Design/early-influence-batch3.cases.json)、[验证](../Design/early-influence-batch3.validation.json)。

## 范围与复用

本批是既有影响力设计的扩展。继续采用“具名处理器＋类型化参数”，复用第一批的AddExact、国家选择与继续机制，新增MatchOpponentByAdding计算。比较逐卡复制与通用JSON指令语言，前者会重复边界逻辑，后者扩大执行面；本批只组合已有选择器/原语，不建立通用脚本解释器。

| 卡牌 | 选择方 | 目标/行为 | 具名模板 |
|---|---|---|---|
| #22 独立红色政权 | US | 南斯拉夫、罗马尼亚、保加利亚、匈牙利、捷克斯洛伐克中的1国；只增加US影响力至追平USSR | MatchOpponentByAdding |
| #14 经互会 | USSR | 4个不同且不由US控制的东欧国家，每国加1 USSR | AddToDistinctNonOpponentControlledCountries |

卡面定位：C-DELUXE-EN英文卡图p4 #22、p3 #14。[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf) §2.1.1–2、§2.1.7、§5.2、§6.1.1提供子区、控制、事件归属和直接放置边界。名称仅用于展示；目标名单和数值只从参数读取，国家稳定度/子区只从world.json读取。印刷OPS、星号等仍唯一保存在cards.json。

## 独立红色政权：追平而非取得控制

输入为已验证目标国的own=US影响力、opponent=USSR影响力。计算add=max(0, opponent-own)，newOwn=own+add，苏联影响力不变。

- US1、USSR4：只增加3，最终4比4，不再增加稳定度，也不是增加4点。
- US6、USSR2：增加0，保留US6，不为强制相等而移除4点。
- 不限制国家控制方：名单中的苏联控制国同样可以选。不能只选择双方未控制国。
- 只能选名单中的一个国家。不能扩展到所有东欧国家；奥地利、芬兰、波兰、东德都不在名单中。

模板不读取稳定度来计算增加量。现有EnsureControlByAdding使用enemy+stability作为目标，本模板使用enemy；两者可共享“只补正差额”的内部计算，但名称和公开契约分开，避免把追平误写为控制。

卡面未要求USSR影响力大于US，故采用AnyListedCountry：允许选择零增量目标，即使名单中另有可获利目标。该项属于根据Add语义、目标条件和§5.2推导的明确解释，不声称FAQ专门裁定。选择零增量目标为ResolvedNoChange；仍按已发生的星号事件处理。没有地图变化不代表事件没发生。

五个名单国家在正式地图上都存在，因此始终提供1国选择；不能因有用目标为零就删除本应合法的选项。地图缺失国家是数据错误，不是无候选规则。预览可显示各国增加量，但不可替玩家自动选收益最大者。

## 经互会：先判断当前控制权，再选不同国家

合法集合L是具有region.eastern_europe子区、且Control(country)≠US的国家。Control由双方现存影响力差额和正式稳定度决定，达到稳定度即控制。该谓词允许USSR控制和双方未控制，不能误写为NeitherSide，也不能用“US有任意影响力”代替US控制。

奥地利和芬兰是双子区国家，均可按东欧属性进入L。西德不能因邻接东德而加入，波兰/东德也不能因战场属性而排除。不要求USSR已有当地或相邻影响力，不花OPS，不受普通敌控加价或DEFCON地区限制；其他活动事件的限制仍必须独立检查。

在事件实际开始时确定候选和StateVersion；响应时重新验证同一版本的合法性。不能先在US控制国放1点、打破控制后再声称目标合法。每个选中国家只加1，原有US影响力保留。事件结束产生的控制变化只更新查询，不自动触发欧洲计分胜利。

### 经互会不足4国：EI3-RULING-01

状态：待用户决定，shortage_policy=RequiresRuling。已提出的方案为：有至少4个合法国家时必须选4国；不足时选全部合法国家、每国仍加1；无候选时ResolvedNoChange。该方案目前不是生效规则。

当前可确定的分支是|L|≥target_count：必须选4个不同合法国家。|L|<target_count时不创建无法完成的四国决策，也不自行减少数量，而是保持当前结算未完成并报告RequiresRuling；不应用任何影响力变化或卡牌去向。收到确认后再更新参数、案例和采用记录。

这项只覆盖卡面本身的US控制过滤导致的不足。第二批EI2-RULING-01没有自动推广到本批。Chernobyl等额外活动效果可能影响目标或整次事件，仍是独立裁定；未知组合先返回RequiresRuling，不把L缩小后直接套用短缺公式。RequiresRuling是开发/规则阻塞状态，不是玩家输赢，也不是已结算事件，不能移动卡牌或推进父帧。

## 统一选择、事务和画面边界

两种模板均复用CountrySelectionRules.ValidateSelection与InfluenceMutation.BuildPlan，不新建逐卡MonoBehaviour。具名InfluenceEventResolver.Resolve负责选模板、参数类型、活动效果策略与选择归属；所有符号仍为planned_entrypoints，无可调用C#。

ResolveDecisionCommand只接收DecisionId与CountryId列表，不能附加自报受益方、稳定度、控制权或增加量。验证会话身份、CommandId、ExpectedStateVersion、DecisionRevision、帧步骤、目标存在、去重、数量及合法集合后，才生成完整计划。错误或过期响应整条拒绝，不部分加点；金额由核心计算。

USSR打出独立红色政权时由US选择；US打出经互会时由USSR选择，PhasingPlayer维持原值。头条或OPS-before/after导致事件开始时局面不同，核心读取真实开始状态，不能沿用预览时的候选缓存。待选择期间不执行无关命令；保存EffectInstanceId、参数版本/摘要、继续位置和决策，恢复时不依赖场景或动画回调。

~~~mermaid
flowchart LR
 A[事件与参数] --> B[核对活动效果]
 B --> C[名单或东欧非敌控候选]
 C --> D[受益方选择完整目标集合]
 D --> E[身份/版本/数量/合法性验证]
 E --> F[追平差额或每国固定增量]
 F --> G[原子提交并记录完成与去重结果]
 G --> H[统一星号去向并返回父帧]
~~~

应用影响力计划、完成标记、事件序列和命令去重记录必须原子提交。重复成功命令返回原结果，不重复增加影响力。按稳定CountryId排序输出增量，输入列表顺序不影响状态摘要。只发布已触发卡牌和公开地图信息，不泄露手牌、牌库或父帧私有数据。地图坐标、语言、主题、动画均不进入规则计算。

## 验证与下一步

案例包括追平差额、保持领先值、合法零变化选择、名单外国家、控制临界值、双子区、无邻接放置、不同国家约束与短缺策略。离线参考模型不证明C#事务、选择权限、存档恢复、卡牌去向或隐藏信息已实现；运行案例单独保持planned。

下一阶段建议先设计持续效果的生效/到期/取消/作用范围，再以越南起义、遏制政策和红色恐怖/清洗检验OPS修正，随后处理北约相关的前置与地区例外。仍按小批核对，不提前把所有持续卡牌写成一个万能流程。

[第一批](early-influence-batch1.md) · [第二批](early-influence-batch2.md) · [返回索引](index.md)

后续已建立[持续效果首批设计](persistent-effects.md)，三张新增参数已绑定；经互会短缺仍待用户明确确认。
