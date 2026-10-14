using System;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class RealignmentTests
    {
        internal static string Data()=>PlayInfluenceEventTests.Read("Data/Rulesets/deluxe-2015/realignment.json");
        internal static RealignmentDefinition Definition()=>RealignmentDefinitionLoader.LoadSubset(Data());
        internal static CountryId C(string name)=>new CountryId("country."+name);
        internal static DefconState Defcon(MapDefinition map,int level=5)=>new DefconState(DefconDefinitionLoader.LoadSubset(PlayInfluenceEventTests.Read("Data/Rulesets/deluxe-2015/defcon.json"),map),level);
        [Test] public void Official_North_Korea_example_includes_local_majority_and_superpower_once()
        {
            var map=DefinitionTests.World();var countries=RegionScoringTests.Empty(map);countries[C("north_korea")]=new CountryState(0,3);
            var p=RealignmentRules.Calculate(Definition(),Defcon(map),countries,C("north_korea"),PlayerSide.US,5,2,Array.Empty<string>());
            Assert.That(p.US.Total,Is.Zero);Assert.That(p.USSR.ControlledNeighbors,Is.Empty);Assert.That(p.USSR.HasMoreInfluence,Is.True);Assert.That(p.USSR.HasSuperpowerNeighbor,Is.True);Assert.That(p.USSR.Total,Is.EqualTo(2));Assert.That(p.UsTotal,Is.EqualTo(5));Assert.That(p.UssrTotal,Is.EqualTo(4));Assert.That(p.DifferenceForUS,Is.EqualTo(1));Assert.That(p.RemovedSide,Is.EqualTo(PlayerSide.USSR));Assert.That(p.Removed,Is.EqualTo(1));Assert.That(p.After.US,Is.Zero);Assert.That(p.After.USSR,Is.EqualTo(2));Assert.That(countries[C("north_korea")].USSR,Is.EqualTo(3));
        }
        [TestCase(PlayerSide.US,6,1,2,0)][TestCase(PlayerSide.USSR,6,1,2,0)][TestCase(PlayerSide.US,1,6,0,1)][TestCase(PlayerSide.USSR,1,6,0,1)][TestCase(PlayerSide.US,3,4,2,1)]
        public void Fixed_US_USSR_dice_remove_loser_only_cap_at_existing_and_tie_changes_nothing(PlayerSide actor,int us,int su,int afterUs,int afterSu)
        {
            var map=DefinitionTests.World();var countries=RegionScoringTests.Empty(map);countries[C("angola")]=new CountryState(2,1);
            var p=RealignmentRules.Calculate(Definition(),Defcon(map),countries,C("angola"),actor,us,su,Array.Empty<string>());
            // US has local-majority +1; no neighbors controlled and no adjacent superpower.
            Assert.That(p.After.US,Is.EqualTo(afterUs));Assert.That(p.After.USSR,Is.EqualTo(afterSu));Assert.That(p.After.US+p.After.USSR,Is.LessThanOrEqualTo(3));
            if(us==3){Assert.That(p.Removed,Is.Zero);Assert.That(p.RemovedSide,Is.Null);}else Assert.That(p.Removed,Is.EqualTo(us==6?1:2));
        }
        [Test] public void Controlled_neighbors_recomputed_from_current_board_and_majority_is_not_control()
        {
            var map=DefinitionTests.World();var b=RegionScoringTests.Empty(map);b[C("iran")]=new CountryState(2,1);b[C("iraq")]=new CountryState(3,0);b[C("pakistan")]=new CountryState(0,2);
            var a=RealignmentRules.Modifiers(Definition(),map,b,C("iran"),PlayerSide.US);Assert.That(a.ControlledNeighbors,Is.EquivalentTo(new[]{C("iraq")}));Assert.That(a.HasMoreInfluence,Is.True);Assert.That(a.Total,Is.EqualTo(2));Assert.That(ControlRules.GetController(2,1,map.GetCountry(C("iran")).Stability),Is.Null);
            b[C("iraq")]=new CountryState(2,1);b[C("iran")]=new CountryState(1,1);var next=RealignmentRules.Modifiers(Definition(),map,b,C("iran"),PlayerSide.US);Assert.That(next.ControlledNeighbors,Is.Empty);Assert.That(next.Total,Is.Zero);Assert.That(a.Total,Is.EqualTo(2));Assert.That(RealignmentRules.Modifiers(Definition(),map,b,C("iran"),PlayerSide.USSR).ControlledNeighbors,Is.EquivalentTo(new[]{C("pakistan")}));
        }
        [Test] public void Legal_target_requires_enemy_but_not_own_presence_or_influence_reachability()
        {
            var map=DefinitionTests.World();var d=Defcon(map);Assert.That(RealignmentRules.CheckTarget(d,C("angola"),new CountryState(0,1),PlayerSide.US,Array.Empty<string>()),Is.EqualTo(RealignmentTargetReason.Allowed));Assert.That(RealignmentRules.CheckTarget(d,C("angola"),new CountryState(4,0),PlayerSide.US,Array.Empty<string>()),Is.EqualTo(RealignmentTargetReason.NoOpponentInfluence));
        }
        [TestCase(4,"france",false)][TestCase(4,"north_korea",true)][TestCase(3,"north_korea",false)][TestCase(3,"iran",true)][TestCase(2,"iran",false)][TestCase(2,"angola",true)]
        public void Defcon_geography_matches_current_region_without_coup_battleground_side_effect(int level,string target,bool allowed)
        {var map=DefinitionTests.World();var d=Defcon(map,level);var reason=RealignmentRules.CheckTarget(d,C(target),new CountryState(1,1),PlayerSide.US,Array.Empty<string>());Assert.That(reason,Is.EqualTo(allowed?RealignmentTargetReason.Allowed:RealignmentTargetReason.GeographicRestriction));Assert.That(d.Level,Is.EqualTo(level));}
        [Test] public void Nuclear_unknown_interventions_bad_dice_and_incomplete_snapshot_fail_closed()
        {
            var map=DefinitionTests.World();var d=Defcon(map);var b=RegionScoringTests.Empty(map);b[C("angola")]=new CountryState(0,1);var definition=Definition();Assert.That(RealignmentRules.CheckTarget(Defcon(map,1),C("angola"),b[C("angola")],PlayerSide.US,Array.Empty<string>()),Is.EqualTo(RealignmentTargetReason.NuclearWarReached));
            Assert.Throws<NotSupportedException>(()=>RealignmentRules.Calculate(definition,d,b,C("angola"),PlayerSide.US,1,1,new[]{"unknown"}));Assert.Throws<ArgumentOutOfRangeException>(()=>RealignmentRules.Calculate(definition,d,b,C("angola"),PlayerSide.US,0,7,Array.Empty<string>()));Assert.Throws<ArgumentOutOfRangeException>(()=>RealignmentRules.Modifiers(definition,map,b,C("angola"),(PlayerSide)99));
            b.Remove(C("france"));Assert.Throws<ArgumentException>(()=>RealignmentRules.Modifiers(definition,map,b,C("angola"),PlayerSide.US));
        }
        [Test] public void Large_data_modifiers_use_long_without_overflow_or_adding_influence()
        {
            var map=DefinitionTests.World();var b=RegionScoringTests.Empty(map);b[C("north_korea")]=new CountryState(1,2);b[C("south_korea")]=new CountryState(0,3);var d=new RealignmentDefinition(1,6,1,int.MaxValue,int.MaxValue,int.MaxValue);var p=RealignmentRules.Calculate(d,Defcon(map),b,C("north_korea"),PlayerSide.US,6,1,Array.Empty<string>());Assert.That(p.USSR.Total,Is.EqualTo(3L*int.MaxValue));Assert.That(p.Removed,Is.EqualTo(1));Assert.That(p.After.USSR,Is.EqualTo(2));
        }
        [Test] public void Loader_uses_shared_values_and_checks_definition_ranges()
        {
            var d=Definition();Assert.That(d.OpsCostPerRoll,Is.EqualTo(1));Assert.That(d.DieMinimum,Is.EqualTo(1));Assert.That(d.DieMaximum,Is.EqualTo(6));Assert.That(d.AdjacentControlledBonus,Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new RealignmentDefinition(0,6,1,1,1,1));Assert.Throws<ArgumentOutOfRangeException>(()=>new RealignmentDefinition(1,6,0,1,1,1));Assert.Throws<ArgumentOutOfRangeException>(()=>new RealignmentDefinition(1,6,1,-1,1,1));
        }
        [TestCase("unknown")][TestCase("fraction")][TestCase("source")][TestCase("section")][TestCase("date")][TestCase("profile")][TestCase("missing")]
        public void Strict_loader_rejects_unknown_fields_types_and_sources(string mutation)
        {
            var j=JObject.Parse(Data());switch(mutation){case "unknown":j["extra"]=true;break;case "fraction":j["modifiers"]["more_local_influence"]=1.5;break;case "source":j["source"]["url"]="https://invalid.example/rules";break;case "section":((JArray)j["source"]["sections"]).RemoveAt(0);break;case "date":j["source"]["checked_on"]="yesterday";break;case "profile":j["content_version"]="9.0.0";break;case "missing":((JObject)j).Remove("die");break;}Assert.Throws<ArgumentException>(()=>RealignmentDefinitionLoader.LoadSubset(j.ToString()));
        }
    }
}
