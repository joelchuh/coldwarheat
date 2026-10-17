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
