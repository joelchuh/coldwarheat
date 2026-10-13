using System;
using System.IO;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class DefconLoaderTests
    {
        public static string Source()=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Rulesets/deluxe-2015/defcon.json"));
        [Test] public void Actual_data_binds_approved_map_and_drives_service()
        {
            var map=DefinitionTests.World();var d=DefconDefinitionLoader.LoadSubset(Source(),map);
            Assert.That(d.Map,Is.SameAs(map));Assert.That(d.InitialLevel,Is.EqualTo(5));Assert.That(d.MaximumLevel,Is.EqualTo(5));Assert.That(d.NuclearLevel,Is.EqualTo(1));
            Assert.That(d.TurnStartImprovement,Is.EqualTo(1));Assert.That(d.BattlegroundCoupDegradation,Is.EqualTo(1));
            var s=new DefconState(d,3);
            Assert.That(DefconService.CheckRestriction(s,DefconActionKind.Coup,new CountryId("country.thailand")).Allowed,Is.False);
            Assert.That(DefconService.CheckRestriction(s,DefconActionKind.Coup,new CountryId("country.iran")).Allowed,Is.True);
            Assert.That(DefconService.PlanChange(s,DefconChangeKind.BattlegroundCoupDegradation,PlayerSide.US).After.Level,Is.EqualTo(2));
        }
        [Test] public void Supported_numeric_values_are_consumed_not_replaced_by_constants()
        {
            var json=JObject.Parse(Source());json["turn_start_improvement"]=2;json["battleground_coup_degradation"]=2;json["initial_level"]=4;
            var d=DefconDefinitionLoader.LoadSubset(json.ToString(),DefinitionTests.World());Assert.That(d.InitialLevel,Is.EqualTo(4));
            Assert.That(DefconService.PlanChange(new DefconState(d,2),DefconChangeKind.TurnStartImprovement).After.Level,Is.EqualTo(4));
            Assert.That(DefconService.PlanChange(new DefconState(d,4),DefconChangeKind.BattlegroundCoupDegradation,PlayerSide.USSR).After.Level,Is.EqualTo(2));
        }
        [TestCase("schema")] [TestCase("version")] [TestCase("id")] [TestCase("status")]
        [TestCase("binding")] [TestCase("missing-field")] [TestCase("extra-field")] [TestCase("scope-type")]
        [TestCase("source-id")] [TestCase("source-url")] [TestCase("source-date")] [TestCase("source-section")]
        [TestCase("extra-source-field")] [TestCase("string-int")] [TestCase("float-int")] [TestCase("huge-int")]
        [TestCase("zero-step")] [TestCase("initial-nuclear")] [TestCase("missing-level")] [TestCase("nuclear-table")]
        [TestCase("noncanonical-level")] [TestCase("unknown-region")] [TestCase("duplicate-region")]
        [TestCase("unknown-action")] [TestCase("duplicate-action")] [TestCase("missing-action")]
        [TestCase("region-type")] [TestCase("null-table")] [TestCase("missing-source")]
        public void Corrupt_or_unsupported_data_is_rejected(string mutation)
        {
            var j=JObject.Parse(Source());var source=(JObject)j["source"];var table=(JObject)j["blocked_regions_by_level"];
            switch(mutation)
            {
                case "schema":j["schema_version"]=2;break;case "version":j["content_version"]="0.3.0";break;
                case "id":j["id"]="other";break;case "status":j["status"]="pending";break;
                case "binding":j["region_binding_status"]="unknown";break;case "missing-field":j.Remove("maximum_level");break;
                case "extra-field":j["ignore_defcon"]=true;break;case "scope-type":j["scope"]=1;break;
                case "source-id":source["id"]="UNKNOWN";break;case "source-url":source["url"]="https://example.org/rules";break;
                case "source-date":source["checked_on"]="2026-99-99";break;case "source-section":source["sections"]=new JArray("unknown");break;
                case "extra-source-field":source["verified"]=true;break;case "string-int":j["initial_level"]="5";break;
                case "float-int":j["initial_level"]=5.0;break;case "huge-int":j["maximum_level"]=long.MaxValue;break;
                case "zero-step":j["turn_start_improvement"]=0;break;case "initial-nuclear":j["initial_level"]=1;break;
                case "missing-level":table.Remove("3");break;case "nuclear-table":table["1"]=new JArray();break;
                case "noncanonical-level":table["02"]=table["2"];table.Remove("2");break;
                case "unknown-region":table["4"]=new JArray("region.unknown");break;
                case "duplicate-region":table["4"]=new JArray("region.europe","region.europe");break;
                case "unknown-action":j["restricted_actions"]=new JArray("Coup","WarEvent");break;
                case "duplicate-action":j["restricted_actions"]=new JArray("Coup","Coup");break;
                case "missing-action":j["restricted_actions"]=new JArray("Coup");break;
                case "region-type":table["4"]=new JArray(1);break;case "null-table":j["blocked_regions_by_level"]=null;break;
                case "missing-source":j.Remove("source");break;
            }
            Assert.That(()=>DefconDefinitionLoader.LoadSubset(j.ToString(),DefinitionTests.World()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Duplicate_json_keys_and_null_inputs_are_rejected()
        {
            var duplicate=Source().Replace("\"initial_level\": 5","\"initial_level\": 5, \"initial_level\": 4");
            Assert.That(duplicate,Is.Not.EqualTo(Source()));
            Assert.That(()=>DefconDefinitionLoader.LoadSubset(duplicate,DefinitionTests.World()),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>DefconDefinitionLoader.LoadSubset("[]",DefinitionTests.World()),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>DefconDefinitionLoader.LoadSubset(null,DefinitionTests.World()),Throws.InstanceOf<ArgumentException>());
            Assert.Throws<ArgumentNullException>(()=>DefconDefinitionLoader.LoadSubset(Source(),null));
        }
    }
}
