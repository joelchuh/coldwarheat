# 教皇与团结工会许可链

description：查Pope／Solidarity的Poland固定影响力、成功来源关联的活动许可与实际事件前置门禁。

状态2026-10-06：#68教皇当选、#101团结工会参数Bound；C#待实现。不是#91（那张是Ortega）。[描述](../../Data/Descriptors/solidarity-chain.data.json)→[参数](../../Data/Cards/deluxe-2015/solidarity-chain.parameters.json)、[Schema](../Contracts/solidarity-chain.schema.json)、[案例](../Design/space-solidarity-events.cases.json)、[验证](../Design/space-solidarity-events.validation.json)。

教皇固定Poland，先移除2USSR，再增加1US，成功发生时激活允许Solidarity的永久许可。双方所有步骤与EventFactLedger成功事实、ActiveEffectInstance来源引用、完成收据在一个影响力/许可事务提交；不在移除与添加之间开放动画或玩家等待。现有US影响力保持累加，移除不转成US；不用OPS费用/邻接、控制筛选或地区分配。USSR不足2时SS1-OPEN-02仍RequiresRuling，建议移除现有再加US1并许可尚未获确认，不能借用只适用于其他具名牌的用户短缺解释。

许可使用既有ActiveEffectService，EffectId=effect.john_paul_ii_elected_pope，sourceResolutionId关联成功事件事实（合格结果引用条约数据event_fact_policy）。持续到取消/终局，expiresAt=null，不在TurnEnd到期。基础范围无已核实取消来源；未知取消交互停RequiresRuling。历史事实与当前许可实例职责不同，不再保存“PopeEverPlayed”布尔值。已移出实体牌或只做OPS/太空/强迫弃牌，都不能独自证明许可。

团结工会在实际EventAttempt读取最新活动许可及其可核验的成功来源，满足才Poland增加3US。条件要求“仍生效”，不能只复用北约AnySuccessfulEvent历史门禁。历史完整、无事实且无许可→BlockedPrerequisite；未知历史/许可→RequiresRuling；活动许可无成功来源或成功事实却缺应存在实例→InvalidState。已核验活动实例和来源事实足以判断许可，不要求无关历史齐全；没有规则授权的损坏状态不会默认放行。事实记录不因取消而删除；本批不为尚未设计的取消来源发明裁定。

门禁与CardPlay用途验证分层：本批冻结EventAttempt结果及已明确的对手OPS路径。USSR做OPS且Pope未生效时Solidarity事件BlockedPrerequisite，OPS依§5.2继续，星号不移出；Pope已生效则事件照常US获影响力，OPS前/后由原选择顺序决定。己方直接选择未满足前置的Event用途，以及头条是否允许预选，不在本批擅设禁止/放行，交既有用途策略，未核实来源停止。US own OPS、Space、forcedDiscard没有自动发生此事件，不能解锁。前置检查在真正执行时，不在更早报价缓存；外层已合法Reserved/Headline来源按原编排执行而不另起行动。

复用InfluenceMutation.BuildPlan、ActiveEffectService.Activate与成功事实政策，统一由编排提交，没有模块回调循环。计划固定Owner=US，独立于RootPhasingPlayer；无PendingDecision、随机、VP/DEFCON/军事变更或额外根名额。重复ResolutionId返回同完成收据，冲突同ID拒绝；跨版本/未知影响力干预在变更前停止。身份、存档、真实原子事务、用途验证和牌区服务尚未运行。

依据[英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)p11 #68、p15 #101及[规则2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)§2.2.4–5、§5.2、§6.1.1、§7.3。相关[事件事实](treaty-protection.md)、[持续效果](persistent-effects.md)与[影响力](mid-influence-batch1.md)可单独检索，画风只影响展示。

本批共同主离线检查22数据＋48参考＝70新项，1265旧回归，共1335通过；27 Schema/30数据/19模块、90参数指针。8runtime未运行，38工作工具实际检查另层，独立只读审阅未发现Critical/Important；22数据子集与48参考均独立复算，meta/1265旧回归未独立运行，两项Minor摘要已主侧修正。
