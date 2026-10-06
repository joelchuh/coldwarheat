# 地区计分服务设计 v0.1

> description: 查询国家控制、地区存在/支配/控制、战场与邻接奖励、东南亚特殊计分、最终六地区汇总和中国牌附加分的服务边界。
> 状态：2026-09-22 设计稿；规则参数已核对，国家目录与首批计分修正参数已建立，C#服务与完整逐卡修正未实现。
> 入口：[模块描述](../Descriptors/region-scoring.module.json)、[地区参数](../../Data/Rulesets/deluxe-2015/region-scoring.json)、[卡牌计分参数](../../Data/Rulesets/deluxe-2015/scoring-card-parameters.json)、[验收案例](../Design/scoring-space.cases.json)。

## 1. 选择与接口

采用纯计算的 RegionScoringService，加 FinalScoringService 汇总。前者只计算一个地区的双方结果，后者检查完整地区集合与附加来源，最后交 VictoryService 判胜。相比把总分判断写进各地区类，这样不会在最终计分的中途提前结束；相比每个地区复制算法，六大地区共用 StandardRegionScoring，东南亚使用 WeightedCountryScoring。

| 待实现入口 | 输入 | 输出 |
|---|---|---|
| CountryControlRules.GetController | 国家稳定度与双方影响力 | US / USSR / None，复用既有控制规则 |
| RegionScoringService.Calculate | RegionId、不可变计分快照、规则定义、已核实修正 | RegionScoringResult，包括双方档位、分项、净VP和欧洲控制信号 |
| FinalScoringService.BuildPlan | 回合末已完成的状态、规则数据、计分提供者 | 确定的地区ID集合与附加项计划 |
| FinalScoringService.Summarize | 完整结果集合、附加分、开始时VP与绑定信息 | FinalScoringResult，交给现有胜负服务 |

这些是拟定符号，entrypoints仍为空。Core不读文件、不查数据库、不引用Unity。加载器将JSON转为不可变定义；会话及RuleEngine负责命令版本、事务和存档。

## 2. 一次计分使用一个事实快照

ScoringContext包含ScoringId、SourceCardId、Mode（Normal或Final）、RulesetVersion、DataDigest、StateVersion、已核实效果上下文。预览使用单独的只读查询，不生成可提交的计分命令。地区、国家与规则逻辑通过稳定ID关联，图形位置和显示名称不参与计算。

先完成来源事件要求的选择/影响力变动，再建立ScoringSnapshot。不能在选中计分牌时就冻结未来结果。快照同时覆盖双方；未受控制的国家也必须存在，不能把漏载国家误认为中立。普通国家控制依R2015 §2.1.7：自身影响力至少达到稳定度，且领先对手至少该稳定度。

BuildRegionFacts按地区成员集合去重，读取CountryControlRules结果，得到每方：总控制国数、计分用战场控制数、非战场控制数、邻接敌方超级大国的控制国数，以及本地区有效战场总数。东/西欧重叠标签中的同一国家只计一次。东南亚是亚洲子区，正常亚洲计分包含它；中国内战可选区域不进入基础国家集合。

计分专用修正以显式策略改变ScoringFacts或某项奖励，必须带SourceEffectId与规则依据。不能修改CountryDefinition.IsBattleground或真实影响力。比如计分时增加战场身份与政变时是否降DEFCON是不同问题。台湾决议、穿梭外交的首批策略已核对，见scoring-effects-batch1；真实服务与其他逐卡策略仍须实现；活动效果存在但对应策略缺失时阻止结算，不能默认忽略。

## 3. 地区档位与公式

在标准地区中，按Control → Domination → Presence → None选择唯一档位；档位分不叠加。定义T为本方总控制国数、B为本方战场控制数、N为本方非战场控制数、BT为地区有效战场总数，另一方用opp表示。

~~~text
Control:    T > T_opp 且 B == BT
Domination: T > T_opp 且 B > B_opp 且 B >= 1 且 N >= 1
Presence:   T >= 1
None:       其他情况
~~~

基础正式地区必须有有效战场；缺失战场集合属于数据错误，不能用空集合“全部满足”获得控制。正式逐卡策略若改变事实，须提供完整一致的结果。

| 地区 | 存在 Presence | 支配 Domination | 控制 Control |
|---|---:|---:|---:|
| 欧洲 | 3 | 7 | 自动胜利 |
| 亚洲 | 3 | 7 | 9 |
| 中东 | 3 | 5 | 7 |
| 非洲 | 1 | 4 | 6 |
| 中美洲 | 1 | 3 | 5 |
| 南美洲 | 2 | 5 | 6 |

数值读取region-scoring.json，表格只是便于审阅的展示。依据：R2015 §10.1与其地图图示、GMT产品页M-DELUXE地图，欧洲/亚洲/中东另核对C-DELUXE-EN英文计分卡图。

~~~text
sideVp = baseVp[tier]
       + controlledBattlegrounds * battlegroundBonus
       + controlledCountriesAdjacentToEnemySuperpower * adjacencyBonus
       + verifiedCardAdjustments
netVpForUS = usVp - ussrVp
~~~

一个国家既是战场又邻接敌方超级大国时，两种奖励可以同时出现；同种奖励按国家去重。邻接查询使用规则地图的超级大国连线，不能依据真实地理距离。欧洲Control使用独立EuropeControlWinner，不对null基础分做算术，不用100分或其他大数模拟胜利。

例如中美洲：USSR控制古巴、海地、多米尼加，US只控制危地马拉；USSR为支配，基础3+战场1+邻接1=5；US存在得1，净值向USSR移动4。两个结果属于同一VP批次，不能分两次触发普通胜利线。来源R2015 §10.1.2示例；这些国家现已在正式地图定义中登记；该例仍是规则说明，不是已运行的业务测试。

## 4. 东南亚是独立算法

东南亚计分不使用三个档位，也不额外叠加标准战场或邻接奖励。只对实际控制的指定国家按权重求和：泰国2，其余六个规则区域各1（缅甸、老挝/柬埔寨、越南、马来西亚、印度尼西亚、菲律宾）。老挝/柬埔寨是一个游戏区域，不拆成两个计分对象。

正常亚洲计分时，以上国家仍按亚洲标准规则参与；不能把东南亚权重再叠加到亚洲分数。东南亚计分牌事件正常完成后移出游戏，移出标记从正式cards.json的card.southeast_asia_scoring读取（旧参数条目已迁移删除），卡牌位置仍由CardDispositionRules处理。地区计算器本身不负责弃置或移出卡牌。来源：M-DELUXE地图、R2015 §2.1.2、§7.2、§10.3.2。

## 5. 最终计分与胜负服务

FinalScoringService从地区参数中选择include_in_final的标准地区，得到六地区集合；不另存第二份可失配列表，也不另算东南亚。末回合边界读取回合规则包，而非在各服务中复制回合数常量。

流程：确认回合末军事审核及合法回合末能力已结束 → 冻结最终快照 → 计算全部地区 → 收集中国牌等已核实附加项 → 完整性检查 → 合并当前VP → VictoryService.Evaluate(FinalScoringCompleted)。这不是实际再打出六张卡，不移动计分牌、不触发“卡牌打出”类钩子。

中国牌持有者在第10回合的最终计分时获得1VP，读取scoring-card-parameters.json中的card.china.final_holder_vp；ChinaState.Owner是依据，不查普通手牌。该项绑定到整个最终计分批次，不能因为附加分暂时达到普通胜利线而跳过其他地区。英文卡图#6与R2015 §10.3.2共同作为依据；不在两个模块各奖励一次。

RegionScoringResult与FinalScoringResult保留分项证据、ScoringId和绑定版本。任一结果缺失、重复或来自不同状态快照，全部拒绝提交。先产生完整计算结果，再在一个事务中应用一次净VP和终局；恢复后按ScoringId去重。若有欧洲控制，向胜负服务传递该信号；否则全部汇总后按正负/零判US、USSR或平局。

~~~mermaid
flowchart LR
    A[规则地图与同一状态快照] --> B[控制权与计分事实]
    B --> C{地区类型}
    C --> D[标准地区档位与奖励]
    C --> E[东南亚控制国权重]
    D --> F[双方分项与净VP]
    E --> F
    F --> G[普通批次或最终汇总]
    G --> H[胜负服务]
~~~

## 6. 数据完整性、预览与验收

正式国家定义已录入并与本文件的地区/东南亚权重引用核对；完整规则包尚未冻结。设计案例可以使用明确标为fixture的合成快照，真实对局仍须通过全部初始化门禁。CountryId、RegionId、超级大国邻接与活动效果交叉引用全部校验。图例、译名或美术风格改变不修改计算结果。

预览只使用该玩家可见的当前地图、公开生效效果与当前规则；不能因对手手牌中是否含计分牌而改变输出。预览不消费事件、不改变VP、不移动卡牌、不生成正式ScoringId。执行时仍须根据最新StateVersion重新验证。

对应案例SC-01起，见[验收设计](../Design/scoring-space.cases.json)。这些是待实现的业务案例，包含档位临界、总数平手、欧洲控制、双重奖励、东南亚、最终汇总、数据缺失和幂等。下一步初始布置、逐卡计分修正与运行服务完成后，再把它们转成运行测试。

[太空竞赛设计](space-race.md) · [返回索引](index.md)

[动态计分效果](scoring-effects-batch1.md)先重分类台湾，再由US选穿梭排除国。排除只从USSR总国/战场/敌邻接事实扣除，Control用战场全集不删除该国；实体消费与整个VP批次同一提交。预览不消费，Final仅台湾规则适用。
