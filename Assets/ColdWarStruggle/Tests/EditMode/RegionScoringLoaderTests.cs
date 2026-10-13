using System;
using System.IO;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class RegionScoringLoaderTests
    {
        public static string Json=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Rulesets/deluxe-2015/region-scoring.json"));
        public static string Schema=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Docs/Contracts/region-scoring-rules.schema.json"));
        [Test] public void Actual_shared_rules_bind_all_regions_and_weights()
        {
            var map=DefinitionTests.World();var d=RegionScoringDefinitionLoader.LoadSubset(Json,Schema,map);
            Assert.That(d.Map,Is.SameAs(map));Assert.That(d.Regions.Count,Is.EqualTo(6));Assert.That(d.CountryWeights.Count,Is.EqualTo(7));
            Assert.That(d.Regions.Single(t=>t.Id=="region.europe").Control,Is.Null);Assert.That(d.Regions.Single(t=>t.Id=="region.asia").Control,Is.EqualTo(9));
            Assert.That(d.Regions.Single(t=>t.Id=="region.middle_east").Domination,Is.EqualTo(5));
            Assert.That(d.CountryWeights[new CountryId("country.thailand")],Is.EqualTo(2));Assert.That(d.BattlegroundBonus,Is.EqualTo(1));
        }
        [Test] public void Numeric_values_come_from_shared_data_not_a_second_runtime_table()
        {
            var j=JObject.Parse(Json);j["regions"][1]["base_vp"]["presence"]=6;j["bonus_vp"]["controlled_battleground"]=2;
            var map=DefinitionTests.World();var d=RegionScoringDefinitionLoader.LoadSubset(j.ToString(),Schema,map);var state=RegionScoringTests.Empty(map);
            RegionScoringTests.Control(map,state,PlayerSide.US,"country.thailand");
            var result=RegionScoringService.CalculateUnmodified(d,RegionScoringTests.Snapshot(map,state),"region.asia");Assert.That(result.US.TotalVp,Is.EqualTo(8));
        }
        [TestCase("version")] [TestCase("id")] [TestCase("status")] [TestCase("contentVersion")] [TestCase("extra")]
        [TestCase("missingRegion")] [TestCase("duplicateRegion")] [TestCase("unknownRegion")] [TestCase("europeNumeric")] [TestCase("asiaNull")]
        [TestCase("negativeBase")] [TestCase("floatBase")] [TestCase("largeBase")] [TestCase("noneNonzero")] [TestCase("wrongKind")] [TestCase("standardNotFinal")]
        [TestCase("negativeBonus")] [TestCase("booleanBonus")] [TestCase("seaStandardBonus")] [TestCase("seaFinal")] [TestCase("seaParent")] [TestCase("seaId")]
        [TestCase("missingWeight")] [TestCase("duplicateWeight")] [TestCase("unknownWeight")] [TestCase("zeroWeight")] [TestCase("floatWeight")] [TestCase("largeWeight")]
        [TestCase("missingSource")] [TestCase("duplicateSource")] [TestCase("unknownSource")] [TestCase("sourceUrl")] [TestCase("sourceScalar")] [TestCase("sourcesObject")]
        public void Corrupt_subset_rejected(string mutation)
        {
            var j=JObject.Parse(Json);var regions=(JArray)j["regions"];var weights=(JArray)j["southeast_asia"]["country_vp"];var sources=(JArray)j["sources"];
            switch(mutation) {
                case "version":j["schema_version"]=2;break;case "id":j["id"]="wrong";break;case "status":j["status"]="pending";break;case "contentVersion":j["content_version"]="9";break;case "extra":j["extra"]=1;break;
                case "missingRegion":regions.RemoveAt(0);break;case "duplicateRegion":regions[1]=regions[0].DeepClone();break;case "unknownRegion":regions[1]["id"]="region.fake";break;
                case "europeNumeric":regions[0]["base_vp"]["control"]=100;break;case "asiaNull":regions[1]["base_vp"]["control"]=null;break;
                case "negativeBase":regions[1]["base_vp"]["presence"]=-1;break;case "floatBase":regions[1]["base_vp"]["presence"]=3.0;break;case "largeBase":regions[1]["base_vp"]["presence"]=(long)int.MaxValue+1;break;
                case "noneNonzero":regions[1]["base_vp"]["none"]=1;break;case "wrongKind":regions[1]["scoring_kind"]="WeightedControlledCountries";break;case "standardNotFinal":regions[1]["include_in_final"]=false;break;
                case "negativeBonus":j["bonus_vp"]["controlled_battleground"]=-1;break;case "booleanBonus":j["bonus_vp"]["controlled_battleground"]=true;break;
                case "seaStandardBonus":j["southeast_asia"]["standard_region_bonuses"]=true;break;case "seaFinal":j["southeast_asia"]["include_in_final"]=true;break;
                case "seaParent":j["southeast_asia"]["parent_region_id"]="region.africa";break;case "seaId":j["southeast_asia"]["id"]="region.fake";break;
                case "missingWeight":weights.RemoveAt(0);break;case "duplicateWeight":weights.Add(weights[0].DeepClone());break;case "unknownWeight":weights[0]["country_id"]="country.fake";break;
                case "zeroWeight":weights[0]["vp"]=0;break;case "floatWeight":weights[0]["vp"]=1.0;break;case "largeWeight":weights[0]["vp"]=(long)int.MaxValue+1;break;
                case "missingSource":sources.RemoveAt(0);break;case "duplicateSource":sources.Add(sources[0].DeepClone());break;case "unknownSource":sources[0]["id"]="PROJECT-UNVERIFIED";break;
                case "sourceUrl":sources[0]["url"]="https://example.invalid/rules";break;case "sourceScalar":sources[0]="R2015";break;case "sourcesObject":j["sources"]=new JObject();break;
            }
            Assert.That(()=>RegionScoringDefinitionLoader.LoadSubset(j.ToString(),Schema,DefinitionTests.World()),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("malformed")] [TestCase("duplicateKey")] [TestCase("trailing")] [TestCase("wrongContract")] [TestCase("unsupportedKeyword")] [TestCase("nullMap")]
        public void Invalid_input_and_contract_rejected(string kind)
        {
            string json=Json,schema=Schema;var map=DefinitionTests.World();
            switch(kind) {
                case "malformed":json="{broken";break;case "duplicateKey":json=json.Replace("\"schema_version\": 1","\"schema_version\": 1, \"schema_version\": 1");break;
                case "trailing":json+=" {}";break;case "wrongContract":schema=schema.Replace("urn:coldwarheat:contract:region-scoring-rules:1","wrong");break;
                case "unsupportedKeyword":var j=JObject.Parse(schema);j["unevaluatedProperties"]=false;schema=j.ToString();break;case "nullMap":map=null;break;
            }
            Assert.That(()=>RegionScoringDefinitionLoader.LoadSubset(json,schema,map),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)]
        public void Europe_control_is_consumed_only_by_explicit_resolved_scoring_evidence(PlayerSide side)
        {
            var map=DefinitionTests.World();var d=RegionScoringDefinitionLoader.LoadSubset(Json,Schema,map);var state=RegionScoringTests.Empty(map);
            RegionScoringTests.Control(map,state,side,map.GetCountriesInRegion("region.europe").Where(c=>c.Battleground).Select(c=>c.Id.Value).ToArray());
            var snapshot=RegionScoringTests.Snapshot(map,state);var tiers=RegionScoringService.InspectActualRegionTiers(d,snapshot);
            Assert.That(tiers.Count,Is.EqualTo(6));var ordinary=VictoryService.Evaluate(VictoryTests.Definition(),new ClosedVpBatchEvidence("other","event",0,0,0));Assert.That(ordinary.Outcome,Is.EqualTo(VictoryOutcome.Continue));
            var result=RegionScoringService.CalculateUnmodified(d,snapshot,"region.europe");var e=new ScoringResolvedEvidence("scoring","R2015.10.1",3,result);
            var v=VictoryService.Evaluate(VictoryTests.Definition(),e);Assert.That(v.Winner,Is.EqualTo(side));Assert.That(v.SignedVp,Is.EqualTo(3));Assert.That(v.Checkpoint,Is.EqualTo(VictoryCheckpoint.ScoringResolved));Assert.That(e.Result,Is.SameAs(result));
        }
        [TestCase(-19,-18,null)] [TestCase(19,20,"US")]
        public void Complete_net_scoring_prevents_intermediate_threshold_and_can_finish(int before,int after,string winner)
        {
            var map=DefinitionTests.World();var state=RegionScoringTests.Empty(map);RegionScoringTests.Control(map,state,PlayerSide.US,"country.thailand","country.vietnam");RegionScoringTests.Control(map,state,PlayerSide.USSR,"country.laos_cambodia","country.malaysia");
            var result=RegionScoringService.CalculateUnmodified(RegionScoringDefinitionLoader.LoadSubset(Json,Schema,map),RegionScoringTests.Snapshot(map,state),"region.southeast_asia");
            var v=VictoryService.Evaluate(VictoryTests.Definition(),new ScoringResolvedEvidence("score","SEA",before,result));Assert.That(v.SignedVp,Is.EqualTo(after));Assert.That(v.Winner,Is.EqualTo(VictoryTests.Side(winner)));
        }
        [TestCase(20)] [TestCase(-20)]
        public void Already_terminal_score_cannot_start_region_scoring(int before)
        {
            var map=DefinitionTests.World();var result=RegionScoringService.CalculateUnmodified(RegionScoringTests.Definition(map),RegionScoringTests.Snapshot(map,RegionScoringTests.Empty(map)),"region.asia");
            Assert.Throws<ArgumentException>(()=>VictoryService.Evaluate(VictoryTests.Definition(),new ScoringResolvedEvidence("score","rule",before,result)));
        }
        [Test] public void Missing_actual_result_is_not_evidence()=>Assert.That(()=>new ScoringResolvedEvidence("score","rule",0,null),Throws.InstanceOf<ArgumentException>());
    }
}
