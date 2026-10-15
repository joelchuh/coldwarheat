# 头条试玩实施计划

description: 新第15场景、显示模型/控制器与地图UI接线；spec Docs/Architecture/headline-playtest-runtime.md。Base d0844da6b8637c0175cc3b6cdd334ec25644a848；branch feat/headline-playtest。使用executing-plans inline流程与一次fresh整批审查，用户持续授权覆盖常规实现/Git；单独等待的只是编辑器退出以做原生验证。

## Task1 独立场景与可见配置
新增Data/Demo/headline-scenario.json和Data/Descriptors/headline-scenario.data.json/catalog；Infrastructure/DataLoading/DemoCatalog.Headlines.cs；PublicPracticeView.HeadlineEnabled。新增HeadlineDemoCatalogTests（路径均在Assets/ColdWarStruggle/Scripts或Tests/EditMode下）。复用现有QuickStartDemoTests.Catalog和共享参数，末尾WithHeadlineScenario；旧14场景对象、能力/初始牌/部署引用不变。Test：合法进入收集/旧quick仍ChooseCard、秘密候选、源目录绑定、真实手牌一致、坏scope/profile/unknown/source/重复/字段拒绝、旧配置flagfalse。
RED→GREEN，完整可用托管回归；Expected全通过，metadata/diff0。同步模块JSON/Wiki/验证和Task1提交。

## Task2 纯显示模型与控制器
新增Presentation/HeadlinePanelFrame.cs、DemoController.Headlines.cs；修改DemoController.LoadScenario/SelectSide触发ResetHeadlineInteraction。新增HeadlineDemoControllerTests/HeadlinePanelFrameTests。具体公共接口：Frame.Create(view,names,observer)、CanChoose、OwnLocked、OpponentLocked、Prompt、ResolutionLines、ObserverLines；不保留原始GameState/其他方View；Controller.SubmitSelectedHeadlineAsync/RetryHeadlineAsync、LastHeadlineResult/CanRetryHeadline。
先RED两方操作顺序/受限候选/切身份与重置/幂等/历史提示、公共排序/取消/等待与无信息泄露、observer版本/局面约束、名字缺失拒绝；再实现并全回归。Expected不绕过Host或用静态卡名推断效果。同步知识/提交。

## Task3 UI/启动/构建与验收
新增DemoView.Headlines.cs；修改手牌选择、DrawPracticeControls/Status及TurnEnd提示，接控制器；不增加Core裁定。Bootstrap Editor/Player及DemoBuild验证链加载独立场景，新增3载荷，新增HeadlinesPlaytest构建入口；DemoLaunchOptions优先新ID。更新相关测试（只改被新行为影响的断言，不削弱旧入口兼容），新增启动/载荷与提示测试。
先RED可测行为，再实现；完整托管、metadata、diff；用户关闭editor后run-unity-editmode实际执行（独立RunId）；独立批次审查后修正阻塞项、构建新播放器并鼠标验收。不能把预览命令/可见窗口当原生pass；失败保留日志、继续独立工作。最后功能分支push核对远程SHA；main与旧exe按真实证据处理。

## Review Focus
普通Actor不误挡同时选择、私有第一牌不从历史/名称/observer边界泄露、首锁不可修改、切身份/重置和异步结果不串局、物理发牌/部署未改、旧14入口兼容、所有能力绑定保留、地图不裁决规则、完成/取消/等待提示忠实公开状态、不同生命周期的源数据和生成载荷一致、native/build/UI证据分开。

## 进度
- [x] Task1 场景/配置
- [x] Task2 模型/控制器
- [ ] Task3 UI/构建/审查/同步
