# 可替换地图视图实施计划

> For agentic workers: 使用 executing-plans 在当前会话实施，test-driven-development验证失败与修复，requesting-code-review进行独立完成稿审查。用户已认可地图可替换方向并授权持续优化；保持首个Demo和单人双边测试范围。

**Goal:** 从DemoView抽出公开地图契约与输入协调器，用国家卡片和紧凑列表两个真实适配器证明同一局面无需改规则即可更换显示。

**Architecture:** IMapView只拿MapViewContext/MapFrame并发出MapIntent。MapPresenter从DemoController的玩家投影与已计算合法目标构建公开帧，并将通过上下文校验的选择转交控制器；既有命令/固定身份会话不变。DemoView保留手牌、操作按钮和状态面板，只把中间国家区交给适配器。

**Tech Stack:** Unity6000.6.0f1、C#、现有IMGUI；不引入包、不新增规则JSON常量。

**Spec:** Docs/Design/implementation-architecture.proposal.md §5、Docs/Wiki/map-presentation.md；本计划是其中同步占位视图子集，最终美术仍未选定。

## Global Constraints

- 一个权威对局不因换视图/浏览地区/选中国家而重建或消耗OPS。两种外观都标为占位测试视图。
- 地图不接收GameState、IGameSession、手牌或控制器；只接收公开静态节点和只读公开状态。控制方和合法性来自现有投影/查询，不在地图重算。
- 契约本体无Unity类型；渲染适配器单独使用Unity。国家ID来自真实国家集合，未知ID不能被猜测或映射到其他国家。
- v1为已经在内存中的公开数据使用同步Initialize/Present/FocusOn/Clear/Dispose；Intent仅SelectCountry。异步资源加载、主题/正式layout装配、动画和超级大国节点交互保留设计，不用空方法伪装完成。
- ViewContextId在换地图、切换视角和重置后更换；StateVersion、FrameSequence及InteractionKey共同约束输入。切换再切回同一身份也必须丢弃旧输入。
- 普通点击只选中国家；事件合法目标点击只编辑本地草稿，仍由“确认完整选择”按钮提交权威事务。
- 同上下文允许新FrameSequence在相同StateVersion刷新草稿；旧帧不覆盖新帧。Dispose后拒绝呈现、聚焦、点击，取消监听。

## Review Focus

- 旧视图保存的委托在Dispose后被调用：协调器再次核对来源及上下文，必须无效果。
- 双向切换身份后回到同一方且StateVersion相同：Controller.ViewGeneration必须使旧输入失效。
- 事件草稿在同版本内改变：旧FrameSequence不能再次切换草稿，帧必须完整覆盖国家集合。
- 控制器在绘制后状态推进/重置但尚未Refresh：点击时重新核对GameId/Version/Generation/InteractionKey，不能接受旧画面输入。
- 加载失败或不完整节点映射：先清旧交互；失败后无可点击旧帧，不把未初始化视图当成功。

## Task 1：公开契约、生命周期与输入门禁

文件：新增Presentation/Map/MapContracts.cs、MapViewBuffer.cs、MapPresenter.cs；修改Presentation/DemoController.cs添加只读ViewGeneration；Tests/EditMode/MapViewTests.cs、MapPresenterTests.cs。

接口：IMapView.Initialize(MapViewContext)、bool Present(MapFrame)、event Action<MapIntent> Intent、FocusOn(CountryId)、Clear()、Dispose()。MapPresenter.Attach(IMapView)、Refresh()、Dispose()。MapFrame包含ContextId/Version/Sequence/InteractionKey和只读MapCountryFrame列表；单国值包含ID、US、USSR、投影控制方、合法/草稿/选中标记。MapViewContext只包含CountryId、名称、地区、稳定度、战场标记。

- [x] 创建最小编译桩和行为测试，先验证失败。覆盖只读复制、完整节点、错误上下文/版本/序号、释放、未知国家；协作测试覆盖换视图不改状态、切换身份/场景/推进后旧输入拒绝、事件草稿不结算、两视图相同操作产生同结果。

```csharp
var before = controller.ReadView();
presenter.Attach(firstView);
presenter.Attach(secondView);
Assert.That(controller.ReadView().GameId, Is.EqualTo(before.GameId));
Assert.That(controller.ReadView().Version, Is.EqualTo(before.Version));
firstView.ReplayCapturedIntent(); // 已退役来源必须被拒绝
Assert.That(controller.SelectedCountry, Is.EqualTo(original));
```

- [x] 实现不可变DTO、MapViewBuffer的校验及输入发送；MapPresenter在提交本地选择前再次比较当前玩家投影与generation，不直接调用规则变更。
- [x] 用Tools/Scripts/run-unity-editmode.ps1 -RunId map-contract-red/green运行实际回归，记录红绿计数。

## Task 2：两种实际诊断视图与Demo接入

文件：新增Presentation/Map/DiagnosticMapView.cs（复用绘制基础、两种明确布局），修改DemoView.cs/DemoView.Events.cs、DemoLaunchOptions.cs/Bootstrap/DemoBootstrap.cs；相应EditMode测试。

- [x] 为启动视图白名单和两适配器同序列效果添加失败测试。`--demo-map=tiles|table`默认tiles，未知或重复值拒绝，不能把CLI当任意类名/路径。
- [x] 抽出中间国家绘制；地区按钮仍是独立导航，通过FocusOn/Region选择显示子集；地图完整公开帧仍覆盖84国。增加“卡片/列表”切换，不调用LoadScenario，保留已选国家与事件草稿。
- [x] 两个适配器共享同一公开输入协议，但实际使用不同布局；按钮/手牌/状态面板仍通过原控制器。
- [x] 跑完整EditMode、Windows构建，按两个启动参数各导出真实游戏PNG检查中文、国家数值、选择提示；不以截图冒充鼠标自动验收。
- [x] 更新模块JSON、目录、Wiki/既有接口实现状态、工作记录和实际验证报告；独立审查后修复，Git常规提交推送并核对。

验证入口示例（每次全新RunId）：

```powershell
& 'E:/TwilightStruggle/Tools/Scripts/run-unity-editmode.ps1' -ProjectRoot 'E:/TwilightStruggle' -UnityEditor 'E:/unity6.6/6000.6.0f1/Editor/Unity.exe' -RunId 'map-contract-red'
```

完成证据：170/170、独立复审通过、最终Preview构建成功、两适配器实际渲染与未知地图退出1。鼠标自动操作在用户暂停后仍返回failed to activate captured window，一次恢复也失败；停止点击，未将截图作为点击成功。
