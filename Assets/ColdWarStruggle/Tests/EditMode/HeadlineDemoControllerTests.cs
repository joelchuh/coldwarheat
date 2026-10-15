using System;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class HeadlineDemoControllerTests
    {
        static CardId Id(string n)=>HeadlineFlowTests.Id(n);
        static DemoController Controller(string us="defectors",string su="fidel")
        {var c=new DemoController(_=>HeadlinePanelFrameTests.Host(us,su));c.LoadScenario("test",PlayerSide.US,new CountryId("country.iran"));return c;}
        [Test] public async Task Both_identities_submit_through_same_host_and_old_receipt_does_not_reopen()
        {
            var c=Controller();c.SelectCard(Id("defectors"));var first=await c.SubmitSelectedHeadlineAsync();Assert.That(first.Accepted,Is.True);Assert.That(c.LastHeadlineResult,Is.SameAs(first));Assert.That(c.SelectedCard,Is.Null);Assert.That(c.CanRetryHeadline,Is.True);
            c.SelectSide(PlayerSide.USSR);Assert.That(c.LastHeadlineResult,Is.Null);Assert.That(c.CanRetryHeadline,Is.False);Assert.That(c.ReadView().Headline.Selection.OpponentCard,Is.Null);
            c.SelectCard(Id("fidel"));var done=await c.SubmitSelectedHeadlineAsync();Assert.That(done.Accepted,Is.True);Assert.That(c.ReadView().Headline.Status,Is.EqualTo(PublicHeadlineStatus.Completed));
            var version=c.ReadView().Version;Assert.That(await c.RetryHeadlineAsync(),Is.SameAs(done));Assert.That(c.ReadView().Version,Is.EqualTo(version));
        }
        [Test] public async Task Historic_first_receipt_returns_only_after_switching_back_and_cannot_roll_state_back()
        {
            var h=HeadlinePanelFrameTests.Host();var c=new DemoController(_=>h);c.LoadScenario("test",PlayerSide.US,new CountryId("country.iran"));c.SelectCard(Id("defectors"));var first=await c.SubmitSelectedHeadlineAsync();
            var done=HeadlineSessionTests.Submit(h.UssrSession,"fidel");c.SelectSide(PlayerSide.USSR);Assert.ThrowsAsync<InvalidOperationException>(()=>c.RetryHeadlineAsync());c.SelectSide(PlayerSide.US);
            Assert.That(c.LastHeadlineResult,Is.Null);Assert.That(await c.RetryHeadlineAsync(),Is.SameAs(first));Assert.That(c.ReadView().Version,Is.EqualTo(done.View.Version));Assert.That(c.ReadView().Headline.Status,Is.EqualTo(PublicHeadlineStatus.Completed));
        }
        [Test] public async Task Reset_discards_receipt_and_does_not_replay_into_new_host()
        {
            var c=Controller();c.SelectCard(Id("defectors"));await c.SubmitSelectedHeadlineAsync();c.LoadScenario("test",PlayerSide.US,new CountryId("country.iran"));
            Assert.That(c.LastHeadlineResult,Is.Null);Assert.That(c.CanRetryHeadline,Is.False);Assert.That(c.SelectedCard,Is.Null);Assert.That(c.ReadView().Headline.Selection.OwnLocked,Is.False);Assert.ThrowsAsync<InvalidOperationException>(()=>c.RetryHeadlineAsync());
        }
        [Test] public void No_selection_or_unimplemented_candidate_cannot_be_submitted()
        {
            var c=Controller("blockade");var v=c.ReadView().Version;Assert.ThrowsAsync<InvalidOperationException>(()=>c.SubmitSelectedHeadlineAsync());c.SelectCard(Id("blockade"));Assert.ThrowsAsync<InvalidOperationException>(()=>c.SubmitSelectedHeadlineAsync());Assert.That(c.ReadView().Version,Is.EqualTo(v));Assert.That(c.CanRetryHeadline,Is.False);
        }
        [Test] public void Other_identity_card_cannot_be_selected_for_headline()
        {var c=Controller();Assert.Throws<ArgumentException>(()=>c.SelectCard(Id("fidel")));Assert.ThrowsAsync<InvalidOperationException>(()=>c.SubmitSelectedHeadlineAsync());}
        [Test] public async Task Successful_lock_cannot_be_modified_or_submitted_twice_as_new_command()
        {
            var c=Controller();c.SelectCard(Id("defectors"));var r=await c.SubmitSelectedHeadlineAsync();Assert.ThrowsAsync<InvalidOperationException>(()=>c.SubmitSelectedHeadlineAsync());Assert.That(c.ReadView().Version,Is.EqualTo(r.View.Version));Assert.That(c.ReadView().Headline.Selection.OwnCard,Is.EqualTo(Id("defectors")));
        }
        [Test] public async Task Waiting_event_continuation_keeps_existing_owner_flow()
        {
            var h=HeadlinePanelFrameTests.Host("decolonization","defectors");var c=new DemoController(_=>h);c.LoadScenario("test",PlayerSide.US,new CountryId("country.iran"));c.SelectCard(Id("decolonization"));await c.SubmitSelectedHeadlineAsync();c.SelectSide(PlayerSide.USSR);c.SelectCard(Id("defectors"));await c.SubmitSelectedHeadlineAsync();
            Assert.That(c.ReadView().Headline.WaitingOwner,Is.EqualTo(PlayerSide.USSR));Assert.That(c.EventTargets().Error,Is.EqualTo(RuleError.None));
            Assert.That((await h.UssrSession.SubmitEventAsync(HeadlineSessionContinuationTests.Choice(h.UssrSession))).Accepted,Is.True);Assert.That(c.ReadView().Headline.Status,Is.EqualTo(PublicHeadlineStatus.Completed));
        }
    }
}
