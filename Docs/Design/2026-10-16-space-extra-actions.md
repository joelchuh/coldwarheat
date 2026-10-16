# 太空额外行动：独立行动进度与真实出牌

状态：批准范围内的自主实施设计；基线 030ed9f。用户已授权继续开发并自行决定常规实现。

## 目标与依据

完成太空第8格独占能力的额外行动，沿用真实卡牌/命令/私有视图与回合审核。官方 R2015 §4.5D、§6.4.4规定正常6/7次、苏联先行、能力提供每方总计8次且被追平立即取消。来源 https://www.gmtgames.com/nnts/TS_Rules-2015.pdf 。项目已有 space-race.md 约定：普通行动完成后再提供额外名额；每次开启重查；可放弃剩余名额；已开始的根行动不撤回。值读取 SpaceAbilityRules.Query，不重复硬编码。

## 选择与边界

采用独立不可变 ActionRoundProgress，记录每方已完成根次数、放弃标志、当前方/轮及是否可选。现有 CompletedActions 成为总数兼容投影，不能再用奇偶推导额外方。相比单纯延长总次数，它可以正确表达美国或苏联连续额外行动；暂不建立尚无消费者的通用能力插件系统。

普通名额按苏联/美国交替；普通全部完成后根据最新轨道查询额度，候选按轮数及苏联优先排序。每完成一个根行动才计一次，头条、对手事件子步、选国和选择OPS都不计次。额外阶段直接复用 ChooseCard：合法出牌即开始，无须额外“接受”事务。新增 DeclineExtraActions 命令只允许可选名额且尚未开始出牌时，明确放弃本回合剩余太空名额。结束后军事/扣留审核→太空弃牌→下一回合或最终计分。

CanBegin 检查当前名额和最新轨道；开始后的多步根行动保留当前身份，完成后才重查下一名额。RemainingAllowance 使用当前方独立计数、动态总额度；中国牌资格使用此值。私有手牌仍仅给固定身份本人，观察模式独立。公开当前进度不暴露手牌。

本批不实现北海油田、一般无牌跳过、其他额外行动来源或完整卡牌处理器。空手额外方可明确放弃，绝不强制用中国牌。现有无牌普通行动缺口仍需后续独立实现。

## 接口

ActionRoundProgress.Create(GameTurnContext, SpaceRaceDefinition)；CompletedFor/DeclinedFor；CurrentActor（完成时null）、Round、IsOptional、IsComplete、TotalCompleted；CanBegin(SpaceTrackState,PlayerSide)；RemainingAllowance(track,side)；CompleteRoot(track,side)；DeclineRemaining(track,side)。非法枚举/外部定义/错误身份或阶段不生成状态。

ActionPracticeState.Rounds 贯穿全部克隆；新回合创建零历史。内部构造器只允许零历史缺省进度，非零遗漏立即拒绝，防止新处理器悄悄丢进度。终局可早于进度完成，普通回合审核必须 Rounds.IsComplete。既有操作ID继续用 TotalCompleted 保持一根一个唯一槽。

## 验收

覆盖双方普通轮序、早期两次/中晚期一次、同格取消、最后普通行动新获能力、根内获失资格不重复计数、明确放弃、非法重复与跨定义；宿主幂等/版本/身份；额外中国牌计分约束；普通14步不退化；额外后军事、弃牌、最终及下一回合重置。独立人工场景与新Player，真实鼠标出牌/放弃/重发/地图切换；单元测试不替代鼠标验证。
