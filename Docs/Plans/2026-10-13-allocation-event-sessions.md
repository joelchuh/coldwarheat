# 额度选择会话接入

> 执行：executing-plans，沿用持续编码授权；完成后独立代码审阅。

**目标：** 将已有两牌额度纯规划接入同一LocalGameHost，让固定身份玩家在版本化决策上提交完整数量分配。

**依据：** Docs/Wiki/early-influence-batch2.md 的事务/幂等契约，以及已实现influence-event-sessions。

**接口：** EventCommandKind新增ResolveAllocation，EventCommandEnvelope.Allocate具名工厂复制只读CountryId/Amount列表；旧构造器继续服务Begin/Resolve，无法混入分配。EventTargetsResult增加Kind/RequiredAmount/Capacities。Core增加ResolveAllocation，复用Resolve的身份/实例/版本/完成检查与同一Plan。玩家命令不含Owner、规则预算/上限、受益方、definition或policy。

**幂等：** 继续使用同一(side,CommandId)收据表。精确相同的分配数组重发返回原对象；改金额、国家、顺序、类型或版本均冲突。不同命令的分配可采用不同顺序，Core输出仍规范排序。格式错误与提交前取消不保存收据；格式正确的规则拒绝保存收据。

**边界：** 仍是可信预装公开事件的人工练习会话；不创建正式出牌/牌区、网络或持久化。UI和数据场景接入属于后续批次。本次不构建播放器、不做鼠标验收。

- [x] RED：新增AllocationSessionTests，验证公开容量只供Owner查询、对手事件选择方与PhasingPlayer分离、三国分配一次完成、零容量开始即完成、错误身份/决策/版本/游戏/类型、空/超长格式、原子非法额度、成功和拒绝幂等、取消后同ID可用、与OPS/旧选择共用收据、等待时OPS阻挡。
- [x] GREEN：宿主在格式校验后沿同一锁内事务调用ResolveAllocation；结果投影不含手牌/规则定义。保持旧235项回归，新增用例必须真实失败后通过。
- [x] 文档：同步会话/规划模块与数据描述、Wiki、目录、计划JSON、验证记录、工作日志；清楚区分UI尚未接入。
- [x] 独立审阅无问题；提交与main远程快进按本次工具核验结果记录。

手算验证：法英以各US2，US开始→v1等待USSR；USSR提交法2/英1/以1→v2、完成序号5，法0/英1/以1，OPS4保持；正确重发只返回原v2收据，后续OPS产生v3不回滚。修复不替换原有国家集合接口。

验证入口：Tools/Scripts/run-unity-editmode.ps1，唯一RunId为allocation-session-red/green及必要修复后缀。
