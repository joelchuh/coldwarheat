# 卡牌静态目录实现计划

> 使用executing-plans在当前会话逐项执行，最后一次独立审阅。沿用用户已批准的静态JSON/纯C#/描述优先架构和持续开发授权，不重复请求编码批准。

目标：把现有103张牌的印刷定义变成只读、类型明确的C#查询目录，为后续牌堆和手牌提供稳定CardId。本批不实现发牌或正式出牌，不将102个参数绑定解释为102个可执行效果。

设计依据：Docs/Wiki/map-card-contracts.md第5–7节，Docs/Descriptors/card-data.module.json，已核对的cards.json v0.45.0与cards-v2.schema.json。

## 边界与取舍

- `CardId`与不可变`CardDefinition`属于Core，区分Event/Scoring/China、PrintedOps、Affiliation、Era、DeckEntry、移出标记、显示提醒、计分地区和只读参数引用。无Unity、文件路径、Owner、牌区或反射执行。
- `CardCatalog`只提供按ID/印刷编号/时代查询。构造时拒绝重复、未知枚举和不完整类型组合，复制外部集合，Ordinal排序；不是RulePack或处理器注册表。
- `CardCatalogLoader.LoadPrintedSubset`在Infrastructure校验Schema、锁定版本、来源引用、效果契约ID/版本、103牌覆盖与独立冻结profile。既有卡牌源不改；profile从已提交基线冻结，运行时不重算批准值。Bound引用只保留标识，不在这里读取所有参数文件或装载handler。
- 复用有界ContractJson，补充cards-v2实际使用的maximum/allOf/if/then/else；预检查未执行分支，继续拒绝未知关键词、远程/循环引用和复杂度超限。不是完整JSON Schema实现。
- 提取稳定JSON摘要小工具供地图与卡牌profile复用；对象键排序，数组顺序保留。所有边界通过真实EditMode验证。

## Task 1：支持卡牌结构契约

- [x] ContractConditionalTests先失败：maximum上下界、allOf全满足、if选择then/else且保留兄弟约束、未选分支非法/循环、复杂度不被分支catch吞掉。
- [x] 最小实现关键词检查及有界验证；原合同与地图回归继续通过。

## Task 2：只读目录与加载

- [x] CardCatalogTests/CardCatalogLoaderTests先失败：真实103牌、35/46/21时代牌、中国牌独立、7计分牌、查询/不可变、错误组合/未知值/重复/缺牌/版本/来源/绑定/摘要。
- [x] Core定义与Infrastructure加载器；已提交cards基线profile、JSON描述及索引。默认UI继续原12练习，不重建没改变的播放器。
- [x] 更新Wiki/模块描述/报告/工作日志并完成独立审阅；Git提交推送由收尾执行并核对远程。

## Review Focus

1. 计分牌PrintedOps必须为空，中国牌不属于任一时代；不得用0OPS混淆。
2. effect声明/Bound不授权事件执行；未知参数目标并不被静态目录冒称已解析。
3. 条件Schema的错误约束在未选分支也必须被预检查，复杂度错误必须传播。
4. Profile必须来自记录的Git基线，不从候选输入自我批准；规范化不改变数组顺序。
5. 公开目录无手牌、Owner和运行状态；查询未知ID/编号明确失败，不能默默选一张牌。

当前计划状态：印刷静态子集已实现，634项测试通过，独立审阅通过，一处Minor文档陈旧项已修正。基础提交df3a649dbc130fb766033b90effd4340182de3ac。
