using System;
using System.IO;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class FinalScoringLoaderTests
    {
        public static string ChinaJson=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Rulesets/deluxe-2015/scoring-card-parameters.json"));
        public static string ChinaSchema=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Docs/Contracts/scoring-card-parameters.schema.json"));
        static string RegionJson=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Rulesets/deluxe-2015/region-scoring.json"));
        static string RegionSchema=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Docs/Contracts/region-scoring-rules.schema.json"));
        public static FinalScoringDefinition Load(MapDefinition map)=>FinalScoringDefinitionLoader.LoadSubset(RegionJson,RegionSchema,ChinaJson,ChinaSchema,map,CardCatalogLoaderTests.Load());
        [Test] public void Formal_shared_files_bind_six_regions_and_china_reward_to_real_catalog()
        {
            var map=DefinitionTests.World();var d=Load(map);
            Assert.That(d.RegionDefinition.Map,Is.SameAs(map));Assert.That(d.RegionIds.Count,Is.EqualTo(6));Assert.That(d.RegionIds.Contains("region.southeast_asia"),Is.False);
            Assert.That(d.ChinaCardId.Value,Is.EqualTo("card.china"));Assert.That(d.ChinaHolderVp,Is.EqualTo(1));
            var r=FinalScoringService.CalculateUnmodified(d,RegionScoringTests.Snapshot(map,RegionScoringTests.Empty(map)),FinalScoringTests.China());
            Assert.That(r.NetVpForUS,Is.EqualTo(1));
            var v=VictoryService.Evaluate(VictoryTests.Definition(),new FinalScoringCompletedEvidence("final","R2015",-1,r));
            Assert.That(v.Outcome,Is.EqualTo(VictoryOutcome.Finished));Assert.That(v.Winner,Is.Null);
        }
        [TestCase(0)] [TestCase(5)] [TestCase(int.MaxValue)]
        public void Reward_is_loaded_from_shared_parameter_not_a_service_constant(int reward)
        {
            var root=JObject.Parse(ChinaJson);root["cards"][0]["parameters"]["final_holder_vp"]=reward;var map=DefinitionTests.World();
            var d=FinalScoringDefinitionLoader.LoadSubset(RegionJson,RegionSchema,root.ToString(),ChinaSchema,map,CardCatalogLoaderTests.Load());
            var r=FinalScoringService.CalculateUnmodified(d,RegionScoringTests.Snapshot(map,RegionScoringTests.Empty(map)),FinalScoringTests.China(PlayerSide.USSR));
            Assert.That(d.ChinaHolderVp,Is.EqualTo(reward));Assert.That(r.NetVpForUS,Is.EqualTo(-reward));
        }
        [TestCase("schema-version")] [TestCase("dataset-id")] [TestCase("content-version")] [TestCase("status")]
        [TestCase("extra-root")] [TestCase("extra-card")] [TestCase("extra-parameter")] [TestCase("wrong-card")] [TestCase("timing")]
        [TestCase("negative")] [TestCase("fraction")] [TestCase("string")] [TestCase("null")] [TestCase("boolean")] [TestCase("overflow")]
        [TestCase("missing-reward")] [TestCase("duplicate-card")] [TestCase("no-card")]
        [TestCase("missing-source")] [TestCase("duplicate-source")] [TestCase("source-url")] [TestCase("source-id")] [TestCase("source-no-url")]
        public void Invalid_card_reward_contract_is_rejected(string kind)
        {
            var root=JObject.Parse(ChinaJson);var card=(JObject)root["cards"][0];var parameter=(JObject)card["parameters"];var sources=(JArray)root["sources"];
            switch(kind)
            {
                case "schema-version":root["schema_version"]=1;break;case "dataset-id":root["id"]="cards.other";break;
                case "content-version":root["content_version"]="0.1.0";break;case "status":root["status"]="unverified";break;
                case "extra-root":root["unknown"]=1;break;case "extra-card":card["unknown"]=1;break;case "extra-parameter":parameter["unknown"]=1;break;
                case "wrong-card":card["id"]="card.fidel";break;case "timing":card["applies_at"]="AnyTurn";break;
                case "negative":parameter["final_holder_vp"]=-1;break;case "fraction":parameter["final_holder_vp"]=1.5;break;
                case "string":parameter["final_holder_vp"]="1";break;case "null":parameter["final_holder_vp"]=JValue.CreateNull();break;
                case "boolean":parameter["final_holder_vp"]=true;break;case "overflow":parameter["final_holder_vp"]=(long)int.MaxValue+1;break;
                case "missing-reward":parameter.Remove("final_holder_vp");break;case "duplicate-card":((JArray)root["cards"]).Add(card.DeepClone());break;
                case "no-card":((JArray)root["cards"]).Clear();break;case "missing-source":sources.RemoveAt(0);break;
                case "duplicate-source":sources[1]=sources[0].DeepClone();break;case "source-url":sources[0]["url"]="https://example.invalid/not-authority";break;
                case "source-id":sources[0]["id"]="unknown";break;case "source-no-url":((JObject)sources[0]).Remove("url");break;
            }
            Assert.Throws<ArgumentException>(()=>FinalScoringDefinitionLoader.LoadSubset(RegionJson,RegionSchema,root.ToString(),ChinaSchema,DefinitionTests.World(),CardCatalogLoaderTests.Load()));
        }
        [TestCase("missing-region")] [TestCase("duplicate-region")] [TestCase("excluded-standard")] [TestCase("included-sea")] [TestCase("string-flag")]
        public void Shared_region_final_flags_and_complete_set_are_checked(string kind)
        {
            var root=JObject.Parse(RegionJson);var regions=(JArray)root["regions"];
            if(kind=="missing-region")regions.RemoveAt(0);if(kind=="duplicate-region")regions[0]=regions[1].DeepClone();
            if(kind=="excluded-standard")regions[0]["include_in_final"]=false;if(kind=="included-sea")root["southeast_asia"]["include_in_final"]=true;
            if(kind=="string-flag")regions[0]["include_in_final"]="true";
            Assert.Throws<ArgumentException>(()=>FinalScoringDefinitionLoader.LoadSubset(root.ToString(),RegionSchema,ChinaJson,ChinaSchema,DefinitionTests.World(),CardCatalogLoaderTests.Load()));
        }
        [Test] public void Missing_catalog_china_is_rejected_instead_of_fabricating_it()
        {
            var cards=new CardCatalog(CardCatalogLoaderTests.Load().Cards.Where(c=>c.Kind!=CardKind.China));
            Assert.Throws<ArgumentException>(()=>FinalScoringDefinitionLoader.LoadSubset(RegionJson,RegionSchema,ChinaJson,ChinaSchema,DefinitionTests.World(),cards));
        }
        [Test] public void Invalid_json_duplicate_properties_and_wrong_contract_ids_fail_closed()
        {
            var map=DefinitionTests.World();var cards=CardCatalogLoaderTests.Load();
            foreach(var json in new[]{"null","[]","{",ChinaJson.Replace("\"final_holder_vp\": 1","\"final_holder_vp\": 1, \"final_holder_vp\": 2")})
                Assert.Throws<ArgumentException>(()=>FinalScoringDefinitionLoader.LoadSubset(RegionJson,RegionSchema,json,ChinaSchema,map,cards));
            var schema=JObject.Parse(ChinaSchema);schema["$id"]="urn:other";
            Assert.Throws<ArgumentException>(()=>FinalScoringDefinitionLoader.LoadSubset(RegionJson,RegionSchema,ChinaJson,schema.ToString(),map,cards));
            Assert.Throws<ArgumentException>(()=>FinalScoringDefinitionLoader.LoadSubset(RegionJson,ChinaSchema,ChinaJson,ChinaSchema,map,cards));
            Assert.Throws<ArgumentNullException>(()=>FinalScoringDefinitionLoader.LoadSubset(RegionJson,RegionSchema,ChinaJson,ChinaSchema,null,cards));
            Assert.Throws<ArgumentNullException>(()=>FinalScoringDefinitionLoader.LoadSubset(RegionJson,RegionSchema,ChinaJson,ChinaSchema,map,null));
        }
    }
}
