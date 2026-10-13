# 军事行动记账与期末评估实现计划

> 使用executing-plans在当前会话inline实施，一次整批独立审阅；用户持续开发与Git授权有效。
**Goal:** 实现规则授权的军事信用记账、重复输入防重、双方期末缺额一次净结算与归零计划。
**Spec:** Docs/Wiki/risk-victory.md第2、4节；Data/Rulesets/deluxe-2015/military-operations.json；RV-M-01至08。
**Architecture:** 不可变MilitaryOperationsDefinition/State、MilitaryCredit及MilitaryTurnEndAssessment；纯MilitaryOperationsService返回计划，Infrastructure读取共享JSON。尚不直接写GameState/VP或结束对局。

## 决定与边界

Definition保存共享initial/reset/penalty和七类credit_sources。MilitaryCredit保存CreditId、TurnNumber、Beneficiary、Kind、Basis、Amount、SourceRuleId，来自可信规则计算；Kind与定义中的Basis匹配才可入账。None来源只能0，不从印刷OPS、骰子加成或卡名猜量。相同ID全部事实一致时不再加；同ID变更事实拒绝。受益方与根行动方不同的战争奖励按Beneficiary入账；累计不设5上限，int溢出拒绝且原状态不动。SourceRuleId是定位信息，本批不把字符串当完整授权证明。

State绑定不可变定义与实际GameTurnContext，Create开始该回合，独立保存双方累计和只读信用记录。记录最多4096条、ID长度最多256为工程输入限额，不是规则上限。评估后禁止新的信用；同回合已经接受的信用重试仍可得到无变化计划。新回合状态创建由未来编排调用，不在服务里推进日历或继承超额。

AssessTurnEnd(state, assessmentId, defcon)使用当前非核战DEFCON，一次计算双方缺额与netVpForUS，返回Assessment和同时归零后的State。只输出完整净值，不逐方改VP、不自行判胜。第一次计划VpDeltaToApply为净值，重复同ID与同DEFCON定义/等级返回原Assessment和0增量；换ID或改变已评估输入拒绝，不用归零后的累计再次评估。完整状态/VP/游标的共同提交与持久化仍需未来宿主。

参数JSON继续作为唯一业务事实，不新建常量卡牌表。Loader严格检查schema/id/status/来源、计入来源枚举、边界策略字符串与数值类型；完整RulePack和逐卡奖励资格不在本接口认证。官方文件不修改。

## Task 1：纯记账与评估

- [x] 添加Core/Rules/MilitaryOperationsDefinition.cs、MilitaryOperationsService.cs与Tests/EditMode/MilitaryOperationsTests.cs，先桩RED后实现GREEN。
- [x] 验证累计/受益方/无上限、合法来源、跨回合与同ID变更、只读记录、溢出回滚、双边净罚分、当前DEFCON、期末归零、重复评估零增量、禁止核战/评估后新信用。
- [x] 保留原840项回归，通过真实Unity XML确认。

## Task 2：共享数据与收尾

- [x] 添加MilitaryOperationsLoader.cs及LoaderTests，真实数据与变异错误先RED/GREEN。
- [x] 更新军事模块/数据JSON、Wiki/索引与工作记录；保留胜负检查及完整事务planned，记录参数hash与真实XML。
- [x] 独立整批审阅通过，无Critical/Important；常规Git同步在本批验证后执行，远端以实际Git核对为准。无UI接线，不重建Demo。

## Review Focus

1. 根行动者、实际行动者与MilitaryCredit.Beneficiary不混用。
2. 重复信用/评估返回零增量且不会把已归零状态重复扣分。
3. MilitaryCreditPolicy来自共享数据，客户端没有直接提交奖励入口。
4. 期末双方同一快照净算，服务不在+2/-2中途宣告赢家。
5. 评估结果和reset是同一计划，真正VP/状态提交仍由未来宿主保证。

基线737bd61dfd3d79b5ff363f046a3330332a5d7e73；已核对GitHub相等。首次远端ls-remote遇TLS中断后只读重试成功，没有重复改写历史。
