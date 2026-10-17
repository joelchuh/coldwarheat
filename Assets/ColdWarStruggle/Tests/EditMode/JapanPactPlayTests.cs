using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Infrastructure;
using ColdWarStruggle.Application;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class JapanPactPlayTests
    {
        internal static readonly CardId Card=new CardId("card.us_japan_mutual_defense_pact");
        internal static CountryId C(string n)=>new CountryId("country."+n);
        internal static ActionPracticeDefinition Bind(ActionPracticeDefinition raw)
        {
            var d=FiveYearPlanPlayTests.Bind(raw);var map=d.Risk.Defcon.Map;
            return new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents,d.DefconVpEvents,d.TurnOpsEvents,d.Realignment,
                d.China??new ChinaCardDefinition(d.Cards,map,ChinaCardPlayTests.China,new OpsRegionalBonus("china","region.asia",1)),true,true,d.HeadlineParameters,d.HeadlineDefectors,d.FinalScoring,d.Blockade,d.FiveYearPlan,d.Wars,d.SpaceEvent,
                d.Olympic??OlympicDefinitionTests.Load(cards:d.Cards),JapanPactDefinitionLoader.Load(JapanPactFoundationTests.Data(),JapanPactFoundationTests.Schema(),map,d.Cards));
        }
        internal static GameState Ready(bool us=false,int own=1,int opponent=3)
        {
            var s=PlayScoringTests.Ready(Card.Value,configure:Bind);var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[C("japan")]=new CountryState(own,opponent);s=PlayScoringTests.Replace(s,board);
            return us?PlayTurnOpsEventTests.Give(ChinaCardPlayTests.AdvanceOrdinary(s),PlayerSide.US,Card.Value):s;
        }
        internal static CardActionTransition Use(GameState s,CardActionKind kind=CardActionKind.Event,string card="us_japan_mutual_defense_pact",string target=null)
            =>ActionPracticeRules.Plan(s,s.Practice.Actor,kind,"japan-"+s.Version,card==null?(CardId?)null:new CardId("card."+card),target==null?(CountryId?)null:C(target));
        internal static GameState Active(bool soviet=true)
        {
            var r=Use(Ready());Assert.That(r.Accepted,Is.True,r.Error.ToString());return soviet?ChinaCardPlayTests.AdvanceOrdinary(r.NextState):r.NextState;
        }
        [TestCase(false,1,7)][TestCase(true,1,7)][TestCase(false,9,9)]
        public void Direct_event_grants_control_activates_and_disposes_once(bool us,int own,int after)
        {
            var s=Ready(us,own);var r=Use(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(n.Countries[C("japan")].US,Is.EqualTo(after));Assert.That(n.Countries[C("japan")].USSR,Is.EqualTo(3));Assert.That(n.JapanPact.Instance,Is.SameAs(r.ActivatedJapanPact));Assert.That(r.ActivatedJapanPact.ActivationSequence,Is.EqualTo(n.EventSequence));
            Assert.That(r.EventTransition.Changes.Count,Is.EqualTo(own==after?0:1));Assert.That(n.InfluenceEvent,Is.Null);Assert.That(n.Cards.RemovedCards,Does.Contain(Card));Assert.That(n.Cards.ResolvingCards,Is.Empty);Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions+1));Assert.That(s.JapanPact.Instance,Is.Null);Pcg32RandomTests.Same(s.RandomState,n.RandomState);
        }
        [TestCase(true,0)][TestCase(true,1)][TestCase(false,0)][TestCase(false,1)][TestCase(false,2)]
        public void Grant_activation_or_final_overflow_leaves_no_candidate(bool version,int offset)
        {
            var s=Ready();s=PlayScoringTests.Replace(s,version:version?long.MaxValue-offset:(long?)null,sequence:version?null:(long?)long.MaxValue-offset);var r=Use(s);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(s.Countries[C("japan")].US,Is.EqualTo(1));Assert.That(s.JapanPact.Instance,Is.Null);Assert.That(s.Cards.GetHand(PlayerSide.USSR),Does.Contain(Card));
        }
        [Test] public void Influence_overflow_and_new_repeat_do_not_partially_grant()
        {
            var s=Ready(opponent:int.MaxValue);Assert.That(Use(s).Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(s.JapanPact.Instance,Is.Null);
            s=PlayTurnOpsEventTests.Give(Active(),PlayerSide.USSR,Card.Value);var r=Use(s);Assert.That(r.Error,Is.EqualTo(RuleError.EventRequiresRuling));Assert.That(r.NextState,Is.Null);
        }
        [Test] public async Task Host_replay_returns_same_public_receipt_and_wrong_identity_does_not_act()
        {
            var host=new LocalGameHost(Ready(),true);var session=host.UssrSession;var v=session.ReadView();var command=new CardActionCommand("japan-retry",v.GameId,v.Version,CardActionKind.Event,Card);
            Assert.That((await host.UsSession.SubmitCardActionAsync(command)).Accepted,Is.False);var r=await session.SubmitCardActionAsync(command);Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.ActivatedJapanPact.Country,Is.EqualTo(C("japan")));Assert.That(r.ActivatedJapanPact.BlockedActor,Is.EqualTo(PlayerSide.USSR));Assert.That(r.View.JapanPact.Duration,Is.EqualTo("UntilCancelledOrGameEnd"));Assert.That(r.Event.Changes.Single().After,Is.EqualTo(7));
            Assert.That(await session.SubmitCardActionAsync(command),Is.SameAs(r));Assert.That((await session.SubmitCardActionAsync(new CardActionCommand("japan-retry",v.GameId,v.Version,CardActionKind.Space,Card))).Error,Is.EqualTo(RuleError.CommandIdConflict));
        }
        [TestCase(CardActionKind.OpponentEventFirst)][TestCase(CardActionKind.OpponentOpsFirst)]
        public void Opponent_orders_activate_at_correct_step_and_finish_one_card(CardActionKind order)
        {
            var s=Ready();var first=Use(s,order);Assert.That(first.Accepted,Is.True,first.Error.ToString());Assert.That(first.NextState.JapanPact.Instance!=null,Is.EqualTo(order==CardActionKind.OpponentEventFirst));
            var ops=Use(first.NextState,CardActionKind.UseOpponentInfluence,null);Assert.That(ops.Accepted,Is.True);var done=Use(ops.NextState,CardActionKind.FinishInfluence,null);Assert.That(done.Accepted,Is.True,done.Error.ToString());Assert.That(done.NextState.JapanPact.Instance,Is.Not.Null);Assert.That(done.NextState.Cards.RemovedCards,Does.Contain(Card));Assert.That(done.NextState.Practice.CompletedActions,Is.EqualTo(1));Assert.That((order==CardActionKind.OpponentEventFirst?first:done).ActivatedJapanPact,Is.Not.Null);
        }
        [Test] public void Ops_first_can_coup_Japan_before_event_then_retains_combined_receipts()
        {
            var s=Use(Ready(),CardActionKind.OpponentOpsFirst).NextState;Assert.That(s,Is.Not.Null);var r=Use(s,CardActionKind.UseOpponentCoup,null,"japan");Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.CoupReceipt,Is.Not.Null);Assert.That(r.ActivatedJapanPact,Is.Not.Null);Assert.That(r.EventTransition,Is.Not.Null);Assert.That(r.NextState.JapanPact.Instance,Is.Not.Null);Assert.That(ControlRules.GetController(r.NextState.Countries[C("japan")].US,r.NextState.Countries[C("japan")].USSR,r.NextState.Map.GetCountry(C("japan")).Stability),Is.EqualTo(PlayerSide.US));
        }
        [TestCase("olympic_games")][TestCase("china")]
        public void Ordinary_and_China_coups_are_blocked_before_random_or_risk(string card)
        {
            var s=Active();if(card!="china")s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card."+card);
            Assert.That(ActionPracticeRules.CheckCoupTarget(s,PlayerSide.USSR,new CardId("card."+card),C("japan")),Is.EqualTo(RuleError.EventForbidden));var r=Use(s,CardActionKind.Coup,card,"japan");Assert.That(r.Error,Is.EqualTo(RuleError.EventForbidden));Assert.That(r.NextState,Is.Null);Assert.That(s.Practice.Risk.Military.UssrEarned,Is.Zero);Assert.That(s.SpentOps,Is.Zero);
        }
        [TestCase("ordinary")][TestCase("opponent")][TestCase("olympic")]
        public void Every_supported_coup_route_uses_same_treaty_gate(string route)
        {
            var s=Active();CardActionKind mode;
            if(route=="opponent"){s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.containment");s=Use(s,CardActionKind.OpponentEventFirst,"containment").NextState;mode=CardActionKind.UseOpponentCoup;}
            else {s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.olympic_games");if(route=="olympic"){s=OlympicPlayTests.Respond(OlympicPlayTests.Play(s).NextState,false).NextState;mode=CardActionKind.UseOlympicCoup;}else mode=CardActionKind.Coup;}
            Assert.That(s,Is.Not.Null);var before=s.RandomState.DrawCount;var r=Use(s,mode,route=="ordinary"?"olympic_games":null,"japan");Assert.That(r.Error,Is.EqualTo(RuleError.EventForbidden));Assert.That(r.NextState,Is.Null);Assert.That(s.RandomState.DrawCount,Is.EqualTo(before));
        }
        [TestCase("ordinary")][TestCase("china")][TestCase("opponent")][TestCase("olympic")]
        public void Every_supported_adjustment_route_blocks_Japan_without_spending_or_rolling(string route)
        {
            var s=Active();CardActionTransition started;
            if(route=="opponent"){s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.containment");s=Use(s,CardActionKind.OpponentEventFirst,"containment").NextState;started=Use(s,CardActionKind.UseOpponentRealignment,null);}
            else if(route=="olympic"){s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.olympic_games");s=OlympicPlayTests.Respond(OlympicPlayTests.Play(s).NextState,false).NextState;started=Use(s,CardActionKind.UseOlympicRealignment,null);}
            else {if(route=="ordinary")s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.olympic_games");started=Use(s,CardActionKind.Realignment,route=="china"?"china":"olympic_games");}
            Assert.That(started.Accepted,Is.True,started.Error.ToString());var n=started.NextState;Assert.That(ActionPracticeRules.CheckRealignmentTarget(n,PlayerSide.USSR,C("japan")),Is.EqualTo(RuleError.EventForbidden));var r=Use(n,CardActionKind.RollRealignment,null,"japan");Assert.That(r.Error,Is.EqualTo(RuleError.EventForbidden));Assert.That(r.NextState,Is.Null);Assert.That(n.SpentOps,Is.Zero);Pcg32RandomTests.Same(n.RandomState,s.RandomState);
        }
        [Test] public void Treaty_allows_US_other_targets_and_legal_influence_that_changes_control()
        {
            var us=PlayTurnOpsEventTests.Give(Active(false),PlayerSide.US,"card.olympic_games");Assert.That(ActionPracticeRules.CheckCoupTarget(us,PlayerSide.US,OlympicPlayTests.Card,C("japan")),Is.EqualTo(RuleError.None));
            var s=PlayTurnOpsEventTests.Give(Active(),PlayerSide.USSR,"card.olympic_games");var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[C("angola")]=new CountryState(1,0);board[C("japan")]=new CountryState(0,10);s=PlayScoringTests.Replace(s,board);
            Assert.That(ActionPracticeRules.CheckCoupTarget(s,PlayerSide.USSR,OlympicPlayTests.Card,C("angola")),Is.EqualTo(RuleError.None));var a=Use(s,CardActionKind.Influence,"olympic_games");Assert.That(a.Accepted,Is.True);var placed=InfluenceRules.PlanPlacement(a.NextState,a.NextState.Operations,new[]{C("japan")});Assert.That(placed.Accepted,Is.True,placed.Error.ToString());Assert.That(placed.NextState.JapanPact,Is.SameAs(s.JapanPact));Assert.That(JapanPactRules.Evaluate(placed.NextState,placed.NextState.JapanPact,TreatyAction.Realignment,PlayerSide.USSR,C("japan")).Status,Is.EqualTo(TreatyRestrictionStatus.BlockedByTreaty));
        }
        [TestCase(false)][TestCase(true)] public void Headline_event_preserves_permanent_state_and_grant(bool us)
        {
            var raw=HeadlineFlowTests.Prepared(us?"us_japan_mutual_defense_pact":"nasser",us?"nasser":"us_japan_mutual_defense_pact");var d=Bind(raw.Practice.Definition);
            var s=new GameState(raw.Map,raw.Countries,raw.Operations,raw.GameId,raw.Version,raw.SpentOps,raw.EventSequence,turn:raw.Turn,deployment:raw.Deployment,cards:raw.Cards,randomState:raw.RandomState,practice:ActionPracticeState.Create(d),japanPact:JapanPactState.Create(d.JapanPact,raw.GameId));
            var n=HeadlineFlowTests.Choose(HeadlineFlowTests.Choose(HeadlineFlowTests.Start(s),PlayerSide.US,us?"us_japan_mutual_defense_pact":"nasser"),PlayerSide.USSR,us?"nasser":"us_japan_mutual_defense_pact");Assert.That(n.Practice.Headline.IsComplete,Is.True);Assert.That(n.Practice.CompletedActions,Is.Zero);Assert.That(n.JapanPact.Instance,Is.Not.Null);Assert.That(n.InfluenceEvent,Is.Null);Assert.That(n.Cards.RemovedCards,Does.Contain(Card));
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)] public void Five_year_plan_Japan_child_completes_parent_with_both_effect_receipts(PlayerSide actor)
        {
            var s=Ready(actor==PlayerSide.US);s=PlayTurnOpsEventTests.Give(s,actor,"card.five_year_plan");var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,actor==PlayerSide.US?new[]{FiveYearPlanPlayTests.Id("five_year_plan")}:Array.Empty<CardId>()},{PlayerSide.USSR,actor==PlayerSide.USSR?new[]{Card,FiveYearPlanPlayTests.Id("five_year_plan")}:new[]{Card}}};var used=hands.Values.SelectMany(x=>x).ToHashSet();var c=s.Cards;
            var cards=new CardZones(c.Catalog,c.Catalog.Cards.Where(x=>x.Kind!=CardKind.China&&!used.Contains(x.Id)).Select(x=>x.Id),hands,Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),c.China,c.EnteredEras);s=PlayInfluenceEventTests.Copy(s,cards:cards);
            var r=FiveYearPlanPlayTests.Begin(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.FiveYearPlanResult.Outcome,Is.EqualTo(FiveYearPlanOutcome.UsEventResolved));Assert.That(r.ActivatedJapanPact,Is.Not.Null);Assert.That(r.EventTransition,Is.Not.Null);Assert.That(r.NextState.JapanPact.Instance,Is.Not.Null);Assert.That(r.NextState.Practice.FiveYearPlan,Is.Null);Assert.That(r.NextState.Cards.ResolvingCards,Is.Empty);Assert.That(r.NextState.Cards.RemovedCards,Does.Contain(Card));Assert.That(r.NextState.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions+1));
        }
        [Test] public void Turn_end_and_next_turn_keep_pact_after_turn_effect_expiry()
        {
            var s=Ready();
            var deal=InitialDealService.Create(s.Practice.Definition.Cards,new InitialDealDefinition(s.Turn,CardZonesTests.China()),new Pcg32Random(s.RandomState));
            s=new GameState(s.Map,s.Countries,s.Operations,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,cards:deal.Cards,randomState:deal.Random,practice:s.Practice,japanPact:s.JapanPact);
            s=Use(PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,Card.Value)).NextState;Assert.That(s,Is.Not.Null);
            var active=s.JapanPact;var assessed=NextTurnPlayTests.Assessed(s);Assert.That(assessed.JapanPact,Is.SameAs(active));var n=NextTurnPlayTests.Apply(assessed,CardActionKind.BeginNextTurn);Assert.That(n.JapanPact,Is.SameAs(active));Assert.That(n.Turn.Number,Is.EqualTo(2));Assert.That(JapanPactRules.Evaluate(n,n.JapanPact,TreatyAction.Coup,PlayerSide.USSR,C("japan")).Status,Is.EqualTo(TreatyRestrictionStatus.BlockedByTreaty));
        }
        [TestCase("missing")][TestCase("game")][TestCase("definition")][TestCase("future-sequence")][TestCase("future-version")]
        public void Aggregate_rejects_missing_mismatched_or_uncommitted_permanent_state(string fault)
        {
            var s=Ready();var pact=s.JapanPact;Assert.That(pact,Is.Not.Null);var version=s.Version;var sequence=s.EventSequence;
            if(fault=="missing")pact=null;else if(fault=="game")pact=JapanPactState.Create(s.Practice.Definition.JapanPact,"other");else if(fault=="definition")pact=JapanPactState.Create(JapanPactFoundationTests.Load(s.Map),s.GameId);
            else {pact=pact.Activate(s,PlayerSide.US,"future").After;if(fault=="future-sequence")version++;else sequence++;}
            Assert.That(()=>new GameState(s.Map,s.Countries,s.Operations,s.GameId,version,s.SpentOps,sequence,turn:s.Turn,cards:s.Cards,randomState:s.RandomState,practice:s.Practice,japanPact:pact),Throws.InstanceOf<ArgumentException>());
        }
    }
}
