using System;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class SpaceEventFoundationTests
    {
        internal static SpaceRaceDefinition Track()=>SpaceRaceDefinitionLoader.LoadSubset(PlayInfluenceEventTests.Read("Data/Rulesets/deluxe-2015/space-race.json"));
        internal static SpaceAttemptState State(int us=0,int ussr=0,SpaceRaceDefinition track=null)=>SpaceAttemptState.Create(new SpaceTrackState(track??Track(),us,ussr),TurnCalendarTests.Calendar(),1);
        internal static SpaceEventDefinition Definition(SpaceRaceDefinition track)=>new SpaceEventDefinition(CardCatalogLoaderTests.Load(),track,new CardId("card.captured_nazi_scientist"),"effect.captured_nazi_scientist",1);
        internal static SpaceEventRequest Request(SpaceAttemptState state,string id="event.1",PlayerSide side=PlayerSide.US,SpaceEventDefinition definition=null,int turn=1,string source="action.1")=>new SpaceEventRequest(id,source,turn,side,definition??Definition(state.Track.Definition));
        // Real ordinary attempts exercise the common track/count path with a deterministic winning/failing die.
        sealed class Die : IRandomProvider
        {
            readonly int value; ulong count;
            internal Die(int value){this.value=value;}
            public RandomSnapshot Capture()=>new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,count+1,1,count);
            public IRandomProvider Fork()=>new Die(value){count=count};
            public int NextInt(int min,int max){count++;return value-1;}
        }
        static SpaceAttemptPlan Attempt(SpaceAttemptState state,string id,PlayerSide side,int roll=1)=>SpaceAttemptRules.PlanAttempt(state,SpaceAttemptTests.Request(id,side,turn:state.Turn.Number),new Die(roll));
        [TestCase(1,2,1)][TestCase(2,0,0)][TestCase(3,2,0)][TestCase(4,0,0)]
        [TestCase(5,3,1)][TestCase(6,0,0)][TestCase(7,4,2)][TestCase(8,2,0)]
        public void Each_target_awards_first_or_second_to_actual_beneficiary(int target,int firstVp,int secondVp)
        {
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})foreach(var first in new[]{true,false})
            {
                var other=first?target-1:target;var state=State(side==PlayerSide.US?target-1:other,side==PlayerSide.USSR?target-1:other);
                var request=Request(state,side:side);var plan=SpaceEventRules.PlanAdvance(state,request);var reward=first?firstVp:secondVp;
                Assert.That(plan.Before,Is.SameAs(state));Assert.That(plan.After.Track.PositionFor(side),Is.EqualTo(target));
                Assert.That(plan.After.Track.PositionFor(side==PlayerSide.US?PlayerSide.USSR:PlayerSide.US),Is.EqualTo(other));
                Assert.That(plan.Receipt.FromPosition,Is.EqualTo(target-1));Assert.That(plan.Receipt.ToPosition,Is.EqualTo(target));
                Assert.That(plan.Receipt.FirstArrival,Is.EqualTo(first));Assert.That(plan.Receipt.AwardVp,Is.EqualTo(reward));
                Assert.That(plan.VpDeltaToApply,Is.EqualTo(side==PlayerSide.US?reward:-reward));
                Assert.That(plan.After.UsAttempts,Is.Zero);Assert.That(plan.After.UssrAttempts,Is.Zero);
                Assert.That(plan.After.EventReceipts[request.Id],Is.SameAs(plan.Receipt));Assert.That(state.EventReceipts,Is.Empty);
                Assert.That(plan.AlreadyApplied,Is.False);Assert.That(state.Track.PositionFor(side),Is.EqualTo(target-1));
            }
        }
        [Test] public void Ordinary_and_event_arrival_orders_share_awards_and_preserve_both_histories()
        {
            var state=State();var eventFirst=SpaceEventRules.PlanAdvance(state,Request(state));
            var ordinarySecond=Attempt(eventFirst.After,"attempt.1",PlayerSide.USSR);
            Assert.That(eventFirst.VpDeltaToApply,Is.EqualTo(2));Assert.That(ordinarySecond.VpDeltaToApply,Is.EqualTo(-1));
            Assert.That(ordinarySecond.After.EventReceipts.Count,Is.EqualTo(1));Assert.That(ordinarySecond.After.Receipts.Count,Is.EqualTo(1));
            var ordinaryFirst=Attempt(state,"attempt.2",PlayerSide.USSR);
            var eventSecond=SpaceEventRules.PlanAdvance(ordinaryFirst.After,Request(ordinaryFirst.After));
            Assert.That(ordinaryFirst.VpDeltaToApply,Is.EqualTo(-2));Assert.That(eventSecond.VpDeltaToApply,Is.EqualTo(1));
            Assert.That(eventSecond.After.UssrAttempts,Is.EqualTo(1));Assert.That(eventSecond.After.Receipts.Count,Is.EqualTo(1));
        }
        [Test] public void Event_ignores_spent_attempt_and_one_ops_then_grants_second_attempt_without_reset()
        {
            var spent=Attempt(State(1),"spent",PlayerSide.US,6).After;
            var req=Request(spent);Assert.That(req.Definition.Card.PrintedOps,Is.EqualTo(1));
            Assert.That(SpaceAttemptRules.ValidateAttempt(spent,SpaceAttemptTests.Request("blocked")),Is.EqualTo(SpaceAttemptEligibility.AttemptLimitReached));
            var plan=SpaceEventRules.PlanAdvance(spent,req);
            Assert.That(plan.After.Track.US,Is.EqualTo(2));Assert.That(plan.After.UsAttempts,Is.EqualTo(1));
            Assert.That(SpaceAbilityRules.Query(plan.After.Track,PlayerSide.US).AttemptLimit,Is.EqualTo(2));
            var twice=Attempt(plan.After,"second",PlayerSide.US).After;
            Assert.That(twice.UsAttempts,Is.EqualTo(2));Assert.That(twice.Track.US,Is.EqualTo(3));
            Assert.That(twice.EventReceipts.Count,Is.EqualTo(1));
        }
        [Test] public void Opponent_event_catches_milestone_and_removes_future_extra_attempt_without_erasing_usage()
        {
            var spent=Attempt(State(2,1),"spent",PlayerSide.US,6).After;
            var caught=SpaceEventRules.PlanAdvance(spent,Request(spent,side:PlayerSide.USSR)).After;
            Assert.That(caught.UsAttempts,Is.EqualTo(1));Assert.That(caught.UssrAttempts,Is.Zero);
            Assert.That(SpaceAttemptRules.ValidateAttempt(caught,SpaceAttemptTests.Request("blocked")),Is.EqualTo(SpaceAttemptEligibility.AttemptLimitReached));
        }
        [TestCase(4)][TestCase(6)][TestCase(8)] public void Other_milestone_abilities_are_derived_after_event(int target)
        {
            var state=State(target-1);var after=SpaceEventRules.PlanAdvance(state,Request(state)).After;
            var a=SpaceAbilityRules.Query(after.Track,PlayerSide.US);
            Assert.That(a.OpponentRevealsHeadlineFirst,Is.True);
            Assert.That(a.EndTurnDiscardMaximum,Is.EqualTo(target>=6?1:0));
            Assert.That(a.ActionRoundAllowance,Is.EqualTo(target==8?(int?)8:null));
        }
        [Test] public void Retry_after_later_ordinary_attempt_keeps_current_state_and_no_repeat_award()
        {
            var state=State();var req=Request(state);var first=SpaceEventRules.PlanAdvance(state,req);
            var current=Attempt(first.After,"ordinary",PlayerSide.US).After;
            var retry=SpaceEventRules.PlanAdvance(current,new SpaceEventRequest(req.Id,req.SourceActionId,1,req.Beneficiary,req.Definition));
            Assert.That(retry.After,Is.SameAs(current));Assert.That(retry.Receipt,Is.SameAs(first.Receipt));
            Assert.That(retry.AlreadyApplied,Is.True);Assert.That(retry.VpDeltaToApply,Is.Zero);Assert.That(current.Track.US,Is.EqualTo(2));
            Assert.Throws<NotSupportedException>(()=>((IDictionary<string,SpaceEventReceipt>)current.EventReceipts).Clear());
        }
        [TestCase("source")][TestCase("beneficiary")][TestCase("definition")][TestCase("turn")]
        public void Conflicting_id_does_not_advance(string defect)
        {
            var state=State();var req=Request(state);var done=SpaceEventRules.PlanAdvance(state,req).After;
            var changed=Request(state,req.Id,defect=="beneficiary"?PlayerSide.USSR:PlayerSide.US,defect=="definition"?Definition(state.Track.Definition):req.Definition,defect=="turn"?2:1,defect=="source"?"different":req.SourceActionId);
            Assert.That(()=>SpaceEventRules.PlanAdvance(done,changed),Throws.InstanceOf<ArgumentException>());
            Assert.That(done.Track.US,Is.EqualTo(1));Assert.That(done.EventReceipts.Count,Is.EqualTo(1));
        }
        [Test] public void Cross_kind_ids_are_rejected_in_both_directions()
        {
            var state=State();var eventDone=SpaceEventRules.PlanAdvance(state,Request(state,"same")).After;
            Assert.That(()=>Attempt(eventDone,"same",PlayerSide.US),Throws.InstanceOf<ArgumentException>());
            var attemptDone=Attempt(state,"same",PlayerSide.US,6).After;
            Assert.That(()=>SpaceEventRules.PlanAdvance(attemptDone,Request(attemptDone,"same")),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void New_turn_retains_event_receipts_and_rejects_old_turn_replay()
        {
            var state=State();var req=Request(state);var done=SpaceEventRules.PlanAdvance(state,req).After;
            var next=SpaceAttemptRules.BeginNextTurn(Attempt(done,"ordinary",PlayerSide.US).After,2);
            Assert.That(next.EventReceipts.Count,Is.EqualTo(1));Assert.That(next.Receipts.Count,Is.EqualTo(1));Assert.That(next.UsAttempts,Is.Zero);
            Assert.That(()=>SpaceEventRules.PlanAdvance(next,req),Throws.InstanceOf<ArgumentException>());
            var again=SpaceEventRules.PlanAdvance(next,Request(next,"new-turn",definition:req.Definition,turn:2));
            Assert.That(again.After.Track.US,Is.EqualTo(3));Assert.That(again.VpDeltaToApply,Is.EqualTo(2));
        }
        [Test] public void Terminal_new_event_requires_ruling_but_successful_terminal_receipt_can_retry()
        {
            var state=State(7);var req=Request(state);var done=SpaceEventRules.PlanAdvance(state,req);
            Assert.That(done.After.Track.US,Is.EqualTo(8));Assert.That(done.VpDeltaToApply,Is.EqualTo(2));
            var next=Request(done.After,"new",definition:req.Definition);
            Assert.That(SpaceEventRules.QueryEligibility(done.After,next),Is.EqualTo(SpaceEventEligibility.TerminalRequiresRuling));
            Assert.Throws<NotSupportedException>(()=>SpaceEventRules.PlanAdvance(done.After,next));
            Assert.That(done.After.EventReceipts.Count,Is.EqualTo(1));Assert.That(SpaceEventRules.PlanAdvance(done.After,req).VpDeltaToApply,Is.Zero);
        }
        [Test] public void Different_track_binding_is_not_accepted_as_equivalent()
        {
            var state=State();var req=Request(state,definition:Definition(Track()));
            Assert.That(()=>SpaceEventRules.PlanAdvance(state,req),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("id")][TestCase("source")][TestCase("turn")][TestCase("side")][TestCase("definition")]
        public void Invalid_request_facts_are_rejected(string defect)
        {
            Assert.That(()=>new SpaceEventRequest(defect=="id"?" ":"id",defect=="source"?new string('x',257):"source",defect=="turn"?0:1,defect=="side"?(PlayerSide)99:PlayerSide.US,defect=="definition"?null:Definition(Track())),Throws.InstanceOf<ArgumentException>());
        }
    }
}
