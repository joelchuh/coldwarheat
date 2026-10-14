# 对手事件与影响力 OPS 顺序编排

> 使用executing-plans同一实现者分批测试；最后一次新独立全分支审查。用户持续开发/常规Git授权优先于重复批准节点。

目标：让测试者用已实现的对手阵营牌完成Event+影响力OPS；行动方明确选先事件或先OPS，等待对手选择不推进根，双方信息与画风接口不变。

架构：Core新增不可变OpponentOpsSequence，保存Card/Actor/Order/CompletedSteps，EventSide派生对手、CurrentStep派生Event/Operations/Complete；仅可信状态持有，客户端只提交两个不同用途的CardActionKind。ActionPracticeState可选Sequence；普通直接Event/OPS路径保持原行为。复用现有纯影响力/DEFCON/Flat服务，不复制卡牌数值。UI接公开进度和会话返回值，MapFrame不变。

依据：Docs/Wiki/turn-flow.md §5-6，2026-10-14重新打开GMT2015规则§5.2。对手执行其事件，PhasingPlayer保持根操作方；事件与OPS按选择顺序完整串行。星号在事件发生后、两步都结束时移出；无变化也算发生。未接前置/取消事件不可当无效果放行。本批只能选择已注册对手事件＋Influence；Coup/Realignment+对手事件后续复用同序列，不能混入。

接口/不变量：
- OpponentOpsSequence.Create(CardId,PlayerSide,OpponentEventOrder)；Advance(CardResolutionStep)只接受当前步，完整后再推进拒绝；EventOccurred由顺序/游标推导。
- CardActionKind追加EventThenInfluence/InfluenceThenEvent；CanPlayOpponentInfluence基于真实手牌、ChooseCard、只对手阵营且事件已支持。未知/自己/中立/计分/中国牌拒绝。
- 开始一次Hand→Resolving，Stage进入第一步，CompletedActions不变；下一步需要的预算/可达性在该步开始时用当前局面计算。实体保留至两步完成。
- BeginOpponentEvent仅包装已注册纯服务。影响力保留PhasingPlayer=rootactor，Owner仍effect.Beneficiary；CompleteEventAction按Sequence续接；DEFCON服务的controller=EventSide、phasing=rootactor，核战/VP终局保留Resolving和根计数、禁止剩余OPS。Flat实例跨步骤保持。
- FinishInfluence若有Sequence则AdvanceOpponentStep(Operations)，之后执行事件或完成；普通Finish不变。拒绝收尾保留此前已提交投放；本命令草稿/溢出全部不提交，无RNG消耗。
- IGameSession不新接口，CardActionResult返回原始Event/DEFCON/Flat结果，PublicPracticeView附只读进度。两个开始用途分别参与SameContent；重复命令返回旧收据不二次执行。
- UI当前已支持对手牌显示“事件→影响力”“影响力→事件”；保持太空/直接Event，解释事件选择方；Placing阶段提示仍有/已完成事件；正常OPS门禁不放开对手政变/调整。

任务与门：
- [x] T1 RED/GREEN：序列两种顺序/固定身份/完整后拒绝/无误步；规则开始/结束/未知事件/错方/牌区/根只加一。Nasser前后顺序影响最终埃及数值；Containment先事件仍按实际USSR报价；Suez选择Owner与根不同，错误回应不推进。新增OpponentOpsSequence.cs与ActionPracticeRules.OpponentOps.cs，最少改Finish和事件收尾。
- [x] T2 RED/GREEN：Host重放/改顺序ID冲突/旧版本；公开进度/Controller/地图合法目标；真实发牌Duck Event+OPS，两个顺序的核战/VP终局截断；选择提交失败及收尾溢出保留前缀；原1815回归全通过。
- [x] T3：Demo界面两顺序入口/状态提示，独立OpponentEventPlaytest，同38数据装配；JSON摘要/Wiki/flow/报告/README/AGENTS更新。构建/源码哈希/载荷/初始渲染与鼠标分开。
- [x] T4：单一新最强reviewer全分支；Critical/Important一次RED/GREEN修复，Minor记账；常规ff合并push核验。

裁定：不增加特殊前置缺失的“自动跳过”假实现。保持原单回合终局审计策略（终局卡留Resolving且根不推进），未来整局事务规范另行迁移。两类命令显式表达顺序，避免仅在客户端临时变量记录继续位置。此有限序列先不承诺嵌套出牌、暂停存档、事件授予OPS、头条/回合末。

附带界面/工具修正：将上批重复Editor菜单改为唯一Realignment菜单并验证实际构建日志无重复注册；事件等待收据按AwaitingChoice显示等待，避免写成事件完成0项。两项是本批明确范围，不改变规则裁定，不为简单文案/菜单字符串新建镜像测试。
