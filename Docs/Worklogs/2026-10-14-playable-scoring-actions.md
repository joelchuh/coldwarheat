# SDD ledger — plan: Docs/Plans/2026-10-14-playable-scoring-actions.md

BASE: 6c641c20c85e4cc61a38e0e3e7c0c7f7c648c04e

Ruling: 用户持续开发授权和既定“接地区计分”队列已覆盖本次接线，不重问编码许可；继续同E项目独立分支单人实现。成本：若用户另有玩法优先级，可在后续反馈调整。
Ruling: 计分牌固定使用CardDefinition.ScoringRegion，不新增CardId到地区映射；使用现有无干预算法/胜负检查点。成本：台湾/穿梭等活动修正仍不能启用。
Ruling: 引入计分后太空也可能达到普通胜利线，必须同批接完整VP批次判胜，避免新增可达终局漏洞。终局统一保留源牌Resolving且不推进根槽，沿用核战中断策略。成本：保存/回放仍待设计。
Ruling: 当前实时预览仅依赖公开地图/定义和版本，与持牌无关；所有地区均可查询。成本：后续活动效果需要加入公开干预快照。
Preflight: CardDefinition已经加载ScoringRegion/RemoveAfterResolvedEvent；RegionScoringService和VictoryService已有对应证据；Risk只存历史，无第二份地图/RNG。UI核战文本必须改为按Checkpoint区分。T1 in progress.

T1 RED: playable-scoring-core-red 1621/1637,16 expected failures (15 unimplemented scoring + Space terminal false),0 skipped,XML c6b7d02e0931db94fc20010217f2c69449790ab554522689a06f36caed4e8046. Full seven-card fixture explicitly enters all eras only for service tests.

T1 GREEN: playable-scoring-core-green 1637/1637,0 skipped, XML013937da4212db77dd88dffa4efb2a7bf9843c0e5687ccca3336cf95dceda585. T2 failing public projection, privacy-independent preview, controller and terminal-reason tests added.

T2 RED: playable-scoring-session-red 1637/1644,7 expected failures (stubs/default result fields),0 skipped,XML90066767bedf0c3cf8a47e7c0535a39a6810b114e8877fdb5d1cc6d860b5f418. Implemented public-only preview and score/reason projection.

T2 GREEN: playable-scoring-session-green 1644/1644,0 skipped,XML9c26edc967253120000691d76318e222b39cc4bf6d9268b1b9d6b0c3bb10adb4. T3 catalog tests added; missing risk/different map guard added in T1 is covered without exposing a new fixture API.

T3 RED: playable-scoring-catalog-red 1645/1648,3 loader-stub failures,0 skipped,XML20b1b3542d14bf000e51a73b22aec61f402eed68772d8ab4de288f984a7edb4c. Same-map definition guard passed. Catalog/UI/shared payloads implemented; display labels distinguish nuclear/scoring/VP terminals and do not falsely say terminal-space card was discarded.

T3 GREEN: playable-scoring-catalog-green1648/1648,0 skipped,exit0,XMLaa7e125d83d4da394815590fc099c751f0c5c922343eba65793acbd694c14b08. BuildScoring exit0, FIRST_DEMO_BUILD_SUCCEEDED97466815;34 byte-equal payloads (32 JSON+2 notices). Actual hand capture exit0, visually inspected1280x820, screenshot in Docs/Images/scoring-demo-hands.png.
Ruling: 用户已回复“已切到前台”，重新选择返回的RiskDemo窗口却短暂显示旧PlayableDemo画面；激活Risk/Playable继续failed to activate captured window，之后捕获是壁纸。停止输入，未执行任何点击。成本：计分按钮/地区预览/行动地图鼠标验收保留缺口；现有截图仅证明初始手牌渲染。
T1/T2/T3 complete. T4 metadata and independent final review pending.

Final: fresh whole-branch review dd87adc, 0 Critical/Important, 2 Minor deferred. Reviewer independently matched1648 tests/XML/190 source hashes/34 payloads. Metadata118 descriptors/118 catalog/70 datasets/209 symbols/594 data refs/221 files.
Final: Ruling: SEA的无手牌UI预览入口延后到预览界面改进 — 早期Demo无该牌且查询API正确 — 成本：当前仅服务/选牌面板可访问SEA。
Final: Ruling: 终局SEA和政变后计分历史的专项测试记为后续集成覆盖债 — 已检查实现正确，非阻塞 — 成本：两条组合暂未专测。
Final: Ruling: 鼠标/行动地图验收保持未验证 — 激活工具失败且手牌截图不能替代 — 成本：交互问题可能需试玩反馈发现。
Final: Ruling: 台湾/穿梭等活动修正不接本练习 — 当前命令不能引入效果且显式无活动状态 — 成本：不是完整计分规则。
Final: Ruling: 一般事件、对手事件OPS、中国牌、头条和回合末按后续计划接线 — 超出当前计分批次 — 成本：暂不能完整对局。
Final: Ruling: 七地区服务覆盖使用明示all-era测试fixture — 实际默认Early牌组不应伪加入中晚期牌 — 成本：默认不能试玩全部七张。
Final: Ruling: 不保证任意手牌路线均可完成十二槽 — 未实现事件限制继承自前批 — 成本：可能提前无牌可用。
Final: Ruling: 存档/回放及跨进程收据持久化延后 — 本批仅本地宿主事务 — 成本：退出无法继续。
Final: Ruling: 不支持任意自定义工厂复用同GameId/version但换状态 — 生产工厂每局生成新ID — 成本：测试扩展须保持这一契约。
Final: Ruling: 外部任意超长GameState标识不增加本批门禁 — 当前为受信任内部状态 — 成本：开放导入前需独立校验。
T1/T2/T3/T4 complete; no fix pass required. Routine ff merge/push follows; remote SHA is checked separately.
