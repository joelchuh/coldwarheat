using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class FinalScoringTests
    {
        public static FinalScoringDefinition Definition(MapDefinition map,int chinaVp=1,RegionScoringDefinition regions=null)
        {
            var d=regions??RegionScoringTests.Definition(map);
            return new FinalScoringDefinition(d,d.Regions.Select(x=>x.Id),new CardId("card.china"),chinaVp);
        }
        public static ChinaCardState China(PlayerSide owner=PlayerSide.US,CardFace face=CardFace.FaceUp)
            =>new ChinaCardState(new CardId("card.china"),owner,face);
        static FinalScoringResult EmptyResult(int chinaVp=1,PlayerSide owner=PlayerSide.US)
        {
            var map=DefinitionTests.World();
            return FinalScoringService.CalculateUnmodified(Definition(map,chinaVp),RegionScoringTests.Snapshot(map,RegionScoringTests.Empty(map)),China(owner));
        }
        [TestCase(PlayerSide.US,CardFace.FaceUp,7)] [TestCase(PlayerSide.US,CardFace.FaceDown,7)]
        [TestCase(PlayerSide.USSR,CardFace.FaceUp,-7)] [TestCase(PlayerSide.USSR,CardFace.FaceDown,-7)]
        public void Complete_regions_and_separate_owner_bonus_are_readonly(PlayerSide owner,CardFace face,int expected)
        {
            var map=DefinitionTests.World();var d=Definition(map,7);var state=RegionScoringTests.Empty(map);
            var s=RegionScoringTests.Snapshot(map,state);var china=China(owner,face);
            var r=FinalScoringService.CalculateUnmodified(d,s,china);
            Assert.That(r.Regions.Select(x=>x.RegionId),Is.EquivalentTo(d.RegionIds));Assert.That(r.Regions.Count,Is.EqualTo(6));
            Assert.That(r.Regions.All(x=>x.SnapshotId==s.Id && x.StateVersion==s.StateVersion && ReferenceEquals(x.Definition,d.RegionDefinition)),Is.True);
            Assert.That(r.SnapshotId,Is.EqualTo(s.Id));Assert.That(r.StateVersion,Is.EqualTo(s.StateVersion));Assert.That(r.Definition,Is.SameAs(d));
            Assert.That(r.NetVpForUS,Is.EqualTo(expected));Assert.That(r.ChinaVpForUS,Is.EqualTo(expected));Assert.That(r.ChinaOwner,Is.EqualTo(owner));
            Assert.That(r.EuropeControlWinner,Is.Null);Assert.That(r.Regions.All(x=>x.NetVpForUS==0),Is.True);
            Assert.Throws<NotSupportedException>(()=>((IList<RegionScoringResult>)r.Regions).Clear());
            Assert.That(china.Owner,Is.EqualTo(owner));Assert.That(china.Face,Is.EqualTo(face));Assert.That(s.Countries.Values.All(x=>x.US==0 && x.USSR==0),Is.True);
        }
        [Test] public void Asia_includes_thailand_without_extra_southeast_asia_score()
        {
            var map=DefinitionTests.World();var state=RegionScoringTests.Empty(map);RegionScoringTests.Control(map,state,PlayerSide.US,"country.thailand");
            var r=FinalScoringService.CalculateUnmodified(Definition(map),RegionScoringTests.Snapshot(map,state),China(PlayerSide.USSR));
            Assert.That(r.Regions.Single(x=>x.RegionId=="region.asia").NetVpForUS,Is.EqualTo(4));Assert.That(r.NetVpForUS,Is.EqualTo(3));
            Assert.That(r.Regions.Any(x=>x.RegionId=="region.southeast_asia"),Is.False);
        }
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)]
        public void Europe_control_wins_even_against_opposing_vp_and_other_regions(PlayerSide side)
        {
            var map=DefinitionTests.World();var state=RegionScoringTests.Empty(map);var other=side==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
            RegionScoringTests.Control(map,state,side,map.GetCountriesInRegion("region.europe").Where(x=>x.Battleground).Select(x=>x.Id.Value).ToArray());
            RegionScoringTests.Control(map,state,other,map.Countries.Where(x=>x.Region!="region.europe").Select(x=>x.Id.Value).ToArray());
            var r=FinalScoringService.CalculateUnmodified(Definition(map),RegionScoringTests.Snapshot(map,state),China(other));
            Assert.That(r.Regions.Count,Is.EqualTo(6));Assert.That(r.EuropeControlWinner,Is.EqualTo(side));Assert.That(r.NetVpForUS,Is.Null);
            int before=side==PlayerSide.US?-19:19;var v=VictoryService.Evaluate(VictoryTests.Definition(),new FinalScoringCompletedEvidence("final","R2015",before,r));
            Assert.That(v.Outcome,Is.EqualTo(VictoryOutcome.Finished));Assert.That(v.Winner,Is.EqualTo(side));Assert.That(v.SignedVp,Is.EqualTo(before));
        }
        [TestCase(-2,-1,PlayerSide.USSR)] [TestCase(-1,0,null)] [TestCase(0,1,PlayerSide.US)] [TestCase(19,20,PlayerSide.US)]
        public void Final_victory_uses_total_sign_and_zero_is_finished_draw(int before,int expected,PlayerSide? winner)
        {
            var v=VictoryService.Evaluate(VictoryTests.Definition(),new FinalScoringCompletedEvidence("final.10","R2015.10.3.2",before,EmptyResult()));
            Assert.That(v.SignedVp,Is.EqualTo(expected));Assert.That(v.Winner,Is.EqualTo(winner));Assert.That(v.Outcome,Is.EqualTo(VictoryOutcome.Finished));
            Assert.That(v.Checkpoint,Is.EqualTo(VictoryCheckpoint.FinalScoringCompleted));Assert.That(v.CheckpointId,Is.EqualTo("final.10"));
            Assert.That(v.SourceRuleId,Is.EqualTo("R2015.10.3.2"));Assert.That(v.PendingMilitary,Is.Null);
        }
        [Test] public void All_regions_finish_even_when_first_region_crosses_twenty_then_opponent_wins()
        {
            var map=DefinitionTests.World();var state=RegionScoringTests.Empty(map);
            RegionScoringTests.Control(map,state,PlayerSide.US,"country.thailand");
            RegionScoringTests.Control(map,state,PlayerSide.USSR,map.Countries.Where(x=>x.Battleground && new[]{"region.middle_east","region.central_america","region.south_america"}.Contains(x.Region)).Select(x=>x.Id.Value).ToArray());
            var r=FinalScoringService.CalculateUnmodified(Definition(map),RegionScoringTests.Snapshot(map,state),China());
            Assert.That(19+r.Regions.Single(x=>x.RegionId=="region.asia").NetVpForUS,Is.EqualTo(23));
            Assert.That(r.NetVpForUS,Is.EqualTo(-28));
            var v=VictoryService.Evaluate(VictoryTests.Definition(),new FinalScoringCompletedEvidence("final","rule",19,r));
            Assert.That(v.Winner,Is.EqualTo(PlayerSide.USSR));Assert.That(v.SignedVp,Is.EqualTo(-9));
        }
        [Test] public void Region_order_and_caller_list_mutation_cannot_change_plan()
        {
            var map=DefinitionTests.World();var region=RegionScoringTests.Definition(map);var ids=region.Regions.Select(x=>x.Id).Reverse().ToList();
            var d=new FinalScoringDefinition(region,ids,new CardId("card.china"),1);ids.Clear();
            Assert.That(d.RegionIds,Is.EqualTo(region.Regions.Select(x=>x.Id)));Assert.Throws<NotSupportedException>(()=>((IList<string>)d.RegionIds).Clear());
        }
        [TestCase("missing")] [TestCase("duplicate")] [TestCase("unknown")] [TestCase("sea")] [TestCase("blank")] [TestCase("excess")]
        public void Incomplete_or_invalid_final_regions_are_rejected(string kind)
        {
            var map=DefinitionTests.World();var region=RegionScoringTests.Definition(map);var ids=region.Regions.Select(x=>x.Id).ToList();
            if(kind=="missing")ids.RemoveAt(0);if(kind=="duplicate")ids[0]=ids[1];if(kind=="unknown")ids[0]="region.unknown";
            if(kind=="sea")ids[0]=region.WeightedRegionId;if(kind=="blank")ids[0]=" ";if(kind=="excess")ids.Add(ids[0]);
            Assert.Throws<ArgumentException>(()=>new FinalScoringDefinition(region,ids,new CardId("card.china"),1));
        }
        [Test] public void Invalid_definition_and_service_arguments_fail_closed()
        {
            var map=DefinitionTests.World();var region=RegionScoringTests.Definition(map);var ids=region.Regions.Select(x=>x.Id);var d=Definition(map);var s=RegionScoringTests.Snapshot(map,RegionScoringTests.Empty(map));
            Assert.Throws<ArgumentNullException>(()=>new FinalScoringDefinition(null,ids,new CardId("card.china"),1));
            Assert.Throws<ArgumentNullException>(()=>new FinalScoringDefinition(region,null,new CardId("card.china"),1));
            Assert.Throws<ArgumentException>(()=>new FinalScoringDefinition(region,ids,new CardId("card.fidel"),1));
            Assert.Throws<ArgumentException>(()=>new FinalScoringDefinition(region,ids,new CardId("card.china"),-1));
            Assert.Throws<ArgumentNullException>(()=>FinalScoringService.CalculateUnmodified(null,s,China()));
            Assert.Throws<ArgumentNullException>(()=>FinalScoringService.CalculateUnmodified(d,null,China()));
            Assert.Throws<ArgumentNullException>(()=>FinalScoringService.CalculateUnmodified(d,s,null));
            var other=DefinitionTests.World();Assert.Throws<ArgumentException>(()=>FinalScoringService.CalculateUnmodified(d,RegionScoringTests.Snapshot(other,RegionScoringTests.Empty(other)),China()));
            Assert.Throws<ArgumentException>(()=>FinalScoringService.CalculateUnmodified(d,RegionScoringTests.Snapshot(map,RegionScoringTests.Empty(map),new[]{"effect.formosan_resolution"}),China()));
            Assert.Throws<ArgumentNullException>(()=>new FinalScoringCompletedEvidence("final","rule",0,null));
        }
        [TestCase(20)] [TestCase(-20)] [TestCase(int.MaxValue)] [TestCase(int.MinValue)]
        public void Previously_terminal_score_cannot_start_final_scoring(int before)
            =>Assert.Throws<ArgumentException>(()=>VictoryService.Evaluate(VictoryTests.Definition(),new FinalScoringCompletedEvidence("final","rule",before,EmptyResult())));
        [Test] public void Aggregation_uses_wide_intermediate_then_checks_actual_overflow()
        {
            var map=DefinitionTests.World();var tables=RegionScoringTests.Tables().Select(t=>new RegionScoreTable(t.Id,1500000000,1500000000,t.Control.HasValue?(int?)1500000000:null));
            var region=new RegionScoringDefinition(map,0,0,tables,"region.southeast_asia","region.asia",RegionScoringTests.Weights(map));var state=RegionScoringTests.Empty(map);
            foreach(var id in new[]{"region.africa","region.asia","region.central_america","region.europe"})
                RegionScoringTests.Control(map,state,id=="region.africa"||id=="region.asia"?PlayerSide.US:PlayerSide.USSR,map.GetCountriesInRegion(id).First(x=>!x.Battleground).Id.Value);
            var d=Definition(map,1,region);var r=FinalScoringService.CalculateUnmodified(d,RegionScoringTests.Snapshot(map,state),China());
            Assert.That(r.NetVpForUS,Is.EqualTo(1));
            state[map.GetCountriesInRegion("region.central_america").First(x=>!x.Battleground).Id]=new CountryState(0,0);
            state[map.GetCountriesInRegion("region.europe").First(x=>!x.Battleground).Id]=new CountryState(0,0);
            Assert.Throws<OverflowException>(()=>FinalScoringService.CalculateUnmodified(d,RegionScoringTests.Snapshot(map,state),China()));
        }
        [Test] public void Final_vp_addition_checks_overflow_without_clamping_to_track()
            =>Assert.Throws<OverflowException>(()=>VictoryService.Evaluate(VictoryTests.Definition(),new FinalScoringCompletedEvidence("final","rule",1,EmptyResult(int.MaxValue))));
    }
}
