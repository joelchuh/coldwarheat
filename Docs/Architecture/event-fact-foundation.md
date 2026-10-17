# 已提交影响力事件事实基础

用户目标：自主持续推进模块化规则与可玩Demo，事实/数据/表现分离。先补马歇尔完整事件所需的可靠成功证据，保留正式宿主原子提交边界。本批架构扩展仅提供纯服务与测试，不注册新牌或改变Player。

## 方案
采用不可变EventFactLedger和明确覆盖集。卡牌所在牌区不能证明事件发生；单个hasMarshall布尔值无法区分无变化成功、未知历史、重复或冲突，均不采用。记录只接受引擎产生的已完成InfluenceEventTransition，不接受UI声明“发生过”。未来其他事件类型另加受限适配器，不设计任意可执行解释器。

## 定义与加载
InfluenceEventFactDefinition绑定同一MapDefinition、CardCatalog、明确受跟踪的InfluenceEffectDefinition实例、ruleset/version及参数摘要。每个实例必须匹配正式事件CardId/EffectId/受益方，无重复且非空；只有覆盖集里的卡牌可被本适配器记录。
EventFactDefinitionLoader.Load(treatyJson,schema,map,cards,effects)消费共享event_fact_policy；只支持成功ResolvedChanged/ResolvedNoChange、四种非成功结果、未知历史RequiresRuling及Verified R2015来源。严格schema/profile/来源，数值规则不复制。与马歇尔影响力Loader组合，后续其他定义可显式加入覆盖集。

## 记录候选
EventFactLedger.Create(definition,gameId,historyKnown)必须显式说明过去的受跟踪事件历史是否已确认。true仅适用于已知新局/明确人工局面，不可把旧存档缺字段默认为已知空。
RecordCompleted(transition)验证Accepted、非空NextState、同game/map/真实cards catalog、无部署、非终局、已完成InfluenceEventExecution、确切受跟踪definition实例、NoActiveInterference、成功Outcome及ResolutionSequence等于状态末序号，版本/序号均正。已等待或失败不产事实。完成副本包含事件ID/牌/效果/受益方与PhasingPlayer/outcome/版本/序号。
相同resolutionId和所有证据相同为AlreadyApplied，返回同ledger；同ID证据不同为CommandIdConflict。新ID版本或序号不前进为StaleContext，旧ledger保持不变。记录自身不递增GameState、不消费随机数或移牌，它附着于既有完成序号；后续宿主把影响力、ledger和牌去向同一事务发布。

## 查询与范围
HasSuccessfulEvent(card)是EvaluateAny单元素简便入口。EvaluateAny先验证非空、无重复、均为Catalog事件牌。只要有已记录成功即Satisfied；无成功且所有目标在已知覆盖集内才NotSatisfied；历史未知或目标尚未覆盖则RequiresRuling。无效查询Rejected，不能用合法成功掩盖无效输入。
集合与事实只读、稳定排序；不得把unknown当false。记录取消/抑制/待选择/普通OPS/牌区变化均不由此适配器发生。NATO、GameState属性及跨回合拷贝、真实手牌宿主、存档/网络与UI属于下一批；本服务不单独声称它们已实现。

## 验收
正式策略/绑定与畸形来源；changed/nochange两种完成；等待/错误身份/版本/序号/不同定义/错误game拒绝；精确重试与冲突、乱序、新不可变追加；已知空/未知/部分覆盖ANY查询；无效列表及修改只读集合拒绝。全量EditMode、元数据和一次只读整分支审查。
