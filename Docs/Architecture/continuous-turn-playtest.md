# 连续回合试玩设计

目的：让用户在现有地图Demo里完成首回合结算后继续第二回合，复用已测试的补牌计划，不重开整局。保留明确“跳过头条”的练习性质与未知事件拒绝，不能宣传完整十回合正式对局。

方案：在现有有限试玩中新增可选NextTurnEnabled与无牌/无目标BeginNextTurn命令。相比直接重建开局或把所有头条事件一次实现，本次复用当前权威快照并原子推进，改动可验证。旧装配默认禁用；新ContinuousPlaytest启用，独立构建。地图IMapView不变。

严格入口：只有完整普通行动后、TurnEnd的Continue审核完成、无活动牌/Resolving/事件/部署/对手序列且未终局才允许；已审核China须正面、OPS效果已清、MilitaryAssessment匹配当前回合，双方军事值已归ResetEarned。任一固定身份可请求，无Actor跳过权限。当前LastTurn时拒绝，不建立回合11、不伪造最终计分。拥有尚未实现的额外行动或回合末弃牌能力时拒绝推进；新增连玩配置的回合末审核在额外行动能力生效时先拒绝，避免少打行动。

事务：Calendar.ForTurn(current+1)取得唯一目标；DefconService.PlanChange(TurnStartImprovement)；Military在新Turn保留已归零值，清当前Assessment并保留历史信用；SpaceAttemptRules.BeginNextTurn保留Track/Receipts且重置尝试数；TurnDealService.Plan补牌/时代洗牌；空的新TurnOpsState；新ActionPracticeState.ChooseCard,CompletedActions=0,Actor=USSR,TurnEnd=null。地图影响力/VP/China/移出牌及未用普通手牌不重置。GameState一次发布Turn/Cards/RNG/轨道/新行动上下文，version/seq各+1，任何溢出/非法补牌整次拒绝。

跨回合唯一ID：当前Space/Coup收据使用CompletedActions，下一回合会冲突。改为同一私有PracticeSlotId函数生成practice.turn.<n>.action.<slot>，操作上下文同样包含Turn。普通/对手政变、太空与计分沿用统一前缀；收据历史不因换回合被丢弃。加入跨两个回合同一槽的真实太空/政变测试，证明不会误当重试。

公开层：PublicPracticeView加入NextTurnEnabled/NextTurnAvailable；当前Turn已有正常玩家只读字段，不暴露TurnDealPlan。结算面板显示“下一回合（跳过头条）”，最后回合或特殊能力阻止时给出明确有限试玩说明。进入后顶栏显示回合N及行动1/6或1/7、双方手牌计数；查询身份仍只见本人手牌。重发同命令返回原收据，不能二次改善DEFCON/补牌。

边界与失败：本命令不负责头条、额外行动、太空期末选择、最终计分、存档或新事件。无可用牌/不兼容状态按稳定错误码拒绝，不靠UI修复。只会在新装配启用，旧TurnEndPlaytest仍停回合末。正常地图/手牌阶段不能调用。

验收：原始默认12行动→审核→下一回合；DEFCON3→4及5封顶；轨道/VP/影响力/牌实体保留；尝试与军事当前回合重置、历史收据保留；下一回合苏先手与手牌目标；重复/冲突/过期/终局/未审核/残留/特殊能力/LastTurn/溢出拒绝；自动真实可行第二回合路线至少执行一次行动。EditMode全量、独立构建/源码载荷哈希、尽可能真实鼠标从默认牌走到下一回合。用户可重置再试玩并反馈。
