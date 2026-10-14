# SDD ledger — plan: Docs/Plans/2026-10-14-quick-start-playtest.md

BASE: 8a18774934f863cc69974b2bc5c3d159fd9c45d0
Ruling: 自主持续优化授权覆盖快速试玩入口 — 用户希望尽快实际游玩反馈，已有工厂/部署可复用 — 成本：默认跳过手动部署，但明确标示且原入口保留。
Ruling: 数据保存示例而非规则默认 — 波兰6/西德4/意大利3只是合法fixture选择，所有数值仍经共享setup验证 — 成本：非最佳策略，试玩反馈可改fixture。
Ruling: 同E项目功能分支本人实现，一次最终review — 既有Unity和工作流环境可用 — 成本：不并发编辑Unity。
Preflight: InitialDeploymentFlow.Resolve按Side/DecisionId/Region/FreeAmount验证；StartPractice清理完成部署并不消耗RNG；Catalog链须最后追加快开，避免前置exact-one装配约束。窗口产品名需构建finally恢复，防止污染旧入口。T1 in progress.

T1/T2 RED: quick-start-red1648/1664,16 failures:15 loader NotImplementedException plus default-index expected2/actual1;0 skipped,XML44f595f30e8fe4cf249ab20a9c07d0e5b91038ebae8c8c95c2d8db9600db34b8. Data/flow and default selector implemented.

T1/T2 GREEN: quick-start-green1664/1664,0 skipped,exit0,XMLdb3c16ecba3580609e2459bf908a7b064c3ec304ca704eacc7c1e233276e2cdb. Bootstrap/Editor shared fixture wiring and independent BuildPlaytest added; UI capture next.

T2 render found China summary second line clipped at y746/h31. Adjusted only action-panel observation/hand/summary vertical slots to preserve full40px text. First build exit0 size97490186 and initial capture exit0; final source build/capture required. Ruling: use actual render for this reversible layout fix, no pixel-coordinate unit test — appearance must be inspected — cost: other window scales remain manual QA.

T1/T2 complete: final source quick-start-layout-final1664/1664,0 skipped,exit0,XML2bcb31d3c7b4d6257fffdc200c89c33f1843a5963cc3ed84fd0c5e913dd5c7e1. Final build exit0/FIRST_DEMO_BUILD_SUCCEEDED97490186;35 payloads byte-equal. Final map/hands actual1280x820 inspected; China row fully visible. Metadata120/120/71/212/597/224 passed.
Ruling: 新Playtest窗口捕获仍为壁纸且激活失败，不执行坐标输入 — 无可信当前画面 — 成本：真实鼠标出牌保留缺口；测试与渲染只证明各自范围。
T3 source frozen; once-only independent final review pending.
