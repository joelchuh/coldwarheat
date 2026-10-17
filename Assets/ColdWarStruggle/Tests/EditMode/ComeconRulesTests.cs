using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class ComeconRulesTests
    {
        internal static CountryId C(string n)=>InfluenceRelocationRulesTests.C(n);
        internal static AddToDistinctCountriesEffect Effect(PlayerSide side=PlayerSide.USSR,int count=4,int amount=1,InfluenceControlFilter filter=InfluenceControlFilter.NotOpponentControlled)
            =>new AddToDistinctCountriesEffect("card.comecon","effect.comecon",side,new[]{"region.eastern_europe"},count,amount,filter);
        internal static CountryId[] Targets()=>new[]{C("poland"),C("romania"),C("finland"),C("austria")};
        static InfluenceEventQuery Query(GameState s,AddToDistinctCountriesEffect d=null)=>InfluenceEventRules.Query(s,d??Effect(),InfluenceEventPolicy.NoActiveInterference);
        static InfluenceEventPlan Plan(GameState s,IEnumerable<CountryId> targets=null,PlayerSide owner=PlayerSide.USSR,long? version=null,AddToDistinctCountriesEffect effect=null)
            =>InfluenceEventRules.Plan(s,effect??Effect(),new InfluenceEventSelection(owner,version??s.Version,targets??Targets()),InfluenceEventPolicy.NoActiveInterference);
        [Test] public void Eligible_query_uses_control_not_presence_and_includes_both_subregions()
        {
            var map=DefinitionTests.World();var stability=map.Countries.Single(c=>c.Id.Equals(C("hungary"))).Stability;
            var s=InfluenceRelocationRulesTests.State(b=>{b[C("hungary")]=new CountryState(stability,0);b[C("finland")]=new CountryState(2,1);});var q=Query(s);
            Assert.That(q.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(q.RequiredTargets,Is.EqualTo(4));Assert.That(q.Kind,Is.EqualTo(InfluenceDecisionKind.CountrySet));
            Assert.That(q.LegalCountries,Has.No.Member(C("hungary")));Assert.That(q.LegalCountries,Has.No.Member(C("west_germany")));Assert.That(q.LegalCountries,Has.Member(C("austria")));Assert.That(q.LegalCountries,Has.Member(C("finland")));Assert.That(q.LegalCountries,Has.Member(C("poland")));
            Assert.That(q.LegalCountries.Select(c=>c.Value),Is.Ordered.Using<string>(StringComparer.Ordinal));Assert.Throws<NotSupportedException>(()=>((IList<CountryId>)q.LegalCountries).Clear());
        }
        [Test] public void Control_filter_is_relative_to_beneficiary_not_hardcoded_US()
        {var s=InfluenceRelocationRulesTests.State(b=>b[C("hungary")]=new CountryState(9,0));var q=Query(s,Effect(PlayerSide.US));Assert.That(q.LegalCountries,Has.No.Member(C("poland")));Assert.That(q.LegalCountries,Has.Member(C("hungary")));}
        [Test] public void Default_any_keeps_opponent_controlled_candidates_and_selection_order()
        {
            var s=InfluenceRelocationRulesTests.State(b=>b[C("poland")]=new CountryState(10,0));var d=new AddToDistinctCountriesEffect("card.comecon","effect.comecon",PlayerSide.USSR,new[]{"region.eastern_europe"},4,1);
            Assert.That(d.ControlFilter,Is.EqualTo(InfluenceControlFilter.Any));Assert.That(Query(s,d).LegalCountries,Has.Member(C("poland")));Assert.That(Plan(s,Targets(),effect:d).Changes.Select(x=>x.Country),Is.EqualTo(Targets()));
        }
        [Test] public void Complete_filtered_placement_changes_only_chosen_Soviet_influence_without_OPS()
        {
            var s=InfluenceRelocationRulesTests.State();var p=Plan(s);Assert.That(p.Resolved,Is.True,p.Error.ToString());var n=p.NextState;
            Assert.That(n.Countries.Sum(x=>(long)x.Value.USSR),Is.EqualTo(s.Countries.Sum(x=>(long)x.Value.USSR)+4));Assert.That(n.Countries.All(x=>x.Value.US==s.Countries[x.Key].US),Is.True);
            foreach(var c in Targets())Assert.That(n.GetInfluence(c,PlayerSide.USSR),Is.EqualTo(s.GetInfluence(c,PlayerSide.USSR)+1));
            Assert.That(n.Operations,Is.SameAs(s.Operations));Assert.That(n.SpentOps,Is.Zero);Assert.That(n.Cards,Is.SameAs(s.Cards));Assert.That(n.RandomState,Is.SameAs(s.RandomState));Assert.That(n.Practice,Is.SameAs(s.Practice));Assert.That(s.GetInfluence(C("poland"),PlayerSide.USSR),Is.EqualTo(4));
        }
        [Test] public void Filtered_input_order_does_not_change_receipt_sequence()
        {
            var s=InfluenceRelocationRulesTests.State();var a=Plan(s,Targets());var b=Plan(s,Targets().Reverse());string Row(InfluenceChanged x)=>x.Country.Value+":"+x.After+":"+x.Sequence;
            Assert.That(a.Changes.Select(Row),Is.EqualTo(b.Changes.Select(Row)));Assert.That(a.Changes.Select(x=>x.Country.Value),Is.Ordered.Using<string>(StringComparer.Ordinal));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)] public void Insufficient_candidates_require_ruling_without_pending_impossible_selection(int count)
        {
            var map=DefinitionTests.World();var east=map.GetCountriesInRegion("region.eastern_europe").OrderBy(c=>c.Id.Value,StringComparer.Ordinal).ToArray();
            var s=InfluenceRelocationRulesTests.State(b=>{for(int i=0;i<east.Length;i++)b[east[i].Id]=i<count?new CountryState(0,0):new CountryState(east[i].Stability,0);});
            var q=Query(s);Assert.That(q.Status,Is.EqualTo(InfluenceEventStatus.RequiresRuling));Assert.That(q.Error,Is.EqualTo(InfluenceEventError.CandidateShortage));Assert.That(q.RequiredTargets,Is.Zero);
            var p=InfluenceEventRules.Plan(s,Effect(),null,InfluenceEventPolicy.NoActiveInterference);Assert.That(p.Status,Is.EqualTo(InfluenceEventStatus.RequiresRuling));Assert.That(p.NextState,Is.Null);Assert.That(p.Changes,Is.Empty);
        }
        [TestCase("enemy")][TestCase("duplicate")][TestCase("few")][TestCase("many")][TestCase("outside")][TestCase("unknown")][TestCase("owner")][TestCase("version")]
        public void Invalid_selection_does_not_commit_any_earlier_placement(string fault)
        {
            var s=InfluenceRelocationRulesTests.State(b=>b[C("hungary")]=new CountryState(10,0));var t=Targets();var owner=PlayerSide.USSR;var version=s.Version;
            switch(fault){case "enemy":t[3]=C("hungary");break;case "duplicate":t[3]=t[0];break;case "few":t=t.Take(3).ToArray();break;case "many":t=t.Append(C("bulgaria")).ToArray();break;case "outside":t[3]=C("west_germany");break;case "unknown":t[3]=C("unknown");break;case "owner":owner=PlayerSide.US;break;case "version":version--;break;}
            var p=Plan(s,t,owner,version);Assert.That(p.Resolved,Is.False);Assert.That(p.NextState,Is.Null);Assert.That(p.Changes,Is.Empty);Assert.That(s.GetInfluence(C("poland"),PlayerSide.USSR),Is.EqualTo(4));
        }
        [TestCase("amount")][TestCase("version")][TestCase("sequence")] public void Late_arithmetic_failure_discards_full_working_copy(string fault)
        {
            var s=InfluenceRelocationRulesTests.State(b=>{if(fault=="amount")b[C("romania")]=new CountryState(0,int.MaxValue);},version:fault=="version"?long.MaxValue:3,sequence:fault=="sequence"?long.MaxValue-1:7);
            var p=Plan(s);Assert.That(p.Error,Is.EqualTo(InfluenceEventError.NumericOverflow));Assert.That(p.NextState,Is.Null);Assert.That(p.Changes,Is.Empty);Assert.That(s.GetInfluence(C("poland"),PlayerSide.USSR),Is.EqualTo(4));
        }
        [Test] public void Explicit_parameters_are_reused_and_duplicate_regions_are_rejected()
        {
            var s=InfluenceRelocationRulesTests.State();var p=Plan(s,new[]{C("poland"),C("romania")},effect:Effect(count:2,amount:3));Assert.That(p.Resolved,Is.True);Assert.That(p.NextState.GetInfluence(C("poland"),PlayerSide.USSR),Is.EqualTo(7));
            Assert.Throws<ArgumentException>(()=>new AddToDistinctCountriesEffect("card.comecon","effect.comecon",PlayerSide.USSR,new[]{"region.europe","region.europe"},4,1,InfluenceControlFilter.NotOpponentControlled));
            Assert.Throws<ArgumentOutOfRangeException>(()=>Effect(filter:(InfluenceControlFilter)99));
        }
        [Test] public void Overlapping_regions_produce_one_candidate_per_country()
        {var s=InfluenceRelocationRulesTests.State();var d=new AddToDistinctCountriesEffect("card.comecon","effect.comecon",PlayerSide.USSR,new[]{"region.europe","region.eastern_europe"},4,1,InfluenceControlFilter.NotOpponentControlled);var q=Query(s,d);Assert.That(q.LegalCountries.Count,Is.EqualTo(q.LegalCountries.Distinct().Count()));}
        [Test] public void Shared_interference_and_end_guards_remain_before_filter_dispatch()
        {var s=InfluenceRelocationRulesTests.State();Assert.That(InfluenceEventRules.Query(s,Effect(),InfluenceEventPolicy.Unknown).Status,Is.EqualTo(InfluenceEventStatus.RequiresRuling));Assert.That(InfluenceEventRules.Query(s,Effect(),InfluenceEventPolicy.Forbidden).Status,Is.EqualTo(InfluenceEventStatus.Blocked));Assert.That(Query(InfluenceRelocationRulesTests.State(ended:true)).Error,Is.EqualTo(InfluenceEventError.GameAlreadyEnded));}
    }
}
