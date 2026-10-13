# 牌区与初始发牌实现记录

基线ff8d2d0，feature/initial-card-deal。沿用户持续编码/Git授权，按同名计划inline执行并将做一次独立审阅。未新增正式工作脚本。

## Task 1

Core/Cards增加ChinaCardState/CardZones以及InitialDealDefinition/Result/Service。每张目录实体恰好一个位置，China独立，EnteredEras与预备区一致，集合只读。排序/洗牌/交替发牌沿用此前setup Wiki的明确工程约定；随机只在Fork上推进，结果包含整副牌与随机快照。

initial-deal-core-red：698项，674通过/24新增失败，均NotImplementedException；initial-deal-core-green：698/698。实际103牌分区、固定小牌组洗牌顺序、未改变源随机及溢出失败有覆盖。

## Task 2

InitialDealDefinitionLoader验证setup结构、身份/版本与所消费来源，只解析turn/hand引用及China。原数组首项验证防止Calendar排序隐藏指针漂移。真实数据通路通过；测试中把共享HandTarget改为6只证明引用读取，不代表采用新规则。

initial-deal-loader-red：729项，699通过/30失败；另1项是对已实现初始回合校验的补充测试，直接通过。initial-deal-loader-green：729/729，零跳过，exit0；XML SHA：07dee0a4d7ac1e86bc11ff019a041326928d694579c032ed526584bae8dbbb35。四次实际记录见验证报告。

## 边界与收尾

没有GameState/会话/UI接入，没有完整开局、牌移动、回合、头条或全局存档；静态目录与发牌结果不会绕过未完成handler门禁。纯函数不是创建请求幂等层。宿主以后共同提交牌区和随机快照。

模块JSON、相关setup数据/模块描述、目录、Wiki及工作记录已同步。729项通过；107份描述与34/67/6目录、入口/.meta已核验。独立审阅Ready to merge，无Critical/Important/Minor；按授权提交和核验远端。

Final: Ruling: 审阅没有评判宿主事务/去重/玩家投影、未消费轨道/完整RulePack、完整存档/后续抽牌和UI。这些明确属于后续集成，不把本批通过当作它们的验收；误读会造成隐藏信息或流程缺口，下一批继续验证。
