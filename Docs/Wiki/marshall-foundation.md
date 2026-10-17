# 马歇尔计划影响力基础

description：先定位显式短缺策略、正式数据绑定、美国选国与零候选事务；区别于完整事件解锁北约。

先读 Docs/Descriptors/marshall-foundation.module.json。规则数据仍来自 Data/Cards/deluxe-2015/alliance-prerequisites.parameters.json；数字和子区不复制到脚本或UI。当前实现与验证见 Docs/Testing/marshall-foundation.validation.json。

flow：MarshallPlanInfluenceLoader.Load → AddToDistinctCountriesEffect（NotOpponentControlled/SelectAllAvailable）→ Query当前西欧控制 → 正候选由US选择min(7,合法数)个不同国家 / 零候选直接Ready → Plan完整校验并在副本各加1 → InfluenceEventFlow完成一次影响力执行。

RequiresRuling 是旧消费者默认策略。马歇尔显式采用用户AP1-RULING-01；零候选生成一次ResolvedNoChange，无需提交空列表。错误拥有者/版本/数量、重复国家、苏控国家和溢出不留下部分变化；Unknown干预不会被短缺政策吞掉。

本模块仅是影响力基础，未加入Demo真实手牌注册、牌去向、EventFactLedger或北约保护。后续需将完整马歇尔事件的成功事实与影响力同一事务提交，才能解除正常出牌“不支持”边界。华沙事件保持待实现。地图表现可继续替换。

设计：Docs/Architecture/marshall-foundation.md；计划：Docs/Plans/2026-10-17-marshall-foundation.md；过程：Docs/Worklogs/2026-10-17-marshall-foundation.md。

验收：源码e8095b8，4006/4006原生EditMode通过，独立审查无Critical/Important/Minor；JSON链接校验190描述/90数据集通过。该批不改Host/UI，沿用已验收的JapanPactPlaytest供测试，马歇尔完整事件仍待接。
