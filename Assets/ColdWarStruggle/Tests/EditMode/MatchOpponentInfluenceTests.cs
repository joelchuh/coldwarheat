using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class MatchOpponentInfluenceTests
    {
        static CountryId C(string n)=>new CountryId("country."+n);
        static MatchOpponentInfluenceEffect Definition()=>new MatchOpponentInfluenceEffect("card.independent_reds","effect.independent_reds",PlayerSide.US,
            new[]{C("yugoslavia"),C("romania"),C("bulgaria"),C("hungary"),C("czechoslovakia")});
        static GameState State(int us=0,int ussr=0,long version=0,long sequence=0)
        {
            var map=DefinitionTests.World();var countries=map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));
            countries[C("romania")]=new CountryState(us,ussr);
            return new GameState(map,countries,new OperationsContext("match-fixture",PlayerSide.USSR,0,Array.Empty<CountryId>(),costs:new InfluenceCosts(1,2)),version:version,eventSequence:sequence);
        }
        [Test] public void Candidate_list_includes_enemy_control_and_zero_change_countries_but_no_other_eastern_countries()
        {
            var s=State(0,8);var q=InfluenceEventRules.Query(s,Definition(),InfluenceEventPolicy.NoActiveInterference);
            Assert.That(q.Status,Is.EqualTo(InfluenceEventStatus.AwaitingChoice));Assert.That(q.Owner,Is.EqualTo(PlayerSide.US));Assert.That(q.RequiredTargets,Is.EqualTo(1));
            Assert.That(q.LegalCountries,Is.EquivalentTo(Definition().Countries));Assert.That(q.LegalCountries,Has.No.Member(C("poland")));
            Assert.That(q.LegalCountries.Select(c=>c.Value),Is.Ordered.Using<string>(StringComparer.Ordinal));
        }
        [TestCase(1,4,4,1)][TestCase(6,2,6,0)][TestCase(0,0,0,0)][TestCase(3,3,3,0)][TestCase(0,int.MaxValue,int.MaxValue,1)]
        public void Adds_only_positive_difference_preserves_opponent_and_does_not_spend_OPS(int us,int ussr,int expected,int changes)
        {
            var s=State(us,ussr);var p=InfluenceEventRules.Plan(s,Definition(),new InfluenceEventSelection(PlayerSide.US,0,new[]{C("romania")}),InfluenceEventPolicy.NoActiveInterference);
            Assert.That(p.Resolved,Is.True);Assert.That(p.NextState.GetInfluence(C("romania"),PlayerSide.US),Is.EqualTo(expected));Assert.That(p.NextState.GetInfluence(C("romania"),PlayerSide.USSR),Is.EqualTo(ussr));
            Assert.That(p.Status,Is.EqualTo(changes==0?InfluenceEventStatus.ResolvedNoChange:InfluenceEventStatus.ResolvedChange));Assert.That(p.Changes.Count,Is.EqualTo(changes));
            Assert.That(p.NextState.Version,Is.EqualTo(1));Assert.That(p.NextState.EventSequence,Is.EqualTo(changes+1));Assert.That(p.NextState.Operations,Is.SameAs(s.Operations));Assert.That(p.NextState.SpentOps,Is.Zero);
            Assert.That(s.GetInfluence(C("romania"),PlayerSide.US),Is.EqualTo(us));
        }
        [TestCase("wrong-owner",InfluenceEventError.WrongDecisionOwner)][TestCase("stale",InfluenceEventError.StaleSelection)]
        [TestCase("outside",InfluenceEventError.InvalidSelection)][TestCase("duplicate",InfluenceEventError.InvalidSelection)][TestCase("empty",InfluenceEventError.InvalidSelection)]
        public void Invalid_selection_is_atomic(string kind,InfluenceEventError error)
        {
            var targets=new[]{C("romania")};if(kind=="outside")targets=new[]{C("poland")};if(kind=="duplicate")targets=new[]{C("romania"),C("romania")};if(kind=="empty")targets=Array.Empty<CountryId>();
            var s=State(1,4);var p=InfluenceEventRules.Plan(s,Definition(),new InfluenceEventSelection(kind=="wrong-owner"?PlayerSide.USSR:PlayerSide.US,kind=="stale"?1:0,targets),InfluenceEventPolicy.NoActiveInterference);
            Assert.That(p.Error,Is.EqualTo(error));Assert.That(p.NextState,Is.Null);Assert.That(p.Changes,Is.Empty);Assert.That(s.GetInfluence(C("romania"),PlayerSide.US),Is.EqualTo(1));
        }
        [Test] public void Waiting_and_unknown_interference_produce_no_next_state()
        {
            var s=State(1,4);var d=Definition();var waiting=InfluenceEventRules.Plan(s,d,null,InfluenceEventPolicy.NoActiveInterference);
            Assert.That(waiting.Status,Is.EqualTo(InfluenceEventStatus.AwaitingChoice));Assert.That(waiting.NextState,Is.Null);
            Assert.That(InfluenceEventRules.Plan(s,d,null,InfluenceEventPolicy.Unknown).Status,Is.EqualTo(InfluenceEventStatus.RequiresRuling));
        }
        [TestCase(long.MaxValue,0)][TestCase(0,long.MaxValue-1)]
        public void Version_or_resolution_sequence_overflow_rejects_whole_plan(long version,long sequence)
        {
            var s=State(1,4,version,sequence);var p=InfluenceEventRules.Plan(s,Definition(),new InfluenceEventSelection(PlayerSide.US,version,new[]{C("romania")}),InfluenceEventPolicy.NoActiveInterference);
            Assert.That(p.Error,Is.EqualTo(InfluenceEventError.NumericOverflow));Assert.That(p.NextState,Is.Null);Assert.That(p.Changes,Is.Empty);Assert.That(s.GetInfluence(C("romania"),PlayerSide.US),Is.EqualTo(1));
        }
        [Test] public void Definition_copies_its_bounded_unique_country_list()
        {
            var values=new[]{C("romania")};var d=new MatchOpponentInfluenceEffect("c","e",PlayerSide.US,values);values[0]=C("poland");
            Assert.That(d.Countries,Is.EqualTo(new[]{C("romania")}));Assert.Throws<NotSupportedException>(()=>((IList<CountryId>)d.Countries).Clear());
            Assert.Throws<ArgumentException>(()=>new MatchOpponentInfluenceEffect("c","e",PlayerSide.US,null));
            Assert.Throws<ArgumentException>(()=>new MatchOpponentInfluenceEffect("c","e",PlayerSide.US,Array.Empty<CountryId>()));
            Assert.Throws<ArgumentException>(()=>new MatchOpponentInfluenceEffect("c","e",PlayerSide.US,new[]{C("romania"),C("romania")}));
            Assert.Throws<ArgumentException>(()=>new MatchOpponentInfluenceEffect("c","e",PlayerSide.US,new[]{default(CountryId)}));
        }
        [Test] public void Unknown_map_country_rejects_instead_of_silently_shrinking_candidates()
        {
            var d=new MatchOpponentInfluenceEffect("c","e",PlayerSide.US,new[]{C("romania"),C("imaginary")});
            Assert.That(InfluenceEventRules.Query(State(),d,InfluenceEventPolicy.NoActiveInterference).Error,Is.EqualTo(InfluenceEventError.InvalidDefinition));
        }
    }
}
