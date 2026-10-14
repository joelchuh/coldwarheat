# 对手事件 OPS 模式工作记录

基线457b4d327897d4f9d659431815f8c8092773bd8d已推送并核验远程；分支feat/opponent-ops-modes。用户持续开发/普通Git授权有效，同一实现者沿用已批准方向，不重复批准。计划见Docs/Plans/2026-10-14-opponent-ops-modes.md。

Pre-flight: 序列可空Mode与新增ChoosingOperations必须同时接公开选项/Controller；旧Influence专用API保留默认Mode；Coup+后事件结果不能被覆盖；调整Finish不得直接Next。选择期间预算0，不能绕过模式选择投放；政变目标预览只属客户端，可改模式。
Ruling: 沿用现有单一E盘Unity工作区的功能分支 — 既有chat managed worktree不指向本工程，已有分支隔离且单实现者 — 不同时运行修改源码的另一实现者。
Ruling: 不新增ChoosingCoupTarget权威状态 — 地图目标是本地意图，确认才提交；零目标可重新选模式 — Controller必须按当前ActiveCard/版本查询。
T1：新增行为测试与最小编译脚手架后运行RED。未启用未实现事件。

T1 RED1843/1858，15新增行为失败：2个未实现SelectMode与13个新命令路由缺失，原1843全通过。实现可空模式/三种OPS接续、实际政变合并与旧路由门禁后运行GREEN。
T1 GREEN1858/1858，0失败/跳过。T2添加Host/Controller与公开待选OPS契约、复合收据、版本/身份/重放和失败不发布测试，开始RED。
T2 RED1860/1864：通用用途/Controller入口和公开Mode尚未接线，4项预期失败；其余复合收据与失败原子性已通过。接公开选项、Coup目标查询、Controller身份缓存与UI顺序/模式面板，修正前批结束按钮措辞，开始GREEN。

T2 GREEN1864/1864。补齐计划中的零合法目标仍可改模式、调整已接受前缀收尾溢出、通用入口事件先核战/VP胜利、政变后VP胜利和未知NATO拒绝，生产行为不变，运行最终回归。

最终1872/1872，0失败/跳过；元数据128描述/128索引/72数据集/256符号/612引用/252文件通过。源码冻结，开始独立OpponentOpsPlaytest构建。

独立构建exit0，97570408 bytes，229份源码冻结/38份载荷哈希相符。重启后native窗口激活恢复，已实际点击两条路线：Duck事件先→模式→南非政变→重发（不二次骰/军事）；切美国→Nasser OPS先→调整→中东Syria双骰移除1苏影响力→结束→Egypt苏+2/星号移出/根切苏联2轮。地图/手牌/用途/结果真实渲染与鼠标已验；对手受益方等待选择和同次Coup+Event复合结果布局仍仅自动测试/静态审查。F12四张游戏帧证据已存Docs/Images，无桌面截图入库。开始全分支独立审查。
