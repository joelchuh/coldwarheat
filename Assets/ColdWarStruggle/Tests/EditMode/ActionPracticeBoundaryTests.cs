using System;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class ActionPracticeBoundaryTests
    {
        static GameState Replace(GameState s,CardZones cards=null,RandomSnapshot random=null,long? version=null,long? sequence=null,bool ended=false)
            =>new GameState(s.Map,s.Countries,s.Operations,s.GameId,version??s.Version,s.SpentOps,sequence??s.EventSequence,ended,
                turn:s.Turn,deployment:s.Deployment,cards:cards??s.Cards,randomState:random??s.RandomState,practice:s.Practice);
        [Test] public void One_turn_slice_stops_after_calendar_number_of_actions_without_silently_settling_turn_end()
        {
            var s=ActionPracticeTests.Prepared();var catalog=s.Practice.Definition.Cards;var early=catalog.GetEraCards(GameEra.Early);var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();
            var limit=s.Turn.Parameters.NormalActionsPerSide;
            hands.Add(PlayerSide.US,early.Where(c=>c.Kind==CardKind.Event && c.Affiliation==CardAffiliation.US).Take(limit).Select(c=>c.Id).ToArray());
            hands.Add(PlayerSide.USSR,early.Where(c=>c.Kind==CardKind.Event && c.Affiliation==CardAffiliation.USSR).Take(limit).Select(c=>c.Id).ToArray());
            Assert.That(hands.Values.All(h=>h.Count==limit),Is.True,"Fixture must have sufficient own events.");
            var used=hands.Values.SelectMany(x=>x).ToHashSet();
            var zones=new CardZones(catalog,early.Select(c=>c.Id).Where(id=>!used.Contains(id)),hands,s.Cards.EraReserve,Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),s.Cards.China,s.Cards.EnteredEras);
            s=Replace(s,zones);s=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.StartPractice,"start").NextState;
            for(int i=0;i<limit*2;i++)
            {
                var actor=i%2==0?PlayerSide.USSR:PlayerSide.US;Assert.That(s.Practice.Actor,Is.EqualTo(actor));Assert.That(s.Practice.ActionRound,Is.EqualTo(i/2+1));
                var begin=ActionPracticeRules.Plan(s,actor,CardActionKind.Influence,"begin"+i,s.Cards.GetHand(actor)[0]);Assert.That(begin.Accepted,Is.True);
                s=ActionPracticeRules.Plan(begin.NextState,actor,CardActionKind.FinishInfluence,"end"+i).NextState;
            }
            Assert.That(s.Practice.Stage,Is.EqualTo(ActionPracticeStage.Complete));Assert.That(s.Practice.CompletedActions,Is.EqualTo(limit*2));Assert.That(s.Cards.DiscardPile.Count,Is.EqualTo(limit*2));
            Assert.That(s.Turn.Number,Is.EqualTo(1));Assert.That(s.Ended,Is.False);
            Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.StartPractice,"restart").Error,Is.EqualTo(RuleError.ActionSliceComplete));
        }
        [Test] public void Both_success_and_failure_discard_the_card_and_commit_exact_random_cursor()
        {
            var initial=ActionPracticeTests.Ready();var id=ActionPracticeTests.Playable(initial,CardActionKind.Space);bool won=false,lost=false;
            for(ulong seed=0;seed<24 && !(won&&lost);seed++)
            {
                var random=Pcg32Random.FromSeed(seed,54).Capture();var s=Replace(initial,random:random);var p=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Space,"try",id);
                Assert.That(p.Accepted,Is.True);Assert.That(p.NextState.Cards.DiscardPile,Does.Contain(id));Assert.That(p.NextState.Practice.CompletedActions,Is.EqualTo(1));
                Pcg32RandomTests.Same(p.SpaceReceipt.RandomAfter,p.NextState.RandomState);Assert.That(s.RandomState,Is.SameAs(random));
                if(p.SpaceReceipt.Success)won=true;else {lost=true;Assert.That(p.NextState.Practice.VpForUS,Is.Zero);Assert.That(p.NextState.Practice.Space.Track.USSR,Is.Zero);}
            }
            Assert.That(won&&lost,Is.True);
        }
        [Test] public void Scoring_card_in_real_hand_is_rejected_for_both_uses()
        {
            var s=ActionPracticeTests.Ready();var catalog=s.Practice.Definition.Cards;
            var scoring=catalog.GetEraCards(GameEra.Early).First(c=>c.Kind==CardKind.Scoring).Id;
            var returned=s.Cards.GetHand(PlayerSide.USSR)[0];
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>> {
                {PlayerSide.US,s.Cards.GetHand(PlayerSide.US).Where(id=>!id.Equals(scoring)).ToArray()},
                {PlayerSide.USSR,s.Cards.GetHand(PlayerSide.USSR).Where(id=>!id.Equals(scoring) && !id.Equals(returned)).Concat(new[]{scoring}).ToArray()}};
            s=Replace(s,new CardZones(catalog,s.Cards.DrawPile.Where(id=>!id.Equals(scoring)).Concat(new[]{returned}),hands,s.Cards.EraReserve,s.Cards.DiscardPile,s.Cards.RemovedCards,s.Cards.ResolvingCards,s.Cards.China,s.Cards.EnteredEras));
            foreach(var kind in new[]{CardActionKind.Space,CardActionKind.Influence})Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,kind,"scoring",scoring).Error,Is.EqualTo(RuleError.UnsupportedCardAction));
        }
        [TestCase("version")] [TestCase("sequence")]
        public void Overflow_rejects_without_spending_card_or_random(string counter)
        {
            var s=Replace(ActionPracticeTests.Ready(),version:counter=="version"?long.MaxValue:(long?)null,sequence:counter=="sequence"?long.MaxValue:(long?)null);
            var p=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Space,"overflow",ActionPracticeTests.Playable(s,CardActionKind.Space));
            Assert.That(p.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(p.NextState,Is.Null);Assert.That(s.Practice.Space.UssrAttempts,Is.Zero);
        }
        [Test] public void Ended_state_is_absorbing()
        { var s=Replace(ActionPracticeTests.Ready(),ended:true);Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.FinishInfluence,"end").Error,Is.EqualTo(RuleError.GameAlreadyEnded)); }
        [TestCase("")] [TestCase(" ")] [TestCase(null)]
        public void Missing_command_id_is_rejected(string id)
        { Assert.That(ActionPracticeRules.Plan(ActionPracticeTests.Prepared(),PlayerSide.USSR,CardActionKind.StartPractice,id).Error,Is.EqualTo(RuleError.InvalidCommand)); }
        [Test] public void Invalid_action_and_missing_card_rejected()
        {
            var s=ActionPracticeTests.Ready();Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,(CardActionKind)99,"invalid").Error,Is.EqualTo(RuleError.InvalidCommand));
            Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Space,"missing").Error,Is.EqualTo(RuleError.InvalidCommand));
        }
    }
}
