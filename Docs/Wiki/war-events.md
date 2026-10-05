# 战争事件：第一批

> description: 查询朝鲜战争、阿以战争、印巴战争的目标政策、单骰修正、固定奖励、影响力转移、阻止接口和事务流程。
> 状态：2026-10-05，3牌参数Bound；全项目25 Bound / 77 Unresolved，102个处理器均planned，C#/Unity尚未实现。
> 入口：[参数描述](../../Data/Descriptors/war-events-batch1.data.json)、[参数](../../Data/Cards/deluxe-2015/war-events-batch1.parameters.json)、[模块描述](../Descriptors/war-events.module.json)、[Schema](../Contracts/war-events-batch1.schema.json)、[案例](../Design/war-events-batch1.cases.json)、[验证](../Design/war-events-batch1.validation.json)。

## 本批三张牌

| 卡牌 | 事件受益方 | 被攻击国 | 单骰扣减 |
|---|---|---|---|
| #11 朝鲜战争 | USSR | 韩国 | 每个美国控制的邻国扣1；韩国自身不扣 |
| #13 阿以战争 | USSR | 以色列 | 每个美国控制的邻国扣1；美国控制以色列再扣1 |
| #24 印巴战争 | EventController | 玩家先选印度或巴基斯坦 | 每个对手控制的目标邻国扣1；目标自身不扣 |

修正骰≥4获胜，修正骰可低于1，不能截断回1。邻国从正式地图country_edges读取，控制按双方影响力差达到稳定度计算，仅有对手影响力不等于控制。§2.1.5的直接相邻超级大国等同该方控制邻国；本批三个目标都没有这种直接连接，不把日本连接美国递归算作韩国连接美国。韩国邻国为日本/朝鲜/台湾；以色列为埃及/约旦/黎巴嫩/叙利亚；印度为缅甸/巴基斯坦；巴基斯坦为阿富汗/印度/伊朗。显示地图的图像距离不决定邻接。

每次未被阻止而完整结算的战争，无论胜败都给受益方2军事行动。胜利时给2VP，并把目标全部对手影响力一比一换成己方：own_after=own_before+opponent_before，opponent_after=0，保留己方原有量。目标双方影响力均为0仍可打，胜利仍有VP。固定数值只定义于war_policy，不能读取printed_ops或遏制/清洗修正后的OPS作为奖励。

战争不是政变，不检查普通OPS可达性，不检查DEFCON地理门禁、不降低DEFCON。朝鲜战争旧版降级条款不适用。军事累计量沿用共享服务的实际非负总量，不发明轨道上限。VP符号及±20门槛引用共享胜负规则。

## 分层与待实现接口

WarEventService注册具名SingleRollInfluenceCaptureWar算法；JSON仅声明政策与稳定ID，不执行eval、任意操作或反射。PrepareAttempt验证内部来源和相关钩子，BuildTargetDecision负责必要选择，ResolveSingleRoll生成不可变WarResolutionPlan。拟定文件Assets/ColdWarStruggle/Scripts/Core/Events/WarEventService.cs，当前不存在；entrypoints为空，只有planned_entrypoints。

地图查询服务提供稳定度、直接邻接和控制状态；InfluenceMutation复用RemoveAll与AddExact组成CaptureAllToOwn原语，使用checked整数；MilitaryOperationsService读取WarEvent信用，VictoryService在VpBatchClosed处理完整批次。TurnFlow拥有PhasingPlayer、EffectController、父帧、事件/OPS先后和卡牌去向，战争服务不重复消耗行动轮或处理卡牌星号。UI/AI只收到玩家视角决策和只读结果，更换2D/3D/写实主题不改变规则ID。

WarContext拟保存RootActionId、FrameId、EffectInstanceId、SourceCardId/EffectId、PhasingPlayer、EffectController、RulesetVersion/DataDigest、StateVersion、TargetCountryId及选择继续位置。韩国/阿以受益方固定USSR：美国打作OPS触发苏联事件时，收益仍归USSR；印巴中立事件由出牌事件控制方选择并受益。

命名约定：本文EventController与既有回合/影响力模块的EffectController是同一身份，未来服务接口统一使用EffectController，不建立第二个控制者字段。参数中的EventController仅是“收益跟随事件控制者”的政策标签；案例event_controller表示该内部身份的测试输入。

## 阻止、选择与结算流程

1. 验证已注册处理器、参数引用、内部事件身份、数据版本及终局。核对完整活动效果集合与相关CardPlayed钩子；未知战争修正或未支持Flower Power组合返回RequiresRuling，尚无完整Flower Power处理器，不能假装只算本牌奖励。
2. 阿以查询已提交的Camp David事件成功事实，沿用treaty_protection/event_fact_policy的成功Outcome集合。事实必须由权威服务生成并带实例/版本/来源；卡牌弃置/移出位置或OPS出牌不是成功事实。已发生则BlockedPrerequisite，零骰、零军事、零战争VP，父OPS仍按§5.2继续。历史不完整停止。Camp David完整加影响力/VP事件仍Unresolved；此处只核实阻止关系。
3. 固定目标直接准备；印巴创建ChooseOne决策，候选恒为印度/巴基斯坦，即使无对手影响力。选的是被攻击国，另一个是叙事攻击方。等待时不抽骰、不记军事；响应验证身份/DecisionId/Revision/StateVersion及目标，禁止玩家提供骰点、修正或收益。
4. 在当前合法版本上冻结目标及控制修正；仅权威IRandomProvider抽一枚六面骰。禁止掷完更换国家或把骰点低于阈值重掷。完整单次结果包含2军事，以及胜利时的转移和2VP。失败也是ResolvedChanged，因为军事改变。
5. 关闭该战争语义VP批次，再查终局；终局吸收后不继续父OPS/后续事件。WAR1-ENGINEERING-01是原子提交次序约定，依据既有完整VP批次原则，不冒称FAQ专门规定了物理标记顺序。若外部已核实钩子在同一批次还产生VP，必须由批次拥有者提供完整清单；本批参考模型不支持任何这种额外VP。
6. 活着则返回父帧、标记事件完成；CardDisposition读取唯一星号：朝鲜战争正常发生后移出，阿以/印巴弃牌。被阻止不冒充成功事件。去向、重复命令、回放由既有运行框架完成。

~~~mermaid
flowchart TD
 A[核对来源/效果/历史] --> B{阿以被戴维营阻止}
 B -->|是| X[BlockedPrerequisite 返回父帧]
 B -->|否| C{需要选被攻击国}
 C -->|印巴| D[等待已认证玩家选择]
 D --> E[验证版本与目标]
 C -->|固定| E
 E --> F[冻结修正 单骰]
 F --> G[固定军事 胜利转移与VP]
 G --> H[关闭完整VP批次 判终局]
 H --> I[终局或返回父帧/卡牌去向]
~~~

等待选择保存类型化目标/Step/DecisionRevision和来源摘要，不保存委托或Unity对象。随机消费、状态增量、实例完成、去重结果及领域事件在同一事务提交；异常回滚。终局、非法响应或未知相关效果不消耗随机状态。实现这些保证的C#代码和运行测试尚未存在。

## 来源、验证与边界

英文卡图PDF p3 #11/#13、p4 #24、p10 #65；2015规则§2.1.5/7、§5.2、§7.6、§8.2.3/4、§10.2/3；FAQv5 PDF p3–5的二版/Deluxe相容说明。Flower Power边界参考FAQ p13 #59，但完整钩子仍未绑定。本批未覆盖Brush War或Iran-Iraq War，不能套默认目标/奖励推断其规则。

案例把数据负例、固定骰参考投影与planned运行验收分开。参考模型假设内部身份/事件事实经过验证，仅检查规则算术与停止行为；不证明真实鉴权、随机持久化、命令幂等、存读档或C#/Unity。旧案例只适配全局绑定数及来源状态，保留业务夹具。经互会短缺和重复激活等旧待裁定边界保持原状。

第二批已补齐Brush War与Iran-Iraq War，详见下方入口；下一步设计战争CardPlayed钩子及其持续/取消效果。

战争补充：[Brush War与两伊战争](war-events-batch2.md)已完成目标/参数设计，五张战争参数Bound；北约过滤不分攻击方，直接超级大国邻接按Deluxe计算。完整战争CardPlayed钩子仍待后批。

首批验证报告保存当时23牌绑定的快照；最新第二批报告及库存见上文补充入口。
