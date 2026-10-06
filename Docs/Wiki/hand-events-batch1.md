# 封锁与五年计划

description：查询私有弃牌阈值、权威随机弃牌、US事件子帧及公开信息/牌区/恢复边界。

状态2026-10-06：#10/#5参数Bound；累计35 Bound / 67 Unresolved，102处理器planned，C#/Unity未实现。[JSON描述](../../Data/Descriptors/hand-events-batch1.data.json)→[参数](../../Data/Cards/deluxe-2015/hand-events-batch1.parameters.json)、[Schema](../Contracts/hand-events-batch1.schema.json)、[案例](../Design/hand-events-batch1.cases.json)、[报告](../Design/hand-events-batch1.validation.json)；[模块描述](../Descriptors/hand-events.module.json)。

## 封锁：美国选择，弃牌或承受移除

Owner固定US，即便USSR根行动触发也由美国回应。只查美国此刻普通Hand里的Event牌，排除China、Scoring、HeadlineReserved、当前InResolution与其他玩家手牌。阈值≥3从本事件参数读，实际OPS由共享OpsQuote(Actor=US, Purpose=OpsThreshold)读取遏制/清洗等修正，不能比较印刷值或用苏联修正、地区奖励。未知相关修正停为RequiresRuling。

美国可弃一张合格牌，也可Decline；无候选直接执行fallback。选中牌Hand→DiscardPile，不触发其事件，星号也只是弃入公共弃牌堆。Decline只移除西德全部US，苏联不变；零US影响力仍完成无变化，不阻止卡文星号移出。错误选牌/身份/旧Revision不自动转fallback，不泄露候选或部分更改。弃牌与fallback均无新行动名额、OPS、军事、DEFCON或随机。

## 五年计划：随机弃牌与明确的事件例外

权威从USSR普通Hand均匀选一张，包含Scoring，排除China及各非Hand区域。先以稳定CardId排序，再调用可持久化Random.NextInt(0,count)；客户端不能传索引、种子覆盖或指定牌。空Hand完成ResolvedNoChange且不抽随机，单候选消耗一次逻辑抽样；后两条是HC1工程/无对象推导，并非专门FAQ。随机收据保存候选状态摘要、结果与RNG游标；私有完整候选不进入玩家公开日志。

USSR/Neutral/Scoring选牌直接Hand→DiscardPile，没有事件、OPS或计分。US关联选牌立即触发US控制事件，显式Hand→InResolution，Reason为RandomDiscardTriggeredEvent；卡文的discard原因不意味着一张实体同时留在DiscardPile。此路由是避免重复位置的HC1表示约定。子帧返回点为父五年计划，继承根PhasingPlayer；不是US的新行动，也不等同US实际打出此牌，不生成新CardUse钩子，不自动发放被弃牌印刷OPS；CIA Created、拆墙等子事件自身卡文行动授权仍正常执行。

事件开始重新查询禁止/前置；被禁事件记录Blocked/Suppressed后弃置，成功事件按印刷星号与正常EventOutcome移出/弃置。选牌路线与RNG收据/子帧位置一同提交并可恢复；全规则包与潜在子处理器预检必须先于随机抽样，缺代码为Faulted，不准抽到未知牌后重抽或默默跳过。子事件终局截断父继续步骤，不改变根核战责任。选牌事实在接受后两方可见，未选手牌保持私有。

## 共享对象与边界

DiscardDecision保存Owner/FrameId/Revision和私有候选快照；回答只SelectedCardId或Decline。HandEventService只返回计划，RuleEngine统一提交，TurnFlow管理ResolutionFrame；CardDisposition沿用既有策略。相同CommandId返回收据，不再抽样/弃置；旧响应不改变牌区/RNG。存档保留抽样已完成标记、选中实体唯一位置及子继续点，显示动画结束不能驱动二次执行。

来源：英文卡图p2 #5/p3 #10；R2015 §5.4弃牌不触发，五年计划卡文按§5.5覆盖US关联事件；§7.4.1/7.4.2阈值读取实际OPS；§9.5/9.8排除中国；FAQv5 PDF p3 #5确认可随机弃计分、p19弃牌堆公开。私有候选、唯一牌区/排序/空手牌/随机游标为HC1工程或文本推导。离线路由参考不代表逐个US事件已执行，真实存档/鉴权/回放/隐藏信息测试待实现。

表现层仅渲染Owner可见候选及公开选牌，不决定随机、阈值或事件归属；画风和手牌动画可以替换。去斯大林化重叠、Flower处罚时机/UN边界仍待裁定，本批不采纳未收到的用户答复。下一批台湾决议与穿梭外交，建立计分时动态战场及一次性修正接口。

主离线验证28数据＋37固定参考＝65新项，992旧回归，共1057通过；21 Schema/25数据描述/18模块/67指针，12runtime未运行。独立审阅两项Important（子事件授权与永久随机收据）已修复，复核合入Yes；135 JSON/67指针/18模块无环及8摘要核对，未独立复跑1057。

HC1-RUNTIME恢复边界：提交前计划若基于旧候选状态则拒绝且不耗随机；已接受CommandId收据永久保存。子事件后续改手牌不能废弃该收据，也不能重试五年计划重抽。
