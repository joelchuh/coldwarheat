using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class BlockadeDemoTests
    {
        internal const string Scene="demo.practice.blockade";
        internal static string Fixture()=>PlayInfluenceEventTests.Read("Data/Demo/blockade-scenario.json");
        internal static DemoCatalog Bind(DemoCatalog c)=>c.WithBlockade(BlockadeDefinitionTests.Data(),BlockadeDefinitionTests.Schema(),PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/cards.json"));
        internal static DemoCatalog Catalog()=>Bind(IndependentRedsPlayTests.Catalog()).WithBlockadeScenario(Fixture());
        [Test] public void Binding_keeps_all_scenarios_services_and_rejects_duplicate_registration()
        {
            var old=IndependentRedsPlayTests.Catalog();var next=Bind(old);Assert.That(next.Scenarios.Count,Is.EqualTo(old.Scenarios.Count));
            foreach(var s in old.Scenarios)
            {
                var t=next.Scenarios.Single(x=>x.Id==s.Id);if(s.PracticeDefinition==null){Assert.That(t,Is.SameAs(s));continue;}
                var a=s.PracticeDefinition;var b=t.PracticeDefinition;
                Assert.That(a.Blockade,Is.Null);Assert.That(b.Blockade,Is.Not.Null);Assert.That(b.Blockade.Map,Is.SameAs(next.Map));Assert.That(b.Blockade.Ops,Is.SameAs(b.Ops));Assert.That(b.Blockade.Cards,Is.SameAs(b.Cards));
                Assert.That(t.Countries,Is.SameAs(s.Countries));Assert.That(b.InfluenceEvents,Is.SameAs(a.InfluenceEvents));Assert.That(b.FinalScoring,Is.SameAs(a.FinalScoring));Assert.That(b.HeadlineParameters,Is.SameAs(a.HeadlineParameters));Assert.That(b.HeadlineDefectors,Is.SameAs(a.HeadlineDefectors));Assert.That(b.China,Is.SameAs(a.China));Assert.That(b.TurnOpsEvents,Is.SameAs(a.TurnOpsEvents));Assert.That(b.Risk,Is.SameAs(a.Risk));
            }
            Assert.Throws<ArgumentException>(()=>Bind(next));
        }
        [Test] public void Binding_before_later_influence_catalog_keeps_blockade_reference()
        {
            var early=Bind(EmptyHandDemoTests.Catalog());var after=IndependentRedsPlayTests.Bind(early);
            foreach(var s in early.Scenarios.Where(s=>s.PracticeDefinition!=null))Assert.That(after.Scenarios.Single(x=>x.Id==s.Id).PracticeDefinition.Blockade,Is.SameAs(s.PracticeDefinition.Blockade));
        }
        [Test] public void Fixture_partitions_real_cards_and_selects_fallback_country_from_definition()
        {
            var c=Catalog();Assert.That(c.Scenarios.Count,Is.EqualTo(23));var scene=c.Scenarios.Last();Assert.That(scene.SelectedCountry,Is.EqualTo(scene.PracticeDefinition.Blockade.FallbackCountry));Assert.That(scene.Title,Does.Contain("人工局面"));
            var host=c.CreateHost(Scene);var su=host.UssrSession.ReadView();var us=host.UsSession.ReadView();Assert.That(su.OwnHand,Is.EqualTo(new[]{"card.blockade"}));Assert.That(us.OwnHand,Is.EqualTo(new[]{"card.marshall_plan","card.independent_reds","card.asia_scoring"}));
            Assert.That(su.Cards.DrawPileCount,Is.EqualTo(31));Assert.That(su.Practice.CompletedActions,Is.Zero);Assert.That(su.Turn.Number,Is.EqualTo(2));Assert.That(host.UssrSession.QueryCardActions().Cards.Single().Event,Is.EqualTo(RuleError.None));
            var west=su.Countries.Single(x=>x.Definition.Id.Equals(scene.SelectedCountry));Assert.That(west.US,Is.EqualTo(5));Assert.That(west.USSR,Is.EqualTo(2));
        }
        [TestCase(true)][TestCase(false)] public async Task Controller_can_play_both_branches_from_real_demo(bool discard)
        {
            var c=Catalog();var scene=c.Scenarios.Last();var controller=new DemoController(c.CreateHost);controller.LoadScenario(Scene,PlayerSide.USSR,scene.SelectedCountry);controller.SelectCard(new CardId("card.blockade"));
            var begin=await controller.PlayCardAsync(CardActionKind.Event);Assert.That(begin.Accepted,Is.True);Assert.That(controller.ReadView().Blockade.Candidates,Is.Empty);Assert.That(controller.PracticePhaseText,Does.Contain("等待美国"));
            controller.SelectSide(PlayerSide.US);Assert.That(controller.ReadView().Blockade.Candidates.Select(x=>x.Card),Is.EqualTo(new[]{new CardId("card.marshall_plan")}));
            if(discard){controller.SelectCard(new CardId("card.marshall_plan"));Assert.That((await controller.DiscardForBlockadeAsync(controller.SelectedCard.Value)).Accepted,Is.True);}else Assert.That((await controller.DeclineBlockadeAsync()).Accepted,Is.True);
            Assert.That(controller.ReadView().Blockade,Is.Null);Assert.That(controller.ReadView().Practice.CompletedActions,Is.EqualTo(1));var west=controller.ReadView().Countries.Single(x=>x.Definition.Id.Equals(scene.SelectedCountry));Assert.That(west.US,Is.EqualTo(discard?5:0));Assert.That(west.USSR,Is.EqualTo(2));
        }
        [TestCase("scope")][TestCase("unknown-field")][TestCase("id")][TestCase("source")][TestCase("turn")][TestCase("event")][TestCase("duplicate")][TestCase("china")][TestCase("late")][TestCase("empty")][TestCase("no-candidate")][TestCase("negative")]
        public void Invalid_fixtures_fail_without_modifying_original(string kind)
        {
            var c=Bind(IndependentRedsPlayTests.Catalog());var data=JObject.Parse(Fixture());var row=data["scenario"];
            switch(kind)
            {
                case "scope":data["scope"]="release";break;case "unknown-field":row["threshold"]=3;break;case "id":row["id"]=IndependentRedsPlayTests.Scene;break;case "source":row["source_scenario_id"]="missing";break;case "turn":row["turn_number"]=1;break;case "event":row["event_card"]="card.fidel";break;
                case "duplicate":row["us_hand"][1]=row["us_hand"][0].DeepClone();break;case "china":row["us_hand"][1]="card.china";break;case "late":row["us_hand"][1]="card.wargames";break;case "empty":row["us_hand"]=new JArray();break;case "no-candidate":row["us_hand"]=new JArray("card.independent_reds","card.asia_scoring");break;case "negative":row["fallback_influence"]["us"]=-1;break;
            }
            Assert.Throws<ArgumentException>(()=>c.WithBlockadeScenario(data.ToString()));Assert.That(c.Scenarios.Count,Is.EqualTo(22));
        }
    }
}
