# 补牌与时代加入工作记录

按已授权持续开发目标，用brainstorming/writing-plans整理纯服务方案，executing-plans单实现者。前批TurnEnd已完成并单独保留可玩Demo；本批不改UI，不宣称连续回合已完成。规则来源R2015 §4.1/4.3–4.5B已查询；共享HandTarget与Era日历直接复用。
Ruling: 单E盘功能分支 — 当前聊天仓库不是Unity项目，已验证前批同步后串行开发 — 依赖测试/独立审查防串改。
Ruling: 沿用既有数字重放顺序并按需重洗 — TURN-03在继续补牌时触发 — 通用抽牌须保持此边界。
Ruling: 总牌不足拒绝整计划 — 无明确专门裁定，不凭空造牌或默默少发 — 异常局面未来需补裁定才可继续。
Ruling: 先交纯服务 — 轨道/头条/特殊选择协调尚缺，避免虚称完整对局 — 本批Demo仍停在首回合末。
Pre-flight: TurnCalendar提供目标；CardZones提供唯一完整分区；TurnDealPlan返回Cards/Random必须由未来宿主一起发布，不向正常视图暴露。T1不改既有入口；T2只文档/验证。准备新增14项可编译RED。

准备脚本首试因Docs/Architecture目录尚未存在失败，未写产品文件；随后意外触发一次既有1960回归（turn-deal-red），不计RED。补建目录后有效RED turn-deal-red-actual为1960/1974，14新增均因NotImplementedException失败；逐条核对XML。实现无副作用补牌计划，准备GREEN。

GREEN1974/1974，14新增全部通过；132描述/catalog、72数据、282符号、620数据依赖校验通过。246份源码哈希冻结。没有改Demo装配，不重新构建旧演示；保留TurnEndPlaytest已验播放器。准备唯一独立整分支审查。

独立审查1c4fee5：0 Critical、0 Important、1 Minor，允许合并；246源码和全部XML哈希核对一致。审查使用完整diff，不重复跑测试。
Final: minor (deferred): 补充Fork抛错及随机已推进后失败的回归；当前溢出测试在第一次采样即失败，局部复制隔离已静态核对。
Final: Ruling: TurnStart资格与Cards/RNG共同提交由未来Host负责 — 无调用入口，不授权玩家自行补牌 — 接线必须补门禁与事务测试。
Final: Ruling: 回合/DEFCON/太空/头条/特殊期末选择未接 — 纯服务刻意隔离 — 本批没有连续回合能力。
Final: Ruling: 私有计划不向客户端传输 — 没有客户端接线 — 未来只投影公开数量和本人手牌。
Final: Ruling: 不新建播放器/UI验收 — 此纯服务未接Demo — 当前用户继续使用TurnEndPlaytest。
Final: Ruling: 总可用牌不足时拒绝 — 工程失败边界不是官方专门裁定 — 异常局面暂不能继续。
Final: Ruling: 只在还需抽牌时重洗 — 沿TURN-03继续补牌边界 — 后续通用抽牌/事件必须保持一致。
Final: Ruling: 同ID但其他静态属性不同的目录/自定义日历由已审核输入前提约束 — 服务仅校验分区与时代 — 完整RulePack摘要绑定仍待实现。
Final: Ruling: 恶意随机提供者违反Fork独立性契约 — 生产Pcg32Random已满足 — 不将服务当不可信插件沙箱。
Final: Ruling: 持久化/并发/命令幂等留宿主 — 纯服务只承诺确定性和完成结果重试不再补发 — 接线需保留固定身份和命令防重。
Task1/2 complete：1974/1974，元数据通过，唯一审查无阻塞。准备同步并开始连续回合试玩接线。
