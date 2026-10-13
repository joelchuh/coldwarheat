# 权威时代上下文与东欧动荡

> 使用 executing-plans 原位执行、test-driven-development 验证，末尾独立审阅。用户已授权持续优化，规则沿用已确认设计，不新增裁定。

目标：把共享回合表转为只读日历，由宿主创建的 GameState.Turn 提供当前回合；东欧动荡读取该时代并复用国家集合选择事务。缺失上下文拒绝，不默认早期。

依据：Docs/Wiki/turn-flow.md、early-influence-batch2.md 的 EI2-RULING-01。数值来自 Data/Rulesets/deluxe-2015/turn-flow.json 和第二批事件参数。纯 Core 禁止 Unity、文件和随机源；使用 C#9/Newtonsoft/现有 NUnit，不新增依赖。

设计范围：TurnCalendar 只查回合的时代、手牌目标和基础行动数；GameTurnContext 不包含阶段转换、已完成名额或发牌状态。GameState 可显式无 Turn 以保留旧人工练习，时代事件必须有 Turn。旧 Apply/WithInfluenceEvent 必须保留同一不可变对象，公开玩家视图允许读回合信息。客户端命令不接受 Era/TurnNumber。完整TurnFlow仍为planned，不能凭该日历绕过开局或回合末服务。

实现文件：Core/State/TurnCalendar.cs（GameEra、EraTurnParameters、GameTurnContext、TurnCalendar）；Infrastructure/DataLoading/TurnCalendarLoader.cs；Core/Effects/EraCountryRemovalEffect.cs；Infrastructure/DataLoading/EraRemovalCatalogLoader.cs。修改 GameState、SessionContracts、InfluenceEventRules/Plan/Flow；BudgetedRemovalCatalogLoader抽出内部文档/来源核验供同批参数复用，旧Load仍只返回原两牌。

接口：TurnCalendar.ForTurn(int) -> GameTurnContext；TurnCalendarLoader.Load(string) -> TurnCalendar；EraRemovalCatalogLoader.Load(string json,string schema,MapDefinition map,IReadOnlyCollection<string> knownEffects) -> EraCountryRemovalEffect；新效果包含Region、TargetCount和只读AmountByEra。查询与确认沿用InfluenceEventRules.Query/Plan及旧EventCommandEnvelope.Resolve。

- [x] 日历RED/GREEN：实际1/3/4/7/8/10边界与手牌/名额；非法回合、区间洞/重叠/倒置/未知时代、错误版本/字段类型；复制只读定义，旧局面不自动猜回合。先测试+未实现签名，再运行唯一RunId，最后实现。
- [x] 事件RED/GREEN：真实参数/地图；候选仅正USSR东欧含奥/芬，够3选3、不足选全部、零候选无选择完成；早/中1晚2且单国不足不转移，允许选较低量；错误身份/过期/重复/越界/伪造数量原子拒绝。缺少时代上下文明确MissingTurnContext。增量按CountryId排序、溢出不提交。
- [x] 会话测试：苏联触发美国选，重发同收据，等待保留同一Turn，OPS/事件完成也保留，正常隐藏手牌边界不变；公共快照仅增加回合只读值。旧295项全部回归。
- [x] 更新模块/数据描述与Wiki/报告/工作日志，标明日历子集和完整状态机区别；独立审阅、必要修复、常规Git同步。

重点风险：时代与卡牌印刷Era混用、复制状态丢Turn、第三牌source/era_source未经验证、构造器接受不完整或可变字典、借新增类型改变原两牌加载行为。每项对应上述边界测试。本批不改Demo场景或构建；通过后再接入可见练习。
