# SDD ledger — plan: Docs/Plans/2026-10-14-playable-coup-actions.md

BASE: 4ce16f866bc6b30ae3bd6cf9e74d632c716249c4

Ruling: 继续按已批准架构和当前持续开发指令，在独立功能分支单人实现；原PlayableDemo保留，新的RiskDemo输出避免锁定文件。
Ruling: 用RiskState保留风险状态与政变收据，国家/RNG仍仅由GameState持有；BindCurrent创建暂时计算输入，不能清空收据。使用内部CoupAttemptState构造器是同Core程序集的可信适配，不作公开恢复API。成本：未来跨回合/存档必须另行设计。
Ruling: 本方/中立普通手牌；现有明确拒绝的未实现事件组合仍拒绝，不能因多一个用途绕过它们。
Pre-flight: T1将Current GameState投影成旧CoupAttemptRules输入；T2只收牌ID+目标意图，不收OPS/RNG/身份；T3使用合法目标结果驱动IMapView。
Task1 in progress.

T1 RED: playable-coup-core-red 1610 total, 1600 passed, 10 failed on NotImplementedException, 0 skipped; XML 498978e26515058dcdd9b033c758f883bcf38495a0414dbadca5c01e92b2d011. Risk preservation test already passed. Test fixture correction before GREEN: Israel is a battleground in approved world.json; use Australia for non-battleground assertion. No rule change.

T1 GREEN: playable-coup-core-green 1610/1610, 0 skipped, XML 2d0103361585d82f83f2dc077e788ea15686d8482f50b9f0439cb314d8542b0b. T2 begins: additive API surface and failing identity/idempotency/projection/controller tests.

T2 RED: playable-coup-session-red 1610/1616 passed, 6 expected failures (unimplemented target/mode query or ignored Target), XML 285ee6b33b58bbb39b60eca23044f6dd40d349fb2a168d0644b045aabf62a6eb.

T2 GREEN: playable-coup-session-green 1616/1616, 0 skipped, XML d72341186c725210306e9baaa3312d9f0c29e1bedd4dfab0c65ff6d9c54c2d10. T3 data wiring test surface added; core version/sequence overflow regressions included.

T3 RED: playable-coup-catalog-red 1619/1621, 2 expected unimplemented-loader failures; XML 830d73a320074cdefb4e3b690b013feb51132f9640fd646101b98e54d12026bc. Both overflow cases passed. Shared loaders and wide map/risk controls now wired; legacy deployment panel retained.

T3 GREEN: playable-coup-catalog-green 1621/1621, 0 skipped, exit0, XML 4c9992b3cbe9b6a78e99409c3e25285eb28890f460a89ad1407cf1ce3be4c836.
Build: BuildRisk exit0, FIRST_DEMO_BUILD_SUCCEEDED 97448097, 32 payloads byte-equal (30 JSON + 2 notices). Capture exit0; Docs/Images/risk-demo-deployment.png visually inspected initial layout. Metadata 117/117 descriptors,70 datasets,201 symbols,591 data references,215 files; diff whitespace clean.
Ruling: 当前窗口工具读取桌面壁纸，激活RiskDemo错误 failed to activate captured window；刷新返回窗口后仅重试一次仍失败，停止输入。保留旧问答等待用户前台协助，不重复打扰。成本：政变点击和行动大地图尚未人工验收。
Ruling: 核战立即终止后保留源卡在Resolving、根槽不完成 — 遵循先前中断/胜负顺序，不为展示清理终局状态 — 成本：后续存档/回放应明确这种合法终局牌区。
T1/T2/T3 complete; T4 independent final review pending. No new permanent workflow script or business constants created.

Final review: review_playable_coup_actions on 4ce16f8..4fcd22b; Critical0 Important0 Minor1; Ready to merge Yes. Reviewer independently matched1621 XML,182 C# source hashes,32 payloads. No fix pass required.
Final: minor (deferred): 尚无政变→实际投放影响力→政变完整序列回归；现有实现绑定最新国家，审查未发现错误，后续补强。
Final: Ruling: 对手事件OPS顺序 — 继续明确拒绝；不把未实现事件绕过去 — 成本：试玩可用牌仍有限。
Final: Ruling: 中国牌用途 — 继续单独持有并拒绝普通用途 — 成本：缺少中国牌策略体验。
Final: Ruling: 计分及完整积分系统 — 本批不阻塞；下一批按计划接入计分 — 成本：当前仍不能完整打一局。
Final: Ruling: 条约/危机/活动效果 — 保持无活动效果且无启用入口 — 成本：未来必须接干预流水线再放行。
Final: Ruling: 头条/奖励/军事审核/下一回合 — 保持明确单回合普通槽练习 — 成本：不具备正式回合结束流程。
Final: Ruling: 所有出牌路线走满槽 — 不保证；文档与选牌禁用明确解释 — 成本：可能须重置重新选择。
Final: Ruling: 跨回合存档/恢复/回放 — 无入口；终局Resolving状态另行设计 — 成本：未来持久化要兼容此终局边界。
Final: Ruling: 扩大地图与鼠标验收 — 无运行证据，不宣称通过 — 成本：可能存在尚未发现的布局/命中问题，待用户前台试玩。
T4 complete: review evidence recorded; user-authorized ff merge/push next. No post-test C# changes.
