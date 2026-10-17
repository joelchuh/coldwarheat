using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class InfluenceRelocationPlayTests
    {
        internal static readonly CardId Card=new CardId("card.de_stalinization");
        internal static CountryId C(string n)=>InfluenceRelocationRulesTests.C(n);
        internal static InfluenceAllocation A(string n,int amount)=>InfluenceRelocationRulesTests.A(n,amount);
        internal static ActionPracticeDefinition Bind(ActionPracticeDefinition d,MapDefinition map)
        {
            var old=d.InfluenceEvents??PlayInfluenceEventTests.Effects(map,d.Cards);
            var effects=new PracticeInfluenceCatalog(map,d.Cards,old.Effects.Values.Append(InfluenceRelocationDefinitionTests.Load()));
            return new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,effects,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment??RealignmentTests.Definition(),d.China,
                d.TurnEndEnabled,d.NextTurnEnabled,d.HeadlineParameters,d.HeadlineDefectors,d.FinalScoring,blockade:d.Blockade,fiveYearPlan:d.FiveYearPlan,wars:d.Wars,spaceEvent:d.SpaceEvent,olympic:d.Olympic);
        }
        internal static Dictionary<CountryId,CountryState> Board(GameState s,bool noSource=false)
        {
            var b=s.Map.Countries.ToDictionary(c=>c.Id,c=>new CountryState(0,0));
            b[C("poland")]=new CountryState(0,noSource?0:4);b[C("romania")]=new CountryState(1,noSource?0:2);
            b[C("angola")]=new CountryState(3,noSource?0:3);b[C("cuba")]=new CountryState(3,0);return b;
        }
        internal static GameState Ready(bool us=false,bool noSource=false)
        {
            var s=PlayScoringTests.Ready(us?"card.olympic_games":Card.Value,configure:d=>Bind(d,d.Risk.Defcon.Map));
            if(us)
            {
                var start=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Influence,"prepare",new CardId("card.olympic_games"));Assert.That(start.Accepted,Is.True);
                var finish=ActionPracticeRules.Plan(start.NextState,PlayerSide.USSR,CardActionKind.FinishInfluence,"prepare-end");Assert.That(finish.Accepted,Is.True);
                s=PlayTurnOpsEventTests.Give(finish.NextState,PlayerSide.US,Card.Value);
            }
            return PlayInfluenceEventTests.Copy(s,Board(s,noSource));
        }
        internal static CardActionTransition Start(GameState s)=>ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Event,"relocation-start",Card);
        internal static InfluenceEventTransition Resolve(GameState s,bool zero=false)
            =>InfluenceEventFlow.ResolveRelocation(s,PlayerSide.USSR,s.InfluenceEvent.DecisionId,
                zero?Array.Empty<InfluenceAllocation>():new[]{A("poland",2)},zero?Array.Empty<InfluenceAllocation>():new[]{A("india",2)});
        [TestCase(false,false)][TestCase(false,true)][TestCase(true,false)][TestCase(true,true)]
        public void Direct_event_keeps_USSR_choice_and_one_star_root_completion(bool us,bool zero)
        {
            var s=Ready(us);var started=Start(s);Assert.That(started.Accepted,Is.True,started.Error.ToString());var w=started.NextState;
            Assert.That(w.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(w.InfluenceEvent.PhasingPlayer,Is.EqualTo(s.Practice.Actor));
            Assert.That(w.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));Assert.That(w.Cards.ResolvingCards,Is.EqualTo(new[]{Card}));
            var raw=Resolve(w,zero);Assert.That(raw.Accepted,Is.True,raw.Error.ToString());var done=ActionPracticeRules.CompleteEventAction(raw);Assert.That(done.Accepted,Is.True,done.Error.ToString());
            var n=done.NextState;Assert.That(n.InfluenceEvent,Is.Null);Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions+1));Assert.That(n.Cards.RemovedCards.Count(x=>x.Equals(Card)),Is.EqualTo(1));
            Assert.That(n.Cards.ResolvingCards,Is.Empty);Assert.That(n.Cards.GetHand(s.Practice.Actor),Has.No.Member(Card));Assert.That(n.Practice.Risk,Is.SameAs(s.Practice.Risk));Assert.That(n.Practice.Space,Is.SameAs(s.Practice.Space));Assert.That(n.Practice.VpForUS,Is.EqualTo(s.Practice.VpForUS));Pcg32RandomTests.Same(n.RandomState,s.RandomState);
            Assert.That(n.Countries[C("poland")].USSR,Is.EqualTo(zero?4:2));Assert.That(n.Countries[C("india")].USSR,Is.EqualTo(zero?0:2));
        }
        [TestCase(true,CardActionKind.UseOpponentInfluence)][TestCase(false,CardActionKind.UseOpponentInfluence)]
        [TestCase(true,CardActionKind.UseOpponentRealignment)][TestCase(false,CardActionKind.UseOpponentRealignment)]
        [TestCase(true,CardActionKind.UseOpponentCoup)][TestCase(false,CardActionKind.UseOpponentCoup)]
        public void US_OPS_and_USSR_choice_work_in_both_orders_for_each_map_mode(bool first,CardActionKind mode)
        {
            var s=Ready(true);var start=ActionPracticeRules.Plan(s,PlayerSide.US,first?CardActionKind.OpponentEventFirst:CardActionKind.OpponentOpsFirst,"begin",Card);Assert.That(start.Accepted,Is.True,start.Error.ToString());var n=start.NextState;
            if(first){Assert.That(n.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.USSR));n=ActionPracticeRules.CompleteEventAction(Resolve(n)).NextState;Assert.That(n,Is.Not.Null);Assert.That(n.Cards.ResolvingCards,Has.Member(Card));Assert.That(n.Cards.RemovedCards,Has.No.Member(Card));}
            Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChoosingOperations));Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.US));
            var use=PlayOpponentOpsTests.Use(n,mode,mode==CardActionKind.UseOpponentCoup?C("angola"):(CountryId?)null);Assert.That(use.Accepted,Is.True,use.Error.ToString());n=use.NextState;
            if(mode!=CardActionKind.UseOpponentCoup)
            {
                Assert.That(n.Operations.Budget,Is.EqualTo(s.Practice.Definition.Cards.GetCard(Card).PrintedOps));
                if(mode==CardActionKind.UseOpponentRealignment){var roll=ActionPracticeRules.Plan(n,PlayerSide.US,CardActionKind.RollRealignment,"roll",target:C("angola"));Assert.That(roll.Accepted,Is.True);n=roll.NextState;}
                var finish=ActionPracticeRules.Plan(n,PlayerSide.US,mode==CardActionKind.UseOpponentInfluence?CardActionKind.FinishInfluence:CardActionKind.FinishRealignment,"finish");Assert.That(finish.Accepted,Is.True);n=finish.NextState;
            }
            if(!first){Assert.That(n.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));n=ActionPracticeRules.CompleteEventAction(Resolve(n)).NextState;}
            Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions+1));Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(n.Practice.OpponentOps,Is.Null);Assert.That(n.Cards.ResolvingCards,Is.Empty);Assert.That(n.Cards.RemovedCards.Count(x=>x.Equals(Card)),Is.EqualTo(1));
            Assert.That(n.Countries[C("poland")].USSR,Is.EqualTo(2));Assert.That(n.Countries[C("india")].USSR,Is.EqualTo(2));
        }
        [TestCase(false)][TestCase(true)] public void Headline_choice_resumes_remaining_headlines_without_spending_an_action(bool usPlays)
        {
            var raw=HeadlineFlowTests.Prepared(usPlays?"de_stalinization":"nasser",usPlays?"nasser":"de_stalinization");var definition=Bind(raw.Practice.Definition,raw.Map);
            var s=new GameState(raw.Map,Board(raw),raw.Operations,raw.GameId,raw.Version,raw.SpentOps,raw.EventSequence,turn:raw.Turn,deployment:raw.Deployment,cards:raw.Cards,randomState:raw.RandomState,practice:ActionPracticeState.Create(definition));
            var start=HeadlineFlowTests.Start(s);var one=HeadlineFlowTests.Choose(start,PlayerSide.US,usPlays?"de_stalinization":"nasser");var waiting=HeadlineFlowTests.Choose(one,PlayerSide.USSR,usPlays?"nasser":"de_stalinization");
            Assert.That(waiting.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(waiting.Practice.Actor,Is.EqualTo(usPlays?PlayerSide.US:PlayerSide.USSR));
            var done=HeadlineResolutionRules.CompleteChoice(Resolve(waiting));Assert.That(done.Accepted,Is.True,done.Error.ToString());var n=done.NextState;
            Assert.That(n.Practice.Headline.IsComplete,Is.True);Assert.That(n.Practice.Headline.CompletedCount,Is.EqualTo(2));Assert.That(n.Practice.CompletedActions,Is.Zero);Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChooseCard));Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(n.InfluenceEvent,Is.Null);Assert.That(n.Cards.RemovedCards,Has.Member(Card));Assert.That(n.Countries[C("india")].USSR,Is.EqualTo(2));
        }
        [Test] public void No_source_finishes_automatically_and_removes_star()
        {var s=Ready(noSource:true);var done=Start(s);Assert.That(done.Accepted,Is.True);Assert.That(done.NextState.InfluenceEvent,Is.Null);Assert.That(done.NextState.Cards.RemovedCards,Has.Member(Card));Assert.That(done.NextState.Practice.CompletedActions,Is.EqualTo(1));}
        [Test] public void Rejected_overlap_keeps_pending_card_and_maps_to_requires_ruling()
        {
            var w=Start(Ready()).NextState;var r=InfluenceEventFlow.ResolveRelocation(w,PlayerSide.USSR,w.InfluenceEvent.DecisionId,new[]{A("poland",2)},new[]{A("poland",2)});
            Assert.That(r.Error,Is.EqualTo(RuleError.EventRequiresRuling));Assert.That(r.EffectError,Is.EqualTo(InfluenceEventError.RelocationOverlap));Assert.That(r.NextState,Is.Null);Assert.That(w.Cards.ResolvingCards,Is.EqualTo(new[]{Card}));Assert.That(w.Countries[C("poland")].USSR,Is.EqualTo(4));
        }
        [Test] public void Finalization_overflow_discards_an_otherwise_valid_migration()
        {
            var s=PlayInfluenceEventTests.Copy(Ready(),version:long.MaxValue-3);var w=Start(s).NextState;var raw=Resolve(w);Assert.That(raw.Accepted,Is.True);
            var done=ActionPracticeRules.CompleteEventAction(raw);Assert.That(done.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(done.NextState,Is.Null);Assert.That(w.Countries[C("poland")].USSR,Is.EqualTo(4));Assert.That(w.Cards.ResolvingCards,Is.EqualTo(new[]{Card}));
        }
        [Test] public void Final_ordinary_slot_completes_without_skipping_to_next_turn()
        {
            var s=Ready();var d=s.Practice.Definition;var ordinary=d.Cards.Cards.Where(c=>c.Kind==CardKind.Event).ToArray();
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.USSR,ordinary.Where(c=>c.Affiliation==CardAffiliation.USSR&&!c.Id.Equals(Card)).Select(c=>c.Id).ToArray()},{PlayerSide.US,ordinary.Where(c=>c.Affiliation!=CardAffiliation.USSR||c.Id.Equals(Card)).Select(c=>c.Id).ToArray()}};
            var cards=new CardZones(d.Cards,d.Cards.Cards.Where(c=>c.Kind==CardKind.Scoring).Select(c=>c.Id),hands,Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),s.Cards.China,new[]{GameEra.Early,GameEra.Mid,GameEra.Late});s=PlayInfluenceEventTests.Copy(s,cards:cards);
            var last=s.Turn.Parameters.NormalActionsPerSide*2-1;
            for(var i=0;i<last;i++)
            {var actor=s.Practice.Actor;var card=s.Cards.GetHand(actor).First(c=>!c.Equals(Card)&&ActionPracticeRules.CanPlay(s,actor,c,CardActionKind.Influence)==RuleError.None);var use=ActionPracticeRules.Plan(s,actor,CardActionKind.Influence,"advance-"+i,card);Assert.That(use.Accepted,Is.True);s=ActionPracticeRules.Plan(use.NextState,actor,CardActionKind.FinishInfluence,"end-"+i).NextState;}
            var w=Start(s).NextState;var n=ActionPracticeRules.CompleteEventAction(Resolve(w)).NextState;Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.Complete));Assert.That(n.Practice.CompletedActions,Is.EqualTo(last+1));Assert.That(n.Turn.Number,Is.EqualTo(s.Turn.Number));Assert.That(n.Ended,Is.False);
        }
    }
}
