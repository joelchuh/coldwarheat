# 已选叛逃者头条运行时实施计划

description: 按既有已确认规则增加头条取消，三任务TDD与一次整批审查。Spec: Docs/Architecture/defectors-headline-runtime.md。

Base:a6162c9ee743cdf2dca8ebb71c4305221f02c8ce；branch feat/defectors-headline。同一项目功能分支，main/player不变。

## Task1 定义、数据载入、排序与完成记录

新增Core/Rules/DefectorsHeadlineDefinition.cs，Infrastructure/DataLoading/DefectorsHeadlineLoader.cs；修改HeadlineResolutionState.cs；新增Tests/EditMode/DefectorsHeadlineDefinitionTests.cs。所有源码路径基于Assets/ColdWarStruggle/Scripts。

接口：DefectorsHeadlineDefinition(CardCatalog catalog,CardId card)只绑定支持的selected-headline语义；Load(json,schema,catalog)读取共享参数和印刷目录；CreateSelected(parameters,catalog,cards,turn,defectors)在US所选Defectors时使用具名优先，其余仍普通排序。队列新增OrderStrategy、CompletedRoots和CurrentCancellationSource；AdvanceAfterCancellation(card,side)推导合法来源，禁止自报。AdvanceAfterResolution不能推进必须取消的当前根，原方法对普通根保留。

先写RED行为：profile/schema/相关语义畸变、载入目录约束、US高于4OPS、USSR不提序、先后精确一次、来源推导与不可篡改、取消记录不算成功、重复/错牌/错方拒绝、未知/无绑定保持普通行为。Expected:命名RED XML实际失败；再实现GREEN和2048既有可用回归。提交定义检查点，同步JSON/Wiki/证据。

## Task2 核心根取消

修改ActionPracticeDefinition末尾追加可选HeadlineDefectors（同目录且无重复handler），HeadlineResolutionRules.SupportedCards和Begin/Advance、ActionPracticeRules.Headlines；新增DefectorsHeadlineResolutionTests。复用共享Move/Commit，current源验证不变。US Defectors源成功弃牌，然后精确取消待执行苏方；USSR Defectors按普通次序自取消，无VP。两张完成仍零普通动作/OPS，RNG保持。

先RED高值/星号/计分取消、影响力/VP/DEFCON无变化、USSR自取消先后、等待US选择后再自取消、先方终局保留现场、技术溢出整批无草稿、无支持配置/未知handler仍拒绝；再实现GREEN和全部可用回归。Expected:不能把取消伪装成功事件，不能取消已完成根或倒转历史。

## Task3 公开会话与验收

修改HeadlineSessionContracts投影OrderStrategy/CompletedRoots/Step.Completion；新增DefectorsHeadlineSessionTests。先RED私有第一牌、公开取消与优先、锁牌和幂等/历史收据、等待恢复返回自取消、正常事件仍有正确Resolved记录；再实现。既有Host事务路径沿用，不加任意Skip/Cancel玩家命令。

更新模块JSON/catalog、Wiki/流程、Worklogs/Testing/来源与源码XMLhash。执行metadata验证与git diff --check Expected:exit0；一次fresh-context整批审查、阻塞项一次TDD修复、常规推功能分支并核对远程SHA。Unity许可未恢复，native160项/asmdef/build/UI均独立待验，不改旧Demo工厂。

## Review Focus

取消不写成功事实、实体弃牌不看星号、US优先与USSR普通次序、选择等待和终局不越过、来源只从可信已选根推导、无普通VP/嵌套暗接、失败原子性与历史收据、公开结果不泄露私牌或原始State、共享数据事实未复制、普通排序/普通动作兼容。

## 进度

- [x] Task1 定义/载入/队列
- [x] Task2 核心结算
- [x] Task3 会话投影实现与托管验证
- [x] 整批源码审查；功能分支同步见最终远程核对
- [ ] Unity原生复核（用户保存退出编辑器后）；新Demo/主线另行验收
