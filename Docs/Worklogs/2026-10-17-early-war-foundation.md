# 早期战争运行基础工作记录

> description: 按计划补三牌共享计算/加载和单骰原子结果，记录原生红绿灯及接线边界。

基线cc309fc2ca6580a05748d4b5290c8ba6b76787a6已与GitHub核对；前批最终Player源码d2791a4、2918通过、55载荷一致，仍保留可运行。当前feat/early-war-foundation，用户文件不动。

设计见Docs/Architecture/early-war-foundation.md，计划见Docs/Plans/2026-10-17-early-war-foundation.md。使用brainstorming/writing-plans/executing-plans/TDD；用户已要求自主持续执行，不重复例行确认。复用已授权Unity目录和本地分支，不复制大型Library缓存。每Task先真正原生失败，再实现通过；只读审查末尾一次。

Ruling: 基础服务的NoActiveInterference/CampDavidPreventsArabIsraeli是可信内部前提，不是新客户端选项 — 与现有DefconVpEventService基础政策边界一致，正式接线必须从权威事实推导 — 如误用于未校验客户端会绕过规则，文档和后续宿主测试必须覆盖。

Task1: 待开始。
