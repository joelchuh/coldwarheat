using System;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class OpponentOpsSequenceTests
    {
        [TestCase(OpponentEventOrder.EventFirst,PlayerSide.USSR,CardResolutionStep.Event,CardResolutionStep.Operations)]
        [TestCase(OpponentEventOrder.OperationsFirst,PlayerSide.US,CardResolutionStep.Operations,CardResolutionStep.Event)]
        public void Two_steps_preserve_card_actor_and_event_owner_and_only_then_complete(OpponentEventOrder order,PlayerSide actor,CardResolutionStep first,CardResolutionStep second)
        {
            var id=new CardId("card.nasser");var s=OpponentOpsSequence.Create(id,actor,order);Assert.That(s.Card,Is.EqualTo(id));Assert.That(s.Actor,Is.EqualTo(actor));Assert.That(s.EventSide,Is.EqualTo(actor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US));Assert.That(s.CurrentStep,Is.EqualTo(first));Assert.That(s.CompletedSteps,Is.Zero);Assert.That(s.EventOccurred,Is.False);
            var middle=s.Advance(first);Assert.That(middle.CurrentStep,Is.EqualTo(second));Assert.That(middle.CompletedSteps,Is.EqualTo(1));Assert.That(middle.EventOccurred,Is.EqualTo(order==OpponentEventOrder.EventFirst));Assert.That(s.CompletedSteps,Is.Zero);Assert.That(middle.Card,Is.EqualTo(id));Assert.That(middle.Actor,Is.EqualTo(actor));
            var done=middle.Advance(second);Assert.That(done.CurrentStep,Is.EqualTo(CardResolutionStep.Complete));Assert.That(done.EventOccurred,Is.True);Assert.That(done.CompletedSteps,Is.EqualTo(2));Assert.Throws<InvalidOperationException>(()=>done.Advance(second));
        }
        [Test] public void Wrong_step_and_invalid_enums_never_advance()
        {
            var s=OpponentOpsSequence.Create(new CardId("card.fidel"),PlayerSide.US,OpponentEventOrder.EventFirst);Assert.Throws<InvalidOperationException>(()=>s.Advance(CardResolutionStep.Operations));Assert.Throws<InvalidOperationException>(()=>s.Advance(CardResolutionStep.Complete));Assert.Throws<InvalidOperationException>(()=>s.Advance((CardResolutionStep)9));Assert.That(s.CompletedSteps,Is.Zero);Assert.Throws<ArgumentOutOfRangeException>(()=>OpponentOpsSequence.Create(new CardId("card.fidel"),(PlayerSide)9,OpponentEventOrder.EventFirst));Assert.Throws<ArgumentOutOfRangeException>(()=>OpponentOpsSequence.Create(new CardId("card.fidel"),PlayerSide.US,(OpponentEventOrder)9));
        }
    }
}
