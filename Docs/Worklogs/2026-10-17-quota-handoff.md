# 额度接近耗尽时的工程交接

本轮按用户授权持续开发，工程E:/TwilightStruggle，远程https://github.com/joelchuh/coldwarheat。用户两个未跟踪文件Assets/unity_baocun.meta和ProjectSettings/PackageManagerSettings.asset一直保留，不加入提交。

最新可运行Player：E:/TwilightStruggle/Builds/JapanPactPlaytest/ColdWarStruggleDemo.exe。需要保留整个文件夹；2026-10-17 11:54构建。已验收美日安保直接出牌及日本目标保护。后续三批基础模块尚未打包进此Player，不宣称马歇尔或北约已可玩。

已完成来源：
- feat/japan-pact-play，最终1e90a3aa；3952原生通过，74份构建载荷和有限真实点击；普通政变禁用提示另经实机验证。
- feat/marshall-foundation，最终e4628726；4006原生通过，无问题独立审查；仅影响力选择/短缺政策。
- feat/event-fact-foundation，最终d7c20ae2；4050原生通过，独立审查0Critical/0Important/1Minor并已远程核对。成功账本基础不等于事件已注册。
- feat/event-fact-state，源代码81aeb3a6；4076原生通过，最终审查和远程提交见本批worklog。仅状态绑定/复制与一次追加。

下一步先读 [渐进式JSON交接](../Design/marshall-runtime-handoff.json)，按purpose挑对应函数，再读 [原子接线草案](../Architecture/marshall-runtime-integration-draft.md)。不要重新实现这三项基础，也不要把普通弃牌/移出牌位置推定为事件发生历史。

已知Minor：JapanPact内部显式替换可清除永久实例（当前命令不触发）；EventFactLedger未注册畸形CardId可能抛异常（当前可信正式定义不会触发）。各自原始工作记录说明范围；接线扩大输入时需补回归并修正。

保留用户原裁定：马歇尔不足7合法国家选择全部、零合法国无变化完成仍算发生。新玩家/旧存档历史不能随意标为已知空。所有后续代码继续带模块JSON、Wiki/flow与测试证据，并分批提交同步。
