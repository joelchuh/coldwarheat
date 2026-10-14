using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class QuickStartDemoTests
    {
        internal const string Id="demo.practice.quick_start";
        internal static string Data()=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Demo/quick-start-scenario.json"));
        internal static DemoCatalog Catalog()=>PlayScoringCatalogTests.Catalog().WithQuickStartScenario(Data());
        static CountryId C(string n)=>new CountryId("country."+n);
        static CountryView Country(PlayerViewSnapshot v,string n)=>v.Countries.Single(c=>c.Definition.Id.Equals(C(n)));
        [Test] public void Fast_start_reuses_rules_and_deal_keeps_manual_setup_and_only_adds_legal_allocations()
        {
            var d=Catalog();var quick=d.Scenarios.Single(s=>s.Id==Id);var source=d.Scenarios.Single(s=>s.Id==DealtDemoCatalogTests.ScenarioId);
            Assert.That(d.Scenarios.Count,Is.EqualTo(14));Assert.That(quick.PracticeDefinition,Is.SameAs(source.PracticeDefinition));Assert.That(quick.InitialCards,Is.SameAs(source.InitialCards));Assert.That(quick.DeploymentDefinition,Is.SameAs(source.DeploymentDefinition));
            var h=d.CreateHost(Id);var v=h.UssrSession.ReadView();var manual=d.CreateHost(source.Id).UssrSession.ReadView();
            Assert.That(manual.Deployment.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(manual.Practice.Stage,Is.EqualTo(ActionPracticeStage.AwaitingStart));Assert.That(Country(manual,"poland").USSR,Is.Zero);
            Assert.That(v.Deployment,Is.Null);Assert.That(v.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChooseCard));Assert.That(v.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(v.Practice.CompletedActions,Is.Zero);Assert.That(v.Practice.VpForUS,Is.Zero);Assert.That(v.Practice.Defcon,Is.EqualTo(5));
            Assert.That(v.Practice.UsMilitary,Is.Zero);Assert.That(v.Practice.UssrMilitary,Is.Zero);Assert.That(v.Practice.UsSpace,Is.Zero);Assert.That(v.Practice.UssrSpace,Is.Zero);Assert.That(v.Practice.HasScoring,Is.True);
            Assert.That(Country(v,"poland").USSR,Is.EqualTo(6));Assert.That(Country(v,"west_germany").US,Is.EqualTo(4));Assert.That(Country(v,"italy").US,Is.EqualTo(3));Assert.That(Country(v,"iran").US,Is.EqualTo(1));Assert.That(Country(v,"nigeria").USSR,Is.Zero);
            Assert.That(v.Countries.Sum(c=>c.US),Is.EqualTo(25));Assert.That(v.Countries.Sum(c=>c.USSR),Is.EqualTo(15));Assert.That(v.OwnHand,Is.EqualTo(manual.OwnHand));Assert.That(h.UsSession.ReadView().OwnHand.Count,Is.EqualTo(8));
        }
        [TestCase("scope")][TestCase("unknown-source")][TestCase("wrong-source")][TestCase("duplicate-id")][TestCase("unknown-country")]
        [TestCase("outside-region")][TestCase("wrong-total")][TestCase("duplicate-country")][TestCase("wrong-order")][TestCase("missing-stage")][TestCase("extra-field")]
        public void Malformed_fixtures_fail_before_catalog_publication(string kind)
        {
            var j=JObject.Parse(Data());var s=(JObject)j["scenario"];var stages=(JArray)s["deployments"];var a=(JArray)stages[0]["allocations"];
            switch(kind)
            {
                case "scope":j["scope"]="rules";break;
                case "unknown-source":s["source_scenario_id"]="missing";break;
                case "wrong-source":s["source_scenario_id"]="demo.setup.deployment";break;
                case "duplicate-id":s["id"]="demo.setup.initial_deal";break;
                case "unknown-country":s["selected_country"]="country.missing";break;
                case "outside-region":a[0]["country_id"]="country.iran";break;
                case "wrong-total":a[0]["amount"]=7;break;
                case "duplicate-country":a.Add(a[0].DeepClone());break;
                case "wrong-order":stages[0]["side"]="US";break;
                case "missing-stage":stages.RemoveAt(1);break;
                case "extra-field":s["vp"]=20;break;
            }
            var d=PlayScoringCatalogTests.Catalog();Assert.Throws<ArgumentException>(()=>d.WithQuickStartScenario(j.ToString()));Assert.That(d.Scenarios.Count,Is.EqualTo(13));Assert.That(d.CreateHost(DealtDemoCatalogTests.ScenarioId).UssrSession.ReadView().Deployment,Is.Not.Null);
        }
        [Test] public void Missing_configuration_and_repeat_load_fail()
        {Assert.Throws<ArgumentException>(()=>PlayableDemoTests.Catalog().WithQuickStartScenario(Data()));Assert.Throws<ArgumentException>(()=>Catalog().WithQuickStartScenario(Data()));}
        static async Task<CardActionResult> Space(IGameSession s)
        {
            var v=s.ReadView();var c=s.QueryCardActions().Cards.First(x=>x.Space==RuleError.None).Card;
            return await s.SubmitCardActionAsync(new CardActionCommand("space",v.GameId,v.Version,CardActionKind.Space,c));
        }
        [Test] public async Task Manual_and_quick_start_have_identical_board_hands_and_first_random_result()
        {
            var d=Catalog();var manual=d.CreateHost(DealtDemoCatalogTests.ScenarioId);var quick=d.CreateHost(Id);
            var j=JObject.Parse(Data());foreach(var row in j["scenario"]["deployments"])
            {
                var session=(string)row["side"]=="USSR"?manual.UssrSession:manual.UsSession;var v=session.ReadView();
                var allocations=row["allocations"].Select(x=>new InfluenceAllocation(new CountryId((string)x["country_id"]),(int)x["amount"]));
                Assert.That((await session.SubmitDeploymentAsync(new DeploymentCommandEnvelope("deploy-"+session.Side,v.GameId,v.Version,v.Deployment.ExecutionId,v.Deployment.DecisionId,allocations))).Accepted,Is.True);
            }
            var before=manual.UssrSession.ReadView();Assert.That((await manual.UssrSession.SubmitCardActionAsync(new CardActionCommand("start",before.GameId,before.Version,CardActionKind.StartPractice))).Accepted,Is.True);
            var v1=manual.UssrSession.ReadView();var v2=quick.UssrSession.ReadView();
            Assert.That(v1.Version,Is.EqualTo(v2.Version));Assert.That(v1.Countries.Select(c=>(c.Definition.Id,c.US,c.USSR)),Is.EqualTo(v2.Countries.Select(c=>(c.Definition.Id,c.US,c.USSR))));Assert.That(manual.ReadLocalTestSnapshot().USHand,Is.EqualTo(quick.ReadLocalTestSnapshot().USHand));
            var a=await Space(manual.UssrSession);var b=await Space(quick.UssrSession);Assert.That(a.Accepted&&b.Accepted,Is.True);Assert.That(a.Space.Roll,Is.EqualTo(b.Space.Roll));Assert.That(a.Space.To,Is.EqualTo(b.Space.To));Assert.That(a.View.Practice.VpForUS,Is.EqualTo(b.View.Practice.VpForUS));
        }
        [Test] public async Task Real_action_then_scoring_and_reset_keep_distinct_authorities()
        {
            var d=Catalog();var c=new DemoController(d.CreateHost);c.LoadScenario(Id,PlayerSide.USSR,C("iran"));var initial=c.ReadView();var initialHands=c.ReadBoth();
            var card=c.CardOptions().Cards.First(x=>x.Coup==RuleError.None).Card;c.SelectCard(card);c.SelectCoupUse();Assert.That((await c.PlayCardAsync(CardActionKind.Coup)).Accepted,Is.True);
            Assert.That(c.ReadView().Practice.Defcon,Is.EqualTo(4));c.SelectSide(PlayerSide.US);var scoring=c.CardOptions().Cards.Single(x=>x.Scoring==RuleError.None).Card;c.SelectCard(scoring);
            Assert.That((await c.PlayCardAsync(CardActionKind.Scoring)).Accepted,Is.True);Assert.That(c.ReadView().Practice.CompletedActions,Is.EqualTo(2));Assert.That(c.ReadView().Practice.Defcon,Is.EqualTo(4));
            c.LoadScenario(Id,PlayerSide.USSR,C("iran"));Assert.That(c.ReadView().GameId,Is.Not.EqualTo(initial.GameId));Assert.That(c.ReadView().Practice.CompletedActions,Is.Zero);Assert.That(c.ReadView().Practice.Defcon,Is.EqualTo(5));Assert.That(c.LastCardResult,Is.Null);Assert.That(c.SelectedCard,Is.Null);Assert.That(c.ReadBoth().USHand,Is.EqualTo(initialHands.USHand));Assert.That(c.ReadBoth().USSRHand,Is.EqualTo(initialHands.USSRHand));
        }
        [Test] public void Launch_defaults_to_quick_start_but_explicit_manual_selection_wins()
        {
            var ids=new[]{"old","demo.setup.initial_deal",Id};Assert.That(DemoLaunchOptions.ScenarioIndex(Array.Empty<string>(),ids),Is.EqualTo(2));
            Assert.That(DemoLaunchOptions.ScenarioIndex(new[]{"--demo-scenario=demo.setup.initial_deal"},ids),Is.EqualTo(1));Assert.That(DemoLaunchOptions.ScenarioIndex(Array.Empty<string>(),ids.Take(2).ToArray()),Is.EqualTo(1));
        }
    }
}
