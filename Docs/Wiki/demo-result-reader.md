# Demo 结果阅读页

> description: 在独立阅读页查看当前公开行动/事件反馈，关闭回到原局面；3087原生及Windows实际展开/关闭/Escape验收通过。

先读[模块JSON](../Descriptors/demo-result-reader.module.json)，再定位DemoView.Feedback.cs的TryOpenFeedback、ReadExpandedFeedback和绘制方法。通过现有公开结果组合显示明细，不接受掷骰或规则参数，不持有权威GameState。

打开只保存显示上下文，关闭不修改对局；ViewGeneration、GameId或Version变化后旧页失效。地图渲染仍由IMapView承担，阅读页不依赖具体地图风格。不是持久历史日志或回放。

测试入口FeedbackReaderTests；实际证据见[验证报告](../Testing/demo-result-reader.validation.json)，工作过程见[工作记录](../Worklogs/2026-10-17-demo-result-reader.md)。

绿色原生：feedback-reader-green-2，3087/3087、0跳过、exit0，XML SHA d56b042db4b694e65b1ae5905b4beef3f930dcb4ddcde2186a621ae31387c1a9。阅读页优先于常规操作绘制；关闭/Escape仅改变表现状态。

可运行成品：E:/TwilightStruggle/Builds/ResultReaderPlaytest/ColdWarStruggleDemo.exe，源942f4b2；58载荷字节一致。真实界面已验证朝鲜战争完整明细以及两种关闭方式不改版本/奖励。长内容滚动与混合头条未逐个鼠标复现，边界见报告。

2026-10-17新增[影响力事件结果明细](influence-result-details.md)：读取公开Before/After而非当前棋盘，按同批执行顺序显示多个头条；历史/失败提示优先。迁移面板复用ReadVisibleFeedback保留政变摘要和拒绝提示；独立构建及实际验收见该批报告。
