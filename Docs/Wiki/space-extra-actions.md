# 太空额外行动

Description：先查 ActionRoundProgress 的双方独立根计数、可选总额度和放弃，再定位真实出牌接线。

当前仅基础模块实现，Task2真实根行动与Task3界面尚在接入，旧Player未更新。描述：Docs/Descriptors/space-extra-actions.module.json。

## 规则与流程

普通行动按时代6/7次，苏联先、美国后。普通名额完成后查询 SpaceAbilityRules.Query 的 ActionRoundAllowance；正式第8格值8，因此早期最多两个额外名额，中晚期最多一个。双方追平无能力。数值源为 Data/Rulesets/deluxe-2015/space-race.json，不在调度器复制。

Create → 按 CurrentActor 执行根行动 → CompleteRoot(最新轨道) → 下一普通/可选名额或完成。可选名额未开始可 DeclineRemaining，放弃不计次数。CanBegin 每次开牌前重查资格；已开始根在轨道变化后仍可完成，下一名额重新查询。正常空手跳过与北海油田不在此批范围。

## 代码按符号读取

- Core/Rules/ActionRoundProgress.cs：Create、Schedule、CompleteRoot、DeclineRemaining、RemainingAllowance。
- Core/Rules/SpaceAbilityRules.cs：Query，计算当前独占能力。
- Tests/EditMode/ActionRoundProgressTests.cs：正常交替、双方额外、总额度、获得/失去、放弃、身份与绑定。

设计见 Docs/Design/2026-10-16-space-extra-actions.md；验证见 Docs/Testing/space-extra-actions.validation.json；后续工作见 Docs/Plans/2026-10-16-space-extra-actions.md。
