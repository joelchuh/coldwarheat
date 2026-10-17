# Marshall influence foundation implementation plan

> REQUIRED SUB-SKILL: executing-plans inline. User authorized autonomous execution; no repeated design approval.

Goal: reusable explicit shortage policy and strictly loaded Marshall influence candidate, without exposing incomplete real card event.
Architecture: extend existing immutable definition/query/planner; reuse Flow. Dedicated Infrastructure loader binds canonical data and printed definition.
Tech stack: pure C#, Unity6000.6.0f1 EditMode, JSON.
Spec: Docs/Architecture/marshall-foundation.md.

## Global constraints
- Base1e90a3aa21a2ee9873091013126a88a3ad3bb10c, branch feat/marshall-foundation; reuse Unity checkout; preserve two untracked user files.
- No real-card registration, NATO/event-fact state, Warsaw handler, Player/UI change or new workflow script.
- Shared canonical numbers only; default shortage remains RequiresRuling. No unknown interference fallback.

## Review Focus
1. Opt-in policy cannot silently alter Comecon/decolonization defaults.
2. Zero candidates must produce one completed no-change candidate without empty decision, with version/sequence overflow rejection.
3. Loader rejects incorrect printed binding, incomplete source evidence and fact-policy redirects; valid parameters use canonical values.
4. Query/plan recompute exact current control; response owner/version/count duplicates and immutable receipt order remain safe.
5. Foundation is not registered as full Marshall event; all UI/real-card hosts retain prior capability until atomic success facts exist.

## Task1 — definition, loader and shared planner foundation
Files: modify Core/Effects/InfluenceEffectDefinition.cs and InfluenceEventRules.cs; create Infrastructure/DataLoading/MarshallPlanInfluenceLoader.cs, Tests/EditMode/MarshallFoundationTests.cs; corresponding module/Wiki/report/worklog, catalog entry.
Interfaces: new enum InfluenceCandidateShortagePolicy { RequiresRuling, SelectAllAvailable }; AddToDistinctCountriesEffect optional last parameter shortagePolicy=RequiresRuling and property ShortagePolicy. MarshallPlanInfluenceLoader.Load(string json,string schema,MapDefinition map,CardCatalog cards) returns AddToDistinctCountriesEffect. Existing Query/Plan and InfluenceEventFlow.Begin/Resolve remain callers.

- [x] Add tests plus compile-only API skeleton. Cases: formal profile/binding and tampered data; all shortage counts0..8; formal western membership; complete positive choices; rejected invalid inputs; shared guardrails and overflow; Flow no-choice/US-owner response and replay; defaults and unknown enum.
```csharp
var q=InfluenceEventRules.Query(state,effect,InfluenceEventPolicy.NoActiveInterference);
Assert.That(q.RequiredTargets,Is.EqualTo(Math.Min(7,legalCount)));
var zero=InfluenceEventRules.Plan(noLegalState,effect,null,InfluenceEventPolicy.NoActiveInterference);
Assert.That(zero.Status,Is.EqualTo(InfluenceEventStatus.ResolvedNoChange));
```
- [x] Run Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId marshall-foundation-red -TimeoutSeconds 600. Expected new failing assertions; no old regressions.
- [x] Implement strict loader and opt-in query; planner skips nonexistent target list only when required=0.
```csharp
if(union.Length<add.TargetCount && add.ShortagePolicy==InfluenceCandidateShortagePolicy.RequiresRuling)
    return Stop(InfluenceEventStatus.RequiresRuling,InfluenceEventError.CandidateShortage);
candidates=union;required=Math.Min(add.TargetCount,union.Length);
// Add branch: when required>0 use validated selection; otherwise no mutation.
```
- [x] Run same native command with marshall-foundation-green. Expected full pass. Validate metadata, diff, commit source/evidence.
- [ ] One fresh whole-branch review; Critical/Important one RED→GREEN fix pass/full suite; Minor ledger. Push feature branch and verify exact remote SHA. No Player build because no registered runtime change.

Self-review: one deliverable, no task interface conflicts; all five review classes have named test cases; source/data/UI boundaries explicit.
