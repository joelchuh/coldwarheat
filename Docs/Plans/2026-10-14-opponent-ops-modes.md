# 对手事件与可选 OPS 模式

沿用执行计划技能：单一实现者、行为TDD、一次全分支独立审查；用户已授权持续开发/普通Git。前批影响力专用命令保持兼容。

目标：已注册的对手事件牌先选事件/OPS顺序，到OPS步骤再选影响力、政变或政局调整。事件改变DEFCON/影响力后重新查询合法目标与报价，保持一张实体牌与一个根行动。依据GMT2015 §5.2、§6及既有纯服务。

方案：OpponentOpsSequence增加可空Mode。旧Create默认Influence；新通用顺序入口以null开始，CurrentStep=Operations时进入ChoosingOperations。SelectMode只允许当前Operations且尚未选择，Advance操作步前必须已选Mode，禁止中途混用。新命令OpponentEventFirst/OpponentOpsFirst携带手牌，新UseOpponentInfluence/UseOpponentRealignment无牌/无目标，UseOpponentCoup无牌/有目标。保持原枚举序号。

不另建政变权威等待阶段：ChoosingOperations中预览政变目标属于客户端意图，确认合法目标才执行整次政变。可随时改选另一OPS模式，不会因零合法目标锁死。QueryCoupTargets支持该阶段准确的ActiveCard，禁止换另一张手牌。其他阶段原接口保持。

T1：先新增序列与规则行为测试及最小API脚手架，运行RED；实现通用开始/模式选择/实时报价、调整完成接事件、政变封装并保留两类结果；GREEN。
- 两顺序×三模式；Nasser实际顺序差异；Suez等待受益方，根Actor保持；Containment实际行动方报价。
- Duck先事件降低DEFCON后欧洲目标禁止，选调整/投放仍可；先OPS政变与之后事件各可能核战/VP终局，必须截断余步且责任为根Actor。
- 政变军事/RNG/影响力与事件状态合并，调整每次两骰/无军事变化；最后星号只移出一次。
- 已接受调整/投放前缀保留，收尾或政变+后续事件溢出不提交当前草稿/RNG/军事；未知事件继续拒绝。

T2：先Host/Controller测试RED，再接公开PendingOperations选项、Mode与通用卡牌用途。QueryCardActions仍返回GameId/Version，命令固定身份/版本/幂等校验不变。Controller用权威ActiveCard查询对手政变目标，缓存按版本/身份失效；切换模式只影响本地预览，确认后不得再选。
- Host同ID重发、内容冲突、旧版本/错误身份/错误ActiveCard；命令结果能同时携带Coup及随后的事件结果。
- Controller默认真实Duck顺序→OPS模式→地图目标→结算；两方事件选择→回原根身份；保留旧普通政变路径。

T3：UI先顺序后模式，显示剩余事件提醒与风险。消除结束投放/调整按钮在有后续事件时误称已弃牌。结果为同命令多收据显示简短组合摘要。独立OpponentOpsPlaytest构建，同38份权威数据。更新模块JSON（非每函数一个）、Wiki/flow/工作记录/报告/README/AGENTS；元数据校验、全量测试、构建与载荷哈希、实际渲染，鼠标单列。

T4：一次最强独立全分支审查，Critical/Important单轮TDD修复，Minor记账；ff合并/正常push并核对远程。

边界：不接未实现事件/前置豁免、中国牌、地区OPS、事件内授予OPS、嵌套/头条/回合末/存档/网络。终局仍沿用根未推进/Resolving保留审计策略。地图接口/规则数据源不变。测试只为跨层规则和恢复边界，不给文案/枚举逐字镜像测试。

Review Focus：不在事件前锁定后续OPS类型；ChoosingOperations期间不能通过普通Influence命令偷投；无合法政变目标仍可改模式；Coup+即时/选择事件既保留两个结果又原子发布；终局截断、后续事件溢出回滚当前Coup但保留此前行动；旧枚举/旧Influence专用API兼容；查询不会泄露牌库/RNG。

进度：T1/T2规则与Host/Controller完成，最终1872/1872通过；T3构建/报告及两条鼠标路线完成；T4审查1项Important已修复，最终1874/1874；真实等待选择路线通过，正在常规同步。
