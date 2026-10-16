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
    public sealed class OlympicDemoTests
    {
        internal static string Fixture()=>PlayInfluenceEventTests.Read("Data/Demo/olympic-scenarios.json");
        internal static DemoCatalog Bind(DemoCatalog c)=>c.WithOlympicEvents(OlympicDefinitionTests.Data,OlympicDefinitionTests.Schema,PlayInfluenceEventTests.Read("Data/Rulesets/deluxe-2015/standard-dice.json"),PlayInfluenceEventTests.Read("Docs/Contracts/standard-dice.schema.json"),PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/cards.json"));
        internal static DemoCatalog Catalog()=>Bind(SpaceEventDemoTests.Catalog()).WithOlympicScenarios(Fixture());
        [Test] public void Registration_preserves_previous_features_and_rejects_duplicate_binding()
        {
            var before=SpaceEventDemoTests.Catalog();var c=Bind(before);Assert.That(c.Scenarios.Count,Is.EqualTo(31));
            foreach(var old in before.Scenarios.Where(s=>s.PracticeDefinition!=null))
            {var d=c.Scenarios.Single(s=>s.Id==old.Id).PracticeDefinition;Assert.That(d.Olympic.Cards,Is.SameAs(d.Cards));Assert.That(d.SpaceEvent,Is.SameAs(old.PracticeDefinition.SpaceEvent));Assert.That(d.Wars,Is.SameAs(old.PracticeDefinition.Wars));Assert.That(d.Risk,Is.SameAs(old.PracticeDefinition.Risk));Assert.That(d.HeadlineParameters,Is.SameAs(old.PracticeDefinition.HeadlineParameters));}
            Assert.Throws<ArgumentException>(()=>Bind(c));Assert.Throws<ArgumentException>(()=>Bind(PlayableDemoTests.Catalog()));
        }
        [Test] public void Early_and_late_production_copy_paths_keep_olympic_registration()
        {
            var c=Bind(PlayCoupCatalogTests.Risk(PlayableDemoTests.Catalog()));var definition=c.Scenarios.Single(s=>s.PracticeDefinition!=null).PracticeDefinition.Olympic;
            c=c.WithPracticeScoring(PlayScoringCatalogTests.Data(),PlayScoringCatalogTests.Schema());c=PlayInfluenceEventCatalogTests.Add(c);c=PlayDefconVpCatalogTests.Add(c);c=PlayTurnOpsCatalogTests.Add(c);c=PlayRealignmentCatalogTests.Add(c);c=ChinaCardSessionTests.Add(c);c=c.WithPracticeTurnEnd().WithPracticeNextTurn().WithQuickStartScenario(QuickStartDemoTests.Data());c=HeadlineDemoCatalogTests.Add(c);
            foreach(var scene in c.Scenarios.Where(s=>s.PracticeDefinition!=null))Assert.That(scene.PracticeDefinition.Olympic,Is.SameAs(definition));
            var old=Bind(EmptyHandDemoTests.Catalog());c=SpaceEventDemoTests.Bind(WarDemoTests.Bind(FiveYearPlanScenarioTests.Bind(BlockadeDemoTests.Bind(IndependentRedsPlayTests.Bind(old)))));
            foreach(var scene in old.Scenarios.Where(s=>s.PracticeDefinition!=null))Assert.That(c.Scenarios.Single(s=>s.Id==scene.Id).PracticeDefinition.Olympic,Is.SameAs(scene.PracticeDefinition.Olympic));
        }
        [Test] public void Four_fixtures_have_real_cards_actor_and_declared_defcon_without_executing_event()
        {
            var c=Catalog();Assert.That(c.Scenarios.Count,Is.EqualTo(35));
            foreach(var row in JObject.Parse(Fixture())["scenarios"])
            {var scene=c.Scenarios.Single(s=>s.Id==(string)row["id"]);var h=c.CreateHost(scene.Id);var s=scene.Actor==PlayerSide.US?h.UsSession:h.UssrSession;var v=s.ReadView();Assert.That(v.Practice.Actor,Is.EqualTo(scene.Actor));Assert.That(v.Practice.Defcon,Is.EqualTo((int)row["defcon"]));Assert.That(v.Practice.CompletedActions,Is.EqualTo((int)row["ordinary_actions_to_prepare"]));Assert.That(v.OwnHand,Has.Member(OlympicPlayTests.Card.Value));Assert.That(v.Cards.ResolvingCount,Is.Zero);Assert.That(v.Olympic,Is.Null);Assert.That(v.Practice.VpForUS,Is.Zero);Assert.That(v.Practice.UsMilitary+v.Practice.UssrMilitary,Is.Zero);Assert.That(v.Countries.Single(x=>x.Definition.Id.Equals(new CountryId("country.angola"))).US,Is.EqualTo(3));}
        }
        [TestCase("contest_us")][TestCase("contest_ussr")][TestCase("boycott_nuclear")][TestCase("boycott_ops")]
        public async Task Shared_fixture_is_playable_through_controller_and_fixed_identity(string name)
        {
            var c=Catalog();var scene=c.Scenarios.Single(s=>s.Id=="demo.practice.olympic_"+name);var ctrl=new DemoController(c.CreateHost);ctrl.LoadScenario(scene.Id,scene.Actor,scene.SelectedCountry);ctrl.SelectCard(OlympicPlayTests.Card);
            Assert.That((await ctrl.PlayCardAsync(CardActionKind.Event)).Accepted,Is.True);ctrl.SelectSide(scene.Actor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US);var r=await ctrl.RespondOlympicAsync(name.StartsWith("contest"));Assert.That(r.Accepted,Is.True);
            if(name=="boycott_nuclear"){Assert.That(r.Olympic.Terminal,Is.True);Assert.That(r.Olympic.DefconAfter,Is.EqualTo(1));}
            if(name=="boycott_ops"){ctrl.SelectSide(scene.Actor);Assert.That(ctrl.CardOptions().PendingOperations.CurrentOps,Is.EqualTo(4));Assert.That((await ctrl.ChooseOlympicOperationsAsync(OperationsMode.Influence)).Accepted,Is.True);Assert.That((await ctrl.SubmitAsync(1)).Accepted,Is.True);Assert.That((await ctrl.FinishInfluenceAsync()).Accepted,Is.True);}
        }
        [TestCase("scope")][TestCase("extra")][TestCase("source")][TestCase("turn")][TestCase("defcon")][TestCase("float")][TestCase("count")][TestCase("card")][TestCase("duplicate-hand")][TestCase("late")][TestCase("unsafe-prep")][TestCase("duplicate-country")]
        public void Invalid_fixture_rejects_without_replacing_source(string defect)
        {
            var c=Bind(SpaceEventDemoTests.Catalog());var data=JObject.Parse(Fixture());var row=data["scenarios"][0];
            switch(defect)
            {case "scope":data["scope"]="release";break;case "extra":row["execute"]="unknown";break;case "source":data["source_scenario_id"]="missing";break;case "turn":data["turn_number"]=1;break;case "defcon":row["defcon"]=1;break;case "float":row["defcon"]=3.5;break;case "count":((JArray)data["scenarios"]).RemoveAt(0);break;case "card":row["card"]="card.fidel";break;case "duplicate-hand":row["ussr_hand"][0]=row["us_hand"][0].DeepClone();break;case "late":row["ussr_hand"][0]="card.wargames";break;case "unsafe-prep":row["ussr_hand"][0]="card.containment";break;case "duplicate-country":((JArray)row["influence"]).Add(row["influence"][0].DeepClone());break;}
            Assert.That(()=>c.WithOlympicScenarios(data.ToString()),Throws.InstanceOf<ArgumentException>());Assert.That(c.Scenarios.Count,Is.EqualTo(31));
        }
    }
}
