using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class EventFactFoundationTests
    {
        static CardId Card(string n)=>new CardId("card."+n);
        static string Data()=>PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/treaty-protection.parameters.json");
        static string Schema()=>PlayInfluenceEventTests.Read("Docs/Contracts/treaty-protection.schema.json");
        static InfluenceEventFactDefinition Definition(string json=null,string schema=null)
        {
            var map=DefinitionTests.World();var cards=CardCatalogLoaderTests.Load();
            var effect=MarshallPlanInfluenceLoader.Load(PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/alliance-prerequisites.parameters.json"),PlayInfluenceEventTests.Read("Docs/Contracts/alliance-prerequisites.schema.json"),map,cards);
            return EventFactDefinitionLoader.Load(json??Data(),schema??Schema(),map,cards,new[]{effect});
        }
        static GameState Board(InfluenceEventFactDefinition d,int legal=8,string game="facts",long version=3,long sequence=7,string id="resolution.1",MapDefinition map=null,CardCatalog cards=null,InfluenceEffectDefinition effect=null)
        {
            map??=d.Map;cards??=d.Cards;effect??=d.Effects[Card("marshall_plan")];var countries=map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));
            var west=map.GetCountriesInRegion("region.western_europe").OrderBy(c=>c.Id.Value,StringComparer.Ordinal).ToArray();
            for(int i=0;i<west.Length;i++)countries[west[i].Id]=i<legal?new CountryState(1,1):new CountryState(1,1+west[i].Stability);
            return new GameState(map,countries,new OperationsContext("fact-fixture",PlayerSide.USSR,0,Array.Empty<CountryId>(),costs:new InfluenceCosts(1,2)),game,version,eventSequence:sequence,
                cards:FiveYearPlanRulesTests.Zones(cards,Array.Empty<string>(),parentResolving:false),randomState:FiveYearPlanRulesTests.Random(),
                influenceEvent:new InfluenceEventExecution(id,effect,PlayerSide.USSR,InfluenceEventPolicy.NoActiveInterference));
        }
        static InfluenceEventTransition Complete(GameState state)
        {
            var t=InfluenceEventFlow.Begin(state,PlayerSide.USSR);Assert.That(t.Accepted,Is.True,t.Error.ToString());
            if(t.NextState.InfluenceEvent.Stage==InfluenceEventStage.AwaitingChoice)
            {
                var q=InfluenceEventRules.Query(t.NextState,t.NextState.InfluenceEvent.Definition,InfluenceEventPolicy.NoActiveInterference);
                t=InfluenceEventFlow.Resolve(t.NextState,q.Owner,t.NextState.InfluenceEvent.DecisionId,q.LegalCountries.Take(q.RequiredTargets).ToArray());
                Assert.That(t.Accepted,Is.True,t.Error.ToString());
            }
            return t;
        }
        // Adversarial same-assembly caller fixture: production transition construction is internal.
        static InfluenceEventTransition Torn(InfluenceEventTransition raw,long? version=null,long? sequence=null,bool ended=false,bool noExecution=false)
        {
            var s=raw.NextState;var n=new GameState(s.Map,s.Countries,s.Operations,s.GameId,version??s.Version,eventSequence:sequence??s.EventSequence,ended:ended,
                cards:s.Cards,randomState:s.RandomState,influenceEvent:noExecution?null:s.InfluenceEvent);
            return (InfluenceEventTransition)Activator.CreateInstance(typeof(InfluenceEventTransition),BindingFlags.Instance|BindingFlags.NonPublic,null,
                new object[]{RuleError.None,InfluenceEventError.None,n,raw.Changes,null,raw.ResolutionSequence,null},null);
        }
        [Test] public void Formal_policy_binds_explicit_coverage_and_canonical_digest()
        {
            var d=Definition();Assert.That(d,Is.Not.Null);Assert.That(d.Effects.Keys,Is.EqualTo(new[]{Card("marshall_plan")}));Assert.That(d.RulesetId,Is.EqualTo("ruleset.deluxe2015"));
            Assert.That(d.ContentVersion,Is.EqualTo("0.1.0"));Assert.That(d.DataDigest,Does.Match("^[0-9a-f]{64}$"));Assert.That(d.Map.Countries.Count,Is.EqualTo(84));
            Assert.That(()=>((IDictionary<CardId,InfluenceEffectDefinition>)d.Effects).Clear(),Throws.InstanceOf<NotSupportedException>());
            var root=JObject.Parse(Data());Assert.That(Definition(root.ToString()).DataDigest,Is.EqualTo(d.DataDigest));
        }
        [TestCase("qualifying")][TestCase("nonqualifying")][TestCase("duplicate")][TestCase("history")][TestCase("source")][TestCase("pending")][TestCase("missing-ref")][TestCase("duplicate-source")][TestCase("url")][TestCase("version")][TestCase("scope")][TestCase("extra")]
        public void Unsupported_policy_or_unverified_reference_rejects(string fault)
        {
            var r=JObject.Parse(Data());var p=r["event_fact_policy"];
            switch(fault){case "qualifying":p["qualifying_outcomes"][0]="Cancelled";break;case "nonqualifying":p["nonqualifying_outcomes"][0]="ResolvedChanged";break;
                case "duplicate":((JArray)p["qualifying_outcomes"]).Add("ResolvedChanged");break;case "history":p["unknown_history"]="KnownEmpty";break;case "source":p["source_refs"][0]["source_id"]="F2010";break;
                case "pending":p["source_refs"][0]["verification"]="Pending";break;case "missing-ref":((JArray)p["source_refs"]).Clear();break;
                case "duplicate-source":((JArray)r["source_catalog"]).Add(r["source_catalog"][0].DeepClone());break;case "url":r["source_catalog"][1]["url"]="https://example.invalid";break;
                case "version":r["content_version"]="99.0.0";break;case "scope":r["scope"]="fixture";break;case "extra":p["code"]="noop";break;}
            Assert.That(()=>Definition(r.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("empty")][TestCase("duplicate")][TestCase("card")][TestCase("effect")][TestCase("beneficiary")][TestCase("digest")][TestCase("version")][TestCase("ruleset")]
        public void Definition_rejects_unbound_or_malformed_coverage(string fault)
        {
            var d=Definition();Assert.That(d,Is.Not.Null);var effect=d.Effects.Values.Single();IEnumerable<InfluenceEffectDefinition> effects=new[]{effect};
            if(fault=="empty")effects=Array.Empty<InfluenceEffectDefinition>();if(fault=="duplicate")effects=new[]{effect,effect};
            if(new[]{"card","effect","beneficiary"}.Contains(fault))effects=new[]{new AddFixedInfluenceEffect(fault=="card"?"card.unknown":effect.CardId,fault=="effect"?"effect.fidel":effect.EffectId,
                fault=="beneficiary"?PlayerSide.USSR:PlayerSide.US,new CountryId("country.france"),1)};
            Assert.That(()=>new InfluenceEventFactDefinition(d.Map,d.Cards,effects,fault=="ruleset"?"unknown":d.RulesetId,fault=="version"?"99.0.0":d.ContentVersion,fault=="digest"?"bad":d.DataDigest),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Contract_and_missing_bindings_reject()
        {Assert.That(()=>Definition(schema:Schema().Replace("urn:coldwarheat:contract:treaty-protection:1","wrong")),Throws.InstanceOf<ArgumentException>());Assert.That(()=>EventFactDefinitionLoader.Load(Data(),Schema(),null,CardCatalogLoaderTests.Load(),Array.Empty<InfluenceEffectDefinition>()),Throws.InstanceOf<ArgumentException>());}
        [TestCase(true)][TestCase(false)] public void Explicit_history_known_controls_absence_not_witnessed_success(bool known)
        {
            var d=Definition();Assert.That(d,Is.Not.Null);var ledger=EventFactLedger.Create(d,"facts",known);Assert.That(ledger,Is.Not.Null);
            Assert.That(ledger.HasSuccessfulEvent(Card("marshall_plan")).Status,Is.EqualTo(known?EventFactQueryStatus.NotSatisfied:EventFactQueryStatus.RequiresRuling));
            Assert.That(ledger.EvaluateAny(new[]{Card("marshall_plan"),Card("warsaw_pact_formed")}).Status,Is.EqualTo(EventFactQueryStatus.RequiresRuling));
            var result=ledger.RecordCompleted(Complete(Board(d)));Assert.That(result.Accepted,Is.True,result.Error.ToString());
            Assert.That(result.After.EvaluateAny(new[]{Card("marshall_plan"),Card("warsaw_pact_formed")}).Status,Is.EqualTo(EventFactQueryStatus.Satisfied));Assert.That(result.After.HistoryKnown,Is.EqualTo(known));
        }
        [TestCase(0)][TestCase(8)] public void Actual_completed_change_or_nochange_records_one_immutable_fact(int legal)
        {
            var d=Definition();Assert.That(d,Is.Not.Null);var s=Board(d,legal);var completed=Complete(s);var ledger=EventFactLedger.Create(d,s.GameId,true);Assert.That(ledger,Is.Not.Null);
            var record=ledger.RecordCompleted(completed);Assert.That(record.Accepted,Is.True,record.Error.ToString());Assert.That(record.AlreadyApplied,Is.False);Assert.That(ledger.Facts,Is.Empty);
            var fact=record.Fact;Assert.That(fact.ResolutionId,Is.EqualTo("resolution.1"));Assert.That(fact.Card,Is.EqualTo(Card("marshall_plan")));Assert.That(fact.EffectId,Is.EqualTo("effect.marshall_plan"));
            Assert.That(fact.Owner,Is.EqualTo(PlayerSide.US));Assert.That(fact.PhasingPlayer,Is.EqualTo(PlayerSide.USSR));Assert.That(fact.Outcome,Is.EqualTo(legal==0?InfluenceEventStatus.ResolvedNoChange:InfluenceEventStatus.ResolvedChange));
            Assert.That(fact.CommittedVersion,Is.EqualTo(completed.NextState.Version));Assert.That(fact.CommitSequence,Is.EqualTo(completed.ResolutionSequence));Assert.That(record.After.Facts.Single(),Is.SameAs(fact));
            Assert.That(record.After.RecordCompleted(completed).AlreadyApplied,Is.True);Assert.That(record.After.RecordCompleted(completed).After,Is.SameAs(record.After));
            Assert.That(()=>((IList<SuccessfulEventFact>)record.After.Facts).Clear(),Throws.InstanceOf<NotSupportedException>());
            Assert.That(completed.NextState.Cards,Is.SameAs(s.Cards));Assert.That(completed.NextState.RandomState,Is.SameAs(s.RandomState));Assert.That(completed.NextState.Operations,Is.SameAs(s.Operations));
        }
        [TestCase("unknown")][TestCase("scoring")][TestCase("china")][TestCase("duplicate")][TestCase("empty")][TestCase("null")]
        public void Invalid_query_is_not_hidden_by_an_existing_success(string fault)
        {
            var d=Definition();Assert.That(d,Is.Not.Null);var first=EventFactLedger.Create(d,"facts",true);Assert.That(first,Is.Not.Null);var ledger=first.RecordCompleted(Complete(Board(d))).After;Assert.That(ledger,Is.Not.Null);
            IEnumerable<CardId> query=fault=="null"?null:fault=="empty"?Array.Empty<CardId>():new[]{Card("marshall_plan"),Card(fault=="unknown"?"unknown":fault=="scoring"?"europe_scoring":fault=="china"?"china":"marshall_plan")};
            var q=ledger.EvaluateAny(query);Assert.That(q.Status,Is.EqualTo(EventFactQueryStatus.Rejected));Assert.That(q.Error,Is.EqualTo(RuleError.InvalidDefinition));Assert.That(ledger.Facts.Count,Is.EqualTo(1));
        }
        [TestCase("game")][TestCase("map")][TestCase("catalog")][TestCase("definition")][TestCase("id")]
        public void Valid_flow_from_wrong_authority_or_definition_cannot_record(string fault)
        {
            var d=Definition();Assert.That(d,Is.Not.Null);var ledger=EventFactLedger.Create(d,"facts",true);Assert.That(ledger,Is.Not.Null);
            var replacement=new AddToDistinctCountriesEffect("card.marshall_plan","effect.marshall_plan",PlayerSide.US,new[]{"region.western_europe"},7,1,InfluenceControlFilter.NotOpponentControlled,InfluenceCandidateShortagePolicy.SelectAllAvailable);
            var s=Board(d,game:fault=="game"?"other":"facts",map:fault=="map"?DefinitionTests.World():null,cards:fault=="catalog"?CardCatalogLoaderTests.Load():null,effect:fault=="definition"?replacement:null,id:fault=="id"?" resolution.1":"resolution.1");
            var record=ledger.RecordCompleted(Complete(s));Assert.That(record.Accepted,Is.False);Assert.That(record.After,Is.Null);Assert.That(ledger.Facts,Is.Empty);
        }
        [Test] public void Waiting_and_rejected_flow_do_not_record_success()
        {
            var d=Definition();Assert.That(d,Is.Not.Null);var ledger=EventFactLedger.Create(d,"facts",true);Assert.That(ledger,Is.Not.Null);var s=Board(d);
            Assert.That(ledger.RecordCompleted(InfluenceEventFlow.Begin(s,PlayerSide.USSR)).Error,Is.EqualTo(RuleError.EventNotStarted));
            Assert.That(ledger.RecordCompleted(InfluenceEventFlow.Begin(s,PlayerSide.US)).Accepted,Is.False);Assert.That(ledger.RecordCompleted(null).Accepted,Is.False);Assert.That(ledger.Facts,Is.Empty);
        }
        [TestCase("version")][TestCase("sequence")][TestCase("ended")][TestCase("missing-execution")]
        public void Torn_internal_receipt_does_not_create_a_fact(string fault)
        {
            var d=Definition();Assert.That(d,Is.Not.Null);var ledger=EventFactLedger.Create(d,"facts",true);Assert.That(ledger,Is.Not.Null);var raw=Complete(Board(d));
            var torn=Torn(raw,version:fault=="version"?0:(long?)null,sequence:fault=="sequence"?raw.NextState.EventSequence+1:(long?)null,ended:fault=="ended",noExecution:fault=="missing-execution");
            Assert.That(ledger.RecordCompleted(torn).Accepted,Is.False);Assert.That(ledger.Facts,Is.Empty);
        }
        [Test] public void Conflicting_id_and_nonadvancing_new_evidence_reject_but_ordered_append_succeeds()
        {
            var d=Definition();Assert.That(d,Is.Not.Null);var empty=EventFactLedger.Create(d,"facts",true);Assert.That(empty,Is.Not.Null);var one=Complete(Board(d));var ledger=empty.RecordCompleted(one).After;Assert.That(ledger,Is.Not.Null);
            Assert.That(ledger.RecordCompleted(Complete(Board(d,version:10,sequence:20))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That(ledger.RecordCompleted(Complete(Board(d,id:"old",version:0,sequence:0))).Error,Is.EqualTo(RuleError.StaleContext));
            Assert.That(ledger.RecordCompleted(Complete(Board(d,id:"bad-sequence",version:10,sequence:0))).Error,Is.EqualTo(RuleError.StaleContext));
            var next=ledger.RecordCompleted(Complete(Board(d,id:"resolution.2",version:10,sequence:20)));Assert.That(next.Accepted,Is.True);Assert.That(next.After.Facts.Select(f=>f.ResolutionId),Is.EqualTo(new[]{"resolution.1","resolution.2"}));
            Assert.That(ledger.Facts.Count,Is.EqualTo(1));Assert.That(next.After.RecordCompleted(one).After,Is.SameAs(next.After));
        }
        [Test] public void Creation_requires_explicit_binding_and_trimmed_game_id()
        {var d=Definition();Assert.That(d,Is.Not.Null);Assert.That(()=>EventFactLedger.Create(null,"facts",true),Throws.InstanceOf<ArgumentException>());Assert.That(()=>EventFactLedger.Create(d," facts",false),Throws.InstanceOf<ArgumentException>());}
    }
}
