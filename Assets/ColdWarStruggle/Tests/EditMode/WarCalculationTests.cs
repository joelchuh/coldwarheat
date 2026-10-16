using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class WarCalculationTests
    {
        internal static CountryId C(string n)=>new CountryId("country."+n);
        internal static CardId Id(string n)=>new CardId("card."+n);
        [TestCase("korean_war","south_korea")][TestCase("arab_israeli_war","israel")][TestCase("indo_pakistani_war","india")][TestCase("indo_pakistani_war","pakistan")]
        public void All_six_rolls_capture_only_on_success_and_preserve_existing_influence(string card,string target)
        {
            var d=EarlyWarDefinitionTests.Fixture();var board=RegionScoringTests.Empty(d.Map);board[C(target)]=new CountryState(3,2);
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})foreach(var roll in Enumerable.Range(1,6))
            {
                var r=WarEventRules.Calculate(d,Id(card),board,side,C(target),roll);var owner=card=="indo_pakistani_war"?side:PlayerSide.USSR;
                Assert.That(r.Beneficiary,Is.EqualTo(owner));Assert.That(r.Penalty,Is.Zero);Assert.That(r.Success,Is.EqualTo(roll>=4));Assert.That(r.AdjustedRoll,Is.EqualTo(roll));
                Assert.That(r.Captured,Is.EqualTo(roll<4?0:owner==PlayerSide.US?2:3));Assert.That(r.After.US,Is.EqualTo(roll<4?3:owner==PlayerSide.US?5:0));Assert.That(r.After.USSR,Is.EqualTo(roll<4?2:owner==PlayerSide.USSR?5:0));
                Assert.That(board[C(target)].US,Is.EqualTo(3));Assert.That(board[C(target)].USSR,Is.EqualTo(2));
            }
        }
        [Test] public void Korea_counts_controlled_neighbors_not_target_or_recursive_US_link()
        {
            var d=EarlyWarDefinitionTests.Fixture();var b=RegionScoringTests.Empty(d.Map);RegionScoringTests.Control(d.Map,b,PlayerSide.US,"country.south_korea","country.japan","country.north_korea","country.taiwan");
            var r=WarEventRules.Calculate(d,Id("korean_war"),b,PlayerSide.US,C("south_korea"),6);
            Assert.That(r.Penalty,Is.EqualTo(3));Assert.That(r.AdjustedRoll,Is.EqualTo(3));Assert.That(r.Success,Is.False);Assert.That(r.TargetControlPenalty,Is.False);Assert.That(r.OpponentSuperpowerAdjacent,Is.False);Assert.That(r.ControlledNeighbors.Select(c=>c.Value),Is.EquivalentTo(new[]{"country.japan","country.north_korea","country.taiwan"}));
        }
        [Test] public void Israel_target_control_adds_penalty_and_adjusted_roll_can_be_negative()
        {
            var d=EarlyWarDefinitionTests.Fixture();var b=RegionScoringTests.Empty(d.Map);RegionScoringTests.Control(d.Map,b,PlayerSide.US,"country.israel","country.egypt","country.jordan","country.lebanon","country.syria");
            var r=WarEventRules.Calculate(d,Id("arab_israeli_war"),b,PlayerSide.USSR,C("israel"),1);Assert.That(r.Penalty,Is.EqualTo(5));Assert.That(r.AdjustedRoll,Is.EqualTo(-4));Assert.That(r.TargetControlPenalty,Is.True);Assert.That(r.Success,Is.False);
        }
        [Test] public void Having_influence_is_not_control_but_exact_stability_difference_is()
        {
            var d=EarlyWarDefinitionTests.Fixture();var b=RegionScoringTests.Empty(d.Map);b[C("japan")]=new CountryState(4,1);
            Assert.That(WarEventRules.Calculate(d,Id("korean_war"),b,PlayerSide.USSR,C("south_korea"),4).Success,Is.True);
            b[C("japan")]=new CountryState(5,1);Assert.That(WarEventRules.Calculate(d,Id("korean_war"),b,PlayerSide.USSR,C("south_korea"),4).Success,Is.False);
        }
        [Test] public void Only_direct_opponent_superpower_connection_is_an_additional_neighbor()
        {
            var world=DefinitionTests.World();var powers=new Dictionary<PlayerSide,IReadOnlyList<CountryId>>{{PlayerSide.US,world.GetSuperpowerNeighbors(PlayerSide.US)},{PlayerSide.USSR,world.GetSuperpowerNeighbors(PlayerSide.USSR).Append(C("india")).ToArray()}};
            var edges=world.Countries.SelectMany(c=>world.GetCountryNeighbors(c.Id).Where(n=>StringComparer.Ordinal.Compare(c.Id.Value,n.Value)<0).Select(n=>new KeyValuePair<CountryId,CountryId>(c.Id,n)));
            var map=new MapDefinition(world.Countries,edges,powers);var d=EarlyWarDefinitionTests.Fixture(map);var b=RegionScoringTests.Empty(map);RegionScoringTests.Control(map,b,PlayerSide.USSR,"country.pakistan");
            var us=WarEventRules.Calculate(d,Id("indo_pakistani_war"),b,PlayerSide.US,C("india"),6);Assert.That(us.Penalty,Is.EqualTo(2));Assert.That(us.OpponentSuperpowerAdjacent,Is.True);
            var su=WarEventRules.Calculate(d,Id("indo_pakistani_war"),b,PlayerSide.USSR,C("india"),6);Assert.That(su.Penalty,Is.Zero);Assert.That(su.OpponentSuperpowerAdjacent,Is.False);
        }
        [Test] public void Empty_target_still_allows_success_and_targets_do_not_require_access()
        {
            var d=EarlyWarDefinitionTests.Fixture();var b=RegionScoringTests.Empty(d.Map);var r=WarEventRules.Calculate(d,Id("indo_pakistani_war"),b,PlayerSide.US,C("pakistan"),6);
            Assert.That(r.Success,Is.True);Assert.That(r.Captured,Is.Zero);Assert.That(WarEventRules.QueryTargets(d,Id("indo_pakistani_war")),Is.EquivalentTo(new[]{C("india"),C("pakistan")}));
        }
        [TestCase("target")][TestCase("card")][TestCase("side")][TestCase("missing-country")][TestCase("extra-country")][TestCase("roll-zero")][TestCase("roll-high")][TestCase("overflow")]
        public void Invalid_or_overflowing_calculation_returns_no_result(string defect)
        {
            var d=EarlyWarDefinitionTests.Fixture();var b=RegionScoringTests.Empty(d.Map);if(defect=="missing-country")b.Remove(C("france"));if(defect=="extra-country")b.Add(C("unknown"),new CountryState(0,0));if(defect=="overflow")b[C("south_korea")]=new CountryState(1,int.MaxValue);
            TestDelegate run=()=>WarEventRules.Calculate(d,Id(defect=="card"?"duck_and_cover":"korean_war"),b,defect=="side"?(PlayerSide)99:PlayerSide.US,C(defect=="target"?"france":"south_korea"),defect=="roll-zero"?0:defect=="roll-high"?7:6);
            if(defect=="overflow")Assert.That(run,Throws.TypeOf<OverflowException>());else Assert.That(run,Throws.InstanceOf<ArgumentException>());
        }
    }
}
