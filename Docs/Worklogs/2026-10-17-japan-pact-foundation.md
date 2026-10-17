# SDD ledger — plan: Docs/Plans/2026-10-17-japan-pact-foundation.md

Base dfeee6ef925467b7adb3d868628d33617d0d2cb2; branch feat/japan-pact-foundation.
Pre-flight: no shared interfaces between tasks; exact APIs and scope agree with spec. Task1 in progress.

Ruling: Continue design and implementation without an approval handoff — user's explicit autonomous continuation overrides skill question gates — a changed preference could require revising this small isolated foundation.
Ruling: Reuse user's Unity checkout on a new feature branch and native PowerShell ledger — preserve Unity caches and current workflow, Unix skill helper scripts unavailable — isolation relies on scoped staging; no other edits observed except two preserved user files.
Ruling: Implement a bounded Japan-only permanent state, with reusable influence primitive — avoids claiming NATO prerequisites/exceptions are supported — future NATO requires its own registration and service extension.
Ruling: Foundation activation is a candidate producer, not a grant/card transaction — actual runtime consumer will compose atomically in the next batch — until integration, players cannot use this new handler.

RED: japan-foundation-red, 3901 total / 3840 passed / 61 failed, all failures JapanPactFoundationTests (missing behavior/API skeletons), no skipped/inconclusive; XML 14a93805b64adc731b483ebe11f280c9c9cfee61ad7c524ecf503b9d006b3ada. Green run includes additional other-country/setup boundary coverage.

Task1 implementation complete: japan-foundation-green, 3902/3902 passed, Unity0, 181092ms, no skipped/inconclusive; XML 941c48b98fe4317488475c78cb37dffc197eded750b836e913700498f42433ca. Metadata187/89 and diff check passed. Independent review pending.
Metadata follow-up: after updating the data description, validation found Data/catalog.json summary mismatch. Core commit 51ae3f4 was already made by a command orchestration that failed to gate on that separate result; no push occurred. Synced the one catalog entry using the existing guarded script, then reran metadata before review. Gameplay test evidence is unchanged.

Final review: a967f04, 0 Critical / 0 Important / 1 Minor; reviewer independently inspected the recorded XML, no new execution. No fix pass needed.
Final: minor (deferred): direct printed-card affiliation/removal/reminder/effect/version/dataset/pointer mutations lack parameterized rejection tests; guards currently correct. Cover when expanding integration tests.
Final: Ruling: Actual dispatch, atomic grant/activation/card disposal and cross-turn retention remain next-batch work — no foundation consumer claims them — current Demo cannot play Japan event until integration.
Final: Ruling: Temporal compatibility is a trusted aggregate precondition in this foundation; add a strict GameState watermark guard during integration — candidate-producing API intentionally precedes publication — wrong composition could otherwise apply future protection to an older board.
Final: Ruling: Actual actor authentication stays at fixed-identity host boundary — foundation checks enum but cannot authenticate callers — integration must preserve session actor or an incorrect actor could bypass a restriction.
Final: Ruling: NATO, cancellation and save import remain unexposed — they require additional facts and lifecycle contracts — those features remain unavailable until their own batches.
Final: Ruling: DEFCON/budget/basic eligibility remain mandatory independent gates — NoTreatyBlock is explicitly not permission — integration must compose every relevant route before spending or rolling.
Final: Ruling: Map/catalog/digest provenance is trusted upstream input, not cryptographic asset authentication — loader and constructor validate supported semantics — untrusted asset import would need a separate authenticity boundary.
Task1 complete (dfeee6e..a967f04; run-unity-editmode japan-foundation-green 3902/3902; metadata187/89). Routine user-authorized push on feature branch, no merge or PR; preserve working checkout and user files.
