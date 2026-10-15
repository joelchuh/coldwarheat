# 头条基础服务实施计划

> **For agentic workers:** 使用executing-plans在当前会话逐项实现；每步按TDD执行，整批一次独立审查。当前持续开发与Git授权已明确，不重复等待编码许可。

**Goal:** 提供可独立测试的头条实体保留、秘密选择/投影及普通排序服务，为后续真实事件根接线准备。
**Architecture:** CardZones保存唯一实体位置；HeadlineSelectionState/Rules只收集选择，不执行事件；应用层按身份投影；普通排序读取共享headline参数。特殊取消由后续编排先处理，不能直接用普通排序代替。
**Tech Stack:** C#9、既有Core/Application/Infrastructure与NUnit；安装内置.NET8做补充验证。Unity许可证恢复后另跑EditMode/构建/鼠标。
**Spec:** Docs/Architecture/headline-runtime-integration.md，及Docs/Wiki/turn-flow.md第4节。

## Global Constraints

- 一个客户端两个固定身份；只读测试观察与正常投影分离。当前服务未接Host，调用者身份认证/版本/幂等仍待接。
- 中国牌独立，头条保留牌参与完整且唯一的牌区分区，不发OPS、不消耗普通行动、不掷随机数。
- 共享事实来自Data/Rulesets/deluxe-2015/turn-flow.json的headline与既有CardDefinition/SpaceAbilityRules；不复制太空阈值。
- HeadlineSelection.Begin接收可信运行时支持牌集合。它是覆盖能力白名单，不是给客户端设置的参数；未知处理器不得以无变化事件代替。
- 当前Demo继续跳过头条；不修改启动工厂、按钮、ActionPractice Actor或事件根。新状态暂不挂GameState。
- 普通排序函数名称明确BuildOrdinaryOrder，仅是候选排序策略；Defectors等先行取消必须由未来编排处理，不能冒称完整结算顺序。
- Unity许可阻断时只报告托管补充测试，开发分支提交/推送，main和已验播放器保留。

## Review Focus

1. 保留牌不得丢失或重复；每个旧CardZones重建点都须保留或明确拒绝未完头条。
2. 空白名单/无合法候选不能自动弃牌、跳过或判负。
3. 第一个秘密提交不能在对手投影或错误中泄露CardId；太空先看后选只改变选择策略。
4. 非法/重复提交不能改变原手牌、保留区、随机或普通行动；同ID重试属于未来Host职责。
5. 数据未知版本、排序策略、字段类型应拒绝；普通排序不能悄悄承诺处理特殊取消。

### Task 1: 牌区保留位置

**Files:** 修改Core/Cards/CardZones.cs、TurnDealService.cs及现有ActionPracticeRules/ChinaCardRules/PracticeTurnEndRules的重建点；新增Tests/EditMode/HeadlineCardZonesTests.cs。
**Interfaces:** CardZones最后新增可选IReadOnlyDictionary<PlayerSide,CardId> headlineReserved=null；暴露只读HeadlineReserved与GetHeadline(PlayerSide)。旧调用默认空；无新牌面常量。

- [x] RED：空区兼容、双方各一张、输入防御复制、重复实体/非法身份/未知牌/未来时代/中国牌拒绝；不同旧重建路径保留；带未结头条的TurnDeal在使用随机前拒绝。
- [x] GREEN：使用原唯一位置与时代校验接入保留区；补齐6处生产重建；不存在的外部调用不臆造。
- [x] 跑HeadlineCardZonesTests以及CardZones/TurnDeal/China/回合末相关回归。Expected: 新用例全部通过，旧分区行为不变。
- [x] 更新card-zones描述/Wiki/工作日志，提交可独立理解的纯牌区增量。

### Task 2: 秘密选择与按身份投影

**Files:** 新增Core/Rules/HeadlineSelectionState.cs、HeadlineSelectionRules.cs，Application/Sessions/HeadlineSelectionView.cs，Tests/EditMode/HeadlineSelectionTests.cs及对应meta。
**Interfaces:** HeadlineSelectionState.Begin(CardCatalog,CardZones,SpaceTrackState,IEnumerable<CardId> supportedCards)；HeadlineSelectionRules.PlanSelection(state,PlayerSide,CardId)返回Accepted/Error/State；HeadlineSelectionView.Create(state,viewer)返回自己的保留牌/候选、对手已锁定标志、允许可见的对手牌与Ready/CanChoose。

- [x] RED：普通两种提交次序、先方不可见/齐备同时可见、锁定不可重选、源不变；US/USSR太空领先及追平、先看后选提前提交拒绝；错误身份/不持有/不支持/中国牌/空候选；输入白名单复制、非法catalog/忙牌区拒绝；投影不可改集合。
- [x] GREEN：Begin冻结本阶段太空策略，只接受无现有保留/Resolving的完整牌区；支持集合验证；成功只手牌→保留区，返回新状态；不含卡牌的拒绝结果不推进任何字段。
- [x] 跑HeadlineSelectionTests及已有Session回归。Expected: 无隐私泄漏，旧会话未接线且保持行为。
- [x] 更新headline-foundation模块/知识摘要/工作日志并提交。

### Task 3: 参数加载与普通排序

**Files:** 新增Core/Rules/HeadlineOrderRules.cs、Infrastructure/DataLoading/HeadlineParametersLoader.cs、Tests/EditMode/HeadlineOrderTests.cs；模块/目录/Wiki/计划/验证报告。
**Interfaces:** HeadlineOrderParameters(int scoringValue,PlayerSide tieFirstSide)；HeadlineParametersLoader.Load(turnFlowJson)；HeadlineOrderRules.BuildOrdinaryOrder(parameters,catalog,usCard,ussrCard)返回只读两个HeadlineOrderEntry(Card,PlayedBy,Value)。

- [x] RED：大小值、平手按参数、双计分、计分对事件、读取实际数据0/US、定制参数证明数据驱动、双方阵营不改变PlayedBy；重复实体/China/未知ID拒绝；坏版本/重复JSON字段/未知排序/缺少或多余headline字段/浮点或越界整数/未知身份拒绝。
- [x] GREEN：共享profile校验复用TurnCalendarLoader；只消费headline三个字段，不把普通OPS修正引入印刷排序。不执行Defectors/任何事件。
- [x] 更新源码副本清单，运行全可用托管集合与metadata验证。Expected: 新增用例及旧1854托管集合均通过；Unity范围独立标待运行。
- [x] 独立整批代码审查，修复阻塞项，记录审查范围/源码hash/测试XML；提交并推送feat/headline-foundation，核对SHA，不合main、不构建假新版。

## 验证命令与环境偏差

当前临时runner位于chat/work/managed-check，使用Unity自带DotNetSdk/dotnet.exe build ManagedCheck.csproj --no-restore，再执行ManagedCheck.dll <fixture正则> <唯一XML路径>。测试副本仅替换三个既有fixture中的Application.dataPath；不改生产实现适配runner。新纯服务测试直接复制。每批记录精确测试数、输出路径与hash。

正式Unity命令：Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId <unique>。当前license exit198，必须恢复后再跑，不能把编译或托管结果当Unity通过。
