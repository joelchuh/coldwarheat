# 头条试玩接线

Task1：17项新增行为RED全部失败，GREEN17通过，全部可用托管2127通过；编译0警告0错误。使用NextTurnSessionTests.Catalog完整链，因为较早QuickStartDemoTests.Catalog仅Scoring还无NextTurn；该任务所需源前提保持，不放宽生产校验。旧14对象/真实手牌/部署和所有能力引用测试通过，正常首张保密与两张结算后苏联第一行动已走实际Host。

UI/Bootstrap尚未接，新播放器未构建；Unity互动编辑器仍运行，等待用户关闭以执行原生测试。

Task2：最初17用例RED0/17；GREEN16/17发现CanChoose合并两种禁选原因。加入真实太空里程碑→次回合的等待保护测试，RED17/18仍复现无候选提示问题。新增只读WaitingForOpponentFirst后GREEN18/18，回归2145/2145。初次测试编译使用了错误fixture helper，已在行为RED前改正。用户确认保存关闭编辑器，零Unity进程后启动headline-presentation-native原生全套，结果待收集。

Task3：真实Unity RED2319项中2306通过/13失败（10入口新行为和3缺失载荷），显式旧入口用例在实现前已通过，作为兼容性保护。接线后GREEN2319/2319；额外检查发现China查询在头条仍有对象，界面禁止头条期间选择，最终原生回归正在执行。Unity工具会自行退出本次批处理进程，没有强制关闭用户编辑器。尚未构建或鼠标验收。

最终原生headline-ui-final：2319/2319，0失败/跳过，Unity退出0；按真实XML保存哈希和变更源码哈希，先前160项原生排除已全部补验。metadata142描述/74数据集通过。用户打开/保存产生的ProjectSettings/PackageManagerSettings.asset及Assets/unity_baocun.meta保持原样，不混入功能提交。

## 最终验收（2026-10-16）

以上按任务保留历史状态。最终默认观察问题已由真实视图RED2319/1→GREEN2320/0修复，全部原生测试含此前160项均已执行。最终构建基于8d8880d，42份共享载荷一致。实际鼠标走通苏方社会主义政府、美国遏制政策、英2/加1移除草稿及确认、苏方奥运会太空、重发幂等；版本/轨道/分数/手牌与提示一致。World→Tiles与中英名称切换保持局面。两张游戏内F12截图入库；完整矩阵未跑项目详见验证报告。

审查汇总：0 Critical，1 Important默认观察已修复，1 Minor过时元数据已按用户同步要求修正；未额外开启二次审查。所有取舍原文归档如下，历史not started和pending字样仅反映当时状态。

```text
# SDD ledger — plan: Docs/Plans/2026-10-15-headline-playtest.md
Base d0844da6b8637c0175cc3b6cdd334ec25644a848; branch feat/headline-playtest.
Pre-flight: T1 optional HeadlineEnabled and separate scenario feed T2 frame/controller and T3 UI labels; new scenario is appended only AFTER old quick start, preserving 14 legacy definitions instead of modifying all old factory copies. T2 controller only submits Host command, T3 uses that controller.
Ruling: Compared enabling old quick_start vs a separate scenario; choose new scenario with exact same deal/deployment, so old 14 entries retain compatibility and new default has honest headline semantics. Cost: one more temporary lesson while building the complete game.
Ruling: Continue previously user-authorized inline source development while native gate waits on editor close; no repeated design/implementation approval menu. No main/player claims without actual native/build/UI evidence.
Task1: not started.
Task 1: complete (75d57ae..20fff6ca4d5c3eb550f4a380a00749772c7eb759; RED0/17, GREEN17/17, regression2127/2127; metadata/diff passed). Test source uses full NextTurnSessionTests.Catalog as required, preserves all legacy entries.

Task 2: Ruling: CanChoose=false conflates space-policy waiting and unsupported hands; add public WaitingForOpponentFirst projection from existing state, not a new rule. Cost: additive view field for future consumers. The new space progression test passes before fix, while unsupported-hand regression still fails; fix must preserve both. User saved/closed Editor; zero Unity processes verified, native gate now executable.
Task 2: complete (base20fff6c; 18 focused,2145 managed passed; metadata142/74 and diff passed; actual Unity headline-presentation-native 2305/2305 passed). No UI/build claim.

Task 3 started base36022b1. Ruling: native licence restored and 2305/2305 verified; use real full Unity for Task3 RED/GREEN instead of supplemental tests, covering previously excluded native cases. Cost: longer runs, no weakening of gate.

Task3 inspection: CardOptions.China stays populated in headline collection although uses are rejected by Core; disable its UI selector until headline completes, avoiding a dead selection. This low-impact UI gate awaits visual verification; no mirror implementation test. Full native final rerun covers final source.

Task 3: source complete (base36022b1; real native RED2306/13, GREEN2319/0, final2319/0 after China UI gate; metadata142/74, diff gate to run before checkpoint). Artifact build/UI and whole-branch review remain.
Ruling: run isolated local build while read-only review checks fixed source; release/push acceptance still waits for review and UI. Independent work reduces waiting, at cost of rebuilding if a blocking source fix is needed.

Final review: 0 Critical,1 Important(default observer),1 Minor(stale retrieval status). Accept Important: allow both information remains user-approved but default privacy with explicit toggle is clearer. Fix actual view pipeline via ReadHeadlineFrame/TestObserverEnabled; real Unity TDD. Metadata correctness is already required by user synchronization workflow; update stale current-state text during final documentation rather than defer inaccurate facts. Build before fix succeeded but is not accepted release; rebuild after fix.

Final: fixed default observer — HeadlineViewPrivacyTests actual initialized view / identity switch / explicit opt-in RED2319/1→GREEN2320/0. No re-review; one blocking-fix pass complete.
Final: Ruling: metadata finding is factual synchronization required by user, corrected during planned documentation completion, not deferred polish; cost: three small status edits, no behavior.
Final: Ruling: reviewer set aside build/UI; independently verify rebuilt final source and actual interactions, keep incomplete cases explicit; cost if tools fail: limited acceptance, no invented pass.
Final: Ruling: future remote async behavior remains unverified, local Host synchronous; cost: further tests required when network implementation exists.
Final: Ruling: unsupported events, no candidates, extra end-turn/final scoring remain explicit stops; cost: this is a limited demo, not a full match.
Final: Ruling: network auth/save/formal multiplayer deferred; current single-window test ownership only; cost: cannot claim production multiplayer secrecy or persistence.
Final: Ruling: preserve user editor files and old players, only new output rebuilt; cost: manual user-file decision later if these need Git.

```

Git策略：延续用户常规提交/推送授权，保存feat/headline-playtest并核对远端，不强推、不混入用户编辑器文件；main保持原有已发布状态。
