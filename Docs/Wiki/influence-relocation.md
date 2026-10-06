# 影响力迁移

description：查询De-Stalinization的苏联选择、来源/目的分配、总量守恒与同国移出移入待裁定。

状态2026-10-06：#33已知参数Bound；全项目33 Bound / 69 Unresolved，102处理器planned，无C#/Unity。入口：[JSON描述](../../Data/Descriptors/influence-relocation.data.json) → [参数](../../Data/Cards/deluxe-2015/influence-relocation.parameters.json)、[Schema](../Contracts/influence-relocation.schema.json)、[案例](../Design/influence-relocation.cases.json)、[验证](../Design/influence-relocation.validation.json)；[影响力模块](../Descriptors/influence-events.module.json)。

苏联选择自己的最多4点迁移至非美国控制国家，每个目的国新加最多2。来源可任意地区有苏联影响力的国家；来源可以是US控制国，不能用US影响力支付。目的国不需已有苏联影响力或普通OPS邻接，费用不是敌控双倍，不能移入超级大国/未启用中国内战。Owner固定USSR；美国OPS触发此牌仍由苏联选择，根行动者不改变。

## 复用原语与事务

RelocationDecision包含DecisionId/Revision/Owner、来源候选/可移量、目的候选和总上限，数值从参数/地图读。响应仅SourceAllocations与DestinationAllocations，服务器从认证身份确认Owner。各条CountryId唯一且Amount正整数；总来源0–4且不超过自己现有量；总目的必须等于来源且每国≤2。两个空数组表示零迁移（up to推导DS1），完成事件但ResolvedNoChange，星号仍可移出。

在工作副本校验完整方案，先计划移除再计划增加，最后作为单个守恒批次提交；UI分两步编辑只是草稿，没有中途移出后等待玩家永久丢失影响力的状态。合计苏联影响力与每国美国影响力不变，DEFCON/VP/军事/随机不变。任一错误本次全方案不提交；不能接受只含来源而缺目的的命令。重读局面后检查控制与可移量，旧Revision/越权不猜默认。

本批接受无来源/目的重叠的完整方案；其目的控制在移除前后相同，所以没有控制快照歧义。**DS1-OVERLAP待裁定：** 同国既移出又移入，或借移出影响该目的国控制资格，未找到专门覆盖的官方答案；整个依赖方案RequiresRuling，不把它说成官方禁止，也不擅自净额抵消来规避新放每国2上限。此待裁定不阻塞明确的不同国家迁移。

## 恢复、表现与依据

PlanRelocation拟定纯Core不可变批次，由RuleEngine一次提交；客户端不能指定效果Owner/最大量/忽略控制标志。存档保存源ResolutionId与决策/批次ID，重复同请求只返回已完成结果，同ID异内容停止。合法候选由权威地图提供，画面只渲染编号/箭头/草稿；更换2D/3D或写实素材不改变迁移算法。

英文卡图p6 #33、R2015 §2.1.7/5.2/6.1.1、FAQv5 PDF p7 #33明确无需OPS邻接；见source_catalog。完整分配事务与零迁移是DS1工程/文本推导，重叠仍待裁定。离线参考只证明数据/算术与拒绝不变，真实身份、事务、持久化和跨事件影响力限制未运行；未知相关状态必须停止。

下一步Blockade私有手牌阈值/弃置选择、Five-Year Plan权威随机弃牌与US事件子帧。两牌尚未绑定，不把本迁移服务兼作手牌算法。

离线验证：15结构基线/负例、28固定迁移参考，加949旧回归，共992通过。20 Schema、24数据描述、17模块和64参数指针已核对；8项runtime未执行。独立只读审阅无Critical/Important/Minor；独立复核28迁移参考、129 JSON/64指针/17模块无环及6摘要，未复跑15 Schema负例/20元校验/949旧回归。
