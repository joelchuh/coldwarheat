# 普通政变原子结算计划

Spec: Docs/Wiki/coup-calculation.md、operations.md及risk-victory.md第3节；当前基础政变已实现，军事/DEFCON/PCG服务已有独立验证。本批不接UI/玩家出牌，继续已授权核心实现。

## 状态与接口

新增Core/Rules/CoupAttemptState.cs和CoupAttemptRules.cs；只依赖现有定义，业务数字仍来自共享数据。CoupAttemptState.Create(gameId,definition,defcon,military,countries,randomSnapshot,activeEffectIds)建立revision0/空收据，不是存档恢复API；Countries必须精确覆盖Defcon绑定地图，拷贝为只读。效果ID快照有界128且唯一，不为空时计划拒绝；它由可信宿主提供，客户端不能自行声明为空。RandomSnapshot作为状态的一部分随整个After一起返回。

CoupAttemptRequest(id,gameId,sourceActionId,sourceRuleId,expectedRevision,turnNumber,target,actor,phasingPlayer,strength,militaryCredit)：必需标识符修剪无变化，id最长128，其余256；版本>=0、turn>0、双方枚举有效、强度和信用正整数。实际预算先由可信策略核实，强度和军事信用分别输入，不据骰子结果计算信用。新请求版本必须匹配；SourceActionId只留溯源，不代表槽位/持牌授权。

CoupAttemptReceipt保存原Request、CoupCalculation、MilitaryCredit、可空DefconChangePlan、RandomBefore/After，内部构造；不保存整个历史聚合状态。CoupAttemptPlan保存Before/After、Receipt、AlreadyApplied。最多4096收据是工程上限；当前只处理同一军事回合，没有回合重置/存档恢复/最终胜负编排。

## PlanAttempt步骤

1. 检查nonnull、GameId/turn匹配；若请求ID已存在，完整事实（含原expectedRevision）一致则返回当前State/旧收据/AlreadyApplied，不验证当前目标、不调用RNG、不回退已推进状态；冲突拒绝。
2. 新请求要求expectedRevision当前、非末期已评估军事状态、有收据容量；基础目标和显式效果门禁通过后才继续。
3. 生成军事信用ID coup/ + request.Id，kind OperationsCoup，basis EffectiveMilitaryOperationsCredit；先PlanCredit。若该信用已存在但没有本政变收据，拒绝不完整事务，不掷骰。军事溢出在RNG之前拒绝。
4. 外部可信IRandomProvider快照必须与State.Random完整一致，Fork独立且同快照；在fork上以[min-1,max)采样+1，检查范围和递增计数/算法stream；原provider不变。
5. 调用已实现Calculate计算影响力；仅BG调用DefconService.PlanChange(BattlegroundCoupDegradation,request.PhasingPlayer)，无论成功。核战返回不可逆信号，无后续牌/事件步骤。
6. 新Countries、军事After、DEFCON After或原状态、RandomAfter、收据和revision+1组装为一个不可变After；任何异常都不得改变Before与原RNG。宿主仍须比较当前Before/version再一次性提交全部状态。

## Task 1: 实现计划和行为覆盖

先状态/请求/计划接口桩及测试，真实RED后实现；GREEN覆盖双方和骰点、失败军事、BG失败降级/核战根责任、非BG不降级、影响力原子结果、PCG可复现且原stream未变、防重当前流不回退/终局后重发、ID冲突/旧版/错game/turn、目标/活动/军事审核/军事溢出/信用孤儿在RNG前拒绝、损坏/共享/不推进/异常随机fork不污染原状态、影响力溢出丢弃军事和RNG计划、状态集合只读/精确地图和无效请求。集合边界4096收据拒绝新动作，不影响合法重发。

## Task 2: 知识与交付

同步模块和catalog/共享数据消费者说明、Wiki流程、计划description JSON、真实验证报告、Worklog、README；全量Unity零失败/跳过、metadata和diff --check。一次独立整批review，例行Git快进/上传与远端HEAD核对，不重建播放器。

## Review Focus

防重发生在目标/核战门禁之前但GameId/turn必须匹配；新请求版本检查；信用孤儿不能默认重掷；原RNG完全不变，After的随机/军事/国家/DEFCON/收据/修订同时形成；Actor与Phasing分离；任何未支持效果在掷骰前拒绝；字典防御复制；纯计划与真正宿主提交/玩家授权界限明确。IRandomProvider仍是可信依赖，不能防恶意隐藏共享内存。
