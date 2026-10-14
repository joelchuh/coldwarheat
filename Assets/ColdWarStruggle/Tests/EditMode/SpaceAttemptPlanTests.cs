using System;
using System.Collections;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class SpaceAttemptPlanTests
    {
        sealed class Calls { public int Forks; public int Draws; }
        sealed class FixedRandom : IRandomProvider
        {
            readonly int roll; readonly Calls calls; readonly string defect;
            ulong state=1,count;
            internal FixedRandom(int roll,Calls calls=null,string defect=null) { this.roll=roll;this.calls=calls ?? new Calls();this.defect=defect; }
            public RandomSnapshot Capture()=>new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,state,1,count);
            public IRandomProvider Fork()
            {
                calls.Forks++;
                if(defect=="same")return this;
                if(defect=="null")return null;
                var copy=new FixedRandom(roll,calls,defect){state=state,count=count};
                if(defect=="mismatch")copy.state++;
                return copy;
            }
            public int NextInt(int min,int max)
            {
                calls.Draws++;
                Assert.That(min,Is.Zero);Assert.That(max,Is.EqualTo(6));
                state++;if(defect!="noAdvance")count++;
                if(defect=="throw")throw new InvalidOperationException("Injected sampling failure");
                return roll-1;
            }
        }
        static void Same(RandomSnapshot a,RandomSnapshot b)
        {
            Assert.That(b.AlgorithmId,Is.EqualTo(a.AlgorithmId));Assert.That(b.State,Is.EqualTo(a.State));
            Assert.That(b.Increment,Is.EqualTo(a.Increment));Assert.That(b.DrawCount,Is.EqualTo(a.DrawCount));
        }
        static IEnumerable StepsAndDice()
        { for(var target=1;target<=8;target++)for(var roll=1;roll<=6;roll++)yield return new TestCaseData(target,roll); }
        [TestCaseSource(nameof(StepsAndDice))]
        public void All_steps_dice_arrival_orders_and_sides_use_shared_parameters(int target,int roll)
        {
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})foreach(var first in new[]{true,false})
            {
                var other=first?target-1:target;
                var state=SpaceAttemptTests.State(side==PlayerSide.US?target-1:other,side==PlayerSide.USSR?target-1:other);
                var request=SpaceAttemptTests.Request(side:side);var calls=new Calls();var random=new FixedRandom(roll,calls);var original=random.Capture();
                var result=SpaceAttemptRules.PlanAttempt(state,request,random);var step=state.Track.Definition.GetSpace(target);
                var success=roll>=step.SuccessMinimum && roll<=step.SuccessMaximum;var award=success?(first?step.FirstVp:step.SecondVp):0;
                Assert.That(result.Before,Is.SameAs(state));Assert.That(result.AlreadyApplied,Is.False);
                Assert.That(result.Receipt.Request,Is.SameAs(request));Assert.That(result.Receipt.Roll,Is.EqualTo(roll));
                Assert.That(result.Receipt.Success,Is.EqualTo(success));Assert.That(result.Receipt.FromPosition,Is.EqualTo(target-1));
                Assert.That(result.Receipt.ToPosition,Is.EqualTo(success?target:target-1));Assert.That(result.Receipt.FirstArrival,Is.EqualTo(success?(bool?)first:null));
                Assert.That(result.Receipt.AwardVp,Is.EqualTo(award));Assert.That(result.VpDeltaToApply,Is.EqualTo(side==PlayerSide.US?award:-award));
                Assert.That(result.After.Track.PositionFor(side),Is.EqualTo(success?target:target-1));
                Assert.That(result.After.Track.PositionFor(side==PlayerSide.US?PlayerSide.USSR:PlayerSide.US),Is.EqualTo(other));
                Assert.That(result.After.AttemptsFor(side),Is.EqualTo(1));Assert.That(state.AttemptsFor(side),Is.Zero);
                Assert.That(result.After.AttemptsFor(side==PlayerSide.US?PlayerSide.USSR:PlayerSide.US),Is.Zero);
                Assert.That(result.After.Receipts[request.Id],Is.SameAs(result.Receipt));Assert.That(state.Receipts,Is.Empty);
                Assert.That(result.ApplyCardDisposition,Is.True);Assert.That(result.Receipt.Disposition,Is.EqualTo(SpaceCardDisposition.Discard));
                Assert.That(result.Receipt.ExecuteCardEvent,Is.False);Assert.That(calls.Forks,Is.EqualTo(1));Assert.That(calls.Draws,Is.EqualTo(1));
                Same(original,random.Capture());Same(original,result.RandomBefore);Same(result.RandomBefore,result.Receipt.RandomBefore);
                Same(result.RandomAfter,result.Receipt.RandomAfter);Assert.That(result.RandomAfter.DrawCount,Is.EqualTo(original.DrawCount+1));
            }
        }
        [Test]
        public void More_ops_do_not_change_dice_or_failure_and_starred_event_is_discarded()
        {
            var state=SpaceAttemptTests.State();var card=CardCatalogTests.Event();
            foreach(var ops in new[]{2,4})
            {
                var plan=SpaceAttemptRules.PlanAttempt(state,SpaceAttemptTests.Request(ops:ops,card:card),new FixedRandom(4));
                Assert.That(plan.Receipt.Success,Is.False);Assert.That(plan.After.Track.US,Is.Zero);Assert.That(plan.After.UsAttempts,Is.EqualTo(1));
                Assert.That(plan.VpDeltaToApply,Is.Zero);Assert.That(card.RemoveAfterResolvedEvent,Is.True);
                Assert.That(plan.Receipt.Disposition,Is.EqualTo(SpaceCardDisposition.Discard));Assert.That(plan.Receipt.ExecuteCardEvent,Is.False);
            }
        }
        [Test]
        public void China_returns_transfer_intent_without_event_or_ordinary_discard()
        {
            var card=new CardDefinition(new CardId("card.china"),6,"name","rules",CardKind.China,4,CardAffiliation.None,null,CardDeckEntry.SpecialChina,false,false,null,null);
            var plan=SpaceAttemptRules.PlanAttempt(SpaceAttemptTests.State(),SpaceAttemptTests.Request(card:card),new FixedRandom(6));
            Assert.That(plan.Receipt.Disposition,Is.EqualTo(SpaceCardDisposition.ChinaTransferRequired));Assert.That(plan.Receipt.ExecuteCardEvent,Is.False);
            Assert.That(plan.After.UsAttempts,Is.EqualTo(1));
        }
        [Test]
        public void Gaining_second_attempt_and_losing_it_are_dynamic_without_erasing_counts()
        {
            var gain=SpaceAttemptRules.PlanAttempt(SpaceAttemptTests.State(1,0),SpaceAttemptTests.Request(),new FixedRandom(1)).After;
            Assert.That(gain.Track.US,Is.EqualTo(2));Assert.That(gain.UsAttempts,Is.EqualTo(1));
            var twice=SpaceAttemptRules.PlanAttempt(gain,SpaceAttemptTests.Request("attempt.2"),new FixedRandom(1)).After;
            Assert.That(twice.UsAttempts,Is.EqualTo(2));Assert.That(twice.Track.US,Is.EqualTo(3));
            Assert.That(SpaceAttemptRules.ValidateAttempt(twice,SpaceAttemptTests.Request("attempt.3")),Is.EqualTo(SpaceAttemptEligibility.AttemptLimitReached));
            var used=SpaceAttemptRules.PlanAttempt(SpaceAttemptTests.State(2,1),SpaceAttemptTests.Request(),new FixedRandom(6)).After;
            var caught=SpaceAttemptRules.PlanAttempt(used,SpaceAttemptTests.Request("opponent",PlayerSide.USSR),new FixedRandom(1)).After;
            Assert.That(caught.Track.USSR,Is.EqualTo(2));Assert.That(caught.UsAttempts,Is.EqualTo(1));
            Assert.That(SpaceAttemptRules.ValidateAttempt(caught,SpaceAttemptTests.Request("next")),Is.EqualTo(SpaceAttemptEligibility.AttemptLimitReached));
        }
        [Test]
        public void Same_id_replays_original_receipt_without_rewinding_current_rng_or_state()
        {
            var state=SpaceAttemptTests.State();var request=SpaceAttemptTests.Request();var random=Pcg32Random.FromSeed(42,54);var before=random.Capture();
            var first=SpaceAttemptRules.PlanAttempt(state,request,random);Same(before,random.Capture());
            var current=new Pcg32Random(first.RandomAfter);current.NextInt(0,100);var currentSnapshot=current.Capture();
            var retry=new SpaceAttemptRequest(request.Id,request.SourceActionId,request.TurnNumber,request.Actor,request.Card,request.EffectiveOps);
            var result=SpaceAttemptRules.PlanAttempt(first.After,retry,current);
            Assert.That(result.AlreadyApplied,Is.True);Assert.That(result.After,Is.SameAs(first.After));Assert.That(result.Before,Is.SameAs(first.After));
            Assert.That(result.Receipt,Is.SameAs(first.Receipt));Assert.That(result.VpDeltaToApply,Is.Zero);Assert.That(result.ApplyCardDisposition,Is.False);
            Same(currentSnapshot,result.RandomBefore);Same(currentSnapshot,result.RandomAfter);Same(currentSnapshot,current.Capture());
        }
        [TestCase("action")] [TestCase("actor")] [TestCase("ops")] [TestCase("card")] [TestCase("turn")]
        public void Same_id_with_conflicting_facts_rejected_before_rng(string defect)
        {
            var req=SpaceAttemptTests.Request();var done=SpaceAttemptRules.PlanAttempt(SpaceAttemptTests.State(),req,new FixedRandom(1)).After;
            var conflict=new SpaceAttemptRequest(req.Id,defect=="action"?"other":req.SourceActionId,defect=="turn"?2:1,defect=="actor"?PlayerSide.USSR:req.Actor,defect=="card"?CardCatalogTests.Event():req.Card,defect=="ops"?3:4);
            var calls=new Calls();Assert.That(()=>SpaceAttemptRules.PlanAttempt(done,conflict,new FixedRandom(1,calls)),Throws.InstanceOf<ArgumentException>());
            Assert.That(calls.Forks,Is.Zero);Assert.That(calls.Draws,Is.Zero);Assert.That(done.Receipts.Count,Is.EqualTo(1));
        }
        [TestCase("scoring")] [TestCase("end")] [TestCase("ops")] [TestCase("count")]
        public void Invalid_attempts_never_fork_or_draw(string defect)
        {
            var state=SpaceAttemptTests.State(defect=="end"?8:0,0);
            if(defect=="count")state=SpaceAttemptRules.PlanAttempt(state,SpaceAttemptTests.Request(),new FixedRandom(6)).After;
            var request=SpaceAttemptTests.Request("new",ops:defect=="ops"?1:4,card:defect=="scoring"?SpaceAttemptTests.Scoring():null);
            var calls=new Calls();var random=new FixedRandom(1,calls);var before=random.Capture();
            Assert.Throws<InvalidOperationException>(()=>SpaceAttemptRules.PlanAttempt(state,request,random));
            Assert.That(calls.Forks,Is.Zero);Assert.That(calls.Draws,Is.Zero);Same(before,random.Capture());
        }
        [Test]
        public void New_turn_resets_counts_once_preserving_receipts_and_rejecting_old_turn_request()
        {
            var request=SpaceAttemptTests.Request();var spent=SpaceAttemptRules.PlanAttempt(SpaceAttemptTests.State(),request,new FixedRandom(6)).After;
            var next=SpaceAttemptRules.BeginNextTurn(spent,2);Assert.That(next.UsAttempts,Is.Zero);Assert.That(next.Receipts.Count,Is.EqualTo(1));
            var spentNext=SpaceAttemptRules.PlanAttempt(next,SpaceAttemptTests.Request("turn2",turn:2),new FixedRandom(6)).After;
            Assert.That(SpaceAttemptRules.BeginNextTurn(spentNext,2),Is.SameAs(spentNext));Assert.That(spentNext.UsAttempts,Is.EqualTo(1));
            Assert.Throws<ArgumentException>(()=>SpaceAttemptRules.PlanAttempt(spentNext,request,new FixedRandom(1)));
            Assert.Throws<NotSupportedException>(()=>((IDictionary<string,SpaceAttemptReceipt>)spentNext.Receipts).Clear());
        }
        [TestCase("same")] [TestCase("null")] [TestCase("mismatch")] [TestCase("noAdvance")] [TestCase("throw")]
        public void Broken_random_provider_does_not_publish_a_plan(string defect)
        {
            var state=SpaceAttemptTests.State();var random=new FixedRandom(1,defect:defect);var before=random.Capture();
            Assert.Throws<InvalidOperationException>(()=>SpaceAttemptRules.PlanAttempt(state,SpaceAttemptTests.Request(),random));
            Same(before,random.Capture());Assert.That(state.Receipts,Is.Empty);Assert.That(state.UsAttempts,Is.Zero);
        }
        [TestCase(0)] [TestCase(7)]
        public void Out_of_range_roll_rejected_without_changing_original(int roll)
        {
            var state=SpaceAttemptTests.State();var random=new FixedRandom(roll);var before=random.Capture();
            Assert.Throws<InvalidOperationException>(()=>SpaceAttemptRules.PlanAttempt(state,SpaceAttemptTests.Request(),random));Same(before,random.Capture());
        }
        [Test]
        public void Real_pcg_restore_reproduces_plan_and_exhaustion_rolls_back()
        {
            var random=Pcg32Random.FromSeed(42,54);var saved=random.Capture();var state=SpaceAttemptTests.State();var req=SpaceAttemptTests.Request();
            var one=SpaceAttemptRules.PlanAttempt(state,req,random);var two=SpaceAttemptRules.PlanAttempt(state,req,new Pcg32Random(saved));
            Assert.That(two.Receipt.Roll,Is.EqualTo(one.Receipt.Roll));Assert.That(two.After.Track.US,Is.EqualTo(one.After.Track.US));Same(one.RandomAfter,two.RandomAfter);Same(saved,random.Capture());
            var exhausted=new Pcg32Random(new RandomSnapshot(saved.AlgorithmId,saved.State,saved.Increment,ulong.MaxValue));var before=exhausted.Capture();
            Assert.Throws<OverflowException>(()=>SpaceAttemptRules.PlanAttempt(state,req,exhausted));Same(before,exhausted.Capture());Assert.That(state.Receipts,Is.Empty);
        }
        [Test]
        public void Null_plan_inputs_rejected()
        {
            Assert.Throws<ArgumentNullException>(()=>SpaceAttemptRules.PlanAttempt(null,SpaceAttemptTests.Request(),new FixedRandom(1)));
            Assert.Throws<ArgumentNullException>(()=>SpaceAttemptRules.PlanAttempt(SpaceAttemptTests.State(),null,new FixedRandom(1)));
            Assert.Throws<ArgumentNullException>(()=>SpaceAttemptRules.PlanAttempt(SpaceAttemptTests.State(),SpaceAttemptTests.Request(),null));
        }
    }
}
