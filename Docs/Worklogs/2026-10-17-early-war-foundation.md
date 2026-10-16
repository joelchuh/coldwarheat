# 早期战争运行基础工作记录

> description: 按计划补三牌共享计算/加载和单骰原子结果，记录原生红绿灯及接线边界。

基线cc309fc2ca6580a05748d4b5290c8ba6b76787a6已与GitHub核对；前批最终Player源码d2791a4、2918通过、55载荷一致，仍保留可运行。当前feat/early-war-foundation，用户文件不动。

设计见Docs/Architecture/early-war-foundation.md，计划见Docs/Plans/2026-10-17-early-war-foundation.md。使用brainstorming/writing-plans/executing-plans/TDD；用户已要求自主持续执行，不重复例行确认。复用已授权Unity目录和本地分支，不复制大型Library缓存。每Task先真正原生失败，再实现通过；只读审查末尾一次。

Ruling: 基础服务的NoActiveInterference/CampDavidPreventsArabIsraeli是可信内部前提，不是新客户端选项 — 与现有DefconVpEventService基础政策边界一致，正式接线必须从权威事实推导 — 如误用于未校验客户端会绕过规则，文档和后续宿主测试必须覆盖。

Task1: 原生RED 2954/2918/36，36个失败全部确认NotImplementedException，旧套件无退步；参数/计算已实现，GREEN进行中。

Task1完成：2954/2954，0失败/跳过，Unity exit0；XML SHA f85d2ddf9d048f9cd29a5a2919c444c89957947a5b1d3413e3989b127810efa7。元数据新增索引初次字段命名不符（path而非descriptor），已按现有契约改正，161项/81数据/406符号/689依赖通过。纯计算尚不产生真实游戏动作。

Task2开始：不可变WarEventState/WarEventPlan接口及单骰服务测试已写，正在原生RED。覆盖胜败军事、固定/中立归属、终局完整收益、零影响力/DEFCON2、累计超过5、戴维营阻止、非法输入零fork、最坏结果溢出、provider七种故障、PCG重放、军事重复ID拒绝及状态防御复制。

Task2 RED：2985/2954/31；实际XML核对30个NotImplemented与1个缺少上下文校验，非编译失败。已实现先验目标/终局/军事信用/最坏收益检查、独立fork、结果一次性返回；阻止分支在潜在收益计算前零变化返回。原生GREEN继续。

Task2完成：2985/2985原生通过、0失败/跳过、exit0；XML SHA921258b8654cb4c537103b3eaf135659ee3d9ae830adfa56ad31672d97b91a5e。共67新增测试。基础不负责会话幂等：重复军事ID拒绝，未来父命令回执层负责返回原结果；随机fork不会替外部游标提交。后续一次fresh审查与Git同步，Player等可玩接线再重建。
