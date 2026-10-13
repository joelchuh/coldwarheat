# 额度选择会话

日期：2026-10-13。基线cfbab4c；feature/allocation-event-sessions。

新增EventCommandEnvelope.Allocate、ResolveAllocation种类及只读数量载荷，宿主仍从固定身份Session确定操作者。容量和有效总量经Query公开，客户端不能上报规则预算。Core共用决策门禁，先验证全部分配再一次完成，旧国家集合流程保留。

幂等比较纳入数量载荷及顺序；同ID精确重发返回原收据，修改金额/国家/类型等冲突。与OPS共用身份收据命名空间；规则拒绝保留收据，格式错误与提交前取消不保留。等待时OPS被阻挡，零容量开始即完成。

真实验证：新增21项在接入前全部失败，原235项通过；接入后256/256真实Unity EditMode通过。证据摘要及SHA见Docs/Testing/allocation-event-sessions.validation.json，完整本地XML位于TestResults/UnityRuns/allocation-session-*。独立审阅通过，无Critical/Important/Minor；只读核验真实XML及SHA，未重复执行Unity。

本批不构建播放器，数量草稿及新场景下一批接入；完整出牌/牌区、回合、保存/回放和联网仍未实现。描述文件、Wiki与目录同步，不新增工作流脚本。

Git：本记录随代码提交，常规快进与推送按工具实际核验记录；不强制覆盖远程。
