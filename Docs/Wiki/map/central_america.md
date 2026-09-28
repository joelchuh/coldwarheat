# Central America：国家与邻接

> description: Central America国家、稳定度、基础战场、子区与双向规则邻接的派生浏览表。
> 数据源：[world.json](../../../Data/Maps/deluxe-2015/world.json)，content_version=0.1.0；仅在源数据变更后重新生成本表，不单独修改数值。
> 来源：M-DELUXE该地区国家框与连接线；图形布局和现代国界不产生规则边。

| CountryId（country.省略） | 名称 | 稳定度 | 战场 | 子区 | 普通国家邻接（country.省略） | 超级大国邻接 |
|---|---|---:|---|---|---|---|
| mexico | Mexico | 2 | 是 | — | guatemala | superpower.us |
| guatemala | Guatemala | 1 | 否 | — | el_salvador, honduras, mexico | — |
| el_salvador | El Salvador | 1 | 否 | — | guatemala, honduras | — |
| honduras | Honduras | 2 | 否 | — | costa_rica, el_salvador, guatemala, nicaragua | — |
| nicaragua | Nicaragua | 1 | 否 | — | costa_rica, cuba, honduras | — |
| costa_rica | Costa Rica | 3 | 否 | — | honduras, nicaragua, panama | — |
| panama | Panama | 2 | 是 | — | colombia, costa_rica | — |
| cuba | Cuba | 3 | 是 | — | haiti, nicaragua | superpower.us |
| haiti | Haiti | 1 | 否 | — | cuba, dominican_republic | — |
| dominican_republic | Dominican Republic | 1 | 否 | — | haiti | — |

[返回数据索引](../map-card-data.md)
