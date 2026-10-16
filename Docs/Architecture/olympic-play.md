# 奥运会正常手牌试玩接线

> description: 将已验证奥运会比赛/抵制/内存续跑接入真实手牌、三种普通OPS和固定身份单机Demo。

用户已授权持续自主开发、Git同步，首版单人测试可查看双方信息。目标是扩充可玩的Demo，不改变可替换地图接口和画风。选定正常行动轮真实手牌切片；头条/特殊嵌套不在本批，OG1-OPEN-01继续保留。现有SpaceEventPlaytest保持可用，新增独立OlympicPlaytest。

## 决策与来源

ActionPracticeDefinition追加可选Olympic定义，要求同Cards/Risk及不重叠处理器；所有生产定义复制入口必须保留它。ActionPracticeState追加OlympicPracticeExecution，绑定原根、回合、牌区、角色、版本与原始国家快照；禁止与Blockade/War/FiveYearPlan/OpponentOps/Headline并存。正常根可Event发动；头条支持集合不添加本牌，特殊借牌不接。

BeginOlympic经既有CanPlay/固定会话/版本/真实手牌门禁，把唯一实体移入Resolving，创建OlympicResolution与对手等待，完成一次状态提交但不占完成槽。CardActionKind追加ParticipateOlympic/BoycottOlympic；玩家不传骰、预算或继续次数，适配器由当前状态取DecisionId。错误身份、陈旧版本、旧根或额外card/target拒绝。首对与接受分支同一提交。平手后只剩内部继续，任何玩家命令都不能改变分支。

## 内部继续与结果

Core的PlanOlympicContinuation只消费当前真实Olympic等待及PCG快照，一次一对；沿用已验证继续令牌。LocalGameHost增加可信AdvanceInternalOlympic(viewer)方法，不加到IGameSession，不接受客户端骰或继续令牌；锁内先完成计划/公共结果投影再赋新状态。DemoController在Unity Update中每帧最多请求一次，仅在Rolling时工作；无UI时宿主调用者负责驱动。并发请求仍由host锁串行化，旧玩家CommandId重发返回原回执，不重掷。后对失败保留此前已提交对和游标。

正常比赛完成：应用VP、即时胜负；未终局按非星号印刷去向弃置并完成一个原根。终局沿用现有试玩的Resolving审计保留，不执行普通收尾。比赛不改地图/军事/太空/OPS效果。OlympicPracticeResult给公开最新一对、累计对数、分支/阶段和完成结果；不携带CardZones、完整GameState、随机内部状态或私有手牌。

## 抵制与普通OPS

先调用共享抵制计划并提交DEFCON/核战。核战时原出牌方输，无OPS。存活进入ChoosingOperations，Sponsor固定为当前根Actor。允许选择Influence/Realignment/Coup或明确Decline；不能再选参赛、不能太空、不消耗额外牌、不产生额外根。

预算通过OpsGeneralRules.QuoteAsIfCard，基值来自Olympic参数，按Sponsor当前普通修正计算；不得用本牌印刷2OPS，也不获得中国牌专属奖励。当前运行时尚无越南区域奖励处理器，不凭参数生成活动效果。首次选择锁模式。影响力使用开始本根时保存的国家快照建立固定可达性，费用用当前控制权，逐点不扩张可达范围。普通投放/调整算法直接复用；它们保留Olympic父状态直到Finish。政变调用CoupAttemptRules，保留普通DEFCON限制、当前敌方影响力、军事信用和原根核战责任；一次全预算。有效操作后Finish放弃余量采用已有EO1工程推导。

Decline只在尚未选用途时可用；已投放/调整后使用原Finish命令。完成时统一清父状态、弃原牌、推进一次根。当前OlympicResolution保存抵制历史，不把它误当OPS执行日志；实际OPS收据/地图/军事由既有权威状态记录，完成结果单独标记OperationsCompleted/Declined。错误操作不撤销已提交的DEFCON下降。

## 宿主与表现

PublicOlympicView投影等待方/可操作性、公开分支/最新骰子、是否等待内部继续。PublicOlympicResult在CardActionResult中保存原回执快照。PendingOperationsOptions识别Olympic并使用事件预算；不影响既有对手事件顺序。Coup查询使用同一选择门禁与预算。控制器独立方法提交上述新增Kind，选择侧只影响固定会话；地图仍经现有合法目标查询和IGameSession提交。

右侧面板优先显示奥运会分支等待与内部掷骰状态；进入OPS后显示普通三用途和明确放弃。选择后复用现有投放/调整面板。结果显示主办加成后的比较、实际赢家、VP；抵制显示DEFCON与剩余行动，核战说明原行动方。内部继续结果可替换最新反馈，重发仍可显示原历史回执。每帧工作仅一对，动画不决定结算。

新增四个明确fixture：双方主办的比赛练习、DEFCON2抵制核战、DEFCON3抵制OPS；通过正式PCG种子与真实牌区初始化，第2回合无头条。不复制卡文全文/卡面，不新增美术。数据descriptor/catalog记录边界，装配复制共享参数/schema/D6与fixture，独立构建校验所有载荷。

## 验证与完成条件

分三任务RED/GREEN：Core真实来源/比赛/续跑；抵制三种OPS/预算/边界；宿主身份/幂等/公共投影/控制器/fixture。最后完整EditMode、元数据、一次fresh整批审查、独立Player构建与载荷SHA核对。工具允许时实际鼠标检查双方回应、抵制核战和投放/调整/政变至少各一路；无法验收的交互如实记录。源码/JSON/Wiki/工作记录一起提交推送；不合并main。

来源与数值沿用Docs/Wiki/olympic-games.md、olympic-contest-foundation.md、olympic-resolution-flow.md。无新裁定；无完整存档/网络/全部卡牌承诺。
