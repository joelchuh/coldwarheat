# 奥运会比赛与抵制基础

> description: 将既有奥运会设计中可独立验证的规则计算落成纯C#；不把参数计划冒充已接会话事件。

## 范围与选择

从b4f92178318a8b6fe5e3b0cb970fafe5c029cbac继续。用户已授权持续自主开发、日常Git；使用brainstorming与writing-plans，常规方案不再次请示。可一次接全部选择/重掷/OPS，也可先独立计算；本批采用后者，避免把未完成的来源、选择与行动授权混进基础服务。地图与Unity表现不变。

本批提供共享标准D6定义、严格正式参数加载、比赛双骰计算、单对骰原子计划、抵制DEFCON/核战计划及非终局后的OPS参数提议。复用IRandomProvider/RandomSnapshot、DefconService、VictoryService。Core无Unity或JSON引用。数值取Data，不在UI/服务复制卡文数字。

## 接口与权威边界

- StandardDieDefinition保存Id/Minimum/Maximum；本版只接受正式die.standard_d6。StandardDieDefinitionLoader.Load(json,schema)验证既有共享数据。
- OlympicGamesDefinition保存Cards/Card/EffectId/Die/SponsorModifier/WinnerVp/TieVp/DefconDelta/BoycottBaseOps；构造对照真实#20、Neutral、Early、2印刷OPS、非星号、正式Bound引用。OlympicGamesDefinitionLoader.Load(json,schema,cards,die,knownEffectIds)只读并严格验证本批所用策略、来源与引用。
- OlympicContestRules.Calculate(definition,sponsor,usRoll,ussrRoll)返回双方原骰/修改总数、Tie、Winner、AwardVp。主办方+参数修正，总数不封顶；较高者获VP，平手无VP但必须继续。
- OlympicEventState保存Definition、Defcon、Victory、VpForUS、Random；无可变集合。它不是GameState，也不证明来源牌、分支选择或玩家授权。
- OlympicEventService.PlanPair(state,sponsor,resolutionId,policy,random)每次只计算一对，US再USSR；通过独立fork取样，两个骰、VP、游标及Tie标志一起返回不可变计划。验证每次取样后游标确实前进且算法/stream未变；第二骰失败不能影响源RNG或返回半结果。调用者负责原子提交、重试、已完成吸收态以及来源/选择/收据链。
- OlympicEventService.PlanBoycott(state,sponsor,rootPhasing,resolutionId,policy)先降DEFCON，再共享核战判定；终局无OPS提议。非终局返回Sponsor与加载的BaseOps。OlympicOperationsOffer只是待验证的参数提议，不授予行动权限；实际普通修正、模式、起点与操作仍由后续集成验证。
- policy仅接受NoActiveInterference，未知值在随机/状态计划前拒绝。正常头条或行动来源绑定和未来嵌套不在此基础中授权。

## 关键不变量

活的DEFCON与VP、合法双方身份/ID、非空同游标随机源先验证。对双方可能奖励预检溢出，再抽骰。原输入不可变。比赛不改DEFCON、不生成军事；抵制不抽骰、不奖VP。核战输家取rootPhasing，不能用抵制选择者替代。独立计划可重复计算，但不表示命令重发安全；只有后续Host负责幂等。

平手结果包含RequiresAnotherPair；服务没有递归或while重掷，也无游戏次数上限。未来编排每次处理有限对数并保存内部继续。本批不实现摘要链、持久化、分支锁、来源帧、Host/DTO、卡牌移区或根推进，不声称整张奥运会可玩。既有完整设计中的结束清理须在接线批次与当前终局Resolving审计策略对齐。

## 资料与未决边界

沿用[奥运会知识](../Wiki/olympic-games.md)、正式参数及Schema。2026-10-17重开GMT规则2015，核对§8.1.3直接抵制核战例和§7.4.3事件OPS一般限制；卡数值沿用既有已亲看英文卡图p4 #20记录。来源URL保留原数据，不保存官方全文或美术。

OG1-OPEN-01头条/嵌套Influence起点仍待裁定；本批不查询或执行OPS，不能默认授权该分支。不改已确认规则解释。

## 验收

正式共享数据实际加载、双方主办的全72种原骰组合、总数>6、双方胜利阈值、平手保留与真实PCG重建；第二骰异常/越界/游标不动/换stream拒绝且源随机不变。抵制DEFCON2→核战四种Sponsor/root组合和非核战提议、非法/终局输入无计划。Unity原生完整回归、元数据、源码哈希；一个fresh只读整批审查。没有界面改动，不重建Player或重复鼠标验收。
