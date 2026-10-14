# 补牌与时代加入工作记录

按已授权持续开发目标，用brainstorming/writing-plans整理纯服务方案，executing-plans单实现者。前批TurnEnd已完成并单独保留可玩Demo；本批不改UI，不宣称连续回合已完成。规则来源R2015 §4.1/4.3–4.5B已查询；共享HandTarget与Era日历直接复用。
Ruling: 单E盘功能分支 — 当前聊天仓库不是Unity项目，已验证前批同步后串行开发 — 依赖测试/独立审查防串改。
Ruling: 沿用既有数字重放顺序并按需重洗 — TURN-03在继续补牌时触发 — 通用抽牌须保持此边界。
Ruling: 总牌不足拒绝整计划 — 无明确专门裁定，不凭空造牌或默默少发 — 异常局面未来需补裁定才可继续。
Ruling: 先交纯服务 — 轨道/头条/特殊选择协调尚缺，避免虚称完整对局 — 本批Demo仍停在首回合末。
Pre-flight: TurnCalendar提供目标；CardZones提供唯一完整分区；TurnDealPlan返回Cards/Random必须由未来宿主一起发布，不向正常视图暴露。T1不改既有入口；T2只文档/验证。准备新增14项可编译RED。

准备脚本首试因Docs/Architecture目录尚未存在失败，未写产品文件；随后意外触发一次既有1960回归（turn-deal-red），不计RED。补建目录后有效RED turn-deal-red-actual为1960/1974，14新增均因NotImplementedException失败；逐条核对XML。实现无副作用补牌计划，准备GREEN。

GREEN1974/1974，14新增全部通过；132描述/catalog、72数据、282符号、620数据依赖校验通过。246份源码哈希冻结。没有改Demo装配，不重新构建旧演示；保留TurnEndPlaytest已验播放器。准备唯一独立整分支审查。
