# 世界地图占位资源来源

2026-10-13：底图只表示陆地轮廓，不含国家疆界，不参与规则邻接与控制权判断。

- 数据：Natural Earth 1:110m land。官方介绍：[Land](https://www.naturalearthdata.com/downloads/110m-physical-vectors/110m-land/)。
- 许可：官方[Terms of Use](https://www.naturalearthdata.com/about/terms-of-use/)声明其矢量和栅格地图数据属于公共领域，可修改并用于电子分发。
- 实际来源：官网 changelog 链接指向维护者仓库；从 [ne_110m_land.shp](https://github.com/nvkelso/natural-earth-vector/blob/master/110m_physical/ne_110m_land.shp)读取。网页下载链接异常，因此本次采用该维护仓库的文件，不把网页所标的 4.0.0 当成本次下载版本。
- Git blob SHA：`69ce127bf49cc005144f63846b102e6b91358e8e`；文件 89,504 字节。
- 下载 SHA-256：`8689e6932b8e370e2ca4587cf3ba21e460b1235db37b6ed3c172c35b4a6088de`。
- 转换：读取 Polygon SHP 的每条记录、parts 和 XY 顶点；x=(经度+180)/360，y=(纬度+90)/180；夹紧到[0,1]并保留七位小数。保留全部128个闭合环、5,143个顶点，写入 `Data/Presentation/world-land.json`。经纬投影宽高比2:1，原点左下。
- 渲染：按扫描线奇偶填充闭合环，生成2048×1024运行时占位纹理。叠加经纬网；不生成现代行政区边界。
- 临时 SHP 位于忽略的 TestResults，未纳入 Git。一次性转换命令未升级为正式工作脚本，本次尚不满足重复流程脚本化依据。

`world-layout.json` 中86个标记是本项目自制的近似显示位置，按既有游戏国家/地区 ID 命名。西/东德、合并地区等仅为玩法节点；位置不是首都数据、领土声明或历史疆界事实。正式风格确认后可独立替换布局。
