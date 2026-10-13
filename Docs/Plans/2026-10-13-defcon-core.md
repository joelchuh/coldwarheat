# DEFCON基础服务实现计划

> 使用executing-plans在当前会话逐项实施，一次整批独立审阅。用户持续优化与Git授权有效；这是已确认risk-victory设计的运行子集，不增加规则解释。

**Goal:** 让后续政变/调整和事件编排调用可测试的DEFCON地区门禁、轨道变更与核战责任信号。
**Architecture:** 不可变DefconDefinition绑定规则MapDefinition；DefconState保存定义与当前Level。纯DefconService返回地理检查结果或轨道计划，不改GameState、不掷骰、不直接判输赢。Infrastructure只解析共享JSON。游戏身份、事务去重、逐卡授权、完整政变、胜负及UI轨道随后接入。
**Tech:** 纯C#9；Infrastructure Newtonsoft/ContractJson；Unity6000.6.0f1 EditMode。
**Spec:** Docs/Wiki/risk-victory.md第1–3节，Data/Rulesets/deluxe-2015/defcon.json。

## 设计与界限

DefconDefinition接收已审核MapDefinition、初始/最大/核战等级、回合改善与战场降级步长、受限动作集合和逐级地区表。全部集合防御复制、只读；要求非核战等级完整、最大等级无地理限制、等级下降限制不能减少、地区存在于地图规则标签；只支持Coup/Realignment限制，Influence/WarEvent明确不套此门禁。最多64个轨道等级为有界工程输入限制，不是规则常量。

CheckRestriction从绑定地图按CountryId取规则国家；比较Region和Subregions，不读画面坐标。未知动作/国家或坏状态是技术错误；核战等级返回NuclearWarReached，不能凭地理允许重新操作。该接口仅检查基础地理限制，Allowed不等于完整政变合法；不提供客户端ignore标记，不推测免费政变或条约例外。

PlanChange接受具名TurnStartImprovement、BattlegroundCoupDegradation、ImproveOne、DegradeOne、SetLevel。前两者读定义步长；One表示单格原语；SetLevel只接收范围内目标且仅此类型允许目标参数。改善上限无变化；降到核战等级返回NuclearWar与明确PhasingPlayer，所有下降均要求有效责任方。不按选择者推断责任；已经核战不能改善救回。计划保留Before/After与来源种类，调用方将未来整笔事务成功后才提交。服务本身没有去重或命令执行资格。

DefconDefinitionLoader.LoadSubset读取唯一defcon.json，严格根字段、schema1/content0.2.0/dataset/status/source身份、受支持来源结构、整数及动作/等级表；不打开路径、不解析完整RulePack，不修改原规则数据。源被标记verified-subset仍不等于完整规则包可启动。

## Task 1：纯定义与轨道规划

- [x] 新增Core/Rules/DefconDefinition.cs、DefconService.cs及Tests/EditMode/DefconTests.cs；先可编译桩再真实RED。
- [x] 测试完整等级/地区/动作门禁、东南亚亚洲与加拿大欧洲归属、非受限动作、只读/损坏定义、恢复非法等级、改善封顶、下降责任、核战吸收与非法目标/参数。
- [x] 实现不可变结构与纯服务，真实EditMode GREEN；既有发牌和部署回归保持。

## Task 2：数据接入、元数据与审阅

- [x] 新增Infrastructure/DataLoading/DefconDefinitionLoader.cs、DefconLoaderTests.cs；实参加载、源/版本/字段/整数/未知地区/重复键与表错误先RED后GREEN。
- [x] 更新现有defcon模块/数据摘要、risk-victory Wiki、索引、独立验证报告与工作记录；仍planned的卡牌例外与编排保持明确。
- [x] 独立整批审阅后提交、快进main、push并比对远端；此批不改Demo，通常无需重建播放器。

## Review Focus

1. 地理允许不能成为绕过NATO/FreeCoup/事件门禁的总授权。
2. State的定义/等级不分离，损坏输入不靠clamp掩盖；只对合法改善封顶。
3. 所有下降责任明确，核战输出使用PhasingPlayer，已核战不再接收改变。
4. DEFCON值、动作与地区从共享定义派生，UI或loader不复制另一张业务常量表。
5. 纯子集不宣称幂等、完整政变、终局命令或全RulePack已实现。

基线acae60a6765b76c71c2c91c190846192c13b948b；最近源码测试762项。旧PreviewDemo与FirstDemo构建保留。
