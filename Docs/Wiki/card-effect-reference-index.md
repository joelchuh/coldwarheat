# 卡牌效果引用索引

description：只需判断参数中的EffectId是否存在时使用；先读模块摘要，再看Load。不是完整卡牌加载器。

状态：已实现；实际边界测试见 [295项回归报告](../Testing/allocation-demo.validation.json)。

[模块JSON](../Descriptors/card-effect-reference-index.module.json) → `CardEffectReferenceIndex.Load` → `BudgetedRemovalCatalogLoader.Load` → `DemoCatalog.WithAllocationScenarios`。

输入是正式 `cards.json`，仅接受schema2、content0.45.0、Deluxe2015、release-candidate。检查卡牌稳定ID、kind、效果绑定与唯一性；China条目必须是card.china且显式空绑定。返回按Ordinal排序的只读EffectId集合，当前103张牌得到102项。卡牌最多256条是解析技术边界，不是游戏规则；JSON大小和深度由ContractJson限制。

该集合仅证明ID在所读文件中存在。它不验证印刷OPS、星号、来源、参数状态、所有卡牌契约或处理器可运行性，不返回RulePack，不绕过待实现标记。完整装配继续看 [正式数据](map-card-data.md)。

```mermaid
flowchart LR
  Cards[唯一卡牌定义 JSON] --> Index[只读效果ID索引]
  Index --> Loader[两牌参数加载与引用核验]
  Params[第二批共享参数] --> Loader
  Loader --> Catalog[Demo人工场景装配]
```

规则数值仍存在共享参数里；不要为新脚本复制另一份效果ID常量表。版本不匹配时报错，先审核新数据再升级支持范围。
