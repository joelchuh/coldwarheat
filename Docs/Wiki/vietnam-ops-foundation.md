# 越南起义运行基础

description：固定国家加点与本回合地区OPS贡献，先读模块JSON，再定位相关符号。

状态：基础已通过3792项原生回归，尚未接入真实出牌或Demo。参数唯一来源effects.deluxe2015.persistent_batch1；原设计见persistent-effects.md。
flow：严格装载 → 地区修正定义与固定加点定义 → 既有影响力事务／不可变回合实例 → 实际Actor筛选地区贡献 → 既有OpsRegionalBudget。真实出牌必须把两部分在同一宿主事务发布，不能先单独提交；该接线为下一批。
正式数值不写入脚本，地图仍用稳定ID和规则地区；图像/材质/坐标不参与。证据见Docs/Testing/vietnam-ops-foundation.validation.json。

代码边界：TurnOpsEffectDefinition区域构造器将固定SU、OpsRegionalBonus与AddFixedInfluenceEffect绑定；旧Flat构造器仍只允许US或控制者对手。GetModifiers排除地区贡献；GetRegionalBonuses校验当前回合与Actor。AddFixedInfluenceEffect经InfluenceEventRules共用事务缓冲和门禁，公开Flow可区分根出牌者与收益方。当前PracticeTurnOpsCatalog仍最多两张Flat，不能误把基础定义直接当作已接真实卡。

后续进展：真实出牌和Demo接线见[vietnam-play](vietnam-play.md)。上文是本基础批次的交付边界；旧Flat构造仍拒绝地区定义，新接线通过显式WithVietnam注册。
