# Asia：国家与邻接

> description: Asia国家、稳定度、基础战场、子区与双向规则邻接的派生浏览表。
> 数据源：[world.json](../../../Data/Maps/deluxe-2015/world.json)，content_version=0.1.0；仅在源数据变更后重新生成本表，不单独修改数值。
> 来源：M-DELUXE该地区国家框与连接线；图形布局和现代国界不产生规则边。

| CountryId（country.省略） | 名称 | 稳定度 | 战场 | 子区 | 普通国家邻接（country.省略） | 超级大国邻接 |
|---|---|---:|---|---|---|---|
| afghanistan | Afghanistan | 2 | 否 | — | iran, pakistan | superpower.ussr |
| pakistan | Pakistan | 2 | 是 | — | afghanistan, india, iran | — |
| india | India | 3 | 是 | — | burma, pakistan | — |
| burma | Burma | 2 | 否 | southeast_asia | india, laos_cambodia | — |
| laos_cambodia | Laos/Cambodia | 1 | 否 | southeast_asia | burma, thailand, vietnam | — |
| thailand | Thailand | 2 | 是 | southeast_asia | laos_cambodia, malaysia, vietnam | — |
| vietnam | Vietnam | 1 | 否 | southeast_asia | laos_cambodia, thailand | — |
| malaysia | Malaysia | 2 | 否 | southeast_asia | australia, indonesia, thailand | — |
| indonesia | Indonesia | 1 | 否 | southeast_asia | malaysia, philippines | — |
| philippines | Philippines | 2 | 否 | southeast_asia | indonesia, japan | — |
| australia | Australia | 4 | 否 | — | malaysia | — |
| taiwan | Taiwan | 3 | 否 | — | japan, south_korea | — |
| japan | Japan | 4 | 是 | — | philippines, south_korea, taiwan | superpower.us |
| south_korea | South Korea | 3 | 是 | — | japan, north_korea, taiwan | — |
| north_korea | North Korea | 3 | 是 | — | south_korea | superpower.ussr |

[返回数据索引](../map-card-data.md)
