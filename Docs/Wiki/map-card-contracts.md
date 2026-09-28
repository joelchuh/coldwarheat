# 地图与卡牌数据契约 v0.2

> description: 定位国家/地区/邻接、卡牌类型/效果绑定、规则包摘要、加载步骤与正式数据验收边界；不包含地图美术设计。
> 状态：2026-09-28；结构契约及正式地图/卡牌印刷定义已建立，Core查询/加载服务与逐卡效果尚未实现。JSON Schema校验不代表游戏可运行。
> 入口：[契约目录](../Contracts/catalog.json)、[地图模块](../Descriptors/map-data.module.json)、[卡牌模块](../Descriptors/card-data.module.json)、[地图展示方案](map-presentation.md)、[验收场景](../Design/map-card-contracts.cases.json)。

## 1. 目标与选择

用户要求继续完成正式地图与卡牌的数据契约，同时保持画风未定。字段、类型、引用、版本与失败边界已建立；后续按用户授权录入了84国与103牌，见[正式数据](map-card-data.md)。当前仍不开始Unity实现。

选用独立JSON静态定义与纯C#不可变对象。把坐标直接写进国家定义会使布局调整污染规则包；把事实只保存在Unity场景或ScriptableObject里会限制无引擎测试与服务器复用。当前独立数据方案与既有架构一致；将来Unity资源适配器可以消费它，但不能成为第二份规则事实。

## 2. 文件层次与读取方式

[Docs/Contracts/catalog.json](../Contracts/catalog.json)的description用于选择Schema；Schema内部description及字段说明用于了解结构。它们是契约文件，不是可执行模块或规则数据包，因此与Data/catalog.json中的数据实例分开登记。正式地图、卡牌及名称实例已各有数据描述与稳定dataset_id。

| 契约 | 内容 | 权威所属 |
|---|---|---|
| map.schema.json | 地区、国家、超级大国、规则邻接与来源 | Core静态定义 |
| cards-v2.schema.json（v1保留样例兼容） | 卡牌印刷事实、类型、绑定状态、参数引用和来源 | Core静态定义与显式效果注册契约 |
| layout.schema.json | 显示位置、拾取形状键、连线绘制路径 | Presentation布局 |
| theme.schema.json | 资源键、卡框、字体和外观覆盖 | Presentation主题 |
| manifest.schema.json | 规则包文件、版本、摘要及覆盖审核 | Infrastructure加载与会话版本绑定 |

每份Schema使用JSON Schema Draft 2020-12，根对象和正式记录拒绝未知字段。跨记录ID唯一、引用、图结构和资料真实性另作语义校验；不能把Schema成功当作这些检查通过。规范参考[JSON Schema Core](https://json-schema.org/draft/2020-12/json-schema-core)和[对象约束](https://json-schema.org/understanding-json-schema/reference/object)。Schema的urn标识由离线契约注册表解析，不自动联网寻找代码或Schema。

规则实例共同记录schema_version、dataset_id、content_version、ruleset_id、scope、description、source_catalog。scope=fixture只供测试；release-candidate仍须完整审核。逐项source_refs具有source_id、locator、verification；正式包拒绝Pending/Fixture证据、未登记来源和空定位。字段组可共用一个已说明覆盖范围的定位；值来自不同版本时拆分定位并记录采用依据。

## 3. 规则地图：图结构，不是世界坐标

| 对象 | 字段 | 不变量 |
|---|---|---|
| RegionDefinition | id、kind、parent_id、name_key、source_refs | 六个ScoringRegion无父节点；Subregion只指向一个ScoringRegion |
| CountryDefinition | id、name_key、stability、base_battleground、primary_region_id、subregion_ids、source_refs | 一个主地区；可属于多个同父子区；稳定度为正整数 |
| SuperpowerDefinition | id、side、name_key、source_refs | 恰好US/USSR各一个；无稳定度、战场值、普通影响力 |
| CountryEdge | a、b、source_refs | 国家间无向规则边；无自环，端点必须存在 |
| SuperpowerEdge | country_id、superpower_id、source_refs | 国家与超级大国规则边，不能退化成可放影响力的普通国家 |

country_edges只存一份边，要求a按ordinal顺序小于b；加载时生成双向查询索引。拒绝反向重复边与重复端点对，不自动修复原件或补猜地图连线。邻接不是由坐标距离、多边形共边、现代地理或视觉线条推导。超级大国邻接使用独立集合；不把它与普通国家控制国数量混计。

主地区采用既有region-scoring数据的六地区ID。子区规划region.eastern_europe、region.western_europe、region.southeast_asia；东南亚父区为亚洲，东西欧父区为欧洲。奥地利和芬兰可同时属于东、西欧，欧洲查询先按国家ID去重。国家的有效地区由主地区和子区关系派生，不再保存一份手写all_regions数组。图上由多个现实国家组成的规则区域仍只有一个CountryId。

规则依据为R2015 §2.1.1–6；本次复核该文件第2–3页。美术国界仅表达视觉布局；不替代棋盘的国家/区域语义。CountryState中的当前影响力、控制查询和持续修正不写入静态文件；计分临时战场修正也不覆盖base_battleground。

## 4. 地图接口与消费者

以下为待实现符号，模块entrypoints为空：

| 拟定入口 | 输入 → 输出 |
|---|---|
| MapDefinition.GetCountry | CountryId → 只读CountryDefinition或UnknownCountry |
| MapDefinition.GetCountryNeighbors | CountryId → 按ordinal稳定排序的普通国家邻接集合 |
| MapDefinition.GetSuperpowerNeighbors | CountryId → 超级大国ID集合 |
| MapDefinition.GetCountriesInRegion | RegionId → 主区/子区国家集合，去重并稳定排序 |
| MapDefinition.IsInRegion | CountryId、RegionId → 根据主区和子区计算的归属 |
| MapDefinitionValidator.Validate | 已解析定义与已核对地区参数 → 分项错误或有效定义 |

Core不读JSON；Infrastructure先验证再构造MapDefinition。行动服务查询规则图，地区计分服务查询地区成员和战场属性，DEFCON查询主地区；均不能读Presentation坐标。合法操作仍需各自规则服务判断，邻接本身不等于所有动作都合法。

## 5. 卡牌定义与三种类型

| 字段组 | 字段及含义 |
|---|---|
| 身份与显示 | id稳定；printed_number仅作来源定位；name_key/rules_text_key指向独立本地化内容，不含卡牌全文或美术 |
| 印刷事实 | printed_ops、affiliation、era、remove_after_resolved_event、display_persistent_reminder；不存当前修正OPS |
| 分类与进入方式 | kind=Event/Scoring/China；deck_entry=EraDeck/SpecialChina |
| 行为入口 | effect_binding(effect_id、contract_version、parameter_binding_status、parameter_refs)、scoring_region_id |
| 追溯 | source_refs；同一id在一个内容版本中只有一个定义 |

- Event：printed_ops为整数，affiliation为US/USSR/Neutral，era为Early/Mid/Late，具有事件绑定，scoring_region_id为空。
- Scoring：printed_ops为空而不是0；affiliation=None；有地区ID与计分效果绑定。无通用OPS用途，是否必须打出、头条及用牌限制由CardPlayRules裁决。移出标记保留数据差异，不能假定所有计分牌相同。
- China：era为空、deck_entry=SpecialChina、effect_binding为空；走既有ChinaState和特殊生命周期，不进入普通牌库。正式Deluxe目录恰好一张且ID为card.china；本次虚构fixture不满足正式目录身份与覆盖审核。

Schema限定普通印刷OPS字段的基础取值范围；具体每张牌的OPS、时代、阵营仍必须逐卡对照选定版本。卡数和印刷编号不拿规则书组件总数直接当作基础牌清单：先区分可选牌、扩展和参考牌，再冻结独立审核清单。显示持久提示标记不是效果的到期条件。

CardDefinition不含Owner、手牌位置、是否公开、当前生效次数或面朝状态。这些归CardState/ChinaState/ActiveEffectState。定义目录可以公开，运行中的手牌和牌库顺序只经PlayerView投影。

## 6. 效果代码与共享参数

CardCatalog.GetCard(CardId)返回只读定义。CardEffectRegistry.Resolve(EffectId, ContractVersion)只从显式注册的处理器中查询；handler_key是注册键，不能作为类名、程序集路径、脚本或反射入口。effect_contracts声明description、版本、handler_key、实现状态与参数契约ID。声明implemented不能证明程序确实实现了它，必须与编译注册表核对；未知键和不支持版本均拒绝依赖操作。

拟定ICardEffect.Resolve(EffectContext)返回Completed或AwaitingDecision及规则计划。EffectContext提供Actor、PhasingPlayer、EffectController、来源/根帧、不可变定义、隔离状态视图和允许的规则原语；通过已授权的影响力、计分、太空等计划服务组合行为，不直接操作Unity或保存文件。待选择结果带Owner、类型化LegalTargets和版本化ContinuationState；不能保存闭包或任意可执行字符串。

卡牌专用数值放独立参数数据集。parameter_refs使用dataset_id与JSON Pointer定位所需片段；引用必须存在且通过该效果自己的参数Schema。v2通过parameter_binding_status区分Unresolved、NoParameters与Bound。Unresolved和NoParameters要求空引用，但前者只用于尚未完成的定义候选，严禁开局；后者必须逐卡核实。Bound要求非空引用，仍须核对引用和类型。v1继续用于原有合成样例，不静默兼容v2。事件推进许可、跨步骤VP时点、到期和拦截优先级属于逐卡行为契约，不从卡名或自由文本猜测。本轮没有实现这些逐卡处理器。

**已完成的迁移：** 中国牌最终持有VP继续由scoring-card-parameters提供；地区与太空数值仍从现有独立数据集读取。2026-09-28东南亚计分牌的移出标记已迁到cards.json中的唯一CardDefinition，旧参数条目已删除，scoring-card-parameters升级schema_version=2/content_version=0.2.0；读取卡牌去向的消费者须改用正式定义。禁止两个来源长期并存。

## 7. 规则包、版本和加载门禁

manifest列出dataset_id、content_version、role、relative_path、schema_contract_id、sha256。path只能解析到已授权规则包根目录内，拒绝绝对路径、越界、重复数据ID和符号链接逃逸。Schema只作语法过滤，最终必须校验解析后的实际路径。旧参数子集尚无独立Schema时，schema_contract_id可为空，但必须有其已登记的领域校验器，不能当作免检。

拟定RulePackLoader.Load从可信本地文件读取，并按下列次序工作：

1. 严格JSON解析：拒绝重复键、非有限数字、未知字段及不支持schema_version；文件大小、条目上限使用工程配置，不冒充桌游规则。
2. 结构校验，然后ID/跨文件引用/图结构/参数类型校验。
3. 与所选版本的独立审核清单逐ID比较，核对地区与完整国家、卡牌范围、来源采用结论；不能仅检查数组长度，更不能用待校验文件自己生成“预期清单”。
4. 拒绝fixture、Pending资料、Unresolved参数、未实现处理器、不支持的效果版本、缺失初始布置或基础规则参数。覆盖审核标记approved也不能替代实际检查。
5. 对清单中文件的原始UTF-8字节计算SHA-256，比较清单值；不在读取时格式化再哈希。文件换行改变也会改变内容摘要；UTF-8无BOM为发布约定。
6. 构造不可变索引，在全部成功后一次发布可用RulePack；失败不留半个目录。会话/存档绑定ruleset_id、ruleset_version、data_digest及支持的GameVersion。

DataDigest v1的输入精确定义为UTF-8文本：第一行ruleset_id，第二行ruleset_version；随后按dataset_id的ordinal升序，每行由dataset_id、TAB、content_version、TAB、sha256组成；所有行含结尾LF，无BOM。ID与版本字段禁止TAB/换行。摘要只覆盖规则数据；主题、布局和本地化资源不进此清单。各文件仍要校验角色与契约匹配。算法/代码兼容性另由GameVersion与支持矩阵校验，不能只凭相同数据摘要放行不兼容引擎。

最终基础包必须含且仅含一份map、一份cards、一份setup以及各必需rules数据集；依赖集合来自明确的规则包规范及现有目录，不因JSON结构通过就容许缺项。当前有七个规则参数子集、正式地图定义、卡牌印刷定义及独立名称字典；[setup定义及开局流程](setup.md)已建立，逐卡效果和运行服务仍待完成。样例manifest只有两文件，刻意标fixture和审核pending，不具备开局资格。

## 8. 校验结果与后续

样例均在[Examples](../Contracts/Examples/README.md)，不放进正式Data目录。结构验证使用真正的Draft 2020-12验证器；跨文件与规则案例见[验收场景](../Design/map-card-contracts.cases.json)，实现状态分别记录。本轮不把一次性检查命令变成正式工程脚本。

按地区的国家/邻接与按时代的卡牌印刷事实已录入，见[正式数据与核对边界](map-card-data.md)。现有数据的目录引用已复核，运行绑定仍未实现。初始布置已建立；下一步逐卡设计参数与执行流程，再做完整规则包审核。美术风格可在此期间独立讨论。

[地图展示方案](map-presentation.md) · [返回索引](index.md)
