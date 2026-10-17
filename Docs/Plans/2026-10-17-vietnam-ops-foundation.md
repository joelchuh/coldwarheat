# Vietnam OPS Foundation Implementation Plan

REQUIRED SUB-SKILL: executing-plans inline; one fresh whole-branch reviewer after GREEN. User authorizes autonomous useful work and Git sync. Existing Unity checkout reused; do not delete it.
Base c9702a16e231ffec90f875169a387d3e421a3980; branch feat/vietnam-ops-foundation.

Goal: reuse a fixed-country grant and immutable turn regional contribution before enabling the Vietnam card in real sessions.
Design: AddFixedInfluenceEffect uses existing InfluenceMutation/Plan/Flow; TurnOpsEffectDefinition gains a regional constructor pairing OpsRegionalBonus with this grant, fixed USSR recipient. Existing flat constructor and flat-only catalog remain closed. GetModifiers excludes regional bonuses; GetRegionalBonuses validates turn/side and returns only actor-owned conditional contributions. VietnamRevoltsDefinitionLoader validates shared schema/profile/sources, exact supported identity/region/country/side/lifetime and printed binding, reads amount/delta from canonical data. No new rule parameter copies.
Existing turn expiry removes all instances; integration into real Event/OPS/headline/UI will be a following batch. This batch does not claim a playable Vietnam card or atomic combined activation; individual grant Plan and immutable activation state are the foundation for that transaction.
Sources: existing persistent-effects Wiki/parameters; official GMT 2015 rule7.4 rechecked2026-10-17. 1−1+1=1 remains user-confirmed interpretation.

## Review Focus
1. Regional contribution must never enter flat Space/OpsThreshold values or change US OPS; invalid actor/turn must fail explicitly.
2. Fixed grant must honor shared interference/setup/ended gates; no partial mutation on amount/version/sequence overflow, and no OPS/adjacency/control restrictions on a direct event.
3. Strict loader must reject wrong semantics and numeric coercions; no hardcoded duplicate amount, and old flat loader/catalog stays compatible and cannot enable unintegrated Vietnam.
4. Multiple accepted targets independently forfeit China/SEA bonuses; existing budget combination and min/max policy preserved.

## Task1 — fixed grant and regional turn effect foundation
Create Core/Effects/AddFixedInfluenceEffect.cs, Infrastructure/DataLoading/VietnamRevoltsDefinitionLoader.cs, Tests/EditMode/VietnamOpsFoundationTests.cs (+metas); extend Core/Effects/InfluenceEventRules.cs and Core/Rules/TurnOpsEffects.cs. Document descriptor/Wiki/report/worklog/catalog/index.
- [x] Tests first with compile-only stubs: strict data bindings/source/type/profile, amount mutation, constructor restrictions, grant/overflow/gates/immutability, fixed USSR independent of controller, Flat separation, actor/turn boundary, retries/conflicts/repeat, China+SEA accepted-prefix budgets; native RED.
- [x] Implement bounded core and loader; native GREEN; metadata/diff checks; commit; one read-only review.
- [x] Record evidence/limits and push/verify remote. No Player rebuild: runtime catalog intentionally remains disabled. Continue real-session integration next.
Validation: Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId <unique> -TimeoutSeconds600; inspect XML. Do not edit C# or loaded data during Unity run.
