# 普通政变基础计算

> description: 查询基础目标合法性、已掷骰结果的影响力变化、共享参数和战场降级意图；完整政变命令仍待实现。
> 2026-10-14：纯C#子集59项新增测试，全量1511通过；没有播放器重建。

## 数据与入口

[模块描述](../Descriptors/coup-calculation.module.json) → Core/Rules/CoupCalculation.cs 的 CoupDefinition、CoupRules.CheckTarget / Calculate；Infrastructure/DataLoading/CoupDefinitionLoader.LoadSubset。
[数据描述](../../Data/Descriptors/coup.data.json) → [coup.json](../../Data/Rulesets/deluxe-2015/coup.json)，存骰子1–6和稳定度乘数2；规则数值由定义注入，正式国家稳定度/战场属性来自DEFCON所绑定的MapDefinition。不根据中文名或画面位置判断。

已核对[2015官方规则书](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf) PDF第7页§6.3：目标需有敌方影响力；结果差值是骰子+有效OPS−稳定度×共享乘数，正值先移除对手，余量增加己方；战场降级不以成功为前提。地区门禁复用DefconService。没有己方影响力或相邻可达不是基础政变的阻止条件。

## 流程与边界

1. 未来宿主检查身份、版本、阶段、预算和卡牌干预，提供当前影响力与真实活动效果集合。
2. CheckTarget在取骰子前进行：核战/地理门禁/敌方影响力；当前任何非空效果集合明确NotSupported，不猜例外。
3. 宿主在完整事务计划中先记军事信用，再通过可信随机源取骰子。军事信用与政变强度是独立值。
4. Calculate接收已核实面值/强度，再次检查基础门禁，以long算差值，以checked保证int影响力存储不溢出，返回不可变Before/After、移除/新增量和战场标记。
5. 宿主按战场标记处理DEFCON与核战；根PhasingPlayer可以不同于Actor。游戏状态、军事、随机游标、卡牌、行动游标与终局必须原子提交。

**本基础模块只实现第2步和第4步**。[后续整体计划](coup-attempts.md)已将军事/随机/影响力/DEFCON和收据组合为不可变After；实际GameState/出牌宿主仍未接入。Calculate不是玩家命令，不读取随机、不自动降级、不重复记军事、不宣布胜者；测试中的组合调用不等于实际游戏接线。

加载器采用严格字段/profile/来源及数值校验，复用有界且拒绝重复属性的ContractJson。它仅装配共享基础参数，不认证完整RulePack。骰范围和系数支持合成定义，用测试证明生产代码没有另一份数值表。

未实现的地区奖励必须先通过未来完整预算策略；General OPS值不能无条件充当最终政变强度。当前空效果参数只能由可信宿主提供，不能让客户端用空集合绕过卡牌限制。未确认的地区小值解释不在本批执行。

## 验证与下一步

CoupCalculationTests覆盖双方对称、各面值、零差/失败、先移除后增加、溢出、防止未知效果与地理/核战绕过；CoupLoaderTests覆盖真实文件、改变数据值、错误profile/类型/来源/字段、正式Iran影响力及Cuba失败战场降级的责任传递。见[实际验证报告](../Testing/coup-base-calculation.validation.json)。

可防重的[普通政变整体计划](coup-attempts.md)已实现，下一步接入权威GameState、出牌和地图命令；逐卡干预按既有明确规则分批接入。
