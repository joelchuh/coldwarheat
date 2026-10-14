# 中英文短名称工作记录

基线fcde880已同步。沿用户持续开发与Git授权，按brainstorming短设计、writing-plans计划与executing-plans单实现者执行；最终唯一独立审查。
Ruling: 项目自译198键不冒称官方译本 — 用户中文试玩需要 — 术语可在独立数据中调整。
Ruling: 同E盘新feature分支 — 延续工程隔离约定 — 仅ff合并不覆盖远程。

T1 RED1991/2007：16新增均因NotImplemented失败。现实现加载器与MapPresenter换名，加入T2视图切换/旧配置用例及stub，下一次同时验T1 GREEN/T2 RED。

T1 GREEN2007/2007原有及新增加载/地图用例；T2 RED2007/2009，仅两个视图切换stub预期失败。现接两包、UI按钮、视口恢复和独立构建，准备最终GREEN。

最终GREEN2009/2009；metadata135描述/目录、73数据、292符号、627数据依赖、274文件通过。冻结253份C#哈希，开始独立LocalizedPlaytest构建。

构建成功97617070字节，253源码冻结哈希/39载荷逐一比对一致。Player.log无Exception/Error/Assertion。窗口捕获先显示桌面，再重新选择返回window并激活一次失败failed to activate captured window；不作为视觉验收，已询问用户切前台，独立代码审查先继续。

维护构建模块description与目录摘要：旧28载荷/PlayableDemo说明更新为当前39载荷和两个新固定构建入口，指向实际最新证据；未把旧构建覆盖为新版本。

最终唯一独立审查review_localized_names完成，精确范围fcde880..9d7c623，0 Critical / 0 Important / 1 Minor。审查者再次核对253源码与39载荷及exe哈希一致，未重跑测试。Git读取权限受限，使用父代理导出的精确patch。审查后仅补文档，不改源码。

Final: Ruling: 延后修复首次地图绘制前切名丢失pending focus — 仅 --demo-focus=selected --demo-hands=open 特殊启动组合，默认试玩和牌局正确 — 此组合返回地图显示全图；下一批保留pending focus并加WorldMap回归。
Final: Ruling: 实际按钮、中文字体、长标题适配与真实视口保持仍未验收 — 窗口恢复读取仍是桌面，不能可靠点击 — 中文版本可人工试玩但不能宣称视觉全部通过。
Final: Ruling: 维持项目自译短名称，不作官方术语或完整翻译保证 — Spec明确范围 — 后续术语校对和规则正文单独开展。
Final: Ruling: 头条、未接事件、特殊回合末、最终计分、存档、联网与AI保持既有边界 — 本批只改变名称显示 — 尚不能完整打一局。

构建description修正后metadata复核135描述/目录、73数据、294符号、628依赖、274文件通过。补中文试玩指引与经过ContinuousPlaytest实测的12行动路线，并明确该路线不等同于LocalizedPlaytest名称按钮已验收。常规Git授权仍有效，准备docs提交及ff/main推送。

2026-10-14用户回复已切前台后补测：LocalizedPlaytest真实中文地图可读。放大1.0→1.4后中文→EN→中文，伊朗/Iran切换正确；视口、选中国家、美1苏0、VP0/DEFCON5/太空0、回合1苏联行动1/6、手牌8/8、版本4序号22均保持。两张游戏F12截图已保存Docs/Images并记录SHA256。

打开手牌时再次failed to activate captured window，重新选择返回窗口并激活一次失败，停止输入并释放用户鼠标键盘。手牌标题/长名与选牌保持的真实UI仍待验收；此前“全部UI待验”历史结论更新为“地图切换通过，手牌待验”。Player.log没有Exception/Error/Assertion。源码未变，不重复运行2009项自动测试或构建。额度已到10%，本次仅保存证据及同步，不继续开发。
