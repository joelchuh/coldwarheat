# 真实计分牌与公开地区预览实施计划

BASE: 6c641c20c85e4cc61a38e0e3e7c0c7f7c648c04e。用户已授权持续开发，可复用地图接口与共享规则数据不变。

目标：从手牌真正打出七种计分牌，在一个净VP批次中更新分数与去向；欧洲控制和普通VP胜利立即结束。默认早期手牌中的欧洲计分可实际使用。地区预览只读公开地图，与任何人的隐私手牌无关。

架构：ActionPracticeDefinition可选RegionScoringDefinition，要求Risk/Victory存在，绑定同一Map。CardActionKind增加Scoring，资格只接受手牌中Scoring类，从CardDefinition.ScoringRegion映射，绝不传客户端地区/分值。ScoringSnapshot使用当前GameState全国家、版本、显式无活动效果。CalculateUnmodified后ScoringResolvedEvidence判胜。Risk.WithVictory保留所有军事/政变记录，仅替换终局结果。继续复用CardActionCommand/host锁内替换。

非终局：共享印刷星号决定Discard/Removed，净VP一次提交并结束一个根槽。终局：与核战一样停止根完成，源卡保留Resolving；公开Checkpoint解释核战/欧洲控制/VP结束。接地区计分后太空也可能跨20，因此同时补Space的ClosedVpBatchEvidence，不能允许终局后继续行动。

T1 Core RED/GREEN：七种region映射、普通/星号去向、零净差完成、欧洲胜利无伪大分、双方净分合并、所有终局吸收/根停止、太空达到阈值、拒绝事件/中国/附带目标、state/RNG/历史保持、溢出原子性。
T2 Session/controller RED/GREEN：Scoring字段资格、公用计分分项投影（无Definition/国家快照/RNG）、同命令幂等和错用途冲突、地区预览版本/未知地区错误且无隐私依赖/副作用；控制器Scoring用途与切换后不遗留政变高亮。
T3 数据/UI：WithPracticeScoring装配共享region-scoring.json+schema，不新增每牌静态映射。追加2payload；独立Builds/ScoringDemo。手牌计分按钮；右侧结果+终局原因；地图旁公开当前地区预览，不冒充当前手牌可出牌资格。保留旧练习与可换地图。
T4 完整回归、构建、载荷/截图、Wiki/JSON/一次独立review、例行commit/ff/push。

边界：无台湾/穿梭等活动计分干预，无头条/期末/最终计分。预览不是出牌，不能泄露对手是否持计分牌。早期固定手牌不含中晚期牌，测试可明确创建守恒fixture来验七牌服务，不伪装完整时代流程。视觉/鼠标证据如受窗口工具阻挡须单列。

Review focus: 计分和太空先把双方分数合成完整批次再判胜；EuropeControl不做null算术；终局保留牌和旧收据；预览与手牌无关，不改变国家/RNG/牌；计分星号只在非终局事件完成时移出；所有Definition复制保持Risk/Scoring；未实现活动效果不可默默当无效果。


文件职责与执行：
- [x] T1 ActionPracticeState.cs 增加可选计分定义；ActionPracticeRules.Scoring.cs 计算/终局计划；PracticeRiskState.cs 保留风险历史更新胜负；ActionPracticeRules.cs 共享牌去向与Space终局；GameState.cs绑定地图。PlayScoringTests.cs先RED再GREEN，run-unity-editmode唯一RunId；每轮检查真实XML。
- [x] T2 ScoringContracts.cs公共DTO/预览；LocalGameHost.Scoring.cs读查询；CardActionContracts.cs/LocalGameHost.Cards.cs提交；DemoController.Scoring.cs缓存只读预览/使用计分牌。PlayScoringSessionTests.cs覆盖幂等/身份/隐私和副作用。
- [x] T3 DemoCatalog.Actions.cs新增装配，DemoBootstrap/DemoBuild复制两份JSON并BuildScoring；DemoView.Scoring.cs展示预览/得分和准确终局原因；Catalog测试RED/GREEN，再完整构建。
- [ ] T4 新模块JSON、Wiki、验证报告、worklog及当前阶段摘要。完整检查后固定diff交一次独立审查，常规快进/push并核对SHA。

继续沿用executing-plans，本人实现；仅批次最终一次独立review。计划任务实现可合并保存单次实现提交与后续审查记录提交，无额外用户批准节点。
