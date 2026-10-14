# 政变进入行动试玩实施计划

目标：同一真实出牌生命周期中，玩家选择自己的可用事件牌和目标国家后执行完整普通政变；看到影响力、军事累计、DEFCON和掷骰结果。扩大行动试玩世界地图，不改规则邻接/地图接口。

架构：新增 PracticeRiskDefinition/State，保存 Defcon/Military/政变历史/VictoryDecision，不保存第二份国家或RNG。每次基于当前GameState国家/RNG和保留的历史绑定临时CoupAttemptState，已有PlanAttempt规划完后只把结果的最新国家/RNG与Risk历史同时提交回GameState。不能用Create重建从而清空历史。

边界：仅无活动效果的普通手牌政变；沿用本方/中立事件OPS限制，中国牌/对手事件/条约/危机未接入。军事信用和强度都从可信普通OPS报价派生，客户端只有牌ID/国家ID。Actor/PhasingPlayer都是本次根行动方；核战胜者使用VictoryService明确判定并把GameState.Ended作为吸收态，旧命令可重发。

任务：
1. 风险状态与当前状态绑定测试：普通/战场成功失败、地图及回合匹配、影响力与太空之后最新国家/RNG不回退、军事和收据保留、核战后拒绝、溢出；RED/GREEN。
2. CardActionCommand新Coup/Target字段，严格载荷组合、会话目标查询和投影；重试不同目标冲突、并发旧版本、秘密过滤测试。
3. 控制器本地选择用途/目标，地图合法高亮，单次确认政变；扩大行动模式地图、顶部固定DEFCON/军事/VP/太空显示；真实共享四数据载荷追加，构建另一路径避免当前用户Demo锁定。
4. Wiki/JSON/报告，完整回归、一次独立审查、例行Git推送。

后续：接现有八种可执行事件/地区计分，再接头条和标准回合结束。不以本练习名额替代完整日历和奖励行动。


Spec: Docs/Wiki/playable-card-actions.md、coup-attempts.md、risk-victory.md、prototype-roadmap.md。
Tech: Unity6000.6.0f1, C#9, existing deterministic PCG; no new package/script.
Review focus: 连续政变/太空/影响力交错不回退历史或RNG；核战立即结束并保留旧收据；命令Target严格匹配Coup用途；地图高亮是提示而非授权；老无Risk练习仍可运行。

- [x] T1 Core Risk + 原子政变计划：PracticeRiskState.cs、ActionPracticeState.cs、ActionPracticeRules.cs、GameState.cs；PlayCoupTests RED/GREEN。
- [x] T2 命令与控制器：CardActionContracts.cs、LocalGameHost.Cards.cs、SessionContracts.cs、DemoController.Cards.cs；PlayCoupSessionTests/ControllerTests RED/GREEN。
- [x] T3 数据装配与大地图UI：DemoCatalog.Actions.cs、DemoBootstrap.cs、DemoBuild.cs、DemoView.Actions.cs与DemoView.cs；启动前校验四份共享数据，追加新构建目录。
- [ ] T4 descriptor/Wiki/报告/工作记录，最终独立审查，快进合并/推送。
