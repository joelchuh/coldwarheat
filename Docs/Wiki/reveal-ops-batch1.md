# CIA Created 与 Lone Gunman

description：查一次对手手牌揭示、私有历史收据、固定阵营视同1OPS、普通政变与根核战责任，以及影响力起点未核实分支。

状态2026-10-12：两牌参数设计已建立，处理器均 planned，C#/Unity尚未实现。先读[数据描述](../../Data/Descriptors/reveal-ops-batch1.data.json)，再定位[参数](../../Data/Cards/deluxe-2015/reveal-ops-batch1.parameters.json)、[Schema](../Contracts/reveal-ops-batch1.schema.json)和[案例](../Design/reveal-ops-batch1.cases.json)。两牌参数已Bound；完整主验见[本批报告](../Design/reveal-ops-batch1.validation.json)。

## 固定角色与一次揭示

| 卡牌 | Actor / DecisionOwner / 揭示接收方 | Hand拥有者 |
|---|---|---|
| #26 CIA Created | US | USSR |
| #62 Lone Gunman | USSR | US |

RootPhasingPlayer由原根行动继承，不能拿它代替表中角色。事件来源还须绑定SourceCardId/EffectId、唯一SourceInstanceId与独立牌区证据、RootPhase、ResolutionId、StepId、RootActionId、FrameId、父帧、规则集/数据摘要及可信提供者证明；角色由已验证参数推导，客户端不能自报身份或授权。

事件实际执行揭示步骤时，只截取对手当前普通Hand的卡牌身份，包含计分牌。InResolution中的本牌及其他结算牌、HeadlineReserved秘密头条、牌库、弃牌、移出游戏和独立公开China区域均不进入快照。源实例必须在权威牌区快照中唯一对应本CardId且处于InResolution；缺失、错实体、Hand/弃牌或重复位置在输出私有内容前拒绝。源卡离开Hand的时点由TurnFlow先行处理，不能为凑“整手”把其他区域合并回来。牌区无变动，不强迫打出、弃置、计分或触发看到的牌，也不消耗随机。

揭示是强制前缀，随后行动可以放弃；空Hand仍写入空快照并给出可选行动。成功揭示后Decline，或者手牌本来为空，事件仍已发生。两牌印刷星号均由正式卡牌定义和正常TurnFlow disposition处理；参数不复制星号事实。等待可选OPS时源实体留在InResolution；终局草稿内记录已发生证据、完成既有去向/根收尾并关闭决策，再原子写Finished，不在已经提交Finished后追加修补。

## 私有快照与事务

`snapshot`保存捕获Revision、拥有者/接收方及稳定卡牌实例和CardId；它是历史收据，不是对手实时手牌视图。后得牌不会追加，之后打出、弃置或转移也不会从历史记录抹去。正常自身手牌视图仍由各玩家自己的当前状态生成。

初次命令先校验来源提供者、阶段、牌区、身份、版本与影响揭示/预算的相关效果，准备不可变snapshot、receipt、grant和decision，并一次提交。提交失败不得向玩家输出任何私有牌名、实例或可推导这些信息的错误。公开日志仅可说明已发生揭示；完整payload只属于权威状态和可信接收方的过滤视图，不能塞进公共事件、旁观者日志或合法目标查询。

后续OPS选择是独立事务。非法目标、错误模式、陈旧Revision等被拒绝时，保留已经合法提交的揭示，不能假装可以让接收方忘记。相同CommandId且请求/来源绑定相同的重试只重放原收据，先验证当前调用者身份与原绑定，不读取当前手牌、不刷新私有内容、不再次创建授权或推进Revision/RNG。冲突重试只返回稳定错误，不把他人的旧收据当错误详情返出。Finished是吸收态，但允许经过同样身份过滤的原收据重放。

`reveal_receipt`的嵌套形状是严格Schema；Source/Actor/Owner、Frame/Resolution/Grant、Revision和独立实例唯一性还有跨字段不变量，不能只靠JSON Schema宣称授权安全。示例里的proof ID、可信身份、已知provider/效果布尔只是假定成立的离线前提；实际会话鉴权、隐藏信息过滤和永久保存仍待实现。

## 普通事件OPS授权

复用[module.event-operations](../Descriptors/event-operations.module.json)的as-if-card授权和[ABM既有政策](defcon-events-batch1.md)，经[手牌模块](../Descriptors/hand-events.module.json)取得快照，由[TurnFlow](turn-flow.md)管理帧和源卡去向。没有新建逐卡行动算法或额外普通行动轮。

卡文基值用`VerifiedEventLiteral`，不是“因为两牌刚好印刷1，所以把所有类似事件绑定printed_ops”。只允许Influence、Coup、Realignment，允许Decline；不发额外卡牌、根slot或被揭示牌的使用权限。不能太空：本事件不能在自身结算中被弃作太空尝试，也没有Grain Sales取得另一实体牌并允许play的专门例外。

OPS上限/下限和合并规则引用`persistent_batch1/ops_policy`，创建grant时保存实际Actor的有效实例证据。CIA在US遏制下为2，清洗下仍为1，遏制与清洗同在为1；Lone在USSR勃列日涅夫下为2。根出牌者的修正不转交Actor。区域奖励按共享用途/花费历史验证；不借用中国牌专属源牌奖励。后续不明中断或重报价停止相关分支，不追溯重算已提交操作。

第一次合法操作锁定模式；拒绝不锁模式、不花预算、不抽随机。Coup整份预算一次政变；Realignment逐次选择目标并重查局面；Influence沿用普通可达性、动态敌控成本和事务计划。可在已接受操作之间Finish放弃余量，沿用EO1从may及逐次选择推导的政策，非专门官方FAQ裁定。不混合模式、不挪用根OPS。未知相关限制不能当“无合法目标”自动结束。

政变/调整仍受普通DEFCON地区表、条约、目标敌方影响力与具名事件门禁；不豁免地理。此处是普通`OperationsCoup`，军事信用引用共享政策，按Actor有效OPS计入；Influence/Realignment不获政变军事。SALT/LADS只按各自已核实政策调整骰，核潜艇只替换美国战场政变降级；这些引用不复制数值。

普通战场政变使DEFCON到核战值时，败者仍为RootPhasingPlayer：US打Lone、USSR政变，则US输。若实际Actor受古巴危机约束，基本目标合法后接受政变企图即Actor输，先于骰子、军事、影响力和DEFCON变化。非法目标不会触发这一“已接受企图”。核潜艇不会取消古巴危机。

## 影响力起点的封闭边界

R2015 §6.1.1规定行动轮开始的友方影响力是可达性基础；§7.4.3要求as-if OPS遵守§6.0，没有明确把起点改为grant创建时。正常ActionRound取RootActionRoundStart里实际Actor的快照，并加Actor超级大国的邻国。即使根玩家先做OPS移除了Actor最后一枚影响力，随后事件也不改用当前空局面；反之中途新增影响力不会扩张根起点。逐点投放不滚动扩张可达范围，当前控制变化仅影响费用。

Headline或特殊嵌套来源必须提供独立核实的具名baseline policy与快照证明。当前资料未找到适用于这些来源的专门起点裁定，故保留RO1-OPEN-01；不能默认GrantCreation。缺少该证明时，Influence目标查询与选择返回RequiresRuling，保留已授权揭示与待决策，仍可选其他已合法模式或Decline。未知真正影响揭示、来源合法性或预算的提供者/效果则在初始揭示前停止。

可接受的source_kind只是类型分类，不等于已实现或已注册。VerifiedHandEventChild要求另行绑定提供者（例如既有五年计划的US子事件设计，只允许CIA，不能调用USSR关联Lone Gunman）和继承根身份；不能把Grain Sales、Star Wars、Missile Envy等未绑定提供者包装成此类型直接放行。牌参数Bound不意味着完整规则包可开局。

## 来源与实际验证边界

重新目视[GMT英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf) PDF p5 #26 / p10 #62，确认两牌的揭示顺序、固定阵营、may、1OPS和星号；旁列译文不作为规则源。[FAQ v5](https://www.gmtgames.com/nnts/FAQv5.pdf) PDF p5–6 #26 / p13 #62确认非持续揭示及§7.4.3，p13 #62给出普通核战与CMC不同责任，p14 #67说明事件OPS与太空的实体弃牌边界。旧版FAQ里“事件OPS不受修正”的回答不适用本批Deluxe as-if卡文。

[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf) §5.2用于成功事件星号，§6.0/6.1.1用于普通行动限制与影响力起点，§7.4.1–3用于Actor修正和CIA+遏制例子，§8.1.3/8.2.3用于根核战与普通军事。工程选择包括原子收据、稳定排序、空手快照和来源验证；都不冒称官方数字游戏API规则。官方资料的有限检索没有消除RO1-OPEN-01。

案例含参数/严格Schema和跨字段负例、独立手算OPS与政变、有限不可变视图/重试和起点参考模型。本批临时checker通过150项（30参数、54契约、66参考），另核对11个参数指针及两牌印刷事实。它们不是真实鉴权、持久化、网络或完整普通OPS实现；10项runtime验收仍待实现，游戏测试执行数为0。主验整合后实际重跑旧1991项，加本批150项共2141项；独立审阅者只重跑本批150并另做13个复探，未独立重跑旧套件。13个复探不重复计入2141。

[返回索引](index.md)

审阅补强：source_kind/provider/实际关联阵营共同校验，FYP授权条件引用共享手牌参数；RootPhase和独立源实体位置进入严格内部契约。正常根/对手事件的ActionRound只能用RootActionRoundStartActor（证据缺失可待裁定），头条或特殊子来源只能用独立具名基准或RequiresRuling。start有限模型也拒绝未离开Hand的源实例；相同合法重试仍先重放旧收据，不读取新牌区。以上是跨字段规则投影，未声称真实服务器鉴权已实现。
