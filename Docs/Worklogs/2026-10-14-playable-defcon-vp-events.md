# SDD ledger — plan: Docs/Plans/2026-10-14-playable-defcon-vp-events.md

BASE: 5d000704cf1f4c595f2d6a3d52a1f86e48e44cae
Ruling: 本批只接两张无需等待选择的即时事件 — 先打通默认早期手牌里的可玩行为，第三张How I Learned继续禁用 — 成本：完整三牌设计仍未全部运行。
Ruling: 即时事件内顺序计算、一次原子提交 — 当前没有插入/选择/恢复点，纯服务每步判终局并截断，避免半结算状态 — 成本：未来完整事件栈需独立可恢复编排，不能把当前实现等同完整DV1。
Ruling: 使用并列类型化catalog而非统一对象黑箱 — 第二种事件有独立轨道/VP输出，复用Event命令及根生命周期即可 — 成本：新增事件族需显式装配和冲突校验。
Preflight: PracticeRiskState须保留军事/政变收据，只换DEFCON/结果；Definition所有复制点保留两个catalog；PlanEvent不再假定所有事件都是Influence；CardActionResult须新增公开轨道反馈，不能把Defcon计划伪装InfluenceEventTransition；Build/Bootstrap2条读取路径共享同一参数和Schema。

T1 RED: defcon-events-core-red1692/1708,16expectedNotImplemented failures,1new catalog guard passes,0skip,XML11a16f994531f9d1fb1f9fea1228386c49cae42a0a5f3c685a1e03635579c2c1. Actual failed methods inspected before implementation.

T1 attempted GREEN:1707/1708,only valid-loader test failed Unsupported contract keyword at ContractJson.CheckSchema. Root cause: shared bounded validator lacks prefixItems/boolean items used by authoritative ordered-step schema; service tests passed. Ruling: 扩展有界验证器的顺序数组与布尔items支持 — 保留正式Schema且拒绝错误顺序/额外尾项，不绕过验证 — 成本：共享基础验证器范围扩大，增加独立Tuple回归并完整跑套件。

Tuple RED:1708/1712,4expected failures (valid loader +3tuple semantics),XMLcd8de45ea812f88b81b4f8ae4d4c3d6d23d65fb9566daf3a00ef8f56492da930. Added bounded prefixItems traversal and boolean items/tail semantics, unknown/malformed/recursive schemas still reject.

T1 GREEN complete: defcon-events-tuple-green1712/1712,0skip,exit0,XML369f84a921afa5f78bf01d20817d0c7bdf8dfe8aacc062e35f3ff3e0bcdda8d4. T2 structural contracts + lifecycle/session tests added; runtime dispatch not yet wired.

T2 RED: defcon-events-action-red1715/1726,11expected dispatch/result failures and3guard passes,0skip,XML e3ab19afaad98d3c7b023b162ea5a541c95764a38e58159b89d4c82b52e583e9. Now PlanEvent dispatches typed track catalog, atomic final state and public DTO forwarded through existing host receipts.

T2 GREEN complete: defcon-events-action-green1726/1726,0skip,exit0,XML2f4e271ccbae63be0d9c975b31b351de9e539a8d3fa797ad99d3b01fc928811d. Includes real coup→space→Duck preservation, terminal replay and full transaction overflow. T3 catalog failing tests next.

T3 RED: defcon-events-catalog-red1726/1729,3expectedNotImplemented failures,0skip,XML1ff329962d18a0454db8c1cdfc6140d98f27e45e7ee11d1d61663b5d2076aaf8. Loader assembly,2payloads and UI ordered result now wired. Build target独立DefconEventPlaytest，不覆盖旧进程。

T3 GREEN complete: defcon-events-catalog-green1729/1729,0skip,exit0,XML07e208657c566c0e998c205c06685d2e2ef1f5f11605ce553b9f32fd6ee28f55. Build exit0,97526820bytes;37payloads exact-source bytes,207 C# hashes; initial map/hands1280x820 actual renders inspected. Metadata123/123/71/225/604/236. Mouse/result panel unverified. Final review pending.
