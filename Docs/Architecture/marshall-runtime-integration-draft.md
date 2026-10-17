# 后续接线草案：马歇尔完整真实事件

依赖已通过的 marshall-foundation、event-fact-foundation 与 event-fact-state。目标按既有 AP1 用户裁定复用影响力选择，并将成功记录与实体牌去向原子发布。

建议有限范围：PracticeInfluenceCatalog 添加可选 EventFacts 定义，要求相同 Map/Cards、覆盖效果与注册实例完全一致；持续提示牌只为明确支持的 card.marshall_plan 开例外，其他持续牌保持拒绝。不能单靠“有事实记录”就允许任意持续事件的纯影响力半实现。

GameState 若注册事实覆盖则要求显式 EventFacts，且 Definition 与目录引用相同；初始新局/人工局面显式已知空，旧数据不自动补空。已有六条复制路径已保留账本。

ActionPracticeRules.CompleteEventAction 与 CompleteFiveYearPlanInfluence：先完成原有根/实体/Owner/Definition验证；等待选择不记事实；真正完成且覆盖时由 ledger.RecordCompleted(raw) 生成候选，再调用 state.WithEventFacts(after)，之后才清 execution、移牌和根推进。全部中间副本在原宿主事务内，任何后续溢出/错误不发布影响力或账本。对手两种顺序与头条共用此完成路径。记录的版本/序号是效果完成位置，最终牌去向可继续推进。

必须补真实入口测试：直接双方打牌；US选0/少于7/7国；等待/错误Owner不记录；对手EventFirst/OpsFirst；头条直接与等待；FiveYearPlan随机抽中Marshall与父子回返；事件结束后普通OPS/回合推进保持；最终移牌溢出完整回滚；同command重放只一次；未注册/缺ledger拒绝。补先前Minor：未注册 malformed effect CardId 必须先检查实例覆盖再构造稳定ID，用 genuine Flow 的回归证明拒绝而不抛出。

完成纯运行时后再明确接 DemoCatalog 两个新局工厂、Bootstrap/Player 载荷、人工练习、公开持续提示及独立构建/实机选择验证。不得把前一批状态基础等同可玩Marshall；NATO规则、Warsaw、历史导入仍另批实现。
