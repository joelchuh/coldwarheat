# 单格太空事件基础

目标是继续补齐可玩规则，同时保持Core不依赖Unity、界面与地图可以替换。本批实现已设计的#18捕获纳粹科学家纯计划；下一批再接固定会话、头条、牌去向与界面。用户已要求自行决定并持续开发，沿用此授权，设计/计划由当前会话自审后内联执行。

## 选择

采用共享单格推进计算 + 事件专用请求/收据。相比复制普通尝试，会统一先后到达奖励；相比任意步数的通用脚本引擎，不提前引入未核实的一小步/越终点策略。普通成功尝试也调用共享计算，失败仍只增加普通次数。

`SpaceAdvanceRules.CalculateSingleStep(track, beneficiary)`返回不可变前后位置、先后到达、共享轨道VP。它不处理随机数、手牌、次数或授权。`SpaceEventDefinition`绑定正式CardCatalog、同一SpaceRaceDefinition及单步参数；严格加载正式space-events-batch1参数/schema，校验来源版本、引用和卡牌绑定。步骤/奖励从共享数据读取，运行代码只限制已经实现的政策。

`SpaceEventRequest`包含Id、SourceActionId、TurnNumber、Beneficiary、Definition。Beneficiary由未来可信事件帧给出，不能从客户端任意申报；此基础服务不是玩家命令入口，也不证明持牌/阶段/事件授权。中立牌不能硬写成US或根行动者。

`SpaceEventRules.PlanAdvance(state, request)`先验证回合/绑定与ID，再返回Before/After、SpaceEventReceipt、AlreadyApplied和VpDeltaToApply。SpaceAttemptState新增只读EventReceipts；普通尝试与跨回合复制时都必须保留。共享ID命名空间拒绝事件与普通尝试互撞；同事件ID同事实返回原收据但保留当前状态，VP待应用为0；不同事实拒绝。两类收据合计工程上限4096。

事件只推进1格，不要求OPS、不接收随机源、不更改任何一方已用次数。能力继续从双方位置查询，不保存易过期布尔值。另一方随后到达同格必须得到后到奖；事件后普通尝试和普通尝试后事件都不可重复发同一格奖励。正常回合重置次数但保留全部收据。

## 未解决与边界

SS1-OPEN-01保持待裁定：已有位置为终点时QueryEligibility返回TerminalRequiresRuling，PlanAdvance明确拒绝，零状态变化；同已成功事件ID在终点重试仍返回原收据。不得将此分支误称“无效果事件已发生”。普通太空终点拒绝语义不变。

服务只输出VP待应用值，不修改全局分数、牌区、行动游标或判断终局。下一批宿主必须把推进、奖励、带星牌移出与胜负检查原子提交。未接存档、网络、外部效果授权或多步事件，不提供通用绕过规则的移动命令。

## 资料

- 既有正式参数：Data/Cards/deluxe-2015/space-events-batch1.parameters.json；共享轨道Data/Rulesets/deluxe-2015/space-race.json。
- [GMT规则2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf) §6.4.2–4：事件可推进、先后奖励和即时能力；§6.4.5–6普通用牌不发生事件与终点限制。
- [GMT FAQ v5](https://www.gmtgames.com/nnts/FAQv5.pdf) PDFp4 #18：事件不占普通尝试；紧接旧二格问答不用于修改本版单格参数。
- [官方示例](https://www.gmtgames.com/nnts/sneakpeek_xop.html)仅辅助确认事件无骰与首格奖励，不用旧示例取代当前版政策。

测试应覆盖双方全部目标格/先后奖、已用次数与能力变化、事件/普通混合顺序、重试/冲突/跨回合/终点、严格加载与错误绑定。以真实Unity原生报告为准。
