# 早期战争运行基础工作记录

> description: 按计划补三牌共享计算/加载和单骰原子结果，记录原生红绿灯及接线边界。

基线cc309fc2ca6580a05748d4b5290c8ba6b76787a6已与GitHub核对；前批最终Player源码d2791a4、2918通过、55载荷一致，仍保留可运行。当前feat/early-war-foundation，用户文件不动。

设计见Docs/Architecture/early-war-foundation.md，计划见Docs/Plans/2026-10-17-early-war-foundation.md。使用brainstorming/writing-plans/executing-plans/TDD；用户已要求自主持续执行，不重复例行确认。复用已授权Unity目录和本地分支，不复制大型Library缓存。每Task先真正原生失败，再实现通过；只读审查末尾一次。

Ruling: 基础服务的NoActiveInterference/CampDavidPreventsArabIsraeli是可信内部前提，不是新客户端选项 — 与现有DefconVpEventService基础政策边界一致，正式接线必须从权威事实推导 — 如误用于未校验客户端会绕过规则，文档和后续宿主测试必须覆盖。

Task1: 原生RED 2954/2918/36，36个失败全部确认NotImplementedException，旧套件无退步；参数/计算已实现，GREEN进行中。

Task1完成：2954/2954，0失败/跳过，Unity exit0；XML SHA f85d2ddf9d048f9cd29a5a2919c444c89957947a5b1d3413e3989b127810efa7。元数据新增索引初次字段命名不符（path而非descriptor），已按现有契约改正，161项/81数据/406符号/689依赖通过。纯计算尚不产生真实游戏动作。
