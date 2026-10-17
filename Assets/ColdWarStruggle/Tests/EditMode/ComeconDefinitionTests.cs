using System;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class ComeconDefinitionTests
    {
        internal static string Data()=>IndependentRedsCatalogTests.Data();
        internal static string Schema()=>IndependentRedsCatalogTests.Schema();
        internal static AddToDistinctCountriesEffect Load(string data=null)=>ComeconDefinitionLoader.Load(data??Data(),Schema(),DefinitionTests.World(),BudgetedRemovalCatalogTests.KnownEffects);
        [Test] public void Formal_shared_data_binds_filtered_placement_only()
        {var d=Load();Assert.That(d.CardId,Is.EqualTo("card.comecon"));Assert.That(d.EffectId,Is.EqualTo("effect.comecon"));Assert.That(d.Beneficiary,Is.EqualTo(PlayerSide.USSR));Assert.That(d.TargetCount,Is.EqualTo(4));Assert.That(d.Amount,Is.EqualTo(1));Assert.That(d.ControlFilter,Is.EqualTo(InfluenceControlFilter.NotOpponentControlled));Assert.That(d.Regions,Is.EqualTo(new[]{"region.eastern_europe"}));var c=new PracticeInfluenceCatalog(DefinitionTests.World(),PlayScoringTests.Ready().Practice.Definition.Cards,new[]{d});Assert.That(c.Effects.Count,Is.EqualTo(1));}
        [TestCase("identity")][TestCase("effect")][TestCase("template")][TestCase("beneficiary")][TestCase("control")][TestCase("shortage")][TestCase("region")][TestCase("count")][TestCase("amount")][TestCase("float")][TestCase("version")][TestCase("extra")][TestCase("unverified")][TestCase("unknown-source")][TestCase("duplicate-source")][TestCase("duplicate-ref")]
        public void Invalid_semantics_or_references_do_not_bind(string fault)
        {
            var r=JObject.Parse(Data());var p=r["parameters"]["comecon"];
            switch(fault){case "identity":p["card_id"]="card.independent_reds";break;case "effect":p["effect_id"]="effect.fidel";break;case "template":p["template"]="AddToDistinctCountries";break;case "beneficiary":p["beneficiary"]="US";break;case "control":p["target_control"]="NeitherSide";break;case "shortage":p["shortage_policy"]="SelectAll";break;case "region":p["target_region_id"]="region.europe";break;case "count":p["target_count"]=0;break;case "amount":p["amount_per_country"]=-1;break;case "float":p["target_count"]=4.5;break;case "version":r["content_version"]="99";break;case "extra":p["script"]="x";break;case "unverified":p["source_refs"][0]["verification"]="Pending";break;case "unknown-source":p["source_refs"][0]["source_id"]="none";break;case "duplicate-source":((JArray)r["source_catalog"]).Add(r["source_catalog"][0].DeepClone());break;case "duplicate-ref":((JArray)p["source_refs"]).Add(p["source_refs"][0].DeepClone());break;}
            Assert.That(()=>Load(r.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Unknown_effect_and_contract_reject()
        {Assert.That(()=>ComeconDefinitionLoader.Load(Data(),Schema(),DefinitionTests.World(),Array.Empty<string>()),Throws.InstanceOf<ArgumentException>());Assert.That(()=>ComeconDefinitionLoader.Load(Data(),Schema().Replace("urn:coldwarheat:contract:early-influence-batch3:1","unknown"),DefinitionTests.World(),BudgetedRemovalCatalogTests.KnownEffects),Throws.InstanceOf<ArgumentException>());}
    }
}
