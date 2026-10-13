# 固定身份部署会话

基线ebc9db9。将部署规则接入既有LocalGameHost，不引入另一份权威状态。新命令只含执行/决策ID和最多128项完整分配；玩家身份绑定在会话，预算和合法目标由Core计算。

- OPS/事件/部署统一身份收据空间，改payload或命令类型冲突；相同内容重发返回原历史结果。
- 原子锁内规划并交换新状态，拒绝/取消不改局面；格式错误或受理前取消不保留收据。
- 新PublicDeploymentView只含公共阶段字段，既有手牌投影和测试观察保持。
- 查询失败返回空目标和零额度，Controller/UI尚待接入。

RED 541总/516通过/25失败，原因是未接入投影/命令入口；实现后GREEN 541/541零跳过。测试覆盖两方流程、不可变输入/历史结果、错误游戏/版本/决策/身份、跨类型冲突、取消与并发重复。XML/SHA见Docs/Testing/deployment-session.validation.json。

本批不重新构建播放器，不声称正式发牌或鼠标验收。下一批接入同一IMapView的部署草稿和明确fixture练习。

独立审阅review_deployment_session通过，无未解决Critical/Important/Minor；计划陈旧的“未实现”表述已修正。分别验证本地提交和远程main后报告同步。
