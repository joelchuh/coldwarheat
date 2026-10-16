using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class BlockadePlayTests
    {
        internal static CardId Id(string n)=>new CardId("card."+n);
        internal static CountryId West=>new CountryId("country.west_germany");
        internal static ActionPracticeDefinition Bind(ActionPracticeDefinition d)
        {
            var map=d.Risk.Defcon.Map;var effect=BlockadeDefinitionLoader.Load(BlockadeDefinitionTests.Data(),BlockadeDefinitionTests.Schema(),map,d.Cards,d.Ops,BudgetedRemovalCatalogTests.KnownEffects);
            return new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents??PlayInfluenceEventTests.Effects(map,d.Cards),d.DefconVpEvents,d.TurnOpsEvents??PlayTurnOpsEventTests.Effects(d.Cards),d.Realignment,d.China,d.TurnEndEnabled,d.NextTurnEnabled,d.HeadlineParameters,d.HeadlineDefectors,d.FinalScoring,blockade:effect);
        }
        internal static GameState Ready(PlayerSide actor=PlayerSide.USSR,bool eligible=true,int west=5)
        {
            var s=PlayScoringTests.Ready("card.blockade",configure:Bind);
            var us=new List<CardId>{Id("independent_reds"),Id("asia_scoring")};if(eligible)us.Add(Id("marshall_plan"));if(actor==PlayerSide.US)us.Add(Id("blockade"));
            var su=new List<CardId>{Id("olympic_games")};if(actor==PlayerSide.USSR)su.Add(Id("blockade"));
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,us},{PlayerSide.USSR,su}};var used=us.Concat(su).ToHashSet();
            var cards=new CardZones(s.Practice.Definition.Cards,s.Practice.Definition.Cards.Cards.Where(c=>c.Kind!=CardKind.China && !used.Contains(c.Id)).Select(c=>c.Id),hands,
                Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),s.Cards.China,s.Cards.EnteredEras);
            var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[West]=new CountryState(west,2);
            s=new GameState(s.Map,board,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,cards:cards,randomState:s.RandomState,practice:s.Practice);
            if(actor==PlayerSide.US){var start=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Influence,"prep",Id("olympic_games"));Assert.That(start.Accepted,Is.True);s=ActionPracticeRules.Plan(start.NextState,PlayerSide.USSR,CardActionKind.FinishInfluence,"prep-finish").NextState;}
            return s;
        }
        internal static CardActionTransition Begin(GameState s)=>ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Event,"blockade",Id("blockade"));
        internal static CardActionTransition Respond(GameState s,bool discard=true)=>ActionPracticeRules.Plan(s,PlayerSide.US,discard?CardActionKind.DiscardForBlockade:CardActionKind.DeclineBlockade,"respond",discard?Id("marshall_plan"):(CardId?)null);
        [TestCase(PlayerSide.USSR)][TestCase(PlayerSide.US)] public void Both_root_actors_wait_for_US_and_accepted_starred_discard_finishes_exactly_one_root(PlayerSide actor)
        {
            var s=Ready(actor);var begin=Begin(s);Assert.That(begin.Accepted,Is.True,begin.Error.ToString());var p=begin.NextState;
            Assert.That(p.Practice.Blockade,Is.Not.Null);Assert.That(p.Practice.Blockade.Owner,Is.EqualTo(PlayerSide.US));Assert.That(p.Practice.Actor,Is.EqualTo(actor));Assert.That(p.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));
            Assert.That(p.Practice.Blockade.Candidates.Select(q=>q.Card.Id),Is.EqualTo(new[]{Id("marshall_plan")}));Assert.That(p.Practice.Blockade.ExpectedVersion,Is.EqualTo(p.Version));Assert.That(p.Cards.ResolvingCards,Is.EqualTo(new[]{Id("blockade")}));Assert.That(begin.BlockadeResult.Outcome,Is.EqualTo(BlockadeOutcome.AwaitingChoice));
            var done=Respond(p);Assert.That(done.Accepted,Is.True,done.Error.ToString());var n=done.NextState;
            Assert.That(n.Practice.Blockade,Is.Null);Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions+1));Assert.That(n.Cards.DiscardPile,Has.Member(Id("marshall_plan")));Assert.That(n.Cards.RemovedCards,Has.Member(Id("blockade")));Assert.That(n.Cards.RemovedCards,Has.No.Member(Id("marshall_plan")));Assert.That(n.Cards.ResolvingCards,Is.Empty);
            Assert.That(n.Countries,Is.EquivalentTo(s.Countries));Assert.That(n.Practice.Risk,Is.SameAs(s.Practice.Risk));Assert.That(n.Practice.Space,Is.SameAs(s.Practice.Space));Assert.That(n.Practice.OpsEffects,Is.SameAs(s.Practice.OpsEffects));Pcg32RandomTests.Same(s.RandomState,n.RandomState);
            Assert.That(done.BlockadeResult.DiscardedCard,Is.EqualTo(Id("marshall_plan")));Assert.That(done.BlockadeResult.Changes,Is.Empty);Assert.That(s.Cards.GetHand(actor),Has.Member(Id("blockade")));
        }
        [TestCase(true,5)][TestCase(true,0)][TestCase(false,5)][TestCase(false,0)] public void Decline_or_no_eligible_card_removes_only_US_influence_and_zero_still_resolves(bool eligible,int west)
        {
            var s=Ready(eligible:eligible,west:west);var b=Begin(s);Assert.That(b.Accepted,Is.True,b.Error.ToString());var r=eligible?Respond(b.NextState,false):b;Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(n.GetInfluence(West,PlayerSide.US),Is.Zero);Assert.That(n.GetInfluence(West,PlayerSide.USSR),Is.EqualTo(2));Assert.That(n.Practice.Blockade,Is.Null);Assert.That(n.Practice.CompletedActions,Is.EqualTo(1));Assert.That(n.Cards.RemovedCards,Has.Member(Id("blockade")));Assert.That(n.Cards.GetHand(PlayerSide.US),Is.EqualTo(s.Cards.GetHand(PlayerSide.US)));Assert.That(n.Cards.DiscardPile,Is.Empty);
            Assert.That(r.BlockadeResult.Outcome,Is.EqualTo(BlockadeOutcome.InfluenceRemoved));Assert.That(r.BlockadeResult.DiscardedCard,Is.Null);Assert.That(r.BlockadeResult.Changes.Count,Is.EqualTo(west==0?0:1));Assert.That(n.Countries.Where(x=>!x.Key.Equals(West)),Is.EquivalentTo(s.Countries.Where(x=>!x.Key.Equals(West))));Pcg32RandomTests.Same(s.RandomState,n.RandomState);
        }
        [TestCase(true)][TestCase(false)] public void Opponent_OPS_sequence_preserves_root_until_both_steps_are_done(bool first)
        {
            var s=Ready(PlayerSide.US);var b=PlayOpponentInfluenceTests.Start(s,"card.blockade",first);Assert.That(b.Accepted,Is.True,b.Error.ToString());
            var waiting=first?b.NextState:PlayOpponentInfluenceTests.Finish(b.NextState).NextState;Assert.That(waiting.Practice.Blockade,Is.Not.Null);Assert.That(waiting.Practice.CompletedActions,Is.EqualTo(1));
            var r=Respond(waiting);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            if(first){Assert.That(n.Practice.Blockade,Is.Null);Assert.That(n.Practice.CompletedActions,Is.EqualTo(1));Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.PlacingInfluence));Assert.That(n.Operations.Budget,Is.EqualTo(1));n=PlayOpponentInfluenceTests.Finish(n).NextState;}
            Assert.That(n.Practice.CompletedActions,Is.EqualTo(2));Assert.That(n.Practice.OpponentOps,Is.Null);Assert.That(n.Cards.ResolvingCards,Is.Empty);Assert.That(n.Cards.RemovedCards,Has.Member(Id("blockade")));Pcg32RandomTests.Same(s.RandomState,n.RandomState);
        }
        [TestCase("low")][TestCase("score")][TestCase("china")][TestCase("other")][TestCase("wrong-owner")][TestCase("no-card")][TestCase("decline-card")][TestCase("target")]
        public void Bad_response_is_rejected_and_never_becomes_fallback(string kind)
        {
            var b=Begin(Ready());Assert.That(b.Accepted,Is.True);var s=b.NextState;CardId? card=kind=="no-card"?null:Id(kind=="low"?"independent_reds":kind=="score"?"asia_scoring":kind=="china"?"china":kind=="other"?"olympic_games":"marshall_plan");
            var r=ActionPracticeRules.Plan(s,kind=="wrong-owner"?PlayerSide.USSR:PlayerSide.US,kind=="decline-card"?CardActionKind.DeclineBlockade:CardActionKind.DiscardForBlockade,"bad",card,kind=="target"?West:(CountryId?)null);
            Assert.That(r.Accepted,Is.False);Assert.That(s.GetInfluence(West,PlayerSide.US),Is.EqualTo(5));Assert.That(s.Cards.GetHand(PlayerSide.US),Has.Member(Id("marshall_plan")));Assert.That(s.Cards.DiscardPile,Is.Empty);Assert.That(s.Practice.CompletedActions,Is.Zero);
        }
        [Test] public void Pending_decision_blocks_other_actions_and_rejects_a_changed_version()
        {
            var b=Begin(Ready());Assert.That(b.Accepted,Is.True);var s=b.NextState;
            Assert.That(ActionPracticeRules.CanRespondToBlockade(s,PlayerSide.USSR),Is.EqualTo(RuleError.WrongActor));Assert.That(ActionPracticeRules.CanRespondToBlockade(s,PlayerSide.US),Is.EqualTo(RuleError.None));
            foreach(var kind in new[]{CardActionKind.FinishInfluence,CardActionKind.StartPractice,CardActionKind.SkipEmptyHand,CardActionKind.AssessTurnEnd,CardActionKind.BeginNextTurn})Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,kind,"bypass").Accepted,Is.False,kind.ToString());
            Assert.That(ActionPracticeRules.CanPlay(s,PlayerSide.USSR,Id("olympic_games"),CardActionKind.Influence),Is.Not.EqualTo(RuleError.None));
            var stale=PlayScoringTests.Replace(s,version:s.Version+1);Assert.That(Respond(stale).Error,Is.EqualTo(RuleError.StaleContext));
        }
        [TestCase(false,1)][TestCase(true,1)][TestCase(false,3)][TestCase(true,2)] public void Numeric_overflow_never_publishes_partial_discard_or_removal(bool discard,int room)
        {
            var s=PlayScoringTests.Replace(Ready(),version:long.MaxValue-room);var b=Begin(s);Assert.That(b.Accepted,Is.True,b.Error.ToString());Assert.That(b.NextState.Version,Is.EqualTo(long.MaxValue-room+1));
            var r=Respond(b.NextState,discard);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(b.NextState.Cards.DiscardPile,Is.Empty);Assert.That(b.NextState.GetInfluence(West,PlayerSide.US),Is.EqualTo(5));
        }
        [Test] public void Headline_waits_for_hand_response_then_another_country_choice_without_spending_root_actions()
        {
            var s=HeadlineFlowTests.Prepared("blockade","truman_doctrine");var d=Bind(s.Practice.Definition);
            s=new GameState(s.Map,s.Countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,deployment:s.Deployment,cards:s.Cards,randomState:s.RandomState,practice:ActionPracticeState.Create(d));
            s=PlayTurnOpsEventTests.Give(s,PlayerSide.US,"card.marshall_plan");var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[West]=new CountryState(5,2);s=PlayInfluenceEventTests.Copy(s,board);
            s=HeadlineFlowTests.Start(s);var us=HeadlineFlowRules.Submit(s,PlayerSide.US,Id("blockade"));Assert.That(us.Accepted,Is.True);var su=HeadlineFlowRules.Submit(us.NextState,PlayerSide.USSR,Id("truman_doctrine"));Assert.That(su.Accepted,Is.True,su.Error.ToString());
            Assert.That(su.NextState.Practice.Blockade,Is.Not.Null);Assert.That(su.NextState.Practice.Headline.CompletedCount,Is.Zero);
            var r=Respond(su.NextState);Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.NextState.Practice.Blockade,Is.Null);Assert.That(r.NextState.InfluenceEvent,Is.Not.Null);Assert.That(r.NextState.Practice.Headline.CompletedCount,Is.EqualTo(1));Assert.That(r.NextState.Practice.CompletedActions,Is.Zero);Assert.That(r.HeadlineSteps.Count,Is.EqualTo(2));
            var raw=InfluenceEventFlow.Resolve(r.NextState,PlayerSide.US,r.NextState.InfluenceEvent.DecisionId,new[]{West});var done=HeadlineResolutionRules.CompleteChoice(raw);Assert.That(done.Accepted,Is.True,done.Error.ToString());Assert.That(done.NextState.Practice.Headline.IsComplete,Is.True);Assert.That(done.NextState.Practice.CompletedActions,Is.Zero);Assert.That(done.NextState.Practice.Actor,Is.EqualTo(PlayerSide.USSR));
        }
        [TestCase("containment",true)][TestCase("red_scare_purge",false)] public void USSR_root_uses_actual_current_US_modifiers(string modifier,bool eligible)
        {
            var s=Ready(eligible:false);s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card."+modifier);s=PlayTurnOpsEventTests.Give(s,PlayerSide.US,"card.duck_and_cover");
            if(!eligible)s=PlayTurnOpsEventTests.Give(s,PlayerSide.US,"card.de_stalinization");
            var activation=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Event,"activate",Id(modifier));Assert.That(activation.Accepted,Is.True,activation.Error.ToString());
            var skip=ActionPracticeRules.Plan(activation.NextState,PlayerSide.US,CardActionKind.Influence,"skip",Id("duck_and_cover"));Assert.That(skip.Accepted,Is.True);
            var ready=ActionPracticeRules.Plan(skip.NextState,PlayerSide.US,CardActionKind.FinishInfluence,"finish");Assert.That(ready.Accepted,Is.True);
            var begin=Begin(ready.NextState);Assert.That(begin.Accepted,Is.True,begin.Error.ToString());
            if(eligible){Assert.That(begin.NextState.Practice.Blockade.Candidates.Single().Card.Id,Is.EqualTo(Id("independent_reds")));Assert.That(begin.NextState.Practice.Blockade.Candidates.Single().GeneralValue,Is.EqualTo(3));}
            else{Assert.That(begin.NextState.Practice.Blockade,Is.Null);Assert.That(begin.NextState.GetInfluence(West,PlayerSide.US),Is.Zero);Assert.That(begin.NextState.Cards.GetHand(PlayerSide.US),Has.Member(Id("de_stalinization")));}
        }
        [Test] public void An_existing_resolving_card_prevents_starting_a_second_event()
        {
            var s=Ready();var c=s.Cards;var other=Id("fidel");var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,c.GetHand(PlayerSide.US)},{PlayerSide.USSR,c.GetHand(PlayerSide.USSR)}};
            var cards=new CardZones(c.Catalog,c.DrawPile.Where(id=>!id.Equals(other)),hands,c.EraReserve,c.DiscardPile,c.RemovedCards,new[]{other},c.China,c.EnteredEras,c.HeadlineReserved);
            s=new GameState(s.Map,s.Countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,cards:cards,randomState:s.RandomState,practice:s.Practice);
            var result=Begin(s);Assert.That(result.Accepted,Is.False);Assert.That(result.NextState,Is.Null);Assert.That(s.Cards.ResolvingCards,Is.EqualTo(new[]{other}));Assert.That(s.Cards.GetHand(PlayerSide.USSR),Has.Member(Id("blockade")));
        }
        [Test] public void No_candidate_fallback_overflow_preserves_root_card_and_influence()
        {
            var s=PlayScoringTests.Replace(Ready(eligible:false),version:long.MaxValue-3);var r=Begin(s);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);
            Assert.That(s.Cards.GetHand(PlayerSide.USSR),Has.Member(Id("blockade")));Assert.That(s.GetInfluence(West,PlayerSide.US),Is.EqualTo(5));Assert.That(s.Cards.RemovedCards,Is.Empty);
        }
    }
}
