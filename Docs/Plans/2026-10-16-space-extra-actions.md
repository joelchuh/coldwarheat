# Space Extra Actions Implementation Plan

> Use executing-plans inline and test-driven-development; one fresh whole-branch review at completion. User authorized autonomous continuation and normal Git push.

**Goal:** 可选太空额外根行动与回合结算连续闭环。
**Spec:** Docs/Design/2026-10-16-space-extra-actions.md
**Architecture:** 独立不可变进度→Core现有根完成入口→固定身份投影与可替换地图旁面板。
**Tech:** 现有Unity6000.6/NUnit/C#/JSON；无新增依赖。

## Global Constraints

基线030ed9f，2510原生测试；E:/TwilightStruggle复用已授权Unity工作目录，分支feat/space-extra-actions，保留两项无关未跟踪文件。共享Space能力数值；不复制卡牌事实。已支持的头条/事件/OPS/回合末路径不回退。范围不含普通空手跳过、北海油田和新卡事件。

## Task 1: 不可变行动进度

Files: Core/Rules/ActionRoundProgress.cs；Tests/EditMode/ActionRoundProgressTests.cs；Docs/Wiki/space-extra-actions.md、Descriptors/space-extra-actions.module.json。
Interfaces: 按Spec的Create、CompleteRoot、DeclineRemaining、CanBegin、RemainingAllowance；状态从零开始且不可变。

- [x] 写可编译行为测试与最小stub，RunId extra-rounds-red，验证失败是行为而非编译。
- [x] 实现正常交替及可选额度调度；测试双方6/7/8总额、早期两个额外、晚期一个、获得/失去资格、错误身份/重复/跨定义、未完成根不变；RunId extra-rounds-green。
- [x] 同步Wiki/JSON/目录和真实验证记录，metadata/diff通过后提交。

## Task 2: 真实根行动与宿主

Files: ActionPracticeState、所有ActionPracticeRules及Headline/TurnEnd/NextTurn/FinalScoring/SpaceDiscard克隆；Application/SessionContracts；Core新PracticeExtraActionRules；Tests/EditMode/SpaceExtraActionPlayTests与SessionTests。
Interfaces: ActionPracticeState.Rounds；DeclineExtraActions；CanDeclineExtraActions；public每方次数/optional/可放弃。沿用固定身份CardActionCommand。

- [x] 测试普通完成→拥有者可选出牌/放弃、子事件仅根计次、China剩余额度、终局截断、下一回合重置、审核不得越过额外名额；旧Unsupported断言更新。RunId extra-play-red。
- [x] 全部克隆携带进度，Next统一CompleteRoot，普通出牌校验新槽，结束门禁改为进度完成。RunId extra-play-green。
- [x] 宿主重复ID/旧版本/冲突/错误身份/overflow及公开无泄露；文档与提交。

## Task 3: 可玩的人工场景与独立Player

Files: Infrastructure数据装配、Data/Demo额外行动fixture与描述、Presentation控制器/面板、Bootstrap/Editor构建；相关EditMode测试。
Interfaces: WithSpaceExtraScenarios使用第2回合美国领先、第10回合苏联领先；复用源地图与按时代筛过的普通行动牌，重建合法时代牌区。已有ChooseCard面板+明确放弃按钮（放在结果区，不遮手牌/重发）；独立Builds/SpaceExtraPlaytest。

- [ ] 写fixture/控制器测试，RunId extra-ui-red；实现并RunId extra-ui-green。
- [ ] 一次新上下文全分支审查；重要项一轮RED/GREEN修复；元数据/diff/构建/载荷字节核对。
- [ ] 实际鼠标：额外正常出牌、放弃、重复命令、军事审核/弃牌/下一回合或最终；F12游戏截图及日志检查。
- [ ] 更新Wiki、JSON、工作记录和验证证据；提交/push功能分支，核对远端SHA。

所有原生测试使用 Tools/Scripts/run-unity-editmode.ps1 的新RunId；每轮读取XML真实总数/失败，未变代码不重复全套。分阶段报告不把设计当实现。
