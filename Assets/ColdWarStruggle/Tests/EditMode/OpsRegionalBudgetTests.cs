using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class OpsRegionalBudgetTests
    {
        internal static CountryId C(string id)=>new CountryId("country."+id);
        internal static OpsGeneralQuote Quote(int basis=4,int delta=0,OpsValuePurpose purpose=OpsValuePurpose.Influence)=>OpsGeneralRules.QuoteCard(OpsGeneralTests.Definition(),OpsGeneralTests.Card(basis),PlayerSide.US,purpose,new[]{new OpsFlatModifier("flat",PlayerSide.US,delta)});
        internal static OpsRegionalBudget Budget(MapDefinition map=null,int basis=4,int delta=0,OpsValuePurpose purpose=OpsValuePurpose.Influence)=>new OpsRegionalBudget(map??DefinitionTests.World(),Quote(basis,delta,purpose),new[]{new OpsRegionalBonus("china","region.asia",1)});
        [TestCase("japan",5)] [TestCase("thailand",5)] [TestCase("vietnam",5)] [TestCase("iran",4)] [TestCase("france",4)]
        public void Candidate_budget_uses_rule_regions_including_subregions(string country,int expected)
        {var b=Budget();Assert.That(b.EffectiveBudget,Is.EqualTo(5));Assert.That(b.HasTargets,Is.False);Assert.That(b.BudgetFor(C(country)),Is.EqualTo(expected));Assert.That(b.Spent,Is.Zero);}
        [TestCase(1,-1,1)] [TestCase(4,-1,4)] [TestCase(4,1,5)] [TestCase(1,-10,1)]
        public void Regional_bonus_is_combined_before_floor_and_after_general_cap(int basis,int delta,int expected)
        {var b=Budget(basis:basis,delta:delta);Assert.That(b.BudgetFor(C("vietnam")),Is.EqualTo(expected));}
        [Test] public void Out_of_region_within_base_is_allowed_but_cannot_regain_bonus()
        {var b=Budget();Assert.That(b.TrySpend(C("thailand"),2,out var a),Is.True);Assert.That(a.TrySpend(C("iran"),1,out var outside),Is.True);Assert.That(outside.EffectiveBudget,Is.EqualTo(4));Assert.That(outside.EligibleBonuses,Is.Empty);Assert.That(outside.TrySpend(C("japan"),1,out var last),Is.True);Assert.That(last.Spent,Is.EqualTo(4));Assert.That(last.TrySpend(C("thailand"),1,out var rejected),Is.False);Assert.That(rejected,Is.Null);Assert.That(b.Spent,Is.Zero);}
        [Test] public void Fifth_target_can_only_stay_in_eligible_region_and_rejection_keeps_prefix()
        {var b=Budget();Assert.That(b.TrySpend(C("thailand"),4,out var a),Is.True);Assert.That(a.TrySpend(C("iran"),1,out var denied),Is.False);Assert.That(denied,Is.Null);Assert.That(a.EffectiveBudget,Is.EqualTo(5));Assert.That(a.TrySpend(C("japan"),1,out var last),Is.True);Assert.That(last.HasTargets,Is.True);Assert.That(last.Spent,Is.EqualTo(5));}
        [Test] public void Multiple_regional_bonuses_drop_independently_with_accepted_history()
        {var map=DefinitionTests.World();var source=new List<OpsRegionalBonus>{new OpsRegionalBonus("china","region.asia",1),new OpsRegionalBonus("vietnam","region.southeast_asia",1)};var b=new OpsRegionalBudget(map,Quote(delta:-1),source);source.Clear();Assert.That(b.EffectiveBudget,Is.EqualTo(5));Assert.That(b.TrySpend(C("vietnam"),1,out var a),Is.True);Assert.That(a.TrySpend(C("japan"),1,out var j),Is.True);Assert.That(j.EffectiveBudget,Is.EqualTo(4));Assert.That(j.TrySpend(C("thailand"),1,out var t),Is.True);Assert.That(t.EffectiveBudget,Is.EqualTo(4));Assert.That(((IList)t.EligibleBonuses).IsReadOnly,Is.True);}
        internal static GameState Placement(long version=0,long sequence=0)
        {var map=DefinitionTests.World();var countries=RegionScoringTests.Empty(map);countries[C("thailand")]=new CountryState(0,2);var b=Budget(map);var ops=new OperationsContext("regional",PlayerSide.US,b.EffectiveBudget,map.Countries.Select(c=>c.Id),costs:new InfluenceCosts(1,2),regionalBudget:b);return new GameState(map,countries,ops,version:version,eventSequence:sequence);}
        [Test] public void Influence_reprices_enemy_control_and_commits_regional_history_per_point()
        {var s=Placement();var p=InfluenceRules.PlanPlacement(s,s.Operations,new[]{C("thailand"),C("thailand"),C("japan"),C("japan")});Assert.That(p.Accepted,Is.True);Assert.That(p.Costs,Is.EqualTo(new[]{2,1,1,1}));Assert.That(p.NextState.SpentOps,Is.EqualTo(5));Assert.That(p.NextState.Operations.RegionalBudget.Spent,Is.EqualTo(5));Assert.That(p.NextState.Operations.RegionalBudget.HasTargets,Is.True);Assert.That(s.Operations.RegionalBudget.Spent,Is.Zero);}
        [Test] public void Influence_batch_rejects_cross_region_overspend_atomically_but_keeps_prior_command()
        {var s=Placement();var one=InfluenceRules.PlanPlacement(s,s.Operations,new[]{C("thailand")});Assert.That(one.Accepted,Is.True);var n=one.NextState;var bad=InfluenceRules.PlanPlacement(n,n.Operations,new[]{C("japan"),C("japan"),C("iran")});Assert.That(bad.Error,Is.EqualTo(RuleError.InsufficientBudget));Assert.That(bad.NextState,Is.Null);Assert.That(n.SpentOps,Is.EqualTo(2));Assert.That(n.GetInfluence(C("japan"),PlayerSide.US),Is.Zero);Assert.That(n.Operations.RegionalBudget.EligibleBonuses.Count,Is.EqualTo(1));var good=InfluenceRules.PlanPlacement(n,n.Operations,new[]{C("iran"),C("japan")});Assert.That(good.Accepted,Is.True);Assert.That(good.NextState.Operations.Budget,Is.EqualTo(4));Assert.That(good.NextState.Operations.RegionalBudget.EligibleBonuses,Is.Empty);}
        [TestCase(true)] [TestCase(false)] public void Numeric_overflow_does_not_commit_regional_prefix(bool version)
        {var s=Placement(version?long.MaxValue:0,version?0:long.MaxValue);var p=InfluenceRules.PlanPlacement(s,s.Operations,new[]{C("japan")});Assert.That(p.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(p.NextState,Is.Null);Assert.That(s.Operations.RegionalBudget.HasTargets,Is.False);}
        [Test] public void Invalid_region_duplicate_id_nonmap_purpose_and_bad_cost_are_rejected()
        {var m=DefinitionTests.World();Assert.Throws<ArgumentException>(()=>new OpsRegionalBudget(m,Quote(),new[]{new OpsRegionalBonus("x","region.unknown",1)}));Assert.Throws<ArgumentException>(()=>new OpsRegionalBudget(m,Quote(),new[]{new OpsRegionalBonus("same","region.asia",1),new OpsRegionalBonus("same","region.europe",1)}));Assert.Throws<ArgumentException>(()=>Budget(purpose:OpsValuePurpose.Space));Assert.Throws<ArgumentException>(()=>Budget(purpose:OpsValuePurpose.OpsThreshold));Assert.Throws<ArgumentOutOfRangeException>(()=>Budget().TrySpend(C("japan"),0,out _));Assert.Throws<ArgumentException>(()=>Budget().BudgetFor(C("unknown")));}
        [Test] public void Context_and_state_reject_mismatched_region_budget_identity_mode_map_or_spent()
        {var m=DefinitionTests.World();var b=Budget(m);var reach=m.Countries.Select(c=>c.Id);var cost=new InfluenceCosts(1,2);Assert.Throws<ArgumentException>(()=>new OperationsContext("x",PlayerSide.USSR,5,reach,costs:cost,regionalBudget:b));Assert.Throws<ArgumentException>(()=>new OperationsContext("x",PlayerSide.US,4,reach,costs:cost,regionalBudget:b));Assert.Throws<ArgumentException>(()=>new OperationsContext("x",PlayerSide.US,5,reach,OperationsMode.Realignment,cost,b));var context=new OperationsContext("x",PlayerSide.US,5,reach,costs:cost,regionalBudget:b);Assert.Throws<ArgumentException>(()=>new GameState(m,RegionScoringTests.Empty(m),context,spentOps:1));var other=DefinitionTests.World();Assert.Throws<ArgumentException>(()=>new GameState(other,RegionScoringTests.Empty(other),context));}
    }
}
