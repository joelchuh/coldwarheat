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
    public class AllocationSessionTests
    {
        static CountryId Id(string name) => BudgetedRemovalTests.Id(name);
        static InfluenceAllocation A(string country,int amount) => BudgetedRemovalTests.A(country,amount);
        static InfluenceAllocation[] Valid() => new[] { A("france",2), A("uk",1), A("israel",1) };
        public static LocalGameHost Host(string game="allocation-game",bool zero=false)
        {
            var initial=zero?BudgetedRemovalTests.State():BudgetedRemovalTests.State(("france",2,0),("uk",2,0),("israel",2,0));
            var state=new GameState(initial.Map,initial.Countries,
                new OperationsContext("ops",PlayerSide.US,4,new[]{Id("france")},costs:new InfluenceCosts(1,2)),game,
                hands:new Dictionary<PlayerSide,IReadOnlyList<string>> { [PlayerSide.US]=new[]{"secret.us"},[PlayerSide.USSR]=new[]{"secret.ussr"} },
                influenceEvent:new InfluenceEventExecution("allocation-event",BudgetedRemovalCatalogTests.Load()["card.suez_crisis"],PlayerSide.US,InfluenceEventPolicy.NoActiveInterference));
            return new LocalGameHost(state,true);
        }
        static EventCommandEnvelope Choice(IGameSession session,string id="allocation",IEnumerable<InfluenceAllocation> values=null)
        {
            var view=session.ReadView();return EventCommandEnvelope.Allocate(id,view.GameId,view.Version,view.Event.ExecutionId,view.Event.DecisionId,values??Valid());
        }
        static CommandEnvelope Placement(IGameSession s,string id)
        { var v=s.ReadView();return new CommandEnvelope(id,v.GameId,v.Version,v.ContextId,new[]{Id("france")}); }
        static Task<EventCommandResult> Begin(LocalGameHost h)=>h.UsSession.SubmitEventAsync(EventSessionTests.Begin(h.UsSession));

        [Test] public async Task Owner_queries_immutable_capacity_and_phasing_player_cannot_choose()
        {
            var host=Host();var started=await Begin(host);Assert.That(started.Accepted,Is.True);
            var us=host.UsSession;var ussr=host.UssrSession;var view=ussr.ReadView();
            var query=ussr.QueryEventTargets(view.Event.ExecutionId,view.Version);
            Assert.That(query.Kind,Is.EqualTo(InfluenceDecisionKind.Allocation));Assert.That(query.RequiredAmount,Is.EqualTo(4));
            Assert.That(query.RequiredTargets,Is.Zero);Assert.That(query.Capacities.Select(c=>c.Amount),Is.EqualTo(new[]{2,2,2}));
            Assert.That(query.Targets,Is.EqualTo(new[]{Id("france"),Id("israel"),Id("uk")}));
            Assert.Throws<NotSupportedException>(()=>((IList<InfluenceAllocation>)query.Capacities).Clear());
            var denied=us.QueryEventTargets(view.Event.ExecutionId,view.Version);
            Assert.That(denied.Error,Is.EqualTo(RuleError.WrongActor));Assert.That(denied.Capacities,Is.Empty);Assert.That(denied.RequiredAmount,Is.Zero);
            Assert.That((await us.SubmitEventAsync(Choice(us))).Error,Is.EqualTo(RuleError.WrongActor));
            Assert.That(view.OwnHand,Is.EqualTo(new[]{"secret.ussr"}));Assert.That(view.OpponentHandCount,Is.EqualTo(1));
            Assert.That(view.Actor,Is.EqualTo(PlayerSide.US));Assert.That(us.ReadView().Version,Is.EqualTo(1));
        }

        [Test] public async Task Three_country_allocation_commits_once_and_retry_preserves_the_original_receipt()
        {
            var host=Host();await Begin(host);var session=host.UssrSession;var values=Valid();var command=Choice(session,values:values);
            values[0]=A("france",999);
            var result=await session.SubmitEventAsync(command);
            Assert.That(result.Accepted,Is.True);Assert.That(result.View.Version,Is.EqualTo(2));
            Assert.That(result.View.Event.Stage,Is.EqualTo(InfluenceEventStage.Resolved));Assert.That(result.View.RemainingOps,Is.EqualTo(4));
            Assert.That(result.Changes.Select(c=>c.Country),Is.EqualTo(new[]{Id("france"),Id("israel"),Id("uk")}));
            Assert.That(result.Changes.Select(c=>c.After),Is.EqualTo(new[]{0,1,1}));Assert.That(result.ResolutionSequence,Is.EqualTo(5));
            Assert.That(result.View.OwnHand,Is.EqualTo(new[]{"secret.ussr"}));
            Assert.That((await host.UsSession.SubmitAsync(Placement(host.UsSession,"ops"))).Accepted,Is.True);
            Assert.That(host.UsSession.ReadView().Version,Is.EqualTo(3));
            Assert.That(await session.SubmitEventAsync(command),Is.SameAs(result));
            Assert.That(session.ReadView().Version,Is.EqualTo(3));
            Assert.Throws<NotSupportedException>(()=>((IList<InfluenceAllocation>)command.Allocations).Clear());
        }

        [TestCase("amount")] [TestCase("country")] [TestCase("order")] [TestCase("kind")]
        public async Task Same_command_id_with_changed_payload_is_a_conflict(string kind)
        {
            var host=Host();await Begin(host);var s=host.UssrSession;var original=Choice(s);
            var receipt=await s.SubmitEventAsync(original);Assert.That(receipt.Accepted,Is.True);
            var values=Valid();if(kind=="amount")values[0]=A("france",1);if(kind=="country")values[0]=A("egypt",2);if(kind=="order")Array.Reverse(values);
            var changed=kind=="kind" ? new EventCommandEnvelope(original.CommandId,original.GameId,original.ExpectedVersion,EventCommandKind.Resolve,
                original.ExecutionId,original.DecisionId,new[]{Id("france")}) : EventCommandEnvelope.Allocate(original.CommandId,original.GameId,
                original.ExpectedVersion,original.ExecutionId,original.DecisionId,values);
            Assert.That((await s.SubmitEventAsync(changed)).Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That(s.ReadView().Version,Is.EqualTo(2));
        }

        [Test] public async Task Rule_rejection_is_atomic_memoized_and_needs_a_new_id_for_correction()
        {
            var host=Host();await Begin(host);var s=host.UssrSession;
            var wrong=Choice(s,values:new[]{A("france",3),A("uk",1)});var rejected=await s.SubmitEventAsync(wrong);
            Assert.That(rejected.Error,Is.EqualTo(RuleError.InvalidSelection));Assert.That(rejected.EffectError,Is.EqualTo(InfluenceEventError.InvalidSelection));
            Assert.That(rejected.Changes,Is.Empty);Assert.That(s.ReadView().Version,Is.EqualTo(1));
            Assert.That(s.ReadView().Event.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));
            Assert.That(await s.SubmitEventAsync(wrong),Is.SameAs(rejected));
            Assert.That((await s.SubmitEventAsync(Choice(s))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            Assert.That((await s.SubmitEventAsync(Choice(s,"corrected"))).Accepted,Is.True);
        }

        [TestCase("game",RuleError.WrongGame)] [TestCase("version",RuleError.StaleVersion)]
        [TestCase("execution",RuleError.StaleContext)] [TestCase("decision",RuleError.StaleDecision)]
        public async Task Incorrect_context_does_not_consume_the_decision(string kind,RuleError expected)
        {
            var host=Host();await Begin(host);var s=host.UssrSession;var v=s.ReadView();
            var command=EventCommandEnvelope.Allocate("bad-context",kind=="game"?"other":v.GameId,kind=="version"?0:v.Version,
                kind=="execution"?"other":v.Event.ExecutionId,kind=="decision"?"other":v.Event.DecisionId,Valid());
            Assert.That((await s.SubmitEventAsync(command)).Error,Is.EqualTo(expected));
            Assert.That(s.ReadView().Version,Is.EqualTo(1));Assert.That((await s.SubmitEventAsync(Choice(s))).Accepted,Is.True);
        }

        [TestCase("empty")] [TestCase("null")] [TestCase("oversized")] [TestCase("missingDecision")] [TestCase("wrongConstructor")]
        public async Task Malformed_allocation_is_not_memoized(string kind)
        {
            var host=Host();await Begin(host);var s=host.UssrSession;var v=s.ReadView();
            var values=kind=="empty"?Array.Empty<InfluenceAllocation>():kind=="null"?null:
                kind=="oversized"?Enumerable.Repeat(A("france",1),129).ToArray():Valid();
            var malformed=kind=="wrongConstructor"?new EventCommandEnvelope("allocation",v.GameId,v.Version,EventCommandKind.ResolveAllocation,
                v.Event.ExecutionId,v.Event.DecisionId,new[]{Id("france")}) : EventCommandEnvelope.Allocate("allocation",v.GameId,v.Version,
                v.Event.ExecutionId,kind=="missingDecision"?null:v.Event.DecisionId,values);
            Assert.That((await s.SubmitEventAsync(malformed)).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That((await s.SubmitEventAsync(Choice(s))).Accepted,Is.True);
        }

        [Test] public async Task Canceled_submission_does_not_reserve_an_id_or_change_state()
        {
            var host=Host();await Begin(host);var s=host.UssrSession;var command=Choice(s);
            var task=s.SubmitEventAsync(command,new CancellationToken(true));
            Assert.That(task.IsCanceled,Is.True);Assert.That(s.ReadView().Version,Is.EqualTo(1));
            Assert.That((await s.SubmitEventAsync(command)).Accepted,Is.True);
        }

        [Test] public async Task Allocation_and_ordinary_commands_share_the_identity_scoped_receipt_namespace()
        {
            var host=Host();await Begin(host);var s=host.UssrSession;
            Assert.That((await s.SubmitAsync(Placement(s,"allocation"))).Error,Is.EqualTo(RuleError.EventInProgress));
            Assert.That((await s.SubmitEventAsync(Choice(s))).Error,Is.EqualTo(RuleError.CommandIdConflict));
            var usRejected=await host.UsSession.SubmitEventAsync(Choice(host.UsSession,"same-side-key"));
            Assert.That(usRejected.Error,Is.EqualTo(RuleError.WrongActor));
            Assert.That((await s.SubmitEventAsync(Choice(s,"same-side-key"))).Accepted,Is.True);
            Assert.That((await s.SubmitAsync(Placement(s,"same-side-key"))).Error,Is.EqualTo(RuleError.CommandIdConflict));
        }

        [Test] public async Task Selection_kind_cannot_be_reinterpreted_by_a_different_event()
        {
            var budget=Host();await Begin(budget);var s=budget.UssrSession;var v=s.ReadView();
            var countrySet=new EventCommandEnvelope("set",v.GameId,v.Version,EventCommandKind.Resolve,v.Event.ExecutionId,v.Event.DecisionId,new[]{Id("france")});
            Assert.That((await s.SubmitEventAsync(countrySet)).Error,Is.EqualTo(RuleError.InvalidSelection));
            var original=new LocalGameHost(InfluenceEventFlowTests.State(),true);await Begin(original);
            Assert.That((await original.UssrSession.SubmitEventAsync(Choice(original.UssrSession))).Error,Is.EqualTo(RuleError.InvalidSelection));
            Assert.That(original.UssrSession.ReadView().Version,Is.EqualTo(1));
        }

        [Test] public async Task Zero_capacity_resolves_on_begin_and_new_allocation_cannot_replay_it()
        {
            var host=Host(zero:true);var begun=await Begin(host);
            Assert.That(begun.Accepted,Is.True);Assert.That(begun.View.Event.Outcome,Is.EqualTo(InfluenceEventStatus.ResolvedNoChange));
            Assert.That(begun.ResolutionSequence,Is.EqualTo(1));Assert.That(begun.View.Version,Is.EqualTo(1));Assert.That(begun.Changes,Is.Empty);
            var s=host.UssrSession;var v=s.ReadView();
            var command=EventCommandEnvelope.Allocate("extra",v.GameId,v.Version,v.Event.ExecutionId,"made-up",Valid());
            Assert.That((await s.SubmitEventAsync(command)).Error,Is.EqualTo(RuleError.EventAlreadyResolved));
            Assert.That(s.ReadView().Version,Is.EqualTo(1));
        }

        [Test] public async Task Waiting_blocks_OPS_and_late_new_allocation_cannot_double_apply()
        {
            var host=Host();await Begin(host);var us=host.UsSession;var v=us.ReadView();
            Assert.That(us.QueryInfluenceTargets(v.ContextId,v.Version).Error,Is.EqualTo(RuleError.EventInProgress));
            Assert.That((await us.SubmitAsync(Placement(us,"ops"))).Error,Is.EqualTo(RuleError.EventInProgress));
            var s=host.UssrSession;var chosen=Choice(s);Assert.That((await s.SubmitEventAsync(chosen)).Accepted,Is.True);
            var late=EventCommandEnvelope.Allocate("new-id",chosen.GameId,chosen.ExpectedVersion,chosen.ExecutionId,chosen.DecisionId,chosen.Allocations);
            Assert.That((await s.SubmitEventAsync(late)).Error,Is.EqualTo(RuleError.StaleVersion));Assert.That(s.ReadView().Version,Is.EqualTo(2));
        }
    }
}
