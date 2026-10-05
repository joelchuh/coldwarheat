# 初始布置与开局流程 v0.1

> description: 查询固定影响力、自由部署范围/顺序、发牌与中国牌、初始轨道、幂等恢复及首回合衔接。
> 状态：2026-09-28；官方基础开局数据与流程设计已建立，SetupRules/SetupFlow尚未实现，不能启动真实对局。
> 入口：[数据描述](../../Data/Descriptors/setup.data.json)、[模块描述](../Descriptors/setup.module.json)、[唯一数据](../../Data/Rulesets/deluxe-2015/setup.json)、[Schema](../Contracts/setup.schema.json)、[验收案例](../Design/setup.cases.json)。

## 范围与选择

按已选R2015 Deluxe基础规则，不启用竞价/让子、US额外影响力、中国内战或Turn Zero。来源为官方规则书第4页§3.1–3.4；子区见§2.1.2，首回合见§4.5A–B。官方事实与数字端交互约定分别说明。

采用独立SetupDefinition与可恢复SetupFlow，保留既有Setup.InitialDeal/Deploy→TurnStart结构。自由部署不是一张虚构OPS牌；不能借用普通AddInfluence的邻接或敌控双倍费用。单独验证部署指令，再复用事务内“增加指定方影响力”的底层原语。世界地图只记录静态属性，开局数值与对局状态各自独立。

## 官方初始布置

下表是setup.json的派生阅读摘要，不是第二个数据源。固定与自由份额相加得到总量；不再独立存一份可失配的total。

| 方 | 固定影响力 | 自由部署 | 总量 |
|---|---|---|---:|
| USSR | 叙利亚1、伊拉克1、朝鲜3、东德3、芬兰1 | 东欧6 | 15 |
| US | 加拿大2、伊朗1、以色列1、日本1、澳大利亚4、菲律宾1、韩国1、巴拿马1、南非1、英国5 | 西欧7 | 25 |

苏联先完成全部部署，美国后部署。其他国家初始双方影响力为零。范围查询来自world.json的subregion_ids，不复制国家白名单；奥地利、芬兰同时属于东西欧，因此两方均可在其上自由部署。加拿大属于游戏的西欧子区，也可接受美国自由部署。

自由份额可集中或分配，只要求投向相应子区并恰好用完；本规则没有“不得超过稳定度”、邻接前置、敌控双倍消耗或双方不能投同一国家的限制。固定影响力不被自由分配覆盖。控制权始终由现有控制规则查询，不单独写入setup.json。

先洗早期牌并各发8张，双方可先看自己的手牌再部署。中国牌正面朝上归苏联，不计入8张普通手牌，也不进普通牌库。晚些时代的牌留在EraReserve；当前基础范围来自已审核cards目录，不能把所有卡牌或可选牌混入开局。

## 共享参数与版本迁移

setup.initial_values使用dataset_id + JSON Pointer，不复制已存在的轨道/手牌数值：

| 参数 | 唯一来源 |
|---|---|
| 初始回合与手牌目标 | turn-flow的第一个时代段first_turn/hand_target |
| DEFCON | defcon.initial_level |
| 双方太空位置 | space-race.initial_position |
| 双方军事累计 | military-operations.initial_earned |
| 有符号VP | victory.initial_signed_vp |

当前解析结果为回合1、8张、DEFCON 5、双方太空0、军事0、VP 0。首时代指针必须仍指向从回合1开始的early段，并对应卡牌Early；参数文件重排不能悄悄改变开局，跨文件验证须报错。

部署顺序的唯一来源现在是setup.deployment_stages数组。turn-flow升级schema_version=3/content_version=0.3.0，删除旧initial_setup_order；TurnFlow消费者需读取新setup，不静默兼容旧字段。setup依赖turn-flow数值，turn-flow数据不反向依赖setup，模块编排可以同时读取二者。

## 状态机与职责

以下是拟实现流程，不是已经存在的函数。

| 步骤 | 输入/行为 | 完成后 |
|---|---|---|
| Setup.Validate | 检查身份、规则包版本/摘要、地图/卡牌/setup、参数引用和全部处理器 | 未就绪拒绝创建；不消耗随机数 |
| Setup.Initialize | 84国双边影响力归零；从共享参数初始化轨道；普通牌全部进入唯一所属区域，中国牌单独持有 | 保存SetupState与确定性随机状态 |
| Setup.InitialDeal | 从Early+EraDeck选牌，一次洗牌，发至目标；记录InitialDealDone及EnteredEras | 双方自己的初始手牌可见 |
| Setup.Deploy.ApplyFixed | 根据当前stage.side应用其fixed_influence，仅一次 | 保存FixedApplied并创建该方部署选择 |
| Setup.Deploy.AwaitAllocation | 当前方提交完整自由分配方案 | 验证后一次应用；苏联结束进入美国ApplyFixed |
| Setup.Complete | 双方部署完成、牌区唯一、初始数据引用全部满足 | 记录SetupCompleted，进入回合1 TurnStart |
| TurnStart | DEFCON按上限恢复、补牌到目标 | 初始已满8张，补牌为0；进入Headline.Collect |

Initialize把轨道提前建好是工程内部组织；部署完成时必须满足§3.4的官方状态。固定影响力按部署方顺序提交，避免一个“初始化所有数值”的步骤掩盖流程位置。

SetupState拟包含SetupId、DefinitionVersion/Digest、StageIndex、Step、InitialDealDone、FixedAppliedBySide、AllocationCommittedBySide、SetupCompleted。阶段决策只有当前部署方拥有；部署期间PhasingPlayer为空，不将它误认为行动轮。中国牌可用面、牌库、手牌、随机游标和去重结果都是权威快照的一部分。

~~~mermaid
flowchart TD
 V[完整规则包校验] --> I[初始化轨道与空状态]
 I --> D[洗早期牌并发牌]
 D --> H[各自查看私有手牌]
 H --> SF[苏联固定布置]
 SF --> S[苏联自由部署]
 S --> UF[美国固定布置]
 UF --> U[美国自由部署]
 U --> C[完成开局]
 C --> T[回合1开始：恢复及补足]
 T --> L[头条选择]
~~~

## 部署命令与事务

SubmitInitialDeploymentCommand沿用CommandId、GameId、ExpectedStateVersion；玩家身份由会话认证，payload含DecisionId与allocations[{country_id,amount}]。不接收客户端指定的“替谁放”、预算、规则版本或直接状态覆盖。

验证顺序：身份/阶段/版本/决策拥有者 → 非空数组、唯一国家、正整数数值 → 国家存在且在目标子区 → 求和恰等于本方自由份额 → 构建不可变增量计划 → 一次提交。固定布置已完成的事实不能因本次非法自由方案而回滚，也不能重复应用。重复国家条目直接拒绝；客户端可在本地编辑时合并，服务器不猜意图。

数字端采用“本地可编辑草稿＋完整方案一次确认”。确认前不改变权威状态，也不暴露草稿；确认后按CommandId幂等返回原结果。禁止撤销已确认的苏联部署后偷看美国方案；重新开局另建GameId。该确认规则是项目输入约定，不冒称桌游额外条款。

错误码拟定：SETUP_NOT_ACTIVE、NOT_DECISION_OWNER、STALE_STATE_VERSION、DECISION_NOT_FOUND、UNKNOWN_COUNTRY、DUPLICATE_COUNTRY、INVALID_ALLOCATION_AMOUNT、OUTSIDE_SETUP_REGION、INCORRECT_ALLOCATION_TOTAL。失败不消费随机数、不部分放置、不推进阶段。

事务同时写影响力、阶段/决策、状态版本、命令去重结果和领域事件。中途崩溃恢复到上一已提交边界；不会重复发牌、固定布置、自由分配或开启回合。恢复前校验规则包和状态完整性，不靠“影响力似乎相等”猜是否执行过。内部推进步骤也使用持久化完成标记与原子事务，不能只保护外部命令。

## 发牌确定性与秘密信息

以下为工程重放约定：先按CardId的ordinal顺序建立输入序列；Fisher–Yates从末尾向前，使用注入且可持久化的随机源NextInt(0, i+1)；从洗后序列首端取牌，按USSR、US交替发到各自目标。轮流发牌次序不是官方额外规则，而是为了固定同种子回放；随机算法版本由基础设施/存档绑定，不能只保存种子。

35张早期普通牌发出16张后剩19张；中期46、后期21留在预备区。这些数量从cards目录与手牌参数派生，只在校验案例作独立预期，不再写进setup常量。103个实体位置互斥：中国牌1＋双方手牌16＋早期余牌19＋时代预备67；弃牌/移出/正在结算区初始为空。不得为避免坏牌、过多计分牌或平衡而重发；基础规则无此开局重抽分支。

玩家快照含公共轨道、已提交影响力、当前部署方、自身手牌和合法目标；对手只见手牌数量。公共事件不得包含发出的CardId、牌库顺序、随机种子/状态、私有决策或未提交草稿。内部完整日志与客户端事件分流。本地同机通过交接遮挡界面展示手牌，规则服务仍须进行视角过滤。

同一创建请求重试返回同一会话，不能生成新种子重新开牌；权威创建身份/请求去重属于Application。查询和UI动画不触发Advance；断线不自动跳过或判负，超时策略另行设计。

## 验证边界与下一步

结构/数据验证可以读取当前候选包，但不创建游戏。当前102个非中国牌处理器仍planned；17牌参数已Bound，85牌仍Unresolved，因此完整规则包的运行门禁仍拒绝开局；不能用空事件绕过。测试未来SetupFlow时可注入明确fixture注册表，但不得将fixture启动当作正式包可运行。

验收将分别覆盖官方固定表、引用、子区、错误部署、私有手牌、随机恢复、内部步骤重复、首回合不多发牌和跨主题一致性。静态数据检查结果见[工作记录](../Worklogs/2026-09-28-setup-design.md)，C#运行案例仍为planned。下一步按小批设计早期卡牌事件参数、选择与持续效果，再结合既有行动/计分/太空服务逐步实现核心。

[回合状态机](turn-flow.md) · [地图与卡牌](map-card-data.md) · [返回索引](index.md)
