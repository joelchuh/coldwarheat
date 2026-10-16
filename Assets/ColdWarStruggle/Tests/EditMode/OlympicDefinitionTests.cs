using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class OlympicDefinitionTests
    {
        internal static string Read(string path)=>PlayInfluenceEventTests.Read(path);
        internal static string Data=>Read("Data/Cards/deluxe-2015/olympic-games.parameters.json");
        internal static string Schema=>Read("Docs/Contracts/olympic-games.schema.json");
        internal static StandardDieDefinition Die()=>StandardDieDefinitionLoader.Load(Read("Data/Rulesets/deluxe-2015/standard-dice.json"),Read("Docs/Contracts/standard-dice.schema.json"));
        internal static OlympicGamesDefinition Load(string data=null,string schema=null,CardCatalog cards=null,StandardDieDefinition die=null)
            =>OlympicGamesDefinitionLoader.Load(data??Data,schema??Schema,cards??CardCatalogLoaderTests.Load(),die??Die(),BudgetedRemovalCatalogTests.KnownEffects);
        [Test] public void Formal_card_and_shared_die_are_bound_without_copying_runtime_values()
        {
            var die=Die();var cards=CardCatalogLoaderTests.Load();var d=Load(cards:cards,die:die);
            Assert.That(d.Die,Is.SameAs(die));Assert.That(d.Cards,Is.SameAs(cards));Assert.That(d.Card,Is.SameAs(cards.GetPrintedCard(20)));
            Assert.That(d.Die.Minimum,Is.EqualTo(1));Assert.That(d.Die.Maximum,Is.EqualTo(6));Assert.That(d.SponsorModifier,Is.EqualTo(2));Assert.That(d.WinnerVp,Is.EqualTo(2));Assert.That(d.TieVp,Is.Zero);Assert.That(d.DefconDelta,Is.EqualTo(-1));Assert.That(d.BoycottBaseOps,Is.EqualTo(4));Assert.That(d.Card.RemoveAfterResolvedEvent,Is.False);
        }
        [TestCase("schema_version","2")][TestCase("content_version","\"0.2.0\"")][TestCase("source_catalog[0].url","\"https://example.invalid/\"")]
        [TestCase("parameters.olympic_games.participate.sponsor_die_modifier","3")][TestCase("parameters.olympic_games.participate.sponsor_die_modifier","\"2\"")]
        [TestCase("parameters.olympic_games.participate.winner_vp","2.0")][TestCase("parameters.olympic_games.participate.tie_vp","1")]
        [TestCase("parameters.olympic_games.participate.tie_policy","\"Stop\"")][TestCase("parameters.olympic_games.participate.draw_order[0]","\"USSR\"")]
        [TestCase("parameters.olympic_games.participate.die_ref.dataset_id","\"rules.other\"")][TestCase("parameters.olympic_games.participate.die_ref.json_pointer","\"/other\"")]
        [TestCase("parameters.olympic_games.participate.branch_change_after_accepted_pair","\"false\"")]
        [TestCase("parameters.olympic_games.boycott.defcon_delta","1")][TestCase("parameters.olympic_games.boycott.nuclear_loser","\"Opponent\"")]
        [TestCase("parameters.olympic_games.boycott.operations.base_ops","2")][TestCase("parameters.olympic_games.boycott.operations.space_allowed","true")]
        [TestCase("parameters.olympic_games.boycott.operations.extra_card","1")][TestCase("parameters.olympic_games.source_refs[0].verification","\"Pending\"")]
        public void Unsupported_semantics_and_coerced_types_fail_even_with_weak_schema(string path,string value)
        {
            var root=JObject.Parse(Data);root.SelectToken(path).Replace(JToken.Parse(value));
            var weak=new JObject{["$id"]="urn:coldwarheat:contract:olympic-games:1",["type"]="object"};
            Assert.That(()=>Load(root.ToString(),weak.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("extra")][TestCase("missing")][TestCase("duplicate-source")][TestCase("duplicate-ref")][TestCase("wrong-schema")]
        public void Contract_shape_and_evidence_are_validated(string fault)
        {
            var root=JObject.Parse(Data);var schema=Schema;
            if(fault=="extra")root["extra"]=true;
            if(fault=="missing")((JObject)root["parameters"]["olympic_games"]).Remove("participate");
            if(fault=="duplicate-source")root["source_catalog"][1]=root["source_catalog"][0].DeepClone();
            if(fault=="duplicate-ref")root["parameters"]["olympic_games"]["source_refs"][1]=root["parameters"]["olympic_games"]["source_refs"][0].DeepClone();
            if(fault=="wrong-schema")schema=schema.Replace("olympic-games:1","other:1");
            Assert.That(()=>Load(root.ToString(),schema),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("absent")][TestCase("affiliation")][TestCase("ops")][TestCase("star")][TestCase("binding")]
        public void Printed_card_contract_is_not_trusted_from_parameter_name(string fault)
        {
            var all=CardCatalogLoaderTests.Load();var c=all.GetPrintedCard(20);
            if(fault=="absent"){Assert.That(()=>Load(cards:new CardCatalog(all.Cards.Where(x=>!x.Id.Equals(c.Id)))),Throws.InstanceOf<ArgumentException>());return;}
            var binding=fault=="binding"?new CardEffectBinding(c.Effect.EffectId,"0.1.0",CardParameterBindingKind.Bound,new[]{new CardParameterReference("effects.other","/parameters/olympic_games")}):c.Effect;
            var altered=new CardDefinition(c.Id,c.PrintedNumber,c.NameKey,c.RulesTextKey,c.Kind,fault=="ops"?3:c.PrintedOps,fault=="affiliation"?CardAffiliation.US:c.Affiliation,c.Era,c.DeckEntry,fault=="star"||c.RemoveAfterResolvedEvent,c.DisplayPersistentReminder,c.ScoringRegion,binding);
            Assert.That(()=>Load(cards:new CardCatalog(all.Cards.Select(x=>x.Id.Equals(c.Id)?altered:x))),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("maximum","8")][TestCase("maximum","\"6\"")][TestCase("minimum","1.0")][TestCase("kind","\"Weighted\"")][TestCase("id","\"die.other\"")]
        public void Shared_die_is_a_strict_uniform_D6(string key,string value)
        {
            var data=JObject.Parse(Read("Data/Rulesets/deluxe-2015/standard-dice.json"));data["definitions"]["standard_d6"][key]=JToken.Parse(value);
            var weak=new JObject{["$id"]="urn:coldwarheat:contract:standard-dice:1",["type"]="object"};
            Assert.That(()=>StandardDieDefinitionLoader.Load(data.ToString(),weak.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Null_dependencies_unknown_effect_and_invalid_direct_definition_are_rejected()
        {
            var cards=CardCatalogLoaderTests.Load();var die=Die();
            Assert.Throws<ArgumentNullException>(()=>OlympicGamesDefinitionLoader.Load(Data,Schema,null,die,BudgetedRemovalCatalogTests.KnownEffects));
            Assert.Throws<ArgumentNullException>(()=>OlympicGamesDefinitionLoader.Load(Data,Schema,cards,null,BudgetedRemovalCatalogTests.KnownEffects));
            Assert.That(()=>OlympicGamesDefinitionLoader.Load(Data,Schema,cards,die,new[]{"effect.other"}),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new StandardDieDefinition("die.standard_d6",0,5),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new OlympicGamesDefinition(cards,die,new CardId("card.olympic_games"),"effect.olympic_games",2,3,0,-1,4),Throws.InstanceOf<ArgumentException>());
        }
    }
}
