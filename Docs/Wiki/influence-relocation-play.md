# 去斯大林化可玩接线

description：查完整来源/目的分配、迁移守恒、角色与地图草稿的实现状态。

先读 `Docs/Descriptors/influence-relocation-play.module.json`；正式规则背景及DS1-OVERLAP见 [原设计](influence-relocation.md)。核心已通过66新增用例及3584原生回归，玩家会话与地图尚在接线，不据此宣布整张牌可玩。

核心flow：`InfluenceRelocationDefinitionLoader.Load`读取既有参数 → `InfluenceEventRules.Query`给出两类只读容量 → `InfluenceEventSelection.Relocate`复制完整有界分配 → `InfluenceEventRules.Plan`校验身份/版本/显式限制/守恒/重叠 → `InfluenceMutation`工作副本排序移出、移入 → 一次新状态。拒绝无NextState；零迁移也生成完成记录。根行动和牌区提交交由后续宿主接线。

数值仍在 `effects.deluxe2015.influence_relocation`；每国实际影响力和控制从地图状态查询。没有普通OPS邻接/费用。来源允许美国控制国，目的不允许；同国重叠符合形状但依赖待裁定时返回RequiresRuling，不静默净额抵销。

测试/构建/审查状态分别记录于 `Docs/Testing/influence-relocation-play.validation.json`，过程见 `Docs/Worklogs/2026-10-17-influence-relocation-play.md`。后续在同模块补固定会话、双草稿地图与实际Player验收。
