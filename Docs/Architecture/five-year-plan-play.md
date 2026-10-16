# 五年计划可玩续接

> description: 在现有真实手牌、对手OPS与头条入口中，接受一次抽样、执行已支持US子事件并回到原根；表现与地图继续独立。

用户目标是持续完善可玩的单窗口测试Demo，允许独立查看双方信息，代码/数据/知识与Git同步；已授权自主决定，无须重复询问普通实现方案。本批属于事件编排接口扩展，按架构方案与分步原生测试执行。基于已实现FiveYearPlanRules.Plan和正式Loader，不新增卡文解释。

## 选择的方案

ActionPracticeDefinition增加可选FiveYearPlan定义并检查同一CardCatalog和处理器互斥。权威由实际已装配InfluenceEvents、DefconVpEvents、TurnOpsEvents推导US子事件能力，不接受客户端指定能力、随机种子或选中牌。所有可能US候选先检查，再调用基础Plan；未知处理器返回明确不支持，草稿全部丢弃。此为有限原型边界，不代表完整102牌或完整联网安全审计。

FiveYearPlanExecution保存不可变父根快照、GameId/回合、父/子ResolutionId和已抽样Selection；ActionPracticeState.ActiveCard继续是父五年计划，Actor继续为根PhasingPlayer。实际子事件源牌另存SelectedCard。GameState/收尾检查该上下文与真实牌区、随机快照、回合、根队列/行动计数绑定。US选择Owner固定为US，但责任仍属于根行动方。

空手/非US/计分路线立即完成父事件；US路线父牌与子牌各占Resolving中的唯一实体，随后选择一个真实处理器：纯影响力复用InfluenceEventFlow、DEFCON/VP复用DefconVpEventService、单回合OPS修正复用Activate。子ResolutionId附属于父ID，避免持续效果幂等标识冲突。子事件不给印刷OPS、不建新行动槽；卡文本身特殊授权OPS的CIA等尚未实现处理器仍拒绝。

纯影响力需要选择时保留Execution和选择状态；响应通过已有固定身份/版本/DecisionId验证，收尾复核选中牌及处理器，不重新抽样。正常完成时先按子牌印刷星号处置实体、清除子上下文，再完成父事件；对手OPS序列只完成Event步骤并按原顺序继续，直接事件完成一个根，头条只推进一项队列。父五年计划无星，正常进入弃牌。

DEFCON/VP子事件触发终局时立即停止，保留父/子Resolving及原根用于审计，零额外行动，责任继承原PhasingPlayer；不处置未完成父根。任一后续收尾或同次后一头条错误仅拒绝本次命令草稿，已接受的前序命令不会回滚。

## 公开结果与界面

Core结果与Application投影只公开已接受选中CardId、Empty/Discarded/Awaiting/Resolved/Interrupted状态、父根及已有子事件公开反馈。绝不投影Selection.Before/Cards、候选列表、牌库顺序或RNG内部状态。重复CommandId返回原抽样收据，子选择重试返回原选择结果。新的CommandId也不能在等待期间再次发动父事件。

CardAction/Headline/InfluenceEvent各结果包装均保留五年计划摘要；政变后接事件的组合包装也保留政变和抽样两者。公开等待快照显示已选牌，切换身份仍能识别这是子事件，只有US有选择资格。Demo使用独立第24人工场景，默认固定可复现随机状态、合法局面和已支持子事件；可重置/重试/切换地图，教程说明每次重试不会重抽。常规未知US事件保持有限原型的失败边界。

## 备选与权衡

直接递归调用普通PlayCard会覆盖父根、从错误玩家手牌取子牌并多推进名额，因此采用独立子上下文后复用纯效果服务。通用任意深度事件解释器暂未有足够运行消费者，本批做一个明确的父子续接结构；后续借牌事件可沿用身份/来源/返回点边界，再按实际需求抽取公共帧类型。

## 验收

原生测试覆盖双方根Actor、空手/计分/Neutral/USSR、US纯影响力等待和无变化、星号/非星、Containment、Duck and Cover正常/核战/VP终局、两种对手顺序、三OPS结束包装、两位置头条与后一头条失败回滚、版本/数值溢出、缺处理器抽前拒绝、同牌区/随机保持、身份/陈旧/重试、隐私投影及UI描述。完成原生/元数据/整批审查、构建、实际窗口鼠标检查、Git推送核对；每项分别记录，不用一种验证替代另一种。
