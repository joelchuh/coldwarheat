# 单人双边测试：第一批核心实现计划

> For agentic workers: 使用 executing-plans 在本会话逐项实现，按 test-driven-development 先验证失败再实现；完成整批后使用 requesting-code-review 独立审阅。此处推荐由主代理实施，不为每个紧耦合小任务分派实现代理。任务勾选只表示实际完成。

**Goal:** 建立能运行真实 EditMode 测试的 Unity 工程，在可信测试场景中完成一次影响力操作，并提供正常玩家视图和单人双边测试的独立只读视图。

**Architecture:** 统一权威 GameState，Core 计算规则，Application 绑定身份及协调提交。一个本地测试宿主持有同一局的美、苏两个固定身份会话；测试观察视图可查看双方手牌，普通玩家视图仍过滤信息。

**Tech Stack:** 本机 Unity 6000.6.0f1、纯 C# Core、Unity Test Framework/EditMode。编辑器所在本机模板声明 Test Framework 1.8.0，实际包解析及编译兼容性由任务 1 验证。独立 dotnet SDK 当前未发现，不以 dotnet test 代替 Unity 测试。

**Spec:** [实现架构](../Design/implementation-architecture.proposal.md)、[普通行动规则](../Wiki/operations.md)、[地图契约](../Wiki/map-card-contracts.md)、[单人测试模式](../Wiki/local-test-mode.md)。

状态：方向和单人查看双方信息已获用户同意；本计划尚未执行。单窗口是本计划的实现建议，不把用户询问双客户端理解为要求联网。

## Global Constraints

- Core 不引用 UnityEngine、UI、文件系统、网络或数据库；定义与动态状态分离。
- 静态地图使用完整 84 国、2 超级大国定义；画面布局与主题不参与规则邻接。
- 普通影响力采用行动轮开始可达性快照；每一点根据当前工作状态重新计算费用。
- 一条完整分配命令全部成功或全部拒绝；之前已经成功的另一条命令不回滚。
- 玩家不能自报 OPS 预算、骰点或结果；切换操作方不改变规则规定的 Actor。
- 先识别同身份、同 CommandId 的重试，再检查新命令版本；相同 ID 不同内容拒绝。
- 首批是可信测试场景；完整 RulePackLoader 不能因处理器未实现而放宽门禁。
- 单人测试是显式本地能力，不把正常 PlayerViewSnapshot 改成全知视图。
- 路径统一为 Assets/ColdWarStruggle；历史 Assets/Game 拟定路径在实际接入时修正。
- 暂不实现两套客户端、网络、AI、完整卡牌、完整开局或最终地图美术。

## Review Focus

- 输入列表提交后被调用者修改：命令和快照采用防御性复制，结果不能漂移（任务 2、3）。
- 第二个投放目标不合法：先前在本命令中的模拟变更也不能进入权威状态（任务 2）。
- 合法高亮查询发生在另一个身份或旧版本：输出携带身份上下文和版本，不能作为授权（任务 3）。
- 两个会话同时从同一版本提交：最多一条成功，另一条过期；串行调度提交（任务 3）。
- 调试宿主切换操作方：不得改写行动归属或让普通会话取得调试快照（任务 3）。

## 任务 1：工程、只读定义与可运行的测试入口

**Files:** 创建 Packages/manifest.json、由编辑器实际生成的 ProjectSettings、Core/Application/Infrastructure 和 Tests/EditMode 的 asmdef；创建 Core/Domain/PlayerSide.cs、CountryId.cs、MapDefinition.cs，Core/Rules/ControlRules.cs，Infrastructure/DataLoading/MapDefinitionLoader.cs，以及 Tests/EditMode/DefinitionTests.cs。路径前缀均为 Assets/ColdWarStruggle/Scripts 或 Assets/ColdWarStruggle/Tests。保留生成的 .meta；Library、Temp、Logs 和 TestResults 不入库。

**Interfaces:** `PlayerSide { US, USSR }`；`CountryId` 是不可变字符串值对象。`CountryDefinition(CountryId id, int stability, string[] regions, bool battleground)`；`MapDefinition.GetCountry(CountryId)`、`GetCountryNeighbors(CountryId)`、`GetSuperpowerNeighbors(PlayerSide)` 返回不可变定义或只读集合。`ControlRules.GetController(int us, int ussr, int stability)` 返回 `PlayerSide?`。

- [ ] 核对干净分支和项目目录。使用新的工作分支；不覆盖已有文件。不启动用户可见窗口来做后台检查。
- [ ] 用已安装编辑器创建并验证最小项目，保留项目生成信息。仅增加本阶段实际需要的测试/JSON包；记录解析后的准确版本及 Packages/packages-lock.json。模板中的协作、AI导航等插件不因出现在模板而全部引入。URP 为后续表现准备，在需要呈现时接入，不把初始默认渲染器视为最终美术决定。
- [ ] 加入测试程序集与最小 API 壳，先运行下列控制判定测试，确认失败来自未实现逻辑，而非包、编译或授权错误。

```csharp
[TestCase(2, 0, 2, PlayerSide.US)]
[TestCase(0, 2, 2, PlayerSide.USSR)]
public void Control_requires_margin(int us, int ussr, int stability, PlayerSide expected)
{
    Assert.That(ControlRules.GetController(us, ussr, stability), Is.EqualTo(expected));
}
[Test] public void Equal_influence_is_uncontrolled()
{
    Assert.That(ControlRules.GetController(2, 2, 2), Is.Null);
}
```

- [ ] 实现并检验正稳定度、非负影响力和合法枚举值，控制判定以差值完成；使用能避免整数溢出的计算。

```text
Reject invalid arguments.
if (long(us) - ussr >= stability) return US
if (long(ussr) - us >= stability) return USSR
return null
```

- [ ] 为地图加载器先写失败测试：未知版本、重复字段/ID、缺失字段、坏引用、重复边、自环及正式数据覆盖不足均拒绝；合法数据得到 84 国和 2 超级大国，邻接双向且稳定排序。Infrastructure 按现有 map.schema.json 及语义约束解析；Core 不解析 JSON。该加载器只加载地图定义，不返回完整可运行规则包。
- [ ] 实现加载和不可变对象，运行 DefinitionTests；修改返回集合的尝试不能改变地图。记录实际测试数量与 XML，不凭退出码单独宣称通过。

测试命令采用编辑器运行入口：

```powershell
& 'E:/unity6.6/6000.6.0f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'E:/TwilightStruggle' -runTests -testPlatform EditMode -testFilter 'ColdWarStruggle.Tests.DefinitionTests' -testResults 'E:/TwilightStruggle/TestResults/definitions.xml' -logFile 'E:/TwilightStruggle/TestResults/definitions.log'
```

实际工作目录若为隔离检出，projectPath、结果和日志一并替换为该绝对路径。测试运行由 Test Runner 退出，不附加提前退出参数。首次包解析失败、授权失败或零测试视为环境问题，不能记录为规则测试通过。每次使用新的结果文件或验证报告生成时间，避免读到上次结果。

## 任务 2：影响力规则及事务边界

**Files:** 创建 Core/State/GameState.cs、CountryState.cs；Core/Rules/OperationsContext.cs、InfluenceRules.cs；Core/Commands/PlaceInfluenceRequest.cs；Core/Rules/PlacementPlan.cs；Core/Events/InfluencePlaced.cs；Tests/EditMode/InfluenceRulesTests.cs 与 Tests/EditMode/Fixtures/InfluenceFixture.cs。

**Interfaces:** `InfluenceFixture.EnemyControlledX(int budget)` 创建 X 稳定度 2、美 0/苏 2、美国可达 X 的测试状态；`InfluenceFixture.Chain(int budget)` 创建 X—Y—Z、仅 X 有美国影响力且无超级大国捷径的场景。测试夹具提供 `State`、`Context`；`InfluenceRules.PlanPlacement(GameState state, OperationsContext context, IReadOnlyList<CountryId> orderedTargets)` 返回 `PlacementPlan`。计划含 `Accepted`、稳定错误码、`Costs`、隔离 `NextState` 与事件；拒绝结果不能带部分变更。

- [ ] 写测试及最小可编译 API 壳，观察下列行为失败。场景值是测试输入，正式业务数据不复制到 C# 常量中。

```csharp
[Test] public void Costs_are_recalculated_after_each_point()
{
    var f = InfluenceFixture.EnemyControlledX(4);
    var p = InfluenceRules.PlanPlacement(f.State, f.Context,
        new[] { new CountryId("X"), new CountryId("X"), new CountryId("X") });
    Assert.That(p.Accepted, Is.True);
    Assert.That(p.Costs, Is.EqualTo(new[] { 2, 1, 1 }));
    Assert.That(f.State.GetInfluence(new CountryId("X"), PlayerSide.US), Is.Zero);
    Assert.That(p.NextState.GetInfluence(new CountryId("X"), PlayerSide.US), Is.EqualTo(3));
}
```

- [ ] 增加并先验证失败：Chain 场景依次向 Y/Z 投放整批拒绝；合法前缀加未知国家整批拒绝；预算不足、空列表、错误 ContextId、错误模式、过期上下文、负影响力、溢出都拒绝。同一目标重复出现合法；已提交命令之后的新坏命令不回滚旧命令。
- [ ] 按确定顺序实现：验证上下文 → 复制工作状态和请求列表 → 逐点依据固定可达集合查目标 → 依据当前工作状态计算费用 → 预算检查及工作状态变更 → 检查不变量 → 返回计划。任一步失败丢弃本计划。没有骰子路径，本批不声称验证随机服务。
- [ ] 通过所有 InfluenceRulesTests，并确认查询/规划没有改变输入对象。GameState 内部变更能力仅供权威管线使用；不向 Presentation 暴露可写引用。

运行任务 1 同一条 Unity 命令，testFilter 改为 `ColdWarStruggle.Tests.InfluenceRulesTests`，结果/日志用 `influence.xml` 与 `influence.log`；预期从有针对性的规则失败变为全部通过。

## 任务 3：单局双身份会话与测试观察视图

**Files:** 创建 Application/Sessions/LocalGameHost.cs、PlayerSession.cs、IGameSession.cs；Application/CommandHandling/CommandEnvelope.cs、PlayerCommandResult.cs、CommandReceiptStore.cs；Application/Queries/PlayerViewSnapshot.cs、LocalTestSnapshot.cs；Tests/EditMode/SessionTests.cs、ProjectionTests.cs、Fixtures/SessionFixture.cs；Application/Queries/LegalTargetsResult.cs。调试入口由可信宿主创建，不作为网络消息或普通玩家参数。

**Interfaces:** `LocalGameHost` 维护一局和串行提交队列；测试夹具创建 host，绑定好的 `UsSession`、`UssrSession` 为 `IGameSession`。`SubmitAsync(CommandEnvelope command, CancellationToken token)` 返回 `PlayerCommandResult`；`ReadView()` 返回 `PlayerViewSnapshot`；`QueryInfluenceTargets(contextId, expectedVersion)` 返回 `LegalTargetsResult`。信封含 CommandId、GameId、ExpectedStateVersion 和 PlaceInfluenceRequest（ContextId、OrderedTargets），没有客户端 Actor 或预算字段。

只读玩家视图包含 StateVersion、公开国家状态、本人手牌和对手手牌数量。`LocalGameHost.ReadLocalTestSnapshot()` 仅在显式 LocalTest 模式可用，返回同版本双方只读手牌及公开状态；不开启时明确拒绝。它不返回可修改 GameState、RNG 或牌库内部对象。测试宿主的 `SelectedSide` 只是选用哪个已绑定会话，不修改当前 OperationsContext.Actor。

- [ ] 先写 SessionTests 验证成功、错误身份、过期新命令及幂等。示例中的 `SessionFixture.Create()` 创建同一局的两个固定身份会话；`MakeUsPlacement(commandId)` 构造使用当前 ContextId 和版本、目标 X 的信封；`SendUsAsync` 调用 UsSession，`ReadPublicVersion` 读取版本。

```csharp
[Test] public async Task Retrying_the_same_command_does_not_apply_twice()
{
    var f = SessionFixture.Create();
    var request = f.MakeUsPlacement("retry-1");
    var first = await f.SendUsAsync(request);
    var version = f.ReadPublicVersion();
    var retry = await f.SendUsAsync(request);
    Assert.That(first.Accepted, Is.True);
    Assert.That(retry, Is.EqualTo(first));
    Assert.That(f.ReadPublicVersion(), Is.EqualTo(version));
}
```

- [ ] 增加失败测试：相同 ID 改目标/版本/context 拒绝；不同身份去重隔离；取消已经完成的等待不撤销结果；两个相同版本的新请求只有一个提交；调用者随后修改原目标数组不影响命令身份和结果。错误文本不含私有数据。
- [ ] 实现提交顺序：信封结构/身份验证 → 收据匹配 → 新命令版本、阶段和 Actor 验证 → Core 生成计划 → 一次提交状态、事件序列、收据 → 过滤返回。拒绝不推进状态和事件序列。查询复用验证器且只读；查错版本明确返回 stale，不能用旧候选授权新操作。
- [ ] ProjectionTests 检验 US 会话只见 US 手牌，USSR 会话只见 USSR 手牌；LocalTest 观察快照同版本包含双方手牌；关闭测试模式后无法取得该快照。切换操作侧不让非 Actor 提交成功，界面能读取双方信息也不等于能绕过行动顺序。
- [ ] 暂存场景中的手牌只用于信息投影测试，不冒充已实现发牌、出牌或完整开局服务。把上述宿主和查询接入将来的单窗口调试界面，本批不宣称已有可点击地图或完整手牌 UI。
- [ ] 运行全部 `ColdWarStruggle.Tests` EditMode 测试，核对测试清单与 XML。保存编译、测试数量、失败数及实际限制。

## 任务 4：描述、审阅与交付

- [ ] 新增实际核心模块 JSON 描述和 Wiki 知识点；列出真实路径/符号、入口、数据依赖和测试入口。首次实现不要把其他 102 个计划中卡牌处理器改成 implemented。
- [ ] 更新 README、工程状态、相关 planned 路径和工作记录。命令首次使用只记入说明；按既有复用门槛决定是否成为正式脚本，不为这次执行新增通用自动 Git 工具。
- [ ] 自查程序集没有逆向或 Unity 核心依赖；未知规则效果在此受限测试场景之外明确不可用。
- [ ] 独立审阅整批变更，重要问题先补失败测试再修复；跑受影响测试及整批套件。只报告实际执行结果。
- [ ] 审阅 Git 差异、提交并按既有授权同步；核对本地、远端 SHA 与工作区。若环境阻塞，交付保存的文件、实际日志和明确未通过项目，不冒称原型可运行。

本批验收完成后，再计划 Fidel、Decolonization 和占位地图。正式 RulePack、完整本地游戏、存档重放及地图接口的运行实现不算本批已完成项目。

## 技术资料与核验范围

- [Unity 编辑器命令行参考](https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html)：用于核对批处理及项目路径等基础参数；这是 6.0 文档，实际 6000.6 的测试兼容性仍需本机运行确认。
- Test Framework 1.8.0 来自本机 6000.6.0f1 编辑器附带模板清单的只读检查；没有据此宣称测试包已安装、授权已验证或测试已执行。
- 本计划的测试示例是计划内容，所有复选框保持未完成。独立 SDK 列表为空，不自动安装新 SDK。
