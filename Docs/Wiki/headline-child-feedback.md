# 头条子事件结果提示

> description: 五年计划头条的即时/等待/续接公开摘要，版本门禁和可滚动结果区；源码/原生/Player有限验收完成，验证见报告。

先读[模块JSON](../Descriptors/headline-child-feedback.module.json)，再按符号查控制器和表现层。规则和权威会话沿用[五年计划](five-year-plan-play.md)，不增加抽样、卡牌处置或裁定。

实现：控制器只保存公开的头条子事件步骤及其GameId/Version；切换身份保持公共结果，局面版本推进后不再作为当前摘要，旧重试不能覆盖已完成结果，场景重置清除缓存。首次秘密头条提交没有公开步骤，不能从这个入口显示任何子牌。

三条入口：HeadlineCommandResult.Steps、EventCommandResult.HeadlineSteps、CardActionResult.HeadlineSteps。反馈区域使用GUI字体测高和滚动布局，消息变化回到顶部；正文复用五年计划结果和子DEFCON/VP/OPS数据。

2918项原生通过，55份载荷一致，定向审查无遗留问题；政变先行到子事件完成实际鼠标验证。瞬时头条有自动回归，超长组合滚动未逐项手测。实际红绿灯、构建/鼠标/复核结果见[验证报告](../Testing/headline-child-feedback.validation.json)和[工作记录](../Worklogs/2026-10-17-headline-child-feedback.md)。
