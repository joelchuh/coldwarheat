# DEFCON、军事行动与胜负服务设计 v0.1

> description: 定位 DEFCON 目标限制、核战责任、军事行动记账、回合末审核和终局检查点；包含双方面临扣留计分牌的裁定矩阵。
> 状态：2026-09-22 设计稿；没有 C# 实现、运行测试或完整规则包。RV-H-11已于2026-09-22由用户确认，新版条款优先判USSR胜。
> 检索：[模块目录](../Descriptors/catalog.json) → 对应 description → 本文相关节 → planned_entrypoints。参数见[数据目录](../../Data/catalog.json)，案例见[risk-victory.cases.json](../Design/risk-victory.cases.json)。

## 1. 服务边界

选择三个纯 C# 规则服务，加一个由既有 TurnFlow 承担的编排层。服务读取不可变规则定义和当前状态，返回具名结果；只有 RuleEngine 事务提交器可写 GameState。无需 Unity、网络、文件系统、系统时间或独立随机源。

相比把全部判断塞入 TurnFlow，拆分后可以单独验证风险和分数；相比可执行 JSON 规则解释器，具名服务更容易追踪责任和调试。JSON 只保存参数、裁定记录与契约，不能写入任意表达式驱动游戏。

| 服务（均待实现） | 输入 → 输出 | 不负责的事项 |
|---|---|---|
| DefconService.CheckRestriction / PlanChange | 行动类型、目标规则标签、来源授权、当前 DEFCON、责任上下文 → 允许/拒绝原因，或轨道变更与 NuclearWar 信号 | 骰子、影响力计算、直接宣布任意玩家胜负 |
| MilitaryOperationsService.PlanCredit / AssessTurnEnd | 经规则计算的军事行动信用、双方累计、期末 DEFCON → 新累计或双方缺额与净 VP 批次 | 用印刷 OPS 猜奖励、读取卡名、提前宣布胜利 |
| VictoryService.Evaluate | 已完成的规则检查点及其证据 → Continue / Deferred / Finished / UnresolvedAdjudication | 计算地区控制、读取客户端提供的胜者、顺便清理所有卡牌 |
| TurnFlow / RuleEngine | 有序计划、结果、保存位置 → 下一步骤及原子提交 | 从动画回调或数组遍历顺序决定规则先后 |

模块描述分别见 [defcon](../Descriptors/defcon.module.json)、[military-operations](../Descriptors/military-operations.module.json)、[victory](../Descriptors/victory.module.json)。不建立三套相互调用的可变服务：它们通过结果对象交给编排层连接，避免循环依赖。

## 2. 数据、上下文与持久化

静态定义分为 defcon.json、military-operations.json、victory.json，每份具有对应 .data.json。DEFCON 恢复参数从旧 turn-flow.json 移到 defcon.json，旧文件仅保留数据引用；原数据 schema_version 升为 2。当前没有运行存档，但未来加载器仍须拒绝旧格式静默混用。

动态状态：DefconState.Level；MilitaryState.EarnedBySide 与 AppliedCreditIds；VictoryState.SignedVp、PendingCandidate、Result；TurnEndAssessment 保存 AssessmentId、TurnNumber、DefconAtAssessment、双方累计快照、双方缺额、净变化和已应用标志。军事累计保存本回合实际所得非负整数，不臆造轨道封顶；超额不会减少下一回合义务。显示轨道是否截断由未来表现适配器处理。

责任上下文必须分开保存 RootActionId、FrameId、PhasingPlayer、Actor、EffectController、SourceCardId、RuleId。头条使用当前结算那张头条的出牌者作为 PhasingPlayer；嵌套事件不因操作者改变而替换根行动责任。

RuleEngine 生成 CreditId、BatchId、CheckpointId，使用命令/步骤序号确定性派生；客户端不能指定这些事实或提交“我获得 4 点军事行动”。每个 ID 的重复处理不得二次加分。保存记录附带规则集版本和数据摘要，恢复同一继续位置，不能重新抽取随机数。

## 3. DEFCON 服务

限制数据是按等级、行动类型、规则地区 ID 查询的只读表。国家可以有多个标签；东南亚通过国家/地区定义归入亚洲限制范围，不能依赖美术地图分区或中文名称判断。尚未建立正式国家目录，因此本次地区 ID 为待正式目录绑定的稳定引用，完整开局校验仍阻塞。

授权对象 ExceptionGrant 必须由已注册的卡牌策略生成，包含 SourceEffectId、ActionKind、RegionScope、IgnoreGeographicRestriction；不接受玩家载荷中的 ignoreDefcon=true。地理限制豁免、战场降级豁免和军事奖励类型是独立能力，不用一个 IsFree 全部覆盖。

基准表及数值以 [DEFCON 参数](../../Data/Rulesets/deluxe-2015/defcon.json) 为唯一来源；依据 R2015 §6.3.4–5、§8.1。影响力投放和战争事件不套用普通政变/调整的地理门禁，仍接受各自卡牌规则校验。

PlanChange 接收内部生成的 ImproveOne、DegradeOne 或已核实卡牌的 SetLevel 意图。达到上限时 ImproveOne 为无变化；批量改善必须由卡牌计划明确拆分或给出有效目标。输入状态越界属于技术错误，不靠钳制掩盖损坏。DegradeOne 到核战等级立即产生责任信号，不能建立 WaitingDecision 让之后的改善补救。缺失 PhasingPlayer 时拒绝整个事务并报告规则错误，不猜成 Actor。

普通政变目标校验和明确事件拦截完成后，按 R2015 §6.3.3 的例子先记军事行动，再掷骰/处理影响力，最后处理战场降级与核战检查。所有步骤作为同一命令事务推进；若已终局，后续对手事件、反应和行动名额结算停止。卡牌若有政变尝试即触发的特殊失败，由已核实的 BeforeCoupAttempt 策略提前处理；未知策略必须在消耗骰子前阻止执行。

DEFCON 达到核战等级时，普通规则的败者是 PhasingPlayer。例如 US 正在行动、USSR 执行事件选择导致降级，不能仅凭实际选择者改判。合法但会自毁的行动仍可提交；UI 可以预警，不能自行把它变成非法行动。

## 4. 军事行动服务

PlanCredit 只接受已完成资格验证的 MilitaryCredit：CreditId、Beneficiary、Kind、Amount、SourceRuleId。Kind 区分 OperationsCoup、WarEvent 和 FreeCoup；调整、影响力与太空不调用军事记账入口。普通政变 Amount 来自预算服务的 MilitaryOperationsCredit，不能误用骰子加成或 CoupStrength。战争收益对象来自事件策略，不能统一取根行动玩家；失败的战争/政变也不因此撤销已获军事行动。

计入规则与来源见 [军事参数](../../Data/Rulesets/deluxe-2015/military-operations.json)，采用 R2015 §7.4.2、§7.5、§8.2。战争牌具体奖励应来自未来对应卡牌数据，不在本服务复制一张战争卡表。

AssessTurnEnd 一次读取同一份状态快照，并计算：

~~~text
required = DefconAtAssessment
shortfallUS   = max(0, required - earnedUS)
shortfallUSSR = max(0, required - earnedUSSR)
netVpForUS = (shortfallUSSR - shortfallUS) * penaltyPerPoint
~~~

例：DEFCON=4，US 累计2，USSR累计1，缺额分别2与3，净变化向 US 加1。若 US 原有19，结果20只是候选胜利，仍须完成扣留计分牌审核。若双方缺额均2，净变化0，不能先加2触发胜利再扣2。

Assessment、净 VP 应用、双方军事累计归零和步骤游标一起提交；这是 R2015 §4.5E 指定的归零边界，修正旧编排中笼统的 ExpireAndReset 描述。完整快照保留在 Assessment 中供审计。已评估的期末义务不因后续事件而重算，也不在 TurnStart 再次扣罚。

## 5. 胜负检查点与不可逆终局

胜负服务使用枚举检查点，不提供每次状态变化都调用的 CheckVictoryEverything。SignedVp 是 US 正、USSR 负的完整整数；不能把中途累计钳制到显示轨道端点，否则后续抵消和最终计分会失真。

| 检查点 | 输入证据与处理 |
|---|---|
| NuclearWarReached | 校验降级记录及责任 → PhasingPlayer 失败，立即停止余下规则步骤 |
| ScoringResolved | 地区服务提供完整双方净分与 EuropeControlWinner；仅出现国家控制变化不能触发欧洲胜利 |
| VpBatchClosed | 同一事件/计分牌的全部双方 VP 已汇总，才检查普通分数线；批次未关闭不判胜 |
| MilitaryAssessed | 保存军事净分后的 CandidateWinner；结果为 Deferred，不发布 GameEnded |
| HeldScoringAudited | 一次取得双方违规布尔值，再按第 6 节矩阵处理，不逐个玩家早退 |
| FinalScoringCompleted | 校验完整地区结果集合与附加结算均完成；欧洲控制优先，否则按总分正负/零给结果 |
| ExplicitCardEnd | 只接受已核实卡牌策略产生的终局证据；不等于 FinalScoring，不自动再计算地区 |

VP 批次以语义来源划分，不以整张牌的 UI 动画或整次根行动划分。对手事件、后续 OPS 和独立触发效果可能属于不同批次；事件需选择时，BatchId 与暂存奖分随帧保存。框架不能以“等待整张牌播完”推迟已经到达的明确终局点。

最终计分接收FinalScoringService汇总的RegionScoringService结果；必需六地区集合从region-scoring数据定义派生，不在服务再复制。不得漏项、重复计某地区、额外独立计东南亚或在中途越过普通分数线就截断。第10回合中国牌持有者1VP已核对并放入scoring-card-parameters；持续计分修正仍须已核实提供者交齐。完整国家与卡牌目录尚未冻结，缺失时报错，不默认0。欧洲控制用独立胜利标志，不用“大额VP”模拟。详见[地区计分](region-scoring.md)。

GameResult 保存 ResultId、Winner 或 Draw、Reason、CheckpointId、SourceRules、StateVersion、公开证据摘要。只有 Continue/Deferred 可以继续。Finished 是吸收态，后续命令不能覆盖原结果；重复命令仍返回原结果。UnresolvedAdjudication 是技术暂停，不是平局、败局或可由玩家跳过的选择。

## 6. 双方扣留计分牌：依据与完整矩阵

[R2015 §10.3.1](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf) 对军事候选赢家明确要求审核；[F2010 官方 FAQ 最后一问，PDF第22页](https://www.gmtgames.com/nnts/FAQv5.pdf) 对双方扣留明确给出 US 胜。FAQ 封面写明设计师认可；[GMT 当前资料入口](https://www.gmtgames.com/p-1138-twilight-struggle-20th-anniversary-hall-of-fame-edition.aspx) 仍将它列在 Deluxe 与旧版资料下。后一点证明来源归属，不证明全部旧答案均有效。

因此关闭“双方扣留的一般情况无依据”的问题（CF-002）；交叉组合CF-003（双方扣留且美国为军事候选赢家）现按用户确认的项目解释解决。旧 FAQ 的军事先胜免审核条目仍被 CF-001 明确排除，不能借双方裁定把它重新引入。

以下 candidate 指军事净结算已达到普通胜利线的一方；若更早发生其他合法终局，根本不会到本表。

| US 扣留 | USSR 扣留 | candidate 无 | candidate US | candidate USSR |
|---|---|---|---|---|
| 否 | 否 | 继续 | US 胜 | USSR 胜 |
| 是 | 否 | USSR 胜 | USSR 胜 | USSR 胜 |
| 否 | 是 | US 胜 | US 胜 | US 胜 |
| 是 | 是 | US 胜 | USSR 胜（CF-003） | US 胜 |

CF-003于2026-09-22获用户确认：优先采用新版明确例外，判US输。这是项目版本解释，不冒充新版官方专门裁定。该行winner=USSR、outcome=Finished；12种情况均已明确。记录与案例 RV-H-01 至 RV-H-12 一一对应。

普通模式继续使用之前确定的权威侧私密审核，不公开整副剩余手牌；这是数字端产品约定，未启用锦标赛的全手牌展示。违规结果不改写 DEFCON 数值，不伪造一次由另一方触发的降级事件。太空回合末弃牌在审核后，不能用于消除违规。

## 7. 与现有状态机的连接

~~~mermaid
flowchart TD
    A[军事行动快照与双方净罚分] --> B[保存候选胜利并归零军事累计]
    B --> C[一次审核双方计分牌]
    C --> D{裁定结果}
    D -->|已判胜负| E[Finished]
    D -->|交叉规则未确认| F[保留现场 暂停]
    D -->|继续| G[中国牌翻面与回合末能力]
    G --> H[到期效果处理]
    H --> I{是否末回合}
    I -->|否| J[推进回合]
    I -->|是| K[完整最终计分]
    K --> E
~~~

TurnFlow 调用结果服务，不解析原文或根据文件顺序挑规则。事务内一旦得到终局就停止；技术错误回滚本次事务并在会话层报告 Faulted，不能把半次政变或未结算净罚分提交为正常状态。玩家公开日志只输出可见的数值、理由和终局；内部审核卡牌 ID 不外泄。

## 8. 验收与下一步

[risk-victory.cases.json](../Design/risk-victory.cases.json) 包含基础边界、12格扣留矩阵、双方净分、嵌套责任、终局截断、存档幂等和隐藏信息案例；均为 planned。已核验 JSON 与引用不等于规则引擎测试通过。

地区计分与太空竞赛已形成设计和基础参数，见[地区计分](region-scoring.md)与[太空竞赛](space-race.md)，仍未实现。下一步整理正式国家/卡牌目录与逐卡计分、推进、终局和拦截策略，冻结规则包后进入纯C#实现。尚未核实的逐卡顺序不靠默认优先级填充。

[返回索引](index.md)

## 持续效果接入（2026-10-01，待实现）

参见[持续效果与OPS修正](persistent-effects.md)：实际行动者决定修正归属，合并修正后报价；太空不使用地区奖励；普通政变信用接有效OPS，战争固定信用和免费政变保持独立。回合末在现有 `ExpireAndReset` 边界到期，读档保留激活序列与行动快照。

## 条约保护接缝（2026-10-05，待实现）

见[北约与美日安保](treaty-protection.md)：前置查已提交的成功事件事实，行动限制查当前控制权。DEFCON地区豁免不绕过条约保护；所有门禁通过后才消耗预算、随机数或改变军事/DEFCON状态。

## 北约局部例外批（2026-10-05，待实现）

[戴高乐与勃兰特](nato-exceptions.md)复用固定影响力变更、永久活动实例及VP批次；取消只结束持续例外并登记未来阻止，不逆转历史收益，不删除北约。勃兰特唯一VP批次在卡文首步关闭，终局停止后续步骤。拆墙完整参数已在事件行动授权批绑定，不能以参数或取消接缝代替运行处理器。

## 拆墙与事件行动授权（2026-10-05，待实现）

见[事件行动授权](event-operations.md)：强制取消/固定收益先提交至可选行动等待点；额外行动采用单独预算，US为Actor，原PhasingPlayer承担普通核战责任。欧洲政变/调整豁免只覆盖DEFCON地理门禁；战场降级、其他事件拦截与终局仍生效。免费政变不计军事；每次调整后重查局面，余量不可转成投放、太空、政变或根OPS。

## 首批战争事件接入（2026-10-05，待实现）

[三张战争](war-events.md)无论胜败都给事件受益方固定2军事，胜利给2VP；信用不读取修正OPS。军事、胜利转移和VP完成后关闭战争VP批次，按共享符号/阈值判终局。
