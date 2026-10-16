using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class EmptyHandActionTests
    {
        internal static GameState Empty(GameState s,params PlayerSide[] sides)
        {
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();var moved=new List<CardId>();
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})
            { hands[side]=sides.Contains(side)?Array.Empty<CardId>():s.Cards.GetHand(side);if(sides.Contains(side))moved.AddRange(s.Cards.GetHand(side)); }
            var c=s.Cards;var cards=new CardZones(s.Practice.Definition.Cards,c.DrawPile,hands,c.EraReserve,c.DiscardPile.Concat(moved),c.RemovedCards,c.ResolvingCards,c.China,c.EnteredEras,c.HeadlineReserved);
            return PlayInfluenceEventTests.Copy(s,cards:cards);
        }
        internal static GameState Ready(int turn=2)=>Empty(Ordinary(turn),PlayerSide.USSR,PlayerSide.US);
        static GameState Ordinary(int turn)
        {
            var s=FinalScoringPlayTests.Ready(turn:turn);var d=s.Practice.Definition;var c=s.Cards;
            var used=new HashSet<CardId>();var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();
            foreach(var side in new[]{PlayerSide.USSR,PlayerSide.US})
            {
                var hand=d.Cards.Cards.Where(x=>x.Kind==CardKind.Event && x.Era<=s.Turn.Era && !used.Contains(x.Id)
                    && (x.Affiliation==CardAffiliation.Neutral || x.Affiliation==(side==PlayerSide.US?CardAffiliation.US:CardAffiliation.USSR)))
                    .Take(s.Turn.Parameters.NormalActionsPerSide).Select(x=>x.Id).ToArray();
                hands[side]=hand;foreach(var id in hand)used.Add(id);
            }
            var cards=new CardZones(d.Cards,d.Cards.Cards.Where(x=>x.Kind!=CardKind.China && x.Era<=s.Turn.Era && !used.Contains(x.Id)).Select(x=>x.Id),
                hands,
                d.Cards.Cards.Where(x=>x.Kind!=CardKind.China && x.Era>s.Turn.Era).Select(x=>x.Id),Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),c.China,
                new[]{GameEra.Early,GameEra.Mid,GameEra.Late}.Where(x=>x<=s.Turn.Era));
            return PlayInfluenceEventTests.Copy(s,cards:cards);
        }
        internal static CardActionTransition Skip(GameState s,PlayerSide? side=null)=>ActionPracticeRules.Plan(s,side??s.Practice.Actor,CardActionKind.SkipEmptyHand,"skip");
        [TestCase(PlayerSide.US,CardFace.FaceUp)] [TestCase(PlayerSide.USSR,CardFace.FaceUp)] [TestCase(PlayerSide.USSR,CardFace.FaceDown)]
        public void Empty_hand_can_skip_without_forcing_or_moving_China(PlayerSide owner,CardFace face)
        {
            var s=ChinaCardPlayTests.SetChina(Ready(),owner,face);var before=s.Practice.Rounds;
            Assert.That(ActionPracticeRules.CanSkipEmptyHand(s,PlayerSide.USSR),Is.EqualTo(RuleError.None));
            var r=Skip(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(n.Practice.Rounds.CompletedFor(PlayerSide.USSR),Is.EqualTo(1));Assert.That(n.Practice.CompletedActions,Is.EqualTo(1));Assert.That(before.TotalCompleted,Is.Zero);
            Assert.That(n.Cards,Is.SameAs(s.Cards));Assert.That(n.RandomState,Is.SameAs(s.RandomState));Assert.That(n.Countries,Is.EquivalentTo(s.Countries));Assert.That(n.Practice.Space,Is.SameAs(s.Practice.Space));Assert.That(n.Practice.Risk,Is.SameAs(s.Practice.Risk));Assert.That(n.Practice.OpsEffects,Is.SameAs(s.Practice.OpsEffects));Assert.That(n.Practice.VpForUS,Is.EqualTo(s.Practice.VpForUS));
            Assert.That(n.Version,Is.EqualTo(s.Version+1));Assert.That(n.EventSequence,Is.EqualTo(s.EventSequence+1));Assert.That(n.Operations.Budget,Is.Zero);
        }
        [TestCase(2,6)] [TestCase(10,7)] public void Both_empty_hands_consume_normal_slots_then_reach_turn_end(int turn,int normal)
        {
            var s=Ready(turn);for(var i=0;i<normal*2;i++)
            { Assert.That(s.Practice.Actor,Is.EqualTo(i%2==0?PlayerSide.USSR:PlayerSide.US));Assert.That(s.Practice.ActionRound,Is.EqualTo(i/2+1));var r=Skip(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());s=r.NextState; }
            Assert.That(s.Practice.Stage,Is.EqualTo(ActionPracticeStage.Complete));Assert.That(s.Practice.CompletedActions,Is.EqualTo(normal*2));Assert.That(Skip(s).Error,Is.EqualTo(RuleError.ActionSliceComplete));
            Assert.That(ActionPracticeRules.CanAssessTurnEnd(s,PlayerSide.US),Is.EqualTo(RuleError.None));s=NextTurnPlayTests.Apply(s,CardActionKind.AssessTurnEnd);
            if(turn==10)Assert.That(FinalScoringPlayTests.Finish(s).Accepted,Is.True);
            else { s=NextTurnPlayTests.Apply(s,CardActionKind.BeginNextTurn);Assert.That(s.Practice.CompletedActions,Is.Zero);Assert.That(s.Cards.GetHand(PlayerSide.USSR).Count,Is.EqualTo(8)); }
        }
        [Test] public void One_empty_hand_interleaves_with_real_opponent_cards()
        {
            var s=Empty(Ordinary(2),PlayerSide.USSR);var initial=s.Cards.GetHand(PlayerSide.US).Count;
            for(var i=0;i<6;i++) { var skipped=Skip(s);Assert.That(skipped.Accepted,Is.True);s=skipped.NextState;s=NextTurnPlayTests.Apply(s,CardActionKind.Influence,s.Cards.GetHand(PlayerSide.US).First().Value);s=NextTurnPlayTests.Apply(s,CardActionKind.FinishInfluence); }
            Assert.That(s.Practice.Rounds.IsComplete,Is.True);Assert.That(s.Cards.GetHand(PlayerSide.US).Count,Is.EqualTo(initial-6));
        }
        [Test] public void China_remains_a_voluntary_play_with_empty_ordinary_hand()
        {
            var s=ChinaCardPlayTests.SetChina(Ready(),PlayerSide.USSR,CardFace.FaceUp);Assert.That(ActionPracticeRules.CanPlay(s,PlayerSide.USSR,ChinaCardPlayTests.China,CardActionKind.Influence),Is.EqualTo(RuleError.None));
            s=NextTurnPlayTests.Apply(s,CardActionKind.Influence,ChinaCardPlayTests.China.Value);Assert.That(Skip(s).Error,Is.EqualTo(RuleError.ActionNotReady));s=NextTurnPlayTests.Apply(s,CardActionKind.FinishInfluence);
            Assert.That(s.Practice.CompletedActions,Is.EqualTo(1));Assert.That(s.Cards.China.Owner,Is.EqualTo(PlayerSide.US));
        }
        [TestCase("card.blockade")] [TestCase("card.europe_scoring")] [TestCase("card.five_year_plan")]
        public void Any_ordinary_hand_card_blocks_skip_including_unsupported_events(string card)
        { var s=PlayTurnOpsEventTests.Give(Ready(),PlayerSide.USSR,card);Assert.That(Skip(s).Error,Is.EqualTo(RuleError.ActionNotReady)); }
        [Test] public void Receiving_a_card_after_skipping_restores_the_obligation_to_play()
        {
            var s=Skip(Ready()).NextState;Assert.That(s,Is.Not.Null);s=Skip(s).NextState;Assert.That(s,Is.Not.Null);s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.fidel");
            Assert.That(Skip(s).Error,Is.EqualTo(RuleError.ActionNotReady));s=NextTurnPlayTests.Apply(s,CardActionKind.Event,"card.fidel");Assert.That(s.Practice.Rounds.CompletedFor(PlayerSide.USSR),Is.EqualTo(2));
        }
        [TestCase(PlayerSide.US)] [TestCase(PlayerSide.USSR)] public void Extra_slots_require_explicit_decline_instead_of_empty_hand_skip(PlayerSide owner)
        {
            var s=Empty(SpaceExtraActionPlayTests.Ready(owner,2,complete:false),PlayerSide.US,PlayerSide.USSR);
            for(var i=0;i<12;i++){var r=Skip(s);Assert.That(r.Accepted,Is.True);s=r.NextState;}
            Assert.That(s.Practice.Rounds.IsOptional,Is.True);Assert.That(s.Practice.Actor,Is.EqualTo(owner));Assert.That(Skip(s).Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(SpaceExtraActionPlayTests.Decline(s,owner).Accepted,Is.True);
        }
        [Test] public void Wrong_side_unknown_side_and_foreign_payload_are_rejected()
        {
            var s=Ready();Assert.That(Skip(s,PlayerSide.US).Error,Is.EqualTo(RuleError.WrongActor));Assert.That(Skip(s,(PlayerSide)99).Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.SkipEmptyHand,"skip",ChinaCardPlayTests.China).Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.SkipEmptyHand,"skip",target:ChinaCardPlayTests.C("iran")).Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(ActionPracticeRules.CanSkipEmptyHand(null,PlayerSide.USSR),Is.EqualTo(RuleError.InvalidCommand));
        }
        [TestCase(true)] [TestCase(false)] public void Overflow_does_not_consume_a_slot(bool version)
        { var s=Ready();s=PlayInfluenceEventTests.Copy(s,version:version?long.MaxValue:s.Version,sequence:version?s.EventSequence:long.MaxValue);var r=Skip(s);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(s.Practice.CompletedActions,Is.Zero); }
        [Test] public async Task Session_enforces_fixed_identity_version_dedup_and_public_availability()
        {
            var s=Ready();var h=new LocalGameHost(s,true);var v=h.UssrSession.ReadView();Assert.That(v.Practice.CanSkipEmptyHand,Is.True);Assert.That(h.UsSession.ReadView().Practice.CanSkipEmptyHand,Is.False);
            var cmd=new CardActionCommand("skip",v.GameId,v.Version,CardActionKind.SkipEmptyHand);Assert.That((await h.UsSession.SubmitCardActionAsync(cmd)).Error,Is.EqualTo(RuleError.WrongActor));Assert.That((await h.UssrSession.SubmitCardActionAsync(new CardActionCommand("old",v.GameId,v.Version-1,CardActionKind.SkipEmptyHand))).Error,Is.EqualTo(RuleError.StaleVersion));
            var done=await h.UssrSession.SubmitCardActionAsync(cmd);Assert.That(done.Accepted,Is.True,done.Error.ToString());Assert.That(await h.UssrSession.SubmitCardActionAsync(cmd),Is.SameAs(done));Assert.That(done.View.Practice.CanSkipEmptyHand,Is.False);Assert.That(h.UsSession.ReadView().Practice.CanSkipEmptyHand,Is.True);Assert.That(done.View.Version,Is.EqualTo(v.Version+1));Assert.That(done.View.OwnHand,Is.Empty);
            Assert.That((await h.UssrSession.SubmitCardActionAsync(new CardActionCommand("skip",v.GameId,v.Version,CardActionKind.AssessTurnEnd))).Error,Is.EqualTo(RuleError.CommandIdConflict));
        }
    }
}
