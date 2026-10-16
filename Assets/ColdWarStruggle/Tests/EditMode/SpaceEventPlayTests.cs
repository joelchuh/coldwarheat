using System;
using System.Linq;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class SpaceEventPlayTests
    {
        internal static readonly CardId Card=new CardId("card.captured_nazi_scientist");
        internal static ActionPracticeDefinition Bind(ActionPracticeDefinition d,SpaceEventDefinition spaceEvent=null)
            =>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment,d.China,
                d.TurnEndEnabled,d.NextTurnEnabled,d.HeadlineParameters,d.HeadlineDefectors,d.FinalScoring,d.Blockade,d.FiveYearPlan,d.Wars,
                spaceEvent??SpaceEventDefinitionLoader.Load(PlayInfluenceEventTests.Read("Data/Cards/deluxe-2015/space-events-batch1.parameters.json"),PlayInfluenceEventTests.Read("Docs/Contracts/space-events-batch1.schema.json"),d.Cards,d.Space,BudgetedRemovalCatalogTests.KnownEffects));
        internal static GameState Ready(PlayerSide actor=PlayerSide.USSR,int us=0,int su=0,int vp=0,int turn=2)
        {
            var s=PlayScoringTests.Ready(Card.Value,configure:d=>Bind(d));var d=s.Practice.Definition;
            var p=ActionPracticeState.CreateAtTurn(d,turn,vp,new SpaceTrackState(d.Space,us,su));
            s=new GameState(s.Map,s.Countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:p.Space.Turn,cards:s.Cards,randomState:s.RandomState,practice:p);
            s=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.StartPractice,"start").NextState;
            if(actor==PlayerSide.US)
            {
                s=PlayTurnOpsEventTests.Give(s,PlayerSide.US,Card.Value);s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.olympic_games");s=Pass(s,"card.olympic_games");
            }
            return s;
        }
        internal static GameState Pass(GameState s,string name)
        {
            var first=ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Influence,"prep",new CardId(name));Assert.That(first.Accepted,Is.True,first.Error.ToString());
            var end=ActionPracticeRules.Plan(first.NextState,first.NextState.Practice.Actor,CardActionKind.FinishInfluence,"finish");Assert.That(end.Accepted,Is.True,end.Error.ToString());return end.NextState;
        }
        internal static CardActionTransition Play(GameState s)=>ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Event,"scientist",Card);
        [TestCase(PlayerSide.US,true)][TestCase(PlayerSide.US,false)][TestCase(PlayerSide.USSR,true)][TestCase(PlayerSide.USSR,false)]
        public void Real_card_event_awards_once_without_attempt_die_or_other_rule_changes(PlayerSide side,bool first)
        {
            var s=Ready(side,side==PlayerSide.USSR && !first?1:0,side==PlayerSide.US && !first?1:0);var r=Play(s);
            Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;var reward=first?2:1;
            Assert.That(r.SpaceEventResult.Receipt.AwardVp,Is.EqualTo(reward));Assert.That(r.SpaceEventResult.Receipt.FirstArrival,Is.EqualTo(first));
            Assert.That(n.Practice.VpForUS,Is.EqualTo(side==PlayerSide.US?reward:-reward));Assert.That(n.Practice.Space.Track.PositionFor(side),Is.EqualTo(1));
            Assert.That(n.Practice.Space.AttemptsFor(side),Is.Zero);Assert.That(n.Practice.Space.EventReceipts.Count,Is.EqualTo(1));
            Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions+1));Assert.That(n.Cards.RemovedCards,Has.Member(Card));Assert.That(n.Cards.GetHand(side),Has.No.Member(Card));Assert.That(n.Cards.ResolvingCards,Is.Empty);
            Pcg32RandomTests.Same(s.RandomState,n.RandomState);Assert.That(n.Countries,Is.EquivalentTo(s.Countries));Assert.That(n.Practice.Risk.Defcon,Is.SameAs(s.Practice.Risk.Defcon));Assert.That(n.Practice.Risk.Military,Is.SameAs(s.Practice.Risk.Military));
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public void Vp_victory_keeps_audit_card_and_stops_root(PlayerSide side)
        {
            var s=Ready(side,vp:side==PlayerSide.US?19:-19);var r=Play(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());
            Assert.That(r.NextState.Ended,Is.True);Assert.That(r.NextState.Practice.Risk.Result.Winner,Is.EqualTo(side));Assert.That(r.NextState.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));Assert.That(r.NextState.Cards.ResolvingCards,Has.Member(Card));Assert.That(r.NextState.Cards.RemovedCards,Has.No.Member(Card));
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public void Terminal_event_rejects_atomically_while_normal_ops_remains_available(PlayerSide side)
        {
            var s=Ready(side,side==PlayerSide.US?8:0,side==PlayerSide.USSR?8:0);var r=Play(s);Assert.That(r.Error,Is.EqualTo(RuleError.EventRequiresRuling));Assert.That(r.NextState,Is.Null);
            Assert.That(ActionPracticeRules.CanPlay(s,side,Card,CardActionKind.Event),Is.EqualTo(RuleError.EventRequiresRuling));Assert.That(ActionPracticeRules.CanPlay(s,side,Card,CardActionKind.Influence),Is.EqualTo(RuleError.None));
            Assert.That(s.Cards.GetHand(side),Has.Member(Card));Assert.That(s.Practice.Space.EventReceipts,Is.Empty);Assert.That(s.Practice.VpForUS,Is.Zero);
        }
        [Test] public void One_ops_card_can_event_but_cannot_use_ordinary_space_and_ops_does_not_fire_event()
        {
            var s=Ready();Assert.That(ActionPracticeRules.CanPlay(s,PlayerSide.USSR,Card,CardActionKind.Space),Is.Not.EqualTo(RuleError.None));
            var r=Pass(s,Card.Value);Assert.That(r.Practice.Space.Track.USSR,Is.Zero);Assert.That(r.Cards.DiscardPile,Has.Member(Card));Assert.That(r.Practice.Space.EventReceipts,Is.Empty);
            Assert.That(Play(s).Accepted,Is.True);
        }
        [Test] public void Already_spent_attempt_is_preserved_when_event_unlocks_second_attempt()
        {
            var s=Ready(su:1);s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.nuclear_test_ban");s=PlayScoringTests.Replace(s,random:WarPlayTests.Die(6));
            var ordinary=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Space,"ordinary",new CardId("card.nuclear_test_ban"));Assert.That(ordinary.Accepted,Is.True,ordinary.Error.ToString());Assert.That(ordinary.SpaceReceipt.Success,Is.False);
            var next=PlayTurnOpsEventTests.Give(ordinary.NextState,PlayerSide.US,"card.olympic_games");next=Pass(next,"card.olympic_games");
            var r=Play(next);Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.NextState.Practice.Space.UssrAttempts,Is.EqualTo(1));Assert.That(r.NextState.Practice.Space.Receipts.Count,Is.EqualTo(1));Assert.That(SpaceAbilityRules.Query(r.NextState.Practice.Space.Track,PlayerSide.USSR).AttemptLimit,Is.EqualTo(2));Pcg32RandomTests.Same(next.RandomState,r.NextState.RandomState);
        }
        [TestCase(true)][TestCase(false)] public void Counter_overflow_does_not_publish_space_or_vp(bool version)
        {var s=Ready();s=PlayScoringTests.Replace(s,version:version?long.MaxValue:s.Version,sequence:version?s.EventSequence:long.MaxValue);var r=Play(s);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(s.Practice.Space.EventReceipts,Is.Empty);}
        [Test] public void Event_registration_requires_exact_cards_track_and_risk()
        {
            var d=Ready().Practice.Definition;Assert.Throws<ArgumentException>(()=>Bind(d,Ready().Practice.Definition.SpaceEvent));
            Assert.Throws<ArgumentException>(()=>new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,spaceEvent:d.SpaceEvent));
        }
        [TestCase(2)][TestCase(3)] public void Receipt_source_contains_turn_and_action_slot(int turn)
        {var r=Play(Ready(turn:turn));Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.SpaceEventResult.Receipt.Request.SourceActionId,Does.Contain("turn."+turn+".action.0"));}
        [TestCase(true)][TestCase(false)] public void Event_reaching_eight_updates_actual_action_scheduler(bool first)
        {
            var s=Ready(us:first?0:8,su:7);var r=Play(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());s=r.NextState;
            while(s.Practice.CompletedActions<12)
            {s=PlayTurnOpsEventTests.Give(s,s.Practice.Actor,"card.olympic_games");s=Pass(s,"card.olympic_games");}
            Assert.That(s.Practice.Rounds.IsComplete,Is.EqualTo(!first));
            if(first){Assert.That(s.Practice.Rounds.IsOptional,Is.True);Assert.That(s.Practice.Actor,Is.EqualTo(PlayerSide.USSR));}
        }
    }
}
