# DEFCON事件与政变干预

description：查古巴导弹危机、核潜艇、SALT Negotiations和ABM Treaty的事件轨道、政变责任与豁免、解除支付、公共弃牌选择和视同OPS授权。

状态2026-10-06：4牌参数Bound；全项目32 Bound / 70 Unresolved，102处理器planned，C#/Unity未实现。入口：[数据描述](../../Data/Descriptors/defcon-events-batch1.data.json) → [参数](../../Data/Cards/deluxe-2015/defcon-events-batch1.parameters.json)、[Schema](../Contracts/defcon-events-batch1.schema.json)、[案例](../Design/defcon-events-batch1.cases.json)、[验证](../Design/defcon-events-batch1.validation.json)；[干预模块](../Descriptors/coup-interventions.module.json)、[事件OPS](../Descriptors/event-operations.module.json)。轨道基础数值、军事类型、OPS合并只引用既有数据，不按卡名猜默认。

## 古巴危机：接受尝试时即刻输

#40为中立3OPS星号事件，先把DEFCON设2，再对EffectController的对手激活本回合危机。任何地区/战场或非战场、普通或事件授予的合法政变一旦真正接受，实际CoupActor即刻输；不会按RootPhasingPlayer判这个特殊输法，不受核潜艇保护。没有骰子、军事、影响力或DEFCON降级后续。DEFCON后来升3/4/5仍有危机，解除也不把DEFCON升回去。危机不限制调整、投放或战争，战争不借政变管线。

基本身份/预算/敌方影响力/DEFCON地理/条约先验证。非法载荷或被门禁拒绝不构成已接受政变；合法但致命的尝试不能当无效命令跳过。管线Validate→BasicLegality→AcceptedCoupFatalCrisis→DieModifiers→既有政变计划。源FAQ明确实际Actor选择发起即刻输，基本合法性及事务输入边界为DC1工程约定，不宣称非法UI点击也输。

受影响方可以在任何稳定命令边界（包括对方行动/等待选择）先单独解除：US从西德或土耳其一国移除自己2点；USSR从古巴移除自己2点。必须同一国、现有量≥2，不能1+1、敌方影响力代付、移除不足量或东德代西德。RemoveExactOwnInfluence+CancelThisInstance原子提交，不消耗根行动/OPS、不改变DEFCON。双方都只有实际受影响方能解除，不把事件控制者当支付方。

ReliefCommand绑定InstanceId/Revision/CountryId，身份来自会话。它是特殊自有命令，不是回答别人Decision的越权入口；暂停的父帧/隐藏选择保持，响应前重验证当前局面。任何稳定边界可用不等于抢占已接受政变或已提交骰结果：终局后解除无效，不能撤销前次原子结果。异步到达按权威修订/命令序号处理；UI在提交政变前展示解除入口和致命后果。该表达是数字端工程边界，runtime尚未验证。

## 核潜艇与SALT：独立修正

#41固定US2OPS星号，本回合US战场政变的BattlegroundCoup降级替换为0，包括验证的事件授予政变；军事照原行动类型，成败都免降级。它不豁免地理/条约，不影响USSR政变，不覆盖直接事件Set/Improve/Degrade DEFCON，不能取消危机。Actor与根Phasing分开：US执行由USSR根帧授予的政变仍按US原因豁免；US根帧下USSR政变仍降级。

#43中立3OPS星号：先DEFCON改善2（最大5从基础轨道读）→双方本回合后续政变骰-1→可选取回公共弃牌的一张非计分牌。SALT修正骰点，不减OPS预算或军事信用，不改战争骰；核潜艇与SALT可同时作用于美国政变，危机优先截断二者。相同效果不同结算的再次激活仍RequiresRuling，不自行叠加/刷新。

回合到期挂ExpireAndReset，激活回合保存；卡牌星号去向与实例期限分离。过期实例混入当前局面停止，不默认延长。

## SALT公共弃牌取回

由EventController选择，候选仅实体DiscardPile中Kind=Event（本基础集非计分/非中国牌），排除Hand、HeadlineReserved、InResolution、Removed、DrawPile和EraReserve；本次源SALT牌还在结算，不能把它当已弃牌取回。可明确放弃；空候选自动不取回仍完成前两项，这是may的工程表达。

候选只返回ID/必要名称，公共区不泄露私有手牌。接受取回前按Revision与当前牌区重验，公开该牌再原子从DiscardPile→ControllerHand；不立即触发其事件、不占第二个行动、不改变已完成的DEFCON和持续修正。来源不合法/旧响应不撤销此前在等待点已提交的前两步，但本次响应零变化。存档保存DecisionId、候选依据及步骤，不能重做改善/激活。

## ABM视同普通OPS

#57中立4OPS无星号：强制DEFCON改善1→EventController可选视同4OPS的普通投放/政变/调整，或放弃。改轨上限不影响后续授权。基值4来自已核实事件字面参数，随后按真实OPS Actor及已验证地区花费历史引用现有ops_policy；不因源牌打印恰好也是4而推导其他事件预算。真实中国牌奖励不能加到ABM，苏联越南奖励在整次东南亚行动时按共享规则判断。

正常预算与mode锁定，政变整份预算一次，调整逐次1OPS，不能切换模式补花；放弃后预算不流入根OPS。无DEFCON地理或条约豁免，仍受危机与SALT/核潜艇。ABM不是卡文free coup，政变信用类型OperationsCoup（按有效OPS）；与拆墙FreeCoup必须分开。头条也能授权这些行动，原头条出牌者保留普通核战责任；不增加根行动名额、不消费额外实体牌。

不能把该事件OPS用于太空：它正在实现事件，未被弃置；FAQ明确ABM不是Grain Sales的实体牌使用例外。也不能把授权视作再次打出ABM或战争牌，不产生额外ActualUse/Flower处罚。未知源证明/活动修正不生成猜测预算。实际普通OPS原语与目标重查询仍由既有服务负责，本批不复制政变/调整完整算法。

## 来源与验证

英文卡图p7 #40/#41/#43、p9 #57；规则§5.5/6.3/6.4/7.4.3/8.1/8.2；FAQ PDF p8–9 #40/#41、p12 #57、p14 #67。按现代卡文与FAQ的Actor/事件原因明确范围，避免旧Nuclear Subs泛指actions误读。DC1稳定输入边界、具名来源证明和可选流程属工程设计；病例参考假定内部上下文可信，不证明真实身份/存档/事务。无C#/Unity运行验收。
