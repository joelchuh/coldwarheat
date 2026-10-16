using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class IndependentRedsCatalogTests
    {
        internal static string Data()=>PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/early-influence-batch3.parameters.json");
        internal static string Schema()=>PlayInfluenceEventTests.Read("Docs/Contracts/early-influence-batch3.schema.json");
        internal static MatchOpponentInfluenceEffect Load(string data=null)=>IndependentRedsCatalogLoader.Load(data??Data(),Schema(),DefinitionTests.World(),BudgetedRemovalCatalogTests.KnownEffects);
        [Test] public void Loads_only_independent_reds_from_existing_shared_dataset_and_binds_printed_card()
        {
            MatchOpponentInfluenceEffect d=null;Assert.DoesNotThrow(()=>d=Load());Assert.That(d.CardId,Is.EqualTo("card.independent_reds"));Assert.That(d.Beneficiary,Is.EqualTo(PlayerSide.US));Assert.That(d.Countries.Count,Is.EqualTo(5));
            var map=DefinitionTests.World();var cards=PlayScoringTests.Ready().Practice.Definition.Cards;var catalog=new PracticeInfluenceCatalog(map,cards,new[]{d});Assert.That(catalog.Effects.Count,Is.EqualTo(1));Assert.That(catalog.Effects.ContainsKey(new CardId("card.comecon")),Is.False);
        }
        [TestCase("identity")][TestCase("beneficiary")][TestCase("count")][TestCase("policy")][TestCase("unknown-country")][TestCase("duplicate-country")][TestCase("unverified")][TestCase("unknown-source")][TestCase("duplicate-source")][TestCase("version")][TestCase("extra-field")]
        public void Rejects_changed_semantics_and_invalid_provenance(string kind)
        {
            var root=JObject.Parse(Data());var p=root["parameters"]["independent_reds"];
            switch(kind)
            {
                case "identity":p["card_id"]="card.comecon";break;
                case "beneficiary":p["beneficiary"]="USSR";break;
                case "count":p["target_count"]=2;break;
                case "policy":p["selection_policy"]="OnlyProfitable";break;
                case "unknown-country":p["target_country_ids"][0]="country.imaginary";break;
                case "duplicate-country":p["target_country_ids"][0]=p["target_country_ids"][1].DeepClone();break;
                case "unverified":p["source_refs"][0]["verification"]="Pending";break;
                case "unknown-source":p["source_refs"][0]["source_id"]="missing";break;
                case "duplicate-source":((JArray)root["source_catalog"]).Add(root["source_catalog"][0].DeepClone());break;
                case "version":root["content_version"]="0.9.0";break;
                case "extra-field":p["script"]="anything";break;
            }
            Assert.Throws<ArgumentException>(()=>Load(root.ToString()));
        }
        [Test] public void Unknown_effect_reference_or_contract_is_rejected()
        {
            Assert.Throws<ArgumentException>(()=>IndependentRedsCatalogLoader.Load(Data(),Schema(),DefinitionTests.World(),Array.Empty<string>()));
            Assert.Throws<ArgumentException>(()=>IndependentRedsCatalogLoader.Load(Data(),Schema().Replace("urn:coldwarheat:contract:early-influence-batch3:1","urn:unknown"),DefinitionTests.World(),BudgetedRemovalCatalogTests.KnownEffects));
        }
    }
}
