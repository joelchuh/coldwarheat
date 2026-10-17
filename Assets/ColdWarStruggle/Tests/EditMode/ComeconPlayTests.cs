using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class ComeconPlayTests
    {
        internal static readonly CardId Card=new CardId("card.comecon");
        internal static CountryId C(string n)=>ComeconRulesTests.C(n);
        internal static ActionPracticeDefinition Bind(ActionPracticeDefinition d,MapDefinition map)
        {
            var old=d.InfluenceEvents??PlayInfluenceEventTests.Effects(map,d.Cards);
            var effects=new PracticeInfluenceCatalog(map,d.Cards,old.Effects.Values.Append(ComeconDefinitionTests.Load()));
            return new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,effects,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment??RealignmentTests.Definition(),d.China,
                d.TurnEndEnabled,d.NextTurnEnabled,d.HeadlineParameters,d.HeadlineDefectors,d.FinalScoring,blockade:d.Blockade,fiveYearPlan:d.FiveYearPlan,wars:d.Wars,spaceEvent:d.SpaceEvent,olympic:d.Olympic);
        }
        internal static Dictionary<CountryId,CountryState> Board(GameState s)
        {
            var b=s.Map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));
            b[C("poland")]=new CountryState(0,4);b[C("romania")]=new CountryState(1,2);b[C("finland")]=new CountryState(2,1);b[C("hungary")]=new CountryState(4,0);b[C("angola")]=new CountryState(3,3);return b;
        }
        internal static GameState Ready(bool us=false)
        {
            var s=PlayScoringTests.Ready(us?"card.olympic_games":Card.Value,configure:d=>Bind(d,d.Risk.Defcon.Map));
            if(us)
            {
                var start=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Influence,"prepare",new CardId("card.olympic_games"));Assert.That(start.Accepted,Is.True);
                var finish=ActionPracticeRules.Plan(start.NextState,PlayerSide.USSR,CardActionKind.FinishInfluence,"prepare-end");Assert.That(finish.Accepted,Is.True);
                s=PlayTurnOpsEventTests.Give(finish.NextState,PlayerSide.US,Card.Value);
            }
            return PlayInfluenceEventTests.Copy(s,Board(s));
        }
        internal static CardActionTransition Start(GameState s)=>ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Event,"comecon-start",Card);
        internal static InfluenceEventTransition Resolve(GameState s)=>InfluenceEventFlow.Resolve(s,PlayerSide.USSR,s.InfluenceEvent.DecisionId,ComeconRulesTests.Targets());
        internal static GameState WithCandidateCount(GameState s,int count)
        {
            var b=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);var targets=ComeconRulesTests.Targets().Take(count).ToArray();
            foreach(var c in s.Map.GetCountriesInRegion("region.eastern_europe"))b[c.Id]=new CountryState(targets.Contains(c.Id)?0:c.Stability,0);
            return PlayInfluenceEventTests.Copy(s,b);
        }
        [TestCase(false)][TestCase(true)] public void Direct_event_fixes_Soviet_choice_and_removes_star_after_one_root(bool us)
        {
            var s=Ready(us);var a=Start(s);Assert.That(a.Accepted,Is.True,a.Error.ToString());var w=a.NextState;
            Assert.That(w.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(w.InfluenceEvent.PhasingPlayer,Is.EqualTo(s.Practice.Actor));Assert.That(w.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));Assert.That(w.Cards.ResolvingCards,Is.EqualTo(new[]{Card}));
            var r=ActionPracticeRules.CompleteEventAction(Resolve(w));Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(n.Cards.RemovedCards.Count(c=>c.Equals(Card)),Is.EqualTo(1));Assert.That(n.Cards.ResolvingCards,Is.Empty);Assert.That(n.InfluenceEvent,Is.Null);Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions+1));
            Assert.That(n.Countries.Sum(x=>(long)x.Value.USSR),Is.EqualTo(s.Countries.Sum(x=>(long)x.Value.USSR)+4));Pcg32RandomTests.Same(s.RandomState,n.RandomState);
        }
        [TestCase(true,CardActionKind.UseOpponentInfluence)][TestCase(false,CardActionKind.UseOpponentInfluence)]
        [TestCase(true,CardActionKind.UseOpponentRealignment)][TestCase(false,CardActionKind.UseOpponentRealignment)]
        [TestCase(true,CardActionKind.UseOpponentCoup)][TestCase(false,CardActionKind.UseOpponentCoup)]
        public void Opponent_card_keeps_both_orders_and_three_OPS_modes(bool first,CardActionKind mode)
        {
            var s=Ready(true);var a=ActionPracticeRules.Plan(s,PlayerSide.US,first?CardActionKind.OpponentEventFirst:CardActionKind.OpponentOpsFirst,"begin",Card);Assert.That(a.Accepted,Is.True);var n=a.NextState;
            if(first){n=ActionPracticeRules.CompleteEventAction(Resolve(n)).NextState;Assert.That(n.Cards.ResolvingCards,Has.Member(Card));Assert.That(n.Cards.RemovedCards,Has.No.Member(Card));}
            Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChoosingOperations));var use=PlayOpponentOpsTests.Use(n,mode,mode==CardActionKind.UseOpponentCoup?C("angola"):(CountryId?)null);Assert.That(use.Accepted,Is.True,use.Error.ToString());n=use.NextState;
            if(mode!=CardActionKind.UseOpponentCoup)
            {
                Assert.That(n.Operations.Budget,Is.EqualTo(s.Practice.Definition.Cards.GetCard(Card).PrintedOps));
                if(mode==CardActionKind.UseOpponentRealignment){var roll=ActionPracticeRules.Plan(n,PlayerSide.US,CardActionKind.RollRealignment,"roll",target:C("angola"));Assert.That(roll.Accepted,Is.True);n=roll.NextState;}
                var finish=ActionPracticeRules.Plan(n,PlayerSide.US,mode==CardActionKind.UseOpponentInfluence?CardActionKind.FinishInfluence:CardActionKind.FinishRealignment,"finish");Assert.That(finish.Accepted,Is.True);n=finish.NextState;
            }
            if(!first){Assert.That(n.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));n=ActionPracticeRules.CompleteEventAction(Resolve(n)).NextState;}
            Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions+1));Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(n.Practice.OpponentOps,Is.Null);Assert.That(n.Cards.ResolvingCards,Is.Empty);Assert.That(n.Cards.RemovedCards.Count(x=>x.Equals(Card)),Is.EqualTo(1));
            foreach(var c in ComeconRulesTests.Targets())Assert.That(n.GetInfluence(c,PlayerSide.USSR),Is.EqualTo(s.GetInfluence(c,PlayerSide.USSR)+1));
        }
        [TestCase(false)][TestCase(true)] public void Headline_choice_returns_to_remaining_headlines_without_action_cost(bool usPlays)
        {
            var raw=HeadlineFlowTests.Prepared(usPlays?"comecon":"nasser",usPlays?"nasser":"comecon");var definition=Bind(raw.Practice.Definition,raw.Map);
            var s=new GameState(raw.Map,Board(raw),raw.Operations,raw.GameId,raw.Version,raw.SpentOps,raw.EventSequence,turn:raw.Turn,deployment:raw.Deployment,cards:raw.Cards,randomState:raw.RandomState,practice:ActionPracticeState.Create(definition));
            var start=HeadlineFlowTests.Start(s);var one=HeadlineFlowTests.Choose(start,PlayerSide.US,usPlays?"comecon":"nasser");var waiting=HeadlineFlowTests.Choose(one,PlayerSide.USSR,usPlays?"nasser":"comecon");
            Assert.That(waiting.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.USSR));var done=HeadlineResolutionRules.CompleteChoice(Resolve(waiting));Assert.That(done.Accepted,Is.True,done.Error.ToString());var n=done.NextState;
            Assert.That(n.Practice.Headline.IsComplete,Is.True);Assert.That(n.Practice.Headline.CompletedCount,Is.EqualTo(2));Assert.That(n.Practice.CompletedActions,Is.Zero);Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(n.InfluenceEvent,Is.Null);Assert.That(n.Cards.RemovedCards,Has.Member(Card));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)] public void Shortage_does_not_move_card_or_finish_root(int count)
        {
            var s=WithCandidateCount(Ready(),count);var a=Start(s);Assert.That(a.Error,Is.EqualTo(RuleError.EventRequiresRuling));Assert.That(a.NextState,Is.Null);Assert.That(s.Cards.GetHand(PlayerSide.USSR),Has.Member(Card));Assert.That(s.Cards.ResolvingCards,Is.Empty);Assert.That(s.Practice.CompletedActions,Is.Zero);
        }
        [Test] public void Accepted_OPS_control_change_stays_but_shortage_cannot_finish_the_parent()
        {
            var s=WithCandidateCount(Ready(true),4);var b=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);var fin=s.Map.GetCountriesInRegion("region.eastern_europe").Single(c=>c.Id.Equals(C("finland")));b[fin.Id]=new CountryState(fin.Stability-1,0);s=PlayInfluenceEventTests.Copy(s,b);
            var a=ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.OpponentOpsFirst,"begin",Card);Assert.That(a.Accepted,Is.True);var use=PlayOpponentOpsTests.Use(a.NextState,CardActionKind.UseOpponentInfluence);Assert.That(use.Accepted,Is.True);var place=InfluenceRules.PlanPlacement(use.NextState,use.NextState.Operations,new[]{fin.Id});Assert.That(place.Accepted,Is.True);var n=place.NextState;
            var finish=ActionPracticeRules.Plan(n,PlayerSide.US,CardActionKind.FinishInfluence,"finish");Assert.That(finish.Error,Is.EqualTo(RuleError.EventRequiresRuling));Assert.That(finish.NextState,Is.Null);Assert.That(n.GetInfluence(fin.Id,PlayerSide.US),Is.EqualTo(fin.Stability));Assert.That(n.Cards.ResolvingCards,Has.Member(Card));Assert.That(n.Cards.RemovedCards,Has.No.Member(Card));Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));Assert.That(n.SpentOps,Is.EqualTo(1));
        }
        [Test] public void Finalization_overflow_discards_otherwise_valid_placement()
        {
            var s=PlayInfluenceEventTests.Copy(Ready(),version:long.MaxValue-3);var w=Start(s).NextState;var raw=Resolve(w);Assert.That(raw.Accepted,Is.True);var done=ActionPracticeRules.CompleteEventAction(raw);Assert.That(done.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(done.NextState,Is.Null);Assert.That(w.GetInfluence(C("poland"),PlayerSide.USSR),Is.EqualTo(4));Assert.That(w.Cards.ResolvingCards,Has.Member(Card));
        }
    }
}
