using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class MarshallFoundationTests
    {
        static CountryId C(string n)=>new CountryId("country."+n);
        static string Data()=>PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/alliance-prerequisites.parameters.json");
        static string Schema()=>PlayInfluenceEventTests.Read("Docs/Contracts/alliance-prerequisites.schema.json");
        static AddToDistinctCountriesEffect Load(string data=null,string schema=null,CardCatalog cards=null,MapDefinition map=null)
            =>MarshallPlanInfluenceLoader.Load(data??Data(),schema??Schema(),map??DefinitionTests.World(),cards??CardCatalogLoaderTests.Load());
        static AddToDistinctCountriesEffect Effect(InfluenceCandidateShortagePolicy policy=InfluenceCandidateShortagePolicy.SelectAllAvailable)
            =>new AddToDistinctCountriesEffect("card.marshall_plan","effect.marshall_plan",PlayerSide.US,new[]{"region.western_europe"},7,1,InfluenceControlFilter.NotOpponentControlled,policy);
        static GameState Board(int legal=8,long version=3,long sequence=7,Action<Dictionary<CountryId,CountryState>> edit=null,bool flow=false)
        {
            var map=DefinitionTests.World();var board=map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));
            var west=map.GetCountriesInRegion("region.western_europe").OrderBy(c=>c.Id.Value,StringComparer.Ordinal).ToArray();
            for(int i=0;i<west.Length;i++)board[west[i].Id]=i<legal?new CountryState(1,1):new CountryState(1,1+west[i].Stability);
            edit?.Invoke(board);
            return new GameState(map,board,new OperationsContext("marshall-fixture",PlayerSide.USSR,0,Array.Empty<CountryId>(),costs:new InfluenceCosts(1,2)),
                version:version,eventSequence:sequence,influenceEvent:flow?new InfluenceEventExecution("marshall",Effect(),PlayerSide.USSR,InfluenceEventPolicy.NoActiveInterference):null);
        }
        static InfluenceEventQuery Query(GameState s,AddToDistinctCountriesEffect d=null)=>InfluenceEventRules.Query(s,d??Effect(),InfluenceEventPolicy.NoActiveInterference);
        static InfluenceEventPlan Plan(GameState s,IEnumerable<CountryId> targets,PlayerSide owner=PlayerSide.US,long? version=null)
            =>InfluenceEventRules.Plan(s,Effect(),targets==null?null:new InfluenceEventSelection(owner,version??s.Version,targets),InfluenceEventPolicy.NoActiveInterference);
        [Test] public void Formal_profile_binds_only_influence_and_explicit_shortage()
        {
            var d=Load();Assert.That(d,Is.Not.Null);Assert.That(d.CardId,Is.EqualTo("card.marshall_plan"));Assert.That(d.Beneficiary,Is.EqualTo(PlayerSide.US));
            Assert.That(d.TargetCount,Is.EqualTo(7));Assert.That(d.Amount,Is.EqualTo(1));Assert.That(d.Regions,Is.EqualTo(new[]{"region.western_europe"}));
            Assert.That(d.ControlFilter,Is.EqualTo(InfluenceControlFilter.NotOpponentControlled));Assert.That(d.ShortagePolicy,Is.EqualTo(InfluenceCandidateShortagePolicy.SelectAllAvailable));
            Assert.That(PlayInfluenceEventTests.Ready().Practice.Definition.InfluenceEvents.Effects.ContainsKey(new CardId(d.CardId)),Is.False);
        }
        [TestCase("card")][TestCase("effect")][TestCase("beneficiary")][TestCase("region")][TestCase("shortage")][TestCase("zero")][TestCase("count")][TestCase("amount")][TestCase("float")][TestCase("overflow")]
        [TestCase("version")][TestCase("scope")][TestCase("extra")][TestCase("unverified")][TestCase("missing-ref")][TestCase("duplicate-ref")][TestCase("duplicate-source")][TestCase("source-url")][TestCase("fact-dataset")][TestCase("fact-pointer")]
        public void Invalid_profile_or_incomplete_evidence_rejects(string fault)
        {
            var r=JObject.Parse(Data());var p=r["parameters"]["marshall_plan"];
            switch(fault)
            {
                case "card":p["card_id"]="card.comecon";break;case "effect":p["effect_id"]="effect.comecon";break;case "beneficiary":p["beneficiary"]="USSR";break;
                case "region":p["target_region_id"]="region.eastern_europe";break;case "shortage":p["shortage_policy"]="RequiresRuling";break;case "zero":p["zero_candidates_outcome"]="Blocked";break;
                case "count":p["target_count"]=0;break;case "amount":p["amount_per_country"]=0;break;case "float":p["target_count"]=7.5;break;case "overflow":p["amount_per_country"]=long.MaxValue;break;
                case "version":r["content_version"]="99.0.0";break;case "scope":r["scope"]="fixture";break;case "extra":p["script"]="noop";break;
                case "unverified":p["source_refs"][0]["verification"]="Pending";break;case "missing-ref":((JArray)p["source_refs"]).RemoveAt(2);break;
                case "duplicate-ref":((JArray)p["source_refs"]).Add(p["source_refs"][0].DeepClone());break;case "duplicate-source":((JArray)r["source_catalog"]).Add(r["source_catalog"][0].DeepClone());break;
                case "source-url":r["source_catalog"][0]["url"]="https://example.invalid";break;
                case "fact-dataset":r["completion_policy"]["fact_policy_ref"]["dataset_id"]="other";break;case "fact-pointer":r["completion_policy"]["fact_policy_ref"]["json_pointer"]="/other";break;
            }
            Assert.That(()=>Load(r.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Loader_rejects_wrong_contract_missing_map_and_card()
        {
            Assert.That(()=>Load(schema:Schema().Replace("urn:coldwarheat:contract:alliance-prerequisites:1","wrong")),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>MarshallPlanInfluenceLoader.Load(Data(),Schema(),null,CardCatalogLoaderTests.Load()),Throws.InstanceOf<ArgumentException>());
            var cards=new CardCatalog(CardCatalogLoaderTests.Load().Cards.Where(c=>c.Id.Value!="card.marshall_plan"));Assert.That(()=>Load(cards:cards),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("affiliation")][TestCase("remove")][TestCase("reminder")][TestCase("effect")][TestCase("version")][TestCase("pointer")][TestCase("dataset")]
        public void Printed_binding_mismatch_rejects(string fault)
        {
            var all=CardCatalogLoaderTests.Load();var c=all.GetCard(new CardId("card.marshall_plan"));
            var binding=new CardEffectBinding(fault=="effect"?"effect.comecon":c.Effect.EffectId,fault=="version"?"99.0.0":c.Effect.ContractVersion,CardParameterBindingKind.Bound,
                new[]{new CardParameterReference(fault=="dataset"?"wrong.dataset":c.Effect.Parameters[0].DatasetId,fault=="pointer"?"/wrong":c.Effect.Parameters[0].JsonPointer)});
            var replacement=new CardDefinition(c.Id,c.PrintedNumber,c.NameKey,c.RulesTextKey,c.Kind,c.PrintedOps,fault=="affiliation"?CardAffiliation.USSR:c.Affiliation,c.Era,c.DeckEntry,
                fault=="remove"?false:c.RemoveAfterResolvedEvent,fault=="reminder"?false:c.DisplayPersistentReminder,c.ScoringRegion,binding);
            Assert.That(()=>Load(cards:new CardCatalog(all.Cards.Select(x=>x.Id.Equals(c.Id)?replacement:x))),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)]
        public void Explicit_policy_requires_all_available_up_to_seven(int legal)
        {
            var s=Board(legal);var q=Query(s);Assert.That(q.Error,Is.EqualTo(InfluenceEventError.None));Assert.That(q.RequiredTargets,Is.EqualTo(Math.Min(7,legal)));
            Assert.That(q.Owner,Is.EqualTo(PlayerSide.US));Assert.That(q.LegalCountries.Count,Is.EqualTo(legal));Assert.That(q.Status,Is.EqualTo(legal==0?InfluenceEventStatus.Ready:InfluenceEventStatus.AwaitingChoice));
            var p=Plan(s,legal==0?null:q.LegalCountries.Take(q.RequiredTargets));Assert.That(p.Resolved,Is.True,p.Error.ToString());
            Assert.That(p.Changes.Count,Is.EqualTo(Math.Min(7,legal)));Assert.That(p.NextState.Version,Is.EqualTo(s.Version+1));Assert.That(p.NextState.EventSequence,Is.EqualTo(s.EventSequence+p.Changes.Count+1));
            Assert.That(p.Status,Is.EqualTo(legal==0?InfluenceEventStatus.ResolvedNoChange:InfluenceEventStatus.ResolvedChange));
            Assert.That(p.NextState.Countries.All(x=>x.Value.USSR==s.Countries[x.Key].USSR),Is.True);Assert.That(p.NextState.Operations,Is.SameAs(s.Operations));Assert.That(p.NextState.Cards,Is.SameAs(s.Cards));Assert.That(p.NextState.RandomState,Is.SameAs(s.RandomState));
        }
        [Test] public void Western_membership_uses_current_control_not_influence_or_map_position()
        {
            var s=Board(100,edit:b=>{b[C("france")]=new CountryState(1,4);b[C("canada")]=new CountryState(2,1);});var q=Query(s);
            foreach(var n in new[]{"canada","turkey","austria","finland"})Assert.That(q.LegalCountries,Has.Member(C(n)));
            Assert.That(q.LegalCountries,Has.No.Member(C("france")));Assert.That(q.LegalCountries,Has.No.Member(C("poland")));
            Assert.That(q.LegalCountries.Select(c=>c.Value),Is.Ordered.Using<string>(StringComparer.Ordinal));
            var targets=q.LegalCountries.Take(7).ToArray();var a=Plan(s,targets);var b=Plan(s,targets.Reverse());
            Assert.That(a.Changes.Select(x=>x.Country),Is.EqualTo(b.Changes.Select(x=>x.Country)));
        }
        [Test] public void Zero_candidates_flow_completes_once_without_empty_response()
        {
            var s=Board(0,flow:true);var t=InfluenceEventFlow.Begin(s,PlayerSide.USSR);Assert.That(t.Accepted,Is.True,t.Error.ToString());
            Assert.That(t.DecisionRequestedSequence,Is.Null);Assert.That(t.Changes,Is.Empty);Assert.That(t.NextState.InfluenceEvent.Outcome,Is.EqualTo(InfluenceEventStatus.ResolvedNoChange));
            Assert.That(t.NextState.Version,Is.EqualTo(s.Version+1));Assert.That(InfluenceEventFlow.Begin(t.NextState,PlayerSide.USSR).Error,Is.EqualTo(RuleError.EventAlreadyResolved));
            Assert.That(Plan(s,Array.Empty<CountryId>()).Error,Is.EqualTo(InfluenceEventError.InvalidSelection));
        }
        [Test] public void Positive_shortage_waits_for_US_choice_and_preserves_identity_and_replay_guards()
        {
            var s=Board(3,flow:true);var begin=InfluenceEventFlow.Begin(s,PlayerSide.USSR);Assert.That(begin.Accepted,Is.True,begin.Error.ToString());var w=begin.NextState;
            Assert.That(w.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.US));var q=Query(w);var targets=q.LegalCountries.ToArray();var id=w.InfluenceEvent.DecisionId;
            Assert.That(InfluenceEventFlow.Resolve(w,PlayerSide.USSR,id,targets).Error,Is.EqualTo(RuleError.WrongActor));
            Assert.That(InfluenceEventFlow.Resolve(w,PlayerSide.US,"old",targets).Error,Is.EqualTo(RuleError.StaleDecision));
            var t=InfluenceEventFlow.Resolve(w,PlayerSide.US,id,targets);Assert.That(t.Accepted,Is.True);Assert.That(t.Changes.Count,Is.EqualTo(3));
            Assert.That(InfluenceEventFlow.Resolve(t.NextState,PlayerSide.US,id,targets).Error,Is.EqualTo(RuleError.EventAlreadyResolved));
        }
        [TestCase("few")][TestCase("many")][TestCase("duplicate")][TestCase("outside")][TestCase("enemy")][TestCase("owner")][TestCase("version")]
        public void Bad_selection_has_no_partial_result(string fault)
        {
            var s=Board(8);var q=Query(s);var targets=q.LegalCountries.Take(7).ToArray();var owner=PlayerSide.US;var version=s.Version;
            switch(fault){case "few":targets=targets.Take(6).ToArray();break;case "many":targets=q.LegalCountries.ToArray();break;case "duplicate":targets[6]=targets[0];break;case "outside":targets[6]=C("japan");break;case "enemy":targets[6]=s.Map.GetCountriesInRegion("region.western_europe").First(c=>!q.LegalCountries.Contains(c.Id)).Id;break;case "owner":owner=PlayerSide.USSR;break;case "version":version--;break;}
            var p=Plan(s,targets,owner,version);Assert.That(p.Resolved,Is.False);Assert.That(p.NextState,Is.Null);Assert.That(p.Changes,Is.Empty);
        }
        [TestCase("amount")][TestCase("version")][TestCase("sequence")][TestCase("zero-version")][TestCase("zero-sequence")]
        public void Overflow_does_not_publish_any_candidate(string fault)
        {
            var zero=fault.StartsWith("zero");var s=Board(zero?0:8,version:fault.Contains("version")?long.MaxValue:3,sequence:fault.Contains("sequence")?long.MaxValue-1:7,
                edit:b=>{if(fault=="amount")b[C("austria")]=new CountryState(int.MaxValue,1);});
            if(fault=="zero-sequence")s=Board(0,sequence:long.MaxValue);
            var q=Query(s);var p=Plan(s,zero?null:q.LegalCountries.Take(7));Assert.That(p.Error,Is.EqualTo(InfluenceEventError.NumericOverflow));Assert.That(p.NextState,Is.Null);Assert.That(p.Changes,Is.Empty);
        }
        [Test] public void Defaults_and_unknown_interference_never_inherit_Marshall_shortage()
        {
            var s=Board(0);var old=new AddToDistinctCountriesEffect("fixture.card","fixture.effect",PlayerSide.US,new[]{"region.western_europe"},7,1,InfluenceControlFilter.NotOpponentControlled);
            Assert.That(old.ShortagePolicy,Is.EqualTo(InfluenceCandidateShortagePolicy.RequiresRuling));Assert.That(Query(s,old).Error,Is.EqualTo(InfluenceEventError.CandidateShortage));
            Assert.That(InfluenceEventRules.Query(s,Effect(),InfluenceEventPolicy.Unknown).Error,Is.EqualTo(InfluenceEventError.InterferenceUnknown));
            Assert.That(InfluenceEventRules.Query(s,Effect(),InfluenceEventPolicy.Forbidden).Error,Is.EqualTo(InfluenceEventError.Forbidden));
            Assert.That(()=>Effect((InfluenceCandidateShortagePolicy)99),Throws.InstanceOf<ArgumentException>());
        }
    }
}
