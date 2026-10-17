# 越南起义真实出牌

description：越南事件即时加点、苏联东南亚条件OPS、对手牌与奥运事件OPS、公开反馈及人工Demo入口。先读对应模块JSON，再按entrypoints定位函数。

状态：真实Core/Host/UI与Demo注册已通过3840项原生回归；71份载荷一致；一次独立审查Important已修复。实机验证越南事件、中国牌地区切换与奥运会抵制后政变；头条/对手路径仅原生覆盖。普通选牌长说明已增加可滚动区域，说明变化归顶，实际验收见[补丁报告](../Testing/card-reason-scroll.validation.json)。固定加点与地区实例基础见[vietnam-ops-foundation](vietnam-ops-foundation.md)。
flow：严格装载共享参数 → WithVietnam显式注册 → Play/Headline/Opponent根出牌 → 私有固定加点Flow候选 → Activate回合实例 → 同一父事务完成与Host发布。成功收据保留国家变化与地区效果两部分。
OPS flow：实际Actor → Flat普通报价 → 当前Actor地区贡献 + 实际中国牌来源奖励 → OpsRegionalBudget跟随每个已接受目标 → 影响力/调整预算或目标政变强度。Space/阈值不读地区奖励；奥运事件OPS不凭空获得中国牌奖励。

参数唯一来源Data/Cards/deluxe-2015/persistent-effects-batch1.parameters.json；地图ID和地区关系来自正式world数据，与地图材质/坐标/画风无关。小值1−1+1=1沿用用户确认PE-INTERPRETATION-01；新resolution重复本回合同效果同方仍RequiresRuling，不新增裁定。
边界：本批只接已支持OPS来源；未来新的事件OPS必须调用同一预算入口并增加行为测试。五年计划只处理美国子牌，越南不纳入其子事件目录。
证据：Docs/Testing/vietnam-play.validation.json；过程与自主决定：Docs/Worklogs/2026-10-17-vietnam-play.md。

Demo入口：demo.practice.vietnam，第39场景；BuildVietnamPlaytest → Builds/VietnamPlaytest。影响力明细与地区条件在同一收据显示；world/tiles地图接口复用。结构见[事件与地区OPS接线](../Architecture/vietnam-play.md)。
