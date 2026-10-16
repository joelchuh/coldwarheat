using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
using UnityEngine;
namespace ColdWarStruggle.Tests
{
    public class FeedbackReaderTests
    {
        static CardId Id(string name)=>WarPlayTests.Id(name);
        static CountryId C(string name)=>WarPlayTests.Country(name);
        sealed class Fixture:IDisposable
        {
            readonly GameObject go;
            public DemoController Controller {get;}
            public DemoView View {get;}
            public Fixture(GameState state,PlayerSide side=PlayerSide.USSR)
            {
                Controller=new DemoController(_=>new LocalGameHost(state,true));go=new GameObject("feedback-reader");View=go.AddComponent<DemoView>();
                View.Initialize(Controller,HeadlineDemoCatalogTests.Catalog().Names,new[]{new DemoScenarioOption("fixture","fixture",side,C("india"))},printedCards:state.Cards.Catalog);
            }
            public async Task Play(string card)
            {Controller.SelectCard(Id(card));var result=await Controller.PlayCardAsync(CardActionKind.Event);Assert.That(result.Accepted,Is.True,result.Error.ToString());}
            public void Dispose()=>UnityEngine.Object.DestroyImmediate(go);
        }
        static void SamePosition(PlayerViewSnapshot before,PlayerViewSnapshot after)
        {
            Assert.That(after.GameId,Is.EqualTo(before.GameId));Assert.That(after.Version,Is.EqualTo(before.Version));Assert.That(after.EventSequence,Is.EqualTo(before.EventSequence));
            Assert.That(after.Countries.Select(c=>(c.Definition.Id,c.US,c.USSR)),Is.EqualTo(before.Countries.Select(c=>(c.Definition.Id,c.US,c.USSR))));
            Assert.That(after.Practice.VpForUS,Is.EqualTo(before.Practice.VpForUS));Assert.That(after.Practice.UssrMilitary,Is.EqualTo(before.Practice.UssrMilitary));
        }
        [Test] public async Task Reading_war_result_and_repeated_open_close_never_reexecutes_the_event()
        {
            using var f=new Fixture(WarPlayTests.Ready("korean_war"));Assert.That(f.View.TryOpenFeedback(),Is.False);await f.Play("korean_war");var before=f.Controller.ReadView();
            Assert.That(f.View.TryOpenFeedback(),Is.True);Assert.That(f.View.IsFeedbackReaderOpen,Is.True);Assert.That(f.View.ReadExpandedFeedback(),Is.EqualTo(f.View.ReadPracticeFeedback()).And.Contain("军事 +2"));
            f.View.CloseFeedback();Assert.That(f.View.IsFeedbackReaderOpen,Is.False);Assert.That(f.View.ReadExpandedFeedback(),Is.Null);Assert.That(f.View.TryOpenFeedback(),Is.True);SamePosition(before,f.Controller.ReadView());
        }
        [Test] public async Task Current_receipt_retry_keeps_reader_and_does_not_duplicate_rewards()
        {
            using var f=new Fixture(WarPlayTests.Ready("korean_war"));await f.Play("korean_war");Assert.That(f.View.TryOpenFeedback(),Is.True);var before=f.Controller.ReadView();var text=f.View.ReadExpandedFeedback();await f.Controller.RetryCardAsync();Assert.That(f.View.IsFeedbackReaderOpen,Is.True);Assert.That(f.View.ReadExpandedFeedback(),Is.EqualTo(text));SamePosition(before,f.Controller.ReadView());
        }
        [Test] public async Task Identity_change_and_same_game_same_version_reload_invalidate_the_reader()
        {
            using var f=new Fixture(WarPlayTests.Ready());await f.Play("indo_pakistani_war");Assert.That(f.View.TryOpenFeedback(),Is.True);f.Controller.SelectSide(PlayerSide.US);Assert.That(f.View.IsFeedbackReaderOpen,Is.False);Assert.That(f.View.ReadExpandedFeedback(),Is.Null);
            f.Controller.SelectSide(PlayerSide.USSR);await f.Controller.RetryCardAsync();Assert.That(f.View.TryOpenFeedback(),Is.True);f.Controller.LoadScenario("fixture",PlayerSide.USSR,C("india"));await f.Play("indo_pakistani_war");Assert.That(f.View.IsFeedbackReaderOpen,Is.False);Assert.That(f.View.ReadExpandedFeedback(),Is.Null);
        }
        [Test] public async Task Advancing_state_hides_open_result_without_relying_on_identity_change()
        {
            using var f=new Fixture(WarPlayTests.Ready());await f.Play("indo_pakistani_war");Assert.That(f.View.TryOpenFeedback(),Is.True);Assert.That((await f.Controller.ConfirmWarAsync()).Accepted,Is.True);Assert.That(f.View.IsFeedbackReaderOpen,Is.False);Assert.That(f.View.ReadExpandedFeedback(),Is.Null);
        }
        [Test] public async Task Two_headline_calculations_are_available_in_the_same_reader()
        {
            using var f=new Fixture(WarContinuationTests.Headlines("korean_war","arab_israeli_war"),PlayerSide.US);var c=f.Controller;c.SelectCard(Id("korean_war"));await c.SubmitSelectedHeadlineAsync();c.SelectSide(PlayerSide.USSR);c.SelectCard(Id("arab_israeli_war"));Assert.That((await c.SubmitSelectedHeadlineAsync()).Accepted,Is.True);
            Assert.That(f.View.TryOpenFeedback(),Is.True);var text=f.View.ReadExpandedFeedback();Assert.That(text,Is.EqualTo(f.View.ReadPracticeFeedback()));Assert.That(text.Split(new[]{"军事 +2"},StringSplitOptions.None).Length-1,Is.EqualTo(2));
        }
        [Test] public async Task Reading_during_a_child_choice_preserves_the_uncommitted_country_draft()
        {
            using var f=new Fixture(FiveYearPlanPlayTests.Ready(actor:PlayerSide.US),PlayerSide.US);await f.Play("five_year_plan");Assert.That(f.Controller.ToggleEventTarget(C("yugoslavia")),Is.True);var before=f.Controller.ReadView();var draft=f.Controller.EventDraft.ToArray();
            Assert.That(f.View.TryOpenFeedback(),Is.True);Assert.That(f.View.ReadExpandedFeedback(),Is.EqualTo(f.View.ReadEventFeedback()));f.View.CloseFeedback();Assert.That(f.Controller.EventDraft,Is.EqualTo(draft));SamePosition(before,f.Controller.ReadView());Assert.That(f.Controller.CanConfirmEvent,Is.True);
        }
    }
}
