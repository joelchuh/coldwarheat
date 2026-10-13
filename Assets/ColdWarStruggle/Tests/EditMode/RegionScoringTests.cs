using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class RegionScoringTests
    {
        public static List<RegionScoreTable> Tables()=>new List<RegionScoreTable> {
            new RegionScoreTable("region.europe",3,7,null),new RegionScoreTable("region.asia",3,7,9),
            new RegionScoreTable("region.middle_east",3,5,7),new RegionScoreTable("region.africa",1,4,6),
            new RegionScoreTable("region.central_america",1,3,5),new RegionScoreTable("region.south_america",2,5,6) };
        public static Dictionary<CountryId,int> Weights(MapDefinition map)=>map.GetCountriesInRegion("region.southeast_asia").ToDictionary(c=>c.Id,c=>c.Id.Value=="country.thailand"?2:1);
        public static RegionScoringDefinition Definition(MapDefinition map,int bg=1,int adj=1)=>new RegionScoringDefinition(map,bg,adj,Tables(),"region.southeast_asia","region.asia",Weights(map));
        public static Dictionary<CountryId,CountryState> Empty(MapDefinition map)=>map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));
        public static void Control(MapDefinition map,Dictionary<CountryId,CountryState> state,PlayerSide side,params string[] ids)
        { foreach(string id in ids) { var c=map.GetCountry(new CountryId(id));state[c.Id]=side==PlayerSide.US?new CountryState(c.Stability,0):new CountryState(0,c.Stability); } }
        public static ScoringSnapshot Snapshot(MapDefinition map,Dictionary<CountryId,CountryState> state,IEnumerable<string> effects=null)
            =>new ScoringSnapshot(map,"scoring.fixture",7,state,effects??Array.Empty<string>());
        [TestCase(0,0,0,0,RegionTier.None)] [TestCase(0,2,0,0,RegionTier.Presence)]
        [TestCase(1,0,0,0,RegionTier.Presence)] [TestCase(1,1,0,0,RegionTier.Domination)]
        [TestCase(1,1,1,0,RegionTier.Presence)] [TestCase(2,0,1,0,RegionTier.Presence)]
        [TestCase(3,0,0,2,RegionTier.Control)] [TestCase(3,0,0,3,RegionTier.Presence)]
        [TestCase(1,1,0,3,RegionTier.Presence)] [TestCase(1,1,2,0,RegionTier.Presence)]
        [TestCase(2,1,1,1,RegionTier.Domination)] [TestCase(3,1,0,0,RegionTier.Control)]
        public void Strict_tier_boundaries_and_control_without_non_battleground(int usBg,int usOther,int sovBg,int sovOther,RegionTier expected)
        {
            var map=DefinitionTests.World();var state=Empty(map);var countries=map.GetCountriesInRegion("region.central_america");
            var bg=countries.Where(c=>c.Battleground).ToArray();var other=countries.Where(c=>!c.Battleground).ToArray();
            Control(map,state,PlayerSide.US,bg.Take(usBg).Concat(other.Take(usOther)).Select(c=>c.Id.Value).ToArray());
            Control(map,state,PlayerSide.USSR,bg.Skip(usBg).Take(sovBg).Concat(other.Skip(usOther).Take(sovOther)).Select(c=>c.Id.Value).ToArray());
            var result=RegionScoringService.CalculateUnmodified(Definition(map),Snapshot(map,state),"region.central_america");
            Assert.That(result.US.Tier,Is.EqualTo(expected));Assert.That(result.Facts.US.Total,Is.EqualTo(usBg+usOther));
            Assert.That(result.Facts.US.NonBattlegrounds,Is.EqualTo(usOther));Assert.That(result.Facts.TotalBattlegrounds,Is.EqualTo(3));
        }
        [Test] public void Official_central_america_example_nets_minus_four_with_cuba_double_bonus()
        {
            var map=DefinitionTests.World();var state=Empty(map);
            Control(map,state,PlayerSide.USSR,"country.cuba","country.haiti","country.dominican_republic");Control(map,state,PlayerSide.US,"country.guatemala");
            var r=RegionScoringService.CalculateUnmodified(Definition(map),Snapshot(map,state),"region.central_america");
            Assert.That(r.US.TotalVp,Is.EqualTo(1));Assert.That(r.USSR.BaseVp,Is.EqualTo(3));Assert.That(r.USSR.BattlegroundVp,Is.EqualTo(1));
            Assert.That(r.USSR.EnemyAdjacencyVp,Is.EqualTo(1));Assert.That(r.USSR.TotalVp,Is.EqualTo(5));Assert.That(r.NetVpForUS,Is.EqualTo(-4));
            Assert.That(r.SnapshotId,Is.EqualTo("scoring.fixture"));Assert.That(r.StateVersion,Is.EqualTo(7));
        }
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)]
        public void Europe_control_has_independent_signal_without_numeric_total(PlayerSide side)
        {
            var map=DefinitionTests.World();var state=Empty(map);Control(map,state,side,map.GetCountriesInRegion("region.europe").Where(c=>c.Battleground).Select(c=>c.Id.Value).ToArray());
            var d=Definition(map);var s=Snapshot(map,state);var r=RegionScoringService.CalculateUnmodified(d,s,"region.europe");
            Assert.That(r.EuropeControlWinner,Is.EqualTo(side));Assert.That(r.NetVpForUS,Is.Null);
            var winner=side==PlayerSide.US?r.US:r.USSR;Assert.That(winner.BaseVp,Is.Null);Assert.That(winner.TotalVp,Is.Null);
            var query=RegionScoringService.InspectActualRegionTiers(d,s);Assert.That(query.Count,Is.EqualTo(6));
            Assert.That(state.Count,Is.EqualTo(84));Assert.That(query.Single(x=>x.RegionId=="region.europe").UsTier,Is.EqualTo(side==PlayerSide.US?RegionTier.Control:RegionTier.None));
        }
        [Test] public void Southeast_asia_weights_do_not_stack_with_standard_bonuses_or_asia()
        {
            var map=DefinitionTests.World();var state=Empty(map);Control(map,state,PlayerSide.US,"country.thailand","country.vietnam");Control(map,state,PlayerSide.USSR,"country.laos_cambodia","country.malaysia");
            var d=Definition(map);var s=Snapshot(map,state);var sea=RegionScoringService.CalculateUnmodified(d,s,"region.southeast_asia");
            Assert.That(sea.US.TotalVp,Is.EqualTo(3));Assert.That(sea.USSR.TotalVp,Is.EqualTo(2));Assert.That(sea.NetVpForUS,Is.EqualTo(1));
            Assert.That(sea.US.Tier,Is.Null);Assert.That(sea.Facts,Is.Null);Assert.That(sea.US.BattlegroundVp,Is.Zero);Assert.That(sea.US.EnemyAdjacencyVp,Is.Zero);
            var asia=RegionScoringService.CalculateUnmodified(d,s,"region.asia");Assert.That(asia.US.TotalVp,Is.EqualTo(4));Assert.That(asia.USSR.TotalVp,Is.EqualTo(3));Assert.That(asia.US.WeightedVp,Is.Zero);
        }
        [Test] public void Overlapping_europe_tags_count_once_and_influence_needs_control_margin()
        {
            var map=DefinitionTests.World();var state=Empty(map);Control(map,state,PlayerSide.US,"country.austria","country.finland");
            var france=new CountryId("country.france");state[france]=new CountryState(map.GetCountry(france).Stability,1);
            var r=RegionScoringService.CalculateUnmodified(Definition(map),Snapshot(map,state),"region.europe");Assert.That(r.Facts.US.Total,Is.EqualTo(2));
        }
        [Test] public void Scoring_rejects_active_modifiers_but_actual_tier_query_ignores_them()
        {
            var map=DefinitionTests.World();var state=Empty(map);Control(map,state,PlayerSide.US,"country.taiwan");
            var d=Definition(map);var s=Snapshot(map,state,new[]{"effect.formosan_resolution","effect.shuttle_diplomacy"});
            Assert.Throws<ArgumentException>(()=>RegionScoringService.CalculateUnmodified(d,s,"region.asia"));
            var r=RegionScoringService.InspectActualRegionTiers(d,s).Single(x=>x.RegionId=="region.asia");Assert.That(r.US.Battlegrounds,Is.Zero);Assert.That(s.ActiveScoringEffects.Count,Is.EqualTo(2));
        }
        [Test] public void Definitions_snapshots_and_queries_are_copied_and_readonly()
        {
            var map=DefinitionTests.World();var state=Empty(map);var tables=Tables();var weights=Weights(map);var effects=new List<string>();
            var d=new RegionScoringDefinition(map,1,1,tables,"region.southeast_asia","region.asia",weights);var s=Snapshot(map,state,effects);
            tables.Clear();weights.Clear();state.Clear();effects.Add("later");Assert.That(d.Regions.Count,Is.EqualTo(6));Assert.That(d.CountryWeights.Count,Is.EqualTo(7));Assert.That(s.Countries.Count,Is.EqualTo(84));Assert.That(s.ActiveScoringEffects,Is.Empty);
            Assert.Throws<NotSupportedException>(()=>((IList<RegionScoreTable>)d.Regions).Clear());Assert.Throws<NotSupportedException>(()=>((IDictionary<CountryId,int>)d.CountryWeights).Clear());
            Assert.Throws<NotSupportedException>(()=>((IDictionary<CountryId,CountryState>)s.Countries).Clear());Assert.Throws<NotSupportedException>(()=>((IList<string>)s.ActiveScoringEffects).Clear());
            Assert.Throws<NotSupportedException>(()=>((IList<ActualRegionTiers>)RegionScoringService.InspectActualRegionTiers(d,s)).Clear());
        }
        [TestCase("missing")] [TestCase("unknown")] [TestCase("nullCountries")] [TestCase("nullEffects")] [TestCase("duplicateEffects")]
        [TestCase("emptyEffect")] [TestCase("manyEffects")] [TestCase("version")] [TestCase("id")] [TestCase("nullMap")]
        public void Invalid_snapshot_is_rejected(string mutation)
        {
            var map=DefinitionTests.World();var state=Empty(map);IEnumerable<string> effects=Array.Empty<string>();long version=0;string id="snapshot";
            switch(mutation) {
                case "missing":state.Remove(state.Keys.First());break;case "unknown":state.Add(new CountryId("country.unknown"),new CountryState(0,0));break;
                case "nullCountries":state=null;break;case "nullEffects":effects=null;break;case "duplicateEffects":effects=new[]{"a","a"};break;
                case "emptyEffect":effects=new[]{" "};break;case "manyEffects":effects=Enumerable.Range(0,129).Select(x=>"effect."+x);break;
                case "version":version=-1;break;case "id":id=" ";break;case "nullMap":map=null;break;
            }
            Assert.That(()=>new ScoringSnapshot(map,id,version,state,effects),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("missingRegion")] [TestCase("duplicateRegion")] [TestCase("missingWeight")] [TestCase("duplicateWeight")]
        [TestCase("unknownWeight")] [TestCase("wrongParent")] [TestCase("wrongSubregion")] [TestCase("negativeBonus")]
        [TestCase("zeroWeight")] [TestCase("nullRegions")] [TestCase("nullWeights")] [TestCase("noBattlegrounds")]
        public void Incomplete_or_invalid_definition_is_rejected(string mutation)
        {
            var map=DefinitionTests.World();IEnumerable<RegionScoreTable> tables=Tables();var weights=Weights(map).ToList();string parent="region.asia",region="region.southeast_asia";int bg=1;
            switch(mutation) {
                case "missingRegion":tables=tables.Skip(1);break;case "duplicateRegion":tables=tables.Concat(tables.Take(1));break;
                case "missingWeight":weights.RemoveAt(0);break;case "duplicateWeight":weights.Add(weights[0]);break;
                case "unknownWeight":weights[0]=new KeyValuePair<CountryId,int>(new CountryId("country.unknown"),1);break;
                case "wrongParent":parent="region.africa";break;case "wrongSubregion":region="region.invalid";break;case "negativeBonus":bg=-1;break;
                case "zeroWeight":weights[0]=new KeyValuePair<CountryId,int>(weights[0].Key,0);break;case "nullRegions":tables=null;break;case "nullWeights":weights=null;break;
                case "noBattlegrounds":map=new MapDefinition(map.Countries.Select(c=>new CountryDefinition(c.Id,c.Stability,c.Region,false,c.Subregions)),Array.Empty<KeyValuePair<CountryId,CountryId>>(),new Dictionary<PlayerSide,IReadOnlyList<CountryId>>{{PlayerSide.US,Array.Empty<CountryId>()},{PlayerSide.USSR,Array.Empty<CountryId>()}});break;
            }
            Assert.That(()=>new RegionScoringDefinition(map,bg,1,tables,region,parent,weights),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Unknown_region_map_mismatch_and_overflow_are_rejected()
        {
            var map=DefinitionTests.World();var state=Empty(map);var d=Definition(map);var s=Snapshot(map,state);
            Assert.Throws<ArgumentException>(()=>RegionScoringService.CalculateUnmodified(d,s,"region.western_europe"));
            Assert.Throws<ArgumentException>(()=>RegionScoringService.CalculateUnmodified(Definition(DefinitionTests.World()),s,"region.asia"));
            Control(map,state,PlayerSide.USSR,"country.cuba");Assert.Throws<OverflowException>(()=>RegionScoringService.CalculateUnmodified(Definition(map,int.MaxValue,int.MaxValue),Snapshot(map,state),"region.central_america"));
        }
        [TestCase(-1,1,2,"region.asia")] [TestCase(1,-1,2,"region.asia")] [TestCase(1,2,-1,"region.asia")]
        [TestCase(1,2,null,"region.asia")] [TestCase(1,2,3,"region.europe")] [TestCase(1,2,3,"")]
        public void Invalid_table_rejected(int presence,int domination,int? control,string id)
            =>Assert.That(()=>new RegionScoreTable(id,presence,domination,control),Throws.InstanceOf<ArgumentException>());
    }
}
