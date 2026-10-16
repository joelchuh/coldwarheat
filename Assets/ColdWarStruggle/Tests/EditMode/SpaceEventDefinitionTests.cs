using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class SpaceEventDefinitionTests
    {
        static string Data()=>PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/space-events-batch1.parameters.json");
        static string Schema()=>PlayInfluenceEventTests.Read("Docs/Contracts/space-events-batch1.schema.json");
        static SpaceEventDefinition Load(string data=null,string schema=null,CardCatalog cards=null,SpaceRaceDefinition track=null)=>SpaceEventDefinitionLoader.Load(data??Data(),schema??Schema(),cards??CardCatalogLoaderTests.Load(),track??SpaceEventFoundationTests.Track(),BudgetedRemovalCatalogTests.KnownEffects);
        [Test] public void Formal_definition_binds_real_card_track_and_runs_single_step()
        {
            var track=SpaceEventFoundationTests.Track();var cards=CardCatalogLoaderTests.Load();var d=Load(cards:cards,track:track);
            Assert.That(d.Cards,Is.SameAs(cards));Assert.That(d.Track,Is.SameAs(track));Assert.That(d.Card,Is.SameAs(cards.GetPrintedCard(18)));
            var state=SpaceEventFoundationTests.State(track:track);var plan=SpaceEventRules.PlanAdvance(state,SpaceEventFoundationTests.Request(state,definition:d));
            Assert.That(plan.After.Track.US,Is.EqualTo(1));Assert.That(plan.VpDeltaToApply,Is.EqualTo(2));
        }
        [TestCase("version")][TestCase("source")][TestCase("source-url")][TestCase("source-edition")][TestCase("duplicate-source")]
        [TestCase("reference")][TestCase("missing-reference")][TestCase("unknown-field")][TestCase("card")][TestCase("effect")]
        public void Invalid_profile_or_provenance_does_not_load(string defect)
        {
            var r=JObject.Parse(Data());var p=r["parameters"]["captured_nazi_scientist"];
            switch(defect)
            {
                case "version":r["content_version"]="9.0.0";break;
                case "source":r["source_catalog"][0]["id"]="unknown";break;
                case "source-url":r["source_catalog"][0]["url"]="https://example.invalid";break;
                case "source-edition":r["source_catalog"][0]["edition"]="First Edition";break;
                case "duplicate-source":((JArray)r["source_catalog"]).Add(r["source_catalog"][0].DeepClone());break;
                case "reference":p["source_refs"][0]["source_id"]="unknown";break;
                case "missing-reference":((JArray)p["source_refs"]).RemoveAt(0);break;
                case "unknown-field":p["execute"]="arbitrary";break;
                case "card":p["card_id"]="card.olympic_games";break;
                case "effect":p["effect_id"]="effect.olympic_games";break;
            }
            Assert.That(()=>Load(r.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("advance_steps")][TestCase("ordinary_attempt_consumption")][TestCase("requires_ordinary_ops_qualification")]
        [TestCase("random_roll")][TestCase("reward_policy")][TestCase("ability_policy")][TestCase("terminal_policy")]
        [TestCase("track_dataset_id")][TestCase("beneficiary")][TestCase("template")]
        public void Relaxed_schema_cannot_enable_an_unsupported_policy(string field)
        {
            var r=JObject.Parse(Data());var p=r["parameters"]["captured_nazi_scientist"];var v=p[field];
            p[field]=v.Type==JTokenType.Integer?(JToken)99:v.Type==JTokenType.Boolean?(JToken)true:(JToken)"unsupported";
            var s=JObject.Parse(Schema());((JObject)s["$defs"]["captured_nazi_scientist"]["properties"][field]).Remove("const");
            Assert.That(()=>Load(r.ToString(),s.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("missing")][TestCase("affiliation")][TestCase("removal")][TestCase("binding")][TestCase("ops")]
        public void Incorrect_card_binding_cannot_supply_event(string defect)
        {
            var all=CardCatalogLoaderTests.Load();var card=all.GetPrintedCard(18);
            var wrong=new CardDefinition(card.Id,card.PrintedNumber,card.NameKey,card.RulesTextKey,card.Kind,defect=="ops"?2:card.PrintedOps,defect=="affiliation"?CardAffiliation.US:card.Affiliation,card.Era,card.DeckEntry,defect=="removal"?false:card.RemoveAfterResolvedEvent,card.DisplayPersistentReminder,card.ScoringRegion,defect=="binding"?CardCatalogTests.Effect():card.Effect);
            var cards=new CardCatalog(all.Cards.Where(c=>defect!="missing" || !c.Id.Equals(card.Id)).Select(c=>c.Id.Equals(card.Id)?wrong:c));
            Assert.That(()=>Load(cards:cards),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Missing_inputs_unknown_effect_and_wrong_contract_are_rejected()
        {
            var cards=CardCatalogLoaderTests.Load();var track=SpaceEventFoundationTests.Track();
            Assert.That(()=>SpaceEventDefinitionLoader.Load(Data(),Schema(),cards,track,Array.Empty<string>()),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>Load(schema:Schema().Replace("space-events-batch1:1","other:1")),Throws.InstanceOf<ArgumentException>());
            Assert.Throws<ArgumentNullException>(()=>SpaceEventDefinitionLoader.Load(Data(),Schema(),null,track,BudgetedRemovalCatalogTests.KnownEffects));
            Assert.Throws<ArgumentNullException>(()=>SpaceEventDefinitionLoader.Load(Data(),Schema(),cards,null,BudgetedRemovalCatalogTests.KnownEffects));
        }
    }
}
