# 权威时代与东欧动荡实现

2026-10-13；feature/era-removal，基于已同步6ede3dccfe06e59f580ce810899e48fcba4032d9。用户持续编码/Git授权有效；规则沿用EI2-RULING-01，无新项目裁定。

共享turn-flow数据加载为TurnCalendar。ForTurn返回不可变GameTurnContext，GameState.Turn与公共玩家视图保存它；OPS、事件等待/结算复制保持。旧fixture无Turn可用原功能，东欧动荡缺失上下文明确拒绝。此子集不推进回合，不处理头条、补牌、时代牌或回合末。

新增EraCountryRemovalEffect与对应参数加载器；Query按东欧正USSR影响力选择min(3,候选数)，Plan按真实时代逐国移除min(当前量,时代量)，排序后原子提交。不把早期印刷牌当成早期对局，不要求最大化三国总移除量。响应仍用已有CountrySet事务，不接受自报时代或金额。

两种第二批加载器复用文档/来源核验，原额度Load仍只返回原两牌。新增TurnCalendar模块描述；同步规则、数据、Wiki、JSON摘要与实际符号。

## 实际验证

- era-calendar-red：333项，295通过/38预期NotImplemented失败；green333/333。
- era-removal-red：358项，333通过/25预期NotImplemented失败；green358/358。
- era-context-boundary-red：371项，370通过/1失败；会话将缺失回合映射成InvalidCommand。补具名RuleError映射后371/371，零跳过。
- 最终XML SHA256：df918147623735025cedd4698342fab9c80f11294389472a1872749e16f3d0a5；六轮详细计数/摘要见[报告](../Testing/era-removal.validation.json)。
- 覆盖1/3/4/7/8/10时代边界、日历洞/重叠/缺失、实际地图双子区、逐国短缺/零候选、身份/版本/重复/伪造数量、规则门禁、数值溢出、等待/完成/重试及普通OPS的Turn保持和手牌投影。

本批不构建播放器、不增加Demo场景，旧九练习构建保留；不声称界面已可玩东欧动荡。独立只读审阅通过，Critical/Important/Minor均无；复核六轮XML计数/摘要、代码与文档，Git同步以实际SHA核对为准。

下一步：将东欧动荡早中/晚期人工练习接入同一Demo，公开显示当前回合/时代；随后再扩展正式回合与出牌编排。
