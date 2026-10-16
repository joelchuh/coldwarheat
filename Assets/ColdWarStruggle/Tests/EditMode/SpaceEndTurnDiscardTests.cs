using System;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class SpaceEndTurnDiscardTests
    {
        internal static readonly CardId Held=new CardId("card.blockade");
        internal static readonly CardId OtherHeld=new CardId("card.duck_and_cover");
        internal static GameState Completed(PlayerSide owner=PlayerSide.US,int turn=9,int ownPosition=6,int otherPosition=4,bool held=true)
        {
            var s=FinalScoringPlayTests.Ready(turn:turn);var d=s.Practice.Definition;
            var track=new SpaceTrackState(d.Space,owner==PlayerSide.US?ownPosition:otherPosition,owner==PlayerSide.USSR?ownPosition:otherPosition);
            var p=ActionPracticeState.CreateAtTurn(d,turn,0,track);
            s=new GameState(s.Map,s.Countries,s.Operations,s.GameId,turn:p.Space.Turn,cards:s.Cards,randomState:s.RandomState,practice:p);
            s=FinalScoringPlayTests.Complete(NextTurnPlayTests.Apply(s,CardActionKind.StartPractice));
            if(held){s=PlayTurnOpsEventTests.Give(s,owner,Held.Value);s=PlayTurnOpsEventTests.Give(s,owner,OtherHeld.Value);}
            return s;
        }
        internal static GameState Assessed(PlayerSide owner=PlayerSide.US,int turn=9,bool held=true)
            =>NextTurnPlayTests.Apply(Completed(owner,turn,held:held),CardActionKind.AssessTurnEnd);
        internal static CardActionTransition Resolve(GameState s,PlayerSide owner,bool discard=true,CardId? card=null)
            =>ActionPracticeRules.Plan(s,owner,discard?CardActionKind.DiscardHeldCard:CardActionKind.KeepHeldCards,"choose",discard?card??Held:null);
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)]
        public void Audit_opens_one_private_choice_for_real_owner_not_ordinary_actor(PlayerSide owner)
        {
            var s=Assessed(owner);Assert.That(s.Practice.SpaceDiscard,Is.Not.Null);
            var choice=s.Practice.SpaceDiscard;Assert.That(choice.Owner,Is.EqualTo(owner));Assert.That(choice.Status,Is.EqualTo(SpaceEndTurnDiscardStatus.Pending));
            Assert.That(choice.Candidates,Is.EquivalentTo(new[]{Held,OtherHeld}));Assert.That(choice.DecisionId,Is.Not.Null.And.Not.Empty);
            Assert.That(ActionPracticeRules.CanResolveSpaceDiscard(s,owner),Is.EqualTo(RuleError.None));
            Assert.That(ActionPracticeRules.CanResolveSpaceDiscard(s,owner==PlayerSide.US?PlayerSide.USSR:PlayerSide.US),Is.EqualTo(RuleError.WrongActor));
            Assert.That(ActionPracticeRules.CanBeginNextTurn(s,owner),Is.EqualTo(RuleError.ActionNotReady));
            Assert.Throws<NotSupportedException>(()=>((IList<CardId>)choice.Candidates).Clear());
        }
        [TestCase(PlayerSide.US,"card.blockade")] [TestCase(PlayerSide.USSR,"card.duck_and_cover")]
        public void Discard_is_one_physical_move_without_event_rng_ops_or_action(PlayerSide owner,string id)
        {
            var s=Assessed(owner);var card=new CardId(id);var r=Resolve(s,owner,card:card);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(n.Practice.SpaceDiscard.Status,Is.EqualTo(SpaceEndTurnDiscardStatus.Discarded));Assert.That(n.Practice.SpaceDiscard.DiscardedCard,Is.EqualTo(card));
            Assert.That(n.Cards.GetHand(owner),Is.EquivalentTo(s.Cards.GetHand(owner).Where(c=>!c.Equals(card))));Assert.That(n.Cards.DiscardPile,Is.EqualTo(s.Cards.DiscardPile.Append(card)));
            Assert.That(n.Cards.RemovedCards,Is.EqualTo(s.Cards.RemovedCards));Assert.That(n.Cards.ResolvingCards,Is.Empty);Assert.That(n.Cards.China,Is.SameAs(s.Cards.China));Assert.That(n.Cards.DrawPile,Is.EqualTo(s.Cards.DrawPile));
            Assert.That(n.Countries,Is.EquivalentTo(s.Countries));Assert.That(n.Practice.VpForUS,Is.EqualTo(s.Practice.VpForUS));Assert.That(n.Practice.Risk,Is.SameAs(s.Practice.Risk));Assert.That(n.Practice.Space,Is.SameAs(s.Practice.Space));
            Assert.That(n.Practice.TurnEnd,Is.SameAs(s.Practice.TurnEnd));Assert.That(n.Practice.CompletedActions,Is.EqualTo(14));Assert.That(n.RandomState,Is.SameAs(s.RandomState));Assert.That(n.Operations.Budget,Is.Zero);
            Assert.That(n.Version,Is.EqualTo(s.Version+1));Assert.That(n.EventSequence,Is.EqualTo(s.EventSequence+1));Assert.That(s.Practice.SpaceDiscard.Status,Is.EqualTo(SpaceEndTurnDiscardStatus.Pending));
            Assert.That(Resolve(n,owner).Error,Is.EqualTo(RuleError.ActionSliceComplete));Assert.That(ActionPracticeRules.CanBeginNextTurn(n,owner),Is.EqualTo(RuleError.None));
            var next=NextTurnPlayTests.Next(n).NextState;Assert.That(next,Is.Not.Null);Assert.That(next.Turn.Number,Is.EqualTo(10));Assert.That(next.Practice.SpaceDiscard,Is.Null);
        }
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)]
        public void Decline_keeps_card_zone_and_unblocks_final_scoring_with_closed_choice(PlayerSide owner)
        {
            var s=Assessed(owner,10);Assert.That(FinalScoringPlayTests.Finish(s).Error,Is.EqualTo(RuleError.ActionNotReady));
            var r=Resolve(s,owner,false);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(n.Practice.SpaceDiscard.Status,Is.EqualTo(SpaceEndTurnDiscardStatus.Declined));Assert.That(n.Practice.SpaceDiscard.DiscardedCard,Is.Null);Assert.That(n.Cards,Is.SameAs(s.Cards));
            Assert.That(Resolve(n,owner,false).Error,Is.EqualTo(RuleError.ActionSliceComplete));
            var final=FinalScoringPlayTests.Finish(n);Assert.That(final.Accepted,Is.True,final.Error.ToString());Assert.That(final.NextState.Practice.SpaceDiscard,Is.SameAs(n.Practice.SpaceDiscard));Assert.That(final.NextState.Ended,Is.True);
        }
        [TestCase(9)] [TestCase(10)] public void Empty_owner_hand_needs_no_decision(int turn)
        {
            var s=Assessed(turn:turn,held:false);Assert.That(s.Practice.SpaceDiscard,Is.Not.Null);Assert.That(s.Practice.SpaceDiscard.Status,Is.EqualTo(SpaceEndTurnDiscardStatus.NoCards));
            Assert.That(s.Practice.SpaceDiscard.Candidates,Is.Empty);Assert.That(Resolve(s,PlayerSide.US,false).Error,Is.EqualTo(RuleError.ActionSliceComplete));
            Assert.That(turn==9?ActionPracticeRules.CanBeginNextTurn(s,PlayerSide.US):ActionPracticeRules.CanCompleteFinalScoring(s,PlayerSide.US),Is.EqualTo(RuleError.None));
        }
        [TestCase(6,6)] [TestCase(5,4)] public void No_exclusive_milestone_means_no_decision(int own,int other)
        {
            var s=NextTurnPlayTests.Apply(Completed(ownPosition:own,otherPosition:other),CardActionKind.AssessTurnEnd);
            Assert.That(s.Practice.SpaceDiscard,Is.Null);Assert.That(ActionPracticeRules.CanBeginNextTurn(s,PlayerSide.US),Is.EqualTo(RuleError.None));Assert.That(Resolve(s,PlayerSide.US).Error,Is.EqualTo(RuleError.ActionNotReady));
        }
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)] public void Held_scoring_loss_precedes_optional_discard(PlayerSide owner)
        {
            var s=PlayTurnOpsEventTests.Give(Completed(owner),owner,"card.europe_scoring");s=NextTurnPlayTests.Apply(s,CardActionKind.AssessTurnEnd);
            Assert.That(s.Ended,Is.True);Assert.That(s.Practice.Risk.Result.Winner,Is.EqualTo(owner==PlayerSide.US?PlayerSide.USSR:PlayerSide.US));Assert.That(s.Practice.SpaceDiscard,Is.Null);Assert.That(Resolve(s,owner).Error,Is.EqualTo(RuleError.GameAlreadyEnded));
        }
        [Test] public void Wrong_owner_invalid_identity_or_payload_never_moves_cards()
        {
            var s=Assessed();Assert.That(Resolve(s,PlayerSide.USSR).Error,Is.EqualTo(RuleError.WrongActor));Assert.That(Resolve(s,(PlayerSide)99).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(ActionPracticeRules.CanResolveSpaceDiscard(null,PlayerSide.US),Is.EqualTo(RuleError.InvalidCommand));
            foreach(var id in new[]{"",new string('x',129)})Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.KeepHeldCards,id).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.DiscardHeldCard,"x").Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.KeepHeldCards,"x",Held).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.DiscardHeldCard,"x",Held,new CountryId("country.iran")).Error,Is.EqualTo(RuleError.InvalidCommand));
            foreach(var id in new[]{"card.china","card.unknown","card.europe_scoring","card.fidel"})Assert.That(Resolve(s,PlayerSide.US,card:new CardId(id)).Error,Is.EqualTo(RuleError.InvalidSelection));
        }
        [TestCase(false)] [TestCase(true)] public void New_card_zone_after_audit_invalidates_choice_and_continuation(bool resolved)
        {
            var s=Assessed();if(resolved)s=Resolve(s,PlayerSide.US,false).NextState;Assert.That(s,Is.Not.Null);
            s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.fidel");
            Assert.That(Resolve(s,PlayerSide.US).Error,Is.EqualTo(RuleError.StaleContext));Assert.That(ActionPracticeRules.CanBeginNextTurn(s,PlayerSide.US),Is.EqualTo(RuleError.StaleContext));
        }
        [Test] public void Foreign_turn_parameters_do_not_authorize_a_choice()
        {
            var s=Assessed();var cal=new TurnCalendar(new[]{new EraTurnParameters(GameEra.Early,1,3,8,6),new EraTurnParameters(GameEra.Mid,4,7,9,7),new EraTurnParameters(GameEra.Late,8,10,9,8)});
            s=new GameState(s.Map,s.Countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:cal.ForTurn(9),cards:s.Cards,randomState:s.RandomState,practice:s.Practice);
            Assert.That(Resolve(s,PlayerSide.US).Error,Is.EqualTo(RuleError.StaleContext));
        }
        [TestCase(true,true)] [TestCase(true,false)] [TestCase(false,true)] [TestCase(false,false)]
        public void Publication_overflow_is_atomic_for_both_choices(bool version,bool discard)
        {
            var s=Assessed();s=PlayInfluenceEventTests.Copy(s,version:version?long.MaxValue:s.Version,sequence:version?s.EventSequence:long.MaxValue);
            var r=Resolve(s,PlayerSide.US,discard);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(s.Practice.SpaceDiscard.Status,Is.EqualTo(SpaceEndTurnDiscardStatus.Pending));Assert.That(s.Cards.GetHand(PlayerSide.US),Has.Count.EqualTo(2));
        }
        [Test] public void Trusted_initial_track_is_bound_and_has_no_attempt_or_vp_history()
        {
            var d=FinalScoringPlayTests.Ready().Practice.Definition;var track=new SpaceTrackState(d.Space,6,4);var p=ActionPracticeState.CreateAtTurn(d,9,3,track);
            Assert.That(p.Space.Track,Is.SameAs(track));Assert.That(p.Space.UsAttempts,Is.Zero);Assert.That(p.Space.UssrAttempts,Is.Zero);Assert.That(p.Space.Receipts,Is.Empty);Assert.That(p.VpForUS,Is.EqualTo(3));Assert.That(p.SpaceDiscard,Is.Null);
            var foreign=FinalScoringPlayTests.Ready().Practice.Definition.Space;Assert.Throws<ArgumentException>(()=>ActionPracticeState.CreateAtTurn(d,9,0,new SpaceTrackState(foreign,6,4)));
        }
    }
}
