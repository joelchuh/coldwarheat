using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Presentation;
using NUnit.Framework;

namespace ColdWarStruggle.Tests
{
    public class EventDemoControllerTests
    {
        static DemoController Controller(string card="decolonization")
        {
            var c=new DemoController(_=>new LocalGameHost(InfluenceEventFlowTests.State(card,gameId:Guid.NewGuid().ToString("N")),true));
            c.LoadScenario("fixture",PlayerSide.US,InfluenceEventFlowTests.Id("angola"));return c;
        }
        static void SelectFour(DemoController c) { foreach(var id in InfluenceEventFlowTests.Four())Assert.That(c.ToggleEventTarget(id),Is.True); }
        [Test] public async Task Clicking_the_current_side_again_keeps_the_selection_draft()
        {
            var c=Controller();await c.BeginEventAsync();c.SelectSide(PlayerSide.USSR);SelectFour(c);
            c.SelectSide(PlayerSide.USSR);Assert.That(c.EventDraft.Count,Is.EqualTo(4));Assert.That(c.CanConfirmEvent,Is.True);
        }
        [Test] public async Task Draft_edits_do_not_change_state_and_only_owner_can_select_candidates()
        {
            var c=Controller();Assert.That((await c.BeginEventAsync()).Accepted,Is.True);
            Assert.That(c.EventTargets().Error,Is.EqualTo(RuleError.WrongActor));Assert.That(c.ToggleEventTarget(InfluenceEventFlowTests.Id("angola")),Is.False);
            c.SelectSide(PlayerSide.USSR);Assert.That(c.ReadView().Actor,Is.EqualTo(PlayerSide.US));
            SelectFour(c);Assert.That(c.EventDraft.Count,Is.EqualTo(4));Assert.That(c.CanConfirmEvent,Is.True);
            Assert.That(c.ReadView().Version,Is.EqualTo(1));Assert.That(c.ReadView().Countries.First(x=>x.Definition.Id.Equals(InfluenceEventFlowTests.Id("angola"))).USSR,Is.Zero);
            Assert.That(c.ToggleEventTarget(InfluenceEventFlowTests.Id("france")),Is.False);
            Assert.That(c.ToggleEventTarget(InfluenceEventFlowTests.Id("kenya")),Is.False);
            Assert.Throws<NotSupportedException>(()=>((IList<CountryId>)c.EventDraft).Clear());
            Assert.That(c.ToggleEventTarget(InfluenceEventFlowTests.Four()[0]),Is.True);Assert.That(c.CanConfirmEvent,Is.False);
            Assert.That(c.ToggleEventTarget(InfluenceEventFlowTests.Id("kenya")),Is.True);Assert.That(c.EventDraft.Count,Is.EqualTo(4));
            c.ClearEventDraft();Assert.That(c.EventDraft,Is.Empty);Assert.That(c.ReadView().Version,Is.EqualTo(1));
        }
        [Test] public async Task Full_draft_commits_once_and_retry_returns_original_receipt()
        {
            var c=Controller();await c.BeginEventAsync();c.SelectSide(PlayerSide.USSR);SelectFour(c);
            var result=await c.ConfirmEventAsync();Assert.That(result.Accepted,Is.True);Assert.That(c.LastEventResult,Is.SameAs(result));
            Assert.That(c.EventDraft,Is.Empty);Assert.That(c.ReadView().Event.Stage,Is.EqualTo(InfluenceEventStage.Resolved));
            Assert.That(c.CanRetryEvent,Is.True);Assert.That(await c.RetryEventAsync(),Is.SameAs(result));Assert.That(c.ReadView().Version,Is.EqualTo(2));
        }
        [Test] public async Task Incomplete_submission_is_rejected_and_keeps_editable_draft()
        {
            var c=Controller();await c.BeginEventAsync();c.SelectSide(PlayerSide.USSR);
            c.ToggleEventTarget(InfluenceEventFlowTests.Four()[0]);Assert.That(c.CanConfirmEvent,Is.False);
            var rejected=await c.ConfirmEventAsync();Assert.That(rejected.Error,Is.EqualTo(RuleError.InvalidSelection));
            Assert.That(c.EventDraft.Count,Is.EqualTo(1));Assert.That(c.ReadView().Version,Is.EqualTo(1));
            foreach(var id in InfluenceEventFlowTests.Four().Skip(1))c.ToggleEventTarget(id);
            Assert.That((await c.ConfirmEventAsync()).Accepted,Is.True);
        }
        [Test] public async Task Switching_side_and_reset_clear_draft_without_granting_event_ownership()
        {
            var c=Controller();await c.BeginEventAsync();c.SelectSide(PlayerSide.USSR);SelectFour(c);
            c.SelectSide(PlayerSide.US);Assert.That(c.EventDraft,Is.Empty);Assert.That(c.CanConfirmEvent,Is.False);
            c.SelectSide(PlayerSide.USSR);Assert.That(c.EventDraft,Is.Empty);
            var oldGame=c.ReadView().GameId;c.LoadScenario("fixture",PlayerSide.US,InfluenceEventFlowTests.Id("angola"));
            Assert.That(c.ReadView().GameId,Is.Not.EqualTo(oldGame));Assert.That(c.CanRetryEvent,Is.False);Assert.That(c.LastEventResult,Is.Null);
            Assert.That(c.ReadView().Event.Stage,Is.EqualTo(InfluenceEventStage.Ready));Assert.That(c.ReadView().Version,Is.Zero);
        }
        [Test] public async Task Fixed_event_button_executes_once_and_clear_draft_does_not_reset_game()
        {
            var c=Controller("fidel");var first=await c.BeginEventAsync();Assert.That(first.Accepted,Is.True);
            Assert.That((await c.BeginEventAsync()).Error,Is.EqualTo(RuleError.EventAlreadyResolved));
            c.ClearEventDraft();Assert.That(c.ReadView().Version,Is.EqualTo(1));Assert.That(c.ReadView().Event.Stage,Is.EqualTo(InfluenceEventStage.Resolved));
        }
        [Test] public async Task External_state_change_invalidates_local_draft_before_it_can_be_reused()
        {
            var h=new LocalGameHost(InfluenceEventFlowTests.State(),true);var c=new DemoController(_=>h);
            c.LoadScenario("fixture",PlayerSide.US,InfluenceEventFlowTests.Id("angola"));await c.BeginEventAsync();c.SelectSide(PlayerSide.USSR);SelectFour(c);
            await h.UssrSession.SubmitEventAsync(EventSessionTests.Choice(h.UssrSession));
            Assert.That(c.EventDraft,Is.Empty);Assert.That(c.CanConfirmEvent,Is.False);Assert.That(c.EventTargets().Error,Is.EqualTo(RuleError.EventAlreadyResolved));
        }
    }
}
