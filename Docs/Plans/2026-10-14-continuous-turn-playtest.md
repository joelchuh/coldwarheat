# 连续回合试玩实现计划

使用executing-plans单实现者＋最终唯一最强整分支审查。用户持续开发到额度10%及日常Git授权有效。
Goal：结算后可进入下一回合，真实补牌/恢复DEFCON/重置尝试，保持地图和历史。
Spec：Docs/Architecture/continuous-turn-playtest.md；R2015§4与已确认有限试玩边界。
Global constraints：纯Core、共享日历/轨道参数、固定身份/版本/幂等、原子提交、界面不裁决，显式跳过头条。

## T1 核心推进
文件：新Core/Rules/PracticeNextTurnRules.cs、Tests/EditMode/NextTurnPlayTests.cs；ActionPracticeState/Rules及Coup/Scoring/Realignment/OpponentOps、PracticeRiskState、GameState、InfluenceRules。
接口：CardActionKind.BeginNextTurn追加；Definition.NextTurnEnabled最后可选false且要求TurnEndEnabled；CanBeginNextTurn(GameState,PlayerSide)及内部PlanNextTurn；GameState.WithNextPracticeTurn一次发布；PracticeRiskState.BeginNextTurn；统一PracticeSlotId/PracticeOperationsId。
- [x] 可编译stub＋测试RED，run-unity-editmode.ps1 -RunId next-turn-core-red。旧1974保持通过，新行为失败。
- [x] 实现门禁：已Continue审核、Complete全部普通行动且无残留、China正面/OPS空/MilitaryReset一致、非最后回合、无额外行动/期末弃牌能力；任一固定合法身份可请求。额外行动能力在审核前也拒绝。
- [x] Plan内Calendar下一回合→Defcon改善→Military重绑保留Reset值/历史并清Assessment→Space.BeginNextTurn→TurnDeal.Plan→ChooseCard0/新空OPS→一次WithNextPracticeTurn。VP/影响力/China/既有牌/历史保留；无牌和无目标；Overflow→NumericOverflow，补牌参数错误→TurnPreparationUnavailable。
- [x] 太空、普通/对手政变、计分的slot和所有行动OperationsId包含回合，测试同槽跨回合不冲突；全量GREEN。

## T2 宿主UI与真实构建
文件：新Infrastructure/DemoCatalog.NextTurn.cs、Tests/EditMode/NextTurnSessionTests.cs；Application/PublicPracticeView及PlayerViewSnapshot、Controller.Cards、View.TurnEnd/Actions、Bootstrap/Editor。
接口：WithPracticeNextTurn位于TurnEnd之后QuickStart之前；PublicPracticeView.NextTurnEnabled/Available；Controller.BeginNextTurnAsync。没有新网络命令类型，沿CardActionCommand固定身份防重。
- [x] RED默认真实12行动后审核→NextTurn，Host重发/冲突/过期与隐藏手牌，Controller报告和新回合字段；错误装配拒绝。
- [x] GREEN接按钮“下一回合（跳过头条）”，顶栏回合号；旧配置不启用。BuildContinuousPlaytest输出Builds/ContinuousPlaytest独立产品名。
- [x] 全量测试/metadata/源码与载荷Hash，独立Unity构建；尽可能真实鼠标12行动→审核→下一回合→操作；记录实际限制。
- [ ] JSON描述、Wiki/索引/路线/README/AGENTS/工作记录/验证报告；唯一审查及必要一次TDD修复，ff/main推送核对。

## Review Focus
1. 跨回合同一槽的太空/政变与军事信用ID冲突；需保留历史同时允许新行动。T1两条同槽回归。
2. Military.InitialEarned与ResetEarned不同，下一回合必须沿已归零结果而非重新开局；T1 fixture测试。
3. 已审核但残留实体/状态不一致、未审核/终局/最后回合不允许推进；T1门禁和溢出测试。
4. 太空额外行动与期末弃牌尚未实现，不得默默跳过；T1自制单步能力fixture分别检查审核/推进门禁。
5. Host幂等/过期与两玩家手牌过滤，UI不能把跳头条试玩标正式整局；T2真实默认牌与私有投影测试。

Ruling: 连续试玩继续显式跳头条 — 沿既有用户认可的试玩边界，先让地图局面连续 — 不能衡量正式对局平衡。
Ruling: 遇未实现期末能力或最后回合停止 — 避免悄悄跳过规则 — 部分路线仍不能完整玩完。
Ruling: 跨回合ID改为含Turn — 保留历史防重必须区分新行动 — 不兼容旧未发布存档；当前本就无存档入口。
