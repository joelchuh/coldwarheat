using System;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class InfluenceRulesTests
    {
        [Test] public void Reprice_after_each_point_without_mutating_input()
        {
            var s=InfluenceFixture.Create();var p=InfluenceRules.PlanPlacement(s,s.Operations,new[]{InfluenceFixture.X,InfluenceFixture.X,InfluenceFixture.X});
            Assert.That(p.Accepted,Is.True);Assert.That(p.Costs,Is.EqualTo(new[]{2,1,1}));Assert.That(p.NextState.GetInfluence(InfluenceFixture.X,PlayerSide.US),Is.EqualTo(3));
            Assert.That(s.GetInfluence(InfluenceFixture.X,PlayerSide.US),Is.Zero);Assert.That(s.SpentOps,Is.Zero);Assert.That(s.Version,Is.Zero);
            Assert.That(p.NextState.SpentOps,Is.EqualTo(4));Assert.That(p.NextState.Version,Is.EqualTo(1));Assert.That(p.NextState.EventSequence,Is.EqualTo(3));
        }
        [TestCase("chain",RuleError.IllegalTarget)] [TestCase("unknown",RuleError.UnknownCountry)]
        [TestCase("budget",RuleError.InsufficientBudget)] [TestCase("empty",RuleError.InvalidCommand)]
        [TestCase("context",RuleError.StaleContext)] [TestCase("mode",RuleError.ModeMismatch)]
        [TestCase("ended",RuleError.GameAlreadyEnded)] [TestCase("overflow",RuleError.NumericOverflow)]
        public void Invalid_batch_has_no_partial_result(string scenario,RuleError error)
        {
            var s=InfluenceFixture.Create(budget:scenario=="budget"?1:4,chain:scenario=="chain",mode:scenario=="mode"?OperationsMode.Coup:OperationsMode.Influence,
                ended:scenario=="ended",usAtX:scenario=="overflow"?int.MaxValue:0);
            var targets=scenario=="empty"?new CountryId[0]:scenario=="chain"?new[]{InfluenceFixture.Y,InfluenceFixture.Z}:scenario=="unknown"?new[]{InfluenceFixture.X,new CountryId("UNKNOWN")}:new[]{InfluenceFixture.X};
            var context=scenario=="context"?new OperationsContext("other",PlayerSide.US,4,new[]{InfluenceFixture.X},costs:new InfluenceCosts(1,2)):s.Operations;
            var p=InfluenceRules.PlanPlacement(s,context,targets);Assert.That(p.Error,Is.EqualTo(error));Assert.That(p.NextState,Is.Null);Assert.That(p.Events,Is.Empty);Assert.That(s.SpentOps,Is.Zero);Assert.That(s.Version,Is.Zero);
        }
        [Test] public void Later_invalid_command_preserves_an_earlier_success()
        {
            var s=InfluenceFixture.Create();var one=InfluenceRules.PlanPlacement(s,s.Operations,new[]{InfluenceFixture.X});
            var next=one.NextState;var bad=InfluenceRules.PlanPlacement(next,next.Operations,new[]{InfluenceFixture.Z});
            Assert.That(bad.Accepted,Is.False);Assert.That(next.GetInfluence(InfluenceFixture.X,PlayerSide.US),Is.EqualTo(1));Assert.That(next.SpentOps,Is.EqualTo(2));
        }
        [Test] public void Round_reachability_is_frozen_and_superpower_neighbors_are_included()
        {
            var s=InfluenceFixture.Create(chain:true);var first=InfluenceRules.PlanPlacement(s,s.Operations,new[]{InfluenceFixture.Y});
            Assert.That(first.Accepted,Is.True);
            Assert.That(InfluenceRules.PlanPlacement(first.NextState,first.NextState.Operations,new[]{InfluenceFixture.Z}).Error,Is.EqualTo(RuleError.IllegalTarget));
            Assert.That(InfluenceFixture.Create().Operations.Reachable,Does.Contain(InfluenceFixture.X));
        }
        [Test] public void Invalid_state_and_context_values_rejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>new CountryState(-1,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new OperationsContext("ops",(PlayerSide)99,4,new CountryId[0]));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new OperationsContext("ops",PlayerSide.US,-1,new CountryId[0]));
            var s=InfluenceFixture.Create();
            Assert.Throws<NotSupportedException>(()=>((IDictionary<CountryId,CountryState>)s.Countries).Clear());
            Assert.Throws<NotSupportedException>(()=>((IList<string>)s.GetHand(PlayerSide.US)).Clear());
        }
        [Test] public void Cost_policy_is_injected_not_copied_into_the_rule()
        {
            var basis=InfluenceFixture.Create();var ctx=new OperationsContext("custom-test",PlayerSide.US,4,basis.Operations.Reachable,costs:new InfluenceCosts(3,4));
            var s=new GameState(basis.Map,basis.Countries,ctx);
            var result=InfluenceRules.PlanPlacement(s,ctx,new[]{InfluenceFixture.X});
            Assert.That(result.Costs,Is.EqualTo(new[]{4}));Assert.That(result.NextState.SpentOps,Is.EqualTo(4));
        }
    }
}
