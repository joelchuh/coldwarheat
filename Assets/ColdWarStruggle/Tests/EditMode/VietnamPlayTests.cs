using System;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
using ColdWarStruggle.Application;
using ColdWarStruggle.Infrastructure;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class VietnamPlayTests
    {
        internal static readonly CardId Card=new CardId("card.vietnam_revolts");
        internal static CountryId C(string name)=>new CountryId("country."+name);
        internal static ActionPracticeDefinition Bind(ActionPracticeDefinition d)
        {
            var map=d.Risk.Defcon.Map;
            var effects=(d.TurnOpsEvents??PlayTurnOpsEventTests.Effects(d.Cards)).WithVietnam(VietnamRevoltsDefinitionLoader.Load(TurnOpsEffectTests.Data(),TurnOpsEffectTests.Schema(),map,d.Cards));
            return new ActionPracticeDefinition(d.Cards,d.Ops,d.Space,d.Calendar,d.Risk,d.Scoring,d.InfluenceEvents??PlayInfluenceEventTests.Effects(map,d.Cards),d.DefconVpEvents??PlayDefconVpEventTests.Effects(d.Cards),effects,d.Realignment??RealignmentTests.Definition(),d.China??new ChinaCardDefinition(d.Cards,map,ChinaCardPlayTests.China,new OpsRegionalBonus("china","region.asia",1)),
                d.TurnEndEnabled,d.NextTurnEnabled,d.HeadlineParameters,d.HeadlineDefectors,d.FinalScoring,blockade:d.Blockade,fiveYearPlan:d.FiveYearPlan,wars:d.Wars,spaceEvent:d.SpaceEvent,olympic:d.Olympic??OlympicDefinitionTests.Load(cards:d.Cards));
        }
        internal static GameState Ready(bool us=false)
        {
            var s=PlayScoringTests.Ready(Card.Value,configure:Bind);
            var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[C("vietnam")]=new CountryState(0,0);board[C("thailand")]=new CountryState(2,2);board[C("japan")]=new CountryState(2,2);board[C("angola")]=new CountryState(2,2);
            s=PlayScoringTests.Replace(s,board);
            if(us){s=ChinaCardPlayTests.AdvanceOrdinary(s);s=PlayTurnOpsEventTests.Give(s,PlayerSide.US,Card.Value);}return s;
        }
        internal static CardActionTransition Play(GameState s)=>ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Event,"vietnam",Card);
        internal static GameState Activated()
        {var r=Play(Ready());Assert.That(r.Accepted,Is.True,r.Error.ToString());return ChinaCardPlayTests.AdvanceOrdinary(r.NextState);}
        static CardActionTransition Use(GameState s,CardActionKind kind,string card=null,string target=null)
            =>ActionPracticeRules.Plan(s,s.Practice.Actor,kind,"next",card==null?(CardId?)null:new CardId("card."+card),target==null?(CountryId?)null:C(target));
        [TestCase(false)][TestCase(true)] public void Either_controller_grants_Soviets_then_activates_and_completes_one_root(bool us)
        {
            var s=Ready(us);var r=Play(s);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(n.GetInfluence(C("vietnam"),PlayerSide.USSR),Is.EqualTo(2));Assert.That(n.GetInfluence(C("vietnam"),PlayerSide.US),Is.Zero);
            Assert.That(r.EventTransition.Changes.Single().After,Is.EqualTo(2));Assert.That(r.ActivatedOpsEffect.AffectedSide,Is.EqualTo(PlayerSide.USSR));Assert.That(r.ActivatedOpsEffect.ActivationSequence,Is.EqualTo(n.EventSequence));
            Assert.That(n.InfluenceEvent,Is.Null);Assert.That(n.Cards.RemovedCards,Does.Contain(Card));Assert.That(n.Cards.ResolvingCards,Is.Empty);Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions+1));
            Assert.That(s.Practice.OpsEffects.Instances,Is.Empty);Assert.That(s.GetInfluence(C("vietnam"),PlayerSide.USSR),Is.Zero);Pcg32RandomTests.Same(s.RandomState,n.RandomState);
        }
        [TestCase(true,0)][TestCase(true,1)][TestCase(false,0)][TestCase(false,1)][TestCase(false,2)]
        public void Grant_or_final_commit_overflow_publishes_nothing(bool version,int offset)
        {
            var s=Ready();s=PlayScoringTests.Replace(s,version:version?long.MaxValue-offset:(long?)null,sequence:version?null:(long?)long.MaxValue-offset);
            var r=Play(s);Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(s.GetInfluence(C("vietnam"),PlayerSide.USSR),Is.Zero);Assert.That(s.Practice.OpsEffects.Instances,Is.Empty);Assert.That(s.Cards.GetHand(PlayerSide.USSR),Does.Contain(Card));
        }
        [Test] public void Influence_overflow_and_repeat_cannot_partially_activate_or_grant()
        {
            var s=Ready();var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[C("vietnam")]=new CountryState(0,int.MaxValue);s=PlayScoringTests.Replace(s,board);Assert.That(Play(s).Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(s.Practice.OpsEffects.Instances,Is.Empty);
            s=PlayTurnOpsEventTests.Give(Activated(),PlayerSide.USSR,Card.Value);var r=Play(s);Assert.That(r.Error,Is.EqualTo(RuleError.EventRequiresRuling));Assert.That(r.NextState,Is.Null);Assert.That(s.GetInfluence(C("vietnam"),PlayerSide.USSR),Is.EqualTo(2));
        }
        [Test] public async Task Host_retry_preserves_both_receipts_and_rejects_changed_payload()
        {
            var host=new LocalGameHost(Ready(),true);var session=host.UssrSession;var v=session.ReadView();var command=new CardActionCommand("vietnam-retry",v.GameId,v.Version,CardActionKind.Event,Card);
            var r=await session.SubmitCardActionAsync(command);Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.Event.Changes.Single().After,Is.EqualTo(2));Assert.That(r.ActivatedOpsEffect.AffectedSide,Is.EqualTo(PlayerSide.USSR));
            Assert.That(await session.SubmitCardActionAsync(command),Is.SameAs(r));Assert.That(session.ReadView().Version,Is.EqualTo(r.View.Version));Assert.That((await session.SubmitCardActionAsync(new CardActionCommand("vietnam-retry",v.GameId,v.Version,CardActionKind.Space,Card))).Error,Is.EqualTo(RuleError.CommandIdConflict));
        }
        [TestCase(CardActionKind.Influence,3)][TestCase(CardActionKind.Realignment,3)]
        public void Ordinary_card_and_China_receive_separate_conditional_budgets(CardActionKind kind,int expected)
        {
            var s=PlayTurnOpsEventTests.Give(Activated(),PlayerSide.USSR,"card.olympic_games");var r=Use(s,kind,"olympic_games");Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.NextState.Operations.Budget,Is.EqualTo(expected));Assert.That(r.NextState.Operations.RegionalBudget.BudgetFor(C("japan")),Is.EqualTo(2));
            r=Use(s,kind,"china");Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.NextState.Operations.Budget,Is.EqualTo(6));Assert.That(r.NextState.Operations.RegionalBudget.BudgetFor(C("japan")),Is.EqualTo(5));Assert.That(r.NextState.Operations.RegionalBudget.BudgetFor(C("angola")),Is.EqualTo(4));
        }
        [TestCase("thailand",3)][TestCase("japan",2)][TestCase("angola",2)]
        public void Normal_coup_quote_strength_and_military_match_target(string target,int expected)
        {
            var s=PlayTurnOpsEventTests.Give(Activated(),PlayerSide.USSR,"card.olympic_games");Assert.That(ActionPracticeRules.QuoteCoupStrength(s,PlayerSide.USSR,OlympicPlayTests.Card,C(target)),Is.EqualTo(expected));
            var r=Use(s,CardActionKind.Coup,"olympic_games",target);Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.CoupReceipt.Calculation.Strength,Is.EqualTo(expected));Assert.That(r.CoupReceipt.MilitaryCredit.Amount,Is.EqualTo(expected));
        }
        [TestCase(CardActionKind.OpponentEventFirst)][TestCase(CardActionKind.OpponentOpsFirst)]
        public void US_plays_Vietnam_in_either_order_without_receiving_regional_bonus(CardActionKind order)
        {
            var s=Ready(true);var first=Use(s,order,"vietnam_revolts");Assert.That(first.Accepted,Is.True,first.Error.ToString());
            Assert.That(first.NextState.GetInfluence(C("vietnam"),PlayerSide.USSR),Is.EqualTo(order==CardActionKind.OpponentEventFirst?2:0));
            var ops=Use(first.NextState,CardActionKind.UseOpponentInfluence);Assert.That(ops.Accepted,Is.True,ops.Error.ToString());Assert.That(ops.NextState.Operations.Budget,Is.EqualTo(2));Assert.That(ops.NextState.Operations.RegionalBudget,Is.Null);
            var done=Use(ops.NextState,CardActionKind.FinishInfluence);Assert.That(done.Accepted,Is.True,done.Error.ToString());Assert.That(done.NextState.GetInfluence(C("vietnam"),PlayerSide.USSR),Is.EqualTo(2));Assert.That(done.NextState.Cards.RemovedCards,Does.Contain(Card));Assert.That(done.NextState.InfluenceEvent,Is.Null);
            var receipt=order==CardActionKind.OpponentEventFirst?first:done;Assert.That(receipt.EventTransition.Changes.Count,Is.EqualTo(1));Assert.That(receipt.ActivatedOpsEffect,Is.Not.Null);
        }
        [TestCase(CardActionKind.UseOpponentInfluence)][TestCase(CardActionKind.UseOpponentRealignment)][TestCase(CardActionKind.UseOpponentCoup)]
        public void Opponent_provided_OPS_uses_actual_Soviet_actor(CardActionKind mode)
        {
            var s=PlayTurnOpsEventTests.Give(Activated(),PlayerSide.USSR,"card.containment");var first=Use(s,CardActionKind.OpponentEventFirst,"containment");Assert.That(first.Accepted,Is.True,first.Error.ToString());
            var r=Use(first.NextState,mode,target:mode==CardActionKind.UseOpponentCoup?"thailand":null);Assert.That(r.Accepted,Is.True,r.Error.ToString());
            if(mode==CardActionKind.UseOpponentCoup){Assert.That(r.CoupReceipt.Calculation.Strength,Is.EqualTo(4));Assert.That(r.CoupReceipt.MilitaryCredit.Amount,Is.EqualTo(4));}else{Assert.That(r.NextState.Operations.Budget,Is.EqualTo(4));Assert.That(r.NextState.Operations.RegionalBudget.BudgetFor(C("angola")),Is.EqualTo(3));}
        }
        [TestCase(CardActionKind.UseOlympicInfluence)][TestCase(CardActionKind.UseOlympicRealignment)][TestCase(CardActionKind.UseOlympicCoup)]
        public void Olympic_event_OPS_receives_bonus_without_physical_China_bonus(CardActionKind mode)
        {
            var s=PlayTurnOpsEventTests.Give(Activated(),PlayerSide.USSR,"card.olympic_games");var first=OlympicPlayTests.Play(s);Assert.That(first.Accepted,Is.True,first.Error.ToString());var boycott=OlympicPlayTests.Respond(first.NextState,false);Assert.That(boycott.Accepted,Is.True,boycott.Error.ToString());
            var r=Use(boycott.NextState,mode,target:mode==CardActionKind.UseOlympicCoup?"thailand":null);Assert.That(r.Accepted,Is.True,r.Error.ToString());
            if(mode==CardActionKind.UseOlympicCoup){Assert.That(r.CoupReceipt.Calculation.Strength,Is.EqualTo(5));Assert.That(r.CoupReceipt.MilitaryCredit.Amount,Is.EqualTo(5));}else{Assert.That(r.NextState.Operations.Budget,Is.EqualTo(5));Assert.That(r.NextState.Operations.RegionalBudget.BudgetFor(C("angola")),Is.EqualTo(4));}
        }
        [Test] public void Space_and_threshold_stay_general_and_US_never_receives_Soviet_bonus()
        {
            var first=Play(Ready());Assert.That(first.Accepted,Is.True);var s=PlayTurnOpsEventTests.Give(first.NextState,PlayerSide.US,"card.olympic_games");Assert.That(Use(s,CardActionKind.Influence,"olympic_games").NextState.Operations.Budget,Is.EqualTo(2));
            s=Activated();foreach(var purpose in new[]{OpsValuePurpose.Space,OpsValuePurpose.OpsThreshold})Assert.That(ActionPracticeRules.QuoteCard(s,PlayerSide.USSR,OlympicPlayTests.Card,purpose).GeneralValue,Is.EqualTo(2));
        }
        [TestCase(false)][TestCase(true)] public void Headline_composes_grant_and_effect_and_preserves_action_rounds(bool us)
        {
            var raw=HeadlineFlowTests.Prepared(us?"vietnam_revolts":"nasser",us?"nasser":"vietnam_revolts");var d=Bind(raw.Practice.Definition);
            var s=new GameState(raw.Map,raw.Countries,raw.Operations,raw.GameId,raw.Version,raw.SpentOps,raw.EventSequence,turn:raw.Turn,deployment:raw.Deployment,cards:raw.Cards,randomState:raw.RandomState,practice:ActionPracticeState.Create(d));
            var n=HeadlineFlowTests.Choose(HeadlineFlowTests.Choose(HeadlineFlowTests.Start(s),PlayerSide.US,us?"vietnam_revolts":"nasser"),PlayerSide.USSR,us?"nasser":"vietnam_revolts");
            Assert.That(n.Practice.Headline.IsComplete,Is.True);Assert.That(n.Practice.CompletedActions,Is.Zero);Assert.That(n.InfluenceEvent,Is.Null);Assert.That(n.Practice.OpsEffects.Instances.Single().AffectedSide,Is.EqualTo(PlayerSide.USSR));Assert.That(n.GetInfluence(C("vietnam"),PlayerSide.USSR),Is.EqualTo(s.GetInfluence(C("vietnam"),PlayerSide.USSR)+2));
        }
        [TestCase(OperationsMode.Influence)][TestCase(OperationsMode.Realignment)]
        public void Olympic_regional_progress_rebinds_only_trusted_context_and_never_restores_lost_bonus(OperationsMode mode)
        {
            var s=PlayTurnOpsEventTests.Give(Activated(),PlayerSide.USSR,"card.olympic_games");var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);board[C("thailand")]=new CountryState(20,20);board[C("japan")]=new CountryState(20,20);s=PlayScoringTests.Replace(s,board);
            s=OlympicPlayTests.Respond(OlympicPlayTests.Play(s).NextState,false).NextState;s=Use(s,mode==OperationsMode.Influence?CardActionKind.UseOlympicInfluence:CardActionKind.UseOlympicRealignment).NextState;Assert.That(s,Is.Not.Null);
            GameState Spend(GameState x,string target)
            {if(mode==OperationsMode.Influence){var a=InfluenceRules.PlanPlacement(x,x.Operations,new[]{C(target)});Assert.That(a.Accepted,Is.True,a.Error.ToString());return a.NextState;}var b=Use(x,CardActionKind.RollRealignment,target:target);Assert.That(b.Accepted,Is.True,b.Error.ToString());return b.NextState;}
            s=Spend(s,"thailand");Assert.That(s.SpentOps,Is.EqualTo(1));Assert.That(ActionPracticeRules.CanContinueOlympicOperations(s,PlayerSide.USSR,mode),Is.EqualTo(RuleError.None));
            var forgedOps=new OperationsContext(s.Operations.Id,s.Operations.Actor,99,s.Operations.Reachable,mode,s.Operations.Costs);var forged=new GameState(s.Map,s.Countries,forgedOps,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,cards:s.Cards,randomState:s.RandomState,practice:s.Practice);
            Assert.That(ActionPracticeRules.CanContinueOlympicOperations(forged,PlayerSide.USSR,mode),Is.EqualTo(RuleError.StaleContext));
            s=Spend(s,"japan");Assert.That(s.Operations.Budget,Is.EqualTo(4));Assert.That(s.Operations.RegionalBudget.EligibleBonuses,Is.Empty);s=Spend(s,"thailand");Assert.That(s.Operations.Budget,Is.EqualTo(4));
            var done=Use(s,mode==OperationsMode.Influence?CardActionKind.FinishInfluence:CardActionKind.FinishRealignment);Assert.That(done.Accepted,Is.True,done.Error.ToString());Assert.That(done.NextState.Practice.Olympic,Is.Null);
        }
        [Test] public void Purge_and_regional_bonus_combine_before_floor_in_actual_one_OPS_action()
        {
            var a=Play(Ready());Assert.That(a.Accepted,Is.True);var s=PlayTurnOpsEventTests.Give(a.NextState,PlayerSide.US,"card.red_scare_purge");s=PlayTurnOpsEventTests.Play(s,"card.red_scare_purge").NextState;s=PlayTurnOpsEventTests.Give(s,PlayerSide.USSR,"card.blockade");
            var r=Use(s,CardActionKind.Influence,"blockade");Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.NextState.Operations.Budget,Is.EqualTo(1));Assert.That(InfluenceRules.PlanPlacement(r.NextState,r.NextState.Operations,new[]{C("vietnam")}).Accepted,Is.True);
            Assert.That(ActionPracticeRules.CanPlay(s,PlayerSide.USSR,new CardId("card.blockade"),CardActionKind.Space),Is.EqualTo(RuleError.SpaceAttemptUnavailable));
        }
        [Test] public void Regional_registration_rejects_wrong_region_country_alias_null_and_duplicate()
        {
            var cards=CardCatalogLoaderTests.Load();var flat=PlayTurnOpsEventTests.Effects(cards);var real=VietnamOpsFoundationTests.Load();Assert.Throws<ArgumentException>(()=>flat.WithVietnam(null));Assert.Throws<ArgumentException>(()=>flat.WithVietnam(TurnOpsEffectTests.Effects()[0]));
            foreach(var fault in new[]{"region","country","identity"})
            {var card=fault=="identity"?new CardId("card.red_scare_purge"):Card;var effect=fault=="identity"?"effect.red_scare_purge":"effect.vietnam_revolts";var bad=new TurnOpsEffectDefinition(card,effect,new OpsRegionalBonus(effect,fault=="region"?"region.asia":"region.southeast_asia",1),new AddFixedInfluenceEffect(card.Value,effect,PlayerSide.USSR,C(fault=="country"?"thailand":"vietnam"),2));Assert.Throws<ArgumentException>(()=>flat.WithVietnam(bad));}
            Assert.That(flat.WithVietnam(real).Effects.Count,Is.EqualTo(3));Assert.Throws<ArgumentException>(()=>flat.WithVietnam(real).WithVietnam(real));Assert.That(flat.Effects.Count,Is.EqualTo(2));
        }
        [Test] public void Turn_end_expires_regional_instance_after_real_event()
        {
            var s=PlayScoringTests.Ready(Card.Value,configure:d=>{
                var b=Bind(d);return new ActionPracticeDefinition(b.Cards,b.Ops,b.Space,b.Calendar,b.Risk,b.Scoring,b.InfluenceEvents,b.DefconVpEvents,b.TurnOpsEvents,b.Realignment,b.China,turnEndEnabled:true,olympic:b.Olympic);});
            var d=s.Practice.Definition;var hands=new System.Collections.Generic.Dictionary<PlayerSide,System.Collections.Generic.IReadOnlyList<CardId>>{{PlayerSide.US,Array.Empty<CardId>()},{PlayerSide.USSR,new[]{Card}}};
            var zones=new CardZones(d.Cards,d.Cards.Cards.Where(c=>c.Kind!=CardKind.China && !c.Id.Equals(Card)).Select(c=>c.Id),hands,Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),Array.Empty<CardId>(),s.Cards.China,new[]{GameEra.Early,GameEra.Mid,GameEra.Late});s=PlayInfluenceEventTests.Copy(s,cards:zones);s=Play(s).NextState;Assert.That(s,Is.Not.Null);
            while(s.Practice.CompletedActions<12)s=ChinaCardPlayTests.AdvanceOrdinary(s);
            var end=TurnEndPlayTests.End(s);Assert.That(end.Accepted,Is.True,end.Error.ToString());Assert.That(end.NextState.Practice.OpsEffects.Instances,Is.Empty);Assert.That(end.NextState.Practice.TurnEnd.ExpiredOpsEffects,Is.EqualTo(1));Assert.That(s.Practice.OpsEffects.Instances.Count,Is.EqualTo(1));
        }
    }
}
