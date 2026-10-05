# 战争事件第二批与持续设计计划（2026-10-05）

用户授权本批完成后继续后续规则设计，至当前5小时账户额度耗尽；保持AGENTS §§22–23设计阶段，日常提交/推送已授权。创建[持续批次JSON](../Design/rules-design-roadmap.json)作为渐进式恢复入口，不把账户/额度明细写入公开仓库，不自动使用额度重置。

本批补齐Brush War #36与Iran-Iraq War #102。复用具名WarEventService与首批已核实政策；Brush只定义阈值/军事/VP三个差异，目标从地图稳定度1/2查询并动态过滤北约；两伊明确引用首批政策。军事/影响力/VP、单骰与终局保持纯Core设计，不增加C#/Unity源码。[战争Wiki](../Wiki/war-events-batch2.md)关联[JSON数据描述](../../Data/Descriptors/war-events-batch2.data.json)、[模块](../Descriptors/war-events.module.json)、[参数](../../Data/Cards/deluxe-2015/war-events-batch2.parameters.json)、Schema与案例。

主检查器真实验证：22数据案例＋60固定骰/候选参考案例=82通过，回归前694项，共776通过；12项运行验收未执行，C#/Unity测试0。[报告](../Design/war-events-batch2.validation.json)记录11个文件摘要、Schema/目录/链接核对。基础地图52个Brush候选；北约按当前控制过滤四个欧洲候选，双方Actor都受保护门禁。直接对手超级大国修正按Deluxe反转后官方FAQ处理，不继承旧不计裁定；目标自身控制不扣。

官方英文卡图p7 #36、p15 #102，规则§2.1.5/7、§7.6、§8.2.3/4、§10.2/3，FAQv5 p8 #36与p3/p5 #21。完整结果后关闭VP批次及类型化变体为工程约定。空合法Brush候选未发现专门裁定，保留RequiresRuling，不给免费军事；正常52候选减北约四国不会为空。Flower Power未知钩子停止，下批补齐。

临时回归适配第一次未匹配旧Bound计数，检查实际原表达式后改为精确计数替换，再完整重跑；业务夹具与正式数据未据此改动。临时模型未固化为正式复用脚本，旧报告不写占位、不重写，正式保留脚本须满足稳定复用证据并配JSON描述。

独立只读审阅未发现重要问题；已采纳P3补全模块战争/北约来源索引。审阅者独立核对112 JSON解析、目录、11摘要、绑定/目标夹具、依赖无环及相对链接；其唯一完整Schema/参考回归尝试因导入jsonschema遭PermissionError而未运行，不把主检查器776项归为独立重跑。提交前审阅精确差异、摘要与候选/磁盘/暂存字节一致性；普通push后核对HEAD/origin/main/线上main和工作区。本日志记录提交前事实，最终推送SHA在聊天/后续恢复记录中核验，不把自身SHA写入自身提交。

本批后25 Bound / 77 Unresolved，102处理器均planned；17Schema、21数据描述、15模块描述。下一批为Flower Power、Camp David Accords与An Evil Empire，优先冻结战争牌出牌时机、处罚、取消/禁止和事件成功事实。
