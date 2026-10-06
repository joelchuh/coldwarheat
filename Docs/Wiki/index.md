# 项目知识索引

先根据 description 选择主题，再读取知识点及其引用。索引不复制完整文档或源文件。

| 知识点 | description | 状态 | 入口 |
|---|---|---|---|
| 开发工作流与共享数据 | 查找模块/脚本 JSON 描述、脚本化条件、共享数据与执行边界 | 已采用；只读核验工具就绪，其他自动化待实现 | [workflow-and-data.md](workflow-and-data.md) |
| 正式脚本目录 | 按description找只读设计快照核验与安全临时目录测试 | 2正式脚本可用；不执行游戏/Git | [catalog.json](../../Tools/Descriptors/catalog.json) |
| 工作流策略 JSON | 快速定位结构化开发约定和描述契约 | 已采用 | [workflow-policy.json](../Workflow/workflow-policy.json) |
| 游戏模块设计 | 查找大模块、规则子模块、命令流程、换画风边界与验收顺序 | v0.1 设计草案；待实现 | [architecture.md](architecture.md) |
| 早期影响力事件第一批 | 查5牌参数、复用原语、受益方选择和事务继续机制 | 参数已绑定，C#与跨卡干预待实现/核实 | [early-influence-batch1.md](early-influence-batch1.md) |
| 早期影响力事件第二批 | 查总额度/每国上限、东欧动荡时代量、铁娘子禁止与用户确认的短缺策略 | 3牌参数已绑定；C#待实现 | [early-influence-batch2.md](early-influence-batch2.md) |
| 早期影响力事件第三批 | 查独立红色政权追平、经互会敌控筛选与候选不足边界 | 2牌参数已绑定；C#待实现 | [early-influence-batch3.md](early-influence-batch3.md) |
| 持续效果与OPS修正 | 查激活/到期/取消、行动者归属、地区奖励与三牌参数 | 设计与参数；C#待实现 | [persistent-effects.md](persistent-effects.md) |
| 条约保护与前置事件 | 查北约/美日安保、动态控制、法国西德例外、DEFCON接缝与事件成功记录 | 2牌参数绑定；C#待实现 | [treaty-protection.md](treaty-protection.md) |
| 战争牌持续与取消 | 查Flower使用资格/待裁定时机、戴维营固定收益、邪恶帝国取消与阻止 | 3牌参数Bound；C#待实现，钩子边界待裁定 | [war-card-hooks.md](war-card-hooks.md) |
| DEFCON事件与政变干预 | 查古巴危机解除/责任、核潜艇原因豁免、SALT取回和ABM普通OPS授权 | 4牌参数Bound；C#待实现 | [defcon-events-batch1.md](defcon-events-batch1.md) |
| 军政府分步事件 | 查先放影响/可选免费行动/源牌预算/调整地区锁 | 1牌参数Bound；待实现 | [junta.md](junta.md) |
| 陷阱根行动与标准骰 | 查Quagmire/BearTrap强迫弃牌/计分/逃脱/跨回合 | 2牌参数Bound；待实现 | [trap-events.md](trap-events.md) |
| UN双牌使用 | 查双实体/配牌预算/压制/一根slot与未知分支 | 1牌参数Bound；待实现 | [un-intervention.md](un-intervention.md) |
| 持续修正第二批 | 查Brezhnev实际OPS与LADS地区政变骰/重复停止 | 2牌参数Bound；待实现 | [persistent-effects-batch2.md](persistent-effects-batch2.md) |
| 后续回合修正蓝图 | 查此前接口与重复停止的研究记录 | 历史蓝图，已由本批契约展开 | [历史蓝图](turn-modifiers-next.md) · [当前契约](persistent-effects-batch2.md) |
| 中期影响力第二批 | 查解放神学额度/穆斯林8国名单/南非分支邻国 | 3牌参数Bound；待实现 | [mid-influence-batch2.md](mid-influence-batch2.md) |
| 国家指标VP事件 | 查Kitchen/进步联盟/里根对实际局面的计数与VP | 3牌参数Bound；待实现 | [country-metric-vp-events.md](country-metric-vp-events.md) |
| 事件太空首批 | 查Scientist一格推进、奖励/能力和终点待裁定 | 1牌参数Bound；待实现 | [space-events-batch1.md](space-events-batch1.md) |
| 教皇／团结工会链 | 查Poland影响力及成功来源关联的活动许可 | 2牌参数Bound；短缺待裁定 | [solidarity-chain.md](solidarity-chain.md) |
| 中期影响力第一批 | 查葡萄牙/阿连德/萨达特/OAS固定目标、清除和中南美额度 | 4牌参数Bound；C#待实现 | [mid-influence-batch1.md](mid-influence-batch1.md) |
| DEFCON与VP顺序事件 | 查鸭子/核禁/停止忧虑的先后、根核战责任、动态VP与直接固定军事 | 3牌参数Bound；C#待实现 | [defcon-vp-events.md](defcon-vp-events.md) |
| 七张计分牌绑定 | 查CardId/RegionId直接绑定、标准/SEA算法、头条/强制弃牌与去向 | 7牌参数Bound；C#待实现 | [scoring-cards.md](scoring-cards.md) |
| 动态计分效果 | 查台湾战场/中国取消、穿梭选国/邻接/Control与一次消费 | 2牌参数Bound，C#待实现 | [scoring-effects-batch1.md](scoring-effects-batch1.md) |
| 手牌事件与子事件 | 查封锁阈值弃牌、五年计划随机弃牌/US子帧、保密与回放边界 | 2牌参数Bound，C#待实现 | [hand-events-batch1.md](hand-events-batch1.md) |
| 影响力迁移 | 查去斯大林化来源/目的分配、守恒、零迁移与重叠待裁定 | 1牌已知参数Bound，C#待实现 | [influence-relocation.md](influence-relocation.md) |
| 持续规则设计批次 | 查当前进行批次、后续队列、逐批验证/Git收尾和未实施边界 | active；运行实现未开始 | [rules-design-roadmap.json](../Design/rules-design-roadmap.json) |
| 战争事件第二批 | 查Brush稳定度与北约保护、超级大国邻接、两伊目标及共享结算 | 2牌参数绑定；五张战争参数齐备，C#待实现 | [war-events-batch2.md](war-events-batch2.md) |
| 战争事件第一批 | 查战争单骰修正、目标选择、影响力转移、军事/VP与戴维营阻止 | 3牌参数绑定；C#待实现 | [war-events.md](war-events.md) |
| 事件授权与欧洲额外行动 | 查拆墙完整步骤、美国OPS预算、可选政变/逐次调整、地区豁免与根行动责任 | 1牌参数绑定；C#待实现 | [event-operations.md](event-operations.md) |
| 北约局部例外与组合事件 | 查戴高乐/勃兰特的固定步骤、VP终局、取消阻止与局部保护恢复 | 2牌参数绑定；C#待实现 | [nato-exceptions.md](nato-exceptions.md) |
| 北约前置影响力事件 | 查马歇尔西欧筛选/确认短缺、华沙互斥分支、额度和成功记录 | 2牌参数绑定；C#待实现 | [alliance-prerequisites.md](alliance-prerequisites.md) |
| 初始布置与开局 | 查固定影响力、自由部署、私有初始手牌、轨道引用、幂等与首回合衔接 | 数据和流程设计已建立；服务待实现 | [setup.md](setup.md) |
| 回合与出牌状态机 | 查找阶段转换、头条保密、事件/OPS 顺序、卡牌去向和读档继续位置 | v0.2 编排设计；待实现 | [turn-flow.md](turn-flow.md) |
| 风险与胜负服务 | 查找 DEFCON 限制与责任、军事净结算、胜负检查点及扣留计分牌矩阵 | v0.1 设计；CF-003已确认 | [risk-victory.md](risk-victory.md) |
| 地区计分与最终汇总 | 查找六地区档位、东南亚权重、最终完整性和中国牌附加分 | v0.1设计；待实现 | [region-scoring.md](region-scoring.md) |
| 太空竞赛 | 查找8格门槛、先后奖励、领先能力、事件推进与回合衔接 | v0.1设计；待实现 | [space-race.md](space-race.md) |
| 地图与卡牌数据契约 | 查找正式数据字段、邻接、三类卡牌、效果注册、Schema及加载门禁 | 契约与正式印刷定义已建立；服务待实现 | [map-card-contracts.md](map-card-contracts.md) |
| 正式地图与卡牌数据 | 按地区查84国与邻接，按时代查103张基础牌及效果待实现边界 | 静态定义已录入；不是可运行规则包 | [map-card-data.md](map-card-data.md) |
| 地图布局与未定画风 | 区分规则图、位置拾取和主题资源，了解2D/3D接入边界 | 设计稿；未选定美术 | [map-presentation.md](map-presentation.md) |
| 数据结构契约目录 | 按description定位三十四份JSON Schema（含事件参数、setup及卡牌v1/v2）与合成样例 | 结构契约；不是完整规则包 | [catalog.json](../Contracts/catalog.json) |
| 模块描述目录 | 先按 description 定位模块，再读完整契约与相关代码 | 二十一个模块描述均 planned | [catalog.json](../Descriptors/catalog.json) |
| 共享规则数据目录 | 查找数据集描述、来源与独立参数文件 | 三十一个参数子集＋地图/卡牌/开局定义＋名称字典；运行服务待实现 | [catalog.json](../../Data/catalog.json) |
| 行动规则 | 查找影响力、政变、调整、预算、对象契约与验收案例 | v0.2 设计稿；未实现 | [operations.md](operations.md) |
| 规则资料 | 查找版本来源、FAQ 适用边界和资料冻结缺口 | 已登记；未完整冻结 | [rules-sources.md](rules-sources.md) |
| 工程边界 | 查找纯 C# 核心、命令、事件、玩家视角、确定性和程序集约束 | 设计约定；待实现 | [AGENTS.md](../../AGENTS.md) |
| Git 工作流 | 理解本地提交、远程同步、忽略文件及每次工作的收尾方式 | 本地已关联；推送另行核验 | [git-workflow.md](git-workflow.md) |
| GitHub 保存 | 查找 coldwarheat 地址、线上提交方式和本地同步限制 | 已记录 | [GitHub 工作记录](../Worklogs/2026-09-17-github-bootstrap.md) |
| 初始化记录 | 查找本次建立的文件、实际验证和尚未完成事项 | 已记录 | [工作记录](../Worklogs/2026-09-16-repository-bootstrap.md) |

## 后续知识点的最小结构

每个实际需要的主题记录：description、实现状态、规则来源及版本、输入输出、关键不变量、代码路径与符号、直接依赖、流程、对应测试、待确认问题。

代码位置以稳定路径和符号名为主，行号只作辅助。没有代码的设计条目注明“待实现”，不得编造函数或测试。

计划逐步展开：领域模型、规则结算、对局阶段、卡牌效果、玩家选择、会话与隐藏信息、随机性、存档回放、表现层边界。此列表不代表已实现功能。
