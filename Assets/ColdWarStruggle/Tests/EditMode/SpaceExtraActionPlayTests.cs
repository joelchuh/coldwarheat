using System;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class SpaceExtraActionPlayTests
    {
        internal static GameState Ready(PlayerSide owner=PlayerSide.US,int turn=9,int vp=0,bool complete=true)
        {
            var s=FinalScoringPlayTests.Ready(turn:turn);var d=s.Practice.Definition;
            var track=new SpaceTrackState(d.Space,owner==PlayerSide.US?8:4,owner==PlayerSide.USSR?8:4);
            var p=ActionPracticeState.CreateAtTurn(d,turn,vp,track);
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();var used=new HashSet<CardId>();
            foreach(var side in new[]{PlayerSide.USSR,PlayerSide.US})
            {
                var hand=d.Cards.Cards.Where(c=>c.Kind==CardKind.Event && c.Era<=p.Space.Turn.Era && !used.Contains(c.Id)
                    && (c.Affiliation==CardAffiliation.Neutral || c.Affiliation==(side==PlayerSide.US?CardAffiliation.US:CardAffiliation.USSR)))
                    .Take(p.Space.Turn.Parameters.NormalActionsPerSide).Select(c=>c.Id).ToArray();
                hands[side]=hand;foreach(var card in hand)used.Add(card);
            }
            var cards=new CardZones(d.Cards,d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && c.Era<=p.Space.Turn.Era && !used.Contains(c.Id)).Select(c=>c.Id),hands,
                d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && c.Era>p.Space.Turn.Era).Select(c=>c.Id),Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),s.Cards.China,
                new[]{GameEra.Early,GameEra.Mid,GameEra.Late}.Where(e=>e<=p.Space.Turn.Era));
            s=new GameState(s.Map,s.Countries,s.Operations,s.GameId,turn:p.Space.Turn,cards:cards,randomState:s.RandomState,practice:p);
            s=NextTurnPlayTests.Apply(s,CardActionKind.StartPractice);return complete?FinalScoringPlayTests.Complete(s):s;
        }
        internal static CardActionTransition Decline(GameState s,PlayerSide side)=>ActionPracticeRules.Plan(s,side,CardActionKind.DeclineExtraActions,"decline");
        internal static GameState Resolve(GameState s,CardActionKind kind,string card=null)
        {var r=ActionPracticeRules.Plan(s,s.Practice.Actor,kind,"extra-"+s.Version,card==null?(CardId?)null:new CardId(card));Assert.That(r.Accepted,Is.True,kind+":"+r.Error);return r.NextState;}
        [TestCase(PlayerSide.US,2,6)] [TestCase(PlayerSide.USSR,2,6)] [TestCase(PlayerSide.US,9,7)] [TestCase(PlayerSide.USSR,9,7)]
        public void Ordinary_completion_offers_correct_owner_and_extra_roots_count_once(PlayerSide owner,int turn,int normal)
        {
            var s=Ready(owner,turn);Assert.That(s.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChooseCard));Assert.That(s.Practice.Actor,Is.EqualTo(owner));
            Assert.That(s.Practice.Rounds.IsOptional,Is.True);Assert.That(s.Practice.CompletedActions,Is.EqualTo(2*normal));
            Assert.That(ActionPracticeRules.CanAssessTurnEnd(s,owner),Is.EqualTo(RuleError.ActionNotReady));
            for(var n=normal;n<8;n++)
            {
                s=PlayTurnOpsEventTests.Give(s,owner,"card.olympic_games");var before=s.Practice.CompletedActions;var old=s.Practice.Rounds;
                s=Resolve(s,CardActionKind.Influence,"card.olympic_games");Assert.That(s.Practice.Rounds,Is.SameAs(old));Assert.That(s.Practice.Actor,Is.EqualTo(owner));
                Assert.That(ActionPracticeRules.CanDeclineExtraActions(s,owner),Is.EqualTo(RuleError.ActionNotReady));
                s=Resolve(s,CardActionKind.FinishInfluence);Assert.That(s.Practice.CompletedActions,Is.EqualTo(before+1));Assert.That(s.Practice.Rounds.CompletedFor(owner),Is.EqualTo(n+1));
            }
            Assert.That(s.Practice.Stage,Is.EqualTo(ActionPracticeStage.Complete));Assert.That(s.Practice.Rounds.IsComplete,Is.True);
            Assert.That(ActionPracticeRules.CanAssessTurnEnd(s,owner),Is.EqualTo(RuleError.None));
            s=Resolve(s,CardActionKind.AssessTurnEnd);Assert.That(s.Practice.SpaceDiscard.Status,Is.EqualTo(SpaceEndTurnDiscardStatus.NoCards));
            s=Resolve(s,CardActionKind.BeginNextTurn);Assert.That(s.Practice.CompletedActions,Is.Zero);Assert.That(s.Practice.Rounds.DeclinedFor(owner),Is.False);Assert.That(s.Practice.Actor,Is.EqualTo(PlayerSide.USSR));
        }
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)]
        public void Decline_changes_only_progress_and_publication_then_permits_audit(PlayerSide owner)
        {
            var s=Ready(owner,2);var r=Decline(s,owner);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(n.Practice.CompletedActions,Is.EqualTo(12));Assert.That(n.Practice.Rounds.DeclinedFor(owner),Is.True);Assert.That(n.Practice.Rounds.IsComplete,Is.True);
            Assert.That(n.Cards,Is.SameAs(s.Cards));Assert.That(n.RandomState,Is.SameAs(s.RandomState));Assert.That(n.Practice.Space,Is.SameAs(s.Practice.Space));Assert.That(n.Practice.Risk,Is.SameAs(s.Practice.Risk));
            Assert.That(n.Countries,Is.EquivalentTo(s.Countries));Assert.That(n.Version,Is.EqualTo(s.Version+1));Assert.That(n.EventSequence,Is.EqualTo(s.EventSequence+1));
            Assert.That(Decline(n,owner).Error,Is.EqualTo(RuleError.ActionSliceComplete));Assert.That(ActionPracticeRules.CanAssessTurnEnd(n,owner),Is.EqualTo(RuleError.None));
        }
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)]
        public void Opponent_event_plus_operations_completes_one_extra_root(PlayerSide owner)
        {
            var card=owner==PlayerSide.US?"card.fidel":"card.containment";var s=PlayTurnOpsEventTests.Give(Ready(owner,2),owner,card);
            s=Resolve(s,CardActionKind.OpponentEventFirst,card);Assert.That(s.Practice.CompletedActions,Is.EqualTo(12));Assert.That(s.Practice.Actor,Is.EqualTo(owner));
            Assert.That(s.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChoosingOperations));Assert.That(ActionPracticeRules.CanDeclineExtraActions(s,owner),Is.EqualTo(RuleError.ActionNotReady));
            s=Resolve(s,CardActionKind.UseOpponentInfluence);s=Resolve(s,CardActionKind.FinishInfluence);
            Assert.That(s.Practice.CompletedActions,Is.EqualTo(13));Assert.That(s.Practice.Actor,Is.EqualTo(owner));Assert.That(s.Practice.OpponentOps,Is.Null);Assert.That(s.Practice.Rounds.IsOptional,Is.True);
        }
        [TestCase(2,true)] [TestCase(9,false)]
        public void China_uses_real_remaining_slots_and_cannot_displace_a_held_scoring_card(int turn,bool allowed)
        {
            var s=ChinaCardPlayTests.SetChina(Ready(turn:turn),PlayerSide.US,CardFace.FaceUp);s=PlayTurnOpsEventTests.Give(s,PlayerSide.US,"card.asia_scoring");
            Assert.That(ActionPracticeRules.CanPlay(s,PlayerSide.US,ChinaCardPlayTests.China,CardActionKind.Influence)==RuleError.None,Is.EqualTo(allowed));
            if(!allowed)return;
            s=Resolve(s,CardActionKind.Influence,ChinaCardPlayTests.China.Value);s=Resolve(s,CardActionKind.FinishInfluence);
            Assert.That(s.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(s.Practice.ActionRound,Is.EqualTo(8));
            Assert.That(s.Cards.China.Owner,Is.EqualTo(PlayerSide.USSR));s=Resolve(s,CardActionKind.Scoring,"card.asia_scoring");Assert.That(s.Practice.Rounds.IsComplete,Is.True);
        }
        [Test] public void Decline_does_not_evade_held_scoring_failure()
        {
            var s=PlayTurnOpsEventTests.Give(Ready(),PlayerSide.US,"card.europe_scoring");s=Decline(s,PlayerSide.US).NextState;
            Assert.That(s,Is.Not.Null);s=Resolve(s,CardActionKind.AssessTurnEnd);Assert.That(s.Ended,Is.True);Assert.That(s.Practice.Risk.Result.Winner,Is.EqualTo(PlayerSide.USSR));Assert.That(s.Practice.SpaceDiscard,Is.Null);
        }
        [Test] public void Extra_event_victory_stops_before_root_completion_or_turn_audit()
        {
            var s=PlayTurnOpsEventTests.Give(Ready(vp:19),PlayerSide.US,"card.duck_and_cover");s=Resolve(s,CardActionKind.Event,"card.duck_and_cover");
            Assert.That(s.Ended,Is.True);Assert.That(s.Practice.CompletedActions,Is.EqualTo(14));Assert.That(s.Practice.TurnEnd,Is.Null);
            Assert.That(Decline(s,PlayerSide.US).Error,Is.EqualTo(RuleError.GameAlreadyEnded));
        }
        [Test] public void Final_turn_decline_audit_and_keep_reach_final_scoring()
        {
            var s=PlayTurnOpsEventTests.Give(Ready(turn:10),PlayerSide.US,"card.blockade");s=Decline(s,PlayerSide.US).NextState;Assert.That(s,Is.Not.Null);
            s=Resolve(s,CardActionKind.AssessTurnEnd);Assert.That(FinalScoringPlayTests.Finish(s).Error,Is.EqualTo(RuleError.ActionNotReady));
            var keep=ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.KeepHeldCards,"keep");Assert.That(keep.Accepted,Is.True,keep.Error.ToString());
            var final=FinalScoringPlayTests.Finish(keep.NextState);Assert.That(final.Accepted,Is.True,final.Error.ToString());Assert.That(final.NextState.Ended,Is.True);Assert.That(final.NextState.Practice.CompletedActions,Is.EqualTo(14));
        }
        [Test] public void Wrong_actor_normal_slot_and_foreign_payload_cannot_decline()
        {
            var s=Ready();Assert.That(Decline(s,PlayerSide.USSR).Error,Is.EqualTo(RuleError.WrongActor));Assert.That(Decline(s,(PlayerSide)8).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(Decline(Ready(complete:false),PlayerSide.USSR).Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(Decline(null,PlayerSide.US).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.DeclineExtraActions,"x",new CardId("card.blockade")).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.DeclineExtraActions,"x",target:ChinaCardPlayTests.C("iran")).Error,Is.EqualTo(RuleError.InvalidCommand));
        }
        [TestCase(true)] [TestCase(false)] public void Decline_publication_overflow_is_atomic(bool version)
        {
            var s=Ready();s=PlayInfluenceEventTests.Copy(s,version:version?long.MaxValue:s.Version,sequence:version?s.EventSequence:long.MaxValue);
            var r=Decline(s,PlayerSide.US);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(s.Practice.Rounds.DeclinedFor(PlayerSide.US),Is.False);
        }
    }
}
