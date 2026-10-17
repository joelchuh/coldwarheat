# 经互会可玩接线

description：先定位共享参数、控制过滤、完整事件事务和实际验收状态。

先读 `Docs/Descriptors/comecon-play.module.json`，再按入口读取所需函数。核心及数据装载已通过41新增用例、3684原生全量回归；真实出牌/Demo接线仍在下一步。正式数据沿用 `effects.deluxe2015.early_influence_batch3`；原规则设计见 [早期第三批](early-influence-batch3.md)。

flow：ComeconDefinitionLoader.Load → AddToDistinctCountriesEffect.ControlFilter → InfluenceEventRules.Query按当前权威控制筛选东欧 → 固定苏联提交四个不同国家 → Plan完整校验并在副本各加一点 → 既有事件/根行动/牌区收尾。

非美国控制包含苏联控制和无人控制；有美国影响力不等于美国控制。奥地利和芬兰使用正式子区归属，不从画面坐标判断。没有普通OPS相邻/敌控费用。候选不足四国保持RequiresRuling，不自动减量、不提前移牌或完成根行动。

实际测试、审查、Player状态见 `Docs/Testing/comecon-play.validation.json`；过程与选择见 `Docs/Worklogs/2026-10-17-comecon-play.md`。地图样式与规则接口继续分离。
