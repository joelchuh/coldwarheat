# 项目知识索引

先根据 description 选择主题，再读取知识点及其引用。索引不复制完整文档或源文件。

| 知识点 | description | 状态 | 入口 |
|---|---|---|---|
| 开发工作流与共享数据 | 查找模块/脚本 JSON 描述、脚本化条件、共享数据与执行边界 | 已采用；自动化工具未实现 | [workflow-and-data.md](workflow-and-data.md) |
| 工作流策略 JSON | 快速定位结构化开发约定和描述契约 | 已采用 | [workflow-policy.json](../Workflow/workflow-policy.json) |
| 游戏模块设计 | 查找大模块、规则子模块、命令流程、换画风边界与验收顺序 | v0.1 设计草案；待实现 | [architecture.md](architecture.md) |
| 回合与出牌状态机 | 查找阶段转换、头条保密、事件/OPS 顺序、卡牌去向和读档继续位置 | v0.2 编排设计；待实现 | [turn-flow.md](turn-flow.md) |
| 风险与胜负服务 | 查找 DEFCON 限制与责任、军事净结算、胜负检查点及扣留计分牌矩阵 | v0.1 设计；CF-003已确认 | [risk-victory.md](risk-victory.md) |
| 地区计分与最终汇总 | 查找六地区档位、东南亚权重、最终完整性和中国牌附加分 | v0.1设计；待实现 | [region-scoring.md](region-scoring.md) |
| 太空竞赛 | 查找8格门槛、先后奖励、领先能力、事件推进与回合衔接 | v0.1设计；待实现 | [space-race.md](space-race.md) |
| 地图与卡牌数据契约 | 查找正式数据字段、邻接、三类卡牌、效果注册、Schema及加载门禁 | 契约与正式印刷定义已建立；服务待实现 | [map-card-contracts.md](map-card-contracts.md) |
| 正式地图与卡牌数据 | 按地区查84国与邻接，按时代查103张基础牌及效果待实现边界 | 静态定义已录入；不是可运行规则包 | [map-card-data.md](map-card-data.md) |
| 地图布局与未定画风 | 区分规则图、位置拾取和主题资源，了解2D/3D接入边界 | 设计稿；未选定美术 | [map-presentation.md](map-presentation.md) |
| 数据结构契约目录 | 按description定位六份JSON Schema（含卡牌v1/v2）与合成样例 | 结构契约；不是完整规则包 | [catalog.json](../Contracts/catalog.json) |
| 模块描述目录 | 先按 description 定位模块，再读完整契约与相关代码 | 八个模块描述均 planned | [catalog.json](../Descriptors/catalog.json) |
| 共享规则数据目录 | 查找数据集描述、来源与独立参数文件 | 七个参数子集＋地图/卡牌定义＋名称字典；运行服务待实现 | [catalog.json](../../Data/catalog.json) |
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
