# 地区基础计分实现计划

> executing-plans inline执行，一次整批独立审阅；用户持续优化/Git授权有效。
**Goal:** 实现六地区基础档位/奖励与东南亚权重，并让胜负服务消费实际地区计分结果。
**Spec:** Docs/Wiki/region-scoring.md；Data/Rulesets/deluxe-2015/region-scoring.json；scoring-space.cases.json的SC-01至13及19基础子集。
**Architecture:** 不可变RegionScoringDefinition、ScoringSnapshot和结果；纯RegionScoringService.CalculateUnmodified/InspectActualRegionTiers；严格共享JSON加载；第五种ScoringResolvedEvidence连接既有VictoryService。
**Tech Stack:** C#9纯Core，Newtonsoft基础读取，Unity EditMode NUnit。

## 边界决定

规则地图、双方影响力和Map绑定参与判定，展示坐标/画风/手牌不参与。快照必须覆盖地图全部国家，保存SnapshotId与StateVersion，复制输入，未知/缺失不能补中立。显式传入活动计分效果ID；CalculateUnmodified对任何活动修正拒绝，避免默默忽略台湾决议或穿梭外交。InspectActualRegionTiers只查询实际地图的六主地区，不算VP、不处理/消费计分修正、无欧洲胜利。

标准档位优先Control、Domination、Presence、None，控制不额外要求非战场。档位分、战场/敌超级大国邻接奖励来自共享参数；成员按CountryId唯一，标签重叠不重复，敌方超级大国邻接用真实规则连线。欧洲Control返回独立EuropeControlWinner，基础/总分与净VP为null，不用大分数或0模拟。普通数值先完整双方汇总，再形成净VP。

东南亚独立按完整权重国家集算控制，不套档位、不叠加标准战场/邻接；正常亚洲计分仍包含这些国家，且不加权重分。共享定义绑定实际地图：标准地区覆盖地图主Region集合且每区有战场，权重覆盖子区全部成员且父区匹配。集合作防御复制，枚举有界，溢出拒绝。

仅ScoringResolvedEvidence把实际只读地区结果送入VictoryService：欧洲控制直接给胜者；否则完整净VP与先前SignedVp合并后检查普通阈值。原先普通批次前已终局的分数不能救回。只读查询不提交任何状态/终局。最终六地区汇总、中国牌附加、动态计分修正、卡牌弃置、完整宿主事务/防重仍待实现，不扩张成完整计分牌处理器。

## Task 1：纯数据与计分

- [x] Core/Rules/RegionScoringDefinition.cs、ScoringSnapshot.cs、RegionScoringService.cs和EditMode/RegionScoringTests.cs，先桩RED/GREEN。
- [x] 验证档位临界/总数及战场平手/全战场但总数平手、控制无非战场也合法、共享阈值、官方中美洲-4例、双重奖励、欧洲独立信号。
- [x] 验证东南亚+1例/不加泰国战场分/亚洲不叠权重、重叠标签只算一次、状态/地图错配/修正活动/溢出/只读防御复制。

## Task 2：共享JSON及胜负接缝

- [x] Infrastructure/DataLoading/RegionScoringDefinitionLoader.cs及LoaderTests；实际数据与损坏/缺漏/错类型/重复/来源策略边界先RED/GREEN。
- [x] VictoryEvidence/Checkpoint/Service增加ScoringResolvedEvidence；新测试证明只有实际计分检查点才消费欧洲信号，数值净分完整后判胜。
- [x] 保留原有1009项回归；不改共享游戏数据的分值或正式地图。
- [x] 同步region-scoring/victory模块、data描述、catalog、Wiki/README和实际报告，用正式metadata工具校验。
- [x] 整批独立审阅、按授权提交/快进main/push核验：e4977b5。无UI接线，不重建播放器。

## Review Focus

1. T/B/N判定严格大于与优先级，避免多算档位或空战场假Control。
2. 欧洲控制用独立终局信号且只在计分证据生效；不能对null算分。
3. SEA与Asia两套算法不串线，邻接取规则图而非显示位置。
4. 活动修正缺处理器必须拒绝；实际档位查询明确不应用计分专用修改。
5. 同一完整地图快照、不可变输出、int溢出与未来最终汇总完整性边界。
