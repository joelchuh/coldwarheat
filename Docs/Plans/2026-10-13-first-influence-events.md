# 首批影响力事件实现计划

> For agentic workers: 使用 executing-plans 在本会话逐项实施、test-driven-development 验证失败与修复；完成有实质内容的任务后使用 requesting-code-review。主代理实现，不分派互相耦合的实现任务。

**Goal:** 把已有五张基础影响力事件设计变成可复用、可验证的 C# 效果与选择逻辑，并逐步接入同一权威局面及单人演示。

**Architecture:** 事件定义是不可变类型化数据，算法是显式注册的封闭处理器。Core 在隔离工作副本上生成影响力变化计划；Application 管理事件实例、决策和身份收据；Presentation 编辑选择草稿并一次提交，不修改局面。

**Tech Stack:** 已实际运行的 Unity 6000.6.0f1、C# 9、Newtonsoft JSON 3.2.2、Unity Test Framework 1.8.0。

**Spec:** [早期影响力事件第一批](../Wiki/early-influence-batch1.md)、[真实首批Demo](../Wiki/first-demo.md)、[事件参数](../../Data/Cards/deluxe-2015/early-influence-batch1.parameters.json)。用户已批准首批Demo，随后要求持续优化至剩余额度10%；本计划执行已有卡牌设计，不另选画风或改规则。

## Global Constraints

- 使用同一 GameState；Core 不引用 Unity、JSON 或文件系统。
- 牌面事实、固定国家、额度和除数从既有共享参数读取，不复制到每个处理器或UI。
- 直接事件不花OPS、不查OPS可达性、不收敌控双倍费用，不自动触发计分或DEFCON。
- 控制补足只增加差额；纳赛尔按移除量向上取整；去殖民化完整选择四个不同的非洲/东南亚国家。
- 杜鲁门只选双方都未控制的欧洲国家，零苏联影响合法；无候选无变化完成。
- 未知活动效果组合返回RequiresRuling；无法确认的短缺不自创min规则。
- 选择拥有者来自事件受益方，不来自出牌者；过期/重复/非法完整选择不能部分提交。
- 纯规划完成不代表正式出牌、弃牌/移出、头条或回合已实现；完整RulePack门禁不能因此放宽。

## Review Focus

- 可变输入集合或参数被修改：定义、选择和结果均防御性复制（任务1）。
- 溢出发生在第二步：前一步清除/增加也不能进入原局面（任务1）。
- 旧选择对应新局面：重新验证当前候选，SourceVersion不符拒绝（任务1/2）。
- 改身份或重复点击：决策Owner固定，同ID改payload冲突，成功后重发原收据（任务2）。
- 参数或规则干预未知：停止并显示明确状态，不把无实现当作空效果成功（任务1/2）。

## 任务1：类型化定义、选择与纯规划

创建 `Scripts/Core/Effects/InfluenceEffectDefinition.cs`，四类封闭定义：ClearOpponentThenEnsureControl、AddThenRemoveFraction、RemoveOpponentFromUncontrolledCountry、AddToDistinctCountries。创建 `InfluenceEventRules.cs`、`InfluenceEventPlan.cs`、内部 `InfluenceMutation.cs`；路径前缀 `Assets/ColdWarStruggle/`。

创建 `Scripts/Infrastructure/DataLoading/EarlyInfluenceCatalogLoader.cs`，读取既有五牌参数及Schema；必要时扩展 `ContractJson`，只支持本地 `$defs/$ref` ，限制递归深度，拒绝远程引用和未知Schema关键词。JSON不能保存任意操作序列、函数名、反射或脚本。

接口：`EarlyInfluenceCatalogLoader.Load(json,schema,map)` 返回按CardId查询的只读定义。`InfluenceEventRules.Query(state,definition,policy)` 返回只读候选与数量/Owner或具名错误。`Plan(state,definition,selection,policy)` 返回隔离的 `InfluenceEventPlan`，含NextState、顺序变化与完成结果；需要但未提供选择时不返回已提交状态。选择携带源版本；非选择事件不接受额外目标。

- [x] 先写真实EditMode失败测试：古巴清US补控制且保留过量USSR、罗马尼亚复用、纳赛尔奇/偶/零及int边界、杜鲁门控制/零目标/无候选、去殖民化25候选与跨区完整选择。
- [x] 覆盖未知干预、错误侧/目标/版本、候选不足待裁定、同国重复、步骤溢出、输入不可变和无OPS成本。
- [x] 扩展参数加载的结构/版本/来源/字段与地图引用验证；未知模板拒绝；Schema组合关键词不能跳过同级限制。
- [x] 实现Core隔离规划，运行新增套件与既有52项，记录XML，更新模块描述/Wiki；此阶段不修改正式卡牌注册表的planned状态。
- [x] 独立审查、修复并提交本任务。纯规划不能冒称事件决策已进入权威宿主。

## 任务2：同一宿主中的事件与决策事务

扩展 `Core/State/GameState.cs` 的事件继续状态，新增 `Application/Sessions` 中的类型化事件命令/结果。复用同一宿主锁、身份投影和收据命名空间；事件等待时禁止无关OPS改变局面。可信演示装配器选择已加载定义，不允许玩家上传任意效果/额度。

- [x] 写失败测试：美国出牌但苏联做去殖民化选择、非法集合不推进、重复响应/改payload、旧版本与重置、完成后重复触发拒绝。
- [x] 实现一次性事件实例与等待选择，计划/完成标记/状态/事件/收据原子提交；查询不写状态；未知活动效果不绕过。
- [x] 校验普通会话不获得私有内部状态；既有影响力模式不退化；完整套件与独立审查后提交。

该任务开工时根据任务1实际接口细化DTO，不把待实现名称登记为真实入口。

## 任务3：演示选择与交付

扩展共享演示数据为明确的事件练习，UI显示候选和“已选/所需”数量，允许修改草稿，一次确认完整选择；Fidel等无选择效果提供单次执行。仍为人工场景，不伪装完整手牌出牌。

- [x] 先写控制器的身份、草稿、重置、幂等回归；验证后接入可替换面板。
- [x] 构建并通过游戏自身导出真实画面；computer-use恢复后再单独做鼠标验收，不以截图代替。
- [x] 同步实际JSON描述、数据依赖、Wiki/工作日志和测试报告，审查后按既有授权提交推送并核对远端。

后续完整牌区生命周期、回合、持续效果系统另分批实施。本计划不声称102个处理器已全部实现。

完成记录：149项EditMode通过；Windows构建与合法/错误场景捕获已验证，九份JSON与源一致；独立复审关闭P2/P3。computer-use窗口激活失败，鼠标路径仍未验收，未以截图替代。
