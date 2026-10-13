# 可恢复随机状态实现记录

基线c3998b6，feature/deterministic-random；沿已授权持续开发，按同名计划inline执行，一次独立审阅。现有规范脚本负责真实Unity验证，没有新增正式工作脚本。

## Task 1

IRandomProvider、RandomSnapshot和Pcg32Random已实现。算法ID固定，状态/流/原始输出数可恢复，Fork独立；大int跨度与拒绝采样中途溢出不发布部分推进。

random-core-red：649项，原634通过，新15失败（NotImplementedException）；random-core-green：649/649通过。

Ruling: 修正测试中手算期待值 -454895641 为 -457095289。独立整数计算显示第三个raw输出3837872008，模2147483649再加int.MinValue得到后者；前两个零被拒绝。不是修改算法迎合测试，也未改变规则。

## Task 2

RandomSnapshotJson只编码字符串，三个ulong以16位小写hex精确保留；结构、版本、算法、重复键和错误值拒绝。random-json-red：674项，649通过/25失败；random-json-green：674/674，零跳过，exit0。最终XML SHA：11a995287539374c33626105dab9536b9fd14738603e178899f03ca801d4b720。

[报告](../Testing/deterministic-random.validation.json)保留四次运行及SHA。模块JSON/目录、Wiki/索引、架构、来源与Apache许可同步。没有改表现层或重建播放器。

## 边界与收尾

原始输出计数包含被拒绝的样本，初始化不计数；单值范围也消费一次。未知版本停止；本批没有GameState、牌库发牌、游戏命令事务或完整存档接入。后续由宿主在整体成功时采用Fork结果；单个可变实例只在宿主串行上下文使用。

674项验证通过；106份描述及33/67/6目录已校验。独立审阅确认Ready to merge，无Critical/Important；移除Wiki易过期的模块数量，属于文档纠正，无需重跑未变化源码。执行已授权Git提交推送，远程状态单独核验。

Final: Ruling: 审阅未判断GameState事务/去重、牌库发牌、完整保存/回放、并发、UI/IL2CPP/跨平台行为。它们未接入或未实际运行，本批不宣称完成；许可只作来源和文本工程核对。后续接入必须另行验证，若把本批测试误作完整对局证明会导致错误结论。
