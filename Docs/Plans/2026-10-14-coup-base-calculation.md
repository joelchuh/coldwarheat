# 普通政变基础计算与共享参数

Spec: Docs/Wiki/operations.md COUP-01/02及risk-victory.md §3。规则来源R2015 §6.3，本次已复核官方PDF第7页。用户持续开发及例行Git同步授权有效。

使用既有纯规则/数据加载分层；不引入可执行JSON解释器，不复制地图稳定度或战场表。范围为无活动卡牌干预的基础目标门禁和已掷骰结果计算，不宣称完整政变命令。骰子由未来宿主通过可信IRandomProvider取得；CheckTarget先于军事/抽样，Calculate再检查输入。

Core/Rules/CoupCalculation.cs定义CoupDefinition(dieMinimum,dieMaximum,stabilityMultiplier)，正整数且max>=min；CoupTargetReason Allowed/NoOpponentInfluence/GeographicRestriction/NuclearWarReached；CoupCalculation不可变保留Definition、地图目标、Actor、Before/After、Roll、Strength、long Margin、Removed/Added、RequiresBattlegroundDegradation。不返回军事奖励：CoupStrength和MilitaryCredit必须由预算策略分别提供。

CoupRules.CheckTarget(DefconState,CountryId,CountryState,Actor,IReadOnlyCollection<string> activeEffectIds)：显式集合必须非null；任何活动实例都NotSupported，保守阻止未知干预；国家从Defcon.Map读取，角色枚举验证；核战吸收优先于无敌方影响力，随后地理限制，最后敌方影响力>0；不要求己方可达/已有影响力。此集合为未来可信宿主收集，玩家不得自行声明为空。

CoupRules.Calculate(definition,defcon,target,before,actor,verifiedStrength,verifiedRoll,activeEffectIds)：强度>0、面值在共享范围、目标基础合法；long margin=roll+strength-stabilityMultiplier*stability；非正无变化，正值先min(margin,opponent)移除，剩余增加己方，int存储溢出拒绝整个计算。战场降级标记独立于成功。当前不执行军事、RNG、DEFCON/胜负/卡牌/游标，不代替身份、版本、阶段、预算和事件授权。

共享Data/Rulesets/deluxe-2015/coup.json: schema1/content0.1.0/id rules.deluxe2015.coup/status verified-subset，source R2015官方链接/checked_on/sections[6.3.1,6.3.2]，die{minimum:1,maximum:6}、stability_multiplier:2。数字由数据读取，不硬编码生产规则。采用既有严格字段加载器模式，未知/遗漏/重复字段、版本、来源、非整数和无效范围拒绝；不是完整RulePack真实性验证，不额外引入Schema。

## Task 1: 核心目标与结果

先接口桩和真实RED，再实现。测试双方、各面值边界、失败/零差/部分移除/刚好清空/溢出增加、无己方可达要求、未知/无敌方/地理/核战/活动实例、非法输入、合成数据参数与long计算/int溢出、BG不论成功均标记。Expected RED新增测试NotImplementedException；GREEN新增及已有全部通过。

## Task 2: 数据加载与依赖衔接

先桩和真实RED，再实现加载；实际数据/换数值/来源版本字段格式错误/重复JSON；实际地图Iran + 通用OPS Quote + Calculate + DefconServicePlan在测试中验证Actor与Phasing不同、BG失败仍可致核战（用合法低DEFCON地区目标），不据测试宣称宿主接线。更新模块/data JSON、catalog、Wiki、索引、README、工作记录与测试证据。Unity全量零失败/零跳过，metadata和diff --check通过。

## Review Focus

目标从Defcon绑定地图读取，不能被替换稳定度/战场值；核战不可逆、未知效果显式失败、骰子参数data-driven；long公式与int结果溢出、角色对称；已掷骰低层API不得冒充客户端命令；BG标记不是已降级，OPS强度不是军事信用。明确无RNG/事务/防重/UI构建。一次最终独立审阅，按已有授权快进同步。
