# 早期战争运行基础

> description: 三张早期战争从共享JSON到纯C#计算、单骰军事/VP计划；纯服务原生通过，未接出牌会话。

从[模块JSON](../Descriptors/early-war-foundation.module.json)按符号读源码；算法规则见[战争事件](war-events.md)，具体实现边界见[设计](../Architecture/early-war-foundation.md)。

固定朝鲜/阿以由苏联受益，印巴跟随事件控制方；规则邻接/控制、阿以自身修正、单骰胜利转移、每次军事和胜利VP分离。游戏地图美术不参与计算。共享数字仅来自war_policy，不能读取印刷/修正OPS当奖励。

计划按两任务推进：参数/纯计算 → 独立随机fork与军事/VP原子结果。内部NoActiveInterference/CampDavidPreventsArabIsraeli需要可信调用方证明；不等于已经实现持续事件/成功事实。玩家命令只应表达卡牌和目标，不应携带该政策。

实际进度以[原生报告](../Testing/early-war-foundation.validation.json)和[工作记录](../Worklogs/2026-10-17-early-war-foundation.md)为准；目前尚不能从Demo使用三张战争。

2026-10-17 Task1完成：正式三牌参数绑定和纯计算通过2954项原生测试；骰子事务由Task2继续。

Task2完成：2985项原生通过。Plan返回Before/After与骰/修正明细、军事信用、VP决定；Blocked为零变化返回。预先检查最坏胜利溢出；一个有界骰来自独立fork，输入状态/随机源不变。重放原输入可得同结果，当前状态已有同军事ID则拒绝并要求宿主回执，不能把它当作完整命令幂等。
