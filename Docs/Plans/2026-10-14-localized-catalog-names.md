# 中文短名称实现计划

使用executing-plans单实现者、TDD、最终唯一最强审查。Spec: Docs/Architecture/localized-catalog-names.md。继续单E盘隔离分支与现有用户授权。
T1 数据/加载：Data/Localization/zh-CN.catalog-names.json（草稿198键已静态比对）；新增Infrastructure/DataLoading/CatalogNamesLoader.cs及LocalizedNamesTests.cs。RED完整包/坏元数据/重复/缺项/类型/只读/稳定ID/真实手牌投影；GREEN显式校验与字典副本。
T2 展示：MapPresenter.ReplaceNames及回归；DemoView.Localization.cs可选Initialize英文包，独立SwitchNameLanguage换表现并保存WorldViewport；Bootstrap读中文；Build预检/拷贝新包与BuildLocalizedPlaytest；真实渲染/按钮/选牌及哈希验证。
T3 元数据：Data descriptor+目录，module descriptor+目录，Wiki/README/AGENTS/工作日志/验证JSON；单次独立审查后必要一次TDD修复，常规提交ff/main push核对。
Review Focus：中文只影响名字，不参与规则/actor/RNG；地区或卡名静态知识不泄露私有手牌；替换names失败不得部分更新/留下可用陈旧点击；MapPresenter上下文新建而game/selectedcard/draft/版本保持；世界地图Center/Zoom恢复避免语言切换跳回全图；真实中文长标题换行/按钮不遮挡；旧Initialize无alternateNames仍可用。

执行结论：T1/T2代码及自动测试/构建完成，T3描述与唯一审查完成；视觉/按钮验收因窗口捕获失败保留未完成，详见验证报告。Minor登记后不在本批扩展修复范围。

补测：地图名称按钮中文→EN→中文实测通过，1.4倍视口/伊朗/牌局版本保持；手牌标题及选牌保持仍因激活失败待验收。详见报告ui.passed/ui.pending。
