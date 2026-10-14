# 快速试玩入口

BASE: 8a18774934f863cc69974b2bc5c3d159fd9c45d0

目标：用户打开独立PlaytestDemo即可从真实手牌选择行动，减少部署准备；保留手动部署练习、可替换地图、只读双手查看。属于既有Demo工厂与部署流程的有界改动，用户已要求自主持续优化Demo。

设计：新增明示fixture JSON，引用真实初始发牌场景，独立保存示例自由部署分配。WithQuickStartScenario只在Risk/Scoring均装配后追加场景；CreateHost复用InitialDeploymentFlow校验/应用每个stage，再Plan(StartPractice)，不直接篡改国家或伪推进正式头条。两边分配以服务的Region/FreeAmount校验，所有初始牌/随机/轨道沿用源场景。每次重开有新GameId，卡牌/随机重现。示例不是官方强制或最佳开局。

- [x] T1 数据与装配 RED/GREEN：数据新描述；DemoCatalog.QuickStart.cs严格字段/来源/重复/分配验证；DemoScenario保留不可变分配；主CreateHost串既有flow。测试正常地图与轨道/源场景不变、错误地区/总数/重复/顺序/未知源/坏scope拒绝、重复载入拒绝、快开不消耗RNG（首次太空与手动部署比较）、新局独立及真实双方行动。
- [x] T2 默认入口 RED/GREEN与界面/构建：ScenarioIndex优先快开，旧fallback保留且显式指定优先；Bootstrap与Editor都加载同源JSON并复制35份载荷；独立BuildPlaytest使用唯一窗口标题且构建后恢复Editor产品名。实际capture检查行动地图和手牌，真实鼠标证据分列。
- [x] T3 更新Wiki/JSON/验证/worklog；完整检查后一次独立审查，常规commit/ff/push核对SHA。

Review focus: 快开只能复用合法部署，不偷加影响力；固定牌/RNG和源场景保持；每次重开独立；默认入口改变有明确fixture标签和手动返回路径；视图/构建同一数据，窗口标题只在新产物生效。

不新增正式游戏规则、活动事件、头条/回合末；不修改早期牌组或map接口，不添加一次性永久脚本。
