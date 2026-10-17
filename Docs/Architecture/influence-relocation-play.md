# 去斯大林化：完整影响力迁移

description：将已有De-Stalinization设计接为可玩事件，复用影响力事务、固定身份会话与地图接口。

用户目标：继续扩大可实际游玩的Demo；规则纯C#、静态参数单源、画风可替换，数据和模块都有JSON摘要，工作进入Git。沿用用户自主决定授权，未确认规则不擅自补齐。

## 设计选择

采用既有InfluenceEvent家族的新Relocation决策类型，复用InfluenceMutation和事件收尾；不为单张牌另建平行宿主，也不把JSON扩展为通用指令语言。地图只编辑完整来源/目的草稿，后端一次校验并提交，既有正常手牌、对手事件+OPS、头条和幂等路径继续复用。

## 规则范围

正式数值从Data/Cards/deluxe-2015/influence-relocation.parameters.json及现有Schema读取。#33固定苏联选择自身最多4点，每个目的国新放最多2点，目的地不得美国控制；不使用OPS邻接或敌控双倍费用。来源可以在美国控制国；美国影响力始终保持。普通MapDefinition不含超级大国/中国内战空间。R2015 §6.1.1，英文卡图p6 #33和FAQv5 PDF p7 #33；既有逐卡英文核对记录保留，本次重新读取FAQ确认事件迁移不使用OPS限制。

现有DS1-OVERLAP仍待裁定：任何来源/目的同国的完整方案返回RequiresRuling，不做净额抵销，也不把未支持说成官方禁止。地图编辑器暂不提供这种方案，并明确说明。双方空数组表示零迁移，依照已记录up-to解释完成并按印刷星号移出；不要求必须搬满4点。未知活动干预/部署/终局继续用既有门禁。

## 核心与契约

RelocateOwnInfluenceEffect继承InfluenceEffectDefinition，读取MaximumTotal和PerDestinationCap。InfluenceEventSelection.Relocate复制有界Sources/Destinations；空数组有效，null/重复国家/非正数量/超过来源/总量不守恒/超上限/非法国家/美国控制目的/错误Owner/过期版本均拒绝。来源与目的总额用long核对；算术、状态版本及事件序号溢出不得部分修改。

InfluenceEventQuery增加来源容量、目的容量和最大总量；顺序按稳定国家ID。不把UI草稿写入GameState。合法查询仅给相应选择者；默认空源可自动无变化完成，其余允许明确零迁移。RelocationRules在独立工作副本里先移出、后移入，排序输出可重复的InfluenceChanged记录；总苏联影响力守恒，美国与DEFCON/军事/VP/随机/卡牌不由纯计划更改。

Infrastructure严格校验唯一数据集/版本/语义/来源/已知EffectId，读数值不在脚本或UI重复写4和2。基础服务可测后才注册PracticeInfluenceCatalog；卡牌生命周期、星号、根槽和对手OPS顺序由已有编排完成。

## 会话与表现

EventCommand新增ResolveRelocation及两个有界列表，固定会话确认Owner、ExecutionId/DecisionId/Version；同ID异内容比较包含两个列表，同ID同内容返回原收据。错误不发布工作副本，完成仅推进一次根；头条完成后继续下一头条或普通行动；US用OPS触发仍由USSR选。

DemoController保存移出/移入两个草稿和当前编辑页；切身份/版本/决策/重置后清空。地图点国家只定位，按钮改草稿；总额相等且合法才允许确认，两个空草稿提供明确“不迁移并完成”。显示总已移出/待移入、每国上限与当前国家，保留重发和结果反馈。新增独立人工练习覆盖苏联直接发动与美国OPS触发，不复制规则数值到场景。画风只通过现有MapPresenter/IMapView交换。

## 验证与交付

先纯规则/装载RED→GREEN，再核心流程/会话/控制器RED→GREEN；全量Unity原生测试。一次fresh整批审查，Important一次定向修复，明确UI与测试证据区别。新增Builds/RelocationPlaytest，保留旧Player，使用共享载荷清单和只读脚本核验；实际鼠标完成至少一条完整迁移与一次零迁移/重发。配套描述、Wiki flow、工作记录与验证报告，提交并推送分支。
