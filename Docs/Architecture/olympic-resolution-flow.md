# 奥运会选择与续跑核心

> description: 独立的可信事件编排，绑定唯一结算牌，锁定对手分支，并逐对产出可提交的平手续跑快照。

用户目标是逐步扩充现有可玩 Demo，保持规则独立于画风；已授权自主设计、编码和常规 Git 同步。本批先完成可单独原生测试的状态组件，下批接 GameState/Host/UI 与事件 OPS。直接循环掷到胜负会占用界面；把重掷交给玩家会产生规则错误；选择不可变状态和每对继续令牌。

## 边界

Core 的 `OlympicResolution` 只接受可信父编排。`Begin` 接受已在唯一 Resolving 区的正式 #20、同一 CardCatalog、OlympicEventState、gameId、resolutionId、revision、正常根/头条来源和 Sponsor/rootPhasing。当前只支持正常根/本牌头条，因此两角色必须相同；特殊嵌套拒绝。父编排还须验证真实 GameState、回合/帧、持牌与触发资格；本组件不把一个 CardZones 参数当成玩家权限。完整外部来源摘要/持久化契约暂不声称实现。

初始 Stage=AwaitingOpponent，PendingOwner=Sponsor 对手。玩家适配层只传已绑定身份、decisionId、expectedRevision、Participate/Boycott，不传骰值。Respond 验证后：参赛与第一对完整双骰一起产出新快照，第二骰异常保留原选择；平手变 Rolling、清除玩家选择、固定分支；比赛胜负变 Completed。Boycott 原子降 DEFCON，核战直接 Completed，否则 AwaitingOperations，只有 Sponsor 可明确 Decline。Offer 仍只是参数，不执行 OPS，也不推进源牌/根。

## 续跑与原子边界

`OlympicPairReceipt` 是内部只读链：Previous、RoundIndex、CommittedRevision、Plan；每次只增加一个结点，不反复复制全部历史。名字中的 CommittedRevision 是父编排提交该计划时的预期版本；产出快照本身没有外部提交。旧快照始终有效且不可变。

每个 Rolling 状态产生唯一 `OlympicContinuation` 对象；ContinueOne 必须收到当前令牌（对象身份绑定），并在抽骰前检查版本溢出。旧令牌/其他事件令牌不能推动新状态。ContinueSlice 为惰性 IEnumerable，最多产出 maxPairs 个完整快照；每次 MoveNext 才计划一对，父编排必须先原子提交已返回快照再请求下一对。发生后续故障时，已返回/已由父提交的快照保留，失败对没有输出。片段预算只限制工作量，不能限制规则重掷总次数。工厂异常不吞掉；dispose 不执行剩余骰。

为当前纯内存试玩采用内部构造、不可变链和对象令牌；不冒充旧完整设计的 canonical JSON/SHA 恢复链、存档、重放或命令幂等。Host 已有幂等机制，接线时须绑定原请求；禁止在玩家 API 暴露 ContinueOne/ContinueSlice。

## 数据与验证

数值全部复用已验证 OlympicEventService、正式奥运会/D6 参数、共享 VP/DEFCON/PCG，不复制奖金或 OPS 常量。无需新规则解释，OG1-OPEN-01 保持未核实。

测试覆盖真实 CardZones 分区/卡目录绑定，双方角色，错误来源/身份/决策/版本/枚举，首对失败仍可抵制，已平手不能改选，旧继续令牌拒绝，三对分片一致，后对故障保留先前快照，真实 PCG 重建，抵制与明确放弃，核战/VP终局及版本溢出前置。无 Unity 场景依赖，不构建新 Player。

规则来源沿用 Docs/Wiki/olympic-games.md：GMT R2015 §8.1.3、§7.4.3，英文 Deluxe #20；原子性与惰性预算属于工程设计。
