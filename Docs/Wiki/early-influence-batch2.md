# 早期影响力事件：第二批

> description: 查询社会主义政府、苏伊士危机的额度移除，东欧动荡的逐国移除，以及选择权限、当前时代、铁娘子禁止与短缺处理。
> 状态：2026-09-30，3牌参数与基础效果设计已建立；短缺策略获用户确认。两批共8牌Bound、94个非中国牌Unresolved，102个处理器均planned；没有C#运行实现。
> 入口：[参数描述](../../Data/Descriptors/early-influence-batch2.data.json)、[参数](../../Data/Cards/deluxe-2015/early-influence-batch2.parameters.json)、[Schema](../Contracts/early-influence-batch2.schema.json)、[模块描述](../Descriptors/influence-events.module.json)、[案例](../Design/early-influence-batch2.cases.json)、[验证报告](../Design/early-influence-batch2.validation.json)。

## 范围、资料与复用选择

继续采用“数据定义＋具名C#处理器”的设计。比较逐卡复制、通用JSON指令语言、共享算法＋类型化参数三种做法，沿用第三种：共享选择和移除算法，各卡保留自己的稳定EffectId、参数、来源与验收案例。JSON不能包含任意脚本、表达式或反射类名。

本批在[第一批机制](early-influence-batch1.md)上增加两个模板，不创建Unity组件，不决定画风。卡牌OPS、星号、阵营、印刷时代仍唯一存于cards.json；地区成员从world.json读取；回合时代区间从turn-flow.era_bands读取。

| 卡牌 | 选择方 | 目标与效果 | 模板 |
|---|---|---|---|
| #7 社会主义政府 | USSR | 西欧移除US影响力，总额3，每国最多2；铁娘子生效时禁止事件 | BudgetedRemoval |
| #28 苏伊士运河危机 | USSR | 法国、英国、以色列范围内移除US影响力，总额4，每国最多2 | BudgetedRemoval |
| #29 东欧动荡 | US | 3个不同东欧国家移除USSR影响力；早/中期每国1，晚期每国2 | DistinctCountryRemovalByEra |

来源：英文卡图C-DELUXE-EN p2 #7、p5 #28/#29、p13 #83；[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf) §2.1.1–2、§4.1、§4.5H、§5.2、§7.1。官方印刷约束与下面的项目解释分开记录，不将旧FAQ的其他条目整体套入本批。

## 项目解释 EI2-RULING-01

2026-09-30检索时未找到覆盖所有短缺与零影响力目标细节的官方专门裁定。向用户提出以下方案后，用户回答“采用上述解释，继续设计”。采用权威为用户确认，source_id=PROJECT-EI2；它不是官方勘误。

1. 额度移除必须完成可完成的额度。按每国上限和现存影响力计算总容量；容量低于总额时移除全部容量，不通过选择空国、少报额度或提前结束逃避移除。
2. 东欧动荡选择有苏联影响力的不同东欧国家；够3国必须选3国，不足3国则选全部。晚期某国只有1点时仅移除1点，不把缺额加到另一个国家。若有更多合格国家，玩家仍自由选择其中3国，不强制挑移除总量最大的组合。
3. 没有可移除影响力时，事件正常发生但无数值变化，不建立无法回答的决策。苏伊士危机仍按已发生的星号事件移出；其他两张正常弃牌。

第3项是短缺策略在零容量时的设计落实，卡牌去向沿用§5.2。该解释只绑定本批三张牌，不改变第一批杜鲁门的零影响力目标规则，也不解决去殖民化受活动效果干预后候选不足的裁定。

## 额度分配契约

先筛选目标范围，再读取被移除方当前影响力I(c)。对于每个目标国，容量capacity(c)=min(I(c), per_country_cap)，本批只在容量大于0的国家生成可分配选项。有效总额required=min(total_amount, Σcapacity(c))。

PendingDecision保存公开的国家容量、required、参数摘要和SourceStateVersion。响应为不重复的`{country_id, amount}`列表；amount必须为正整数，不能超过该国容量，合计必须等于required。国家范围、上限、总额和被移除方都由核心计算，客户端不能覆盖。数量错误、重复国家、零/负/小数数量、越界国家和伪造数据整条拒绝，不将非法请求静默截断。

例如苏伊士危机：法国3、英国1、以色列0，则容量为2、1、0，只移除3点；法国剩1，英国剩0。不允许为凑齐4点从法国移除3点。若法英以各2，可选2＋2或2＋1＋1；不限于两个国家。

先在局部工作快照完成验证，再调用拟定RemoveExact原语生成增量。该原语要求amount≤old且非负；策略层负责预先计算容量和短缺，不能让底层原语擅自决定不足怎么办。不要把事件总额作为普通OPS预算处理；无敌控双倍成本、可达性条件、DEFCON地区限制或军事行动奖励。

## 东欧动荡契约

EventContext提供权威TurnNumber，经共享era_bands查询唯一时代；再从amount_by_era读取每国量。第7回合与第8回合必须分别落在mid和late。重抽到的早期牌在晚期仍按晚期效果执行，不读取CardDefinition.era作效果时代。非法回合、重叠/缺失时代区间或未知时代是数据/上下文错误，不能默认按早期处理。

候选是东欧且USSR影响力>0的国家。count=min(target_count, 候选数)，确认时必须选择count个不同候选，响应只含CountryId列表，不接受自报移除量。核心对每个选中国家计算min(当前USSR影响力, 当前时代量)，一次提交全部结果。晚期选中三个各1点的国家是合法选择，即使其他候选各有更多影响力；“尽量完成国家数”不等于替玩家最大化损失。

奥地利、芬兰同时属于东欧和西欧，两种选择器均从正式地图的subregion_ids判断；不要根据现实地理或某一方控制权排除。东欧动荡不能把6点作为自由分配预算，也不能在一个国家重复执行三次。

## 铁娘子与事件结果

社会主义政府的blocked_by_effect_id引用现有effect.the_iron_lady。其是否生效只由可信ActiveEffectState查询；卡牌在弃牌堆/移出区不能证明事件曾生效。铁娘子完整处理器仍Unresolved/planned，本批只冻结其对社会主义政府的禁止关系。

- 已明确生效：主动选择社会主义政府事件应在合法行动查询/命令验证时拒绝；作为对手事件被OPS触发时记录Suppressed，无决策、无移除，OPS仍按原出牌帧执行。
- 明确未生效：继续本批结算。
- 活动效果服务/恢复状态缺失：报告技术错误或RequiresRuling，不把未知当作false。

持久效果独立于卡牌区域保存，禁止关系不在回合末自动清除。若未来有明确取消规则，由其处理器更新状态。本批不实现铁娘子的VP、阿根廷或英国其他效果。其他未核实活动效果组合继续走RequiresRuling，不静默放行。

无变化的ResolvedNoChange、明确禁止的Suppressed、非法命令拒绝和技术错误是不同结果。只有正常发生的事件才进入统一CardDispositionRules，并读取cards.json中的星号；本批参数不复制牌区常量。

## 选择与结算流程

沿用第一批的EffectInstanceId、ResolutionFrame、DecisionId/Revision、参数版本/摘要、StateVersion、命令幂等与原子事务。新增的额度分配也是一个完整响应；UI的逐点点击只是本地草稿，不逐次改变权威状态。

~~~mermaid
flowchart TD
 A[事件开始与参数解析] --> B{活动效果允许}
 B -->|明确禁止| C[Suppressed并返回父帧]
 B -->|未知| D[停止依赖事务并报告]
 B -->|允许| E[按范围筛选并计算容量或国家数]
 E --> F{可移除量为零}
 F -->|是| G[ResolvedNoChange]
 F -->|否| H[受益方提交完整分配或国家列表]
 H --> I[验证身份/版本/全部约束]
 I --> J[生成完整移除计划]
 J --> K[一次提交状态/完成标记/事件/去重结果]
 G --> L[统一卡牌去向并返回父帧]
 K --> L
~~~

US打出社会主义政府或苏伊士危机触发对方事件时，由USSR选择；USSR打出东欧动荡时由US选择。PhasingPlayer保持原值。OPS-before/after只影响事件实际开始时的地图快照，不改变选择归属。决策等待期间禁止无关状态变更，读档后使用同一版本契约继续。

非法响应不改变影响力、卡牌区域、随机状态、控制查询、决策或完成标记。重复成功CommandId返回原结果；数组顺序不同的语义相同分配应产生相同排序后的增量/状态摘要，但不同CommandId仍须按当前决策状态验证，不能重放效果。公开决策不包含对手手牌、牌库或父帧私有字段。

## 拟定代码与验证边界

| 拟定入口（均未实现） | 责任 |
|---|---|
| InfluenceEventResolver.Resolve | 具名模板、事件禁止检查、当前时代与继续位置 |
| CountrySelectionRules.ValidateSelection | 国家范围、唯一性和有效数量 |
| RemovalAllocationRules.ValidateAndBuildPlan | 额度容量、整数/每国/总额校验与完整增量计划 |
| InfluenceMutation.BuildPlan | 已校验的RemoveExact；不决定短缺政策 |

完整拟定路径与依赖见模块JSON；entrypoints仍为空，planned_entrypoints不可调用。案例区分参数验证、一次性离线参考模型和未来C#运行验收。离线通过不能证明事务、存档、权限、事件编排或联网实现已存在。

下一批可设计Independent Reds与COMECON，继续扩充补齐影响力和排除敌方控制目标的选择机制；持续效果的统一存续/取消随后单独展开。[返回索引](index.md)
