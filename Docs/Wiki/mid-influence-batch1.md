# 中期影响力第一批

description：查询葡萄牙帝国瓦解、阿连德、萨达特与OAS的固定目标、移除/添加和中南美总额分配。

状态2026-10-06：#52/#54/#72/#70参数Bound；51 Bound / 51 Unresolved，102处理器planned，无C#/Unity。[JSON描述](../../Data/Descriptors/mid-influence-batch1.data.json)→[参数](../../Data/Cards/deluxe-2015/mid-influence-batch1.parameters.json)、[Schema](../Contracts/mid-influence-batch1.schema.json)、[案例](../Design/mid-influence-batch1.cases.json)、[验证](../Design/mid-influence-batch1.validation.json)。

葡萄牙帝国瓦解固定Angola与SE African States各加2USSR，总4；SE African States是正式单一国家节点country.southeast_african_states，不在非洲任选国家。阿连德只Chile加2USSR，不追平/确保控制。萨达特在Egypt清全部USSR再US加1；原US保持再累加，USSR零仍加1，不把卡文US1当净控制差。

OAS固定US选择中美/南美国额度总2；可一国2或两个不同国家各1，允许跨区与敌控/零己方影响国。查询正式两个地区并集，不含北美Canada或欧洲；事件不需OPS邻接、不收敌控双倍。响应CountryId唯一、Amount正整数且总和恰好2；不能选零/短缺或重复行来绕过上限。每国最大2来自总额，卡面没有“必须两个不同国家”限制；此为MI1卡文推导，FAQ明确跨区各1合法。

既有BuildPlan/ValidateSelection/RemovalAllocationRules可复用，固定变更以同一原子影响力计划提交；Sadat清除与添加之间不开放等待，不留下半清除状态。OAS完整分配在工作副本验证后一次提交，Owner=US，不变根行动者/名额。固定两牌Owner=USSR、萨达特=US，美国打苏联牌OPS也不改变受益方。非法/越权/旧Revision/未知相关状态保持整个当前方案零修改。

不改变VP/DEFCON/军事/随机，不使用 printed OPS 或世界坐标。计划、决策Revision、来源ResolutionId与完成收据由权威核心持久化，重复请求只返回原收据。星号及事件真实发生的移出由既有CardDisposition读cards.json；不是影像动画决定。没有实际C#代码、鉴权、事务或存档运行验证。

依据英文p9 #52/#54、p11 #70/#72；R2015事件归属/直接影响力；FAQv5 PDFp14 #70的and/or范围。共享地图和参数只读，数据/代码/表现层分离，更换画风不改目标或算术。未知其他影响力限制不猜为未生效。下一批更多影响力/成功前置与太空事件，继续按已核实原语展开。

主离线验证18数据＋25参考＝43新项，1222旧回归，共1265通过；25 Schema/28数据描述/19模块/86指针，8runtime未运行。38工作工具检查另层。独立审阅合入Yes；无需邻接夹具Minor改为零影响力，25参考再次独立核对、18结构局部标准库检查/159JSON/86指针/126链接/8摘要一致。未独立全1265或Schema元验证/真实运行。
