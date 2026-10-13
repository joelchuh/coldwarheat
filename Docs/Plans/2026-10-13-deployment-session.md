# 部署命令与固定身份会话计划

> 执行：executing-plans；沿用持续优化授权。主代理实现，完成后独立审阅。

目标：把已实现的InitialDeploymentFlow接入同一LocalGameHost，不创建第二份权威状态。设计依据：Docs/Wiki/setup.md的部署命令与事务、既有SessionCommand/事件幂等设计。

## 接口与边界

- `DeploymentCommandEnvelope`只含CommandId/GameId/ExpectedVersion/ExecutionId/DecisionId/allocations，不接受预算或自报玩家。收集最多129项后由宿主拒绝超128；不可变副本。
- `IGameSession.QueryDeploymentTargets(executionId,expectedVersion)`按绑定身份返回合法目标与总额；错误返回空目标/零额度。
- `IGameSession.SubmitDeploymentAsync(envelope,token)`持同一锁，复用统一命令编号空间；相同身份同一内容返回原历史收据，跨命令类型/更改payload拒绝冲突。
- `PlayerViewSnapshot.Deployment`仅公开执行ID、阶段序号、当前拥有者、DecisionId和完成状态；不暴露完整定义、GameState或他人手牌。
- 合法提交调用规则Flow，原子交换新状态并记收据；非法提交不改变状态。取消发生在受理之前不记收据；已完成的历史重试不能改为新提交。
- 开始部署仍由可信fixture/未来SetupFlow执行；没有玩家“重发固定布置”入口。

## Task 1：会话与测试

文件：Application/Sessions/DeploymentSessionContracts.cs、LocalGameHost.Deployment.cs；必要修改SessionContracts.cs/LocalGameHost.cs；Tests/EditMode/DeploymentSessionTests.cs。

- [ ] 先写成功两阶段、身份与私有投影、完整事务、重复/冲突/跨类型/同ID双身份、错误游戏/版本/决策/执行ID、取消、129项边界和并发重复测试。
- [ ] 真正运行RED后实现接口；全量EditMode GREEN。
- [ ] 更新模块JSON、Wiki、真实工作记录；独立审阅并按授权提交/推送。

## Review Focus

共享去重缓存不能返回其他类型的null收据；历史收据不得包含后续状态；DecisionId/Version都由权威状态校验；投影不得携带规则定义或未提交草稿；错误输入数组必须在外部枚举边界受限。

## 执行记录

依赖初始部署规则批次审阅完成后再实施；当前文件仅计划，接口尚不可调用。
