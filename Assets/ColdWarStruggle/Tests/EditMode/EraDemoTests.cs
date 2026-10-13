using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Presentation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class EraDemoTests
    {
        public static string Fixtures=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Demo/era-scenarios.json"));
        static CountryId Id(string n)=>BudgetedRemovalTests.Id(n);
        public static DemoCatalog Catalog(string fixtures=null)=>AllocationDemoCatalogTests.Catalog().WithEraScenarios(fixtures??Fixtures,
            EraRemovalTests.Parameters(),EraRemovalTests.Schema(),AllocationDemoCatalogTests.Cards,TurnCalendarTests.Source());
        [TestCase(7,GameEra.Mid,1,2)] [TestCase(8,GameEra.Late,0,1)]
        public async Task Actual_controller_runs_the_two_era_lessons(int number,GameEra era,int finlandAfter,int polandAfter)
        {
            var catalog=Catalog();Assert.That(catalog.Scenarios.Count,Is.EqualTo(11));
            var scenario=catalog.Scenarios.Single(s=>s.Id=="demo.event.east_european_unrest.turn"+number);
            var c=new DemoController(catalog.CreateHost);c.LoadScenario(scenario.Id,scenario.Actor,scenario.SelectedCountry);
            var initial=c.ReadView();Assert.That(initial.Turn.Number,Is.EqualTo(number));Assert.That(initial.Turn.Era,Is.EqualTo(era));
            Assert.That(initial.Turn,Is.SameAs(scenario.Turn));Assert.That((await c.BeginEventAsync()).Accepted,Is.True);
            Assert.That(c.ToggleEventTarget(Id("poland")),Is.False);c.SelectSide(PlayerSide.US);
            foreach(var country in new[]{"austria","finland","poland"})Assert.That(c.ToggleEventTarget(Id(country)),Is.True);
            Assert.That(c.CanConfirmEvent,Is.True);Assert.That(c.ReadView().Version,Is.EqualTo(1));
            var result=await c.ConfirmEventAsync();Assert.That(result.Accepted,Is.True);Assert.That(result.View.Turn,Is.SameAs(initial.Turn));
            Assert.That(result.View.Countries.Single(x=>x.Definition.Id.Equals(Id("austria"))).USSR,Is.Zero);
            Assert.That(result.View.Countries.Single(x=>x.Definition.Id.Equals(Id("finland"))).USSR,Is.EqualTo(finlandAfter));
            Assert.That(result.View.Countries.Single(x=>x.Definition.Id.Equals(Id("poland"))).USSR,Is.EqualTo(polandAfter));
            Assert.That(result.View.Countries.Single(x=>x.Definition.Id.Equals(Id("romania"))).USSR,Is.EqualTo(2));
            Assert.That(await c.RetryEventAsync(),Is.SameAs(result));Assert.That(c.ReadView().Version,Is.EqualTo(2));
            c.LoadScenario(scenario.Id,scenario.Actor,scenario.SelectedCountry);Assert.That(c.ReadView().GameId,Is.Not.EqualTo(initial.GameId));
            Assert.That(c.ReadView().Version,Is.Zero);Assert.That(c.ReadView().Turn.Number,Is.EqualTo(number));Assert.That(c.EventDraft,Is.Empty);
        }
        [Test] public void Old_lessons_still_have_no_invented_turn_and_new_catalog_does_not_mutate_base()
        {
            var before=AllocationDemoCatalogTests.Catalog();var after=before.WithEraScenarios(Fixtures,EraRemovalTests.Parameters(),EraRemovalTests.Schema(),AllocationDemoCatalogTests.Cards,TurnCalendarTests.Source());
            Assert.That(before.Scenarios.Count,Is.EqualTo(9));Assert.That(after.Scenarios.Count,Is.EqualTo(11));
            foreach(var scenario in before.Scenarios)Assert.That(after.CreateHost(scenario.Id).UsSession.ReadView().Turn,Is.Null);
        }
        [TestCase("missing")] [TestCase("zero")] [TestCase("past-end")] [TestCase("decimal")]
        [TestCase("overflow")] [TestCase("era")] [TestCase("amount")] [TestCase("duplicate")]
        [TestCase("unknown-card")] [TestCase("non-fixture")] [TestCase("unknown-country")]
        public void Era_fixtures_cannot_override_rules_or_use_unknown_turns(string kind)
        {
            var root=JObject.Parse(Fixtures);var row=(JObject)root["scenarios"][0];
            switch(kind)
            {
                case "missing":row.Remove("turn_number");break;
                case "zero":row["turn_number"]=0;break;
                case "past-end":row["turn_number"]=11;break;
                case "decimal":row["turn_number"]=7.5;break;
                case "overflow":row["turn_number"]=long.MaxValue;break;
                case "era":row["era"]="late";break;
                case "amount":row["removal_amount"]=99;break;
                case "duplicate":row["id"]="demo.event.suez_crisis";break;
                case "unknown-card":row["event_card"]="card.suez_crisis";break;
                case "non-fixture":root["scope"]="official";break;
                case "unknown-country":row["selected_country"]="country.unknown";break;
            }
            Assert.That(()=>Catalog(root.ToString()),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Base_and_allocation_entrypoints_still_reject_extra_turn_field()
        {
            var root=JObject.Parse(AllocationDemoCatalogTests.Fixtures);root["scenarios"][0]["turn_number"]=8;
            Assert.That(()=>EventDemoCatalogTests.Catalog().WithAllocationScenarios(root.ToString(),EraRemovalTests.Parameters(),EraRemovalTests.Schema(),AllocationDemoCatalogTests.Cards),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Build_payload_includes_same_calendar_and_era_fixtures_for_eleven_lessons()
        {
            ColdWarStruggle.Editor.DemoBuild.SynchronizeData();var dir=Path.Combine(UnityEngine.Application.streamingAssetsPath,"ColdWarStruggle");
            string Read(string name)=>File.ReadAllText(Path.Combine(dir,name));
            Assert.That(File.Exists(Path.Combine(dir,"era-scenarios.json")),Is.True,"Era fixture payload missing");
            Assert.That(Read("era-scenarios.json"),Is.EqualTo(Fixtures));Assert.That(Read("turn-flow.json"),Is.EqualTo(TurnCalendarTests.Source()));
            var catalog=DemoCatalog.Load(Read("world.json"),Read("map.schema.json"),Read("influence-scenarios.json"),Read("influence-placement.json"),Read("names.json"),Read("world.profile.json"))
                .WithEventScenarios(Read("event-scenarios.json"),Read("early-influence-batch1.parameters.json"),Read("early-influence-parameters.schema.json"))
                .WithAllocationScenarios(Read("allocation-scenarios.json"),Read("early-influence-batch2.parameters.json"),Read("early-influence-batch2.schema.json"),Read("cards.json"))
                .WithEraScenarios(Read("era-scenarios.json"),Read("early-influence-batch2.parameters.json"),Read("early-influence-batch2.schema.json"),Read("cards.json"),Read("turn-flow.json"));
            Assert.That(catalog.Scenarios.Count,Is.EqualTo(11));Assert.That(catalog.Scenarios.Last().Turn.Number,Is.EqualTo(8));
        }
    }
}
