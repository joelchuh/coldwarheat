# Influence Result Details Implementation Plan

> REQUIRED SUB-SKILL: executing-plans inline; one fresh whole-branch reviewer. User requests autonomous continued development, no routine approval handoff.

**Goal:** 玩家从公开事件结果直接看清国家/阵营/前后数值，且不误读旧收据；修正迁移反馈空间。
**Architecture:** 仅Presentation，复用公开InfluenceChanged和既有结果页，按收据顺序格式化，不读取隐藏状态或重算规则。
**Spec:** 本文设计段及Docs/Worklogs/2026-10-17-influence-result-details.md
**Base:** cbe8ca28c96820772411439007df68b51064d9b8
**Branch:** feat/influence-result-details
**Stack:** Unity6000.6 C# Presentation/NUnit，原有结果/本地化/地图接口。

## Design and constraints
- 范围明确的既有流程改进；用户已要求自主决定，brainstorming设计已在会话说明，无额外审批。
- 影响力明细显示国家名、固定阵营、Before→After与带符号差值，遵循收据顺序；零变化明确显示，不从现有棋盘猜测。
- 直接即时事件/确认选择/五年计划子事件/多个头条保持上下文。事件还在等待时不把空Changes叫作零变化完成。
- ReadPracticeFeedback/ReadEventFeedback保留已有错误与历史优先级；历史结果不追加本次明细。现有隐藏信息与身份切换约束保持。
- 扩展既有CurrentHeadlineFeedback记录公开影响力事件，按同一批收据顺序分块；其他种类已有格式保留。
- 迁移控件压缩空隙，反馈至少可显示多行且不覆盖底部按钮；共享政变摘要/拒绝反馈。此为新一批用户授权的可读性工作，不偷加到经互会审查修补。
- 无规则/牌数值/Host修改，无新工作流脚本，无新数据副本；保留用户未跟踪文件、旧Players。

## Review Focus
1. Historical retries, failed commands and side/scene changes must not present old influence values as a fresh result.
2. Multiple headline and Five-Year Plan child effects retain execution order and never duplicate rows or omit the parent context.
3. Waiting choices with empty Changes remain waiting; completed zero change is explicit.
4. Language changes resolve stable IDs using current display names; neither formatting nor opening/closing alters state or drafts.
5. Relocation panel can show readable feedback and prior coup summary without overlapping confirmation or bottom controls; actual screenshot separate from tests.

## Task1 — receipt formatting, lifecycle presentation and live proof
Files: new Presentation/DemoView.InfluenceFeedback.cs +meta, Tests/EditMode/InfluenceFeedbackTests.cs +meta; modify DemoView.Actions.cs, DemoView.Events.cs, DemoView.HeadlineFeedback.cs, DemoController.HeadlineFeedback.cs, DemoView.Relocation.cs, Editor/DemoBuild.cs. Update demo-result-reader descriptor/Wiki, new details descriptor/Wiki/report and catalog.
- [x] Tests before implementation: immediate multi-change/selection/migration/zero, both sides and change signs, no waiting completion, historical retry/failed/identity/scene invalidation, names switch and fallback, two headlines/mixed child, reader leaves state and drafts unchanged; native RED.
- [x] Implement minimal shared formatter and append at correct receipt boundary; extend supported public headline summaries; native GREEN, inspect failures.
- [x] Layout/BuildInfluenceFeedbackPlaytest registration, metadata/ledger, source commit. Independent reviewer once; Important one RED/GREEN fix pass.
- [x] Build independent Player with existing70 payloads, verify; real event/relocation result reader and waiting layout; capture/hash evidence. Commit/push/remote verify and continue useful work.

Native: Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId <unique> -TimeoutSeconds600. Do not edit C# or loaded data during runs.
