using System;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class ActionRoundProgressTests
    {
        static SpaceTrackState Track(int us=0,int su=0,int allowance=8)=>new SpaceTrackState(new SpaceRaceDefinition(0,1,1,6,SpaceAbilityTests.Steps(),SpaceAbilityTests.Abilities(rounds:allowance)),us,su);
        static ActionRoundProgress Begin(SpaceTrackState t,int turn=1)=>ActionRoundProgress.Create(TurnCalendarTests.Calendar().ForTurn(turn),t.Definition);
        static ActionRoundProgress Ordinary(ActionRoundProgress p,SpaceTrackState t)
        {
            for(var n=0;n<p.Turn.Parameters.NormalActionsPerSide*2;n++)p=p.CompleteRoot(t,p.CurrentActor.Value);
            return p;
        }
        [TestCase(1,6)] [TestCase(4,7)] [TestCase(10,7)]
        public void Ordinary_slots_alternate_and_finish_without_special_entitlement(int turn,int normal)
        {
            var t=Track();var p=Begin(t,turn);var original=p;
            for(var i=0;i<2*normal;i++)
            {
                var actor=i%2==0?PlayerSide.USSR:PlayerSide.US;
                Assert.That(p.CurrentActor,Is.EqualTo(actor));Assert.That(p.Round,Is.EqualTo(i/2+1));Assert.That(p.IsOptional,Is.False);
                Assert.That(p.CanBegin(t,actor),Is.True);Assert.That(p.CanBegin(t,actor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US),Is.False);
                p=p.CompleteRoot(t,actor);
            }
            Assert.That(p.IsComplete,Is.True);Assert.That(p.TotalCompleted,Is.EqualTo(normal*2));Assert.That(p.CompletedFor(PlayerSide.US),Is.EqualTo(normal));
            Assert.That(p.RemainingAllowance(t,PlayerSide.USSR),Is.Zero);Assert.That(original.TotalCompleted,Is.Zero);Assert.That(original.CurrentActor,Is.EqualTo(PlayerSide.USSR));
            Assert.That(p.CanBegin(t,PlayerSide.US),Is.False);Assert.Throws<InvalidOperationException>(()=>p.CompleteRoot(t,PlayerSide.US));
        }
        [TestCase(PlayerSide.US,1,6)] [TestCase(PlayerSide.USSR,1,6)]
        [TestCase(PlayerSide.US,4,7)] [TestCase(PlayerSide.USSR,10,7)]
        public void Exclusive_owner_receives_total_eight_not_normal_plus_one(PlayerSide side,int turn,int normal)
        {
            var t=Track(side==PlayerSide.US?8:4,side==PlayerSide.USSR?8:4);var p=Ordinary(Begin(t,turn),t);
            for(var n=normal;n<8;n++)
            {
                Assert.That(p.CurrentActor,Is.EqualTo(side));Assert.That(p.IsOptional,Is.True);Assert.That(p.Round,Is.EqualTo(n+1));
                Assert.That(p.RemainingAllowance(t,side),Is.EqualTo(8-n));p=p.CompleteRoot(t,side);
            }
            Assert.That(p.IsComplete,Is.True);Assert.That(p.CompletedFor(side),Is.EqualTo(8));Assert.That(p.TotalCompleted,Is.EqualTo(8+normal));
        }
        [TestCase(0,0)] [TestCase(7,4)] [TestCase(8,8)]
        public void No_exclusive_eighth_milestone_means_no_extra_slot(int us,int su)
        { var t=Track(us,su);Assert.That(Ordinary(Begin(t),t).IsComplete,Is.True); }
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)]
        public void Declining_ends_all_remaining_optional_slots_without_completing_a_root(PlayerSide side)
        {
            var t=Track(side==PlayerSide.US?8:4,side==PlayerSide.USSR?8:4);var p=Ordinary(Begin(t),t);var n=p.DeclineRemaining(t,side);
            Assert.That(n.IsComplete,Is.True);Assert.That(n.TotalCompleted,Is.EqualTo(12));Assert.That(n.DeclinedFor(side),Is.True);
            Assert.That(n.RemainingAllowance(t,side),Is.Zero);Assert.That(p.DeclinedFor(side),Is.False);Assert.That(p.IsComplete,Is.False);
            Assert.Throws<InvalidOperationException>(()=>n.DeclineRemaining(t,side));
        }
        [Test] public void Decline_after_one_extra_keeps_exact_completed_count()
        {
            var t=Track(8,4);var p=Ordinary(Begin(t),t).CompleteRoot(t,PlayerSide.US).DeclineRemaining(t,PlayerSide.US);
            Assert.That(p.TotalCompleted,Is.EqualTo(13));Assert.That(p.CompletedFor(PlayerSide.US),Is.EqualTo(7));Assert.That(p.IsComplete,Is.True);
        }
        [Test] public void Acquiring_on_last_normal_root_is_seen_at_the_boundary()
        {
            var t=Track(7,4);var p=Begin(t);for(var i=0;i<11;i++)p=p.CompleteRoot(t,p.CurrentActor.Value);
            var advanced=new SpaceTrackState(t.Definition,8,4);p=p.CompleteRoot(advanced,PlayerSide.US);
            Assert.That(p.CurrentActor,Is.EqualTo(PlayerSide.US));Assert.That(p.Round,Is.EqualTo(7));Assert.That(p.RemainingAllowance(advanced,PlayerSide.US),Is.EqualTo(2));
        }
        [Test] public void Loss_during_started_root_does_not_cancel_it_but_prevents_further_slots()
        {
            var t=Track(8,4);var p=Ordinary(Begin(t),t);var tied=new SpaceTrackState(t.Definition,8,8);
            Assert.That(p.CanBegin(t,PlayerSide.US),Is.True);Assert.That(p.CanBegin(tied,PlayerSide.US),Is.False);
            var n=p.CompleteRoot(tied,PlayerSide.US);Assert.That(n.TotalCompleted,Is.EqualTo(13));Assert.That(n.IsComplete,Is.True);
        }
        [Test] public void Ability_values_come_from_definition()
        {
            var t=Track(8,4,9);var p=Ordinary(Begin(t),t);Assert.That(p.RemainingAllowance(t,PlayerSide.US),Is.EqualTo(3));
            for(var i=0;i<3;i++)p=p.CompleteRoot(t,PlayerSide.US);Assert.That(p.CompletedFor(PlayerSide.US),Is.EqualTo(9));Assert.That(p.IsComplete,Is.True);
        }
        [Test] public void Current_normal_slot_cannot_be_declined_or_completed_by_opponent()
        {
            var t=Track(8,4);var p=Begin(t);Assert.Throws<InvalidOperationException>(()=>p.DeclineRemaining(t,PlayerSide.USSR));
            Assert.Throws<InvalidOperationException>(()=>p.CompleteRoot(t,PlayerSide.US));
            var optional=Ordinary(p,t);Assert.Throws<InvalidOperationException>(()=>optional.DeclineRemaining(t,PlayerSide.USSR));
        }
        [Test] public void Invalid_identity_and_foreign_definitions_are_rejected()
        {
            var t=Track();var p=Begin(t);Assert.Throws<ArgumentNullException>(()=>ActionRoundProgress.Create(null,t.Definition));
            Assert.Throws<ArgumentNullException>(()=>ActionRoundProgress.Create(p.Turn,null));
            Assert.Throws<ArgumentOutOfRangeException>(()=>p.CompletedFor((PlayerSide)9));Assert.Throws<ArgumentOutOfRangeException>(()=>p.DeclinedFor((PlayerSide)9));
            Assert.Throws<ArgumentOutOfRangeException>(()=>p.CanBegin(t,(PlayerSide)9));Assert.Throws<ArgumentNullException>(()=>p.CanBegin(null,PlayerSide.US));
            Assert.Throws<ArgumentException>(()=>p.CompleteRoot(Track(),PlayerSide.USSR));Assert.Throws<ArgumentException>(()=>p.RemainingAllowance(Track(),PlayerSide.US));
        }
    }
}
