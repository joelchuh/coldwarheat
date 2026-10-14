using System;
using System.IO;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class CoupLoaderTests
    {
        static string Source()=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Rulesets/deluxe-2015/coup.json"));
        [Test]
        public void Formal_data_map_and_ops_quote_form_verified_influence_calculation()
        {
            var definition=CoupDefinitionLoader.LoadSubset(Source());
            Assert.That(definition.DieMinimum,Is.EqualTo(1));Assert.That(definition.DieMaximum,Is.EqualTo(6));Assert.That(definition.StabilityMultiplier,Is.EqualTo(2));
            var policy=OpsGeneralDefinitionLoader.LoadSubset(File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Cards/deluxe-2015/persistent-effects-batch1.parameters.json")),File.ReadAllText(Path.Combine(DefinitionTests.Root,"Docs/Contracts/persistent-effects-batch1.schema.json")));
            var quote=OpsGeneralRules.QuoteCard(policy,OpsGeneralTests.Card(2),PlayerSide.USSR,OpsValuePurpose.Coup,new[]{new OpsFlatModifier("plus",PlayerSide.USSR,1)});
            // No regional bonus/effect interception in this fixture; host provides the final strength explicitly.
            var state=new DefconState(DefconTests.Definition(),5);
            var result=CoupRules.Calculate(definition,state,new CountryId("country.iran"),new CountryState(2,0),quote.Actor,quote.GeneralValue,4,Array.Empty<string>());
            Assert.That(result.Margin,Is.EqualTo(3));Assert.That(result.After.US,Is.Zero);Assert.That(result.After.USSR,Is.EqualTo(1));
            var change=DefconService.PlanChange(state,DefconChangeKind.BattlegroundCoupDegradation,PlayerSide.US);
            Assert.That(result.RequiresBattlegroundDegradation,Is.True);Assert.That(change.After.Level,Is.EqualTo(4));Assert.That(state.Level,Is.EqualTo(5));
        }
        [Test]
        public void Failed_legal_battleground_coup_still_signals_degradation_and_root_responsibility()
        {
            var definition=CoupDefinitionLoader.LoadSubset(Source());var state=new DefconState(DefconTests.Definition(),2);
            var result=CoupRules.Calculate(definition,state,new CountryId("country.cuba"),new CountryState(1,0),PlayerSide.USSR,1,1,Array.Empty<string>());
            Assert.That(result.Removed,Is.Zero);Assert.That(result.Added,Is.Zero);Assert.That(result.RequiresBattlegroundDegradation,Is.True);
            var plan=DefconService.PlanChange(state,DefconChangeKind.BattlegroundCoupDegradation,PlayerSide.US);
            Assert.That(plan.NuclearWar,Is.True);Assert.That(plan.PhasingPlayer,Is.EqualTo(PlayerSide.US));Assert.That(result.Actor,Is.EqualTo(PlayerSide.USSR));
        }
        [Test]
        public void Loaded_synthetic_values_drive_calculation_without_production_constant_copy()
        {
            var j=JObject.Parse(Source());j["die"]["minimum"]=2;j["die"]["maximum"]=8;j["stability_multiplier"]=3;
            var d=CoupDefinitionLoader.LoadSubset(j.ToString());Assert.That(d.DieMinimum,Is.EqualTo(2));
            var c=CoupRules.Calculate(d,CoupCalculationTests.State(),new CountryId("country.test"),new CountryState(0,1),PlayerSide.US,2,8,Array.Empty<string>());
            Assert.That(c.Margin,Is.EqualTo(4));Assert.That(c.Added,Is.EqualTo(3));
        }
        [Test]
        public void Source_section_order_not_significant()
        {
            var j=JObject.Parse(Source());j["source"]["sections"]=new JArray("6.3.2","6.3.1");
            Assert.That(CoupDefinitionLoader.LoadSubset(j.ToString()).StabilityMultiplier,Is.EqualTo(2));
        }
        [TestCase("schema")] [TestCase("version")] [TestCase("id")] [TestCase("status")]
        [TestCase("sourceId")] [TestCase("sourceUrl")] [TestCase("date")] [TestCase("section")] [TestCase("duplicateSection")] [TestCase("missingSection")]
        [TestCase("extra")] [TestCase("missing")] [TestCase("sourceExtra")] [TestCase("dieExtra")]
        [TestCase("float")] [TestCase("string")] [TestCase("overflow")] [TestCase("negative")] [TestCase("bounds")] [TestCase("multiplier")]
        public void Unsupported_or_corrupt_rule_file_is_rejected(string defect)
        {
            var j=JObject.Parse(Source());
            switch(defect)
            {
                case "schema":j["schema_version"]=2;break;case "version":j["content_version"]="2.0.0";break;
                case "id":j["id"]="other";break;case "status":j["status"]="pending";break;
                case "sourceId":j["source"]["id"]="other";break;case "sourceUrl":j["source"]["url"]="https://example.com";break;
                case "date":j["source"]["checked_on"]="2026-13-01";break;case "section":j["source"]["sections"]=new JArray("6.3.1","other");break;
                case "duplicateSection":j["source"]["sections"]=new JArray("6.3.1","6.3.1");break;case "missingSection":j["source"]["sections"]=new JArray("6.3.1");break;
                case "extra":j["extra"]=0;break;case "missing":j.Remove("die");break;case "sourceExtra":j["source"]["extra"]=0;break;case "dieExtra":j["die"]["extra"]=0;break;
                case "float":j["die"]["minimum"]=1.0;break;case "string":j["die"]["maximum"]="6";break;case "overflow":j["die"]["maximum"]=2147483648L;break;
                case "negative":j["die"]["minimum"]=-1;break;case "bounds":j["die"]["minimum"]=7;break;case "multiplier":j["stability_multiplier"]=0;break;
            }
            Assert.That(()=>CoupDefinitionLoader.LoadSubset(j.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase(null)] [TestCase("")] [TestCase("[]")] [TestCase(@"{""id"":1,""id"":2}")]
        public void Invalid_root_and_duplicate_json_rejected(string json)=>Assert.That(()=>CoupDefinitionLoader.LoadSubset(json),Throws.InstanceOf<ArgumentException>());
    }
}
