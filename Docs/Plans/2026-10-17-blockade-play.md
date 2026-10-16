# 封锁可玩接线 Implementation Plan

> **For agentic workers:** Use executing-plans inline; one fresh whole-branch review. 用户已要求自主持续开发，不重复询问常规设计批准。

**Goal:** 将已验证封锁基础接入直接事件、两种对手事件/OPS顺序、头条、固定身份会话及Demo私有选择。

**Architecture:** ActionPracticeState 增加显式 BlockadeDecision，沿用 ResolvingEvent 阶段，保留根 Actor。候选保存在权威状态，PlayerViewSnapshot 只向US投影；CardActionCommand 继续提供固定身份、ExpectedVersion和幂等。弃牌或西德移除与原卡移出/原根续接合为宿主一次事务，头条泵识别等待并在回应后继续。

**Tech Stack:** Unity 6000.6.0f1 / pure C# Core / NUnit / existing command host and IMGUI Demo.

**Spec:** Docs/Wiki/hand-events-batch1.md；Docs/Wiki/blockade-hand-discard.md；前置实现 e1001a4e185db5839d5c4142f4e5be8a63a27503 已推送并核对远程。

## Global Constraints

- 不复制阈值与国家常量；BlockadeDefinition 绑定实际 Map/Cards/Ops。
- US可弃合格普通事件牌或Decline；无候选直接fallback；非法/过期响应不fallback。
- 候选及其数量不公开；被接受弃牌进入公共弃牌堆，星号被弃牌也不移出。
- 响应不发新OPS、不消耗随机、不改军事/DEFCON/根行动身份；原Blockade成功按星号移出。
- 头条/对手OPS/行动根各自完成一次；等待时其他普通操作不可绕过。
- 单窗口两个固定身份、画风可替换、正式共享JSON加载与Player载荷保持一致。
- 本批不实现五年计划、未知持续干扰、联网、存档或完整整局。

## Review Focus

1. 苏联触发却由美国回应；苏联视图、拒绝结果和旧收据均不得包含美国候选。
2. US自己打Blockade走两种OPS顺序，只有两步都完成才消费一个根；回应不是新行动。
3. 没候选、仅计分、China/已预留头条牌排除、零西德US仍成功移出原牌。
4. 旧版本、换局、重复/冲突CommandId、错误选择/状态绑定拒绝且牌区/影响力/RNG不变。
5. 头条等待与后继头条失败时原子回滚；数值溢出不得发布部分移除或弃牌。

### Task 1: Core回应与续接

Files: 新 Scripts/Core/Rules/BlockadeDecision.cs、ActionPracticeRules.Blockade.cs；修改 ActionPracticeState.cs、ActionPracticeRules.cs/.Events.cs/.OpponentOps.cs、HeadlineResolutionRules.cs、State/GameState.cs；新 Tests/EditMode/BlockadePlayTests.cs（路径均在Assets/ColdWarStruggle下）。

Interfaces: `ActionPracticeDefinition.Blockade`；`ActionPracticeState.Blockade`；`CardActionKind.DiscardForBlockade/DeclineBlockade`；`ActionPracticeRules.CanRespondToBlockade(GameState,PlayerSide)`；`CardActionTransition.BlockadeResult/HeadlineSteps`。决定保存实际牌区/效果/回合绑定及创建后的版本；候选来自HandDiscardRules。

- [x] 先写真实牌区测试及最小编译接口，跑原生红灯。
- [x] 实现开始/等待/弃牌/移除/完成，支持直接和两种OPS顺序；阶段门禁及根绑定。
- [x] 接头条支持清单、暂停、回应后续接与步骤回执；测试先后两个待选择、不消耗普通名额。
- [x] 覆盖错误方/非法选择/过期绑定/空候选/零移除/溢出回滚，跑全原生绿灯。

### Task 2: 会话投影与控制器

Files: 新 Application/Sessions/BlockadeView.cs；修改 SessionContracts.cs、CardActionContracts.cs、HeadlineSessionContracts.cs、LocalGameHost.Cards.cs、CardSessionView.cs；新 Presentation/DemoController.Blockade.cs；新 Tests/EditMode/BlockadeSessionTests.cs。

Interfaces: `PlayerViewSnapshot.Blockade` 包含Owner/CanChoose/DecisionId和仅US可见候选CardId+EffectiveOps；公开结果只包含已接受弃牌/西德实际变化，零候选明示完成；PublicCardSummary.DiscardPile使双方能查询已公开弃牌，不公开牌库；`DemoController.DiscardForBlockadeAsync(CardId)`/`DeclineBlockadeAsync()`。

- [x] 红灯：私有候选、跨身份尝试、旧版本/换局、同命令重放、冲突ID、切换身份清旧选择。
- [x] 使用已有固定身份命令宿主投影、收据缓存和SendCard控制器，不新增客户端可信状态。
- [x] 完成公开头条步骤结果和当前状态分离，原生回归。

### Task 3: Demo与交付

Files: 新 Data/Demo/blockade-scenario.json 及数据描述；新 Infrastructure/DataLoading/DemoCatalog.Blockade.cs；修改所有ActionPracticeDefinition重建点传递Blockade、DemoBootstrap/Editor构建载荷；新 Presentation/DemoView.Blockade.cs；新增Demo接线测试、模块JSON/Wiki/工作记录/验证报告。

- [x] 固定人工练习：美国含足门槛与不足门槛牌，西德双方均有影响力；场景标fixture。
- [x] 先验接线与坏数据红灯，再实现绑定/载荷/私有面板，既有目录功能保持。
- [ ] 全原生测试与元数据检查、独立只读审查、提交源码。
- [ ] 构建独立BlockadePlaytest，校验实际载荷，真实窗口检查弃牌、Decline、身份隔离与重发。
- [ ] 记录真实限制与证据，Git推送并核对远程；继续后续有用批次。
