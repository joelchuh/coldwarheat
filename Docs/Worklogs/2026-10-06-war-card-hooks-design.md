# 战争牌持续与取消设计（2026-10-06）

基线ca5ac313f4916e43ff87a132b719c22ab6b08569，第二批战争已普通推送且线上main/HEAD/origin一致、工作区clean。按用户持续授权和AGENTS §§22–23推进设计，不进入C#/Unity实现。

本批Flower #59、Camp #65、Evil #97的已知参数绑定。增加[CardUse钩子模块](../Descriptors/card-play-hooks.module.json)，复用ActiveEffect、固定影响力、VP、成功事实及取消/阻止；区分实际CardPlayer与根行动者/事件控制者。数据与Schema闭合，单一战争集合；Arab禁用门禁引用WAR1，成功历史引用条约参数。[Wiki](../Wiki/war-card-hooks.md)链接[数据描述](../../Data/Descriptors/war-card-hooks.data.json)、参数/案例/报告，未在运行UI放实现细节。

来源目视英文卡图p9 #59/p10 #65/p14 #97，R2015及FAQv5；Evil明确同时取消与未来阻止。Camp按卡文VP→三国影响力→完整成功事实，VP终局截断后续为明确工程编排约定。Flower处罚先后与UN压制交互未找到官方专门覆盖，已向用户提出项目解释；收到采纳前保持RequiresRuling，不采第三方冲突评论作规则。Grain Sales/Star Wars等子使用来源未设计，也明确停止。

主临时检查器24数据+56固定参考=80新项，旧业务776项回归，共856通过；12runtime计划未运行，C#/Unity测试0。第一次字段检查误写persistent_reminder，读取实际CardDefinition后修正为display_persistent_reminder并完整重跑；正式数据与业务期望未为错误检查器改变。[报告](../Design/war-card-hooks.validation.json)记录12文件SHA与18Schema/22数据/16模块。参考身份/来源/历史可信由夹具假定，不证明真实鉴权、事务或存档。

独立只读审阅没有重要阻塞：独立PS核对12摘要、118JSON解析、目录/绑定/指针/16模块无环及171相对链接；目视英文卡图并核对官方FAQ/规则。其唯一内存检查器尝试在jsonschema导入时PermissionError，未重试/提权，故不声称独立重跑856或Schema meta验证。提交前刷新链接/摘要，核验候选/磁盘/暂存一致；逐批普通推送后核对线上main。本日志不写自身未来提交SHA，实际Git核验在聊天与下一批基线记录。

库存28 Bound / 74 Unresolved，102处理器均planned。下一批Cuban Missile Crisis、Nuclear Subs、SALT Negotiations、ABM Treaty，继续核对DEFCON政变前置与事件授权行动；Flower两项待裁定不阻塞其他有依据设计。
