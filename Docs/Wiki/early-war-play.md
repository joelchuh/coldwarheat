# 早期战争可玩接线

> description: 从真实手牌发起三张战争、印巴等待目标以及后续OPS/头条/地图进度；纯规则Task1已通过，Player接线继续。

先读[模块JSON](../Descriptors/early-war-play.module.json)再按entrypoints定位函数。规则算术和共享数据见[战争基础](early-war-foundation.md)，本模块只负责编排和提交边界。

Task1已实现ActionPracticeDefinition.Wars、WarDecision/CanResolveWar、直接BeginWar/CompleteWar。固定两牌直接结算；印巴目标待选不消费随机/军事，不推进根。待选绑定game/version、turn、实体Cards、Random和原国家局面。确认后国家/军事/VP/random统一提交；终局不完成根，非终局按星号移出或弃置。纯Core测试3016全过（新增27项），尚未注册Demo，不能宣称新版可玩。

流程：事件意图→固定目标或待选国家→WarEventService→PracticeRisk.After→国家/随机统一Commit→终局或实体牌处置/Next。对手OPS/头条和会话公开DTO由后续任务继续，不把基础WarEventState传给客户端。

证据见[验证报告](../Testing/early-war-play.validation.json)，开发路径见[计划](../Plans/2026-10-17-early-war-play.md)。
