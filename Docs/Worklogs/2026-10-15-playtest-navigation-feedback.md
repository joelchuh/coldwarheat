# 地图导航与回合末反馈修复

description: 追踪中文切名丢首次聚焦、回合末禁用无原因两项已知问题；不改规则裁定。

基线0fc00330aee9f6a6902ccd688d521d3e35983a42。用户明确授权用现有credits继续开发至余额耗尽，取代此前10%停止条件；不购买或启用自动充值。开工余额62326.313175，周额度已用完。

沿单E盘分支fix/playtest-navigation-feedback；按project-knowledge、brainstorming bounded设计、systematic-debugging和TDD执行。持续开发/日常Git授权已明确，不反复要求批准修复。

根因1：WorldMapView.Initialize清掉pendingCountry/pendingRegion，旧名称切换仅还原已经应用的Center/Zoom；准备增加只读显示快照与恢复方法，涵盖排队请求并保留一次消费。
根因2：PlayerViewSnapshot把CanAssessTurnEnd/CanBeginNextTurn的RuleError压成bool，UI丢失拒绝原因；准备保留公开原因码，Controller给阶段对应提示，仍由Core决定资格。

RED：新增真实DemoView首帧前聚焦/地区聚焦与已应用视口用例；两身份额外行动、普通审核、特殊弃牌和最终计分提示用例。新API仅NotImplemented桩，未实现。

测试执行被Unity授权阻断：playtest-feedback-red返回TEST_REPORT_MISSING，编辑器退出198，日志Access token is unavailable / Found 0 entitlement groups。没有测试执行，不能计为RED。已异步请用户恢复Unity Hub许可证，未触碰账号认证。等待期间完成头条运行时接线审计及planned模块摘要，尚未写头条实现。

后续发现安装内置.NET SDK 8.0.318，可做独立托管补充检查，无下载包、无许可证修改。临时runner使用已有NUnit；只在临时测试副本将三个dataPath数据入口指向实际项目。五个反馈用例RED为NotImplemented异常，修复后5/5 GREEN；正式测试文件收敛为TurnEndFeedbackTests，最终选中1854项托管回归全部通过，所有生产源码在此.NET环境编译零警告/错误。它不验证Unity原生调用、asmdef隔离、播放器或鼠标。

地图适配器补充探测在Unity原生ECall处失败，不能用此环境验证原生生命周期。三条地图回归草稿已移到chat临时work/managed-check/MapNavigationRegressionTests.pending.cs.txt；地图生产源码未改，待许可证恢复继续。原三处NotImplemented桩已全部实现/移除，不把已知失败测试合入主线。

原因提示保留Core RuleError，经公开投影与Controller文案到按钮；未变更规则资格或任何牌区。特殊能力、最终计分仍未实现。详细源hash、XML证据、排除范围见Docs/Testing/turn-end-feedback.validation.json。临时补充runner是本次故障的临时检查，不因多次重试就升级成正式工作脚本。

头条接线设计/JSON索引已完成；metadata通过136描述/136目录项、73数据集，非编译证明。用户现有credits仍正常扣用；不购买、不开自动充值、不消耗重置卡。

独立审查review_turn_end_feedback：0 Critical / 0 Important / 0 Minor；逐项接受未审范围：地图修复保留待办，完整头条/特殊能力/最终计分另批实现，Unity原生/构建/鼠标证据仍缺失。LF归一后再次编译零警告/错误，1854托管用例再次全过。仅检查点推送开发分支，main保留上次Unity验收基线；不将缺少许可证当作可以跳过Unity验收的依据。
