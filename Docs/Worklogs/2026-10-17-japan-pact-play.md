# SDD ledger — plan: Docs/Plans/2026-10-17-japan-pact-play.md

Base0e26bbdaf5a64fffb5908a4d089854d3c6604dfc (foundation remote SHA verified); branch feat/japan-pact-play.
Pre-flight Task1→Task2: public JapanPact and ActivatedJapanPact receipt, explicit WithJapanPact factory — exact names agree. Task1 in progress; Task2 pending.
Ruling: Continue inline without approval handoffs under user's explicit autonomous instruction — existing direction and official rules are clear — changing preference later could require revising the isolated slice.
Ruling: Store pact on GameState instead of every transient practice phase — central copies preserve permanent state and aggregate chronology can be validated — future multiple permanent families may warrant a collection wrapper, without changing map presentation.
Ruling: Preserve foundation candidate API but enforce chronological watermark at GameState publication — a private candidate precedes the committed board — missing this invariant could otherwise pair future protection with old state.
Ruling: Reuse Unity checkout on feature branch and durable PowerShell ledger, preserve untracked user assets — consistent project workflow avoids duplicate imports — scoped staging is required because checkout is shared.
Pre-RED setup failure: one-off exact-replacement guard found two Olympic assignments and stopped before remaining API skeletons. The orchestrated Unity command nevertheless started, exiting with CS1061 / TEST_REPORT_MISSING (no tests ran). Narrowed the replacement to the definition assignment and completed stubs; subsequent orchestration explicitly checks exit_code before dependent commands.

Task1 RED japan-play-core-red-2: 3935 total / 3902 passed / 33 new JapanPactPlayTests failures, no old regression or skipped tests; Unity exit2, 186055ms. XML SHA256 ae7c754338fed382c1cb19e54d38c1cbac938b53c2b74eacdc4a84427b97d232. Implementation follows this observed failing suite. A preflight-only patch count mismatch (20 vs 21 generic clones) wrote nothing; corrected the expected count, then separately patched the Olympic replacement clone.

Task1 first GREEN attempt: 3934/3935; only cross-turn test failed TurnPreparationUnavailable. Root cause: PlayScoringTests.Ready deliberately enters all eras, so TurnDealService correctly rejects advancing back to Early era. Ruling: use NextTurnPlayTests real early-deal fixture for cross-turn assertion — preserves production chronology guard — a fixture-only correction, not relaxation of the rule.
Fixture correction initially used an internal ActionPracticeState constructor unavailable to tests (CS1729; no tests ran). Replaced with public Create plus normal StartPractice command.
Task1 green-3: same 3934/3935; revised fixture attempted StartPractice after losing completed deployment, so setup guard rejected and a missing assertion caused NullReference in Give. Final fixture keeps the already started Ready practice and replaces only test card zones/random with InitialDealService output before playing pact. Production behavior unchanged.

Task1: complete; native japan-play-core-green-4 3935/3935, Unity0, 178236ms, XML cae1bc063b7be131e58ce87579e00dacc29a23f2c29705638804035f5503aa5d; metadata 188 descriptors / 89 datasets passed. Task2 begins from this tested Core/Host/catalog state.
Task2 RED preflight: NUnit Does.Not.Contain selected a string-only overload for CountryId; replaced with collection Has.No.Member / Has.Member. No tests ran in initial compile-failure attempt.

Task2 RED japan-play-demo-red-2: 3952 total / 3936 pass / 16 new Demo/Presentation failures; registration-preservation test already passes Task1. XML 2c285b515d2dba75e00e8b4f47a872c631fa332e27d26d69426fa1e7f9a2775e. Added closed fixture loader, public permanent feedback, combined receipts, explicit bootstrap/build registration and 74-entry payload manifest.
Task2 first GREEN: 3951/3952. Headline assertion counted the country in both permanent summary and influence detail; production emitted one grant. Tightened assertion to the exact country+US change-row prefix, preserving the intended no-duplicate-grant check.

Task2 implementation complete: japan-play-demo-green-2 3952/3952, Unity0,180536ms, XML8798af265d01ae4572125c9d56c3347b789d2a3e5bed2d1343e194ce1686d443. One fresh whole-branch review, independent build and limited native UI remain.

Final review: review_japan_play, base0e26bbd..d7e3cf5; 0 Critical / 0 Important / 1 Minor, source ready. Independently read matching3952 XML.
Final: minor (deferred): GameState.WithPractice internal explicit inactive same-definition state can replace an active pact. Current commands never supply it; future internal callers should be guarded and tested before cancellation/import expansion.
Final: Ruling: Actual Player/layout/mouse acceptance set aside by reviewer is verified by parent — build exit0,74 payloads/832454 bytes, direct event and protected/allowed target interaction observed — limited route does not prove every card/UI branch.
Final: Ruling: NATO/cancellation/save-import/later interactions remain excluded — registered Japan-only slice is explicit, no whole-game claim — those future services need their own lifecycle tests.
Native UI: ordinary coup and adjustment Japan disabled with unchanged state/OPS; Angola adjustment rolled4/4 and spent1 OPS. Grant1->7 with SU3 retained and expanded permanent explanation observed. F12 captures copied with SHA into validation. No Error/Exception observed in player log.
Ruling: Fix ordinary coup rejection hint hidden by the drawing branch with one display-only call — source/public preview already tested, actual UI exposed the missing render — verification is build plus repeat mouse rendering; no new mirror test/full-suite rerun for this reversible low-impact layout correction, per developer test guidance.3952 suite applies to pre-followup source d7e3cf5.
