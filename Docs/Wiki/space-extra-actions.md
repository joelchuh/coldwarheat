# 太空额外行动

Description：先查 ActionRoundProgress 的双方独立根计数、可选总额度和放弃，再定位真实出牌接线。

基础模块、真实根行动、两个人工入口及界面已接入；2567项原生测试通过。独立SpaceExtraPlaytest构建、47份载荷和两条实际鼠标路线已通过；审查0阻塞、1个显示Minor已修复并复看。其他旧Player未更新。描述：Docs/Descriptors/space-extra-actions.module.json。

## 规则与流程

普通行动按时代6/7次，苏联先、美国后。普通名额完成后查询 SpaceAbilityRules.Query 的 ActionRoundAllowance；正式第8格值8，因此早期最多两个额外名额，中晚期最多一个。双方追平无能力。数值源为 Data/Rulesets/deluxe-2015/space-race.json，不在调度器复制。

Create → 按 CurrentActor 执行根行动 → CompleteRoot(最新轨道) → 下一普通/可选名额或完成。可选名额未开始可 DeclineRemaining，放弃不计次数。CanBegin 每次开牌前重查资格；已开始根在轨道变化后仍可完成，下一名额重新查询。后续[普通空手跳过](empty-hand-actions.md)通过SkipOrdinarySlot接入，计数含合法跳过；北海油田仍待接。

## 代码按符号读取

- Core/Rules/ActionRoundProgress.cs：Create、Schedule、CompleteRoot、DeclineRemaining、RemainingAllowance。
- Core/Rules/SpaceAbilityRules.cs：Query，计算当前独占能力。
- Tests/EditMode/ActionRoundProgressTests.cs：正常交替、双方额外、总额度、获得/失去、放弃、身份与绑定。

设计见 Docs/Design/2026-10-16-space-extra-actions.md；验证见 Docs/Testing/space-extra-actions.validation.json；后续工作见 Docs/Plans/2026-10-16-space-extra-actions.md。

## 真实事务接线

ActionPracticeState.Rounds贯穿头条/事件/OPS/结束克隆，非零历史缺失进度立即拒绝。ActionPracticeRules.Next只在根结束调用CompleteRoot；CanPlay在ChooseCard检查当前能力；China资格按当前方RemainingAllowance。DeclineExtraActions只在可选ChooseCard接受，卡牌/随机/地图均不变。PublicPracticeView只投影双方已完成次数和按viewer计算的放弃资格；手牌走原隐私边界。回合末/下一回合/最终计分都要求Rounds完成，下一回合新建零历史。

测试：SpaceExtraActionPlayTests和SpaceExtraActionSessionTests覆盖双方、早晚期、事件子步、China与计分保留、终局、审核/弃牌/后续、身份/版本/幂等/溢出。

## 练习与表现

WithSpaceExtraScenarios在18入口后追加第19（第2回合美8/苏4）和第20（第10回合美4/苏8）。来源地图与普通手牌不复制到JSON；从源手牌取时代普通额度，额外持有牌须来自源牌库且符合时代，重新按Era划分手牌/牌库/储备。CreateTurnEndFixture经真实普通出牌到v25或v29，只停在可选名额前。初始太空轨道是人工配置，没有捏造奖励/随机历史。

DemoController.SpaceExtra发送现有固定身份命令。DrawExtraActionChoice在顶部状态条右侧提供放弃，已行动计数在OPS摘要前，原结果区保持可见，选择牌即走既有用途按钮；只有尚未出牌的可选槽能放弃。PracticePhaseText区分太空额外7/8、8/8。地图和手牌观察接口不变。Editor/Player装配同一JSON，BuildSpaceExtraPlaytest输出独立Builds/SpaceExtraPlaytest。

真实验收：早期投放+事件完成两个额外根→第3回合补牌；晚期放弃→审核→保留手牌→最终美国VP1胜出。幂等重发与错误身份按钮已核对。最后四处纯显示改动后重新编译Player并实测，不重复未变规则的全套测试。详细截图/日志路径和限制见验证JSON。
