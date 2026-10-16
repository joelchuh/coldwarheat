using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class FiveYearPlanDefinitionTests
    {
        internal static FiveYearPlanDefinition Load(string data=null,string schema=null,CardCatalog cards=null)=>FiveYearPlanDefinitionLoader.Load(data??BlockadeDefinitionTests.Data(),schema??BlockadeDefinitionTests.Schema(),cards??CardCatalogLoaderTests.Load(),BudgetedRemovalCatalogTests.KnownEffects);

        [Test] public void Formal_profile_binds_exact_printed_catalog_and_selection_policy()
        {
            var cards=CardCatalogLoaderTests.Load();var d=Load(cards:cards);
            Assert.That(d.Cards,Is.SameAs(cards));Assert.That(d.Card,Is.SameAs(cards.GetCard(new CardId("card.five_year_plan"))));
            Assert.That(d.EffectId,Is.EqualTo("effect.five_year_plan"));Assert.That(d.HandOwner,Is.EqualTo(PlayerSide.USSR));
            Assert.That(d.TriggeredAffiliation,Is.EqualTo(CardAffiliation.US));Assert.That(d.DiscardCount,Is.EqualTo(1));
        }

        [TestCase("card_id")][TestCase("effect_id")][TestCase("template")][TestCase("hand_owner")][TestCase("source_zone")]
        [TestCase("candidate_kind_policy")][TestCase("sampling_policy")][TestCase("empty_hand_policy")][TestCase("singleton_policy")]
        [TestCase("immediate_event_affiliation")][TestCase("child_event_controller")][TestCase("child_event_source")]
        [TestCase("child_phasing_policy")][TestCase("ordinary_discard_destination")][TestCase("triggered_event_zone")]
        [TestCase("triggered_event_disposition")][TestCase("candidate_visibility")][TestCase("selected_card_visibility")]
        [TestCase("creates_card_use")][TestCase("automatically_grants_discarded_card_ops")][TestCase("consumes_root_slot")][TestCase("discard_count")]
        public void Runtime_semantics_are_guarded_even_when_schema_constants_are_relaxed(string field)
        {
            var root=JObject.Parse(BlockadeDefinitionTests.Data());var p=root["parameters"]["five_year_plan"];
            p[field]=p[field].Type==JTokenType.Boolean?(JToken)true:p[field].Type==JTokenType.Integer?(JToken)2:(JToken)"unsupported";
            var schema=JObject.Parse(BlockadeDefinitionTests.Schema());
            ((JObject)schema["$defs"]["five_year_plan"]["properties"][field]).Remove("const");
            Assert.That(()=>Load(root.ToString(),schema.ToString()),Throws.InstanceOf<ArgumentException>());
        }

        [TestCase("unknown-source")][TestCase("duplicate-source")][TestCase("source-url")][TestCase("missing-source")]
        [TestCase("unknown-ref")][TestCase("duplicate-ref")][TestCase("missing-ref")][TestCase("unverified")]
        [TestCase("version")][TestCase("extra-field")][TestCase("blockade-shape")][TestCase("duplicate-json-property")]
        public void Provenance_profile_and_enclosing_document_are_closed(string kind)
        {
            var root=JObject.Parse(BlockadeDefinitionTests.Data());var p=root["parameters"]["five_year_plan"];
            switch(kind)
            {
                case "unknown-source":root["source_catalog"][0]["id"]="unknown";break;
                case "duplicate-source":((JArray)root["source_catalog"]).Add(root["source_catalog"][0].DeepClone());break;
                case "source-url":root["source_catalog"][0]["url"]="https://example.invalid";break;
                case "missing-source":((JArray)root["source_catalog"]).RemoveAt(0);break;
                case "unknown-ref":p["source_refs"][0]["source_id"]="unknown";break;
                case "duplicate-ref":p["source_refs"][1]=p["source_refs"][0].DeepClone();break;
                case "missing-ref":((JArray)p["source_refs"]).RemoveAt(0);break;
                case "unverified":p["source_refs"][0]["verification"]="Pending";break;
                case "version":root["content_version"]="9.0.0";break;
                case "extra-field":p["script"]="ignored?";break;
                case "blockade-shape":root["parameters"]["blockade"]["minimum_effective_ops"]=0;break;
            }
            var json=root.ToString();if(kind=="duplicate-json-property")json=json.Replace("\"discard_count\": 1","\"discard_count\": 1, \"discard_count\": 1");
            Assert.That(()=>Load(json),Throws.InstanceOf<ArgumentException>());
        }

        [TestCase("absent-card")][TestCase("affiliation")][TestCase("star")][TestCase("effect")][TestCase("parameter-binding")]
        public void Printed_catalog_must_match_the_selected_effect(string kind)
        {
            var original=CardCatalogLoaderTests.Load();var c=original.GetCard(new CardId("card.five_year_plan"));
            var wrong=kind=="absent-card"?new CardCatalog(original.Cards.Where(x=>!x.Id.Equals(c.Id))):new CardCatalog(original.Cards.Select(x=>!x.Id.Equals(c.Id)?x:
                new CardDefinition(c.Id,c.PrintedNumber,c.NameKey,c.RulesTextKey,c.Kind,c.PrintedOps,kind=="affiliation"?CardAffiliation.USSR:c.Affiliation,c.Era,c.DeckEntry,kind=="star",c.DisplayPersistentReminder,c.ScoringRegion,
                    kind=="effect"?CardCatalogTests.Effect():kind=="parameter-binding"?new CardEffectBinding(c.Effect.EffectId,c.Effect.ContractVersion,CardParameterBindingKind.NoParameters,Array.Empty<CardParameterReference>()):c.Effect)));
            Assert.That(()=>Load(cards:wrong),Throws.InstanceOf<ArgumentException>());
        }

        [Test] public void Missing_context_effect_references_or_wrong_schema_are_rejected()
        {
            var cards=CardCatalogLoaderTests.Load();var known=BudgetedRemovalCatalogTests.KnownEffects;
            Assert.That(()=>FiveYearPlanDefinitionLoader.Load(BlockadeDefinitionTests.Data(),BlockadeDefinitionTests.Schema(),null,known),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>FiveYearPlanDefinitionLoader.Load(BlockadeDefinitionTests.Data(),BlockadeDefinitionTests.Schema(),cards,null),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>FiveYearPlanDefinitionLoader.Load(BlockadeDefinitionTests.Data(),BlockadeDefinitionTests.Schema(),cards,Array.Empty<string>()),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>FiveYearPlanDefinitionLoader.Load(BlockadeDefinitionTests.Data(),BlockadeDefinitionTests.Schema(),cards,new[]{" "}),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>FiveYearPlanDefinitionLoader.Load(BlockadeDefinitionTests.Data(),BlockadeDefinitionTests.Schema(),cards,known.Where(s=>s!="effect.five_year_plan").ToArray()),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>Load(schema:BlockadeDefinitionTests.Schema().Replace("urn:coldwarheat:contract:hand-events-batch1:1","urn:wrong")),Throws.InstanceOf<ArgumentException>());
        }
    }
}
