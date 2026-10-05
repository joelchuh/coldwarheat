# 战争事件：第二批

description：Brush War的稳定度/北约目标过滤、直接超级大国邻接修正，以及两伊战争对已有战争政策的明确引用。

状态：2026-10-05，新增2牌参数Bound，五张基础战争参数均已设计；全项目25 Bound / 77 Unresolved，102处理器仍planned。入口：[数据描述](../../Data/Descriptors/war-events-batch2.data.json) → [参数](../../Data/Cards/deluxe-2015/war-events-batch2.parameters.json)；[模块](../Descriptors/war-events.module.json)、[Schema](../Contracts/war-events-batch2.schema.json)、[案例](../Design/war-events-batch2.cases.json)、[验证](../Design/war-events-batch2.validation.json)。

| 卡牌 | 被攻击国 | 胜利阈值 | 每次合法结算军事 | 胜利VP |
|---|---|---|---|---|
| #36 Brush War | 稳定度1/2且未受北约保护的国家 | 修正骰≥3 | 3 | 1 |
| #102 Iran-Iraq War | 玩家先选伊朗或伊拉克 | 修正骰≥4 | 2 | 2 |

二者均由EffectController选择/受益，仅扣每个对手控制的目标邻国，不扣目标自身；直接相邻对手超级大国额外扣1。胜利保留己方已有量并一比一转移目标全部对手影响力。零影响力也合法；失败仍给军事。DEFCON、OPS修正、随机事务、完整战争结果VP批次及父帧职责沿用[第一批](war-events.md)。

## 复用方式与差异边界

Iran-Iraq已逐卡核实四阈值/二军事/二VP，显式引用war_events_batch1/war_policy，不重复静态数字。BrushPolicyVariant只声明3个已核实差异，其余字段引用同一基准；ResolveVerifiedPolicyVariant拟只接受该固定类型及指定字段，不做通用JSON深合并、不加载可执行表达式。注册算法仍为SingleRollInfluenceCaptureWar，扩展目标政策为ChooseFromMapQuery及北约门禁查询，未新增C#源码。

Brush合法候选先从world.json稳定度筛选，再调用ActionRestrictionService评估EventAttack/源卡brush_war：北约生效且当前美国控制的欧洲国家，不论美国还是苏联攻击都禁止。欧洲包含加拿大/土耳其；不过候选仍受稳定度筛选，例如加拿大稳定度4，不能因地图在欧洲就放行。法国/西德例外按共享条约政策动态查询，但这两国稳定度3/4，本批目标筛选已排除，不能凭有例外便攻击。北约未生效或目标非美控则无该保护；失去/恢复美国控制必须重新查询。

无北约保护不等于全部合法：稳定度、状态版本、其他已核实相关限制仍必须通过。美日安保只阻止苏联政变/调整，不泛化为战争免疫；日本本身稳定度4，因此Brush不能选它。DEFCON2不阻止战争，也不因战场目标而降级。活动效果或相关钩子未知停止，Flower Power目前仍未绑定，不能忽略其潜在处罚。

邻接只计算直接边，不递归沿邻国连接超级大国。Deluxe §2.1.5与FAQ p8明确应计算超级大国，排除旧版不计算的裁定。例如苏联攻击墨西哥，对手美国直接相邻扣1；美国攻击阿富汗，直接相邻苏联扣1；己方超级大国相邻不扣。墨西哥还邻接危地马拉，美国控制危地马拉时另扣1，多个修正累计。目标本身对手控制不额外扣。

## 选择、停止与返回父帧

PrepareAttempt核实内部帧/数据摘要/完整相关效果集合 → 确定战争政策 → 构造并过滤目标 → 创建PendingDecision → EffectController提交被攻击国 → 重新验证StateVersion与候选 → 固定目标与修正 → 单骰 → 完整军事/胜利转移/VP结果 → VpBatchClosed → 终局或统一卡牌去向/返回父帧。

非法稳定度、被北约保护、旧版本、错误身份或选择前骰子均零副作用。候选为空时当前RequiresRuling，不给军事、不抽骰，也不制造不可回答决策：未发现专门说明零合法目标的官方边界，不自创免费奖励规则。正常地图无相关异常时候选不会为空。此保守门禁单列运行待核实验收；后续如找到权威依据再更新版本。

两伊固定二选一无需最低己方/对手影响力，不能在掷骰后改目标。军事实际累计不截到印刷轨道最大值。Brush无星号正常发生后弃牌，两伊带星号正常发生后移出；即使失败已记军事也算发生。去向仅从cards.json读取。

来源：英文卡图PDF p7 #36、p15 #102；2015规则§2.1.5/7、§7.6、§8.2.3/4、§10.2/3；FAQv5 PDF p8 #36及p3/p5 #21，使用其Deluxe反转后的超级大国裁定。类型化变体及完整结果提交为工程约定，非专门额外规则裁定。

案例区分封闭参数负例、固定骰/候选投影和planned运行验收。内部身份/活动效果/卡牌钩子可信性是假设，离线模型不证明真实鉴权、随机保存、命令幂等、存读档或Unity。后续持续计划见[批次JSON](../Design/rules-design-roadmap.json)，下一批补齐Flower Power、Camp David Accords与An Evil Empire。
