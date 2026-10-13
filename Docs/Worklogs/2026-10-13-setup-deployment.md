# 初始影响力部署规则子集

延续持续优化授权，按既有setup设计实现固定布置和苏先美后完整分配。数值仍从setup.json读取；自由范围查询地图子区，未复制国家白名单。

- 定义、阶段与结果不可变；准备状态须零影响力、零OPS且无事件/已有部署。
- Begin只应用苏联固定布置；Resolve完整校验后同事务应用本方分配和下一方固定布置。
- 完成只标DeploymentComplete；不发牌、不建中国牌、不进入TurnStart。测试状态显式fixture。
- 复用InfluenceMutation；保留手牌、回合、GameId和OPS；连续事件序号，失败无部分提交。
- 直接规则和LocalGameHost都拒绝部署期间的旧OPS/事件入口。

测试：首次RED因NUnit负向集合断言误选字符串重载而编译失败，改用Has.No.Member；RED2为506总/467通过/39预期未实现失败。首轮GREEN 506/506。追加10项边界，其中宿主查询错误码确认为None而非SetupInProgress；补充门禁后516/516。原始XML/SHA与日志见Docs/Testing/setup-deployment.validation.json。

本批尚未增加部署命令/地图草稿或重新构建播放器；后续从相同权威GameState与IMapView接入。完整开局仍保留规则包门禁。

独立审阅review_setup_deployment通过（无Critical/Important）；修正Wiki将事件误称全局Ordinal排序的文字。建议增加的跨阶段序号溢出组合属于可选覆盖，现有提交路径使用同一私有缓冲并捕获溢出，暂不另增运行。
