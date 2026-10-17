# Event fact foundation implementation plan

> REQUIRED SUB-SKILL: executing-plans inline; user delegated routine decisions. One final fresh whole-branch review.

Goal: immutable evidence from completed influence flow and explicit known/unknown prerequisite queries.
Architecture: typed definition + strict canonical policy loader + completed-flow adapter; no GameState/host/UI registration yet.
Tech stack: C#, Unity6000.6.0f1, existing JSON contracts.
Spec: Docs/Architecture/event-fact-foundation.md.

## Global constraints
- Basee4628726c8a22bfa7f1dd4bc8544639cdaa22239, branch feat/event-fact-foundation; reuse current checkout, preserve two user files.
- Coverage is explicit; no arbitrary UI facts, no automatic completeness for unknown history, no card-zone inference.
- Existing shared treaty policy and original card catalog remain single sources. No Player build or new workflow script.

## Review Focus
1. Mixed query with success plus invalid/unknown ID validates fully before returning success; partial known coverage cannot produce false absence.
2. Completed receipt and authority snapshot must agree, including genuine execution instance, source game/map/cards, version/sequence and outcome.
3. Same ID exact retry differs from conflict; new fact must advance both watermarks, while old ledger and receipts remain immutable.
4. Loader rejects unsupported successful/non-success sets, source/profile/binding errors; no dormant NATO capability implied.
5. No GameState/host integration sneaks in; callers must still publish facts with the parent event atomically and verify known-empty histories.

## Task1 — definition, loader, ledger and native evidence
Create Core/Effects/InfluenceEventFactDefinition.cs, Core/Effects/EventFactLedger.cs, Infrastructure/DataLoading/EventFactDefinitionLoader.cs, Tests/EditMode/EventFactFoundationTests.cs and matching .meta; module JSON, Wiki, catalog, report/worklog.
Interfaces: InfluenceEventFactDefinition(MapDefinition,CardCatalog,IEnumerable<InfluenceEffectDefinition>,string ruleset,string contentVersion,string digest); EventFactDefinitionLoader.Load(string json,string schema,MapDefinition map,CardCatalog cards,IEnumerable<InfluenceEffectDefinition> effects); EventFactLedger.Create(definition,gameId,bool historyKnown); RecordCompleted(InfluenceEventTransition) returns EventFactRecording(Error,After,Fact,AlreadyApplied); HasSuccessfulEvent(CardId)/EvaluateAny(IEnumerable<CardId>) returns EventFactQuery(Status,Error). Status enum Satisfied/NotSatisfied/RequiresRuling/Rejected. SuccessfulEventFact exposes ResolutionId,Card,EffectId,Owner,PhasingPlayer,Outcome,CommittedVersion,CommitSequence; constructors internal.

- [x] Write tests and compile-only signatures. Fixture uses real CardZones/catalog, known Marshall definition and InfluenceEventFlow for actual changed/nochange evidence. Test invalid data/binding, rejected/awaiting/no-execution, game/map/catalog mismatch, exact retry/conflict/older new ID, unknown/known/partial queries, immutable ordering and no GameState effects.
```csharp
var record=ledger.RecordCompleted(completed);
Assert.That(record.Accepted,Is.True);
Assert.That(ledger.Facts,Is.Empty);
Assert.That(record.After.HasSuccessfulEvent(card).Status,Is.EqualTo(EventFactQueryStatus.Satisfied));
Assert.That(record.After.RecordCompleted(completed).AlreadyApplied,Is.True);
```
- [x] Run Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId event-fact-red -TimeoutSeconds 600. Expected new assertion failures, old4006 pass.
- [x] Implement bound definition and strict shared loader; ledger validates evidence before immutable append and fully validates queries before evaluation.
```csharp
if(matchesExistingExactly)return new EventFactRecording(RuleError.None,this,existing,true);
if(idAlreadyUsed)return new EventFactRecording(RuleError.CommandIdConflict);
if(version<=lastVersion || sequence<=lastSequence)return new EventFactRecording(RuleError.StaleContext);
// Query success from a witnessed fact; otherwise absence requires known history AND complete requested coverage.
```
- [x] Full native event-fact-green; expected full pass, metadata/diff valid; update evidence and commit.
- [x] One whole-branch review; one RED→GREEN/full-suite significant fix pass if needed; Minor ledger; routine feature push and exact remote check.

Self-review: one task, no shared task interface conflicts. Five review concerns have explicit tests. Boundary never promises runtime event registration, save import, NATO or visual acceptance.
