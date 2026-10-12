# 军备竞赛与峰会

本次用户裁定的当前整合验证入口：[2026-10-13报告](../Design/confirmed-rulings-20261013.validation.json)。原批次报告保留为当时版本的历史记录，不代表修订后的最新验证。
description：查询军事领先比较、六主区实际控制加成、不可变竞赛结果、双骰/VP耐久收据和胜者DEFCON继续；Arms比较轨道位置时采用用户确认上限投影，保留原军事累计。

状态2026-10-12：两牌参数契约已Bound，运行处理器均planned；没有C#/Unity实现。[数据描述](../../Data/Descriptors/contest-events.data.json) → [参数](../../Data/Cards/deluxe-2015/contest-events.parameters.json)、[Schema](../Contracts/contest-events.schema.json)、[案例](../Design/contest-events.cases.json)。

采用既有[组合事件模块](../Descriptors/composite-events.module.json)，拟定`Assets/ColdWarStruggle/Scripts/Core/Cards/Contests/ContestEventService.cs`的`BuildMilitaryOutcome`、`BuildRegionalContest`、`ResolveWinnerDecision`；文件与运行入口尚未实现。`ContestOutcome`复用不可变比较证据/胜者/VP计划，复杂条件留具名服务，JSON不执行表达式。根PhasingPlayer、实际CardPlayer、EffectController、DecisionOwner独立，内部授权不能由玩家载荷自报。

## Arms Race：只奖励PhasingPlayer

#39为中立3OPS中期非星号。当前军事轨道严格领先对手时，根PhasingPlayer获1VP；同时满足当前DEFCON对应军事义务则改为3VP，不能相加为4。相等或落后时双方均不获分；本批采用`ResolvedNoChange`表达完成比较后的零奖分，这是工程结果标签。军事、DEFCON和随机状态均不变；印刷OPS不能作军事信用或奖分。当前DEFCON取事件执行快照，不取未来回合末值。

**CE-OPEN-01已由用户在2026-10-13明确确认关闭**，来源为 `PROJECT-CE1-USER-20261013`。仅Arms Race计算双方比较位置 `min(comparison_track_maximum, military_earned)`；当前用户确认的参数上限为5，因此7对5、9对8都平手，7对4仍领先。奖分仍只给根PhasingPlayer，满足当前DEFCON军事义务时仍以3替代1。

共享军事服务继续 `StoreNonnegativeEarnedTotalWithoutInventedCap`，输入、累计账本、回合末军事考核和其它卡牌都不因此被截断。上限只存于Arms参数，脚本读取参数；现有军事数据没有同语义的共享轨道最大值，不拿DEFCON最大值或别牌军事奖励5凑作引用。未来真正新增多个同语义消费者时再提取共享定义。旧GMT示例仅保留研究依据，用户确认不是官方新版封顶条款。

卡文明确受益方为PhasingPlayer，不照抄中立事件通用EffectController政策。角色分离夹具只验证内部映射，不授权Grain Sales、Missile Envy或其他尚未核实的来源。头条/子帧仍由既有编排提供可信角色及法律来源；缺少明确授权先停。

## Summit：实际地图与两枚骰

#45为中立1OPS中期非星号。每方各一标准D6，每个达到Domination或Control的主地区加1，修正总数较高的一方得2VP；相等没有胜者、VP或DEFCON选择，不重掷。一个地区最多加1，Control不与Domination叠加；修正总数不钳到6。

六主区集合只读共享`region-scoring /regions`中的StandardRegion：欧洲、亚洲、中东、非洲、中美洲、南美洲。东南亚归亚洲，东/西欧不另给加成。通过地区模块拟定`InspectActualRegionTiers`查询同一完整84国快照；复用国家控制和地区档位谓词，基础战场身份只读地图。Presence无加成；欧洲Control只给一次骰修正，不产生欧洲计分自动胜利。不创建ScoringId，不发地区基础/战场/邻接VP，也不调用最终计分。

台湾仍按基础非战场：Formosan只限定实际亚洲计分/最终亚洲计分。Shuttle完全不影响本事件，不选排除国、不消费实例或移动Holding卡。已知无关计分修正按已核实范围排除；未知相关国家/地区/骰/VP/DEFCON策略不能默认忽略。有效OPS、战争、政变骰修正不自动迁入峰会。

## 结果先提交，再等待胜者选择

1. 验证终局、来源、卡牌/效果匹配、角色、StateVersion/RulesetVersion/DataDigest、完整地图和军事/DEFCON/VP状态，确认全部相关干预与VP提供者已知。未终局状态不得已有DEFCON1或达到普通VP胜利线。初始非法输入、未知相关效果或未解决裁定均零骰、零修改。
2. 冻结实际地图地区证据。共享标准骰定义来自`rules.deluxe2015.standard-dice`；唯一抽样顺序放在参数`receipt_policy.draw_order`，约定US后USSR。此顺序是软件确定性约定，非官方物理掷骰顺序。
3. 两枚RawD6、六地区档位、加成、修正总数、胜者或平手、完整本事件VP批次、RNG位置、来源/请求摘要与继续位置在同一事务提交。`VpBatchClosed`立即查共享胜负阈值。达到终局则完成已接受工作并停止，不创建DEFCON选择；例如US原18VP赢得峰会即20VP，DEFCON2保持不动。
4. 未终局且有胜者才等待胜者的`Pass / ImproveOne / DegradeOne`决定。Owner是ContestWinner，可能不同于EffectController和PhasingPlayer。Pass保留轨道；改善上限按共享DEFCON规则为无变化；降至核战等级立即判根PhasingPlayer败。合法自毁选项不因后果被删掉。Nuclear Subs不阻止事件DEFCON变化，FAQ明确支持。
5. 非星号去向、事件完成和返回父帧由统一CardDisposition/TurnFlow处理；等待选择中的事件不能提前完成。终局不执行后续父OPS或创额外根slot。独立规则中断必须有已核实继续合同；恢复不会重掷或按新地图重算旧结果。

初始事务失败时回滚本步尚未提交的骰、VP、RNG和收据。后续错误Owner、旧Revision、旧StateVersion、错误DecisionId或技术失败，保留已经提交的竞赛骰/VP，不回到抽骰步骤。`Finished`是吸收态。VP先于可选选择依据卡文次序、§10.3.1和既有批次终局合同推导，不冒称存在专门Summit时序FAQ。

## 严格输入与持久化边界

Schema含`$defs/arms_race`、`summit`参数，及`initial_input`、`contest_outcome`、`paired_receipt`、`defcon_response`形状；`defcon_continuation_input`把权威收据、独立当前状态和选择意图组合起来。完整对象关闭额外键，明确整数/角色/来源/枚举；胜者、奖分和平手形状一致。Schema之外还必须验证国家ID集合完整且唯一、卡牌/效果相配、同一版本、实际控制推导、两方不能同时在同区支配/控制、RNG前后游标、VP计算和终局/决定关系，不能把结构合法当规则合法。

保存RootActionId、FrameId、ResolutionId、实际来源/角色、StateVersion/RulesetVersion/DataDigest、原始请求摘要、两骰与地区证据、ContestOutcome、VP批次、RNG前后位置、收据摘要、Step、DecisionId/Owner/Revision。请求摘要对原始context/state/evidence作规范化哈希，排除摘要自身及传输CommandId；收据摘要绑定全部收据内容但排除自身。真实规范编码/散列版本需作为引擎版本合同；本离线模型使用排序紧凑UTF-8 JSON，仅用于夹具内容核对。

重复命令先按原Resolution与请求摘要查询已接受收据，再考虑当前地图，完全相同只返回原结果；同ID不同请求冲突。选择命令只含决定身份/版本/意图，不能提交替换收据、骰点、VP或胜者。收据由权威存储提供；后续纯投影另收当前状态，核对来源帧、根行动、数据绑定、版本、VP、DEFCON及RNG游标，不从损坏收据反向覆盖当前VP。现存Finished优先停止新选择；独立中断导致当前状态与收据快照不同，须由已核实继续提供者解释，否则本批停止。哈希是内容绑定，不替代鉴权。保存/恢复、并发去重、原子提交和真实IRandomProvider均未实现，不能以离线投影声称已验证这些机制。

## 来源与验收

来源：[英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)PDFp7 #39、p8 #45、p6 #35；[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)§2.0/2.1、§4.2/4.5C、§8.1/8.2、§10.1.1/10.3.1；[FAQv5](https://www.gmtgames.com/nnts/FAQv5.pdf)PDFp9 #41/p15 #73。[旧设计师示例](https://www.gmtgames.com/nnts/sneakpeek_xop.html)仅用于说明军事cap证据缺口。原始长文本/卡图不打包为发布数据。

案例分静态参数负例、完整输入/收据形状负例、手写期望的有限算术，以及另列的planned runtime。地图案例包括实际全部欧洲战场且无非战场的Control、总国数/战场数平手、非战场条件、两方混合地区、双方影响力临界、台湾/穿梭，以及双方六区最大修正。子agent独立手算地图预期，没有调用参考resolver。临时离线模型检查初始/后续纯投影与摘要一致性；没有游戏随机源、运行事务、存档、客户端视图或C#/Unity测试。

本批只增加两事件的数据与设计；军事存储、共有DEFCON/胜负阈值、正式地图和骰定义不重复修改。接缝见[风险与胜负](risk-victory.md)、[地区计分](region-scoring.md)、[动态计分](scoring-effects-batch1.md)及[陷阱标准骰](trap-events.md)。

2026-10-13修订验证：117项＝原107＋新增10（30数据形状、25输入形状、62有限参考）；4个旧overflow期望调整，其余期望保留，12运行验收未运行。新例覆盖7/5平手、双方上限投影、原累计不变与3VP即时终局；Summit规则及既有收据行为不变。
