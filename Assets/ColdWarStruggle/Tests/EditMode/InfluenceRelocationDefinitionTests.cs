using System;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class InfluenceRelocationDefinitionTests
    {
        internal static string Data()=>PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/influence-relocation.parameters.json");
        internal static string Schema()=>PlayInfluenceEventTests.Read("Docs/Contracts/influence-relocation.schema.json");
        internal static RelocateOwnInfluenceEffect Load(string data=null)=>InfluenceRelocationDefinitionLoader.Load(data??Data(),Schema(),BudgetedRemovalCatalogTests.KnownEffects);
        [Test] public void Formal_definition_binds_only_the_verified_relocation_profile()
        {var d=Load();Assert.That(d.CardId,Is.EqualTo("card.de_stalinization"));Assert.That(d.EffectId,Is.EqualTo("effect.de_stalinization"));Assert.That(d.Beneficiary,Is.EqualTo(PlayerSide.USSR));Assert.That(d.MaximumTotal,Is.EqualTo(4));Assert.That(d.PerDestinationCap,Is.EqualTo(2));}
        [TestCase("maximum")][TestCase("minimum")][TestCase("cap")][TestCase("owner")][TestCase("beneficiary")][TestCase("source-side")]
        [TestCase("adjacency")][TestCase("cost")][TestCase("controller")][TestCase("overlap")][TestCase("total-policy")][TestCase("commit")]
        [TestCase("identity")][TestCase("effect")][TestCase("version")][TestCase("schema-version")][TestCase("extra")]
        [TestCase("unverified")][TestCase("unknown-source")][TestCase("duplicate-source")][TestCase("duplicate-reference")]
        public void Changed_semantics_or_unverified_references_fail_closed(string fault)
        {
            var root=JObject.Parse(Data());var p=root["parameters"]["de_stalinization"];
            switch(fault)
            {
                case "maximum":p["maximum_total"]=5;break;case "minimum":p["minimum_total"]=1;break;
                case "cap":p["destination_policy"]["max_added_per_country"]=3;break;
                case "owner":p["selection_owner"]="US";break;case "beneficiary":p["beneficiary"]="US";break;
                case "source-side":p["source_policy"]["side"]="US";break;
                case "adjacency":p["destination_policy"]["uses_ops_adjacency"]=true;break;case "cost":p["destination_policy"]["uses_enemy_control_cost"]=true;break;
                case "controller":p["destination_policy"]["excluded_controller"]="USSR";break;
                case "overlap":p["overlap_policy"]="Allow";break;case "total-policy":p["total_policy"]="Any";break;case "commit":p["commit_policy"]="Incremental";break;
                case "identity":p["card_id"]="card.fidel";break;case "effect":p["effect_id"]="effect.fidel";break;
                case "version":root["content_version"]="0.2.0";break;case "schema-version":root["schema_version"]=2;break;
                case "extra":p["execute"]="script";break;case "unverified":p["source_refs"][0]["verification"]="Pending";break;
                case "unknown-source":p["source_refs"][0]["source_id"]="missing";break;
                case "duplicate-source":((JArray)root["source_catalog"]).Add(root["source_catalog"][0].DeepClone());break;
                case "duplicate-reference":((JArray)p["source_refs"]).Add(p["source_refs"][0].DeepClone());break;
            }
            Assert.Throws<ArgumentException>(()=>Load(root.ToString()));
        }
        [Test] public void Unknown_effect_and_wrong_contract_are_rejected()
        {
            Assert.Throws<ArgumentException>(()=>InfluenceRelocationDefinitionLoader.Load(Data(),Schema(),Array.Empty<string>()));
            Assert.Throws<ArgumentException>(()=>InfluenceRelocationDefinitionLoader.Load(Data(),Schema().Replace("urn:coldwarheat:contract:influence-relocation:1","urn:other"),BudgetedRemovalCatalogTests.KnownEffects));
        }
    }
}
