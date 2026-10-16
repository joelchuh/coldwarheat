using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class EarlyWarDefinitionTests
    {
        internal static string Data()=>PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/war-events-batch1.parameters.json");
        internal static string Schema()=>PlayInfluenceEventTests.Read("Docs/Contracts/war-events-batch1.schema.json");
        internal static WarEventCatalog Load(string data=null,string schema=null,MapDefinition map=null,CardCatalog cards=null)
            =>EarlyWarDefinitionLoader.Load(data??Data(),schema??Schema(),map??DefinitionTests.World(),cards??CardCatalogLoaderTests.Load(),BudgetedRemovalCatalogTests.KnownEffects);
        // Independent fixture assembly lets calculation tests fail at the calculation, not the new loader.
        internal static WarEventCatalog Fixture(MapDefinition map=null)
        {
            var root=JObject.Parse(Data());var p=root["war_policy"];
            var policy=new WarPolicy((int)p["die_sides"],(int)p["minimum_winning_result"],(int)p["opponent_controlled_neighbor_penalty"],(int)p["military_credit"]["amount"],(int)p["victory_vp"]);
            var branches=new[]{"korean_war","arab_israeli_war","indo_pakistani_war"};
            var effects=branches.Select((name,i)=>{var e=root["parameters"][name];return new WarEventDefinition(new CardId((string)e["card_id"]),(string)e["effect_id"],(WarEventKind)i,e["target_policy"]["country_ids"].Select(x=>new CountryId((string)x)));});
            return new WarEventCatalog(map??DefinitionTests.World(),CardCatalogLoaderTests.Load(),policy,effects);
        }
        [Test] public void Formal_three_card_catalog_uses_shared_policy_and_printed_bindings()
        {
            var map=DefinitionTests.World();var cards=CardCatalogLoaderTests.Load();var d=Load(map:map,cards:cards);
            Assert.That(d.Map,Is.SameAs(map));Assert.That(d.Cards,Is.SameAs(cards));Assert.That(d.Effects.Count,Is.EqualTo(3));
            Assert.That(d.Policy.DieSides,Is.EqualTo(6));Assert.That(d.Policy.WinningResult,Is.EqualTo(4));Assert.That(d.Policy.NeighborPenalty,Is.EqualTo(1));Assert.That(d.Policy.MilitaryCredit,Is.EqualTo(2));Assert.That(d.Policy.VictoryVp,Is.EqualTo(2));
            Assert.That(WarEventRules.QueryTargets(d,new CardId("card.indo_pakistani_war")).Select(c=>c.Value),Is.EquivalentTo(new[]{"country.india","country.pakistan"}));
        }
        [TestCase("source")][TestCase("source-url")][TestCase("duplicate-source")][TestCase("reference")][TestCase("missing-reference")][TestCase("version")][TestCase("target")][TestCase("unknown-field")][TestCase("gate")][TestCase("policy-reference")]
        public void Provenance_and_document_semantics_are_closed(string defect)
        {
            var r=JObject.Parse(Data());var p=r["parameters"]["arab_israeli_war"];
            switch(defect)
            {
                case "source":r["source_catalog"][0]["id"]="unknown";break;
                case "source-url":r["source_catalog"][0]["url"]="https://example.invalid";break;
                case "duplicate-source":((JArray)r["source_catalog"]).Add(r["source_catalog"][0].DeepClone());break;
                case "reference":p["source_refs"][0]["source_id"]="unknown";break;
                case "missing-reference":((JArray)p["source_refs"]).RemoveAt(0);break;
                case "version":r["content_version"]="9.0.0";break;
                case "target":p["target_policy"]["country_ids"][0]="country.france";break;
                case "unknown-field":p["execute"]="eval";break;
                case "gate":p["prevention_gate"]["source_card_id"]="card.nato";break;
                case "policy-reference":p["war_policy_ref"]["json_pointer"]="/missing";break;
            }
            Assert.That(()=>Load(r.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)] public void Source_edition_must_match_the_bound_profile(int index)
        {
            var r=JObject.Parse(Data());r["source_catalog"][index]["edition"]="First edition rules 2005";
            Assert.That(()=>Load(r.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("die_sides")][TestCase("minimum_winning_result")][TestCase("opponent_controlled_neighbor_penalty")][TestCase("victory_vp")][TestCase("include_direct_superpower_neighbors")][TestCase("ops_modifiers_apply")][TestCase("capture")]
        public void Unsupported_policy_is_rejected_even_with_relaxed_schema(string field)
        {
            var r=JObject.Parse(Data());var p=r["war_policy"];var v=p[field];p[field]=v.Type==JTokenType.Boolean?(JToken)!(bool)v:v.Type==JTokenType.Integer?(JToken)99:(JToken)"unsupported";
            var schema=JObject.Parse(Schema());((JObject)schema["$defs"]["WarPolicy"]["properties"][field]).Remove("const");
            Assert.That(()=>Load(r.ToString(),schema.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Wrong_card_binding_map_or_known_effects_cannot_assemble_catalog()
        {
            var original=CardCatalogLoaderTests.Load();var map=DefinitionTests.World();var absent=new CardCatalog(original.Cards.Where(c=>c.Id.Value!="card.korean_war"));
            Assert.That(()=>Load(cards:absent),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>EarlyWarDefinitionLoader.Load(Data(),Schema(),map,original,Array.Empty<string>()),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>Load(schema:Schema().Replace("contract:war-events-batch1:1","contract:other:1")),Throws.InstanceOf<ArgumentException>());
            var printed=original.GetCard(new CardId("card.korean_war"));var altered=new CardDefinition(printed.Id,printed.PrintedNumber,printed.NameKey,printed.RulesTextKey,printed.Kind,printed.PrintedOps,CardAffiliation.US,printed.Era,printed.DeckEntry,printed.RemoveAfterResolvedEvent,printed.DisplayPersistentReminder,printed.ScoringRegion,printed.Effect);
            Assert.That(()=>Load(cards:new CardCatalog(original.Cards.Select(c=>c.Id.Equals(printed.Id)?altered:c))),Throws.InstanceOf<ArgumentException>());
        }
    }
}
