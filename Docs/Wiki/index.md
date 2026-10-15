# 项目知识索引

先根据 description 选择主题，再读取知识点及其引用。索引不复制完整文档或源文件。

| 知识点 | description | 状态 | 入口 |
|---|---|---|---|
| 回合末结算试玩 | 查军事净罚分、扣留审计矩阵、中国牌翻面与公开结算报告 | 有限单回合；验证见报告 | [turn-end-playtest.md](turn-end-playtest.md) |
| 对手事件＋投放 | 查两种顺序、选择者与根操作方、一次移牌和终局截断 | 有限二步试玩；见验证报告 | [opponent-event-influence.md](opponent-event-influence.md) |
| 普通政局调整试玩 | 查真实牌预算、逐次双骰、动态修正与地图目标 | RealignmentPlaytest；验证见报告 | [playable-realignment.md](playable-realignment.md) |
| DEFCON与VP事件试玩 | 查Duck/Test Ban顺序、核战责任与终局截断 | DefconEventPlaytest；1729项测试，鼠标未验收 | [playable-defcon-vp-events.md](playable-defcon-vp-events.md) |
| 有界JSON契约 | 查Schema支持边界、tuple与尾项验证 | 共享加载器基础服务 | [bounded-json-contract.md](bounded-json-contract.md) |
| 真实出牌影响力事件 | 查八牌事件、受益方选择、星号去向与原子收尾 | EventPlaytest；1691项测试，鼠标未验收 | [playable-influence-events.md](playable-influence-events.md) |
| 快速行动试玩 | 查示例部署、默认入口、真牌政变/计分和重置 | PlaytestDemo；证据见报告 | [quick-start-playtest.md](quick-start-playtest.md) |
| 计分与公开预览 | 查七牌绑定、净VP/终局与不读手牌的地区预览 | 1648项回归；ScoringDemo | [playable-scoring-actions.md](playable-scoring-actions.md) |
| 政变与风险试玩 | 查真实手牌政变、地图高亮、风险轨道、核战和幂等 | 新RiskDemo；验证见报告 | [playable-coup-actions.md](playable-coup-actions.md) |
| 真实出牌行动试玩 | 查从真实手牌使用影响力/太空、牌去向与轨道、明确跳过头条边界 | 1599项回归；独立PlayableDemo | [playable-card-actions.md](playable-card-actions.md) |
| 卡牌印刷静态目录 | 查103牌类型、ID/编号/时代查询、来源和冻结摘要；不执行事件 | 634项EditMode通过，完整规则包待实现 | [card-catalog.md](card-catalog.md) |
| 卡牌效果引用索引 | 仅核对稳定效果ID存在性，不判断完整卡牌可执行 | 102个只读引用已接入两牌参数装配 | [card-effect-reference-index.md](card-effect-reference-index.md) |
| 回合日历与权威时代 | 查共享时代表、不可变Turn及复制保持，区别于完整状态机 | 371回归通过；不自动推进阶段 | [turn-calendar.md](turn-calendar.md) |
| 从原型到完整游戏 | 区分exe、源码和设计，查询地图可替换边界与可玩里程碑 | 诊断原型；完整对局待接通 | [prototype-roadmap.md](prototype-roadmap.md) |
| 开局与规则 Demo | 查运行、发牌/部署、影响力/会话/UI/构建模块及真实测试 | 762项EditMode通过；十三练习含真实手牌，鼠标实测待完成 | [first-demo.md](first-demo.md) |
| 世界地图占位视图 | 查86节点布局、公共领域陆地、镜头、拾取和资源装配 | 467项回归通过；局部标签实际渲染已核对，鼠标待验收 | [world-map-view.md](world-map-view.md) |
| 单人双边测试与首批计划 | 查一个客户端、两方身份、调试观察和正常信息过滤，以及首批核心任务 | 首批影响力已实现 | [模式说明](local-test-mode.md) · [计划](../Plans/2026-10-13-core-influence-first-slice.md) · [JSON](../Plans/2026-10-13-core-influence-first-slice.json) |
| 游戏实现与可替换地图接口 | 查整体分层、地图输入输出、状态面板和首批影响力闭环 | 影响力与同步诊断地图接口已实现；资源接口待接入 | [接口提案](../Design/implementation-architecture.proposal.md) · [JSON](../Design/implementation-architecture.proposal.json) |
| 编码前方向讨论 | 比较第一版交付顺序、统一状态、占位地图与数据边界 | 方向已认可；首批已实现 | [coding-direction-discussion.md](../Design/coding-direction-discussion.md) · [JSON](../Design/coding-direction-discussion.json) |
| 全局平局结果契约 | 查8组完整状态的Draw表示、旧命令重放与新行动拒绝边界 | Schema修订；游戏运行待实现 | [draw-global-contracts.md](draw-global-contracts.md) |
| 2026-10-13项目裁定 | 查5牌已获用户确认的短缺、军事比较与零分结束政策 | 已确认；运行待实现 | [confirmed-rulings-20261013.md](confirmed-rulings-20261013.md) |
| 借用卡牌模块 | 查父子帧、角色、能力表与表现层边界 | 模块planned；三张借牌事件有限设计 | [nested-card-events.md](nested-card-events.md) |
| Missile Envy | 查最高OPS交换、用户解释与后续强制义务 | 参数Bound；真实OPS/回合/陷阱待实现 | [missile-envy.md](missile-envy.md) |
| Grain Sales | 查随机借牌、私有使用/退回与有限路线 | 1牌参数Bound；随机与运行服务待实现 | [grain-sales.md](grain-sales.md) |
| Star Wars | 查公开弃牌、空池解释与有限子事件 | 1牌参数Bound；消费者运行待实现 | [star-wars.md](star-wars.md) |
| 嵌套出牌下一步计划 | 查Star Wars、Grain Sales、Missile Envy及Draw兼容审计顺序 | 分批设计；三牌参数已绑定；全部运行服务待实现 | [nested-card-sources.md](nested-card-sources.md) |
| 开发工作流与共享数据 | 查找模块/脚本 JSON 描述、脚本化条件、共享数据与执行边界 | 已采用；只读核验工具就绪，其他自动化待实现 | [workflow-and-data.md](workflow-and-data.md) |
| 元数据校验工具 | 查共享配置、catalog/描述/入口/数据依赖检查和只读边界 | 正式工具；48案例及2次真实链接检查 | [metadata-validation.md](metadata-validation.md) |
| 正式脚本目录 | 按description找快照核验、审阅基线导出及对应测试 | 已登记正式脚本；含有界Unity验证，均不自动Git同步 | [catalog.json](../../Tools/Descriptors/catalog.json) |
| Unity验证工作流 | 查版本校验、预览、新输出、XML真实计数与失败保留 | 31脚本测试与149游戏测试通过；独立复审通过 | [unity-verification-workflow.md](unity-verification-workflow.md) |
| 审阅基线导出 | 查干净HEAD、受跟踪工作字节、新目标、SHA清单与失败边界 | 工作流工具；游戏实现未开始 | [export-review-baseline.md](export-review-baseline.md) |
| 工作流策略 JSON | 快速定位结构化开发约定和描述契约 | 已采用 | [workflow-policy.json](../Workflow/workflow-policy.json) |
| 游戏模块设计 | 查找大模块、规则子模块、命令流程、换画风边界与验收顺序 | 总体设计；首批影响力已实现 | [architecture.md](architecture.md) |
| 首批事件C#规划 | 查八牌纯规划、选择校验、参数加载与后续事务接入 | 八牌有宿主与UI，387项回归通过 | [first-influence-events.md](first-influence-events.md) |
| 早期影响力事件第一批 | 查5牌参数、复用原语、受益方选择和事务继续机制 | 五牌基础C#与选择事务已实现；跨卡干预待实现/核实 | [early-influence-batch1.md](early-influence-batch1.md) |
| 早期影响力事件第二批 | 查总额度/每国上限、东欧动荡时代量、铁娘子禁止与用户确认的短缺策略 | 三牌规划/宿主/UI已实现；含时代对照 | [early-influence-batch2.md](early-influence-batch2.md) |
| 早期影响力事件第三批 | 查独立红色政权追平、经互会敌控筛选与候选不足边界 | 2牌参数已绑定；C#待实现 | [early-influence-batch3.md](early-influence-batch3.md) |
| 持续效果与OPS修正 | 查激活/到期/取消、行动者归属、地区奖励与三牌参数 | 设计与参数；C#待实现 | [persistent-effects.md](persistent-effects.md) |
| 条约保护与前置事件 | 查北约/美日安保、动态控制、法国西德例外、DEFCON接缝与事件成功记录 | 2牌参数绑定；C#待实现 | [treaty-protection.md](treaty-protection.md) |
| 战争牌持续与取消 | 查Flower使用资格/待裁定时机、戴维营固定收益、邪恶帝国取消与阻止 | 3牌参数Bound；C#待实现，钩子边界待裁定 | [war-card-hooks.md](war-card-hooks.md) |
| DEFCON事件与政变干预 | 查古巴危机解除/责任、核潜艇原因豁免、SALT取回和ABM普通OPS授权 | 4牌参数Bound；C#待实现 | [defcon-events-batch1.md](defcon-events-batch1.md) |
| Ask Not换手牌 | 查先弃后抽、牌堆重洗与隐私边界 | 1牌参数Bound；抽牌服务待实现 | [ask-not.md](ask-not.md) |
| 拉美债务危机 | 查US私有OPS弃牌与USSR两国翻倍选择 | 1牌参数Bound；真实消费者待实现 | [latin-american-debt-crisis.md](latin-american-debt-crisis.md) |
| 晚期当回合限制 | 查伊朗门US骰方修正与切尔诺贝利主地区OPS门禁 | 2牌参数Bound；真实消费者待实现 | [late-restrictions-batch1.md](late-restrictions-batch1.md) |
| 延迟VP事件 | 查WWBY即时核战/next-US豁免及U2本回合UN奖金 | 2牌参数Bound；未定检查点待核，待实现 | [deferred-vp-events-batch1.md](deferred-vp-events-batch1.md) |
| 晚期受限行动事件 | 查KAL/Glasnost/Ortega顺序前缀和共享行动交接 | 3牌参数Bound；实际共享行动待实现 | [late-event-operations-batch1.md](late-event-operations-batch1.md) |
| 叛逃者 | 查头条取消、苏方自取消与用牌VP | 1牌参数Bound；未知嵌套待核，待实现 | [defectors.md](defectors.md) |
| 晚期手牌事件 | 查人质加倍、恐怖主义随机弃牌及Ames持续私有视图 | 3牌参数Bound；短缺待核，待实现 | [late-hand-batch1.md](late-hand-batch1.md) |
| OPEC与北海石油 | 查七国控制VP、永久阻止、回合额度与太空名额合并 | 2牌参数Bound；未知重复/迟到来源待核，待实现 | [opec-north-sea-batch1.md](opec-north-sea-batch1.md) |
| 战争游戏 | 查三选择、DEFCON前提、完整VP批次与立即结束 | 1牌参数Bound；恰零结束判平局已确认，待实现 | [wargames.md](wargames.md) |
| 中国牌事件转交 | 查持有者分支、面向、VP/亚洲奖励及独立牌区 | 3牌参数Bound；运行服务待实现 | [china-transfer-batch1.md](china-transfer-batch1.md) |
| 奥运会 | 查对手分支、平局强制重掷、内部续跑与抵制后普通OPS | 1牌参数Bound；特殊来源影响力起点待核实，待实现 | [olympic-games.md](olympic-games.md) |
| 晚期影响力第一批 | 查铁娘子/改革者/海军营房/潘兴II及长期禁止、VP先行 | 4牌参数Bound；Marine短缺解释已确认，待实现 | [late-influence-batch1.md](late-influence-batch1.md) |
| 一次手牌揭示与行动 | 查CIA/Lone私有快照、收据重放、普通OPS及影响力起点边界 | 2牌参数Bound；头条影响力起点待核实，待实现 | [reveal-ops-batch1.md](reveal-ops-batch1.md) |
| 军备竞赛与峰会 | 查军事比较/实际地图地区加成/双骰收据/胜者DEFCON选择 | 2牌参数Bound；军事比较封顶已确认，待实现 | [contest-events.md](contest-events.md) |
| 一小步太空事件 | 查落后门禁/两格/只发第二格VP/越终点停止 | 参数Bound；待实现 | [one-small-step.md](one-small-step.md) |
| 中期影响力第三批 | 查跨区四国/运河固定三国/傀儡零影响筛选/VOA非欧洲移除 | 4牌参数Bound；短缺解释已确认，待实现 | [mid-influence-batch3.md](mid-influence-batch3.md) |
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
| 持续规则设计批次 | 查当前进行批次、后续队列、逐批验证/Git收尾和未实施边界 | 设计队列；运行进度另见Demo | [rules-design-roadmap.json](../Design/rules-design-roadmap.json) |
| 战争事件第二批 | 查Brush稳定度与北约保护、超级大国邻接、两伊目标及共享结算 | 2牌参数绑定；五张战争参数齐备，C#待实现 | [war-events-batch2.md](war-events-batch2.md) |
| 战争事件第一批 | 查战争单骰修正、目标选择、影响力转移、军事/VP与戴维营阻止 | 3牌参数绑定；C#待实现 | [war-events.md](war-events.md) |
| 事件授权与欧洲额外行动 | 查拆墙完整步骤、美国OPS预算、可选政变/逐次调整、地区豁免与根行动责任 | 1牌参数绑定；C#待实现 | [event-operations.md](event-operations.md) |
| 北约局部例外与组合事件 | 查戴高乐/勃兰特的固定步骤、VP终局、取消阻止与局部保护恢复 | 2牌参数绑定；C#待实现 | [nato-exceptions.md](nato-exceptions.md) |
| 北约前置影响力事件 | 查马歇尔西欧筛选/确认短缺、华沙互斥分支、额度和成功记录 | 2牌参数绑定；C#待实现 | [alliance-prerequisites.md](alliance-prerequisites.md) |
| 初始布置与开局 | 查固定影响力、自由部署、私有初始手牌、轨道引用、幂等与首回合衔接 | 真实发牌/部署地图练习已实现；完整开局阶段衔接待接入 | [setup.md](setup.md) |
| 回合与出牌状态机 | 查找阶段转换、头条保密、事件/OPS 顺序、卡牌去向和读档继续位置 | v0.2 编排设计；待实现 | [turn-flow.md](turn-flow.md) |
| 风险与胜负服务 | 查找 DEFCON 限制与责任、军事净结算、胜负检查点及扣留计分牌矩阵 | DEFCON/军事及六类基础胜负检查点已实现；完整编排待接入 | [risk-victory.md](risk-victory.md) |
| 地区计分与最终汇总 | 查找六地区档位、东南亚权重、最终完整性和中国牌附加分 | 基础计分和最终汇总已实现；最终宿主待接 | [region-scoring.md](region-scoring.md) |
| 最终计分基础 | 查六区+中国牌一次汇总、欧洲优先、零分平局与严格加载 | 核心已实现；最终宿主/UI待接 | [final-scoring.md](final-scoring.md) |
| 通用 OPS 报价 | 查Actor归属、Flat合并/上下限、真实牌/视同来源与太空工厂 | General子集已实现；地区与活动实例待接 | [ops-general.md](ops-general.md) |
| 政变基础计算 | 目标门禁、共享骰子/稳定度系数、影响力与战场意图 | C#基础子集；完整命令待接 | [coup-calculation.md](coup-calculation.md) |
| 政变整体计划 | 军事/随机/影响力/DEFCON一体结果、收据防重与版本 | C#已验证；宿主待接 | [coup-attempts.md](coup-attempts.md) |
| 太空竞赛 | 查找8格门槛、先后奖励、领先能力、事件推进与回合衔接 | 轨道/领先能力/普通尝试计划已实现；宿主待接 | [space-race.md](space-race.md) |
| 地图与卡牌数据契约 | 查找正式数据字段、邻接、三类卡牌、效果注册、Schema及加载门禁 | 契约与正式印刷定义已建立；服务待实现 | [map-card-contracts.md](map-card-contracts.md) |
| 正式地图与卡牌数据 | 按地区查84国与邻接，按时代查103张基础牌及效果待实现边界 | 静态定义已录入；不是可运行规则包 | [map-card-data.md](map-card-data.md) |
| 可替换地图视图实现 | 查同步公开帧、过期输入、两适配器及局面保持 | 170回归通过；正式资源与画风未定 | [replaceable-map-view.md](replaceable-map-view.md) |
| 地图布局与未定画风 | 区分规则图、位置拾取和主题资源，了解2D/3D接入边界 | 设计稿；未选定美术 | [map-presentation.md](map-presentation.md) |
| 数据结构契约目录 | 按description定位五十三份JSON Schema（含事件参数、setup及卡牌v1/v2）与合成样例 | 结构契约；不是完整规则包 | [catalog.json](../Contracts/catalog.json) |
| 模块描述目录 | 先按 description 定位模块，再读完整契约与相关代码 | 模块目录；按各描述文件区分已实现子集与planned | [catalog.json](../Descriptors/catalog.json) |
| 共享规则数据目录 | 查找数据集描述、来源与独立参数文件 | 数据集目录；含规则、Demo、地图布局/陆地与工程技术配置 | [catalog.json](../../Data/catalog.json) |
| 行动规则 | 查找影响力、政变、调整、预算、对象契约与验收案例 | 影响力子集已实现；政变/调整待实现 | [operations.md](operations.md) |
| 规则资料 | 查找版本来源、FAQ 适用边界和资料冻结缺口 | 已登记；未完整冻结 | [rules-sources.md](rules-sources.md) |
| 牌区与初始发牌 | 查唯一实体位置、独立中国牌、确定性洗牌与共享引用 | 已接GameState/身份投影/手牌预览；完整出牌待实现 | [card-zones.md](card-zones.md) |
| 可恢复随机状态 | 查完整状态/Fork、无偏范围采样、精确JSON与失败不推进 | 初始发牌/太空尝试计划使用随机副本；完整存档待实现 | [deterministic-random.md](deterministic-random.md) |
| 工程边界 | 查找纯 C# 核心、命令、事件、玩家视角、确定性和程序集约束 | 实际分层与后续设计边界 | [AGENTS.md](../../AGENTS.md) |
| Git 工作流 | 理解本地提交、远程同步、忽略文件及每次工作的收尾方式 | 本地已关联；推送另行核验 | [git-workflow.md](git-workflow.md) |
| GitHub 保存 | 查找 coldwarheat 地址、线上提交方式和本地同步限制 | 已记录 | [GitHub 工作记录](../Worklogs/2026-09-17-github-bootstrap.md) |
| 初始化记录 | 查找本次建立的文件、实际验证和尚未完成事项 | 已记录 | [工作记录](../Worklogs/2026-09-16-repository-bootstrap.md) |


## 后续知识点的最小结构

每个实际需要的主题记录：description、实现状态、规则来源及版本、输入输出、关键不变量、代码路径与符号、直接依赖、流程、对应测试、待确认问题。

代码位置以稳定路径和符号名为主，行号只作辅助。没有代码的设计条目注明“待实现”，不得编造函数或测试。

计划逐步展开：领域模型、规则结算、对局阶段、卡牌效果、玩家选择、会话与隐藏信息、随机性、存档回放、表现层边界。此列表不代表已实现功能。

- [持续 OPS 事件试玩](playable-flat-ops-events.md)：遏制/清洗两牌的真实激活、后续统一报价、公开当前OPS；完整回合末未接。

- [对手事件与三种OPS](opponent-ops-modes.md)：先顺序后用途、投放/政变/调整、当前目标、复合结果与单根收尾；入口摘要见JSON。

- [地区OPS预算](regional-ops-budget.md)：冻结普通报价、全部目标历史、条件奖励/合并保底与原子费用，供不同来源复用。
- [中国牌试玩](china-card-playtest.md)：独立牌四用途、立即转交、计分义务、宿主与独立界面入口；回合末翻面已接，事件转交仍待接。

- [补牌与时代加入](turn-deal.md)：纯服务按共享手牌目标补牌、旧牌用尽再洗弃牌、时代混入与随机事务；已由连续回合试玩接入。

- [连续回合试玩](continuous-turn-playtest.md)：回合末审核后补牌/轨道准备并进入下一回合，仍跳头条；纯规则、Host、公开投影和按钮分层。

- [中英文短名称](localized-catalog-names.md)：独立中文数据/稳定ID加载/地图与手牌切换；项目译名不等于规则全文本地化。

- [头条运行时接线规划](../Architecture/headline-runtime-integration.md)：定位已实现的有限基础/事件根/会话与可选回合入口，以及尚待UI/原生验收的范围。

- [头条基础纯服务](headline-foundation.md)：保留牌区、秘密选择、太空先看后选投影与普通印刷值排序；尚未挂Host/Demo，托管验证与Unity验收分开。

- [有限头条事件结算](headline-resolution.md)：普通队列、已支持事件、选择恢复和终局截断已实现；Host/UI待接，勿误认为已启用Demo头条。

- [头条收集与本地会话](headline-session.md)：可选回合入口/权威牌区、Host/公开收据和选择继续已接，原生验收独立。

- [已选叛逃者头条运行时](defectors-headline.md)：已选头条取消/弃牌/公开结果已实现，普通VP/嵌套未接；原生已在头条试玩集成中通过。

- [策略游戏工程参考与取舍](../Architecture/game-engineering-reference-notes.md)：Factorio确定性、Unity命令模式及本项目隐藏信息边界；[头条试玩接入草案](../Architecture/headline-playtest-integration-draft.md)已由[实际接线](headline-playtest.md)接续。

- [头条试玩接线](headline-playtest.md)：第15场景/保密选择/地图UI与构建入口已接，原生通过；实际构建与鼠标状态见报告。
