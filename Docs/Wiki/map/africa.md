# Africa：国家与邻接

> description: Africa国家、稳定度、基础战场、子区与双向规则邻接的派生浏览表。
> 数据源：[world.json](../../../Data/Maps/deluxe-2015/world.json)，content_version=0.1.0；仅在源数据变更后重新生成本表，不单独修改数值。
> 来源：M-DELUXE该地区国家框与连接线；图形布局和现代国界不产生规则边。

| CountryId（country.省略） | 名称 | 稳定度 | 战场 | 子区 | 普通国家邻接（country.省略） | 超级大国邻接 |
|---|---|---:|---|---|---|---|
| morocco | Morocco | 3 | 否 | — | algeria, spain_portugal, west_african_states | — |
| algeria | Algeria | 2 | 是 | — | france, morocco, saharan_states, tunisia | — |
| tunisia | Tunisia | 2 | 否 | — | algeria, libya | — |
| west_african_states | West African States | 2 | 否 | — | ivory_coast, morocco | — |
| saharan_states | Saharan States | 1 | 否 | — | algeria, nigeria | — |
| ivory_coast | Ivory Coast | 2 | 否 | — | nigeria, west_african_states | — |
| nigeria | Nigeria | 1 | 是 | — | cameroon, ivory_coast, saharan_states | — |
| cameroon | Cameroon | 1 | 否 | — | nigeria, zaire | — |
| zaire | Zaire | 1 | 是 | — | angola, cameroon, zimbabwe | — |
| angola | Angola | 1 | 是 | — | botswana, south_africa, zaire | — |
| south_africa | South Africa | 3 | 是 | — | angola, botswana | — |
| botswana | Botswana | 2 | 否 | — | angola, south_africa, zimbabwe | — |
| zimbabwe | Zimbabwe | 1 | 否 | — | botswana, southeast_african_states, zaire | — |
| southeast_african_states | Southeast African States | 1 | 否 | — | kenya, zimbabwe | — |
| kenya | Kenya | 2 | 否 | — | somalia, southeast_african_states | — |
| somalia | Somalia | 2 | 否 | — | ethiopia, kenya | — |
| ethiopia | Ethiopia | 1 | 否 | — | somalia, sudan | — |
| sudan | Sudan | 1 | 否 | — | egypt, ethiopia | — |

[返回数据索引](../map-card-data.md)
