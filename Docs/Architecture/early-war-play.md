# 早期战争可玩接线

> description: 三张战争从真实出牌到权威结算、头条及地图目标选择的模块边界；画面仍可替换。

用户希望持续自主开发可试玩Demo、单人可查看双方、模块独立并维护JSON知识入口。本批沿用已审查推送的f2faf6c7f7bd95f7fc69776fb5df477d0ba8cb61战争基础。采用brainstorming的架构路径、writing-plans和当前会话执行；用户“自行决定并不用询问”覆盖例行设计审批，继续保存可审阅规格和计划。

## 方案

可选方案有在UI直接模拟、把战争塞进影响力效果解释器、或用现有出牌宿主接独立WarEventService。选择第三种：现有命令/收据负责身份和幂等，战争服务负责一次骰与完整结果，地图只提交国家ID。避免UI持有骰子、规则或私有牌库，也不扩大通用效果语言。

朝鲜/阿以目标固定，按“事件”即结算。印巴先进入WarDecision，只把实体牌移入Resolving，展示印度/巴基斯坦候选；不掷骰、不发军事、不完成根行动。确认目标的ResolveWar命令由会话固定身份提交，当前版本/同一决策上下文验证后结算。错误身份/目标/旧版本拒绝，取消或随意跳过不是该卡规则，不提供这些动作。

## Core边界

ActionPracticeDefinition追加可选Wars（同CardCatalog/Map、处理器互斥、必须有Risk）；ActionPracticeState追加可选War，禁止与Blockade/FiveYearPlan/Influence选择并存。WarDecision保存只读候选、owner、root actor、turn、game/version、resolution ID及权威绑定，不能直接传给客户端。

ActionPracticeRules.War.cs提供CanResolveWar及内部BeginWar/CompleteWar。普通结算ID必须含turn和action slot；头条沿用含turn/side的root ID。军事信用跨回合保留，不因CompletedActions重置而重复。

PracticeRiskState.After(WarEventPlan)核对同一Before军事/DEFCON/胜利定义并接收After。Commit同时写国家、军事、VP、random；终局保留正在结算的实体牌，停止OPS/后续头条且不增加根行动数。普通结束按真实星号去向，事件前/后OPS继续原根，军事永远归战争受益方。

本批支持的封闭事件集合不能产生Camp David/Flower Power事实，也不开放任意存档或外部活动效果注入，所以宿主只使用NoActiveInterference。不是从弃牌堆推导没有事实。后续注册这些持续效果或导入状态前，必须先实现权威事实与战争policy推导；基础中的CampDavid阻止分支并不在本批伪装成已可玩。

## 会话与显示

复用CardActionCommand的target字段，玩家不提交骰点、奖励或policy。LocalGameHost原收据层让完全相同的重发返回原结果，不能再掷骰；同ID不同内容拒绝。PublicWarView只含当前公开选择，PublicWarResult只含已公开的骰/修正来源、目标、收益及前后值，不包含WarEventState、random、牌库、完整私有状态。

PublicHeadlineStep保留战争结果；印巴响应完成后继续后续头条，途中任何错误都回滚整个宿主草案。对手先政变再战争的回执必须保留两种结果，核战争变则不执行战争。

MapPresenter复用IMapView/MapFrame合法国家高亮和SelectCountry草稿，不新增美术耦合。War决策有独立interaction key以拒绝旧地图输入。DemoController确认目标，DemoView中文显示待选/骰点/控制修正/转移/军事/VP，支持既有反馈滚动。头条缓存只存公开DTO并绑定game/version，旧回执不覆盖当前结果。

## 数据与验收

正式战争数值仍只有war-events-batch1.parameters.json。新增一个明确scope=fixture的三场景JSON及其description数据条目；不得把人工局面叫正式开局。DemoCatalog.WithEarlyWars严格加载正式参数，WithEarlyWarScenarios构造可复现真实手牌。每个复制ActionPracticeDefinition的工厂保留Wars；Bootstrap和Build拷贝参数/schema/fixture并核对输出载荷。

原生回归覆盖三牌胜败、双方印巴、固定苏联归属、DEFCON2、零影响力、终局、待选错身份/目标/上下文/溢出、重复命令、对手OPS顺序、头条取消/继续/终局、下一回合同槽不同信用、公共DTO不泄露、地图换视图/旧输入及反馈。每任务RED应是业务缺失而非编译失败，随后完整GREEN。最终构建Windows Player、逐项真实点击和日志检查；未手动覆盖的路径如实标注，整批一次fresh只读审查后推送。
