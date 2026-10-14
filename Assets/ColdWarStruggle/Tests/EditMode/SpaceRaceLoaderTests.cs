using System;
using System.IO;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class SpaceRaceLoaderTests
    {
        static string Source() => File.ReadAllText(Path.Combine(DefinitionTests.Root, "Data/Rulesets/deluxe-2015/space-race.json"));
        [Test]
        public void Actual_shared_data_loads_all_steps_and_abilities()
        {
            var definition = SpaceRaceDefinitionLoader.LoadSubset(Source());
            Assert.That(definition.InitialPosition, Is.Zero); Assert.That(definition.FinalPosition, Is.EqualTo(8));
            Assert.That(definition.BaseAttemptsPerTurn, Is.EqualTo(1));
            Assert.That(definition.DieMinimum, Is.EqualTo(1)); Assert.That(definition.DieMaximum, Is.EqualTo(6));
            Assert.That(definition.Spaces.Select(s => s.MinimumEffectiveOps), Is.EqualTo(new[] {2,2,2,2,3,3,3,4}));
            Assert.That(definition.Spaces.Select(s => s.SuccessMinimum), Is.EqualTo(Enumerable.Repeat(1,8)));
            Assert.That(definition.Spaces.Select(s => s.SuccessMaximum), Is.EqualTo(new[] {3,4,3,4,3,4,3,2}));
            Assert.That(definition.Spaces.Select(s => s.FirstVp), Is.EqualTo(new[] {2,0,2,0,3,0,4,2}));
            Assert.That(definition.Spaces.Select(s => s.SecondVp), Is.EqualTo(new[] {1,0,0,0,1,0,2,0}));
            var result = SpaceAbilityRules.Query(new SpaceTrackState(definition,8,0),PlayerSide.US);
            Assert.That(result.AttemptLimit,Is.EqualTo(2)); Assert.That(result.EndTurnDiscardMaximum,Is.EqualTo(1));
            Assert.That(result.ActionRoundAllowance,Is.EqualTo(8)); Assert.That(result.OpponentRevealsHeadlineFirst,Is.True);
        }
        [Test]
        public void Numeric_parameters_come_from_data_not_a_second_rule_table()
        {
            var j=JObject.Parse(Source()); j["base_attempts_per_turn"]=2; j["die"]["maximum"]=9;
            j["spaces"][0]["min_effective_ops"]=5; j["spaces"][0]["success_roll"]["max"]=7; j["spaces"][0]["vp"]["first"]=6;
            j["abilities"][0]["value"]=3; j["abilities"][2]["value"]=2; j["abilities"][3]["value"]=9;
            var definition=SpaceRaceDefinitionLoader.LoadSubset(j.ToString());
            Assert.That(definition.DieMaximum,Is.EqualTo(9)); Assert.That(definition.GetSpace(1).MinimumEffectiveOps,Is.EqualTo(5));
            Assert.That(definition.GetSpace(1).SuccessMaximum,Is.EqualTo(7)); Assert.That(definition.GetSpace(1).FirstVp,Is.EqualTo(6));
            Assert.That(SpaceAbilityRules.Query(new SpaceTrackState(definition,0,0),PlayerSide.US).AttemptLimit,Is.EqualTo(2));
            var result=SpaceAbilityRules.Query(new SpaceTrackState(definition,8,0),PlayerSide.US);
            Assert.That(result.AttemptLimit,Is.EqualTo(3)); Assert.That(result.EndTurnDiscardMaximum,Is.EqualTo(2)); Assert.That(result.ActionRoundAllowance,Is.EqualTo(9));
        }
        [Test]
        public void Source_and_ability_order_do_not_change_identity()
        {
            var j=JObject.Parse(Source());
            j["sources"]=new JArray(((JArray)j["sources"]).Reverse().Select(x=>x.DeepClone()));
            j["abilities"]=new JArray(((JArray)j["abilities"]).Reverse().Select(x=>x.DeepClone()));
            Assert.That(SpaceAbilityRules.Query(new SpaceTrackState(SpaceRaceDefinitionLoader.LoadSubset(j.ToString()),2,0),PlayerSide.US).AttemptLimit,Is.EqualTo(2));
        }
        [TestCase("schema")] [TestCase("version")] [TestCase("id")] [TestCase("status")] [TestCase("description")] [TestCase("missing")] [TestCase("extra")]
        [TestCase("reset")] [TestCase("advance")] [TestCase("active")] [TestCase("catalog")] [TestCase("sourceCount")] [TestCase("sourceId")] [TestCase("sourceUrl")] [TestCase("date")] [TestCase("sections")] [TestCase("pages")] [TestCase("sourceExtra")] [TestCase("mapUrl")] [TestCase("listedBy")] [TestCase("scope")]
        [TestCase("initial")] [TestCase("base")] [TestCase("die")] [TestCase("dieExtra")] [TestCase("stepCount")] [TestCase("stepOrder")] [TestCase("stepId")] [TestCase("stepExtra")] [TestCase("stepPosition")] [TestCase("ops")] [TestCase("success")] [TestCase("vp")] [TestCase("ref")] [TestCase("movedAbility")]
        [TestCase("abilityCount")] [TestCase("abilityId")] [TestCase("abilityKind")] [TestCase("abilityValue")] [TestCase("headline")] [TestCase("abilityExtra")] [TestCase("float")] [TestCase("string")] [TestCase("overflow")] [TestCase("nullSpaces")] [TestCase("nullAbility")] [TestCase("duplicateAbility")]
        public void Unsupported_or_corrupt_profiles_are_rejected(string defect)
        {
            var j=JObject.Parse(Source()); var source=(JObject)j["sources"][0]; var map=(JObject)j["sources"][1];
            var spaces=(JArray)j["spaces"]; var s=(JObject)spaces[0]; var abilities=(JArray)j["abilities"]; var a=(JObject)abilities[0];
            switch(defect)
            {
                case "schema":j["schema_version"]=2;break; case "version":j["content_version"]="unknown";break;case "id":j["id"]="unknown";break;
                case "status":j["status"]="pending";break;case "description":j["description"]=false;break;case "missing":j.Remove("base_attempts_per_turn");break;
                case "extra":j["unknown"]=1;break;case "reset":j["attempt_reset_boundary"]="EachAction";break;case "advance":j["ordinary_advance_steps"]=2;break;
                case "active":j["ability_active_rule"]="Ahead";break;case "catalog":j["formal_card_catalog_status"]="pending";break;
                case "sourceCount":((JArray)j["sources"]).RemoveAt(0);break;case "sourceId":source["id"]="M-DELUXE";break;
                case "sourceUrl":source["url"]="https://example.org";break;case "date":source["checked_on"]="2026-99-99";break;
                case "sections":source["sections"]=new JArray("3.4","3.4","7.4.2");break;case "pages":source["pdf_pages"]=new JArray(4,7,19);break;
                case "sourceExtra":source["verified"]=true;break;case "mapUrl":map["url"]="https://example.org";break;
                case "listedBy":map["listed_by"]=false;break;case "scope":map["scope"]=" ";break;
                case "initial":j["initial_position"]=-1;break;case "base":j["base_attempts_per_turn"]=0;break;case "die":j["die"]["maximum"]=2;break;
                case "dieExtra":j["die"]["unknown"]=1;break;case "stepCount":spaces.RemoveAt(7);break;
                case "stepOrder":var tmp=spaces[0].DeepClone();spaces[0]=spaces[1].DeepClone();spaces[1]=tmp;break;
                case "stepId":s["id"]="unknown";break;case "stepExtra":s["unknown"]=1;break;case "stepPosition":s["position"]=2;break;
                case "ops":s["min_effective_ops"]=0;break;case "success":s["success_roll"]["min"]=4;break;case "vp":s["vp"]["second"]=-1;break;
                case "ref":spaces[1]["ability_id"]="unknown";break;case "movedAbility":s["ability_id"]=spaces[1]["ability_id"].DeepClone();spaces[1]["ability_id"]=null;break;
                case "abilityCount":abilities.RemoveAt(0);break;case "abilityId":a["id"]="unknown";break;case "abilityKind":a["kind"]="1";break;
                case "abilityValue":a["value"]=0;break;case "headline":abilities[1]["value"]="Other";break;case "abilityExtra":a["unknown"]=1;break;
                case "float":a["value"]=2.0;break;case "string":a["value"]="2";break;case "overflow":a["value"]=long.MaxValue;break;
                case "nullSpaces":j["spaces"]=null;break;case "nullAbility":abilities[0]=null;break;case "duplicateAbility":abilities[1]=abilities[0].DeepClone();break;
            }
            Assert.That(()=>SpaceRaceDefinitionLoader.LoadSubset(j.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [Test]
        public void Duplicate_keys_invalid_roots_and_size_limits_rejected()
        {
            var duplicate=Source().Replace("\"schema_version\": 1", "\"schema_version\": 1, \"schema_version\": 1");
            Assert.That(duplicate,Is.Not.EqualTo(Source()));
            foreach(var json in new[]{duplicate,"[]","{}","null",null,Source()+" {}",new string(' ',2000001)})
                Assert.That(()=>SpaceRaceDefinitionLoader.LoadSubset(json),Throws.InstanceOf<ArgumentException>());
        }
    }
}
