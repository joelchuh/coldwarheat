# 八张影响力事件的真实出牌生命周期

BASE: 687912e12a7e69e52c3edd232d37e327b9c26f77。沿用用户自主持续开发/常规Git授权，本批本人实施，一次最终独立review。

目标：八个已有纯影响力效果从真实手牌通过Event用途进入，直接效果/选择事务复用原服务。发起方可以直接发动任一已支持阵营事件；选择仍归Definition.Beneficiary。此次不接对手事件+OPS顺序、持续效果、中国牌、头条或回合末。原13练习/快速入口/地图适配继续可用。

契约：PracticeInfluenceCatalog绑定Map/CardCatalog与只读CardId→InfluenceEffectDefinition，拒绝重复/无印刷匹配/错误effectId或阵营。ActionPracticeDefinition增加可选InfluenceEvents并守同图/同牌目录；CardActionKind.Event与Stage.ResolvingEvent追加不重排旧枚举。CanPlay只接受自己手中且已注册事件；选牌与选择命令不传规则定义/预算。

PlanEvent纯事务：预备手牌→Resolving、预算0、ResolvingEvent根槽不变；附可信InfluenceEventExecution(Phasing=当前行动者，Policy=NoActiveInterference)，调用既有Begin。AwaitingChoice发布原始DecisionSourceVersion；Resolved经CompleteEventAction清除事件、按印刷星号移出/弃牌、Next只推进一个根槽。拒绝/溢出不发布中间状态。内部纯步骤可各增版本，宿主只提交最终状态；版本不是行动计数。源牌始终唯一。

选择：LocalGameHost的既有EventCommand Resolve/ResolveAllocation先作纯转换，再CompleteEventAction完成牌去向/根槽，全部成功后锁内一次提交。旧人工事件无Practice保持原流程。CompleteEventAction需验证ActiveCard/Resolving/Phasing与事件绑定，待选直接透传。任何最终完成溢出都整条拒绝，不先发布影响力。旧收据重发沿用返回且不重复结算。

反馈：CardActionTransition持原事件转换供公共DTO投影，PublicCardEventResult只给CardId/Stage/Outcome/公开Changes，不带定义、RNG或其他手牌。等待选择由PublicEventView解释，完成选择沿用EventCommandResult。控制器启动新牌清掉过期事件结果；接收事件后刷新并清掉旧选牌/政变意图。

- [x] T1 Core RED/GREEN：注册绑定/地图门禁，固定三牌/零变化/星号，Truman/Decolonization/两个分配/Unrest选择，错误身份/阶段/非法选择、等待时其他行动阻挡，牌守恒、无OPS/RNG/VP/军事/DEFCON副作用、最后槽完成、版本/序号溢出原子拒绝。
- [x] T2 Session/Controller RED/GREEN：立即效果和选择原子发布，双方选择拥有者、旧版本/游戏/跨命令ID、重复确认与新牌后旧收据、公共DTO隐私、控制器实际地图选择与结果。
- [x] T3 Catalog/UI：复用batch1/2加载器与已复制数据，在Scoring之后/QuickStart之前装配。所有Definition复制保留绑定。卡用途增加事件；选择方提示/切换、直接和选择结果显示，地图复用现有选择意图。独立Builds/EventPlaytest，保留旧运行中的Playtest。至少默认Suez/Socialist和US的Nasser可用。
- [ ] T4 完整测试、构建、载荷、实际渲染，Wiki/JSON与worklog，一次最终review，常规commit/ff/push核对远程SHA。鼠标工具若仍不能可靠捕获/激活就停止输入并明确未验证。

Review focus: 对手事件直接发动不等于OPS；Phasing/选择Owner/下一Actor不能混淆；零变化星号仍完成；选择版本不因收尾包装失效；牌去向/影响力/根推进原子失败且重复请求不执行；既有人工事件不被自动清掉；无活动效果前提真实成立；选牌/手牌投影不泄露对手私有信息。
