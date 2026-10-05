# 战争牌持续与取消

description：定位Flower Power战争牌资格、戴维营固定收益、邪恶帝国永久取消/阻止，及实际出牌者和事件收益方的区别。

状态：2026-10-06，3牌已知参数Bound；全项目28 Bound / 74 Unresolved，102处理器planned，C#/Unity未实现。Bound不是可运行/裁定齐备证明。入口：[数据描述](../../Data/Descriptors/war-card-hooks.data.json) → [参数](../../Data/Cards/deluxe-2015/war-card-hooks.parameters.json)；[钩子模块](../Descriptors/card-play-hooks.module.json)、[组合模块](../Descriptors/composite-events.module.json)、[Schema](../Contracts/war-card-hooks.schema.json)、[案例](../Design/war-card-hooks.cases.json)、[验证](../Design/war-card-hooks.validation.json)。

## 三牌与可复用步骤

Flower #59：永久激活CardUsePenalty，苏联收益方固定。Evil #97：先取消Flower实例、登记未来禁止，再美国1VP；没有Flower实例也可正常执行。已有分数不回退。两张均移出事件牌，Evil取消/阻止两种关系由英文p14确认。

Camp #65：按英文p10顺序美国1VP（关闭唯一VP批次）→以色列/约旦/埃及各加1美国影响力→完成事件后登记成功事实。固定影响力不受OPS邻接/敌控双倍费用限制。VP终局截断后续步骤，不在已结束对局继续加影响力；此为WH1编排约定，非额外FAQ。首次完整成功才作为WAR1阿以永久门禁，不从Removed/Discard/Ops使用推断成功。

影响力批次、VP批次、Activate、CancelAndPrevent、RecordSuccessfulEventFact是具名原语；只引用数据不接受任意表达式。静态定义不包含对局ActiveEffectInstance或PreventionRecord；未来实例持有SourceResolutionId、EffectId、Controller、Duration与规则摘要，UI只读图标投影，可换画风。

## 实际使用与身份

CardPlayer是实际使用这张牌的一方，RootPhasingPlayer保留核战责任，EffectController决定中立事件收益或选择；三者不能互代。美国OPS打朝鲜/阿以战争，即使战争受益为苏联，Flower资格仍按美国CardPlayer判断。苏联OPS打中立战争而由美国执行其他选择，不因此成为美国CardUse。

UseId/CardId/角色/出处/父帧都由编排服务建立。客户端只提交合法意图或DecisionId/Revision/选项，不得填CardPlayer、资格、奖励数值或来源证明。CardUseHook输出计划，编排层提交；hooks不调用turn-flow，避免循环依赖。

普通行动与分别执行的头条可产生ActualUse。头条预约/揭示只是显示，不产生处罚。验证合法实际使用后，在事件/OPS执行前查询当前活动效果和永久禁用；此前已完成Flower可影响随后执行的战争。头条实际执行边界是遵循§4.5串行执行的工程约定，不把同时揭示当两牌同时完成。尚未设计Grain Sales/Star Wars/Missile Envy等来源契约的子使用必须RequiresRuling，不能泛化为同一出牌来源。

## Flower资格与未核实边界

五牌集合只存/hook_policy/war_card_ids：阿以、朝鲜、Brush、印巴、两伊。美国Event或Operations使用属于处罚资格；SpaceRace不罚，不论太空成功；Discard和RevealOnly不罚。不是战争集合的牌不罚，不按名称包含war猜分类。

已完整成功Camp使Arab事件永久不可用；美国仍可用其OPS，按FAQ #59不罚。没有Camp成功事实，正常阿以使用属于资格。War胜负、骰点、目标敌方影响力是否为0都不改变使用资格；选目标失败/响应非法不是新的CardUse，不能重复收费。

**待裁定WH1-TIMING、WH1-UN：** 官方p13 #59定义处罚适用范围，但未找到覆盖处罚与战争胜利VP先后的专门答案；p7 #32也未明确UN压制与Flower交互。已向用户提出项目解释：先独立处罚/判胜再执行战争；UN压制免罚。在收到明确采纳前，数据保留RequiresRuling，不把建议当规则。ChargeQualified只说明资格，BuildPenaltyPlan仍停止；UN使用即使其他资格成立也停止。不会据公开第三方评论冻结相互冲突的答案。

## 成功历史、取消与恢复

复用treaty_protection/event_fact_policy：ResolvedChanged/ResolvedNoChange才是已验证成功；NotTriggered/BlockedPrerequisite/Suppressed/Cancelled不能解锁。Evil正常即使没有Flower实例也永久禁止其未来事件；Flower之后被禁止时OPS仍可用，实体牌不因星号自动移出。未知或不完整历史RequiresRuling；已成功Evil的事实与持久禁止状态必须一致，否则停止并报告存档损坏，不私自修补。

同ResolutionId重放返回AlreadyCompleted，不能重复影响力/VP/禁止；不同ResolutionId重复激活已有Flower保留旧规则的RequiresRuling，不自行刷新/叠加。取消只对持久处罚部分，不撤销历史2VP；不得借取消复活已移出实体牌。未来钩子去重键为CardUseId+EffectInstanceId，持续效果取消后不能重算已处理使用。

保存ActualUse快照/已结算HookId、当前步骤与父帧；读档不重算已接受资格、不重复关闭VP批次。非法响应/未知状态失败时本次计划无部分提交、无随机游标推进。本批没有骰子；战争骰仍只由WarEventService消费。

## 依据与验证边界

英文卡图p9 #59、p10 #65、p14 #97，R2015 §4.5/5.2/6.1.1/6.4.5/7.3/7.5/10.2–3，FAQv5 PDF p13 #59及p7 #32；见数据source_catalog。WH1把卡文步骤、使用快照与幂等设计明确列为工程约定，两项裁定仍pending。案例分Schema/固定参考/运行验收，参考模型假定身份及来源可信，不能证明真实授权、存档与C#/Unity。全部runtime验收未运行。
