using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class DefconTests
    {
        public static Dictionary<int,IReadOnlyList<string>> Regions()=>new Dictionary<int,IReadOnlyList<string>>
        {
            [5]=Array.Empty<string>(),[4]=new[]{"region.europe"},
            [3]=new[]{"region.europe","region.asia"},
            [2]=new[]{"region.europe","region.asia","region.middle_east"}
        };
        public static DefconDefinition Definition()=>new DefconDefinition(DefinitionTests.World(),5,5,1,1,1,
            new[]{DefconActionKind.Coup,DefconActionKind.Realignment},Regions());
        [TestCase(5,"country.france",true)] [TestCase(4,"country.france",false)]
        [TestCase(4,"country.japan",true)] [TestCase(3,"country.japan",false)]
        [TestCase(3,"country.iran",true)] [TestCase(2,"country.iran",false)]
        [TestCase(2,"country.angola",true)] [TestCase(2,"country.cuba",true)]
        [TestCase(3,"country.thailand",false)] [TestCase(4,"country.canada",false)]
        public void Rule_regions_not_display_geography_control_coup_and_realignment(int level,string country,bool allowed)
        {
            var state=new DefconState(Definition(),level);
            foreach(var action in new[]{DefconActionKind.Coup,DefconActionKind.Realignment})
            {
                var result=DefconService.CheckRestriction(state,action,new CountryId(country));
                Assert.That(result.Allowed,Is.EqualTo(allowed));
                Assert.That(result.Reason,Is.EqualTo(allowed?DefconRestrictionReason.None:DefconRestrictionReason.GeographicRestriction));
                if(allowed)Assert.That(result.BlockedRegion,Is.Null);else Assert.That(result.BlockedRegion,Is.Not.Null);
            }
        }
        [TestCase(DefconActionKind.Influence)] [TestCase(DefconActionKind.WarEvent)]
        public void Nonrestricted_actions_do_not_inherit_geographic_gate(DefconActionKind action)
            => Assert.That(DefconService.CheckRestriction(new DefconState(Definition(),2),action,new CountryId("country.france")).Allowed,Is.True);
        [Test] public void Definition_copies_and_freezes_collections()
        {
            var actions=new[]{DefconActionKind.Coup,DefconActionKind.Realignment};var regions=Regions();var europe=(string[])regions[4];
            var d=new DefconDefinition(DefinitionTests.World(),5,5,1,1,1,actions,regions);
            actions[0]=DefconActionKind.WarEvent;europe[0]="broken";regions.Clear();
            Assert.That(d.RestrictedActions,Does.Contain(DefconActionKind.Coup));Assert.That(d.BlockedRegions[4],Is.EqualTo(new[]{"region.europe"}));
            Assert.Throws<NotSupportedException>(()=>((IList<string>)d.BlockedRegions[4]).Clear());
            Assert.Throws<NotSupportedException>(()=>((IDictionary<int,IReadOnlyList<string>>)d.BlockedRegions).Clear());
            Assert.Throws<NotSupportedException>(()=>((IList<DefconActionKind>)d.RestrictedActions).Clear());
        }
        [TestCase("missing-level")] [TestCase("extra-level")] [TestCase("unknown-region")]
        [TestCase("duplicate-region")] [TestCase("null-regions")] [TestCase("less-restricted-lower")]
        [TestCase("max-restricted")] [TestCase("null-map")] [TestCase("duplicate-action")]
        [TestCase("unsupported-action")] [TestCase("missing-action")] [TestCase("null-table")]
        public void Invalid_definitions_are_rejected(string mutation)
        {
            var regions=Regions();var map=DefinitionTests.World();IEnumerable<DefconActionKind> actions=new[]{DefconActionKind.Coup,DefconActionKind.Realignment};
            if(mutation=="missing-level")regions.Remove(3);if(mutation=="extra-level")regions[1]=Array.Empty<string>();
            if(mutation=="unknown-region")regions[2]=new[]{"region.unknown"};if(mutation=="duplicate-region")regions[4]=new[]{"region.europe","region.europe"};
            if(mutation=="null-regions")regions[4]=null;if(mutation=="less-restricted-lower")regions[2]=new[]{"region.europe"};
            if(mutation=="max-restricted")regions[5]=new[]{"region.europe"};if(mutation=="null-map")map=null;
            if(mutation=="duplicate-action")actions=new[]{DefconActionKind.Coup,DefconActionKind.Coup};
            if(mutation=="unsupported-action")actions=new[]{DefconActionKind.Coup,DefconActionKind.WarEvent};
            if(mutation=="missing-action")actions=Array.Empty<DefconActionKind>();
            Assert.That(()=>new DefconDefinition(map,5,5,1,1,1,actions,mutation=="null-table"?null:regions),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase(1,5,1,1,1)] [TestCase(6,5,1,1,1)] [TestCase(5,5,0,1,1)]
        [TestCase(5,5,5,1,1)] [TestCase(5,1000,1,1,1)] [TestCase(5,5,1,0,1)]
        [TestCase(5,5,1,1,0)] [TestCase(5,5,1,1,5)]
        public void Invalid_track_parameters_fail(int initial,int max,int nuclear,int improve,int degrade)
            => Assert.That(()=>new DefconDefinition(DefinitionTests.World(),initial,max,nuclear,improve,degrade,
                new[]{DefconActionKind.Coup,DefconActionKind.Realignment},Regions()),Throws.InstanceOf<ArgumentException>());
        [TestCase(0)] [TestCase(6)] [TestCase(int.MinValue)]
        public void Corrupt_current_level_is_rejected(int level)=>Assert.Throws<ArgumentOutOfRangeException>(()=>new DefconState(Definition(),level));
        [TestCase(2,3)] [TestCase(4,5)] [TestCase(5,5)]
        public void Turn_start_improves_with_cap_without_mutating_input(int level,int expected)
        {
            var state=new DefconState(Definition(),level);var plan=DefconService.PlanChange(state,DefconChangeKind.TurnStartImprovement);
            Assert.That(plan.Before,Is.SameAs(state));Assert.That(state.Level,Is.EqualTo(level));Assert.That(plan.After.Level,Is.EqualTo(expected));
            Assert.That(plan.After.Definition,Is.SameAs(state.Definition));Assert.That(plan.Changed,Is.EqualTo(level!=expected));Assert.That(plan.NuclearWar,Is.False);
        }
        [TestCase(DefconChangeKind.BattlegroundCoupDegradation)] [TestCase(DefconChangeKind.DegradeOne)] [TestCase(DefconChangeKind.SetLevel)]
        public void Nuclear_signal_preserves_phasing_player_and_input(DefconChangeKind kind)
        {
            var state=new DefconState(Definition(),2);var plan=DefconService.PlanChange(state,kind,PlayerSide.US,kind==DefconChangeKind.SetLevel?(int?)1:null);
            Assert.That(plan.NuclearWar,Is.True);Assert.That(plan.PhasingPlayer,Is.EqualTo(PlayerSide.US));Assert.That(plan.After.Level,Is.EqualTo(1));Assert.That(state.Level,Is.EqualTo(2));
            Assert.Throws<InvalidOperationException>(()=>DefconService.PlanChange(plan.After,DefconChangeKind.ImproveOne));
            Assert.That(DefconService.CheckRestriction(plan.After,DefconActionKind.Influence,new CountryId("country.france")).Reason,Is.EqualTo(DefconRestrictionReason.NuclearWarReached));
        }
        [TestCase(2)] [TestCase(5)] public void Any_degradation_requires_responsibility(int level)
        {
            var state=new DefconState(Definition(),level);
            Assert.That(()=>DefconService.PlanChange(state,DefconChangeKind.DegradeOne),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>DefconService.PlanChange(state,DefconChangeKind.SetLevel,null,level-1),Throws.InstanceOf<ArgumentException>());
            Assert.That(state.Level,Is.EqualTo(level));
        }
        [Test] public void Invalid_actions_targets_and_change_parameters_fail()
        {
            var s=new DefconState(Definition(),3);
            Assert.That(()=>DefconService.CheckRestriction(s,(DefconActionKind)99,new CountryId("country.france")),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>DefconService.CheckRestriction(s,DefconActionKind.Coup,new CountryId("country.missing")),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>DefconService.PlanChange(s,(DefconChangeKind)99),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>DefconService.PlanChange(s,DefconChangeKind.SetLevel),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>DefconService.PlanChange(s,DefconChangeKind.SetLevel,PlayerSide.US,0),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>DefconService.PlanChange(s,DefconChangeKind.SetLevel,PlayerSide.US,6),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>DefconService.PlanChange(s,DefconChangeKind.ImproveOne,null,4),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>DefconService.PlanChange(s,DefconChangeKind.DegradeOne,(PlayerSide)99),Throws.InstanceOf<ArgumentException>());
            Assert.Throws<ArgumentNullException>(()=>new DefconState(null,3));
            Assert.Throws<ArgumentNullException>(()=>DefconService.PlanChange(null,DefconChangeKind.ImproveOne));
            Assert.Throws<ArgumentNullException>(()=>DefconService.CheckRestriction(null,DefconActionKind.Coup,new CountryId("country.france")));
        }
        [Test] public void Configured_steps_are_used_and_legal_fatal_coup_is_not_geographically_rejected()
        {
            var d=new DefconDefinition(DefinitionTests.World(),5,5,1,2,2,new[]{DefconActionKind.Coup,DefconActionKind.Realignment},Regions());
            Assert.That(DefconService.PlanChange(new DefconState(d,2),DefconChangeKind.TurnStartImprovement).After.Level,Is.EqualTo(4));
            var s=new DefconState(d,2);Assert.That(DefconService.CheckRestriction(s,DefconActionKind.Coup,new CountryId("country.angola")).Allowed,Is.True);
            Assert.That(DefconService.PlanChange(s,DefconChangeKind.BattlegroundCoupDegradation,PlayerSide.USSR).After.Level,Is.EqualTo(1));
            Assert.That(DefconService.PlanChange(new DefconState(d,3),DefconChangeKind.SetLevel,null,5).After.Level,Is.EqualTo(5));
        }
    }
}
