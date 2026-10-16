# 奥运会规则基础工作记录

> description: 正式D6与奥运会参数、比赛计算及抵制计划的分批原生验证记录。

从已同步b4f92178318a8b6fe5e3b0cb970fafe5c029cbac建立feat/olympic-contest-foundation；设计/计划3179692。沿用用户自主持续、编码及日常Git授权，内联执行，完成后一次fresh只读审查。未改地图或Player入口。

Task1 RED：3255总数，旧3213通过、新42失败、零跳过，失败均源于NotImplemented接口（包括拒绝用例预期ArgumentException却得到NotImplementedException）。新增测试实际读取正式CardCatalog、奥运会/D6参数和Schema，双方全部72种骰组合；未以数据设计案例冒充游戏测试。实现严格加载与纯比较后正在原生GREEN验证。

Ruling: 本批只落实独立规则基础，不实施旧完整设计中的摘要链/选择/来源授权/OPS动作/牌去向。OlympicOperationsOffer是参数提议而不是权威授权；这避免将未完成的会话状态机暴露给玩家。下一批须完成真实决策和分支锁后才能注册可玩事件。

Task1 GREEN：3255/3255，零失败/跳过；共享D6严格加载、卡牌绑定、72骰组合与异常输入均通过。后续单对骰和抵制服务尚待Task2。

Task2 RED：3299总数，旧3255通过、新44均NotImplemented失败、零跳过。实现后GREEN3299/3299、零跳过。每对独立fork、逐次游标检查、双方可能奖励预检、平手继续标志与抵制先DEFCON/核战后Offer已验证。测试中六种非法前提的异常断言收紧为ArgumentException/InvalidOperationException，避免泛Exception通过。
