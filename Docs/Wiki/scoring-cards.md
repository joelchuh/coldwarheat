# 七张计分牌的正式绑定

description：按CardId/RegionId查询共享标准/东南亚算法、头条/强制弃牌及最终模式与实体去向。

状态2026-10-06：#1/#2/#3/#37/#38/#79/#81参数Bound；44 Bound / 58 Unresolved，102处理器planned，无C#/Unity。[地区数据描述](../../Data/Descriptors/region-scoring.data.json)→[唯一地区数据](../../Data/Rulesets/deluxe-2015/region-scoring.json)、[结构契约](../Contracts/region-scoring-rules.schema.json)、[案例](../Design/scoring-cards.cases.json)、[验证](../Design/scoring-cards.validation.json)。

## 同一份数据，两种算法

CardDefinition.scoring_region_id是本牌地区唯一事实。六张标准牌的parameter_ref直接引用regions数组内同ID定义，SEA引用southeast_asia；加载时再校验ID/算法形状一致，不以数组位置猜地区。StaticDefinitionCatalog按已冻结版本/摘要提供只读定义，CardScoringService.ResolveFromCard调用RegionScoringService，不为每牌建一套分值或if名称。

欧洲/亚洲/中东/非洲/中美/南美按标准Control→Domination→Presence计算双方总国、战场/非战场、敌邻接，奖值取单一地区数据；欧洲Control传EuropeControlWinner，不以100等大数代替。SEA只按正式七个指定国家权重，泰国2其他1，不额外加标准战场/邻接，也不另入最终六地区。国家控制与成员仍来自正式地图/权威快照，画面位置不参与。

## 来源和出牌流程

实际计分出牌（普通行动或头条）经TurnFlow验证持有/身份/当前帧，ScoringContext.Mode=Normal、ScoringInvocationKind=PlayedScoringCard，并绑定真实SourceCardId；在事件实际开始时重查局面与已知策略，先处理台湾/穿梭等合法决定，再双方一个净VP批次、去向与收据一起提交。RootPhasingPlayer/行动名额保持父流程，不能因为US选择穿梭就多开US行动。

计分印刷OPS为null、affiliation=None；OPS和太空用途拒绝。头条合法、比较值0来自turn-flow规则，不能把null OPS变可花预算，也不复制头条优先规则。双计分头条仍US先。净VP为US分−USSR分，零净差照常完成计分事件；SEA成功后按cards.json星号移出，其他六张弃置。强制弃牌（例如五年计划）不执行计分，即使SEA有星号也只是Discard；取消头条不偷做计分或星号成功移出。

最终模式复用六地区定义计算，include_in_final从现有定义派生；没有SourceCard实体被打出、不消费ScoringCard实体/新名额/CardUse，排除SEA单独计分，穿梭不适用而台湾适用。整个最终汇总与中国持有附加项一并交既有VictoryService，不能中途用普通±20截断。计分牌扣留检查仍在回合末服务，不混入计算器。

## 数据与运行边界

新增Schema仅约束已有单一数据的结构/算法形状；正值是否符合印刷事实由冻定数据/源图核对，不把Schema当规则正确性证明。业务分值、地图、七牌印刷事实均保持；仅补Schema与绑定、来源PDF页码和版本描述。R2015本地已验证官方副本p2/3为地图/卡牌、p4头条、p5强制弃牌、p8 §7.2、p10 §10；旧地区数据p18/19引用是误页，现修正。英文计分卡p2/p7/p12已目视交叉核对。

未绑定/未知计分修正、缺完整快照、错地区参数、失配SourceCard、旧决定或未注册运行处理器全部停止，不猜空国/零分或直接放行开局。本批Bound代表设计与数据绑定，真实Command/RNG/事务/隐藏信息/存档/终局仍待实现。

同时ps1规范改LF，避免新checkout自动转CRLF使已保存工具文件SHA过期；PowerShell7已按LF运行通过，脚本内容不变。下一批事件DEFCON/VP顺序与军事固定奖励，继续复用现有服务。

主离线验证17数据＋27固定参考＝44新项，1129旧回归，共1173通过；23 Schema/26数据描述/19模块/76指针，9runtime未运行。38工具实际检查独立记录。独立审阅合入Yes，模式/调用来源命名Minor已闭环；27参考独立重算、148JSON/76指针/19模块无环/169链接/9摘要核对；未独立跑17数据/元Schema/1129历史或runtime。

契约字段映射：CardPlayContext.Mode为Scoring或Headline，是本次出牌用途；ScoringContext.Mode为Normal或Final，是汇总/终局算法模式；ScoringInvocationKind为PlayedScoringCard/FinalScoring/OtherEvent，决定卡文干预资格。真实计分牌对应Normal＋PlayedScoringCard，最终汇总对应Final＋FinalScoring；预览另由只读标志表示，不是第三种Mode。
