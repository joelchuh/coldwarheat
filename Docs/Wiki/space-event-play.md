# Scientist太空事件试玩

> description: 真实手牌/头条发动Scientist，查看一次轨道/奖励与双方能力变化；终点未知分支仍待裁定。
> 当前：源码已接，3213项原生测试全通过；独立审查和Player验收进行中，尚未交付新可执行版。

先读[模块JSON](../Descriptors/space-event-play.module.json)，按entrypoints定位实现；纯推进与加载复用[基础](space-event-foundation.md)。[设计](../Architecture/space-event-play.md)和[计划](../Plans/2026-10-17-space-event-play.md)记录边界。

流程：固定会话Event/Headline → 现有身份/版本/牌源验证 → CheckSpaceEvent → SpaceEventRules.PlanAdvance → ClosedVpBatchEvidence/VictoryService → 轨道、VP、真实牌去向与Next一次发布 → PublicSpaceEventResult → 原有结果页。中立事件受益者取可信出牌方；不掷骰、不计算事件OPS、不耗普通太空次数、不改军事/DEFCON/国家。正常星号移出；VP终局留Resolving且停止根及后续头条。

头条先应用Defectors取消，再执行事件；选择集合对实际终点持牌者隐藏未知事件选项，运行时也再次拒绝。候选集合只在权威端保存，普通玩家只见自己的手牌候选。新获得的太空4能力不会重开已完成的选择；第8格资格由现有行动调度器即时查询。

公共结果只含卡牌、受益方、位置、奖励和双方能力前后值，无GameState/随机源/牌库。重复命令返回原收据；旧收据不能把旧头条等待状态带回当前界面。混合战争/太空头条同批摘要都保留，展开阅读不执行命令。

人工数据见[数据描述](../../Data/Descriptors/space-event-scenarios.data.json)：第28首到、第29后到、第30能力格、第31终点待裁定；四个都是第2回合fixture，非自然开局。美国入口通过真实苏联OPS行动交棒。所有奖励继续读取正式太空轨道，不复制到练习文件。终点Event停止，OPS仍可用。

规则、视图和地图仍独立。新构建目标为Builds/SpaceEventPlaytest/ColdWarStruggleDemo.exe；旧ResultReaderPlaytest不自动更新。详细原生、构建载荷、源码/Managed哈希、鼠标验收和审查结果见[验证报告](../Testing/space-event-play.validation.json)。完整外部事件干预、保存恢复、联网及完整整局仍未实现。
