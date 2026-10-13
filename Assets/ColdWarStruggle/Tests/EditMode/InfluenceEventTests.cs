using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ColdWarStruggle.Tests
{
    public class InfluenceEventTests
    {
        [Test] public void Unrepresentable_selection_definition_is_rejected_at_construction()
            => Assert.Throws<ArgumentOutOfRangeException>(()=>new AddToDistinctCountriesEffect("fixture","fixture",PlayerSide.USSR,new[]{"region.africa"},129,1));

        public static string Parameters => File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Cards/deluxe-2015/early-influence-batch1.parameters.json"));
        public static string Schema => File.ReadAllText(Path.Combine(DefinitionTests.Root,"Docs/Contracts/early-influence-parameters.schema.json"));
        static CountryId Id(string name) => new CountryId("country."+name);
        static GameState State(string country=null, int us=0, int ussr=0, long version=0, long sequence=0)
        {
            var map=DefinitionTests.World();var countries=map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));
            if(country!=null)countries[Id(country)]=new CountryState(us,ussr);
            // Zero OPS and no starting local influence intentionally cannot authorize ordinary placement.
            return new GameState(map,countries,new OperationsContext("event-fixture",PlayerSide.US,0,new CountryId[0],costs:new InfluenceCosts(1,2)),version:version,eventSequence:sequence);
        }
        static InfluenceEffectDefinition Definition(GameState state,string card) => EarlyInfluenceCatalogLoader.Load(Parameters,Schema,state.Map)["card."+card];
        static InfluenceEventPlan Plan(GameState state,string card,InfluenceEventSelection selection=null)
            => InfluenceEventRules.Plan(state,Definition(state,card),selection,InfluenceEventPolicy.NoActiveInterference);

        [TestCase("fidel","cuba",1,3)] [TestCase("fidel","cuba",8,8)]
        [TestCase("romanian_abdication","romania",0,3)]
        public void Clear_then_control_uses_map_stability_and_only_adds(string card,string country,int own,int expected)
        {
            var s=State(country,12,own);var p=Plan(s,card);
            Assert.That(p.Status,Is.EqualTo(InfluenceEventStatus.ResolvedChange));
            Assert.That(p.NextState.GetInfluence(Id(country),PlayerSide.US),Is.Zero);
            Assert.That(p.NextState.GetInfluence(Id(country),PlayerSide.USSR),Is.EqualTo(expected));
            Assert.That(s.GetInfluence(Id(country),PlayerSide.US),Is.EqualTo(12));
            Assert.That(p.NextState.SpentOps,Is.Zero);Assert.That(p.NextState.Operations,Is.SameAs(s.Operations));
            Assert.That(p.Changes[0].Side,Is.EqualTo(PlayerSide.US));
            Assert.That(p.NextState.Version,Is.EqualTo(1));Assert.That(p.ResolutionSequence,Is.EqualTo(p.Changes.Count+1));
        }
        [TestCase(0,0)] [TestCase(4,2)] [TestCase(5,2)] [TestCase(int.MaxValue,1073741823)]
        public void Nasser_removes_ceiling_of_old_US_value(int us,int remaining)
        {
            var s=State("egypt",us,1);var p=Plan(s,"nasser");
            Assert.That(p.Resolved,Is.True);Assert.That(p.NextState.GetInfluence(Id("egypt"),PlayerSide.US),Is.EqualTo(remaining));
            Assert.That(p.NextState.GetInfluence(Id("egypt"),PlayerSide.USSR),Is.EqualTo(3));
            Assert.That(p.Changes[0].Side,Is.EqualTo(PlayerSide.USSR));Assert.That(s.GetInfluence(Id("egypt"),PlayerSide.US),Is.EqualTo(us));
        }
        [Test] public void Truman_requires_neither_side_control_but_zero_USSR_is_legal()
        {
            var s=State("france",3,0);var d=Definition(s,"truman_doctrine");var q=InfluenceEventRules.Query(s,d,InfluenceEventPolicy.NoActiveInterference);
            Assert.That(q.Owner,Is.EqualTo(PlayerSide.US));Assert.That(q.RequiredTargets,Is.EqualTo(1));
            Assert.That(q.LegalCountries,Has.No.Member(Id("france")));Assert.That(q.LegalCountries,Has.Member(Id("canada")));
            Assert.That(q.LegalCountries,Has.No.Member(Id("japan")));
            var p=Plan(s,"truman_doctrine",new InfluenceEventSelection(PlayerSide.US,0,new[]{Id("canada")}));
            Assert.That(p.Status,Is.EqualTo(InfluenceEventStatus.ResolvedNoChange));Assert.That(p.Changes,Is.Empty);
            Assert.That(p.NextState.Version,Is.EqualTo(1));Assert.That(p.ResolutionSequence,Is.EqualTo(1));
            Assert.That(Plan(s,"truman_doctrine",new InfluenceEventSelection(PlayerSide.US,0,new[]{Id("france")})).Error,Is.EqualTo(InfluenceEventError.InvalidSelection));
        }
        [Test] public void Truman_with_no_uncontrolled_country_completes_without_waiting()
        {
            var s=State();var values=s.Countries.ToDictionary(x=>x.Key,x=>new CountryState(s.Map.GetCountry(x.Key).Stability,0));
            s=new GameState(s.Map,values,s.Operations);var p=Plan(s,"truman_doctrine");
            Assert.That(p.Status,Is.EqualTo(InfluenceEventStatus.ResolvedNoChange));Assert.That(p.NextState,Is.Not.Null);
        }
        [Test] public void Decolonization_uses_region_union_and_complete_distinct_selection_without_OPS()
        {
            var s=State("vietnam",9,0);var d=Definition(s,"decolonization");var q=InfluenceEventRules.Query(s,d,InfluenceEventPolicy.NoActiveInterference);
            Assert.That(q.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(q.LegalCountries.Count,Is.EqualTo(25));Assert.That(q.RequiredTargets,Is.EqualTo(4));
            Assert.That(q.LegalCountries,Has.Member(Id("vietnam")));Assert.That(q.LegalCountries,Has.No.Member(Id("pakistan")));
            var targets=new[]{Id("vietnam"),Id("angola"),Id("thailand"),Id("nigeria")};
            var choice=new InfluenceEventSelection(PlayerSide.USSR,0,targets);targets[0]=Id("france");
            var p=Plan(s,"decolonization",choice);Assert.That(p.Resolved,Is.True);
            Assert.That(p.NextState.GetInfluence(Id("vietnam"),PlayerSide.USSR),Is.EqualTo(1));Assert.That(p.NextState.GetInfluence(Id("vietnam"),PlayerSide.US),Is.EqualTo(9));
            Assert.That(p.NextState.SpentOps,Is.Zero);Assert.That(p.Changes.Count,Is.EqualTo(4));Assert.That(p.NextState.EventSequence,Is.EqualTo(5));
            Assert.That(s.GetInfluence(Id("vietnam"),PlayerSide.USSR),Is.Zero);
            Assert.Throws<NotSupportedException>(()=>((IList<CountryId>)choice.Targets).Clear());
            Assert.Throws<NotSupportedException>(()=>((IList<InfluenceChanged>)p.Changes).Clear());
        }
        [TestCase("short")] [TestCase("duplicate")] [TestCase("outside")] [TestCase("unknown")]
        [TestCase("owner")] [TestCase("stale")] [TestCase("nullTargets")]
        public void Invalid_complete_selection_never_produces_a_partial_plan(string kind)
        {
            var s=State();var targets=new[]{Id("angola"),Id("nigeria"),Id("thailand"),Id("vietnam")};var owner=PlayerSide.USSR;long version=0;
            if(kind=="short")targets=targets.Take(3).ToArray();
            if(kind=="duplicate")targets[3]=targets[0];if(kind=="outside")targets[3]=Id("france");if(kind=="unknown")targets[3]=Id("imaginary");
            if(kind=="owner")owner=PlayerSide.US;if(kind=="stale")version=1;if(kind=="nullTargets")targets=null;
            var p=Plan(s,"decolonization",new InfluenceEventSelection(owner,version,targets));
            Assert.That(p.Status,Is.EqualTo(InfluenceEventStatus.Rejected));Assert.That(p.NextState,Is.Null);Assert.That(p.Changes,Is.Empty);Assert.That(s.Version,Is.Zero);
            if(kind=="owner")Assert.That(p.Error,Is.EqualTo(InfluenceEventError.WrongDecisionOwner));
            if(kind=="stale")Assert.That(p.Error,Is.EqualTo(InfluenceEventError.StaleSelection));
        }
        [Test] public void Missing_choice_waits_without_changing_the_source_state()
        {
            var s=State();var p=Plan(s,"decolonization");Assert.That(p.Status,Is.EqualTo(InfluenceEventStatus.AwaitingChoice));
            Assert.That(p.NextState,Is.Null);Assert.That(s.Version,Is.Zero);
        }
        [TestCase(InfluenceEventPolicy.Unknown,InfluenceEventStatus.RequiresRuling)]
        [TestCase(InfluenceEventPolicy.Forbidden,InfluenceEventStatus.Blocked)]
        [TestCase((InfluenceEventPolicy)99,InfluenceEventStatus.RequiresRuling)]
        public void Interference_must_be_explicitly_supported(InfluenceEventPolicy policy,InfluenceEventStatus expected)
        {
            var s=State();var p=InfluenceEventRules.Plan(s,Definition(s,"fidel"),null,policy);
            Assert.That(p.Status,Is.EqualTo(expected));Assert.That(p.NextState,Is.Null);
        }
        [Test] public void Shortage_in_distinct_selection_requires_a_ruling_not_an_invented_minimum()
        {
            var s=State();var d=new AddToDistinctCountriesEffect("fixture","fixture",PlayerSide.USSR,new[]{"region.southeast_asia"},30,1);
            Assert.That(InfluenceEventRules.Query(s,d,InfluenceEventPolicy.NoActiveInterference).Error,Is.EqualTo(InfluenceEventError.CandidateShortage));
            Assert.That(InfluenceEventRules.Plan(s,d,null,InfluenceEventPolicy.NoActiveInterference).Status,Is.EqualTo(InfluenceEventStatus.RequiresRuling));
        }
        [TestCase("mutation")] [TestCase("version")] [TestCase("sequence")]
        public void Overflow_rolls_back_the_entire_event_plan(string where)
        {
            var s=State("nigeria",0,where=="mutation"?int.MaxValue:0,where=="version"?long.MaxValue:0,where=="sequence"?long.MaxValue:0);
            var p=Plan(s,"decolonization",new InfluenceEventSelection(PlayerSide.USSR,s.Version,new[]{Id("angola"),Id("nigeria"),Id("thailand"),Id("vietnam")}));
            Assert.That(p.Error,Is.EqualTo(InfluenceEventError.NumericOverflow));Assert.That(p.Changes,Is.Empty);Assert.That(p.NextState,Is.Null);
            Assert.That(s.GetInfluence(Id("angola"),PlayerSide.USSR),Is.Zero);
        }
        [Test] public void Fixed_effect_rejects_extra_selection_and_ended_game_rejects_event()
        {
            var s=State();Assert.That(Plan(s,"fidel",new InfluenceEventSelection(PlayerSide.USSR,0,new[]{Id("cuba")})).Error,Is.EqualTo(InfluenceEventError.InvalidSelection));
            var ended=new GameState(s.Map,s.Countries,s.Operations,ended:true);
            Assert.That(Plan(ended,"fidel").Error,Is.EqualTo(InfluenceEventError.GameAlreadyEnded));
        }
        [Test] public void Catalog_has_five_typed_read_only_definitions()
        {
            var c=EarlyInfluenceCatalogLoader.Load(Parameters,Schema,DefinitionTests.World());Assert.That(c.Count,Is.EqualTo(5));
            Assert.That(c["card.fidel"],Is.TypeOf<ClearThenControlEffect>());Assert.That(c["card.nasser"],Is.TypeOf<AddThenRemoveFractionEffect>());
            Assert.Throws<NotSupportedException>(()=>((IDictionary<string,InfluenceEffectDefinition>)c).Clear());
        }
        [TestCase("version")] [TestCase("unknownTemplate")] [TestCase("unknownCountry")] [TestCase("unknownRegion")]
        [TestCase("pendingSource")] [TestCase("wrongCard")] [TestCase("unknownField")] [TestCase("missingParameter")]
        public void Bad_parameter_catalog_is_rejected(string mutation)
        {
            var d=JObject.Parse(Parameters);var p=d["parameters"];
            switch(mutation)
            {
                case "version":d["content_version"]="99";break;
                case "unknownTemplate":p["fidel"]["template"]="ExecuteScript";break;
                case "unknownCountry":p["fidel"]["target_country_id"]="country.imaginary";break;
                case "unknownRegion":p["decolonization"]["target_region_ids"][0]="region.imaginary";break;
                case "pendingSource":p["fidel"]["source_refs"][0]["verification"]="Pending";break;
                case "wrongCard":p["fidel"]["card_id"]="card.nasser";break;
                case "unknownField":p["nasser"]["eval"]="anything";break;
                case "missingParameter":((JObject)p).Remove("nasser");break;
            }
            Assert.Throws<ArgumentException>(()=>EarlyInfluenceCatalogLoader.Load(d.ToString(),Schema,DefinitionTests.World()));
        }
    }
}
