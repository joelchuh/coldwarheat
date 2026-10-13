# 基础胜负检查点实现计划

> 使用executing-plans在当前会话inline实施；一次整批独立审阅。用户持续优化和常规Git授权有效。
**Goal:** 把完整VP批次、军事候选/扣留审核和核战责任四个基础检查点实现为纯计算。
**Spec:** Docs/Wiki/risk-victory.md第5、6节；Data/Rulesets/deluxe-2015/victory.json；RV-H全矩阵及RV-V-01/02。
**Architecture:** 不可变VictoryDefinition和具名证据对象；VictoryService.Evaluate统一返回只读VictoryDecision。加载器读取已有共享阈值及12行裁定。服务不写GameState，不模拟尚未完成的宿主事务或存档。
**Tech Stack:** Unity6000.6/C#9纯Core，Infrastructure的Newtonsoft严格JSON读取，EditMode NUnit。

## 决定与边界

这是既定风险/胜负架构中的下一子集。比较了直接修改GameState、建立完整新胜负状态机、无状态检查点评估三个方案；选择最后一个，先复用现有DEFCON/军事结果提供强类型证据。完整命令防重、终局吸收态和持久化仍由未来宿主实现，不能称为整局可玩。

ClosedVpBatchEvidence只接受已经完整汇总的双方非负奖分和批次前SignedVp；先用long算净值，再检查int范围，最后检查共享胜利阈值。它是可信调用方声明批次已关闭的入口，不收集动画或中间奖分。普通/军事批次前分数必须在自动胜利线内，拒绝以新批次救回已经应当终局的分数。

MilitaryAssessedEvidence消费不可变MilitaryTurnEndAssessment，计算完整净VP并返回Deferred及PendingMilitaryVictory，即使没有候选也必须审核；Pending绑定同一VictoryDefinition和评估来源/回合。HeldScoringAuditedEvidence消费该Pending及权威双方违规布尔值，按共享矩阵一次查询，公开结果不包含任何手牌ID。另一个规则定义不能复用旧Pending。结果只给绝对SignedVp，不给可重复累加增量；未来宿主将其与军事状态/游标一起提交。

NuclearWarEvidence只接受真实DefconChangePlan的核战结果，赢家是PhasingPlayer的对方；不接收客户端直接声明的winner或用Actor猜责任。四种证据为封闭继承集，其余ScoringResolved、FinalScoringCompleted和ExplicitCardEnd检查点不提供伪实现。

每行保存case_id、条件、outcome、winner、authority和adoption_status。Definition校验12种条件完整且唯一、case_id唯一、枚举/初值/阈值合法，防御复制。未采用行只能UnresolvedAdjudication且无winner；不猜平局。共享文件现有12行均已采用，CF-003仍明确是用户项目解释。Loader严格校验schema2、来源/政策/采用记录、符号与类型；final_tie_score只确认现有格式，本批不实现最终计分。

## Task 1：证据与纯评估

- [x] 添加Core/Rules/VictoryDefinition.cs、VictoryEvidence.cs、VictoryService.cs及Tests/EditMode/VictoryTests.cs，可编译桩先RED，再GREEN。
- [x] 用独立预期验证12格矩阵、19+2-3=18、完整独立事件达线、军备延后/两边扣留CF003、核战责任、只读拷贝、溢出/错类型/跨定义拒绝。
- [x] 测试结果无手牌ID、无Unity依赖；文档明确不承担宿主防重或终局吸收。

## Task 2：共享加载与同步

- [x] 添加Infrastructure/DataLoading/VictoryDefinitionLoader.cs与LoaderTests；真实数据和恶意/损坏结构先RED/GREEN。
- [x] 不在代码复制12行胜者常量；修改合成测试数据能改变对应结果，官方共享数据原样保留。
- [x] 更新模块/数据JSON、catalog、Wiki、README和Worklog；核对路径/符号/描述/status与真实XML。
- [x] 一次整批独立审阅通过，无重要/次要问题；常规Git同步按已授权流程执行，结果以实际远端核验为准。无UI改动，不重建播放器。

## Review Focus

1. 军事候选不能绕过双边审核；旧FAQ先胜免审未复活。
2. VP先净算再判胜，不按奖分顺序或显示轨道截断；溢出不返回半次结果。
3. Pending来源/定义绑定，未采用行明确暂停；不凭null赢家猜平局。
4. 核战只用原降级计划的责任；无手牌内容泄漏。
5. 当前结果是纯决定而非已提交的GameResult；完整最终计分/特殊卡终局/吸收态/防重仍待集成。
