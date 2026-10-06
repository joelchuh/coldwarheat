# 国家局面指标与直接VP

description：查询Kitchen Debates、Alliance for Progress、Reagan Bombs Libya的实际国家指标、条件与完整VP批次。

状态2026-10-06：#48 US1Mid星号、#78 US3Mid星号、#84 US2Late星号，参数Bound；C#待实现。[描述](../../Data/Descriptors/country-metric-vp-events.data.json)→[参数](../../Data/Cards/deluxe-2015/country-metric-vp-events.parameters.json)、[Schema](../Contracts/country-metric-vp-events.schema.json)、[案例](../Design/country-metric-vp-events.cases.json)、[验证](../Design/country-metric-vp-events.validation.json)。

采用既有组合事件的封闭具名指标步骤，计划CountryMetricVpEventService.BuildPlan读取当前权威国家快照、共享地图与控制规则，生成MetricEvidence和单个VP批次交编排/VictoryService，不互相修改服务。不允许参数输入可执行表达式，也不接受玩家自报计数/获分；不创建数据库或通用脚本引擎。实际事件执行时读取局面，OPS前/后导致的变化必须体现，报价缓存不替代当前状态。

| 卡牌 | 读取与运算 | 零量/条件不满足 |
|---|---|---|
| 厨房辩论 | 比较双方在全地图实际控制的基础战场数量，US严格更多才US2VP | 相等/少于是BlockedPrerequisite，Reason=NotMoreControlledBattlegrounds，事件未发生，星号弃置 |
| 进步联盟 | 中美/南美两地区并集内US实际控制的基础战场，每国US1VP | 零战场仍ResolvedNoChange，事件成功，星号移出；R2015 §5.2 Example3直接明确 |
| 里根轰炸利比亚 | Libya USSR影响力按完整2点一组，每组US1VP | floor(n/2)，0/1点仍ResolvedNoChange，事件成功；没有控制前置，不移除/转换影响 |

所有奖分固定US，不随根行动者或事件出牌方改变。厨房读取基础world.json的base_battleground，控制由影响力差≥稳定度的既有规则判断；不能只凭有影响力或缓存旗帜计数。不把台湾的亚洲计分重分类或Shuttle的临时排除合并进指标；FAQ明确Shuttle不影响Kitchen，Formosan的既有范围仅实际Asia计分。进步联盟不含北美Canada或其他地区，不按同区存在/支配/控制档位加分。利比亚哪方控制都不影响读取USSR点数，不把印刷OPS、US影响力、净差或军事值代入；奇数余1没有完整一组，整数向下取整是every2的文本推导，不冒称专门FAQ。

流程：可信帧/版本/已终局与重复收据检查→验证相关完整状态→查询指标→具名条件或数量→完整单事件VP计划/来源证据→编排原子提交VP及成功/牌区收据→即时胜负检查，终局停止余续。零量不伪造非零VP，仍按事件结果区分去向。星号策略读取cards.json，不在参数各复制remove布尔；真实牌区/鉴权/收据/事务尚未实现。此批不擅自扩大未满足己方事件用途或头条资格，外围用途策略未核实时停止。

无选择、骰子、影响力/军事/DEFCON变化或额外根名额。指标结果包含查询状态版本和数据摘要，不能在不同版本提交旧计划。相同ResolutionId与请求返回原完成收据，不在新的局面重新奖分；同ID不同内容冲突，未知相关效果/地图定义/国家快照、负或非整数影响力拒绝，当前步骤零修改。基础控制与计分特殊效果职责分离，表现层可自由更换地图样式。

依据[英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)p8 #48/p12 #78/p13 #84、[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)§2.1.7/5.2/10.2–10.3.1、[FAQv5](https://www.gmtgames.com/nnts/FAQv5.pdf)PDFp15 #73。后续可为其他已核实事件添加具名指标，当前不凭记忆补Arms Race的特殊角色语义。

主离线16数据＋45参考＝61新项、1335旧回归，共1396通过；28 Schema/31数据/19模块/93参数指针；6runtime未运行，38工具实际检查另层。独立只读审阅Ready Yes，无Critical/Important；16有限结构＋45参考独立复算，meta/1335旧模型未独立运行，两项Minor已主侧修正。
