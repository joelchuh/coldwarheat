# 单条目录同步工具工作记录

> description: 三批重复工作后固化目录摘要同步，记录真实失败/修复/验证与写入边界。

从已同步99efc8f建立feat/catalog-entry-sync，设计与计划25d5bb9。复用共享metadata-validation配置和只读校验器，不复制目录常量。默认预览；写入双SHA绑定、候选全库校验和原子替换；Data业务status保留。用户要求自主继续，当前会话内联实施。

首RED26/2/24；首次实现26/22/4暴露Windows子进程GBK输出，固定stdout UTF-8后31通过，包含实际Junction与3个故障注入/大小边界。自检补候选SHA：RED35/31/4→GREEN35/35。既有只读校验器48/48。各原始summary复制到TestResults/catalog-sync，SHA与fixture路径在正式报告。未改游戏C#，不重复Unity3087回归。下一步真实工程预览、登记描述、fresh只读审查和Git同步。

真实工程先对early-war-play描述执行只读预览，结果unchanged；再调整本工具description，检查preview的before/after和双SHA后执行write，输出updated，仅Tools catalog同步选中摘要。完整元数据166描述、82数据集、420符号、699依赖通过。进入fresh只读审查。
