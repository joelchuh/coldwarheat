# 牌区与初始发牌

> description: 查每张牌的唯一位置、独立中国牌、时代预备区、确定性洗牌发牌及setup共享引用。纯Core与加载子集已实现，并接入GameState、身份投影及发牌/部署Demo；完整RulePack仍未实现。

先读[模块描述](../Descriptors/card-zones.module.json)，再读所需函数。实现基于已审核[卡牌静态目录](card-catalog.md)、[随机源](deterministic-random.md)与既有[开局设计](setup.md)。

## 权威牌区

`Core/Cards/CardZones.cs`保存DrawPile、双方手牌、EraReserve、DiscardPile、RemovedCards、ResolvingCards、China与EnteredEras。目录中的每个实体必须恰好出现一次；中国牌只能位于独立ChinaCardState，不能出现在普通牌库/手牌或预备区。

EnteredEras必须从Early连续到已进入时代，拒绝重复/缺段/未知；输入可无序，输出规范排序。未进入时代的普通牌只能在EraReserve，已经进入的不能留在那里。所有集合有界读取并防御复制；牌库、手牌、弃牌与结算牌保留顺序；无序预备区与移出牌按CardId Ordinal排序。`GetHand`只接受US/USSR；未知身份拒绝。

ChinaCardState含稳定Id、Owner和Face，不改动静态CardDefinition。US持有或FaceDown可表达后续局面；InitialDealDefinition则只接受标准开局的USSR/FaceUp、回合1/Early。不存在自动转交中国牌或时代加入方法，这些后续按规则实现。

CardZones是内部完整信息。正常玩家投影只能选择本人手牌和公开数量，不可把整个对象传给客户端；PlayerViewSnapshot现只含本人手牌和PublicCardSummary，显式测试观察只额外提供双方手牌，不返回牌库或RNG。

## 初始发牌

`InitialDealService.Create(catalog, definition, random)`先验证初始卡牌与足够牌数，然后只推进`random.Fork()`。定义的HandTarget来自GameTurnContext的共享参数，不在服务里硬编码8。沿用已确认的数字重放约定：

1. Early普通牌按CardId Ordinal建立序列（CardCatalog查询保证此顺序）。
2. Fisher–Yates从末到首；每步NextInt(0,i+1)。
3. 从洗后首端USSR、US交替发到目标，剩余牌保持洗后顺序。
4. 中晚期普通牌留在EraReserve；中国牌独立。弃牌、移出与结算区初始为空。
5. 返回InitialDealResult，包含新的CardZones及推进后的RandomSnapshot；原随机源未改变。

```mermaid
flowchart LR
 A[审核目录与开局参数] --> B[检查牌数和独立中国牌]
 B --> C[Fork随机源]
 C --> D[早期牌洗牌与交替发牌]
 D --> E[校验全部实体唯一完整]
 E --> F[返回牌区和随机快照]
 F --> G[后续由宿主共同提交]
```

官方初始牌数来自共享参数；排序、Fisher–Yates方向和交替发牌是工程重放约定，不冒称官方额外条款。当前规范数据形成双方各8、牌库19、预备67、中国牌1。这些计数仅在测试中作独立预期，业务服务从目录与参数推导。

函数不接收既有局面，不承担创建请求去重；调用方重复调用相同输入得到相同结果，不等于游戏会话已实现幂等。失败/随机耗尽不发布半副牌；事务宿主接入时必须共同提交牌区与随机快照，不能只采用其中一半。

## 数据加载边界

`InitialDealDefinitionLoader.LoadSubset`接收setup、其Schema、turn-flow文本和已审核CardCatalog；核对版本/身份、根与中国牌Verified来源、两个受支持JSON Pointer以及原始数组首项确实为early且first_turn1，再通过TurnCalendarLoader读取参数。原数组重排不能被Calendar内部排序掩盖。中国牌ID/类型/初始持有面与牌数需成立。

Schema检查其他字段的结构，但本接口不解析DEFCON/太空/军事/VP引用，不认证部署事实，也不审核全部事件处理器。返回值不是可执行RulePack，不能绕过完整开局门禁。

## 实际验证

CardZonesTests覆盖唯一完整分区、只读集合、时代与中国牌；InitialDealTests覆盖固定小牌组顺序、实际103牌、随机隔离与失败；InitialDealLoaderTests覆盖真实引用和变异错误。测试数量与原始XML摘要见[验证报告](../Testing/initial-card-deal.validation.json)。该报告对应纯服务批次；随后Demo集成的测试、播放器与截图见[真实手牌报告](../Testing/dealt-setup-demo.validation.json)。

GameState已成对持有牌区与随机快照，拒绝与旧fixture手牌混用；状态复制保留两者。测试开局可先看真实手牌再部署，见[Demo说明](first-demo.md)。完整出牌、头条、轨道和胜负继续分批实现。

2026-10-14运行时更新：[中国牌试玩](china-card-playtest.md)已接普通四用途/立即背面转交与[地区OPS预算](regional-ops-budget.md)；回合末翻面、事件转交、越南效果等仍按上述设计待接。该子集不代表完整回合编排。
