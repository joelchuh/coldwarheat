# Scientist太空事件接入试玩

继续把已验证的单格基础接到真实游戏流程，供用户单人查看双方信息试玩。规则独立于Unity，地图接口不变；沿用当前feature分支开发、固定身份会话与可展开结果页。用户要求自主继续，当前会话自审方案并内联实施。

## 接线选择

采用现有即时事件路径加一个独立SpaceEvent处理器，不伪装成普通Space尝试，不新增任意MoveSpace玩家命令。玩家仍只提交已有Event用途和CardId；受益方是已验证的真实出牌者（中立牌）。正式Definition作为ActionPracticeDefinition的可选SpaceEvent注册，必须与Cards/Space对象共享，要求Risk存在且不与其它处理器重叠。所有DemoCatalog复制保留该注册。

处理器生成带回合的来源ID，先调用基础计划，拒绝重复计划与终点新事件；用ClosedVpBatchEvidence即时判断VP胜利，再原子发布新Space、VP/Risk、真实牌去向和行动推进。保持原RandomSnapshot、军事行动、DEFCON、国家影响力与原有普通次数。正常完成按星号移出；终局沿现有试玩审计约定留在Resolving、不推进根行动和后续头条。

SS1-OPEN-01在普通Event选项返回EventRequiresRuling。头条选择阶段按该实体牌实际持牌方位置过滤这张牌；过滤结果只在权威状态保留，玩家仍只见自己的候选，不暴露对方手牌。运行时再次校验，不能用自造命令绕过。未核实终点不会标事件已发生。普通OPS仍可用，普通Space继续按有效OPS/次数/终点检查。

## 头条与公共结果

静态支持表登记Scientist。正常头条使用真实PlayedBy，无普通根动作消耗；Defectors取消仍先于事件，取消不推进轨道、不发奖励、牌进入弃牌；VP终局停止后续牌。已有选择组不因为刚获得太空4能力重开，下一个选择组再查询。

CardActionTransition附带SpaceEventPlan；Application只投影PublicSpaceEventResult的Card、Beneficiary、From/To、FirstArrival、Award、NetVp、双方能力前后变化。DTO不含权威状态、CardCatalog、牌堆或随机流。直接用牌与PublicHeadlineStep都传递摘要；现有头条多结果集合追加SpaceEvent，不覆盖同一收据里的战争/FiveYearPlan结果。版本或游戏变化后旧结果不当作当前待办，重试不再次执行。

表现层新增DemoView.SpaceEvent.cs，显示事件名、受益方、轨道前后、先到/后到与VP、能力变化、不消耗普通次数。继续使用原有展开结果页，显示代码不裁决规则。不会为了事件另造地图或绑定画风。

## 练习场景与构建

单独Data/Demo/space-event-scenarios.json记录四个人工第2回合练习：苏联首到、美国后到、苏联从1到2取得第二次尝试资格、苏联已经8的新事件停止。场景载入器读取位置/手牌/受益方/预备动作等fixture数据，所有奖励仍取正式轨道。美国场景通过一个真实苏联OPS动作将行动交给美国，不直接改CurrentPlayer。正式开局保持原数据。

WithSpaceEvents后接WithSpaceEventScenarios；编辑器和Player读取相同参数/schema/fixture。新BuildSpaceEventPlaytest输出Builds/SpaceEventPlaytest/ColdWarStruggleDemo.exe，保留旧ResultReaderPlaytest。构建检查全部载荷字节及Managed DLL哈希；实机分别验证直接首到、后到、能力变化和终点按钮/局面，截图与日志保存TestResults。若UI工具不可用，明确保留未验收项，不把原生测试等同鼠标验收。

## 验证边界

真实规则/会话测试覆盖双方和先后奖励、已用次数、普通OPS/Space区别、星号去向、VP终局、旧请求/跨回合ID、终点零修改、头条双方/取消/终局/私有候选以及混合摘要。注册复制、数据错误、载荷一致性单独验证。保持其它未实现事件、未知外部干预、持久化与联网边界；本批不是完整卡牌集。
