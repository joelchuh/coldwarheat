# 卡牌静态目录

description：按CardId、印刷编号或时代查询不可变卡牌定义；定位三类牌不变量、条件Schema和冻结profile。用于牌堆/手牌的后续基础，不是可执行事件注册表。

状态：2026-10-13已实现印刷静态子集，634项EditMode通过；本批未修改Demo UI或重建播放器。先读[模块描述](../Descriptors/card-data.module.json)、[卡牌数据描述](../../Data/Descriptors/card-definitions.data.json)、[印刷profile描述](../../Data/Descriptors/card-printed-profile.data.json)。

## 类型与查询

`Core/Domain/CardId.cs`使用Ordinal稳定值身份，拒绝空值、非法前缀、过长标识与尾换行。默认struct不代表有效卡牌，目录Contains返回false，GetCard明确抛错。

`CardDefinition`不保存持有者、手牌位置、公开状态或Unity资源。Event有1–4印刷OPS、US/USSR/Neutral阵营、时代、效果声明；Scoring的OPS为空、阵营None、有计分地区；China固定身份card.china、独立牌组、时代为空且没有事件绑定。中国牌的具体印刷OPS由数据/profile冻结，Core不复制一份数值常量。

`CardEffectBinding`只保存效果ID、版本与只读参数引用。NoParameters必须零引用，Bound必须非空且不重复；Unresolved不能进入此目录。DatasetId允许既有数据中的连字符；JSON Pointer仅检查语法，不在这里打开文件或解析参数类型。handler_key不会变成反射入口。

`CardCatalog.GetCard`、`GetPrintedCard`未知即失败。`GetEraCards`返回Ordinal排序的只读定义集合，不包含中国牌。构造时防御复制并拒绝重复ID/印刷编号、空/null或超256条输入；这个条目上限是工程边界，不是卡牌规则。实际基础目录为103张：95 Event、7 Scoring、1 China，早/中/晚时代集合为35/46/21。

## 加载与冻结

`CardCatalogLoader.LoadPrintedSubset(json, schema, approvedProfile)`不读取任意文件路径。调用方提供文本，依次经过：

1. ContractJson有界解析与cards-v2 Schema验证。
2. 锁定cards.deluxe2015.definitions、v0.45.0、ruleset.deluxe2015和release-candidate。
3. 印刷行来源全部Verified且定位/来源存在，来源ID唯一；效果声明ID唯一，绑定版本和声明匹配。
4. Core类型/引用集合不变量，以及103牌、印刷编号1–103和唯一中国牌覆盖。
5. 比对独立冻结profile，成功后发布整个不可变目录。

[cards.printed-profile.json](../../Data/Cards/deluxe-2015/cards.printed-profile.json)来自记录的Git提交df3a649中的既有cards.json，不是运行时拿候选源生成期望值。规范化由`CanonicalJsonDigest.Compute`完成：按Ordinal排序对象键、保留数组顺序、紧凑JSON UTF-8 SHA256；与地图profile共享算法。排版/对象键顺序不影响摘要，卡牌事实或数组顺序变化会被拒绝。修改源后须重新核对并明确更新profile；这不是文件传输的原始字节摘要，也不是官方内容认证。

打印事实与事件规则是两层审核：现有104条印刷来源引用全部Verified；效果声明仍有19条Pending引用。此接口允许保留已登记的效果声明用于静态查询，不发布处理器、不解析参数目标、更不会打开完整RulePack门禁。完整可运行规则包仍必须拒绝依赖未核实来源、未实现处理器或缺失服务。

## Schema支持范围

`ContractJson`为仓库契约补充maximum、allOf、if/then/else：allOf全部成立；if成功校验then，失败校验else；缺失分支不加限制；没有if时then/else不作用于数据。语义依据[条件校验文档](https://json-schema.org/understanding-json-schema/reference/conditionals)和[组合文档](https://json-schema.org/understanding-json-schema/reference/combining)。

执行前检查所有分支的结构，即使该分支不会选择，也不容许未知关键词、远程或循环引用。if的不匹配可选择else，但复杂度超限会继续抛出，不能被误当作条件为false。沿用深度64、共享预算100000和受支持关键词白名单；这仍不是完整Draft 2020-12验证器。

流程：描述定位 → 可信JSON/schema/profile → 只读目录 → 按ID/编号/时代查询 → 后续动态牌区/出牌服务。查询不会抽牌、消耗随机数或改变任何GameState。

## 验证与下一步

真实运行、失败修复与SHA见[验证报告](../Testing/card-catalog.validation.json)，设计/实现区别见[工作记录](../Worklogs/2026-10-13-card-catalog.md)。核心测试入口为CardCatalogTests、CardCatalogLoaderTests和ContractConditionalTests；既有地图profile与部署/事件回归共同通过。

可恢复随机源与[独立牌区/初始发牌](card-zones.md)已实现，已接入GameState与手牌预览；静态目录不承担这些动态责任。现有八个事件算法仍需正式出牌生命周期接线，不能因为本页有103个定义就称102个事件全部可玩。
