# 普通空手行动衔接

Description：定位零普通手牌时的跳过门禁、中国牌自愿使用、双方名额推进与人工试玩入口。

当前状态：Core/session、界面和人工入口已实现，empty-ui-green 的2600项原生测试通过；Player待验。计划见 Docs/Plans/2026-10-17-empty-hand-actions.md；摘要入口 Docs/Descriptors/empty-hand-actions.module.json。

## 规则边界

R2015 §4.5D：普通手牌不足时可以缺席剩余行动；§5.1：有牌时不能任意放弃出牌；§9.8：中国牌不因缺牌而被强迫使用。来源 https://www.gmtgames.com/nnts/TS_Rules-2015.pdf ，2026-10-17 核对。

实现采用逐次确认当前普通名额，便于单窗口测试，也让每次都能重新检查手牌。没有牌不等于没有合法动作：未实现卡牌、未完成事件、选择、部署都不能通过此入口绕开。

## 规则流程

固定身份 SkipEmptyHand 命令 → CanSkipEmptyHand 检查当前阶段/身份/普通名额/空手 → SkipOrdinarySlot 返回新进度 → 原有事务发布新版本 → 下一方或回合末。

CompletedActions 与 CompletedFor 表示已消耗行动名额，包含完成根和规则允许的普通空手跳过；太空额外名额放弃不增加此计数。中国牌、国家、卡牌区、随机和风险轨道不随跳过改变。

代码入口：Core/Rules/PracticeEmptyHandRules.cs 的 CanSkipEmptyHand / PlanSkipEmptyHand；ActionRoundProgress.SkipOrdinarySlot；Application/Sessions/CardActionContracts.cs 的 PublicPracticeView.CanSkipEmptyHand。测试 EmptyHandActionTests 覆盖双方交替、早晚期、中国牌、获牌后重查、太空额外衔接、终局/下一回合和身份/幂等/溢出。

## 试玩入口

新增人工回合末入口，复用现有源地图和印刷牌库，按时代重建实体分区。普通前若干名额通过真实命令准备，仅留下苏联空手、美国一张普通牌和苏联正面中国牌。不会伪称这是完整自然对局的历史；正式连续对局也使用同一规则入口。

WithEmptyHandScenario 在20场景后追加第21场景；DemoTurnEndSetup.OrdinaryActionsToPrepare 仅影响可信人工准备，默认原有全部普通名额不变。当前第2回合实际执行10个根行动到v21，再交由用户操作。准备用牌保留原顺序，额外剩余牌必须来自源牌库且支持美国当时代普通OPS。

DemoController.SkipEmptyHandAsync 发送固定身份命令，按钮和提示依据公开查询结果显示。BuildEmptyHandPlaytest 输出 Builds/EmptyHandPlaytest，Editor/Player复用相同数据。
