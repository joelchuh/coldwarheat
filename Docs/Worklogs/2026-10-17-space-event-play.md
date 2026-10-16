# Scientist太空事件试玩工作记录

> description: 将单格太空事件接入真实手牌、头条、公共结果与独立人工试玩，逐批记录验证。

从已同步88fb40b8b92ea7011d99a0af05fa6db5c1529a70建立feat/space-event-play，设计/计划c13f4f4。沿用用户自主持续、编码及常规Git授权；内联执行，保留用户两项未跟踪文件。

Task1：原生RED3170/3151/19，18项缺少注册/执行/验证，1项测试构造错误（无部署的Turn1被正常门禁拒绝）改为Turn2/3显式人工局面；旧3150均通过。实现后GREEN3170/3170、零跳过。SpaceEvent注册要求共享Cards/Space/Risk与独立处理器；统一VP胜负、普通次数不变、即时Next/Move原子发布。终点新EventRequiresRuling；真实持牌方终点时从头条候选过滤，不影响OPS。仅基础+Core接线完成，本批Player尚未构建。

Ruling: 终点未知事件保持待裁定，未采用任何新项目解释；正常星号移出、终局保持Resolving并停止根推进，沿用既有审计约定。
