# 太空回合末弃牌设计

目标：补齐太空第6格领先能力在连续回合中的真实选择；保持单窗口双身份、权威命令和隐藏手牌边界。用户已授权自主持续开发，不再次等待阶段审批。属于状态与跨层投影的架构增量，沿用当前已激活的E:/TwilightStruggle工程和新功能分支，不重建Unity缓存。

## 方案与边界

选择权威状态内的一个有界回合末决策。相比UI直接移牌，它能做身份/版本/重试校验；相比提前搭通用动态工作流引擎，现有卡牌命令与不可变状态足以表达本次能力。复用SpaceAbilityRules和CardZones，数据数值来自既有space-race.json。

流程：完成普通行动→军事/扣留计分牌审核→未终局才清理/中国牌翻正并创建太空弃牌选择→有权方选弃一张或保留→下一回合/最终计分。尚未支持的第8格额外行动继续拒绝，不把弃牌支持扩称完整太空编排。已终局不创建选择；无能力不创建；有能力而普通手牌空，自动记录NoCards，无须多按按钮。

规则来源：R2015 §4.5、§6.4.4；F2010官方FAQ v5 §6.4 PDF第20–21页。能力只在拥有者到达里程碑且对手尚未到达时有效；可从多张普通手牌选一张；不是随机或弃全部；扣留计分牌先判负，不能用此能力补救；弃牌不执行事件，星号牌也进弃牌堆。中国牌为独立实体，永不列为普通手牌候选。新版军审胜负条款仍优先，旧FAQ其他段落不覆盖本项目既定裁定。

来源链接：https://www.gmtgames.com/nnts/TS_Rules-2015.pdf ; https://www.gmtgames.com/nnts/FAQv5.pdf 。2026-10-16已核对。

## Core与宿主契约

新增SpaceEndTurnDiscardState：Owner、Status(Pending/Declined/Discarded/NoCards)、Candidates、DiscardedCard、DecisionId；内部绑定原Track、TurnEndReceipt及当前CardZones引用。构造不公开，ForAudit工厂只在审核完成且继续时使用；能力上限只支持正式值1，未知上限拒绝。候选为本方普通Event牌，稳定ID排序、防御复制。解析选择后沿用原审核/轨道，更新绑定牌区和状态；下一回合清除该决策，最终计分保留已完成记录。

ActionPracticeState.SpaceDiscard只允许与同一已审核完整回合共存，禁止把Pending与最终计分同时挂入状态。CardActionKind追加DiscardHeldCard、KeepHeldCards；沿既有LocalGameHost身份、版本、去重与一次发布路径。弃置须有Card、保留须无Card，两者均无国家Target。错误侧统一WrongActor；非法候选InvalidSelection；旧版本StaleVersion；重复新ID不能二次消费。数值溢出不发布部分牌区/收据。

CanResolveSpaceDiscard验证阶段、审核/风险/回合引用、空待处理根、同一绑定牌区/轨道和当前能力。CanBeginNextTurn/CanCompleteFinalScoring以共享完成门禁替换旧的能力一律不支持分支：Pending拒绝，Resolved放行，其余陈旧/未知状态拒绝。第8格仍UnsupportedCardAction。

Application新增PublicSpaceEndTurnDiscard到PlayerViewSnapshot.SpaceDiscard。仅Owner且Pending且仍可提交时收到Candidates/CanChoose；对手候选永远为空。已实际弃置的CardId属于公开弃牌结果，保留选择不泄露牌名。对象/集合不可变，不泄露Core状态、完整牌区或随机状态。

## 可测试场景

CreateAtTurn新增可选initialTrack参数，要求与definition.Space同一实例；仍是可信人工初态、零尝试/无历史，不表示已挣过奖励或存档恢复。

将原FinalSetup提取为内部DemoTurnEndSetup和共享真实行动准备方法。原第16场景数据、结果、版本保持不变。新Data/Demo/space-end-turn-discard-scenarios.json仅引用该场景的地图/普通行动牌，加双方初始太空位置、回合、保留牌及标题，避免复制规则和地图。

追加第17/18场景：第9回合美国领先→审核→弃/保留→第10回合；第10回合苏联领先→审核→弃/保留→最终计分。初态显示人工局面；正常牌动作仍通过14个Influence/Finish生成。保留牌不被准备流程消费。新fixture只支持Late时代，全部牌区与时代一致；新增普通牌必须从源DrawPile移动，不能复制实体或使用中国/计分牌。

## UI与验证

右侧回合末面板显示拥有者、选牌/弃置/保留；非拥有者只见等待及切身份入口。沿既有手牌弹层选候选，不能在此选择中国牌或使用OPS。选牌与鼠标操作只改展示状态；提交才通过命令改权威状态。明示Pending期间不能下一回合或最终结算。

用独立SpaceDiscardPlaytest构建，保留已验收FinalScoringPlaytest。Editor和Player同一装配链与正式载荷；测试覆盖双方、能力失效/空手/星号/对手事件牌/扣留计分先终局/错误侧/非法参数/溢出/幂等/隐私/第10回合门禁。真实点击两条分支至少弃置→续回合和保留→最终，不冒称十回合全局与全卡可玩。
