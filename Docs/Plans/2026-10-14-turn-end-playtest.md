# 回合末结算试玩实现计划

按 writing-plans / executing-plans 单实现者执行，最后一轮最强独立整分支审查。基线 f94dbf0ce55e8122e85dc4da7c46c55c73448385 已本地/远程核对。用户持续开发与日常Git授权有效。

目标：六轮双方行动全部完成后，玩家可查看一份真实回合末结算，正确处理军事净罚分、扣留计分牌及胜负；未终局才翻正中国牌和清理已实现的回合OPS效果。
Spec：Docs/Wiki/turn-flow.md §7、risk-victory.md与已确认共享victory.json矩阵；官方R2015 §4.5E–H/8.2/10.3.1本轮复核。中国牌§9.3。首版仍跳过头条、只运行一回合，下一回合/补牌/最终计分/太空弃牌选择不冒充完成。

## 设计与边界
- 选择显式“结算本回合”命令，作为此试玩的确定性阶段推进入口。相比把结算塞入所有出牌/事件完成函数，此入口可清楚展示动作已完成和回合审核的区别；未来总协调器可复用内部计算。按钮不能跳过行动，不携带VP/赢家/手牌答案。
- CardActionKind追加AssessTurnEnd（保留枚举值）；ActionPracticeDefinition可选TurnEndEnabled。仅最新装配启用，旧练习兼容。
- CanAssessTurnEnd必须要求已Complete、完成全部普通行动、无ActiveCard/Resolving/事件/对手序列/部署、未终局、尚未评估。本阶段无行动方，任一固定合法身份都可请求同一审核；幂等由Host的身份＋命令ID负责，新ID重复被拒绝。
- 先MilitaryOperationsService.AssessTurnEnd得到双方同快照的缺额、净VP和归零；VictoryService军事证据只产生候选，再由权威普通手牌Kind.Scoring检查及HeldScoring证据取共享矩阵最终结果。不逐方先判20VP，不在UI或控制器复制矩阵。
- 无裁定行返回EventRequiresRuling且不发布任何部分。终局保留结算快照与归零结果，停止中国牌翻面及效果到期。Continue才保留同一持有者翻正China、清除当前两个有限回合OPS实例。太空尝试计数不提前重置，不发牌、不改Turn/DEFCON、不自动开始下一回合。
- 结果存入不可变PracticeTurnEndReceipt：军事快照、前后VP、双方是否扣留计分牌、军事候选/最终Decision、是否翻面与到期个数。公开投影只含结算必要事实，不公开剩余手牌ID、牌库或RNG。
- 最终GameState/卡区/VP/军事/效果/receipt一个WithPractice发布，version/eventsequence溢出整次拒绝；原始状态保持。后续所有出牌在Complete/Ended门禁下停止。
- GameState/ActionPracticeState检验已审核结果与Complete、行动数、Risk.Assessment/VP一致。不要借bool配置启用未知事件。

## T1 核心审核
文件：新增Core/Rules/PracticeTurnEndRules.cs；修改ActionPracticeState、ActionPracticeRules.Plan分发、PracticeRiskState、TurnOpsEffects；测试Tests/EditMode/TurnEndPlayTests.cs。
接口：CanAssessTurnEnd(GameState,PlayerSide)；PlanTurnEnd通过既有CardActionTransition；PracticeTurnEndReceipt存于ActionPracticeState.TurnEnd；WithTurnEnd风险状态同时接纳军事计划与最终Decision。
- [x] 先写可编译stub及行为测试，运行run-unity-editmode.ps1 -RunId turn-end-core-red，预期仅新增失败。
- [x] 实现最小编排，覆盖12种候选/扣留组合（尤其CF003美候选且双方扣留→苏胜）、净额抵消、军事归零/源快照、未知裁定、早调用/未配置/重复/非法身份、版本与序号溢出、终局不翻面/不清效果、Continue翻面与到期/不推进回合。
- [x] 同命令测试保存前后权威牌区/RNG一致，跑全量GREEN。测试造局明确fixture，不修改正式初始发牌。

## T2 宿主与界面交付
文件：新增Application/Sessions/TurnEndView.cs、Presentation/DemoView.TurnEnd.cs、Infrastructure/DataLoading/DemoCatalog.TurnEnd.cs；修改CardActionContracts、DemoController.Cards、DemoView.Actions/Scoring、Bootstrap与Editor；测试TurnEndSessionTests.cs。
接口：公开PublicPracticeView.TurnEnd及TurnEndAvailable（无私有对象）；Controller.AssessTurnEndAsync；DemoCatalog.WithPracticeTurnEnd在China之后QuickStart之前。
- [x] RED测试Host固定身份/幂等/命令冲突、两身份查询一致且无牌名、Controller可切换查看、拒绝在行动中审核、实际12行动路线抵达审核入口。
- [x] GREEN接线，面板始终从当前快照显示报告（切换身份/重发不会丢），独立TurnEndPlaytest构建；保留地图IMapView接口。
- [x] 新模块JSON+catalog/index/Wiki/工作记录/validation；全量tests、metadata、source/payload hash、构建和尽可能真实鼠标结算。若固定发牌路线未能到达，记录限制，不偷偷改正式牌库。
- [x] 唯一独立整分支审查；Critical/Important单轮TDD修复，无复审，Minor记账；ff/main推送及远程核对。

## Review Focus
1. 军事净额跨20分与双方扣留的冲突优先级；应只按最终矩阵一次结束。
2. Complete不代表无未结算根；所有Active/Resolving/事件残留必须拒绝，不清理以掩盖损坏状态。
3. 任意身份只能请求无输入的已到达审核，不得跳过最后行动；重复/冲突/过期不再奖罚/翻面。
4. 终局前后的China/持续OPS处理时机；清理不得抢在计分牌审计前，普通牌堆不应变化。
5. 公开报告跨身份不暴露手牌，UI不能把军事候选误标赢家或把单回合Complete说成完整整局。
