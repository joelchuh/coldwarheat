using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Infrastructure;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ColdWarStruggle.Tests
{
    public class EventDemoCatalogTests
    {
        public static string Fixtures=>File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Demo/event-scenarios.json"));
        public static DemoCatalog Catalog()=>DemoTests.Catalog().WithEventScenarios(Fixtures,InfluenceEventTests.Parameters,InfluenceEventTests.Schema);
        [Test] public async Task Five_event_fixtures_use_shared_definitions_and_each_completes_through_real_sessions()
        {
            var catalog=Catalog();Assert.That(catalog.Scenarios.Count,Is.EqualTo(7));
            foreach(var scenario in catalog.Scenarios.Skip(2))
            {
                var host=catalog.CreateHost(scenario.Id);var start=scenario.Actor==PlayerSide.US?host.UsSession:host.UssrSession;
                var view=start.ReadView();Assert.That(view.Event.Stage,Is.EqualTo(InfluenceEventStage.Ready));Assert.That(view.RemainingOps,Is.Zero);
                var result=await start.SubmitEventAsync(EventSessionTests.Begin(start));Assert.That(result.Accepted,Is.True,scenario.Id);
                if(result.View.Event.Stage==InfluenceEventStage.AwaitingChoice)
                {
                    var owner=result.View.Event.Owner==PlayerSide.US?host.UsSession:host.UssrSession;var v=owner.ReadView();
                    var q=owner.QueryEventTargets(v.Event.ExecutionId,v.Version);
                    result=await owner.SubmitEventAsync(EventSessionTests.Choice(owner,targets:q.Targets.Take(q.RequiredTargets)));
                }
                Assert.That(result.Accepted,Is.True,scenario.Id);Assert.That(result.View.Event.Stage,Is.EqualTo(InfluenceEventStage.Resolved));
                Assert.That(result.View.RemainingOps,Is.Zero);Assert.That(catalog.CreateHost(scenario.Id).UsSession.ReadView().GameId,Is.Not.EqualTo(view.GameId));
            }
            Assert.That(catalog.CreateHost("demo.event.fidel").UsSession.ReadView().Event.CardId,Is.EqualTo("card.fidel"));
        }
        [TestCase("unknown-card")] [TestCase("non-fixture")] [TestCase("duplicate")]
        [TestCase("unknown-country")] [TestCase("budget")] [TestCase("extra-field")]
        public void Untrusted_or_ambiguous_fixtures_fail_before_catalog_is_published(string kind)
        {
            var json=JObject.Parse(Fixtures);var first=(JObject)json["scenarios"][0];
            if(kind=="unknown-card")first["event_card"]="card.unimplemented";
            if(kind=="non-fixture")json["scope"]="official";
            if(kind=="duplicate")first["id"]=DemoTests.Catalog().Scenarios[0].Id;
            if(kind=="unknown-country")first["selected_country"]="country.missing";
            if(kind=="budget")first["ops"]=4;
            if(kind=="extra-field")first["beneficiary"]="US";
            Assert.Throws<ArgumentException>(()=>DemoTests.Catalog().WithEventScenarios(json.ToString(),InfluenceEventTests.Parameters,InfluenceEventTests.Schema));
        }
        [Test] public void Standard_build_payload_contains_the_exact_event_fixtures_and_parameter_contract()
        {
            ColdWarStruggle.Editor.DemoBuild.SynchronizeData();var root=Path.Combine(UnityEngine.Application.streamingAssetsPath,"ColdWarStruggle");
            Assert.That(File.ReadAllText(Path.Combine(root,"event-scenarios.json")),Is.EqualTo(Fixtures));
            Assert.That(File.ReadAllText(Path.Combine(root,"early-influence-batch1.parameters.json")),Is.EqualTo(InfluenceEventTests.Parameters));
            Assert.That(File.ReadAllText(Path.Combine(root,"early-influence-parameters.schema.json")),Is.EqualTo(InfluenceEventTests.Schema));
        }
    }
}
