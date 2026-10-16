# 五年计划随机弃牌基础

> description: 确定性单张手牌抽样、唯一牌区与US子事件交接；本基础不执行嵌套事件。

用户目标继续补齐可玩卡牌，保持纯C#、固定身份、数据共享、私有信息边界和可替换地图。既有规则设计见Docs/Wiki/hand-events-batch1.md；正式参数effects.deluxe2015.hand_events_batch1 /parameters/five_year_plan已核对。本批不新增规则解释。

采用不可变计划：定义绑定实际印刷CardCatalog；调用者已经把五年计划放入唯一Resolving区。服务从苏联普通Hand按稳定CardId排序，在独立PCG快照上做一次NextInt；空手不抽样，单候选也调用一次。中国牌、预留头条、原牌、牌库和对方手牌都不是候选。计分牌可以被抽到，普通弃置不执行计分。

选择US事件之前，先检查所有可能的US候选都有调用方声明支持的子事件处理器；缺失则在抽样前拒绝整份计划。支持集合是权威装配输入，不由客户端传入。不允许只根据抽中牌临时决定是否支持，也不能重抽。收到US牌只安排事件交接，不授予印刷OPS、不新建根、不改变根行动方。

返回FiveYearPlanSelection包含Before/Cards、RandomBefore/RandomAfter、SelectedCard和Route（EmptyHand/Discarded/AwaitingUsEvent）。非US事件和计分牌直接进入DiscardPile；US事件进入Resolving，父五年计划仍在其中；两张实体各只出现一次。服务不提前处理原牌去向，也不宣称子牌已经发生。后续执行层必须在接受该计划后记录固定返回点、根责任和公共已选牌事实；重试使用原收据，不再次调用本服务。

此选择计划属于权威内部：Before含完整隐藏牌区，不能发送给客户端；后续会话仅投影已接受的SelectedCard/公开结果。保留Before实际引用和RandomBefore用于事务绑定，不在本基础伪造存档、命令幂等或完整事件栈。

数据装载沿用同一JSON/Schema。抽取内部HandEventContract负责共同profile、来源集合和Verified引用验证，Blockade与FiveYearPlan各自验证分支语义，避免两份来源规则漂移。Blockade现有测试必须全回归。

备选评估：直接递归调用现有ActionPractice事件会覆盖父根和牌区，暂不采用；先随机抽到再看处理器会使结果依赖实现缺口，禁止。当前方案先把确定性和牌区边界独立验证，再实现可暂停的子事件续接。

验收：空手/单张/计分/中立/苏联/美国、稳定顺序、PCG快照不变、溢出失败、父牌位置、头条和China排除、缺US能力抽前拒绝、错误catalog、数据语义/来源变异；全原生测试、元数据、独立只读审查、Git提交推送。此批不新增Player入口，封锁Player保持可用。
