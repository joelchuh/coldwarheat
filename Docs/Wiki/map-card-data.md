# 正式地图与基础卡牌数据

> description: 按地区查国家及规则邻接，按时代查卡牌印刷字段；定位唯一JSON、来源、参数迁移与未实现范围。
> 状态：2026-09-28，已录入并逐项目视核对的定义候选；尚无Unity工程、C#查询/加载服务或可运行事件处理器。

## 权威文件与检索

先读[数据目录](../../Data/catalog.json)的description，再打开命中描述，用稳定ID/时代/地区筛选所需记录。工具可解析整个JSON建立字典，但只向调用方返回命中项；不需要把全文交给模型。下列分组表由定义派生，供浏览和复核，不作为第二份规则输入。

| 数据 | 唯一文件 | 读取方式 |
|---|---|---|
| 国家、地区、邻接 | [world.json](../../Data/Maps/deluxe-2015/world.json) | countries按primary_region_id筛选；country_edges按端点筛选，查询时双向展开 |
| 卡牌基础事实 | [cards.json](../../Data/Cards/deluxe-2015/cards.json) | cards按era或id筛选；中国牌kind=China单独读取 |
| 英文短名称 | [名称字典](../../Data/Localization/en-US.catalog-names.json) | entries[name_key]；仅标题，rules_text_key正文尚未编写 |

## 地区索引

| 地区 | 已录入国家 | 战场国 | 查阅 |
|---|---:|---:|---|
| Europe | 21 | 5 | [europe](map/europe.md) |
| Asia | 15 | 6 | [asia](map/asia.md) |
| Middle East | 10 | 6 | [middle_east](map/middle_east.md) |
| Africa | 18 | 5 | [africa](map/africa.md) |
| Central America | 10 | 3 | [central_america](map/central_america.md) |
| South America | 10 | 4 | [south_america](map/south_america.md) |

共有84个国家、6主地区、3子区、112条普通国家边和9条超级大国边。中国内战框是可选规则，未加入国家数组。奥地利、芬兰同时属于东西欧；台湾的基础战场值为false，逐卡效果不能覆盖原定义。

本次特别复查：黎巴嫩—约旦存在、以色列—伊拉克不存在；韩国—台湾存在；缅甸—泰国不存在；意大利—利比亚不存在。跨地区红色虚线仍是普通规则边，不能因为画面换成写实地图而丢失。加拿大在欧洲计分区，老挝/柬埔寨等组合框仍是单个规则对象。

## 卡牌时代索引

| 分组 | 数量 | 查阅 |
|---|---:|---|
| 冷战早期 | 35 | [early](cards/early.md) |
| 冷战中期 | 46 | [mid](cards/mid.md) |
| 冷战后期 | 21 | [late](cards/late.md) |
| 中国牌特殊组 | 1 | [china](cards/china.md) |

编号1–103共103张：95张Event、7张Scoring、1张China。Defectors（#103）按卡面归早期，不能按编号上限推成后期。第16页明确标OPTIONAL的104–110不启用；不从组件总卡数推断基础牌范围。中国牌保留OPS=4，但era=null、affiliation=None、SpecialChina是特殊生命周期的规范化表达；卡面早期横幅不等于进入早期牌库。

## 效果与参数的状态

新[卡牌Schema v2](../Contracts/cards-v2.schema.json)保留v1合成样例，新增parameter_binding_status：

- Unresolved：尚未设计参数，parameter_refs必须为空；禁止进入可执行规则包。当前42个非中国牌绑定为此状态；三批影响力10牌、首批持续效果3牌、条约2牌、北约前置2牌、戴高乐/勃兰特2牌，加拆墙1牌及战争5牌、战争牌持续/取消3牌、DEFCON事件4牌、影响力迁移1牌、手牌事件2牌、动态计分2牌、计分7牌、DEFCON/VP3牌、中期影响力4牌、事件太空1牌、教皇链2牌、国家指标VP3牌、中期影响力第二批3牌，共60牌已完成参数绑定。
- NoParameters：已逐卡核实确实无需外部参数，引用必须为空。不能靠默认值采用。
- Bound：至少一个引用；加载器仍要校验dataset_id、JSON Pointer和该效果的参数类型。

所有effect_contracts仍标planned；[第一批5牌](early-influence-batch1.md)、[第二批3牌](early-influence-batch2.md)和[第三批2牌](early-influence-batch3.md)以及[持续效果首批3牌](persistent-effects.md)及[条约保护2牌](treaty-protection.md)及[北约前置2牌](alliance-prerequisites.md)及[北约局部例外2牌](nato-exceptions.md)和[拆墙1牌](event-operations.md)、[战争3牌](war-events.md)及[战争第二批2牌](war-events-batch2.md)已绑定参数，其余42个仍保留Pending项目设计。handler_key只预留显式注册键，不能反射、eval或下载执行。结构验证接受这些设计记录，不代表运行门禁放行；C#加载器实现后必须拒绝Unresolved、Pending证据、未注册处理器和缺少setup。影响力四批12牌已有执行/选择设计，并补充戴高乐/勃兰特组合事件与拆墙可选欧洲行动，第二批短缺策略已获用户确认，首批持续效果已设计，其他持续效果、额外交互与其他卡牌仍待设计。计分服务已有通用设计，也不据此假称计分卡处理器已实现。

scoring-card-parameters已升级schema_version=2、content_version=0.2.0：仅保留中国牌final_holder_vp。东南亚remove_after_resolved_event唯一来源为cards.json的card.southeast_asia_scoring；旧消费者必须迁移，不回退读取已删除的参数。地区权重仍只来自region-scoring，太空阈值仍只来自space-race。

## 画面更换接口

规则只使用CountryId/CardId。MapLayout通过node_id、anchor、pick_shape_key关联地图；Theme通过country_overrides/card_overrides及默认资源键提供外观。名称来自独立字典。更换文明式、纸地图或写实外观时修改布局/主题及对应视图适配器；稳定ID、邻接与印刷规则值保持原意。2D变3D需要新的布局和视图适配器，不承诺只换一张贴图就完成。

~~~mermaid
flowchart LR
 A[Data目录 description] --> B[命中数据描述]
 B --> C[按地区或时代定位记录]
 C --> D[规则图与卡牌定义]
 D --> E[规则服务：待实现]
 D --> V[视图组合：待实现]
 L[可替换MapLayout] --> V
 T[可替换Theme与名称] --> V
~~~

## 核对、限制和下一步

原始来源及SHA见[资料登记](rules-sources.md)。本次是同一执行者对官方图像的逐项目视转录与复查，加上结构/引用/领域断言；不是第二位审阅者的独立审核，不能将自动化通过理解为官方内容认证。派生表与JSON一致只能证明同步，不能证明源规则无误。完整规则包仍为候选；[setup定义](setup.md)已建立，最终manifest与运行处理器尚未完成。

静态核对结果：26项新增数据/结构案例和31项既有Schema案例通过；两项新增运行案例待实现。验证项见[数据案例](../Design/map-card-data.cases.json)，执行证据见[工作记录](../Worklogs/2026-09-28-map-card-data.md)。没有运行Unity或C#业务测试。初始影响力与双方自由部署约束已见[开局设计](setup.md)，下一步按小批卡牌冻结参数、效果流程和边界案例；保持设计优先，完成规则设计后再进入核心代码实施。

[表现层契约](map-presentation.md) · [返回索引](index.md)

[战争牌持续与取消](war-card-hooks.md)绑定Flower、戴维营与邪恶帝国；已知参数与待裁定边界分开记录。

[DEFCON事件四牌](defcon-events-batch1.md)参数已绑定，服务仍planned。
