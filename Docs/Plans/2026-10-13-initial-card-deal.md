# 牌区与初始发牌实现计划

> 用户已授权持续开发；执行executing-plans，主线程实现，一次独立审阅。基础为已验证的CardCatalog与IRandomProvider，先交付纯规则服务和可信数据加载，不接完整游戏或宣称可打一局。

## 决定与边界

建立不可变CardZones保存有序DrawPile、双方普通手牌、EraReserve、DiscardPile、RemovedCards、ResolvingCards与独立ChinaCardState、EnteredEras。所有普通牌恰好出现一次；未知/重复/遗漏拒绝。未进入时代的牌只能位于EraReserve，已进入时代的牌不能留在Reserve；EnteredEras按Early→Mid→Late连续。中国牌必须从目录中取得唯一China定义，并从普通牌区排除。中国牌持有方与朝向独立保存，卡牌静态定义不增加动态字段。

输入集合有界枚举并防御复制，查询只读。牌库/手牌/弃牌/结算顺序保持输入；无序的EraReserve与RemovedCards采用Ordinal规范化。Core不读文件，不依赖Unity。当前不提供移动/抽牌/时代加入操作，未来单独加入规则校验。

InitialDealDefinition包含从setup/turn-flow解析的初始回合上下文、HandTarget、ChinaCardState。初始时代必须Early且回合1；数量来自TurnCalendar。InitialDealService.Create(catalog,definition,random)只生成初始结果，不接受既有牌区，因此也不伪称已有命令幂等。随机从Fork推进，返回新CardZones及RandomSnapshot，输入随机状态不变；上层未来一次共同提交。数据/牌数不足在采样前拒绝，异常不发布半副牌。

沿setup Wiki既有重放约定：Early普通牌按CardId Ordinal排序，从尾到头Fisher–Yates，每步NextInt(0,i+1)；首端USSR、US交替，各到HandTarget；剩余保持洗后顺序。中国牌不参与洗牌/手牌数；Mid/Late留在Reserve。完整真实初始预期35早期、8+8手牌、19牌库、67预备、1中国牌，数量只作为独立数据测试期望而非业务代码常量。

Infrastructure的InitialDealDefinitionLoader.LoadSubset(setupJson,setupSchema,turnFlowJson,catalog)严格校验现有setup身份/版本、来源与中国牌行；只消费初始turn/hand两个引用，要求dataset与既有指针准确匹配并验证原数组第一个确实为early/first_turn1，防止重排被排序后的Calendar掩盖。通过TurnCalendarLoader获得共享参数；验证China ID/类型、USSR/FaceUp标准初始状态，未识别规则停止。只读目录+发牌配置不是可执行RulePack；其余轨道/部署与全部事件完整开局门禁仍由未来编排处理。

## Task 1：牌区与洗牌发牌

- [x] 先写测试并运行RED：集合防御复制与只读、牌实体唯一完整、China独立、Reserve/EnteredEras、未知ID/side/face；已知小牌组固定零随机的洗后顺序与交替发牌；实际103牌分区、确定性、原随机状态不变、错误或溢出不发布结果。
- [x] 实现Core/Cards的ChinaCardState、CardZones、InitialDealDefinition/Result/Service；只测试实际不变量，不为预留未来操作写空类。
- [x] 运行实际Unity回归。

## Task 2：加载已确认数据

- [x] 先写RED：真实setup/turn-flow加载；支持已验证共享数值读取；版本/来源/引用/原数组顺序/中国牌错误与不足牌数拒绝。
- [x] 实现InitialDealDefinitionLoader，正向实证来自规范数据，变异数据逐项拒绝。
- [x] 同步模块描述、Wiki/索引、setup现状、工作记录与实际报告；独立审阅后按授权Git同步。

## Review Focus

1. 每个目录实体恰好出现一次，包括独立China；手牌之外的重复/遗漏仍拒绝。
2. DrawPile首端定义、Fisher–Yates边界和USSR/US交替与已有重放设计一致。
3. 输入随机不推进，结果失败不暴露或部分修改；返回的随机快照与新牌区同时使用。
4. 时代参数从共享文件读取，重排和错指针不能静默绕过；未消费的轨道不得被称为已验证。
5. 权威牌区不进入正常玩家公开视图；本批尚无UI/命令/完整RulePack接入或完整存档。

基线：ff8d2d0e4925f34b4d664b1404edf6f95cb2047d；实现前674项测试。


两个实现任务已通过实际729项回归；独立审阅通过；进入已授权Git同步。
