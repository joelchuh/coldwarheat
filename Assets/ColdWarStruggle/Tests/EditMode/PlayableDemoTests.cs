using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayableDemoTests
    {
        static string Read(string path)=>File.ReadAllText(Path.Combine(DefinitionTests.Root,path));
        internal static DemoCatalog Catalog()=>DealtDemoCatalogTests.Catalog().WithActionPractice(
            Read("Data/Cards/deluxe-2015/persistent-effects-batch1.parameters.json"),Read("Docs/Contracts/persistent-effects-batch1.schema.json"),
            Read("Data/Rulesets/deluxe-2015/space-race.json"),TurnCalendarTests.Source());
        static DemoController Controller()
        {var c=new DemoController(id=>new ColdWarStruggle.Application.LocalGameHost(ActionPracticeTests.Prepared(),true));c.LoadScenario("test",PlayerSide.USSR,new CountryId("country.poland"));return c;}
        [Test] public void Catalog_enables_only_real_deal_scenario_without_changing_deployment()
        {
            var d=Catalog();Assert.That(d.Scenarios.Count,Is.EqualTo(13));
            foreach(var s in d.Scenarios)
            {var v=d.CreateHost(s.Id).UsSession.ReadView();Assert.That(v.Practice!=null,Is.EqualTo(s.Id==DealtDemoCatalogTests.ScenarioId));}
            var view=d.CreateHost(DealtDemoCatalogTests.ScenarioId).UsSession.ReadView();Assert.That(view.Deployment.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(view.Practice.Stage,Is.EqualTo(ActionPracticeStage.AwaitingStart));
            Assert.That(view.Practice.UsSpace,Is.Zero);Assert.That(view.Practice.UssrSpace,Is.Zero);Assert.That(view.OwnHand.Count,Is.EqualTo(8));
        }
        [Test] public async Task Card_selection_is_intent_only_and_clears_after_play()
        {
            var c=Controller();await c.StartPracticeAsync();var before=c.ReadView();var card=c.CardOptions().Cards.First(x=>x.Space==RuleError.None).Card;
            c.SelectCard(card);Assert.That(c.SelectedCard,Is.EqualTo(card));Assert.That(c.ReadView().Version,Is.EqualTo(before.Version));
            Assert.That((await c.PlayCardAsync(CardActionKind.Space)).Accepted,Is.True);Assert.That(c.SelectedCard,Is.Null);Assert.That(c.LastCardResult.Space,Is.Not.Null);
            var v=c.ReadView().Version;Assert.That((await c.RetryCardAsync()).Accepted,Is.True);Assert.That(c.ReadView().Version,Is.EqualTo(v));
        }
        [Test] public async Task Identity_change_cannot_reuse_other_side_selection_or_retry()
        {
            var c=Controller();await c.StartPracticeAsync();var id=c.CardOptions().Cards.First().Card;c.SelectCard(id);c.SelectSide(PlayerSide.US);
            Assert.That(c.SelectedCard,Is.Null);Assert.That(c.CanRetryCard,Is.False);Assert.Throws<ArgumentException>(()=>c.SelectCard(id));
        }
        [Test] public async Task Influence_card_drives_existing_map_targets_and_explicit_finish()
        {
            var c=Controller();await c.StartPracticeAsync();var id=c.CardOptions().Cards.First(x=>x.Influence==RuleError.None).Card;c.SelectCard(id);
            await c.PlayCardAsync(CardActionKind.Influence);Assert.That(c.LegalTargets().Targets,Does.Contain(new CountryId("country.poland")));
            Assert.That((await c.SubmitAsync(1)).Accepted,Is.True);Assert.That((await c.FinishInfluenceAsync()).Accepted,Is.True);
            Assert.That(c.ReadView().Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(c.ReadView().Cards.ResolvingCount,Is.Zero);
        }
        [Test] public async Task Reset_clears_selection_receipt_and_creates_fresh_authority()
        {
            var d=Catalog();var c=new DemoController(d.CreateHost);c.LoadScenario(DealtDemoCatalogTests.ScenarioId,PlayerSide.USSR,new CountryId("country.poland"));
            var old=c.ReadView().GameId;c.SelectCard(c.CardOptions().Cards.First().Card);
            c.LoadScenario(DealtDemoCatalogTests.ScenarioId,PlayerSide.USSR,new CountryId("country.poland"));
            Assert.That(c.ReadView().GameId,Is.Not.EqualTo(old));Assert.That(c.SelectedCard,Is.Null);Assert.That(c.LastCardResult,Is.Null);Assert.That(c.CanRetryCard,Is.False);await Task.CompletedTask;
        }
    }
}
