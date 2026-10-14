# SDD ledger — plan: Docs/Plans/2026-10-14-playable-influence-events.md

BASE: 687912e12a7e69e52c3edd232d37e327b9c26f77
Ruling: 接已存在的八个纯影响力效果，不重写规则/数值 — 用户要求可玩闭环，原独立练习已验证 — 成本：其余事件仍禁用；不能代表102个效果已实现。
Ruling: 允许直接发动对手已实现事件，但不同时获OPS — 复用印刷阵营/既有Beneficiary与Phasing区分 — 成本：OPS与对手事件顺序需后续单独接线。
Ruling: 不在通用InfluenceEventFlow中耦合练习状态 — 由ActionPracticeRules.CompleteEventAction与Host组合最终事务 — 成本：新的消费者须调用完成包装，不可只发布中间效果结果。
Ruling: 内部纯步骤多次递增版本，Host只发布最终状态 — DecisionSourceVersion来自实际待选快照且根槽独立计数 — 成本：版本数字不能被UI当回合计数。
Preflight: GameState.WithPractice保留InfluenceEvent且清部署，Complete必须显式清事件；Resolve包装失败必须丢弃影响力中间状态；定义复制与QuickStart装配顺序需保留新catalog；现有事件UI/MapPresenter按PublicEventView驱动选择，可复用。T1 in progress.

T1 RED: playable-events-core-red1664/1680,16 expected NotImplementedException failures,0skip,exit2,XML932c9567d1d795668a4276181d9badfa4c2b2017eb3245a778672f5510b4c97f. Core catalog/event transaction implemented; 8 effects remain data-loaded.

T1 GREEN: playable-events-core-green1680/1680,0skip,exit0,XML14c5e1b3d8699f67f201311d7793efaa1aafd3411b07dedb6d9cd31cf9982f7d. T2 DTO/host/controller failing cases next; existing generic Next calendar limits still apply.

T2 RED: playable-events-session-red1681/1687,6 expected failures (permission/DTO/host completion/controller),0skip,XML e1684e53da0cc05b850979f00ef46a0ebbf003553835354d1c08b90f20a1ca22. Host now calls complete wrapper before committing, public event result and controller Event use wired. T1 final calendar-slot regression added against existing Next service (no new calendar implementation).

T1/T2 GREEN complete: playable-events-session-green1688/1688,0skip,exit0,XML8658f93460ce3acf9ee3d8aa0cc8d8282a5d2a1fab2fc1d65975bd998df65443. Includes final-slot opponent event regression. T3 catalog/UI next.

T3 RED: playable-events-catalog-red1688/1691,3 expected NotImplementedException failures,0skip,XML1f35b67ba56ae8c3ab5d56f2eb479c0591c64501208193ebcc0360bfc4da3939. Catalog/UI/build wired, same existing35payloads; no duplicated rule values.

T3 GREEN complete: playable-events-catalog-green1691/1691,0skip,exit0,XMLbd43ff1bbe2cbc3aeb8fed573eb1da39fdf7fdfec44ca629a9cc7da7f7037050. Real default Suez→Nasser chain covered. EventPlaytest build exit0,97496844 bytes,35 payloads byte-identical to canonical files,198 C# source hashes recorded. Initial map/hands actual1280x820 render inspected; Event button/choice and mouse are not visually verified. Metadata121/121/71/217/600/228 passed; independent review pending.
