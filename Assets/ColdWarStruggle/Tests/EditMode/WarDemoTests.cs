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
    public class WarDemoTests
    {
        internal static string Fixture()=>PlayInfluenceEventTests.Read("Data/Demo/early-war-scenarios.json");
        internal static DemoCatalog Bind(DemoCatalog c)=>c.WithEarlyWars(EarlyWarDefinitionTests.Data(),EarlyWarDefinitionTests.Schema(),PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/cards.json"));
        internal static DemoCatalog Catalog()=>Bind(FiveYearPlanScenarioTests.Catalog()).WithEarlyWarScenarios(Fixture());
        [Test] public void Registration_retains_prior_services_and_rejects_duplicate_registration()
        {
            var before=FiveYearPlanScenarioTests.Catalog();var after=Bind(before);Assert.That(after.Scenarios.Count,Is.EqualTo(24));
            foreach(var s in before.Scenarios)
            {
                var n=after.Scenarios.Single(x=>x.Id==s.Id);if(s.PracticeDefinition==null){Assert.That(n,Is.SameAs(s));continue;}
                var a=s.PracticeDefinition;var b=n.PracticeDefinition;Assert.That(a.Wars,Is.Null);Assert.That(b.Wars.Effects.Count,Is.EqualTo(3));Assert.That(b.Wars.Cards,Is.SameAs(b.Cards));Assert.That(b.Wars.Map,Is.SameAs(after.Map));Assert.That(b.FiveYearPlan,Is.SameAs(a.FiveYearPlan));Assert.That(b.Blockade,Is.SameAs(a.Blockade));Assert.That(b.HeadlineParameters,Is.SameAs(a.HeadlineParameters));Assert.That(b.HeadlineDefectors,Is.SameAs(a.HeadlineDefectors));Assert.That(b.FinalScoring,Is.SameAs(a.FinalScoring));Assert.That(b.China,Is.SameAs(a.China));Assert.That(b.Risk,Is.SameAs(a.Risk));Assert.That(n.Countries,Is.SameAs(s.Countries));
            }
            Assert.Throws<ArgumentException>(()=>Bind(after));
        }
        [Test] public void Later_hand_and_influence_registration_preserves_war_binding()
        {
            var before=Bind(EmptyHandDemoTests.Catalog());var after=FiveYearPlanScenarioTests.Bind(BlockadeDemoTests.Bind(IndependentRedsPlayTests.Bind(before)));
            foreach(var s in before.Scenarios.Where(x=>x.PracticeDefinition!=null))Assert.That(after.Scenarios.Single(x=>x.Id==s.Id).PracticeDefinition.Wars,Is.SameAs(s.PracticeDefinition.Wars));
        }
        [Test] public void Three_artificial_scenes_have_real_partition_and_no_precompleted_actions()
        {
            var c=Catalog();Assert.That(c.Scenarios.Count,Is.EqualTo(27));var rows=JObject.Parse(Fixture())["scenarios"];
            foreach(var row in rows)
            {
                var id=(string)row["id"];var scene=c.Scenarios.Single(s=>s.Id==id);Assert.That(scene.Title,Does.Contain("人工局面"));var h=c.CreateHost(id);var v=h.UssrSession.ReadView();Assert.That(v.Turn.Number,Is.EqualTo(2));Assert.That(v.Practice.CompletedActions,Is.Zero);Assert.That(v.Practice.VpForUS,Is.Zero);Assert.That(v.Practice.UssrMilitary,Is.Zero);Assert.That(v.Cards.DrawPileCount,Is.EqualTo(31));Assert.That(v.Cards.ResolvingCount,Is.Zero);Assert.That(v.OwnHand,Is.EqualTo(row["ussr_hand"].Select(t=>(string)t)));Assert.That(h.UsSession.ReadView().OwnHand,Is.EqualTo(row["us_hand"].Select(t=>(string)t)));Assert.That(v.War,Is.Null);
                Assert.That(h.UssrSession.QueryCardActions().Cards.Single(x=>x.Card.Equals(new CardId((string)row["card"]))).Event,Is.EqualTo(RuleError.None));
            }
        }
        [TestCase("korean_war",null,true,0)][TestCase("arab_israeli_war",null,false,2)][TestCase("indo_pakistani_war","india",true,0)][TestCase("indo_pakistani_war","pakistan",false,1)]
        public async Task Each_fixture_runs_real_commands_and_reproducible_control_modifiers(string name,string target,bool win,int penalty)
        {
            var c=Catalog();var id="demo.practice."+name;var scene=c.Scenarios.Single(s=>s.Id==id);var controller=new DemoController(c.CreateHost);controller.LoadScenario(id,PlayerSide.USSR,scene.SelectedCountry);controller.SelectCard(new CardId("card."+name));var r=await controller.PlayCardAsync(CardActionKind.Event);Assert.That(r.Accepted,Is.True,r.Error.ToString());
            if(target!=null){Assert.That(controller.ReadView().War,Is.Not.Null);controller.SelectCountry(new CountryId("country."+target));r=await controller.ConfirmWarAsync();}
            Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.War.Roll,Is.EqualTo(4));Assert.That(r.War.Penalty,Is.EqualTo(penalty));Assert.That(r.War.Success,Is.EqualTo(win));Assert.That(r.View.Practice.UssrMilitary,Is.EqualTo(2));Assert.That(r.View.Practice.CompletedActions,Is.EqualTo(1));Assert.That(r.View.Practice.VpForUS,Is.EqualTo(win?-2:0));Assert.That(await controller.RetryCardAsync(),Is.SameAs(r));
        }
        [TestCase("scope")][TestCase("extra")][TestCase("source")][TestCase("turn")][TestCase("terminal")][TestCase("seed")][TestCase("count")][TestCase("id")][TestCase("card")][TestCase("target")][TestCase("country")][TestCase("negative")][TestCase("duplicate-country")][TestCase("duplicate-hand")][TestCase("missing-root")][TestCase("china")][TestCase("late")]
        public void Invalid_or_ambiguous_fixture_does_not_modify_existing_catalog(string defect)
        {
            var c=Bind(FiveYearPlanScenarioTests.Catalog());var root=JObject.Parse(Fixture());var row=root["scenarios"][0];
            switch(defect)
            {
                case "scope":root["scope"]="release-candidate";break;case "extra":root["execute"]="unknown";break;case "source":root["source_scenario_id"]="missing";break;case "turn":root["turn_number"]=1;break;case "terminal":root["initial_vp"]=20;break;case "seed":root["random_seed"]=-1;break;case "count":((JArray)root["scenarios"]).RemoveAt(0);break;case "id":row["id"]="demo.practice.five_year_plan";break;case "card":row["card"]="card.fidel";break;case "target":row["selected_country"]="country.india";break;case "country":row["influence"][0]["country_id"]="unknown";break;case "negative":row["influence"][0]["us"]=-1;break;case "duplicate-country":((JArray)row["influence"]).Add(row["influence"][0].DeepClone());break;case "duplicate-hand":row["us_hand"][0]=row["ussr_hand"][0].DeepClone();break;case "missing-root":row["ussr_hand"][0]="card.containment";break;case "china":row["us_hand"][0]="card.china";break;case "late":row["us_hand"][0]="card.wargames";break;
            }
            Assert.Throws<ArgumentException>(()=>c.WithEarlyWarScenarios(root.ToString()));Assert.That(c.Scenarios.Count,Is.EqualTo(24));
        }
    }
}
