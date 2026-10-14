using System;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class SpaceAttemptTests
    {
        internal static SpaceAttemptState State(int us=0, int ussr=0, int turn=1) => SpaceAttemptState.Create(new SpaceTrackState(new SpaceRaceDefinition(0,1,1,6,SpaceAbilityTests.Steps(),SpaceAbilityTests.Abilities()),us,ussr),TurnCalendarTests.Calendar(),turn);
        internal static SpaceAttemptRequest Request(string id="attempt.1",PlayerSide side=PlayerSide.US,int ops=4,int turn=1,CardDefinition card=null,string source="action.1") => new SpaceAttemptRequest(id,source,turn,side,card ?? CardCatalogTests.Event(),ops);
        internal static CardDefinition Scoring() => new CardDefinition(new CardId("card.scoring"),2,"name","rules",CardKind.Scoring,null,CardAffiliation.None,GameEra.Early,CardDeckEntry.EraDeck,false,false,"region.asia",CardCatalogTests.Effect());
        [Test]
        public void Creation_binds_calendar_and_track_without_claiming_a_completed_move()
        {
            var state=State(3,5,4);
            Assert.That(state.Track.US,Is.EqualTo(3));Assert.That(state.Track.USSR,Is.EqualTo(5));Assert.That(state.Turn.Number,Is.EqualTo(4));
            Assert.That(state.Turn.Era,Is.EqualTo(GameEra.Mid));Assert.That(state.UsAttempts,Is.Zero);Assert.That(state.UssrAttempts,Is.Zero);
            Assert.That(state.Receipts,Is.Empty);
            Assert.Throws<NotSupportedException>(()=>((IDictionary<string,SpaceAttemptReceipt>)state.Receipts).Clear());
            Assert.Throws<ArgumentOutOfRangeException>(()=>state.AttemptsFor((PlayerSide)99));
        }
        [TestCase(0,1,SpaceAttemptEligibility.InsufficientOps)] [TestCase(0,2,SpaceAttemptEligibility.Allowed)]
        [TestCase(3,2,SpaceAttemptEligibility.Allowed)] [TestCase(4,2,SpaceAttemptEligibility.InsufficientOps)] [TestCase(4,3,SpaceAttemptEligibility.Allowed)]
        [TestCase(6,3,SpaceAttemptEligibility.Allowed)] [TestCase(7,3,SpaceAttemptEligibility.InsufficientOps)] [TestCase(7,4,SpaceAttemptEligibility.Allowed)] [TestCase(8,4,SpaceAttemptEligibility.TrackCompleted)]
        public void Target_ops_and_end_boundary_apply_symmetrically(int position,int ops,SpaceAttemptEligibility expected)
        {
            var state=State(position,position);
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})
            {
                Assert.That(SpaceAttemptRules.ValidateAttempt(state,Request(side:side,ops:ops)),Is.EqualTo(expected));
                Assert.That(state.AttemptsFor(side),Is.Zero);
            }
        }
        [Test]
        public void Scoring_card_is_rejected_even_if_caller_supplies_positive_ops()
        { Assert.That(SpaceAttemptRules.ValidateAttempt(State(),Request(card:Scoring())),Is.EqualTo(SpaceAttemptEligibility.ScoringCard)); }
        [Test]
        public void Wrong_turn_is_rejected_before_eligibility()
        { Assert.Throws<ArgumentException>(()=>SpaceAttemptRules.ValidateAttempt(State(turn:2),Request(turn:1))); }
        [Test]
        public void Sequential_turn_resets_once_and_preserves_track_and_calendar()
        {
            var state=State(4,2);var next=SpaceAttemptRules.BeginNextTurn(state,2);
            Assert.That(next.Track,Is.SameAs(state.Track));Assert.That(next.Calendar,Is.SameAs(state.Calendar));Assert.That(next.Turn.Number,Is.EqualTo(2));
            Assert.That(next.UsAttempts,Is.Zero);Assert.That(next.UssrAttempts,Is.Zero);Assert.That(next.Receipts,Is.Empty);
            Assert.That(SpaceAttemptRules.BeginNextTurn(next,2),Is.SameAs(next));Assert.That(state.Turn.Number,Is.EqualTo(1));
        }
        [TestCase(1,0)] [TestCase(2,1)] [TestCase(1,3)] [TestCase(10,11)]
        public void Reset_rejects_old_skipped_or_out_of_calendar_turns(int current,int next)
        {Assert.That(()=>SpaceAttemptRules.BeginNextTurn(State(turn:current),next),Throws.InstanceOf<ArgumentException>());}
        [TestCase("id")] [TestCase("source")] [TestCase("turn")] [TestCase("side")] [TestCase("card")] [TestCase("ops")] [TestCase("longId")]
        public void Invalid_request_facts_rejected(string defect)
        {
            Assert.That(()=>new SpaceAttemptRequest(defect=="id"?" ":defect=="longId"?new string('a',257):"a",defect=="source"?"":"source",defect=="turn"?0:1,defect=="side"?(PlayerSide)99:PlayerSide.US,defect=="card"?null:CardCatalogTests.Event(),defect=="ops"?0:2),Throws.InstanceOf<ArgumentException>());
        }
        [Test]
        public void Missing_context_rejected()
        {
            Assert.Throws<ArgumentNullException>(()=>SpaceAttemptState.Create(null,TurnCalendarTests.Calendar(),1));
            Assert.Throws<ArgumentNullException>(()=>SpaceAttemptState.Create(State().Track,null,1));
            Assert.Throws<ArgumentNullException>(()=>SpaceAttemptRules.ValidateAttempt(null,Request()));
            Assert.Throws<ArgumentNullException>(()=>SpaceAttemptRules.ValidateAttempt(State(),null));
            Assert.Throws<ArgumentNullException>(()=>SpaceAttemptRules.BeginNextTurn(null,2));
            Assert.That(()=>SpaceAttemptState.Create(State().Track,TurnCalendarTests.Calendar(),0),Throws.InstanceOf<ArgumentException>());
        }
    }
}
