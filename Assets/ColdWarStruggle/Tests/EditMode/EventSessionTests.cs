using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using NUnit.Framework;

namespace ColdWarStruggle.Tests
{
    public class EventSessionTests
    {
        public static EventCommandEnvelope Begin(IGameSession s,string id="begin")
        { var v=s.ReadView();return new EventCommandEnvelope(id,v.GameId,v.Version,EventCommandKind.Begin,v.Event.ExecutionId); }
        public static EventCommandEnvelope Choice(IGameSession s,string id="choice",IEnumerable<CountryId> targets=null)
        { var v=s.ReadView();return new EventCommandEnvelope(id,v.GameId,v.Version,EventCommandKind.Resolve,v.Event.ExecutionId,v.Event.DecisionId,targets??InfluenceEventFlowTests.Four()); }
        static LocalGameHost Host(string card="decolonization")=>new LocalGameHost(InfluenceEventFlowTests.State(card),true);
        static CommandEnvelope Placement(IGameSession s,string id)
        { var v=s.ReadView();return new CommandEnvelope(id,v.GameId,v.Version,v.ContextId,new[]{InfluenceEventFlowTests.Id("angola")}); }

        [Test] public async Task Phasing_player_begins_but_beneficiary_chooses_and_all_views_share_one_game()
        {
            var h=Host();var begin=await h.UsSession.SubmitEventAsync(Begin(h.UsSession));Assert.That(begin.Accepted,Is.True);
            Assert.That(begin.View.Event.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(begin.View.Event.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));
            Assert.That(begin.View.Version,Is.EqualTo(1));Assert.That(h.UssrSession.ReadView().Version,Is.EqualTo(1));
            Assert.That((await h.UsSession.SubmitEventAsync(Choice(h.UsSession))).Error,Is.EqualTo(RuleError.WrongActor));
            var result=await h.UssrSession.SubmitEventAsync(Choice(h.UssrSession));Assert.That(result.Accepted,Is.True);
            Assert.That(result.View.Event.Stage,Is.EqualTo(InfluenceEventStage.Resolved));Assert.That(result.View.Actor,Is.EqualTo(PlayerSide.US));
            Assert.That(result.View.RemainingOps,Is.EqualTo(4));Assert.That(result.Changes.Count,Is.EqualTo(4));Assert.That(result.ResolutionSequence,Is.EqualTo(6));
            Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(2));Assert.That(h.ReadLocalTestSnapshot().PublicView.Event.Stage,Is.EqualTo(InfluenceEventStage.Resolved));
        }
        [Test] public async Task Decision_queries_are_readonly_versioned_and_owned()
        {
            var h=Host();var v=h.UsSession.ReadView();Assert.That(h.UsSession.QueryEventTargets(v.Event.ExecutionId,0).Error,Is.EqualTo(RuleError.EventNotStarted));
            await h.UsSession.SubmitEventAsync(Begin(h.UsSession));v=h.UssrSession.ReadView();
            var q=h.UssrSession.QueryEventTargets(v.Event.ExecutionId,v.Version);
            Assert.That(q.Error,Is.EqualTo(RuleError.None));Assert.That(q.Targets.Count,Is.EqualTo(25));Assert.That(q.RequiredTargets,Is.EqualTo(4));
            Assert.That(q.DecisionId,Is.EqualTo(v.Event.DecisionId));Assert.That(q.Viewer,Is.EqualTo(PlayerSide.USSR));
            Assert.Throws<NotSupportedException>(()=>((IList<CountryId>)q.Targets).Clear());
            Assert.That(h.UsSession.QueryEventTargets(v.Event.ExecutionId,v.Version).Error,Is.EqualTo(RuleError.WrongActor));
            Assert.That(h.UssrSession.QueryEventTargets(v.Event.ExecutionId,0).Error,Is.EqualTo(RuleError.StaleVersion));
            Assert.That(h.UssrSession.QueryEventTargets("other",v.Version).Error,Is.EqualTo(RuleError.StaleContext));
            Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(1));Assert.That(h.UsSession.ReadView().EventSequence,Is.EqualTo(1));
        }
        [Test] public async Task Invalid_complete_selection_is_atomic_and_rejection_receipt_is_stable()
        {
            var h=Host();await h.UsSession.SubmitEventAsync(Begin(h.UsSession));var s=h.UssrSession;
            var shortChoice=Choice(s,targets:InfluenceEventFlowTests.Four().Take(3));var rejected=await s.SubmitEventAsync(shortChoice);
            Assert.That(rejected.Error,Is.EqualTo(RuleError.InvalidSelection));Assert.That(rejected.Changes,Is.Empty);
            Assert.That(s.ReadView().Version,Is.EqualTo(1));Assert.That(s.ReadView().Event.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));
            Assert.That(await s.SubmitEventAsync(shortChoice),Is.SameAs(rejected));
            Assert.That((await s.SubmitEventAsync(Choice(s))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That((await s.SubmitEventAsync(Choice(s,"correct"))).Accepted,Is.True);
        }
        [Test] public async Task Successful_retry_returns_original_receipt_after_later_state_changes()
        {
            var h=Host();var b=Begin(h.UsSession);var a=await h.UsSession.SubmitEventAsync(b);
            var c=Choice(h.UssrSession);var done=await h.UssrSession.SubmitEventAsync(c);
            Assert.That(done.Accepted,Is.True);Assert.That((await h.UsSession.SubmitAsync(Placement(h.UsSession,"ops"))).Accepted,Is.True);
            Assert.That(await h.UsSession.SubmitEventAsync(b),Is.SameAs(a));Assert.That(await h.UssrSession.SubmitEventAsync(c),Is.SameAs(done));
            Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(3));
            Assert.That((await h.UsSession.SubmitEventAsync(Begin(h.UsSession,"again"))).Error,Is.EqualTo(RuleError.EventAlreadyResolved));
        }
        [Test] public async Task Ordinary_and_event_commands_share_one_identity_scoped_receipt_namespace()
        {
            var h=Host("fidel");var ordinary=await h.UsSession.SubmitAsync(Placement(h.UsSession,"same"));
            Assert.That(ordinary.Error,Is.EqualTo(RuleError.EventInProgress));
            Assert.That((await h.UsSession.SubmitEventAsync(Begin(h.UsSession,"same"))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            var begin=await h.UsSession.SubmitEventAsync(Begin(h.UsSession,"start"));Assert.That(begin.Accepted,Is.True);
            Assert.That((await h.UsSession.SubmitAsync(Placement(h.UsSession,"start"))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That((await h.UssrSession.SubmitEventAsync(Begin(h.UssrSession,"start"))).Error,Is.EqualTo(RuleError.EventAlreadyResolved));
            Assert.That(h.UsSession.ReadView().Version,Is.EqualTo(1));
        }
        [Test] public async Task OPS_submission_and_target_query_are_blocked_while_waiting()
        {
            var h=Host();await h.UsSession.SubmitEventAsync(Begin(h.UsSession));var s=h.UsSession;var v=s.ReadView();
            Assert.That((await s.SubmitAsync(Placement(s,"ops"))).Error,Is.EqualTo(RuleError.EventInProgress));
            Assert.That(s.QueryInfluenceTargets(v.ContextId,v.Version).Error,Is.EqualTo(RuleError.EventInProgress));
            Assert.That(s.ReadView().Version,Is.EqualTo(1));
        }
        [TestCase("game")] [TestCase("version")] [TestCase("event")] [TestCase("decision")]
        public async Task Old_or_misdirected_decision_is_rejected_without_changes(string kind)
        {
            var h=Host();await h.UsSession.SubmitEventAsync(Begin(h.UsSession));var s=h.UssrSession;var v=s.ReadView();
            var command=new EventCommandEnvelope("bad",kind=="game"?"old-game":v.GameId,kind=="version"?0:v.Version,EventCommandKind.Resolve,
                kind=="event"?"old-event":v.Event.ExecutionId,kind=="decision"?"old-decision":v.Event.DecisionId,InfluenceEventFlowTests.Four());
            var expected=kind=="game"?RuleError.WrongGame:kind=="version"?RuleError.StaleVersion:kind=="event"?RuleError.StaleContext:RuleError.StaleDecision;
            Assert.That((await s.SubmitEventAsync(command)).Error,Is.EqualTo(expected));Assert.That(s.ReadView().Version,Is.EqualTo(1));
        }
        [Test] public async Task Concurrent_complete_choices_commit_once()
        {
            var h=Host();await h.UsSession.SubmitEventAsync(Begin(h.UsSession));var a=Choice(h.UssrSession,"a");var b=Choice(h.UssrSession,"b");
            var results=await Task.WhenAll(Task.Run(()=>h.UssrSession.SubmitEventAsync(a)),Task.Run(()=>h.UssrSession.SubmitEventAsync(b)));
            Assert.That(results.Count(r=>r.Accepted),Is.EqualTo(1));Assert.That(results.Count(r=>r.Error==RuleError.StaleVersion),Is.EqualTo(1));
            Assert.That(h.UssrSession.ReadView().Version,Is.EqualTo(2));
        }
        [Test] public async Task Cancellation_before_submission_and_malformed_requests_do_not_commit()
        {
            var h=Host();var s=h.UsSession;var cancel=new CancellationTokenSource();cancel.Cancel();
            Assert.That(s.SubmitEventAsync(Begin(s),cancel.Token).IsCanceled,Is.True);
            Assert.That((await s.SubmitEventAsync(null)).Error,Is.EqualTo(RuleError.InvalidCommand));
            var v=s.ReadView();
            Assert.That((await s.SubmitEventAsync(new EventCommandEnvelope("x",v.GameId,0,(EventCommandKind)99,v.Event.ExecutionId))).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That((await s.SubmitEventAsync(new EventCommandEnvelope("x",v.GameId,0,EventCommandKind.Begin,v.Event.ExecutionId,targets:InfluenceEventFlowTests.Four()))).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(s.ReadView().Version,Is.Zero);
        }
        [Test] public async Task Caller_cannot_mutate_choice_or_obtain_opponent_hand_from_event_receipts()
        {
            var h=Host();var begun=await h.UsSession.SubmitEventAsync(Begin(h.UsSession));var targets=InfluenceEventFlowTests.Four();
            var c=Choice(h.UssrSession,targets:targets);targets[0]=InfluenceEventFlowTests.Id("france");var result=await h.UssrSession.SubmitEventAsync(c);
            Assert.That(result.Accepted,Is.True);Assert.That(result.View.OwnHand,Is.EqualTo(new[]{"private.ussr"}));
            Assert.That(begun.View.OwnHand,Is.EqualTo(new[]{"private.us"}));Assert.That(result.View.OpponentHandCount,Is.EqualTo(1));
            Assert.That(result.Changes[0].Country,Is.EqualTo(InfluenceEventFlowTests.Id("angola")));
            Assert.Throws<NotSupportedException>(()=>((IList<InfluenceChanged>)result.Changes).Clear());
        }
        [Test] public async Task Unknown_policy_stops_and_cannot_fall_back_to_OPS()
        {
            var h=new LocalGameHost(InfluenceEventFlowTests.State(policy:InfluenceEventPolicy.Unknown),true);
            var r=await h.UsSession.SubmitEventAsync(Begin(h.UsSession));Assert.That(r.Error,Is.EqualTo(RuleError.EventRequiresRuling));
            Assert.That(r.EffectError,Is.EqualTo(InfluenceEventError.InterferenceUnknown));
            Assert.That((await h.UsSession.SubmitAsync(Placement(h.UsSession,"ops"))).Error,Is.EqualTo(RuleError.EventInProgress));
        }
        [Test] public async Task Ordinary_game_cannot_accept_a_player_supplied_event_instance()
        {
            var h=new LocalGameHost(InfluenceFixture.Create(),false);var s=h.UsSession;var v=s.ReadView();
            Assert.That(v.Event,Is.Null);
            Assert.That((await s.SubmitEventAsync(new EventCommandEnvelope("x",v.GameId,v.Version,EventCommandKind.Begin,"invented"))).Error,Is.EqualTo(RuleError.EventNotAvailable));
            Assert.That(s.QueryEventTargets("invented",v.Version).Error,Is.EqualTo(RuleError.EventNotAvailable));
            Assert.That(s.ReadView().Version,Is.EqualTo(v.Version));
        }
        [Test] public async Task Fresh_host_rejects_previous_games_choice_even_when_event_and_decision_ids_repeat()
        {
            var old=Host();await old.UsSession.SubmitEventAsync(Begin(old.UsSession));var oldCommand=Choice(old.UssrSession);
            var fresh=new LocalGameHost(InfluenceEventFlowTests.State(gameId:"fresh-game"),true);
            await fresh.UsSession.SubmitEventAsync(Begin(fresh.UsSession));
            Assert.That((await fresh.UssrSession.SubmitEventAsync(oldCommand)).Error,Is.EqualTo(RuleError.WrongGame));
            Assert.That(fresh.UssrSession.ReadView().Event.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));
            Assert.That(fresh.UssrSession.ReadView().Version,Is.EqualTo(1));
        }
    }
}
