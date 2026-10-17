# 去斯大林化可玩接线

description：查完整来源/目的分配、迁移守恒、角色与地图草稿的实现状态。

先读 `Docs/Descriptors/influence-relocation-play.module.json`；正式规则背景及DS1-OVERLAP见 [原设计](influence-relocation.md)。核心已通过66新增用例及3584原生回归，固定身份会话及真实出牌通过新增33项、3617原生回归，双草稿地图再新增26项，3643原生测试通过；Player和鼠标验收仍待完成。

核心flow：`InfluenceRelocationDefinitionLoader.Load`读取既有参数 → `InfluenceEventRules.Query`给出两类只读容量 → `InfluenceEventSelection.Relocate`复制完整有界分配 → `InfluenceEventRules.Plan`校验身份/版本/显式限制/守恒/重叠 → `InfluenceMutation`工作副本排序移出、移入 → 一次新状态。拒绝无NextState；零迁移也生成完成记录。EventCommandEnvelope.Relocate复制双列表，固定会话验证身份和幂等；InfluenceEventFlow.ResolveRelocation复用ActionPracticeRules/HeadlineResolutionRules原子收尾和移牌。

数值仍在 `effects.deluxe2015.influence_relocation`；每国实际影响力和控制从地图状态查询。没有普通OPS邻接/费用。来源允许美国控制国，目的不允许；同国重叠符合形状但依赖待裁定时返回RequiresRuling，不静默净额抵销。

测试/构建/审查状态分别记录于 `Docs/Testing/influence-relocation-play.validation.json`，过程见 `Docs/Worklogs/2026-10-17-influence-relocation-play.md`。两个人工入口 demo.practice.relocation_ussr / demo.practice.relocation_us；先选牌使用事件，美国打出时切至苏联。地图先编辑移出，再编辑移入；总数相等才确认，两份空草稿按钮明确表示不迁移。切换地图保留草稿，切身份/状态/重置清空。

表现flow：WithInfluenceRelocation/WithRelocationScenarios → DemoController.Relocation查询容量并维护两份本地字典 → RelocationMapTargets按编辑阶段高亮 → ConfirmEventAsync提交完整双列表。旧CountrySet/Allocation入口保持原语义。新构建方法BuildRelocationPlaytest；共享Player映射新增三个文件，总69项。
