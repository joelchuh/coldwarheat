# 有限头条事件结算

description: 将已保留/揭示的头条接入既有事件服务，记录RED-GREEN、根责任与终局边界。

基础分支feat/headline-foundation已推送并核对远程278ca1bf9f1a6244993dd5767f01786e2e6806c4；main未变。本批从该点分出feat/headline-event-resolution；计划见同名Docs/Plans。用户询问Unity账号：仅知Hub安装位置，未获许可恢复证据，不猜账号。当前开始Task1。

Task1：18项新游标用例先全部失败于占位，完成后连同79个已有头条用例共97项通过。参数/目录/完整牌区校验、按当前根精确一次、跨回合身份与只读快照均覆盖。未改Practice/Host，尚不能执行头条事件。

Task2：31个行为测试先全部RED，完成后31GREEN且整批1982托管通过。测试首轮因NUnit链式Contain仅接受字符串编译失败，改为集合Superset/Member后才记录RED；实现初版CardId没有运算符重载，按现有IEquatable改用Equals后编译零警告错误。真实功能覆盖即时/七计分/持续OPS、双方出牌方与受益方、等待暂停、终局、后张最新地图、无OPS与普通计数、溢出原子性及第一普通行动。

根上下文存于Practice.Headline，所有同回合构造携带；下一回合唯一入口重置。共享效果入口按已授权卡源调用，Move显式辨别Hand/HeadlineReserved/Resolving；不迁回手牌或伪增CompletedActions。完整Host/选择继续/UI仍未接。

Task3：17个新增选择恢复用例先全部RED，GREEN17，最后全部可用托管1999项通过/0失败/0跳过；新增总66项。五种选择处理器、跨Owner两次等待、旧Decision/错方/错载荷、已发布前缀保持、本次选择后溢出丢草稿、后张读取选点、后张终局、外来executionID、普通事件保持原行为均已验证。CompleteChoice不会把原始效果草稿提前发布；旧快照重放仍由未来Host版本/幂等负责，不把纯函数当宿主。

测试通过的是选定.NET8/NUnit集合，不是Unity EditMode；160项原生测试仍排除，许可证未恢复。整批独立审查与分支推送待完成，现有播放器未变。
