using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;

namespace ColdWarStruggle.Tests
{
    public class InfluenceEventFlowTests
    {
        public static CountryId Id(string name)=>new CountryId("country."+name);
        public static CountryId[] Four()=>new[]{Id("angola"),Id("nigeria"),Id("thailand"),Id("vietnam")};
        public static GameState State(string card="decolonization",InfluenceEventPolicy policy=InfluenceEventPolicy.NoActiveInterference,
            long version=0,long sequence=0,string gameId="event-game",bool ended=false)
        {
            var map=DefinitionTests.World();
            var definitions=EarlyInfluenceCatalogLoader.Load(InfluenceEventTests.Parameters,InfluenceEventTests.Schema,map);
            var values=map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));
            values[Id("cuba")]=new CountryState(5,1);
            return new GameState(map,values,new OperationsContext("ops",PlayerSide.US,4,new[]{Id("angola")},costs:new InfluenceCosts(1,2)),
                gameId,version,eventSequence:sequence,ended:ended,
                hands:new System.Collections.Generic.Dictionary<PlayerSide,System.Collections.Generic.IReadOnlyList<string>>
                { [PlayerSide.US]=new[]{"private.us"},[PlayerSide.USSR]=new[]{"private.ussr"} },
                influenceEvent:new InfluenceEventExecution("event-1",definitions["card."+card],PlayerSide.US,policy));
        }

        [Test] public void Begin_waits_in_same_state_and_beneficiary_owns_the_versioned_decision()
        {
            var s=State();var t=InfluenceEventFlow.Begin(s,PlayerSide.US);
            Assert.That(t.Accepted,Is.True);Assert.That(t.NextState.Version,Is.EqualTo(1));
            Assert.That(t.NextState.InfluenceEvent.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));
            Assert.That(t.NextState.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.USSR));
            Assert.That(t.NextState.InfluenceEvent.DecisionSourceVersion,Is.EqualTo(1));
            Assert.That(t.NextState.InfluenceEvent.DecisionId,Is.Not.Null.And.Not.Empty);
            Assert.That(t.DecisionRequestedSequence,Is.EqualTo(1));Assert.That(t.ResolutionSequence,Is.Null);Assert.That(t.Changes,Is.Empty);
            Assert.That(s.InfluenceEvent.Stage,Is.EqualTo(InfluenceEventStage.Ready));Assert.That(s.Version,Is.Zero);
            Assert.That(t.NextState.Operations,Is.SameAs(s.Operations));Assert.That(t.NextState.SpentOps,Is.Zero);
            Assert.That(t.NextState.GetHand(PlayerSide.USSR),Is.EqualTo(new[]{"private.ussr"}));
        }
        [Test] public void Complete_choice_changes_four_countries_and_marks_execution_resolved_atomically()
        {
            var waiting=InfluenceEventFlow.Begin(State(),PlayerSide.US).NextState;
            var done=InfluenceEventFlow.Resolve(waiting,PlayerSide.USSR,waiting.InfluenceEvent.DecisionId,Four());
            Assert.That(done.Accepted,Is.True);Assert.That(done.NextState.Version,Is.EqualTo(2));
            Assert.That(done.NextState.InfluenceEvent.Stage,Is.EqualTo(InfluenceEventStage.Resolved));
            Assert.That(done.NextState.InfluenceEvent.Outcome,Is.EqualTo(InfluenceEventStatus.ResolvedChange));
            Assert.That(done.Changes.Count,Is.EqualTo(4));Assert.That(done.ResolutionSequence,Is.EqualTo(6));
            Assert.That(done.NextState.EventSequence,Is.EqualTo(6));Assert.That(done.NextState.SpentOps,Is.Zero);
            Assert.That(Four().All(c=>done.NextState.GetInfluence(c,PlayerSide.USSR)==1),Is.True);
            Assert.That(waiting.GetInfluence(Id("angola"),PlayerSide.USSR),Is.Zero);
            Assert.That(InfluenceEventFlow.Begin(done.NextState,PlayerSide.US).Error,Is.EqualTo(RuleError.EventAlreadyResolved));
            Assert.That(InfluenceEventFlow.Resolve(done.NextState,PlayerSide.USSR,waiting.InfluenceEvent.DecisionId,Four()).Error,Is.EqualTo(RuleError.EventAlreadyResolved));
        }
        [Test] public void Invalid_choice_wrong_owner_and_wrong_decision_leave_waiting_state_untouched()
        {
            var s=State();Assert.That(InfluenceEventFlow.Begin(s,PlayerSide.USSR).Error,Is.EqualTo(RuleError.WrongActor));
            Assert.That(InfluenceEventFlow.Resolve(s,PlayerSide.USSR,"unknown",Four()).Error,Is.EqualTo(RuleError.EventNotStarted));
            var w=InfluenceEventFlow.Begin(s,PlayerSide.US).NextState;var d=w.InfluenceEvent.DecisionId;
            Assert.That(InfluenceEventFlow.Begin(w,PlayerSide.US).Error,Is.EqualTo(RuleError.EventInProgress));
            Assert.That(InfluenceEventFlow.Resolve(w,PlayerSide.US,d,Four()).Error,Is.EqualTo(RuleError.WrongActor));
            Assert.That(InfluenceEventFlow.Resolve(w,PlayerSide.USSR,"other",Four()).Error,Is.EqualTo(RuleError.StaleDecision));
            var bad=InfluenceEventFlow.Resolve(w,PlayerSide.USSR,d,Four().Take(3).ToArray());
            Assert.That(bad.Error,Is.EqualTo(RuleError.InvalidSelection));Assert.That(bad.NextState,Is.Null);Assert.That(bad.Changes,Is.Empty);
            Assert.That(w.Version,Is.EqualTo(1));Assert.That(w.InfluenceEvent.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));
        }
        [Test] public void Fixed_and_zero_change_events_finish_once_without_a_decision()
        {
            var done=InfluenceEventFlow.Begin(State("fidel"),PlayerSide.US);
            Assert.That(done.Accepted,Is.True);Assert.That(done.NextState.GetInfluence(Id("cuba"),PlayerSide.US),Is.Zero);
            Assert.That(done.NextState.GetInfluence(Id("cuba"),PlayerSide.USSR),Is.EqualTo(3));
            Assert.That(done.NextState.InfluenceEvent.Stage,Is.EqualTo(InfluenceEventStage.Resolved));Assert.That(done.DecisionRequestedSequence,Is.Null);
            var s=State("truman_doctrine");var w=InfluenceEventFlow.Begin(s,PlayerSide.US).NextState;
            var zero=InfluenceEventFlow.Resolve(w,PlayerSide.US,w.InfluenceEvent.DecisionId,new[]{Id("canada")});
            Assert.That(zero.Accepted,Is.True);Assert.That(zero.NextState.InfluenceEvent.Outcome,Is.EqualTo(InfluenceEventStatus.ResolvedNoChange));
            Assert.That(zero.Changes,Is.Empty);Assert.That(zero.ResolutionSequence,Is.EqualTo(2));
        }
        [TestCase(InfluenceEventPolicy.Unknown,RuleError.EventRequiresRuling)]
        [TestCase(InfluenceEventPolicy.Forbidden,RuleError.EventForbidden)]
        public void Unresolved_policy_cannot_be_bypassed(InfluenceEventPolicy policy,RuleError expected)
        {
            var s=State(policy:policy);var result=InfluenceEventFlow.Begin(s,PlayerSide.US);
            Assert.That(result.Error,Is.EqualTo(expected));Assert.That(result.NextState,Is.Null);Assert.That(s.Version,Is.Zero);
        }
        [TestCase(true)] [TestCase(false)]
        public void Waiting_transition_overflow_is_atomic(bool version)
        {
            var s=State(version:version?long.MaxValue:0,sequence:version?0:long.MaxValue);
            var r=InfluenceEventFlow.Begin(s,PlayerSide.US);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));
            Assert.That(r.NextState,Is.Null);Assert.That(s.InfluenceEvent.Stage,Is.EqualTo(InfluenceEventStage.Ready));
        }
        [Test] public void Ordinary_OPS_are_blocked_until_event_resolves_and_then_preserve_completed_marker()
        {
            var s=State("fidel");Assert.That(InfluenceRules.PlanPlacement(s,s.Operations,new[]{Id("angola")}).Error,Is.EqualTo(RuleError.EventInProgress));
            var done=InfluenceEventFlow.Begin(s,PlayerSide.US).NextState;
            var placed=InfluenceRules.PlanPlacement(done,done.Operations,new[]{Id("angola")});
            Assert.That(placed.Accepted,Is.True);Assert.That(placed.NextState.InfluenceEvent.Stage,Is.EqualTo(InfluenceEventStage.Resolved));
            Assert.That(InfluenceEventFlow.Begin(placed.NextState,PlayerSide.US).Error,Is.EqualTo(RuleError.EventAlreadyResolved));
        }
        [Test] public void Ended_game_rejects_without_effects()
            => Assert.That(InfluenceEventFlow.Begin(State(ended:true),PlayerSide.US).Error,Is.EqualTo(RuleError.GameAlreadyEnded));

        [Test] public void Resolution_overflow_preserves_the_pending_decision_and_all_countries()
        {
            var w=InfluenceEventFlow.Begin(State(version:long.MaxValue-1),PlayerSide.US).NextState;
            var r=InfluenceEventFlow.Resolve(w,PlayerSide.USSR,w.InfluenceEvent.DecisionId,Four());
            Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(r.Changes,Is.Empty);
            Assert.That(w.InfluenceEvent.Stage,Is.EqualTo(InfluenceEventStage.AwaitingChoice));
            Assert.That(Four().All(c=>w.GetInfluence(c,PlayerSide.USSR)==0),Is.True);
        }
    }
}
