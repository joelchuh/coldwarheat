# DEFCON事件与政变干预（2026-10-06）

基线1ce627516a9bea5a712168e8324c1b964ac80592；战争牌持续/取消已普通推送且线上main/HEAD/origin一致、工作区clean。Flower处罚时机/UN交互仍待用户裁定，本批不依赖这些待裁定操作。遵守AGENTS §§22–23，继续用户授权的规则设计，没有C#/Unity实现。

本批四牌Cuban Missile Crisis #40、Nuclear Subs #41、SALT Negotiations #43、ABM Treaty #57参数Bound。[Wiki](../Wiki/defcon-events-batch1.md)→[JSON数据描述](../../Data/Descriptors/defcon-events-batch1.data.json)→参数/Schema/案例，新增[CoupIntervention模块](../Descriptors/coup-interventions.module.json)，其余复用既有影响力、持续效果、DEFCON、军事与事件OPS。UI主题独立，策略以稳定原因/Actor/牌区而非文本名称查询。

来源目视英文卡图p7/p9、R2015 §6.3/7.4.3/8.1/8.2、FAQv5 PDF p8–9 #40/#41、p12 #57、p14 #67。危机实际Actor接受尝试即输，任意稳定边界自己的单国2影响力解除，不改变DEFCON；核潜艇按战场政变原因免降级，不免事件/地理/危机。SALT双方骰-1，不减OPS或军事，公共Discard可选取回非计分不触发；ABM视同4OPS普通授权，非free coup，不能太空。

主临时验证28数据+65固定参考=93新项，856旧业务回归，共949通过；15runtime未执行、C#/Unity测试0。[报告](../Design/defcon-events-batch1.validation.json)记录12摘要、19Schema/23数据/17模块。首跑案例误用了不存在的country.guinea，正式地图拒绝后改为真实非战场country.cameroon并完整重跑，不为夹具增加虚构国家或改变正式地图。参考模型只证明干预/轨道/牌区/预算投影，不执行完整政变随机/影响力/军事，不证明真实鉴权、唯一牌区、存档与抢占。

独立只读审阅没有重要规则阻塞：独立PS拒绝重复JSON键，核对124 JSON、目录、63数据指针、17模块无环、12摘要及案例/绑定库存。采纳P3预算来源措辞，明确SourceCardPrintedOps与VerifiedEventLiteral。此前代理jsonschema权限限制已知，本次不重复导入，不冒称独立重跑949或Schema meta验证。提交前刷新链接摘要、精确差异与候选/磁盘/暂存一致；普通push后核对线上main。日志不写自身未来SHA；后续基线及聊天报告记录实际提交事实。

全项目32 Bound / 70 Unresolved，102处理器planned；3项回合效果重复激活仍保留RequiresRuling。下一批影响力迁移与隐藏手牌选择（De-Stalinization、Blockade、Five Year Plan），先定义移除/再放置与公开/私有/随机选择边界。
