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
    public class SpaceEventDemoTests
    {
        internal static string Fixture()=>PlayInfluenceEventTests.Read("Data/Demo/space-event-scenarios.json");
        internal static DemoCatalog Bind(DemoCatalog c)=>c.WithSpaceEvents(PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/space-events-batch1.parameters.json"),PlayInfluenceEventTests.Read("Docs/Contracts/space-events-batch1.schema.json"),PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/cards.json"));
        internal static DemoCatalog Catalog()=>Bind(WarDemoTests.Catalog()).WithSpaceEventScenarios(Fixture());
        [Test] public void Registration_preserves_previous_rules_and_rejects_duplicates()
        {
            var old=WarDemoTests.Catalog();var c=Bind(old);Assert.That(c.Scenarios.Count,Is.EqualTo(27));
            foreach(var scene in old.Scenarios)
            {
                var n=c.Scenarios.Single(s=>s.Id==scene.Id);if(scene.PracticeDefinition==null){Assert.That(n,Is.SameAs(scene));continue;}
                var a=scene.PracticeDefinition;var d=n.PracticeDefinition;Assert.That(a.SpaceEvent,Is.Null);Assert.That(d.SpaceEvent,Is.Not.Null);Assert.That(d.SpaceEvent.Cards,Is.SameAs(d.Cards));Assert.That(d.SpaceEvent.Track,Is.SameAs(d.Space));Assert.That(d.Risk,Is.SameAs(a.Risk));Assert.That(d.Wars,Is.SameAs(a.Wars));Assert.That(d.FiveYearPlan,Is.SameAs(a.FiveYearPlan));Assert.That(d.Blockade,Is.SameAs(a.Blockade));Assert.That(d.FinalScoring,Is.SameAs(a.FinalScoring));Assert.That(d.HeadlineParameters,Is.SameAs(a.HeadlineParameters));
            }
            Assert.Throws<ArgumentException>(()=>Bind(c));
        }
        [Test] public void Later_supported_factory_chain_keeps_registered_space_event()
        {
            var old=Bind(EmptyHandDemoTests.Catalog());var c=WarDemoTests.Bind(FiveYearPlanScenarioTests.Bind(BlockadeDemoTests.Bind(IndependentRedsPlayTests.Bind(old))));
            foreach(var scene in old.Scenarios.Where(s=>s.PracticeDefinition!=null))Assert.That(c.Scenarios.Single(s=>s.Id==scene.Id).PracticeDefinition.SpaceEvent,Is.SameAs(scene.PracticeDefinition.SpaceEvent));
        }
        [Test] public void Four_fixtures_prepare_real_actor_and_card_partition_without_firing_event()
        {
            var c=Catalog();Assert.That(c.Scenarios.Count,Is.EqualTo(31));
            foreach(var row in JObject.Parse(Fixture())["scenarios"])
            {
                var scene=c.Scenarios.Single(s=>s.Id==(string)row["id"]);Assert.That(scene.Title,Does.Contain("人工局面"));var h=c.CreateHost(scene.Id);var s=scene.Actor==PlayerSide.US?h.UsSession:h.UssrSession;var v=s.ReadView();
                Assert.That(v.Practice.Actor,Is.EqualTo(scene.Actor));Assert.That(v.Practice.CompletedActions,Is.EqualTo((int)row["ordinary_actions_to_prepare"]));Assert.That(v.Practice.UsSpace,Is.EqualTo((int)row["us_space"]));Assert.That(v.Practice.UssrSpace,Is.EqualTo((int)row["ussr_space"]));Assert.That(v.Practice.VpForUS,Is.Zero);Assert.That(v.Practice.UsMilitary+v.Practice.UssrMilitary,Is.Zero);Assert.That(v.OwnHand,Has.Member(SpaceEventPlayTests.Card.Value));Assert.That(v.Cards.ResolvingCount,Is.Zero);
            }
        }
        [TestCase("first",2,1)][TestCase("second",1,1)][TestCase("ability",0,2)]
        public async Task Fixture_event_runs_through_controller_and_real_fixed_identity(string name,int award,int position)
        {
            var c=Catalog();var scene=c.Scenarios.Single(s=>s.Id=="demo.practice.space_event_"+name);var ctrl=new DemoController(c.CreateHost);ctrl.LoadScenario(scene.Id,scene.Actor,scene.SelectedCountry);ctrl.SelectCard(SpaceEventPlayTests.Card);
            var r=await ctrl.PlayCardAsync(CardActionKind.Event);Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.SpaceEvent.AwardVp,Is.EqualTo(award));Assert.That(r.SpaceEvent.ToPosition,Is.EqualTo(position));Assert.That(r.SpaceEvent.Beneficiary,Is.EqualTo(scene.Actor));Assert.That(await ctrl.RetryCardAsync(),Is.SameAs(r));
            if(name=="ability")Assert.That(r.SpaceEvent.AfterUSSR.AttemptLimit,Is.EqualTo(2));
        }
        [Test] public async Task Terminal_fixture_exposes_pending_ruling_and_allows_real_ops_instead()
        {
            var c=Catalog();var h=c.CreateHost("demo.practice.space_event_terminal");var s=h.UssrSession;var option=s.QueryCardActions().Cards.Single(x=>x.Card.Equals(SpaceEventPlayTests.Card));Assert.That(option.Event,Is.EqualTo(RuleError.EventRequiresRuling));Assert.That(option.Influence,Is.EqualTo(RuleError.None));
            var v=s.ReadView();var rejected=await s.SubmitCardActionAsync(WarSessionTests.Command(s,CardActionKind.Event,SpaceEventPlayTests.Card));Assert.That(rejected.Error,Is.EqualTo(RuleError.EventRequiresRuling));Assert.That(s.ReadView().Version,Is.EqualTo(v.Version));
            var ops=await s.SubmitCardActionAsync(WarSessionTests.Command(s,CardActionKind.Influence,SpaceEventPlayTests.Card,id:"ops"));Assert.That(ops.Accepted,Is.True);Assert.That(ops.SpaceEvent,Is.Null);Assert.That(ops.View.Practice.UssrSpace,Is.EqualTo(8));
        }
        [TestCase("scope")][TestCase("extra")][TestCase("source")][TestCase("turn")][TestCase("terminal-vp")][TestCase("seed")][TestCase("count")][TestCase("id")][TestCase("card")][TestCase("country")][TestCase("position")][TestCase("float")][TestCase("actor")][TestCase("preparation")][TestCase("duplicate-hand")][TestCase("missing-root")][TestCase("china")][TestCase("late")][TestCase("unsafe-prep")]
        public void Invalid_fixture_rejects_without_replacing_old_catalog(string defect)
        {
            var c=Bind(WarDemoTests.Catalog());var data=JObject.Parse(Fixture());var row=data["scenarios"][0];
            switch(defect)
            {
                case "scope":data["scope"]="release-candidate";break;case "extra":row["execute"]="arbitrary";break;case "source":data["source_scenario_id"]="missing";break;case "turn":data["turn_number"]=1;break;case "terminal-vp":data["initial_vp"]=20;break;case "seed":data["random_seed"]=-1;break;case "count":((JArray)data["scenarios"]).RemoveAt(0);break;case "id":row["id"]="demo.practice.five_year_plan";break;case "card":row["card"]="card.fidel";break;case "country":row["selected_country"]="missing";break;case "position":row["ussr_space"]=9;break;case "float":row["us_space"]=0.4;break;case "actor":row["actor"]="everyone";break;case "preparation":row["ordinary_actions_to_prepare"]=1;break;case "duplicate-hand":row["us_hand"][0]=row["ussr_hand"][0].DeepClone();break;case "missing-root":row["ussr_hand"][0]="card.containment";break;case "china":row["us_hand"][0]="card.china";break;case "late":row["us_hand"][0]="card.wargames";break;case "unsafe-prep":data["scenarios"][1]["ussr_hand"][0]="card.containment";break;
            }
            Assert.That(()=>c.WithSpaceEventScenarios(data.ToString()),Throws.InstanceOf<ArgumentException>());Assert.That(c.Scenarios.Count,Is.EqualTo(27));
        }
    }
}
