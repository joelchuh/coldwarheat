# DEFCON、VP与固定军事事件

description：查询Duck and Cover、Nuclear Test Ban、How I Learned的顺序与公式、DEFCON选择、责任和直接军事信用。

状态2026-10-06：#4/#34/#46参数Bound；47 Bound / 55 Unresolved，102处理器planned，未执行C#/Unity。[JSON描述](../../Data/Descriptors/defcon-vp-events.data.json)→[参数](../../Data/Cards/deluxe-2015/defcon-vp-events.parameters.json)、[Schema](../Contracts/defcon-vp-events.schema.json)、[案例](../Design/defcon-vp-events.cases.json)、[报告](../Design/defcon-vp-events.validation.json)。

Duck and Cover先按DirectEvent原因降DEFCON1；到核战级即由既有服务判根PhasingPlayer输，停止，不再给美国VP。否则固定US获得5减变化后DEFCON的分值，并关闭本事件唯一VP批次。出牌者/根行动者是USSR也不改奖励US。

Nuclear Test Ban先以效果控制者的当前DEFCON减2算VP并判胜；不终局再改善DEFCON2，按共享最大级夹限。不能先改善后算分。组合事件按已确认工程序列每个完整VP批次/DEFCON变更即检查终局，不强行执行余步；卡文顺序与终局规则分开登记。

How I Learned先由EffectController选DEFCON轨道任意合法等级，含核战1，不只允许改善/安全选项。选择Owner/Revision从内部帧绑定，非法/旧/越权回应不改轨道/军事。设置并核战审核后，未终局再为控制者计固定5军事行动。信用类型DirectEventFixedCredit，量来自本事件而非2印刷OPS；不借WarEvent或OperationsCoup类型。既有军事存储为非负累计总量，不用图形最大格发明内部cap，也不把旧军事量覆盖成5。核战1选择仍根PhasingPlayer承担，不错判作Cuban Crisis实际Actor责任。

三个事件没有抽骰、不改变影响力，不自动花OPS或开额外根名额；响应结束回父帧。核潜艇按政变原因豁免，不能挡直接事件DEFCON。古巴危机也不是直接设置DEFCON的致命Actor拦截，仍保留其活动实例。未知相关活动状态/来源必须停止，不因本批三参数Bound就放行真实规则包。

DefconVpEventService返回具名有序步骤/决定计划；具体轨道、VP与军事交既有纯服务，由RuleEngine提交每个可恢复边界。状态保存Step/ResolutionId/Owner/Revision/成功收据；相同命令不重复奖分/信用，终局吸收。前序完整步已提交后不因后续非法回应倒退；当前非法响应零变更。星号由cards.json正式定义与成功结果决定，界面只显示，不影响逻辑。

来源英文p2 #4/p6 #34/p8 #46；R2015 §5.2/8.1/8.2/10.2–10.3。三个固定公式/量是卡文参数，各服务只读它，不在脚本复制；最高/核战级、胜利阈值、军事存储在共享规则中。卡文顺序后逐步终局截断/事务划分为DV1工程约定，未运行真实游戏事务。下一批继续其余有明确来源的固定收益/影响力事件，保持可更换表现层。

主离线验证19数据＋30固定参考＝49新项，1173旧回归，共1222通过；24 Schema/27数据描述/19模块/82指针，8runtime未运行，38工具检查独立。独立只读审阅合入Yes；两Minor（计数/模块契约摘要）已闭环，30参考独立重算、12摘要与7工具摘要/LF核对。未独立重跑Schema/1173旧/38工具/8runtime，不夸称独立全1222。
