# 影响力事件结果明细

description：按公开收据定位国家/阵营/前后/差值与多头条结果显示，保留历史和隐藏信息边界。

先读 `Docs/Descriptors/influence-result-details.module.json`，再定位ReadPracticeFeedback、ReadEventFeedback、ReadHeadlineFeedback；共享格式器WithInfluenceChanges已接入，3744项完整原生GREEN已通过，Player及审查尚待完成。

flow：公开InfluenceChanged/事件收据 → 既有历史与错误优先级 → 按执行顺序显示国家、阵营、Before→After和差值 → 简短反馈/展开阅读页。输入仍来自Session的只读投影，不由表现层重新计算规则。多头条显示每个公开事件块，五年计划保留父子上下文，未完成选择不当作零变化完成。

不保存隐藏手牌或跨局历史，不新增规则数据副本。真实Player与测试分开记录于 `Docs/Testing/influence-result-details.validation.json`；迁移面板的已知反馈空间问题在本批独立处理。

代码定位：DemoView.InfluenceFeedback格式化；Actions/Events在错误与历史覆盖前组合；HeadlineFeedback按公开执行步骤展开；FiveYearPlan保留父子上下文。DemoController.HeadlineFeedback只缓存当前GameId/Version的公开事件。DemoView.Relocation复用ReadVisibleFeedback，把内嵌滚动反馈从5像素增至52像素；完整内容可展开阅读。

验证：influence-feedback-green-2，3744/3744，0跳过/不确定，XML SHA 144da1377e3f36f08ebe049ce110befaf73143758429a1984d961f7632d3c147。16项新增覆盖公开结果与上下文，两项旧战争混合头条预期更新并加强重发/身份断言。
