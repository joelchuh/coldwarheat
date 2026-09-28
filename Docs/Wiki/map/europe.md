# Europe：国家与邻接

> description: Europe国家、稳定度、基础战场、子区与双向规则邻接的派生浏览表。
> 数据源：[world.json](../../../Data/Maps/deluxe-2015/world.json)，content_version=0.1.0；仅在源数据变更后重新生成本表，不单独修改数值。
> 来源：M-DELUXE该地区国家框与连接线；图形布局和现代国界不产生规则边。

| CountryId（country.省略） | 名称 | 稳定度 | 战场 | 子区 | 普通国家邻接（country.省略） | 超级大国邻接 |
|---|---|---:|---|---|---|---|
| canada | Canada | 4 | 否 | western_europe | uk | superpower.us |
| uk | United Kingdom | 5 | 否 | western_europe | benelux, canada, france, norway | — |
| norway | Norway | 4 | 否 | western_europe | sweden, uk | — |
| sweden | Sweden | 4 | 否 | western_europe | denmark, finland, norway | — |
| finland | Finland | 4 | 否 | eastern_europe, western_europe | sweden | superpower.ussr |
| denmark | Denmark | 3 | 否 | western_europe | sweden, west_germany | — |
| benelux | Benelux | 3 | 否 | western_europe | uk, west_germany | — |
| france | France | 3 | 是 | western_europe | algeria, italy, spain_portugal, uk, west_germany | — |
| spain_portugal | Spain/Portugal | 2 | 否 | western_europe | france, italy, morocco | — |
| italy | Italy | 2 | 是 | western_europe | austria, france, greece, spain_portugal, yugoslavia | — |
| greece | Greece | 2 | 否 | western_europe | bulgaria, italy, turkey, yugoslavia | — |
| turkey | Turkey | 2 | 否 | western_europe | bulgaria, greece, romania, syria | — |
| west_germany | West Germany | 4 | 是 | western_europe | austria, benelux, denmark, east_germany, france | — |
| east_germany | East Germany | 3 | 是 | eastern_europe | austria, czechoslovakia, poland, west_germany | — |
| poland | Poland | 3 | 是 | eastern_europe | czechoslovakia, east_germany | superpower.ussr |
| czechoslovakia | Czechoslovakia | 3 | 否 | eastern_europe | east_germany, hungary, poland | — |
| austria | Austria | 4 | 否 | eastern_europe, western_europe | east_germany, hungary, italy, west_germany | — |
| hungary | Hungary | 3 | 否 | eastern_europe | austria, czechoslovakia, romania, yugoslavia | — |
| yugoslavia | Yugoslavia | 3 | 否 | eastern_europe | greece, hungary, italy, romania | — |
| romania | Romania | 3 | 否 | eastern_europe | hungary, turkey, yugoslavia | superpower.ussr |
| bulgaria | Bulgaria | 3 | 否 | eastern_europe | greece, turkey | — |

[返回数据索引](../map-card-data.md)
