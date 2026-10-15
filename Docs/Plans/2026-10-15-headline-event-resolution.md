# 有限头条事件根与连续结算

description: 在已验证的秘密选择/普通排序之上接七张计分牌及现有十二个事件。实施计划，当前用户持续开发授权有效。Spec: Docs/Architecture/headline-runtime-integration.md；采用executing-plans逐项TDD，整批一次独立审查。

## 约束与接口决策

- 不伪造普通行动计数或临时把头条放回手牌。保留真实PlayedBy，使用同一GameState和既有事件服务；每个头条单独进入Resolving，另一张仍保留。
- 新增HeadlineResolutionState：只读两张已公开普通顺序、TurnNumber、CompletedCount、Current；内部Advance验证当前实体后前进一次。当前批不实现Defectors/嵌套/事件授予OPS，运行支持集合从已注册效果及计分服务推导，所有未支持牌在Begin前拒绝。
- ActionPracticeState追加可选Headline字段及HeadlineReady阶段。Actor在未完成头条时取Current.PlayedBy，普通计数不变。保留历史完整队列到下一回合；普通行动继续使用既有日历。构造器校验未完成头条仅能处于HeadlineReady/ResolvingEvent/终局Complete，计数为0，无OpponentOps或期末报告。
- HeadlineResolutionRules.Begin(GameState,HeadlineOrderParameters)仅接受已经保留两张、尚未开始普通行动的权威稳定状态；生成队列后Advance到选择等待/完成/终局。真实Host阶段认证和秘密选择状态连接留后批，不暴露玩家SkipPhase。
- HeadlineResolutionRules.Advance(GameState)只推进HeadlineReady，无选择时在同一事务最多两张；CompleteChoice(InfluenceEventTransition)先复用既有完成服务，再继续后续牌，技术错误不返回部分NextState。
- 结果为权威HeadlineResolutionTransition(Error,NextState,Steps)，Steps有界保留每次处理的Card/PlayedBy及既有事件结果，未来Host投影不能发送完整GameState。
- ActionPracticeRules原有事件/计分分支抽出已校验的效果执行入口；普通路径保留原计数和版本行为。Headline路径只复用此入口、单一移动与完成规则。
- Next分派根用途：头条只推进Headline.CompletedCount，普通行动才增CompletedActions。两张完成进入ChooseCard、苏联行动1。第一张终局则保留当前Resolving和后张Reserved，按既有试玩审计策略不清理/不消费普通名额。
- Move用明确来源区(Hand/HeadlineReserved/Resolving)区分，不用移回手牌来欺骗旧函数。头条派发和现有普通动作门禁分离；操作预算始终0。
- 事件执行ID以回合+头条出牌方建立，与practice.event普通根不同；OPS效果激活、选择ID和胜负证据均用当前根ID。排序冻结印刷值，不随先发生的普通OPS修正重排。
- 中文/地图/UI、启动工厂与回合入口不接本批；Unity许可证恢复前只补充托管验证、源分支提交，不更新main或播放器。

## 执行任务

1. 纯队列：新增Core/Rules/HeadlineResolutionState.cs及HeadlineResolutionStateTests.cs。CreateOrdinary(parameters,catalog,cards,turn)要求两张保留牌和空Resolving，复核目录分区，冻结普通顺序；AdvanceAfterResolution(card,side)只递增正确当前游标；CurrentResolutionId按回合+出牌方构建。先RED不完整/错误分区、空输入、只读队列、逐张精确一次、错牌/错方/已完成拒绝、跨回合身份唯一，再实现。Expected: 新纯队列用例RED失败、GREEN通过。此纯游标不证明效果已执行。
2. 根身份与无选择头条：新增HeadlineResolutionRules和ActionPracticeRules.Headlines，修改PracticeState及共享Next/Move/效果入口；先验证Actor/阶段门禁/复制点，再接无选择头条。先RED两张即时、双方阵营责任、各类计分、持续效果、星号/无变化、无OPS/计数、旧值计分、首张核战/VP/欧洲终局截断、溢出原子性，再实现共享入口与有界Advance。
3. 等待选择与完成：先RED五种选择、跨双方owner、源版本/重复/错身份、后张读取最新局面、等待时源牌位置、首张完成后第二张失败不泄漏草稿、普通事件回归，再接CompleteChoice与步骤结果；更新模块JSON/Wiki/flow/证据，整批独立审查、修复阻塞项、推送。

## 待实施时核对

任务未完成前模块入口仍标planned，不能以计划冒称实现。任务2/3 Expected: 所有新增用例RED-GREEN、1933既有托管集合保持通过；Unity原生160项待许可后运行，未运行不计通过。最终检查源码hash/XML、metadata和diff，审查0阻塞后推送，main不变。现有8影响力+2DEFCON/VP+2持续OPS各自定义已共享，计分总数从CardCatalog筛选，不把19作为生产常量。不得在无新依据时修改现有用户裁定。

## 完成记录

- [x] Task1 队列
- [x] Task2 根与即时结算
- [ ] Task3 等待选择、回归、审查及分支同步

## Review Focus

牌区完整/普通计数为0；出牌方与Owner/核战责任分离；第一张终局不执行第二张；选择等待前缀不回滚、当前事务失败不发布草稿；普通路径版本和收据不变；无处理器不跳过；纯服务与Host/UI/native验收边界真实。
