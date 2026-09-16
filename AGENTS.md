# ColdWarStruggle — Codex 项目开发规范

> 文档版本：0.1  
> 项目代号：ColdWarStruggle  
> 文档日期：2026-09-16  
> 引擎：Unity 6.x，沿用实际项目安装版本  
> 渲染管线：URP  
> 主要语言：C#  
> 当前阶段：项目初始化与规则核心设计

本文放在 Unity 项目根目录，与 Assets、Packages、ProjectSettings 同级，作为 Codex 的项目级开发指令。本文描述的是开发计划，不代表对应代码、脚本和参考资料已经存在。

## 1. 项目目标

ColdWarStruggle 是一款双人回合制冷战战略游戏。当前目标是在个人学习原型中按已确认版本的《冷战热斗》规则实现游戏核心，再逐步增加地图交互、镜头、动画、音效和事件演出。

核心规则尽可能忠实于确认的原版规则；表现层参考《文明》系列的交互体验，使用自制或获授权资源。

总体开发顺序：

1. 建立纯 C# 规则核心与联网接口边界。
2. 完成规则实现与自动测试。
3. 实现完整本地双人对局，以及存档和回放。
4. 开发 Unity 地图、UI、动画与音效。
5. 接入服务器权威联网。
6. 开发 AI。
7. 完善教程、设置和发布流程。

核心原则：从第一天按照联网游戏设计，但前期不实现真正的网络通信。

## 2. 规则资料与素材边界

开发阶段使用自行整理的规则摘要、自制占位资源和合法取得的参考资料。不要将原作规则书全文、卡牌描述全文、卡牌美术、地图素材或 Logo 直接纳入可发布资源。

使用资料时记录来源、版本和对应规则编号。商业发布涉及名称、文本和美术授权时，另行确认发布方案；不因这一事项阻塞当前可独立推进的工程骨架和规则测试。

## 3. 强制架构原则

### 3.1 GameCore 必须独立于 Unity

规则核心必须是纯 C#。ColdWarStruggle.Core 禁止引用：

- UnityEngine、MonoBehaviour、ScriptableObject。
- GameObject、Transform、Unity UI。
- 网络 SDK 和平台 SDK。
- 场景对象、动画、音效和资源加载接口。

规则必须能够脱离场景，通过 EditMode 测试运行。核心程序集在 asmdef 中设置 noEngineReferences，并以实际 Unity 版本支持的 C# 和 API 为准。

### 3.2 玩家不能直接修改 GameState

禁止在 UI、地图脚本或网络代码中直接执行：

```csharp
country.UsInfluence += 1;
state.Defcon -= 1;
```

所有玩家操作必须转换为 Command，由 RuleEngine 验证和执行。执行成功后产生领域事件与新状态快照；表现层根据结果更新画面。

命令示例：

```text
AddInfluenceCommand
CoupCommand
RealignmentCommand
PlayCardCommand
ResolveDecisionCommand
EndActionRoundCommand
```

事件示例：

```text
InfluenceChangedEvent
CardPlayedEvent
CoupResolvedEvent
DefconChangedEvent
VictoryPointsChangedEvent
DecisionRequestedEvent
```

### 3.3 表现层不能裁决规则

Unity 表现层负责显示玩家可见快照、收集输入、生成 Command、根据 Event 播放动画，以及展示错误和合法目标。

规则状态不能依赖动画完成、协程结束或弹窗关闭才能正确保存。动画可以跳过，不能改变规则结算结果。

### 3.4 为联网保留服务器权威架构

未来服务器是唯一权威状态持有者。客户端提交意图，例如国家和所使用的卡牌；骰子结果、OPS 预算、影响力变化和 DEFCON 变化由权威核心计算。

玩家身份由会话绑定验证，不能仅相信 Command 中自报的 PlayerId。

### 3.5 隐藏信息必须有边界

完整 GameState 只用于权威规则执行和可信的内部测试。客户端、UI 和正常 AI 获得经过玩家视角过滤的 PlayerViewSnapshot。

对手手牌、牌库顺序、秘密选择和尚未揭示的头条牌不得通过快照、事件、日志或合法行动查询泄露。

### 3.6 AI 与真人使用相同接口

AI 读取允许公开的状态、查询合法行动并生成 Command。AI 不得直接修改 GameState，也不得读取对手秘密信息来提高正常对局难度。

## 4. 目标目录结构

```text
ColdWarStruggle/
├── AGENTS.md
├── README.md
├── Assets/
│   └── ColdWarStruggle/
│       ├── Scripts/
│       │   ├── Core/
│       │   │   ├── Domain/
│       │   │   ├── State/
│       │   │   ├── Commands/
│       │   │   ├── Events/
│       │   │   ├── Rules/
│       │   │   ├── Cards/
│       │   │   ├── Decisions/
│       │   │   ├── Random/
│       │   │   ├── Serialization/
│       │   │   └── Validation/
│       │   ├── Application/
│       │   │   ├── Sessions/
│       │   │   ├── CommandHandling/
│       │   │   ├── Queries/
│       │   │   └── Replay/
│       │   ├── Presentation/
│       │   │   ├── Map/
│       │   │   ├── Camera/
│       │   │   ├── UI/
│       │   │   ├── Animation/
│       │   │   └── Audio/
│       │   ├── Infrastructure/
│       │   │   ├── Persistence/
│       │   │   ├── Logging/
│       │   │   └── DataLoading/
│       │   ├── Networking/
│       │   └── AI/
│       ├── Data/
│       │   ├── Countries/
│       │   ├── Cards/
│       │   ├── Regions/
│       │   └── Localization/
│       ├── Scenes/
│       ├── Prefabs/
│       ├── Materials/
│       ├── Models/
│       ├── Textures/
│       ├── Audio/
│       └── Tests/
│           ├── EditMode/
│           └── PlayMode/
├── Docs/
│   ├── Architecture/
│   ├── Decisions/
│   ├── References/
│   ├── Rules/
│   ├── Testing/
│   └── Roadmap/
├── Tools/
│   └── Scripts/
└── TestResults/
```

Assets/ColdWarStruggle/Scripts 放游戏 C# 代码；Tools/Scripts 放工程自动化脚本；Docs/References 放规则来源和参考记录。

目录按功能逐步填充，不为未来功能提前编写大量空类。

## 5. Assembly Definition 规划

计划使用以下程序集：

```text
ColdWarStruggle.Core
ColdWarStruggle.Application
ColdWarStruggle.Infrastructure
ColdWarStruggle.Presentation
ColdWarStruggle.Networking
ColdWarStruggle.AI
ColdWarStruggle.Tests.EditMode
ColdWarStruggle.Tests.PlayMode
```

依赖规则：

| 程序集 | 允许的项目依赖 |
|---|---|
| Core | 无 |
| Application | Core |
| Infrastructure | Core、Application |
| Presentation | Core、Application |
| Networking | Core、Application |
| AI | Core、Application |
| Tests | 按测试目标引用对应程序集 |

禁止循环依赖。Networking 和 AI 尚未实现时保留目录与接口规划即可。依赖注入和实现组装由入口或 Bootstrap 完成。

## 6. GameCore 核心模型

### 6.1 状态

按需求逐步建立：

```text
GameState
PlayerState
CountryState
CardState
DeckState
TurnState
DefconState
SpaceRaceState
ScoringState
ActiveEffectState
PendingDecision
GameResultState
```

GameState 最终应表达：

- 游戏 ID、规则版本和状态版本号。
- 当前回合、行动轮、阶段、行动玩家和事件响应玩家。
- DEFCON、VP、军事行动值和太空竞赛。
- 国家影响力、手牌、牌库、弃牌堆和移出游戏的卡牌。
- 持续效果、尚未完成的玩家选择和胜负状态。

明确区分“当前行动玩家”“事件受益玩家”和“负责选择的玩家”，不能简单使用一个 CurrentPlayer 替代全部角色。

### 6.2 稳定 ID

国家、卡牌、地区和效果必须使用稳定 ID：

```text
country.france
country.iran
region.europe
card.marshall_plan
effect.quagmire
```

业务逻辑不得依赖显示名称。显示名称由本地化数据提供。

### 6.3 命令信封

命令信封建议包含：

```text
CommandId
GameId
ExpectedStateVersion
PlayerId
CommandType
Payload
```

提交时间可以用于外围日志，不参与规则判定。命令 ID 和玩家身份在会话层绑定。规则引擎根据当前状态验证卡牌、预算、阶段和目标。

支持重复命令返回原结果；不得重复消耗 OPS、推进状态版本或生成新的随机结果。

### 6.4 执行结果

使用统一 CommandResult 表达接受或拒绝、稳定错误码、领域事件和状态版本。玩家快照在会话层按身份投影。

错误码示例：

```text
NOT_CURRENT_PLAYER
COUNTRY_NOT_FOUND
INSUFFICIENT_OPS
COUP_BLOCKED_BY_DEFCON
PENDING_DECISION_REQUIRED
STATE_VERSION_MISMATCH
```

非法命令不得部分修改状态、消耗随机数或改变牌堆。

## 7. 多步骤选择机制

卡牌事件可能要求选择国家、卡牌或执行顺序，不能假设一个 Command 总能立即完成所有操作。

使用 PendingDecision 表示游戏正在等待选择：

1. PlayCardCommand 启动事件。
2. 引擎产生 DecisionRequestedEvent。
3. PendingDecision 保存到 GameState。
4. 玩家提交 ResolveDecisionCommand。
5. 引擎验证选择，并继续或完成事件。

PendingDecision 根据需要包括：

```text
DecisionId
DecisionType
OwningPlayer
MinimumSelections
MaximumSelections
LegalTargets
RemainingBudget
SourceCardId
ContinuationState
```

存在待处理选择时，只允许该状态支持的命令。中途状态必须可以存档和恢复，不能只保存在 Unity 协程或弹窗中。

## 8. 随机数与确定性

GameCore 禁止直接调用 UnityEngine.Random、System.Random.Shared、DateTime.Now 或 Guid.NewGuid。

通过接口注入随机数：

```csharp
public interface IRandomProvider
{
    int NextInt(int minInclusive, int maxExclusive);
}
```

实现规划：

```text
SeededRandomProvider
FixedRandomProvider
ServerRandomProvider
```

相同初始状态、规则版本、命令序列和随机状态必须产生相同结果。随机算法和状态应可持久化；恢复存档不能从种子重新开始随机序列。

记录骰子和洗牌等随机结果。状态 Hash 使用稳定排序和稳定编码，不使用对象默认 GetHashCode。

## 9. 卡牌系统

采用“数据定义 + 效果代码”的混合结构。

静态数据：

```text
Id
InternalName
Ops
Side
Era
CardType
RemoveAfterEvent
Tags
EffectId
```

复杂效果通过统一接口实现，并使用显式 CardEffectRegistry 注册。不得依据卡牌显示名称编写 if 分支；避免依赖运行时反射发现效果，以兼顾 IL2CPP。

规则文本、显示文本和规则逻辑分离。持续效果具有稳定 ID、作用范围、到期条件和明确的结算顺序。

## 10. 状态、事件日志与回放

当前采用“权威状态快照 + Command 日志 + Domain Event 日志”，暂不要求完整 Event Sourcing。

用途：Bug 复现、对局回放、断线重连、状态校验和自动测试。

事件建议包含：

```text
EventId
SequenceNumber
GameId
CommandId
StateVersion
EventType
Payload
RulesVersion
```

权威日志和玩家可见日志分离。普通客户端回放不提前展示当时尚未公开的信息。

## 11. 本地与未来网络会话

定义统一接口：

```csharp
public interface IGameSession
{
    Task<CommandResult> SubmitAsync(
        IGameCommand command,
        CancellationToken cancellationToken);
}
```

第一阶段实现 LocalGameSession。未来增加 NetworkGameSession 和 ReplayGameSession。

Unity UI 通过 IGameSession 提交命令，并接收玩家视角快照和事件；不直接持有可修改的权威 GameState。

网络协议、鉴权和传输实现留在外围层，不能进入 Core。

## 12. 保存和兼容性

保存数据包含 SchemaVersion、RulesVersion 和 GameVersion。开发阶段优先使用便于检查的 JSON。

保存内容覆盖待处理选择、持续效果、随机状态、命令去重记录和事件序号。

测试序列化前后等价、非法数据拒绝和支持范围内的版本升级。正式存档不直接序列化 MonoBehaviour 或场景对象。

## 13. 规则测试要求

按“整理规则解释 → 测试案例 → 实现 → 验证 → 重构”推进。

优先测试：

- 国家控制权、影响力成本和邻接关系。
- OPS 消耗、政变计算、战场国与 DEFCON。
- DEFCON 地区限制、军事行动值和政局调整。
- 地区计分、头条阶段和对手事件顺序。
- 计分牌、中国牌、太空竞赛和持续效果。
- 回合结束、胜负判断和多步骤玩家选择。
- 重复命令、过期命令和非法命令不改变状态。
- 存档恢复、确定性回放和隐藏信息投影。

不要根据讨论示例臆断正式规则。例如，计分牌留在手中的最终处理、DEFCON 导致失败的责任玩家，都必须按确认版本的规则与裁定实现。

测试命名示例：

```csharp
Coup_OnBattlegroundCountry_ShouldLowerDefcon()
Coup_WhenRegionBlockedByDefcon_ShouldBeRejected()
Control_ShouldRequireLeadAtLeastEqualToStability()
Command_WithStaleStateVersion_ShouldBeRejected()
DuplicateCommand_ShouldNotExecuteTwice()
Replay_WithSameSeed_ShouldProduceSameStateHash()
PlayerView_ShouldNotRevealOpponentHand()
```

修复规则 Bug 时，添加能复现问题的测试。重要规则测试关联资料来源和规则编号。

## 14. Tools/Scripts 规划

Windows 开发优先使用 PowerShell。脚本尚未创建，按阶段实现。

| 脚本 | 用途 | 实现时机 |
|---|---|---|
| bootstrap-project.ps1 | 创建缺失目录和基础配置 | 必要时 |
| run-editmode-tests.ps1 | 命令行运行规则测试 | 工程骨架 |
| run-playmode-tests.ps1 | 运行场景测试 | 表现层 |
| run-all-tests.ps1 | 汇总已实现的测试和报告 | 工程骨架 |
| validate-game-data.ps1 | 校验国家、卡牌和地区数据 | 数据格式建立后 |
| validate-architecture.ps1 | 检查程序集依赖 | 架构稳定后 |
| validate-references.ps1 | 检查规则来源记录 | 规则条目增多后 |
| export-replay.ps1 | 导出复现日志 | 回放阶段 |
| verify-replay.ps1 | 重放并验证最终 Hash | 回放阶段 |
| build-windows.ps1 | 生成 Windows 测试版本 | 可运行游戏后 |

脚本要求：非交互运行；失败返回非零退出码；错误清晰；不硬编码开发者个人路径；通过参数或配置取得 Editor 路径；不删除用户文件；不自动提交 Git。

测试脚本实际调用 Unity Test Framework；读取测试报告判断结果，不能只根据进程退出就宣称测试通过。没有安装 Unity 时明确报告无法执行。

## 15. Docs/References 规划

| 文件 | 内容 |
|---|---|
| source-index.md | 规则来源、版本和获取日期 |
| rules-summary.md | 自行整理的规则摘要 |
| faq-errata-notes.md | FAQ、勘误和裁定摘要 |
| rule-interpretations.md | 歧义规则的项目决定 |
| card-rulings.csv | 卡牌效果、顺序和测试编号 |
| country-data.csv | 国家、地区、稳定度和邻接关系 |
| glossary.md | OPS、DEFCON、VP 等术语 |
| test-traceability.md | 规则与自动测试对应关系 |
| legal-ip-notes.md | 资源来源和发布事项 |
| open-questions.md | 未确认的规则问题 |

争议解释记录：规则编号、问题、采用解释、来源、影响代码、对应测试和决定日期。

Codex 不得凭记忆补全不确定规则。资料冲突时记录 open-questions，并继续不受影响的任务；只有该项实现确实需要用户决定时才提问。

## 16. 架构决策记录

重要决定放入 Docs/Decisions，格式为背景、决定、理由、替代方案、影响、状态和日期。

规划文件：

```text
ADR-001-pure-csharp-game-core.md
ADR-002-command-event-boundary.md
ADR-003-deterministic-randomness.md
ADR-004-state-snapshot-and-event-log.md
ADR-005-card-effect-registration.md
ADR-006-player-view-and-hidden-information.md
```

不得在没有说明的情况下推翻已接受的 ADR。

## 17. Unity 表现层目标

规则核心稳定后制作第一版表现层：欧洲局部地图、10～15 个国家、镜头平移缩放、Hover、点击、国家信息面板、双方影响力、合法目标高亮、命令提交和事件驱动的数值动画。

后期增加世界地图、控制权视觉变化、骰子与政变演出、DEFCON 仪表、新闻事件、卡牌动画、音效、教程和设置。

先使用简化地图与占位资源验证交互，再提高美术质量。动画可取消或跳过，不参与规则判定。

## 18. 开发里程碑

### Milestone 0：工程骨架

完成目录、基础 asmdef、无 UnityEngine 依赖的 Core、可运行 EditMode 测试、README、基础 ADR 和测试脚本。

### Milestone 1：最小规则闭环

完成创建游戏、玩家身份、合法行动查询、增加影响力命令、OPS 验证、权威状态变化、事件输出和玩家快照。

### Milestone 2：基础对局规则

完成回合与行动轮、卡牌与 OPS、影响力与控制权、政变、政局调整、DEFCON、军事行动、VP、计分和基础胜负。

### Milestone 3：完整本地规则

完成确认版本的地图与卡牌、特殊状态、持续效果和边界测试。本地双人可以完整打一局；具备存档、读档和回放能力。

### Milestone 4：Unity 正式表现层

完成地图、UI、动画、音效、事件演出、设置和基础教程。

### Milestone 5：联网

完成房间、身份验证、服务器权威、命令协议、隐藏信息过滤、状态同步、重连、超时、投降、版本检查和日志。

### Milestone 6：AI

依次实现随机合法行动 AI、启发式 AI、局面评估与搜索、难度分级。正常 AI 只使用其玩家视角信息。

## 19. Codex 工作规范

每次开始工作：阅读本文件，检查目录和 Git 状态，读取任务相关文档，确认当前里程碑，说明实施范围，完成修改并运行相关验证。

遵守以下约束：

- 不覆盖用户已有修改，不执行破坏性 Git 命令。
- 未获授权不自动提交或推送；用户已授权的后续操作按授权执行。
- 不把规则堆积在 MonoBehaviour 中。
- 不让 UI、网络和 AI 直接修改权威 GameState。
- 不提前实现无需求的大系统，不以未来重构为理由破坏依赖边界。
- 不在规则未确认时自行发明规则。
- 公共接口变化时同步更新文档和必要测试。
- 汇报实际修改、验证结果、无法运行的验证和下一项具体任务。

## 20. 第一次 Codex 任务

第一次进入项目只完成 Milestone 0：

1. 检查 Unity 版本、现有文件和 Git 状态。
2. 保留已有有效配置，建立需要的目录。
3. 创建 Core、Application 和 EditMode 测试 asmdef。
4. 创建最小类型：PlayerSide、GameId、CountryId、GameState、IGameCommand、GameEvent、CommandResult、IRandomProvider。
5. 创建最小 LocalGameSession 接口与骨架。
6. 添加具有实际意义的 EditMode 测试，例如固定随机源范围和 Core 基础不变量。
7. 创建 README 与 ADR-001 至 ADR-003。
8. 实现测试脚本并运行可用验证。
9. 汇报修改文件、依赖关系、测试结果和下一步。

暂不实现地图、完整卡牌、真实联网、AI、Spring Boot 或大型美术资源。用户后续明确要求进入下一阶段时，以最新任务为准。

## 21. 可直接交给 Codex 的启动指令

```text
请阅读项目根目录 AGENTS.md，并按其中的架构和工作规范推进 ColdWarStruggle。

现在完成 Milestone 0：工程骨架。

修改前检查现有 Unity 项目结构、Editor 版本和 Git 状态，不覆盖已有修改。建立纯 C# GameCore、程序集依赖、最小 LocalGameSession、EditMode 测试、基础文档和测试脚本。

完成后运行可以运行的验证，并汇报：
1. 创建和修改的文件；
2. 程序集依赖关系；
3. 实际测试结果；
4. Unity 版本或包依赖问题；
5. 下一项最小开发任务。

如果当前目录不是 Unity 项目，先创建可独立准备的文档和源码规划，说明缺失的项目条件，不伪造 Unity 配置或测试通过结果。

本次先不开始地图、完整卡牌、真实联网或 AI。
```

## 22. 已确认的当前工作阶段与知识库约定（2026-09-16）

本节记录用户后续决定；与上文“第一次任务”示例冲突时，以本节为准。

- 项目根目录：E:\TwilightStruggle；本地目录名 TwilightStruggle，GitHub 仓库 joelchuh/coldwarheat，内部代号 ColdWarStruggle。
- 已发现本机 Unity 6000.6.0f1。尚未创建 Unity 工程，不假定 URP 和测试包已经配置。
- 用户选择先细化全部规则设计，当前不执行第 20/21 节的 M0 源码初始化示例。
- 规则基准为官方 Deluxe 基础规则，首版不启用可选规则、扩展或自定义平衡；精确资料版本和卡牌范围仍需在设计中锁定。
- 用户已授权创建本地和 GitHub 仓库，以及本项目后续工作代码和文档的常规提交与推送。工程脚本仍不自动提交；由工作流程审阅后明确执行 Git 操作。
- 开工先查 Docs/Wiki/index.md，通过摘要定位知识点，再定位相关函数及必要上下文。使用项目技能 .agents/skills/project-knowledge/SKILL.md。
- 每次完成工作同步更新相关 Wiki 和 Docs/Worklogs；标明实现与计划，报告实际验证。
- 用户于 2026-09-17 确认已创建 GitHub 仓库 joelchuh/coldwarheat，实际可见性为 Public；沿用用户创建时的设置，不擅自更改可见性。
- 本地 Git 与 GitHub 的同步状态分别核验；通过插件创建线上提交不代表本地完成了 push。
