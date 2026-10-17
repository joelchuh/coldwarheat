# 美日安保出牌集成

先读 Docs/Descriptors/japan-pact-play.module.json，再按 entrypoints 定位。

范围：显式注册后的真实直接出牌、两种对手事件顺序、头条和五年计划美国子事件。复用 AddToControl 补足美国控制；保留苏联影响力。GameState 保留条约，回合末不清除。所有已支持的政变/政局调整来源共用目标检查；影响力投放仍按普通规则。

流程：提交意图 → 身份/版本检查 → 私有补点候选 → 私有条约激活 → 原子发布与牌去向 → 玩家快照/回执。跨回合及六个状态复制入口保留同一条约；时间水位拒绝未提交的未来实例。

不含：北约、条约取消、存档导入、完整游戏宣称。Task1 验证中；Demo/独立构建见后续工作记录。

计划：Docs/Plans/2026-10-17-japan-pact-play.md；工作记录：Docs/Worklogs/2026-10-17-japan-pact-play.md；验证：Docs/Testing/japan-pact-play.validation.json。
