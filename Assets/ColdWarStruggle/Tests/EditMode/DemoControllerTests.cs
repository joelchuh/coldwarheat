using System;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class DemoControllerTests
    {
        [Test] public async Task Switching_side_does_not_grant_the_opponents_action()
        {
            var data=DemoTests.Catalog();var c=new DemoController(data.CreateHost);var scenario=data.Scenarios[0];c.LoadScenario(scenario.Id,scenario.Actor,scenario.SelectedCountry);
            c.SelectSide(PlayerSide.USSR);Assert.That(c.ReadView().Actor,Is.EqualTo(PlayerSide.US));
            Assert.That((await c.SubmitAsync(1)).Error,Is.EqualTo(RuleError.WrongActor));Assert.That(c.ReadView().Version,Is.Zero);
            c.SelectSide(PlayerSide.US);Assert.That((await c.SubmitAsync(1)).Accepted,Is.True);
        }
        [Test] public async Task Retry_is_idempotent_and_reset_drops_old_commands()
        {
            var data=DemoTests.Catalog();var c=new DemoController(data.CreateHost);var s=data.Scenarios[0];c.LoadScenario(s.Id,s.Actor,s.SelectedCountry);
            var first=await c.SubmitAsync(3);Assert.That(first.Accepted,Is.True);Assert.That(c.CanRetry,Is.True);
            Assert.That(await c.RetryAsync(),Is.SameAs(first));Assert.That(c.ReadView().RemainingOps,Is.Zero);
            var oldGame=c.ReadView().GameId;c.LoadScenario(s.Id,s.Actor,s.SelectedCountry);
            Assert.That(c.CanRetry,Is.False);Assert.That(c.LastResult,Is.Null);Assert.That(c.ReadView().GameId,Is.Not.EqualTo(oldGame));Assert.That(c.ReadView().Version,Is.Zero);
        }
        [Test] public void Unknown_selection_does_not_change_selected_country()
        {
            var data=DemoTests.Catalog();var c=new DemoController(data.CreateHost);var s=data.Scenarios[0];c.LoadScenario(s.Id,s.Actor,s.SelectedCountry);
            Assert.Throws<ArgumentException>(()=>c.SelectCountry(new CountryId("unknown")));Assert.That(c.SelectedCountry,Is.EqualTo(s.SelectedCountry));
        }
    }
}
