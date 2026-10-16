using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class BlockadeDefinitionTests
    {
        internal static string Data()=>PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/hand-events-batch1.parameters.json");
        internal static string Schema()=>PlayInfluenceEventTests.Read("Docs/Contracts/hand-events-batch1.schema.json");
        internal static OpsGeneralDefinition Ops()=>OpsGeneralDefinitionLoader.LoadSubset(PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/persistent-effects-batch1.parameters.json"),PlayInfluenceEventTests.Read("Docs/Contracts/persistent-effects-batch1.schema.json"));
        internal static BlockadeDefinition Load(string data=null)=>BlockadeDefinitionLoader.Load(data??Data(),Schema(),DefinitionTests.World(),CardCatalogLoaderTests.Load(),Ops(),BudgetedRemovalCatalogTests.KnownEffects);
        [Test] public void Actual_profile_binds_exact_shared_objects_and_one_implemented_effect()
        {
            var map=DefinitionTests.World();var cards=CardCatalogLoaderTests.Load();var ops=Ops();BlockadeDefinition d=null;
            Assert.DoesNotThrow(()=>d=BlockadeDefinitionLoader.Load(Data(),Schema(),map,cards,ops,BudgetedRemovalCatalogTests.KnownEffects));
            Assert.That(d.Map,Is.SameAs(map));Assert.That(d.Cards,Is.SameAs(cards));Assert.That(d.Ops,Is.SameAs(ops));
            Assert.That(d.Card,Is.SameAs(cards.GetCard(new CardId("card.blockade"))));Assert.That(d.EffectId,Is.EqualTo("effect.blockade"));
            Assert.That(d.Owner,Is.EqualTo(PlayerSide.US));Assert.That(d.MinimumEffectiveOps,Is.EqualTo(3));Assert.That(d.FallbackCountry.Value,Is.EqualTo("country.west_germany"));
        }
        [TestCase("identity")][TestCase("owner")][TestCase("threshold")][TestCase("ops-purpose")][TestCase("ops-reference")][TestCase("discard-trigger")][TestCase("country")][TestCase("fallback-side")][TestCase("visibility")][TestCase("unverified")][TestCase("unknown-source")][TestCase("duplicate-ref")][TestCase("duplicate-source")][TestCase("source-url")][TestCase("version")][TestCase("extra-field")][TestCase("five-year-shape")]
        public void Rejects_mutated_semantics_provenance_and_enclosing_document(string kind)
        {
            var root=JObject.Parse(Data());var p=root["parameters"]["blockade"];
            switch(kind)
            {
                case "identity":p["card_id"]="card.five_year_plan";break;
                case "owner":p["decision_owner"]="USSR";break;
                case "threshold":p["minimum_effective_ops"]=0;break;
                case "ops-purpose":p["ops_purpose"]="Influence";break;
                case "ops-reference":p["ops_policy_ref"]["json_pointer"]="/unknown";break;
                case "discard-trigger":p["discard_event_policy"]="Trigger";break;
                case "country":p["fallback_influence"]["country_id"]="country.france";break;
                case "fallback-side":p["fallback_influence"]["side"]="USSR";break;
                case "visibility":p["candidate_visibility"]="Public";break;
                case "unverified":p["source_refs"][0]["verification"]="Pending";break;
                case "unknown-source":p["source_refs"][0]["source_id"]="unknown";break;
                case "duplicate-ref":p["source_refs"][1]=p["source_refs"][0].DeepClone();break;
                case "duplicate-source":((JArray)root["source_catalog"]).Add(root["source_catalog"][0].DeepClone());break;
                case "source-url":root["source_catalog"][0]["url"]="https://example.invalid";break;
                case "version":root["content_version"]="9.0.0";break;
                case "extra-field":p["script"]="ignored?";break;
                case "five-year-shape":root["parameters"]["five_year_plan"]["discard_count"]=2;break;
            }
            Assert.That(()=>Load(root.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Missing_references_contract_or_threshold_purpose_are_rejected()
        {
            var map=DefinitionTests.World();var cards=CardCatalogLoaderTests.Load();var ops=Ops();var known=BudgetedRemovalCatalogTests.KnownEffects;
            Assert.That(()=>BlockadeDefinitionLoader.Load(Data(),Schema(),map,cards,ops,Array.Empty<string>()),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>BlockadeDefinitionLoader.Load(Data(),Schema(),map,cards,ops,known.Where(s=>s!="effect.blockade").ToArray()),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>BlockadeDefinitionLoader.Load(Data(),Schema().Replace("urn:coldwarheat:contract:hand-events-batch1:1","urn:wrong"),map,cards,ops,known),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>BlockadeDefinitionLoader.Load(Data(),Schema(),map,cards,new OpsGeneralDefinition(1,4,new[]{OpsValuePurpose.Coup}),known),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>BlockadeDefinitionLoader.Load(Data(),Schema(),null,cards,ops,known),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("absent-card")][TestCase("affiliation")][TestCase("star")][TestCase("effect")][TestCase("parameter-binding")]
        public void Printed_catalog_must_match_the_effect_contract(string kind)
        {
            var original=CardCatalogLoaderTests.Load();var c=original.GetCard(new CardId("card.blockade"));
            var wrong=kind=="absent-card"?new CardCatalog(original.Cards.Where(x=>!x.Id.Equals(c.Id))):new CardCatalog(original.Cards.Select(x=>!x.Id.Equals(c.Id)?x:
                new CardDefinition(c.Id,c.PrintedNumber,c.NameKey,c.RulesTextKey,c.Kind,c.PrintedOps,kind=="affiliation"?CardAffiliation.US:c.Affiliation,c.Era,c.DeckEntry,kind!="star",c.DisplayPersistentReminder,c.ScoringRegion,
                    kind=="effect"?CardCatalogTests.Effect():kind=="parameter-binding"?new CardEffectBinding(c.Effect.EffectId,c.Effect.ContractVersion,CardParameterBindingKind.NoParameters,Array.Empty<CardParameterReference>()):c.Effect)));
            Assert.That(()=>BlockadeDefinitionLoader.Load(Data(),Schema(),DefinitionTests.World(),wrong,Ops(),BudgetedRemovalCatalogTests.KnownEffects),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("null-map")][TestCase("null-cards")][TestCase("null-ops")][TestCase("unknown-country")][TestCase("owner")][TestCase("minimum")][TestCase("maximum")]
        public void Definition_rejects_incomplete_context(string kind)
        {
            Assert.That(()=>new BlockadeDefinition(kind=="null-map"?null:DefinitionTests.World(),kind=="null-cards"?null:CardCatalogLoaderTests.Load(),kind=="null-ops"?null:Ops(),new CardId("card.blockade"),"effect.blockade",kind=="owner"?PlayerSide.USSR:PlayerSide.US,kind=="minimum"?0:kind=="maximum"?5:3,new CountryId(kind=="unknown-country"?"country.unknown":"country.west_germany")),Throws.InstanceOf<ArgumentException>());
        }
    }
}
