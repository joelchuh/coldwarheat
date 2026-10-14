# 通用 OPS 报价

> description: 查实际行动者的Flat修正合并、共享上下限、真实牌/视同牌来源及太空请求衔接；地区奖励与活动实例生命周期另查。
> 2026-10-14：C#子集已实现，76项新增测试，全量1452项通过。入口：[模块JSON](../Descriptors/ops-general.module.json) → entrypoints符号 → 测试及[实际报告](../Testing/ops-general-quote.validation.json)。

## 计算与共享数据

`OpsGeneralDefinitionLoader.LoadSubset(json,schema)`复用持续效果首批的可信Schema，只装配`ops_policy`中的minimum、ordinary_maximum和Flat用途，检查既定版本、来源与Verified引用。数值仍在[共享参数](../../Data/Cards/deluxe-2015/persistent-effects-batch1.parameters.json)，不复制到生产常量表。Schema验证通过不代表卡牌效果已激活，也不是规则包认证。

`OpsGeneralRules.QuoteCard`读取真实CardDefinition.PrintedOps；`QuoteAsIfCard`接可信事件授予的正整数OPS，限制为Influence/Coup/Realignment。计分牌没有OPS，视同OPS不能代替实体牌作Space或门槛用牌。

只累加AffectedSide等于实际Actor的Flat delta，先求long类型的Raw=Base+Sum，再统一算`GeneralValue=max(minimum,min(ordinaryMaximum,Raw))`。例如4+1−1仍为4，不能先封顶再减成3。结果保留Base、总修正、Raw、GeneralValue和按ID排序的只读应用条目；卡牌定义不改变。中国牌不会自动附加亚洲奖励。

规则依据：[2015规则书§7.4–7.4.3](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)，按受影响玩家及实际行动者计算，通用修正合并且适用于视同OPS。

## 接口与责任

`OpsFlatModifier`只是可信来源提供的条目，不是激活事件的许可。调用方负责来源、有效期、取消与同效果重复裁定；本模块仅检查条目ID重复、空/非法输入和128条工程上限。不能用不同ID绕过上游同效果重复政策。

对Space/OpsThreshold不使用地区奖励；对Influence/Coup/Realignment，General结果仅通用部分，不能在地区条件尚未核实时充当完整可花预算。后续地区组合应使用Raw及政策，不能盲目在已保底的GeneralValue上再加奖励。PE-INTERPRETATION-01已于2026-10-14获得用户确认：1−1+1合并后保底得1，属于项目解释。地区预算仍未实现；当前General模块的范围不变。

流程：共享政策 → 真实卡或已核实视同来源 + Actor + Flat条目 → 只读Quote → SpaceAttemptRequest.FromQuote（仅真实卡/Space） → 既有太空资格/计划。

`FromQuote`固定读取报价中的Card对象、Actor和GeneralValue，保留ID/来源/回合校验。它不替代持牌、阶段、中国牌状态或活动限制，也不提交GameState。原SpaceAttemptRequest数值构造器仍为可信底层接口。

## 代码与验证

- Core/Rules/OpsGeneralRules.cs：不可变定义/条目/Quote及QuoteCard、QuoteAsIfCard。
- Infrastructure/DataLoading/OpsGeneralDefinitionLoader.cs：受支持配置与来源装配。
- Core/Rules/SpaceAttemptState.cs：FromQuote工厂。
- OpsGeneralTests.cs（46项）：两侧/所有用途、合并顺序、只读集合、边界/128条极值累计与非法输入。
- OpsGeneralLoaderTests.cs（30项）：真实共享文件、数值变化、坏配置与真实Loader→Quote→SpacePlan衔接。

未实现：地区花费历史、越南/中国牌完整奖励、持续实例状态机、完整出牌与OPS宿主、UI和播放器重建。
