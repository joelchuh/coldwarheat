using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class CardCatalogLoaderTests
    {
        public static string Source=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Cards/deluxe-2015/cards.json"));
        public static string Schema=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Docs/Contracts/cards-v2.schema.json"));
        public static string Profile=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Cards/deluxe-2015/cards.printed-profile.json"));
        public static CardCatalog Load()=>CardCatalogLoader.LoadPrintedSubset(Source,Schema,Profile);
        static JObject Card(JObject root,string id)=>(JObject)((JArray)root["cards"]).Single(c=>(string)c["id"]==id);
        [Test] public void Actual_catalog_has_three_kinds_and_excludes_china_from_all_era_decks()
        {
            var catalog=Load();Assert.That(catalog.Cards,Has.Count.EqualTo(103));
            Assert.That(catalog.GetEraCards(GameEra.Early),Has.Count.EqualTo(35));Assert.That(catalog.GetEraCards(GameEra.Mid),Has.Count.EqualTo(46));Assert.That(catalog.GetEraCards(GameEra.Late),Has.Count.EqualTo(21));
            Assert.That(catalog.Cards.Count(c=>c.Kind==CardKind.Scoring),Is.EqualTo(7));Assert.That(catalog.Cards.Count(c=>c.Kind==CardKind.Event),Is.EqualTo(95));
            var china=catalog.GetCard(new CardId("card.china"));Assert.That(china.Kind,Is.EqualTo(CardKind.China));Assert.That(china.Era,Is.Null);Assert.That(china.Effect,Is.Null);Assert.That(china.PrintedOps,Is.EqualTo(4));
            var asia=catalog.GetPrintedCard(1);Assert.That(asia.PrintedOps,Is.Null);Assert.That(asia.ScoringRegion,Is.EqualTo("region.asia"));
            var sea=catalog.GetCard(new CardId("card.southeast_asia_scoring"));Assert.That(sea.RemoveAfterResolvedEvent,Is.True);
            var fidel=catalog.GetPrintedCard(8);Assert.That(fidel.Affiliation,Is.EqualTo(CardAffiliation.USSR));Assert.That(fidel.PrintedOps,Is.EqualTo(2));Assert.That(fidel.Effect.EffectId,Is.EqualTo("effect.fidel"));
            Assert.That(fidel.Effect.Parameters.Single().DatasetId,Is.EqualTo("effects.deluxe2015.early_influence_batch1"));Assert.That(fidel.RemoveAfterResolvedEvent,Is.True);
        }
        [TestCase("scope")] [TestCase("version")] [TestCase("duplicate-id")] [TestCase("duplicate-number")] [TestCase("missing-card")]
        [TestCase("ops")] [TestCase("score-ops")] [TestCase("source-pending")] [TestCase("source-unknown")] [TestCase("source-duplicate")]
        [TestCase("binding-unknown")] [TestCase("binding-version")] [TestCase("binding-unresolved")] [TestCase("binding-duplicate-ref")]
        [TestCase("contract-duplicate")] [TestCase("contract-missing")] [TestCase("extra-field")] [TestCase("overflow-number")]
        public void Invalid_definition_is_rejected_even_with_a_synthetic_trusted_digest(string kind)
        {
            var root=JObject.Parse(Source);var fidel=Card(root,"card.fidel");
            switch(kind)
            {
                case "scope":root["scope"]="fixture";break;case "version":root["content_version"]="0.46.0";break;
                case "duplicate-id":fidel["id"]="card.nasser";break;case "duplicate-number":fidel["printed_number"]=1;break;
                case "missing-card":((JArray)root["cards"]).RemoveAt(0);break;case "ops":fidel["printed_ops"]=5;break;
                case "score-ops":Card(root,"card.asia_scoring")["printed_ops"]=0;break;
                case "source-pending":fidel["source_refs"][0]["verification"]="Pending";break;
                case "source-unknown":fidel["source_refs"][0]["source_id"]="UNKNOWN";break;
                case "source-duplicate":((JArray)root["source_catalog"]).Add(root["source_catalog"][0].DeepClone());break;
                case "binding-unknown":fidel["effect_binding"]["effect_id"]="effect.unknown";break;
                case "binding-version":fidel["effect_binding"]["contract_version"]="9.0.0";break;
                case "binding-unresolved":fidel["effect_binding"]["parameter_binding_status"]="Unresolved";fidel["effect_binding"]["parameter_refs"]=new JArray();break;
                case "binding-duplicate-ref":((JArray)fidel["effect_binding"]["parameter_refs"]).Add(fidel["effect_binding"]["parameter_refs"][0].DeepClone());break;
                case "contract-duplicate":((JArray)root["effect_contracts"]).Add(root["effect_contracts"][0].DeepClone());break;
                case "contract-missing":((JArray)root["effect_contracts"]).RemoveAt(0);break;
                case "extra-field":fidel["owner"]="USSR";break;case "overflow-number":fidel["printed_number"]=long.MaxValue;break;
            }
            Assert.That(()=>CardCatalogLoader.LoadPrintedSubset(root.ToString(),Schema,TrustedSyntheticProfile(root)),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Profile_ignores_format_and_object_key_order_but_rejects_modified_facts_or_array_order()
        {
            var root=JObject.Parse(Source);var reordered=new JObject(root.Properties().Reverse().Select(p=>new JProperty(p.Name,p.Value.DeepClone())));
            Assert.DoesNotThrow(()=>CardCatalogLoader.LoadPrintedSubset(reordered.ToString(Formatting.Indented),Schema,Profile));
            Card(root,"card.fidel")["printed_ops"]=3;Assert.That(()=>CardCatalogLoader.LoadPrintedSubset(root.ToString(),Schema,Profile),Throws.InstanceOf<ArgumentException>());
            root=JObject.Parse(Source);root["cards"]=new JArray(((JArray)root["cards"]).Reverse().Select(x=>x.DeepClone()));Assert.That(()=>CardCatalogLoader.LoadPrintedSubset(root.ToString(),Schema,Profile),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("hash")] [TestCase("dataset")] [TestCase("extra")] [TestCase("canonicalization")] [TestCase("schema-id")]
        public void Invalid_profile_or_contract_fails_closed(string kind)
        {
            var p=JObject.Parse(Profile);var schema=JObject.Parse(Schema);
            switch(kind){case "hash":p["sha256"]=new string('0',64);break;case "dataset":p["dataset_id"]="unapproved";break;case "extra":p["approve"]=true;break;case "canonicalization":p["canonicalization"]="other";break;case "schema-id":schema["$id"]="urn:other";break;}
            Assert.That(()=>CardCatalogLoader.LoadPrintedSubset(Source,schema.ToString(),p.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Printed_subset_does_not_execute_or_resolve_declared_handlers_and_parameter_files()
        {
            var catalog=Load();Assert.That(catalog.Cards.Where(c=>c.Kind!=CardKind.China).All(c=>c.Effect!=null),Is.True);
            Assert.That(typeof(CardCatalog).GetMethods().Any(m=>m.Name.Contains("Execute") || m.Name.Contains("CreateGame")),Is.False);
            Assert.That(typeof(CardDefinition).GetProperties().Any(p=>p.Name=="Owner" || p.Name=="Hand" || p.Name=="Handler"),Is.False);
        }
        // Only synthetic tests may recompute a trusted digest to exercise checks beyond the profile gate.
        static string TrustedSyntheticProfile(JObject value)
        {
            JToken Canonical(JToken t)=>t is JObject o?new JObject(o.Properties().OrderBy(p=>p.Name,StringComparer.Ordinal).Select(p=>new JProperty(p.Name,Canonical(p.Value)))):t is JArray a?new JArray(a.Select(Canonical)):t.DeepClone();
            using var hash=SHA256.Create();var p=JObject.Parse(Profile);p["sha256"]=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(Canonical(value).ToString(Formatting.None)))).Replace("-","").ToLowerInvariant();
            p["cards_content_version"]=value["content_version"].DeepClone();return p.ToString();
        }
    }
}
