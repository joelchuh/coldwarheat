# 最终计分试玩接线设计

> description: 将已验证最终计分核心接入权威命令、公开报告、固定状态栏Demo与可直接测试的第10回合场景。

用户已授权自主持续开发、同步Git并使用现有额度。沿用E:/TwilightStruggle、单窗口双身份、默认保密/主动观察、固定状态栏和可换IMapView。基础为93a072e；不重新询问设计批准。

## 方案

选择独立FinalScoring命令，位于军事/扣留审核之后；保留两个报告，便于解释最终VP。不要复用普通计分命令逐区判胜，也不要把最终奖励塞进军事记录。

1. ActionPracticeDefinition新增可选FinalScoringDefinition，必须启用TurnEnd并与Scoring为同一实例，日历LastTurn=10。CardActionKind尾部追加FinalScoring。ActionPracticeState新增不可变PracticeFinalScoringReceipt，允许最终VP不同于原TurnEnd.Decision.SignedVp，但严格关联原审核、BeforeVp、最终Decision与Risk.Result。旧状态不变。
2. CanCompleteFinalScoring验证第10回合、全部动作完成、无头条选择/待处理事件/牌根/部署、军事审核Continue且引用匹配、China正面、OPS已清、军事已归零、无额外行动/期末弃牌能力、手牌无计分牌。配置未启用/过早/已终局/伪造上下文均拒绝。状态/地图与China同次读取。
3. PlanFinalScoring在同一纯计划中创建ScoringSnapshot、计算最终结果、判胜并Commit一次：VP/Ended/报告/Version/EventSequence一同提交；不改随机数/牌区/国家。溢出返回NumericOverflow且不发布。既有Host收据处理同ID同内容重试；新ID再次结算拒绝。零分Ended=true而Winner=null。
4. PublicFinalScoring只含公开六区分项、ChinaOwner、Before/AfterVP、净分与胜利原因，无完整规则定义/国家对象/手牌/随机数。经PublicPracticeView获得。控制器复用SendCard的generation和重发保护。
5. Demo增加最终计分按钮/报告（固定状态栏与独立报告区域），保持地图接口。明确欧洲控制无数字净分、零分平局、其他正负胜利。正常头条场景也装配最终定义；新增独立人工第10回合练习，原15场景保留。

## 场景初始化

Core增加受限CreateAtTurn(definition,turnNumber,initialVp)用于可信场景初始状态，不允许已终局分数；不接受伪造CompletedActions/审核结果。基础Create仍从第1回合。Infrastructure从明确fixture JSON建立第10回合局面、正式目录合法手牌和China，随后通过真实StartPractice/Influence/FinishInfluence完成14个普通行动，停在军事审核前。场景准备无随机掷骰，不能以fixture冒充玩完九个回合。这样无需暴露内部可变状态或提前实现存档恢复。

## 数据与范围

原region-scoring.json和scoring-card-parameters.json仍唯一数值源。加载最终定义时复用场景现有RegionScoringDefinition，验证相等的共享参数，不生成两个不一致规则实例；必要时加载器增加绑定已有定义的重载。新demo.final_scoring_scenario JSON保存fixture国家影响力、VP、China与起始回合及description，校验ID/界限。加载/Bootstrap/Editor Build共享载荷一致。独立Builds/FinalScoringPlaytest，不覆盖旧播放器。

仍不支持台湾决议等活动计分修正、太空期末选择和全部事件；遇到未支持能力阻止最终结算。源码测试、构建、载荷哈希、鼠标验收分开记录；未完成任何一项不冒称通过。

## 验收

基础正/负/零与欧洲控制；审核前/非10回合/已终局/配置错配/非法命令拒绝；保留军事报告且最终只加一次；卡区/RNG/地图不变；数值或版本溢出回滚；固定身份、旧版本、重发/冲突、公开投影无秘密。初始化严格、有完整14次真实动作；所有旧场景对象/能力不丢；固定面板可读，切地图/名称不改局面。最终原生全套和独立审查后构建并人工点击审核→最终计分→重发/切图。
