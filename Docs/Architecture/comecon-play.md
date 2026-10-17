# 经互会可玩接线

description：把既有经互会参数接入可复用的不同国家放置、真实出牌和地图选择；短缺仍待裁定。

用户要求持续自主开发、共享数据与画风分离。本批沿用现有CardAction/InfluenceEvent/session/map，不建新的逐卡宿主。规则从effects.deluxe2015.early_influence_batch3的comecon读取：苏联选择四个不同、非美国控制的东欧国家，各增加一点。依据既有英文卡#14、R2015控制和事件放置条款；2026-10-17重新查询GMT英文卡图提取核对。官方来源 https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf ，不是旁列译文。

复用AddToDistinctCountriesEffect并增加封闭InfluenceControlFilter枚举Any/NotOpponentControlled；旧构造调用默认Any，未知枚举拒绝。查询先取正式子区并去重，再按当前权威控制过滤。苏联控制与无控制均合法；有美国影响力不等于美国控制。奥地利/芬兰双子区从world读取；不引入OPS邻接、DEFCON或敌控费用。已有Plan完成所有目标验证后在工作副本添加，保留排序/原子拒绝边界；新过滤模式按稳定ID发出变化。

ComeconDefinitionLoader严格检查既有schema/profile/具名card/effect、USSR、东欧、过滤、target_count/amount正整数以及Verified来源，数值不写到UI。只绑定经互会，不改变IndependentReds消费者。数据shortage_policy=RequiresRuling保持生效：不足四国（包括零）拒绝依赖结算，不自动减量、不移牌、不推进根槽。EI3-RULING-01不能因其他牌的用户解释而自动解答；未知活动干预继续共享门禁。

真实出牌通过既有PracticeInfluenceCatalog和CountrySet选择。直接美/苏出牌、US OPS前后、头条完成都固定USSR选择；重发只返回旧收据。DemoCatalog.WithComecon给已有定义附加效果，保留奥运会/战争/手牌/头条/最终计分配置。单个新人工fixture和现有地图控件提供四国草稿；仅fixture设置手牌和影响力。共享Player清单仅新增该fixture；正式参数/schema已有载荷映射。

与新增独立效果类型相比，封闭过滤枚举复用已有数量/选择/事务，减少相同放置算法；不采用任意JSON谓词或脚本DSL。除明确过滤模式外，不改变旧事件候选或输入顺序语义。

测试分核心装载及完整生命周期两批，先观察RED再GREEN。独立审查一次；新ComeconPlaytest保持旧播放器。Windows更新弹窗若仍遮挡，继续标记鼠标未验收，不以自动测试替代。持久化、联网、NATO和其他持续事件均不因此被宣称已实现。
