using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class InfluenceRelocationRulesTests
    {
        internal static CountryId C(string name)=>new CountryId("country."+name);
        internal static InfluenceAllocation A(string name,int amount)=>new InfluenceAllocation(C(name),amount);
        internal static RelocateOwnInfluenceEffect Effect(int maximum=4,int cap=2)=>new RelocateOwnInfluenceEffect("card.de_stalinization","effect.de_stalinization",PlayerSide.USSR,maximum,cap);
        internal static GameState State(Action<Dictionary<CountryId,CountryState>> edit=null,long version=3,long sequence=7,bool ended=false)
        {
            var map=DefinitionTests.World();var countries=map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));
            countries[C("poland")]=new CountryState(0,4);countries[C("romania")]=new CountryState(1,2);edit?.Invoke(countries);
            return new GameState(map,countries,new OperationsContext("relocation-fixture",PlayerSide.US,0,Array.Empty<CountryId>(),costs:new InfluenceCosts(1,2)),version:version,eventSequence:sequence,ended:ended);
        }
        static InfluenceEventPlan Plan(GameState s,IEnumerable<InfluenceAllocation> sources,IEnumerable<InfluenceAllocation> targets,PlayerSide owner=PlayerSide.USSR,long? version=null,RelocateOwnInfluenceEffect effect=null)
            =>InfluenceEventRules.Plan(s,effect??Effect(),InfluenceEventSelection.Relocate(owner,version??s.Version,sources,targets),InfluenceEventPolicy.NoActiveInterference);
        static void Unchanged(GameState s,InfluenceEventPlan plan)
        {Assert.That(plan.Resolved,Is.False);Assert.That(plan.NextState,Is.Null);Assert.That(plan.Changes,Is.Empty);Assert.That(s.Countries[C("poland")].USSR,Is.EqualTo(4));}
        [TestCase(0)][TestCase(1)][TestCase(4)]
        public void Complete_allocation_conserves_influence_and_does_not_require_OPS_access(int amount)
        {
            var s=State();var source=amount==0?Array.Empty<InfluenceAllocation>():new[]{A("poland",amount)};
            var targets=amount==0?Array.Empty<InfluenceAllocation>():amount==1?new[]{A("india",1)}:new[]{A("india",2),A("angola",2)};
            var p=Plan(s,source,targets);Assert.That(p.Resolved,Is.True,p.Error.ToString());
            Assert.That(p.NextState.Countries.Sum(x=>(long)x.Value.USSR),Is.EqualTo(s.Countries.Sum(x=>(long)x.Value.USSR)));
            Assert.That(p.NextState.Countries.All(x=>x.Value.US==s.Countries[x.Key].US),Is.True);
            Assert.That(s.Countries[C("poland")].USSR,Is.EqualTo(4));Assert.That(p.NextState.Countries[C("poland")].USSR,Is.EqualTo(4-amount));
            Assert.That(p.NextState.Operations,Is.SameAs(s.Operations));Assert.That(p.NextState.SpentOps,Is.Zero);
            Assert.That(p.NextState.Cards,Is.SameAs(s.Cards));Assert.That(p.NextState.RandomState,Is.SameAs(s.RandomState));Assert.That(p.NextState.Practice,Is.SameAs(s.Practice));
            Assert.That(p.NextState.Version,Is.EqualTo(s.Version+1));Assert.That(p.ResolutionSequence,Is.EqualTo(s.EventSequence+p.Changes.Count+1));
            Assert.That(p.Status,Is.EqualTo(amount==0?InfluenceEventStatus.ResolvedNoChange:InfluenceEventStatus.ResolvedChange));
        }
        [Test] public void Query_uses_current_own_influence_and_opponent_control_with_stable_order()
        {
            var s=State(b=>{b[C("poland")]=new CountryState(10,4);b[C("cuba")]=new CountryState(3,0);});
            var q=InfluenceEventRules.Query(s,Effect(),InfluenceEventPolicy.NoActiveInterference);
            Assert.That(q.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(q.MaximumAmount,Is.EqualTo(4));Assert.That(q.Kind,Is.EqualTo(InfluenceDecisionKind.Relocation));
            Assert.That(q.SourceCapacities.Select(a=>a.Country),Has.Member(C("poland")));Assert.That(q.SourceCapacities.Single(a=>a.Country.Equals(C("romania"))).Amount,Is.EqualTo(2));
            Assert.That(q.DestinationCapacities.Select(a=>a.Country),Has.No.Member(C("poland")));Assert.That(q.DestinationCapacities.Select(a=>a.Country),Has.No.Member(C("cuba")));
            Assert.That(q.DestinationCapacities.Single(a=>a.Country.Equals(C("india"))).Amount,Is.EqualTo(2));
            Assert.That(q.SourceCapacities.Select(a=>a.Country.Value),Is.Ordered.Using<string>(StringComparer.Ordinal));Assert.That(q.DestinationCapacities.Select(a=>a.Country.Value),Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.Throws<NotSupportedException>(()=>((IList<InfluenceAllocation>)q.SourceCapacities).Clear());Assert.Throws<NotSupportedException>(()=>((IList<InfluenceAllocation>)q.DestinationCapacities).Clear());
            var p=Plan(s,new[]{A("poland",2)},new[]{A("india",2)});Assert.That(p.Resolved,Is.True);Assert.That(p.NextState.Countries[C("poland")].US,Is.EqualTo(10));
        }
        [Test] public void Multiple_sources_and_destination_order_produce_same_sorted_changes()
        {
            var s=State();var from=new[]{A("romania",2),A("poland",2)};var to=new[]{A("india",2),A("angola",2)};
            var p=Plan(s,from,to);var reversed=Plan(s,from.Reverse(),to.Reverse());Assert.That(p.Resolved,Is.True);
            string Describe(InfluenceChanged x)=>x.Country.Value+":"+x.Before+":"+x.After+":"+x.Sequence;
            Assert.That(p.Changes.Select(Describe),Is.EqualTo(reversed.Changes.Select(Describe)));
            Assert.That(p.Changes.Select(x=>x.Country),Is.EqualTo(new[]{C("poland"),C("romania"),C("angola"),C("india")}));
            Assert.That(p.Changes.All(x=>x.Side==PlayerSide.USSR),Is.True);Assert.Throws<NotSupportedException>(()=>((IList<InfluenceChanged>)p.Changes).Clear());
        }
        [Test] public void Selection_copies_both_lists_and_caps_enumeration()
        {
            var s=State();var from=new[]{A("poland",2)};var to=new[]{A("india",2)};var choice=InfluenceEventSelection.Relocate(PlayerSide.USSR,s.Version,from,to);
            from[0]=A("poland",99);to[0]=A("cuba",99);var p=InfluenceEventRules.Plan(s,Effect(),choice,InfluenceEventPolicy.NoActiveInterference);Assert.That(p.Resolved,Is.True);
            Assert.Throws<NotSupportedException>(()=>((IList<InfluenceAllocation>)choice.Sources).Clear());Assert.Throws<NotSupportedException>(()=>((IList<InfluenceAllocation>)choice.Destinations).Clear());
            IEnumerable<InfluenceAllocation> Infinite(){while(true)yield return A("poland",1);}
            var bounded=InfluenceEventSelection.Relocate(PlayerSide.USSR,s.Version,Infinite(),Infinite());Assert.That(bounded.Sources.Count,Is.EqualTo(InfluenceEventSelection.MaxTargets+1));Assert.That(bounded.Destinations.Count,Is.EqualTo(InfluenceEventSelection.MaxTargets+1));
        }
        [TestCase("null-source")][TestCase("null-destination")][TestCase("unbalanced")][TestCase("over-total")]
        [TestCase("over-source")][TestCase("over-country")][TestCase("duplicate-source")][TestCase("duplicate-destination")]
        [TestCase("zero-source")][TestCase("zero-destination")][TestCase("negative-source")][TestCase("negative-destination")]
        [TestCase("unknown-source")][TestCase("unknown-destination")][TestCase("us-controlled-destination")]
        [TestCase("empty-source-only")][TestCase("empty-destination-only")][TestCase("too-many-source")][TestCase("too-many-destination")]
        [TestCase("huge-source")][TestCase("huge-destination")]
        public void Invalid_allocations_never_commit_partial_removal(string fault)
        {
            var s=State(b=>b[C("cuba")]=new CountryState(3,0));IEnumerable<InfluenceAllocation> from=new[]{A("poland",2)},to=new[]{A("india",2)};
            switch(fault)
            {
                case "null-source":from=null;break;case "null-destination":to=null;break;
                case "unbalanced":to=new[]{A("india",1)};break;
                case "over-total":from=new[]{A("poland",4),A("romania",1)};to=new[]{A("india",2),A("angola",2),A("japan",1)};break;
                case "over-source":from=new[]{A("romania",3)};to=new[]{A("india",2),A("angola",1)};break;
                case "over-country":from=new[]{A("poland",3)};to=new[]{A("india",3)};break;
                case "duplicate-source":from=new[]{A("poland",1),A("poland",1)};break;
                case "duplicate-destination":to=new[]{A("india",1),A("india",1)};break;
                case "zero-source":from=new[]{A("poland",0)};break;case "zero-destination":to=new[]{A("india",0)};break;
                case "negative-source":from=new[]{A("poland",-2)};break;case "negative-destination":to=new[]{A("india",-2)};break;
                case "unknown-source":from=new[]{A("imaginary",2)};break;case "unknown-destination":to=new[]{A("imaginary",2)};break;
                case "us-controlled-destination":to=new[]{A("cuba",2)};break;
                case "empty-source-only":from=Array.Empty<InfluenceAllocation>();break;case "empty-destination-only":to=Array.Empty<InfluenceAllocation>();break;
                case "too-many-source":from=Enumerable.Repeat(A("poland",1),129);break;case "too-many-destination":to=Enumerable.Repeat(A("india",1),129);break;
                case "huge-source":from=new[]{A("poland",int.MaxValue),A("romania",int.MaxValue)};break;
                case "huge-destination":to=new[]{A("india",int.MaxValue),A("angola",int.MaxValue)};break;
            }
            var p=Plan(s,from,to);Unchanged(s,p);Assert.That(p.Error,Is.EqualTo(InfluenceEventError.InvalidSelection));
        }
        [Test] public void Overlap_requires_ruling_and_cannot_be_netted_to_zero()
        {
            var s=State();var p=Plan(s,new[]{A("poland",2)},new[]{A("poland",2)});Unchanged(s,p);Assert.That(p.Status,Is.EqualTo(InfluenceEventStatus.RequiresRuling));Assert.That(p.Error,Is.EqualTo(InfluenceEventError.RelocationOverlap));
        }
        [TestCase(PlayerSide.US)][TestCase((PlayerSide)99)] public void Wrong_owner_is_rejected_even_for_zero_choice(PlayerSide owner)
        {var s=State();var p=Plan(s,Array.Empty<InfluenceAllocation>(),Array.Empty<InfluenceAllocation>(),owner);Unchanged(s,p);Assert.That(p.Error,Is.EqualTo(InfluenceEventError.WrongDecisionOwner));}
        [Test] public void Stale_selection_and_wrong_decision_shape_are_rejected()
        {
            var s=State();var p=Plan(s,new[]{A("poland",1)},new[]{A("india",1)},version:s.Version-1);Unchanged(s,p);Assert.That(p.Error,Is.EqualTo(InfluenceEventError.StaleSelection));
            foreach(var choice in new[]{new InfluenceEventSelection(PlayerSide.USSR,s.Version,new[]{C("india")}),InfluenceEventSelection.Allocate(PlayerSide.USSR,s.Version,new[]{A("india",1)})})
            {p=InfluenceEventRules.Plan(s,Effect(),choice,InfluenceEventPolicy.NoActiveInterference);Unchanged(s,p);Assert.That(p.Error,Is.EqualTo(InfluenceEventError.InvalidSelection));}
        }
        [TestCase(InfluenceEventPolicy.Unknown,InfluenceEventError.InterferenceUnknown)][TestCase(InfluenceEventPolicy.Forbidden,InfluenceEventError.Forbidden)]
        public void Existing_interference_gate_precedes_relocation(InfluenceEventPolicy policy,InfluenceEventError error)
        {var s=State();var p=InfluenceEventRules.Plan(s,Effect(),null,policy);Unchanged(s,p);Assert.That(p.Error,Is.EqualTo(error));}
        [Test] public void Ended_game_cannot_start_relocation()
        {var s=State(ended:true);var p=InfluenceEventRules.Plan(s,Effect(),null,InfluenceEventPolicy.NoActiveInterference);Unchanged(s,p);Assert.That(p.Error,Is.EqualTo(InfluenceEventError.GameAlreadyEnded));}
        [Test] public void No_source_auto_completes_without_requiring_a_fabricated_move()
        {
            var s=State(b=>{b[C("poland")]=new CountryState(0,0);b[C("romania")]=new CountryState(1,0);});
            var q=InfluenceEventRules.Query(s,Effect(),InfluenceEventPolicy.NoActiveInterference);Assert.That(q.Status,Is.EqualTo(InfluenceEventStatus.Ready));Assert.That(q.SourceCapacities,Is.Empty);
            var p=InfluenceEventRules.Plan(s,Effect(),null,InfluenceEventPolicy.NoActiveInterference);Assert.That(p.Status,Is.EqualTo(InfluenceEventStatus.ResolvedNoChange));Assert.That(p.Changes,Is.Empty);
        }
        [Test] public void Nonempty_sources_require_an_explicit_complete_choice()
        {var p=InfluenceEventRules.Plan(State(),Effect(),null,InfluenceEventPolicy.NoActiveInterference);Assert.That(p.Status,Is.EqualTo(InfluenceEventStatus.AwaitingChoice));Assert.That(p.NextState,Is.Null);}
        [TestCase("destination")][TestCase("version")][TestCase("sequence")]
        public void Any_overflow_discards_the_whole_work_copy(string where)
        {
            var s=State(b=>{if(where=="destination")b[C("india")]=new CountryState(0,int.MaxValue);},version:where=="version"?long.MaxValue:3,sequence:where=="sequence"?long.MaxValue-1:7);
            var p=Plan(s,new[]{A("poland",2)},new[]{A("india",2)});Unchanged(s,p);Assert.That(p.Error,Is.EqualTo(InfluenceEventError.NumericOverflow));
        }
        [Test] public void Reusable_algorithm_reads_definition_caps_instead_of_card_literals()
        {
            var s=State(b=>b[C("poland")]=new CountryState(0,7));var effect=Effect(7,3);
            var p=Plan(s,new[]{A("poland",7)},new[]{A("india",3),A("angola",3),A("japan",1)},effect:effect);
            Assert.That(p.Resolved,Is.True);Assert.That(p.NextState.Countries[C("poland")].USSR,Is.Zero);Assert.That(p.NextState.Countries[C("india")].USSR,Is.EqualTo(3));
        }
        [TestCase(0,2)][TestCase(4,0)][TestCase(-1,2)] public void Invalid_definition_caps_are_rejected(int total,int cap)
            =>Assert.Throws<ArgumentOutOfRangeException>(()=>Effect(total,cap));
    }
}
