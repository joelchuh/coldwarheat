# 马歇尔计划影响力基础

用户目标：持续扩充可落地的模块化规则、共享数据、可替换地图；自主决定实施细节。此批仅交付影响力候选与既有选择流程的基础，不将半张牌接入真实手牌。

## 选择与边界
复用 AddToDistinctCountriesEffect，新增 InfluenceCandidateShortagePolicy（RequiresRuling 默认、SelectAllAvailable 显式）。另一方案是复制专用马歇尔选国实现，会重复相同控制过滤和原子加点；直接开放真实事件则缺失永久成功事实，均不采用。

Query 根据当前控制与西欧子区过滤；targetCount/amount 从共享参数加载。显式 SelectAllAvailable 时 required=min(targetCount,legalCount)。零候选返回 Ready，由 Plan/Flow 一次完成 ResolvedNoChange，无空决策。合法数为正时继续由美国提交完整集合。经互会及旧消费者默认行为不变。Unknown/Forbidden/终局/部署门禁先于候选短缺处理。

MarshallPlanInfluenceLoader严格校验 alliance_prerequisites schema/profile、三项来源和已确认解释、completion_policy 引用、单牌身份/受益方/地区/策略，并绑定正式印刷牌的 Bound 参数引用。返回 AddToDistinctCountriesEffect 仅表达影响力部分；不写 EventFactLedger、不自动注册 PracticeInfluenceCatalog、无卡牌去向/北约保护。完整事件日后需由宿主把影响力和成功事实同一事务提交。

## 原子性与复用
既有 InfluenceMutation 副本执行所有加点，溢出整体拒绝；固定 US 受益方与实际出牌方分开。零变化仍推进一次版本/完成序号；正常手牌、随机状态、OPS 与持续条约不变。等待状态继续复用 InfluenceEventExecution 的 owner/version/decisionId，重复完成拒绝。

## 验收
正式数据绑定及畸形数据拒绝；西欧加拿大/土耳其/双子区筛选，苏联控制与仅有影响力区分；0..6短缺与7/8候选；输入重复/少选/多选/越界/错误身份/过期；零候选无决策；后段影响力/版本/序号溢出；既有经互会默认短缺仍待裁定。全量原生 EditMode、元数据和一次整分支审查。无播放器变化，不重建旧 Demo。

来源：Docs/Wiki/alliance-prerequisites.md 的 GMT Deluxe #23/R2015 与用户 AP1-RULING-01。当前在线 PDF 文本主要抽取旁列译文，不能把本次请求截图当作已目视英文证据；按此前已核对并由用户确认的正式参数实现，不引入新裁定。
