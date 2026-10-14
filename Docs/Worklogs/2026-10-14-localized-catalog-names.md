# 中英文短名称工作记录

基线fcde880已同步。沿用户持续开发与Git授权，按brainstorming短设计、writing-plans计划与executing-plans单实现者执行；最终唯一独立审查。
Ruling: 项目自译198键不冒称官方译本 — 用户中文试玩需要 — 术语可在独立数据中调整。
Ruling: 同E盘新feature分支 — 延续工程隔离约定 — 仅ff合并不覆盖远程。

T1 RED1991/2007：16新增均因NotImplemented失败。现实现加载器与MapPresenter换名，加入T2视图切换/旧配置用例及stub，下一次同时验T1 GREEN/T2 RED。

T1 GREEN2007/2007原有及新增加载/地图用例；T2 RED2007/2009，仅两个视图切换stub预期失败。现接两包、UI按钮、视口恢复和独立构建，准备最终GREEN。

最终GREEN2009/2009；metadata135描述/目录、73数据、292符号、627数据依赖、274文件通过。冻结253份C#哈希，开始独立LocalizedPlaytest构建。

构建成功97617070字节，253源码冻结哈希/39载荷逐一比对一致。Player.log无Exception/Error/Assertion。窗口捕获先显示桌面，再重新选择返回window并激活一次失败failed to activate captured window；不作为视觉验收，已询问用户切前台，独立代码审查先继续。
