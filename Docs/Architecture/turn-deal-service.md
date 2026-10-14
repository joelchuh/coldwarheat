# 补牌与时代加入：纯规则服务

目标：下一回合协调器可以复用一个确定性补牌计划，补足普通手牌，按时代加入新牌，旧牌库用尽后才重洗弃牌。保留当前可玩Demo作为已验证交付，不把未接线服务称为连续对局。
规则：R2015 §4.1、4.3、4.3.1、4.4、4.5B（2026-10-14查官方PDF）。目标数量来自GameTurnContext.Parameters.HandTarget；时代来自同一日历，不写第二份8/9或4/8常量。

接口：TurnDealService.Plan(CardCatalog catalog, CardZones cards, GameTurnContext turn, IRandomProvider random) -> TurnDealPlan。返回Before/Cards/Turn/Random、UsDealt/UssrDealt、EraAdded(bool)、DiscardReshuffles(int)。完整牌区与随机状态仅供权威宿主，不返回普通客户端。服务无GameState写入/无UI依赖。

边界：宿主确认已经抵达TurnStart且完成回合末/特殊选择后才能调用。服务验证无Resolving、牌区与传入目录匹配、当前EnteredEras最高时代不晚于目标；新进入时代必须恰为下一时代且目标为该时代FirstTurn；已进入的同一时代不再加入。没有开始第二回合按钮，也不重置轨道。原手牌超过目标保持，不擅自丢牌；不足只补差额，中国牌不计手牌、不转交、不翻面。

确定性：保留原手牌顺序，USSR/US交替，每次跳过已足额的一方；从牌库首端取牌。新加入时代取EraReserve中目标时代，按CardId顺序追加到当前Draw，再Fisher–Yates倒序洗；弃牌保持。需要继续发牌且Draw为空时，以当前Discard顺序执行同算法，清空Discard。恰好发完即足额时不主动触发下一次抽牌重洗，沿既有TURN-03“继续补牌”边界；后续通用抽牌必须复用该边界。Removed和未来Reserve永不进入弃牌重洗。只推进Fork，返回Capture与Cards成对提交；失败不发布部分。

不足全部可用牌（Draw+将入时代+Discard）时拒绝，不消耗随机副本；不凭空造牌或默默少发。这为缺少专门裁定的异常输入设置工程失败边界，不宣称官方要求弃局。随机耗尽也整次抛出，原随机及牌区不变。最多256实体，手牌目标再大也由可用计数提前拒绝，不无界循环。

重试：相同输入/RNG输出一致。把完成结果再次用于相同目标时手牌满足且时代已进入，不再洗牌/发牌；不等同Host命令幂等，也不授权任意时刻重新补牌。

取舍：独立纯服务优于把洗牌写进UI或一次性大改整个回合协调器：可独立测试全部时代和耗尽分支；成本是本批没有新UI能力。未来协调器共同提交回合、DEFCON/太空计数与此计划，并进入头条。

验收：现有全套测试+新增有确定顺序的小牌组、真实103牌、同回合无变化、超额手牌、时代边界/重复、旧牌先发/弃牌延后、移出排除、Resolving/错时代/不匹配目录/不足及随机溢出原子性。JSON描述/Wiki/报告同步，独立整分支审查和日常Git上传。
