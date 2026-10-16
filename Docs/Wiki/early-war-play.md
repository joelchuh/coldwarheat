# 早期战争可玩接线

> description: 三张早期战争已接真实手牌、OPS、头条、会话与可替换地图；27场景Player已构建并验收四种战争结果，3081测试、58载荷、Windows实机与独立审查全部通过。

先读[模块JSON](../Descriptors/early-war-play.module.json)再按entrypoints定位函数。规则算术和共享数据见[战争基础](early-war-foundation.md)，本模块只负责编排和提交边界。

当前接口包括ActionPracticeDefinition.Wars、WarDecision/CanResolveWar、直接BeginWar/CompleteWar。固定两牌直接结算；印巴目标待选不消费随机/军事，不推进根。待选绑定game/version、turn、实体Cards、Random和原国家局面。确认后国家/军事/VP/random统一提交；终局不完成根，非终局按星号移出或弃置。对手OPS/头条、公开DTO、地图确认和三个人工场景均已接线。完整原生3081通过；最终构建源20273fe，58载荷字节一致；初版四种战争胜败实机验收，最终版朝鲜路径复查通过。成品在Builds/WarPlaytest/ColdWarStruggleDemo.exe。

流程：事件意图→固定目标或待选国家→WarEventService→PracticeRisk.After→国家/随机统一Commit→终局或实体牌处置/Next。对手OPS/头条继续原根；公开DTO不把基础WarEventState传给客户端。以下Task记录是开发历史，以本段和验证报告最新状态为准。

证据见[验证报告](../Testing/early-war-play.validation.json)，开发路径见[计划](../Plans/2026-10-17-early-war-play.md)。

Task2完成：对手事件先后OPS、政变两回执、核战停止、头条等待/继续/取消/终局及跨回合同槽信用均通过。新增18项，首次internal夹具编译失败已用真实回合流程替换，不计为RED；有效RED16项均未支持，GREEN3034/3034、0跳过、exit0，XML SHA fc10ed1f414f5b5db8b58b5680445863c103189207245250be7f5b738a923fc4。会话/Player接线继续。

Task3A：会话公开DTO、重发/错身份/冲突、地图替换及中文战争反馈通过。RED3052/3034/18，GREEN3052/3052、0跳过、exit0、SHA9f034e36d3935f13576215b85e7d0ce6554d101d2c06409685bb0cce34beba02。公开头条缓存统一按最后一个有摘要的步骤选择，并保留原五年计划接口。后续补战争后继续待选时的事件面板显示、三人工场景和Player，不把这一阶段声明成构建完成。

Task3B：三个共享JSON人工场景、严格加载、复制定义保留Wars及Editor/Player载荷接线完成；事件面板实际调用ReadEventFeedback，战争后继续待选也可读结果。RED3077/3052/25（24未实现、1缺显示），GREEN3077/3077、0跳过、exit0、SHA da4708181351267af4909253df8f72a2d6ca4b4d2cf9b907e63a99e675e22b00。原场景24个保留，加3个共27。种子42/stream54首骰4，朝鲜成功、阿以两修正失败、印度成功/巴基斯坦一修正失败均由真实命令确认。Windows构建和人工验收继续。

审查修复：CurrentHeadlineFeedback保留同一回执中所有战争/五年计划的只读公开摘要，ReadHeadlineFeedback按执行顺序组合给行动和事件面板；旧回执不能覆盖当前版本。四项回归RED3081/3077/4→GREEN3081全过，SHA54f721476ae7580d1b9e5026245cc2951e93ce808f8d7188843ec360761c6ec0。后续版本变化时隐藏旧摘要；这不是跨回合历史日志。
