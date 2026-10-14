# 太空轨道与能力查询基础实现计划

> executing-plans inline执行，一次整批独立审阅；用户持续优化/Git授权有效。开始前检查剩余额度，用户最新要求覆盖旧10%停止线：剩余约1%时使用一张已有Codex额度重置卡，兑换结果需重新查询验证。
**Goal:** 把已有八格轨道和四项领先能力的共享数据转为可调用的纯C#定义及只读查询，供未来普通尝试/头条/回合调度使用。
**Spec:** Docs/Wiki/space-race.md；Data/Rulesets/deluxe-2015/space-race.json。
**Architecture:** 不可变SpaceRaceDefinition/SpaceTrackState，SpaceAbilityRules.Query返回当前能力值。严格LoadSubset装配。这里只计算位置派生事实，不提供玩家随意移动太空的命令。
**Tech Stack:** C#9、Newtonsoft、Unity EditMode。

## 范围与决定

先实现定义/位置/能力查询，下一批再接普通尝试、骰子、计数、奖励、防重收据和卡牌去向。这一划分避免在没有出牌授权与事务时把普通工具函数当成完整太空行动。直接事件推进（Scientist/OneSmallStep）仍保留各自待裁定边界，不顺手补默认政策。

轨道起点、普通次数、骰子范围和各格数值取共享JSON；位置连续且稳定ID唯一，成功骰区间在骰子范围内，奖励非负，能力引用唯一有效。轨道必须有界；最多64格/能力是工程输入上限，不是游戏规则。普通一格推进/重置时机/领先规则是本子集支持的政策，未知政策明确拒绝。

SpaceTrackState保存同一Definition、双方合法位置；只读，没有Advance方法，后续由可信规则计划产生新状态。输入位置不表示已经发放VP或执行行动，只读查询不能生成VP/重置尝试。四种能力均按本人达到或越过对应格、对方尚未达到推导。第8格的返回值是总行动额度8，而非+1；无此能力时返回null，由TurnCalendar提供基础额度。第2格无领先能力时返回基础次数1；第4格返回是否对手先揭示；第6格无能力返回0。

用Kind枚举连接明确的能力类型，整数能力与头条策略分别验证，不依赖显示名称。加载器核对现有schema1/content0.2.0/来源引用/完整8格与4能力形状；不认证完整RulePack。Core可接受有界合成参数验证数据驱动，实际共享值不复制进生产逻辑。

## Task 1：纯定义与查询

- [x] Core/Rules/SpaceRaceDefinition.cs、SpaceTrackState.cs、SpaceAbilityRules.cs；EditMode/SpaceAbilityTests.cs先桩RED/GREEN。
- [x] 全9x9位置组合与双方对称查询，重点测试刚到/越过/对手追平/越过后均失效；多能力同时拥有；无领先能力时默认值。
- [x] 测试数据防御复制、唯一/连续位置与ID/能力引用、非法枚举/空/超限/越界、未知能力类型。
- [x] 不掷骰、不计VP、不消费卡牌/次数、不创建额外行动；结果只读。

## Task 2：共享数据加载与交付

- [x] Infrastructure/DataLoading/SpaceRaceDefinitionLoader.cs及测试；实际共享8格/4能力，损坏字段/类型/顺序/来源/政策/重复JSON/限额等拒绝；先RED/GREEN。
- [x] 同步space模块/data描述、catalog、Wiki和实际验证报告。描述明确普通尝试和事件推进仍不可调用。
- [x] 全量回归与正式metadata校验；整批审阅通过（0 Critical/Important/Minor），本地提交完成；推送状态以工作记录及实际远程核验为准。
- [x] 不接Unity表现层，不重建播放器。

## Review Focus

领先能力必须是本方位置>=里程碑且对手<里程碑，不能只比较双方当前位置谁更远。数据数值唯一来源。TrackState不是任意移动命令。查询结果仅给额度/资格，没有用牌授权或已完成行动。保留完整回合/头条编排责任。

## 来源复核（2026-10-14）

已重新读取GMT托管[2015规则书](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)：PDF共32页。§3.4在PDF第4页；§6.4在第7–8页；§7.4.2在第8页。共享太空数据原pdf_pages=[7,8,19]，已在本次收尾同步数据描述的对应来源页码为[4,7,8]；仅校正引用，不改轨道数值。
领先能力对应2/4/6/8格，先到有效、对手追到取消；第8格提供每回合总8行动轮。该复核支持现有位置谓词，不扩张为完整太空事件裁定。
