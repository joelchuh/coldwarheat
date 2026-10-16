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
    public class FiveYearPlanScenarioTests
    {
        internal const string Scene="demo.practice.five_year_plan";
        internal static string Fixture()=>PlayInfluenceEventTests.Read("Data/Demo/five-year-plan-scenario.json");
        internal static DemoCatalog Bind(DemoCatalog c)=>c.WithFiveYearPlan(BlockadeDefinitionTests.Data(),BlockadeDefinitionTests.Schema(),PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/cards.json"));
        internal static DemoCatalog Catalog()=>Bind(BlockadeDemoTests.Catalog()).WithFiveYearPlanScenario(Fixture());
        [Test] public void Binding_preserves_prior_scenarios_and_services_without_duplicate_registration()
        {
            var before=BlockadeDemoTests.Catalog();var after=Bind(before);Assert.That(after.Scenarios.Count,Is.EqualTo(23));
            foreach(var s in before.Scenarios)
            {
                var next=after.Scenarios.Single(x=>x.Id==s.Id);if(s.PracticeDefinition==null){Assert.That(next,Is.SameAs(s));continue;}
                var a=s.PracticeDefinition;var b=next.PracticeDefinition;Assert.That(b.FiveYearPlan.Cards,Is.SameAs(b.Cards));Assert.That(a.FiveYearPlan,Is.Null);
                Assert.That(next.Countries,Is.SameAs(s.Countries));Assert.That(next.Turn,Is.SameAs(s.Turn));Assert.That(b.Blockade,Is.SameAs(a.Blockade));Assert.That(b.InfluenceEvents,Is.SameAs(a.InfluenceEvents));Assert.That(b.FinalScoring,Is.SameAs(a.FinalScoring));Assert.That(b.HeadlineParameters,Is.SameAs(a.HeadlineParameters));Assert.That(b.HeadlineDefectors,Is.SameAs(a.HeadlineDefectors));Assert.That(b.China,Is.SameAs(a.China));Assert.That(b.Risk,Is.SameAs(a.Risk));
            }
            Assert.Throws<ArgumentException>(()=>Bind(after));
        }
        [Test] public void Later_influence_and_hand_event_registration_preserves_five_year_binding()
        {
            var early=Bind(EmptyHandDemoTests.Catalog());var later=BlockadeDemoTests.Bind(IndependentRedsPlayTests.Bind(early));
            foreach(var s in early.Scenarios.Where(x=>x.PracticeDefinition!=null))Assert.That(later.Scenarios.Single(x=>x.Id==s.Id).PracticeDefinition.FiveYearPlan,Is.SameAs(s.PracticeDefinition.FiveYearPlan));
        }
        [Test] public void Fixture_is_24th_scene_with_real_card_partition_and_no_precompleted_actions()
        {
            var c=Catalog();Assert.That(c.Scenarios.Count,Is.EqualTo(24));var scene=c.Scenarios.Last();Assert.That(scene.Id,Is.EqualTo(Scene));Assert.That(scene.Title,Does.Contain("人工局面"));var h=c.CreateHost(Scene);var su=h.UssrSession.ReadView();var us=h.UsSession.ReadView();
            Assert.That(su.OwnHand,Is.EqualTo(new[]{"card.five_year_plan","card.independent_reds"}));Assert.That(us.OwnHand,Is.EqualTo(new[]{"card.olympic_games","card.duck_and_cover"}));Assert.That(su.Turn.Number,Is.EqualTo(2));Assert.That(su.Practice.CompletedActions,Is.Zero);Assert.That(su.Cards.DrawPileCount,Is.EqualTo(31));Assert.That(su.Cards.ResolvingCount,Is.Zero);Assert.That(su.FiveYearPlan,Is.Null);
            Assert.That(h.UssrSession.QueryCardActions().Cards.Single(x=>x.Card.Equals(new CardId("card.five_year_plan"))).Event,Is.EqualTo(RuleError.None));
        }
        [TestCase(false)][TestCase(true)] public async Task Demo_can_finish_child_and_return_to_direct_or_opponent_parent(bool opponent)
        {
            var c=Catalog();var scene=c.Scenarios.Last();var controller=new DemoController(c.CreateHost);controller.LoadScenario(Scene,PlayerSide.USSR,scene.SelectedCountry);controller.SelectCard(new CardId("card.five_year_plan"));
            var begin=await controller.PlayCardAsync(opponent?CardActionKind.OpponentEventFirst:CardActionKind.Event);Assert.That(begin.Accepted,Is.True,begin.Error.ToString());Assert.That(begin.FiveYearPlan.SelectedCard,Is.EqualTo(new CardId("card.independent_reds")));Assert.That(controller.PracticePhaseText,Does.Contain("等待美国"));
            controller.SelectSide(PlayerSide.US);Assert.That(controller.ToggleEventTarget(scene.SelectedCountry),Is.True);var done=await controller.ConfirmEventAsync();Assert.That(done.Accepted,Is.True);Assert.That(done.FiveYearPlan.Outcome,Is.EqualTo(FiveYearPlanOutcome.UsEventResolved));Assert.That(controller.ReadView().FiveYearPlan,Is.Null);Assert.That(controller.ReadView().Countries.Single(x=>x.Definition.Id.Equals(scene.SelectedCountry)).US,Is.EqualTo(3));
            Assert.That(controller.ReadView().Practice.CompletedActions,Is.EqualTo(opponent?0:1));if(opponent)Assert.That(controller.ReadView().Practice.OpponentOps.CurrentStep,Is.EqualTo(CardResolutionStep.Operations));Assert.That(await controller.RetryEventAsync(),Is.SameAs(done));
            controller.LoadScenario(Scene,PlayerSide.USSR,scene.SelectedCountry);Assert.That(controller.CanRetryEvent,Is.False);Assert.That(controller.ReadView().OwnHand.Count,Is.EqualTo(2));
        }
        [Test] public async Task Retrying_child_after_later_action_identifies_historical_receipt()
        {
            var c=new DemoController(_=>new ColdWarStruggle.Application.LocalGameHost(PlayTurnOpsEventTests.Give(FiveYearPlanPlayTests.Ready(),PlayerSide.US,"card.olympic_games"),true));var target=new CountryId("country.yugoslavia");c.LoadScenario("fixture",PlayerSide.USSR,target);c.SelectCard(new CardId("card.five_year_plan"));await c.PlayCardAsync(CardActionKind.Event);c.SelectSide(PlayerSide.US);c.ToggleEventTarget(target);var done=await c.ConfirmEventAsync();Assert.That(done.Accepted,Is.True);Assert.That(c.EventResultHistoryMessage,Is.Null);
            c.SelectCard(new CardId("card.olympic_games"));Assert.That((await c.PlayCardAsync(CardActionKind.Influence)).Accepted,Is.True);Assert.That(await c.RetryEventAsync(),Is.SameAs(done));Assert.That(c.EventResultHistoryMessage,Does.Contain("历史").And.Contain("不会重复"));Assert.That(c.ReadView().Version,Is.GreaterThan(done.View.Version));
        }
        [TestCase("scope")][TestCase("field")][TestCase("id")][TestCase("source")][TestCase("turn")][TestCase("event")][TestCase("child")][TestCase("target")][TestCase("negative")][TestCase("no-change")][TestCase("duplicate")][TestCase("china")][TestCase("late")][TestCase("empty")]
        public void Invalid_artificial_data_is_rejected_without_mutating_source(string kind)
        {
            var c=Bind(BlockadeDemoTests.Catalog());var d=JObject.Parse(Fixture());var r=d["scenario"];
            switch(kind)
            {
                case "scope":d["scope"]="release";break;case "field":r["random_index"]=0;break;case "id":r["id"]=BlockadeDemoTests.Scene;break;case "source":r["source_scenario_id"]="missing";break;case "turn":r["turn_number"]=1;break;case "event":r["event_card"]="card.fidel";break;case "child":r["child_card"]="card.marshall_plan";break;case "target":r["selected_country"]="country.france";break;case "negative":r["influence"]["us"]=-1;break;case "no-change":r["influence"]["us"]=3;break;case "duplicate":r["us_hand"][0]="card.independent_reds";break;case "china":r["us_hand"][0]="card.china";break;case "late":r["us_hand"][0]="card.wargames";break;case "empty":r["us_hand"]=new JArray();break;
            }
            Assert.Throws<ArgumentException>(()=>c.WithFiveYearPlanScenario(d.ToString()));Assert.That(c.Scenarios.Count,Is.EqualTo(23));
        }
    }
}
