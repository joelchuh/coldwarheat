# 元数据只读校验器实现记录

基线b7c963e737b79a284fc6ea6626d5d1c7634d387f，已与GitHub核验一致；feature/metadata-validator。
六个真实复用批次及范围见计划。Pre-flight: 配置指向三catalog及现有workflow-policy必备字段；Data catalog业务status与descriptor可读status不同，明确不作相等断言。
Ruling: C# symbol只作词法定位，不宣称编译认证；路径/文件需调用期间稳定，不作为并发攻击沙箱。
Task1：已建共享配置、正式CLI桩和真实文件系统/子进程测试，等待RED；所有临时fixture保留。

首次默认沙箱运行因Temp文件权限失败，没有有效报告，不计行为RED。red-02为38失败；其中36条返回NOT_IMPLEMENTED，2个成功路径断言失败无详细消息。最初汇总断言错误地要求38条都含相同消息，阻止了实现写入；因此green命名的那次仍运行桩，38失败，并非GREEN。修正记录断言及写入后，green-02真实38/38通过，fixture C:/Users/JC/AppData/Local/Temp/coldwarheat-metadata-tests-rw0h9rif。

在上述自有fixture内额外创建真实NTFS根/入口Junction，两个入口均LINK_REJECTED；产物保留，未改原fixture。正式脚本/测试/配置均有描述，登记数变为111（34模块、69数据、8脚本）；等待实际仓库运行及独立审阅。

真实仓库首次校验在world-layout.data.json失败：两份早期视觉描述沿用kind=dataset且无dataset_id，其余数据描述均使用data/dataset_id。Ruling: 将world-layout/world-land描述对齐通用data形状，保留原id并把同一稳定值填入dataset_id；不改实际地图/陆地JSON或任何调用引用。这样统一后续检索，代价为仅描述格式变更。Wiki索引条目已插入既有表格。

真实仓库校验111描述、69数据、147个C#词法入口、575个data依赖全部通过。前后261个输入文件总摘要一致，Git状态一致；报告中的源码/配置SHA固定。未改游戏源码，不重复运行Unity；等待整批独立审阅。

2026-10-14独立审阅：1项Important（Windows扩展名大小写/尾随点空格别名漏检）和1项Minor（Wiki固定脚本数过时）。作者加入10个真实CLI回归，review-red为48中10失败，确认漏报及非规范路径错误码；统一扩展名并拒绝所有相对子路径段尾随点/空格后，review-green为48/48。合法大写CS和JSON也保留支持。移除Wiki固定脚本数；一次修复轮完成，未追加审阅。真实链接两项仍为此前独立证据，路径链接实现未改。
