using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class DemoTests
    {
        public static DemoCatalog Catalog() => DemoCatalog.Load(DefinitionTests.MapJson,DefinitionTests.Schema,
            File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Demo/influence-scenarios.json")),
            File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Rulesets/deluxe-2015/influence-placement.json")),
            File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Localization/en-US.catalog-names.json")),DefinitionTests.Profile);
        [Test] public async Task Both_scenarios_are_fresh_bounded_worlds_with_working_operations()
        {
            var c=Catalog();Assert.That(c.Map.Countries.Count,Is.EqualTo(84));Assert.That(c.Scenarios.Count,Is.EqualTo(2));
            foreach(var scenario in c.Scenarios)
            {
                var h=c.CreateHost(scenario.Id);var session=scenario.Actor==PlayerSide.US?h.UsSession:h.UssrSession;var v=session.ReadView();
                Assert.That(v.Actor,Is.EqualTo(scenario.Actor));Assert.That(v.Countries.Count,Is.EqualTo(84));
                var r=await session.SubmitAsync(new CommandEnvelope("demo",v.GameId,v.Version,v.ContextId,Enumerable.Repeat(scenario.SelectedCountry,3)));
                Assert.That(r.Accepted,Is.True);Assert.That(r.Costs,Is.EqualTo(new[]{2,1,1}));Assert.That(r.View.RemainingOps,Is.Zero);
                Assert.That(h.ReadLocalTestSnapshot().USHand.Count,Is.GreaterThan(0));Assert.That(h.ReadLocalTestSnapshot().USSRHand.Count,Is.GreaterThan(0));
                var reset=c.CreateHost(scenario.Id);Assert.That(reset.UsSession.ReadView().Version,Is.Zero);Assert.That(reset.UsSession.ReadView().GameId,Is.Not.EqualTo(v.GameId));
            }
        }
        [Test] public void Bad_demo_actor_and_unknown_scenario_rejected()
        {
            Assert.Throws<ArgumentException>(()=>Catalog().CreateHost("missing"));
            var text=File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Demo/influence-scenarios.json"));
            Assert.Throws<ArgumentException>(()=>DemoCatalog.Load(DefinitionTests.MapJson,DefinitionTests.Schema,text.Replace("\"actor\": \"US\"","\"actor\": \"BOGUS\""),
                File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Rulesets/deluxe-2015/influence-placement.json")),File.ReadAllText(Path.Combine(DefinitionTests.Root,"Data/Localization/en-US.catalog-names.json")),DefinitionTests.Profile));
        }
    }
}
