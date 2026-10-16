using System;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public sealed class OlympicOperationsTests
    {
        static CountryId C(string name)=>new CountryId("country."+name);
        internal static GameState Pending(PlayerSide side=PlayerSide.USSR,int level=5,bool purge=false,bool containment=false)
        {
            var s=OlympicPlayTests.Ready(side,0,level,6,1,6,1);var board=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);
            board[C("angola")]=new CountryState(20,20);board[C("zimbabwe")]=new CountryState(0,0);board[C("botswana")]=new CountryState(0,0);
            board[C("france")]=new CountryState(3,0);board[C("italy")]=new CountryState(0,1);board[C("zaire")]=new CountryState(0,0);board[C("southeast_african_states")]=new CountryState(0,0);s=PlayScoringTests.Replace(s,board);
            if(containment)
            {
                s=PlayTurnOpsEventTests.Give(s,s.Practice.Actor,"card.containment");s=PlayTurnOpsEventTests.Play(s,"card.containment").NextState;
            }
            if(purge)
            {
                if(s.Practice.Actor==side){s=PlayTurnOpsEventTests.Give(s,side,"card.captured_nazi_scientist");s=SpaceEventPlayTests.Pass(s,"card.captured_nazi_scientist");}
                s=PlayTurnOpsEventTests.Give(s,s.Practice.Actor,"card.red_scare_purge");s=PlayTurnOpsEventTests.Play(s,"card.red_scare_purge").NextState;
            }
            if(s.Practice.Actor!=side){s=PlayTurnOpsEventTests.Give(s,s.Practice.Actor,"card.captured_nazi_scientist");s=SpaceEventPlayTests.Pass(s,"card.captured_nazi_scientist");}
            var begin=OlympicPlayTests.Play(s);Assert.That(begin.Accepted,Is.True,begin.Error.ToString());
            var boycott=OlympicPlayTests.Respond(begin.NextState,false);Assert.That(boycott.Accepted,Is.True,boycott.Error.ToString());return boycott.NextState;
        }
        internal static CardActionTransition Use(GameState s,CardActionKind kind,CountryId? target=null)
            =>ActionPracticeRules.Plan(s,s.Practice.Actor,kind,"olympic-ops",target:target);
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)]
        public void Influence_uses_event_four_not_printed_two_and_consumes_no_second_card(PlayerSide side)
        {
            var s=Pending(side);var quote=ActionPracticeRules.QuoteOlympicOperations(s,side,OpsValuePurpose.Influence);
            Assert.That(quote.BaseValue,Is.EqualTo(4));Assert.That(quote.GeneralValue,Is.EqualTo(4));Assert.That(quote.Card,Is.Null);
            Assert.That(ActionPracticeRules.QuoteCard(s,side,OlympicPlayTests.Card,OpsValuePurpose.Influence).GeneralValue,Is.EqualTo(2));
            var r=Use(s,CardActionKind.UseOlympicInfluence);Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(n.Operations.Budget,Is.EqualTo(4));Assert.That(n.Operations.RegionalBudget,Is.Null);Assert.That(n.Practice.Olympic.Mode,Is.EqualTo(OperationsMode.Influence));
            Assert.That(n.Cards,Is.SameAs(s.Cards));Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));Assert.That(s.Practice.Olympic.Mode,Is.Null);
            var placed=InfluenceRules.PlanPlacement(n,n.Operations,new[]{C("angola"),C("angola"),C("angola"),C("angola")});Assert.That(placed.Accepted,Is.True,placed.Error.ToString());
            Assert.That(placed.NextState.SpentOps,Is.EqualTo(4));Assert.That(InfluenceRules.PlanPlacement(placed.NextState,placed.NextState.Operations,new[]{C("angola")}).Error,Is.EqualTo(RuleError.InsufficientBudget));
            var done=Use(placed.NextState,CardActionKind.FinishInfluence);Assert.That(done.Accepted,Is.True,done.Error.ToString());AssertFinished(s,done.NextState);
            Assert.That(done.OlympicResult.Outcome,Is.EqualTo(OlympicPracticeOutcome.OperationsCompleted));Pcg32RandomTests.Same(s.RandomState,done.NextState.RandomState);
        }
        static void AssertFinished(GameState before,GameState after)
        {
            Assert.That(after.Practice.Olympic,Is.Null);Assert.That(after.Practice.CompletedActions,Is.EqualTo(before.Practice.CompletedActions+1));
            Assert.That(after.Cards.ResolvingCards,Is.Empty);Assert.That(after.Cards.DiscardPile,Does.Contain(OlympicPlayTests.Card));Assert.That(after.Cards.RemovedCards,Has.No.Member(OlympicPlayTests.Card));
            Assert.That(after.Cards.GetHand(PlayerSide.US),Is.EqualTo(before.Cards.GetHand(PlayerSide.US)));Assert.That(after.Cards.GetHand(PlayerSide.USSR),Is.EqualTo(before.Cards.GetHand(PlayerSide.USSR)));
        }
        [TestCase(PlayerSide.US,true,false,3)][TestCase(PlayerSide.USSR,true,false,3)]
        [TestCase(PlayerSide.US,false,true,4)][TestCase(PlayerSide.USSR,false,true,4)][TestCase(PlayerSide.US,true,true,4)]
        public void Flat_effects_use_actual_sponsor_and_normal_cap(PlayerSide side,bool purge,bool containment,int expected)
        {
            var s=Pending(side,purge:purge,containment:containment);
            foreach(var purpose in new[]{OpsValuePurpose.Influence,OpsValuePurpose.Realignment,OpsValuePurpose.Coup})Assert.That(ActionPracticeRules.QuoteOlympicOperations(s,side,purpose).GeneralValue,Is.EqualTo(expected));
            var n=Use(s,CardActionKind.UseOlympicInfluence).NextState;Assert.That(n.Operations.Budget,Is.EqualTo(expected));
        }
        [Test] public void Fixed_root_reachability_and_current_control_cost_are_preserved()
        {
            var s=Use(Pending(),CardActionKind.UseOlympicInfluence).NextState;
            Assert.That(s.Operations.Reachable.Contains(C("botswana")),Is.True);Assert.That(s.Operations.Reachable.Contains(C("zimbabwe")),Is.False);
            var z=InfluenceRules.PlanPlacement(s,s.Operations,new[]{C("botswana")});Assert.That(z.Accepted,Is.True);
            Assert.That(InfluenceRules.PlanPlacement(z.NextState,z.NextState.Operations,new[]{C("zimbabwe")}).Error,Is.EqualTo(RuleError.IllegalTarget));
            var f=InfluenceRules.PlanPlacement(s,s.Operations,new[]{C("france"),C("france"),C("france")});Assert.That(f.Accepted,Is.True,f.Error.ToString());Assert.That(f.Costs,Is.EqualTo(new[]{2,1,1}));
        }
        [TestCase(PlayerSide.US)][TestCase(PlayerSide.USSR)]
        public void Realignment_reuses_rolls_budget_and_finishes_one_parent(PlayerSide side)
        {
            var s=Pending(side);var choose=Use(s,CardActionKind.UseOlympicRealignment);Assert.That(choose.Accepted,Is.True,choose.Error.ToString());var n=choose.NextState;
            Assert.That(n.Operations.Mode,Is.EqualTo(OperationsMode.Realignment));Assert.That(n.Operations.Budget,Is.EqualTo(4));Assert.That(n.Operations.RegionalBudget,Is.Null);
            for(int i=0;i<4;i++){var r=Use(n,CardActionKind.RollRealignment,C("angola"));Assert.That(r.Accepted,Is.True,r.Error.ToString());n=r.NextState;Assert.That(r.Realignment,Is.Not.Null);}
            Assert.That(n.RandomState.DrawCount,Is.EqualTo(s.RandomState.DrawCount+8));Assert.That(n.SpentOps,Is.EqualTo(4));Assert.That(Use(n,CardActionKind.RollRealignment,C("angola")).Error,Is.EqualTo(RuleError.InsufficientBudget));
            var done=Use(n,CardActionKind.FinishRealignment);Assert.That(done.Accepted,Is.True,done.Error.ToString());AssertFinished(s,done.NextState);Assert.That(done.NextState.Practice.Risk.Military,Is.SameAs(s.Practice.Risk.Military));
        }
        [TestCase(PlayerSide.US,5,false)][TestCase(PlayerSide.USSR,5,false)][TestCase(PlayerSide.US,3,true)][TestCase(PlayerSide.USSR,3,true)]
        public void Coup_uses_four_and_credits_military_with_nuclear_root_interrupt(PlayerSide side,int level,bool terminal)
        {
            var s=Pending(side,level);Assert.That(ActionPracticeRules.QuoteCoupStrength(s,side,OlympicPlayTests.Card,C("angola")),Is.EqualTo(4));
            var r=Use(s,CardActionKind.UseOlympicCoup,C("angola"));Assert.That(r.Accepted,Is.True,r.Error.ToString());var n=r.NextState;
            Assert.That(r.CoupReceipt,Is.Not.Null);Assert.That(n.Practice.Risk.Defcon.Level,Is.EqualTo(level-2));Assert.That(n.Ended,Is.EqualTo(terminal));Assert.That(n.RandomState.DrawCount,Is.EqualTo(s.RandomState.DrawCount+1));
            Assert.That(side==PlayerSide.US?n.Practice.Risk.Military.UsEarned:n.Practice.Risk.Military.UssrEarned,Is.EqualTo(4));
            if(terminal){Assert.That(n.Cards.ResolvingCards.Single(),Is.EqualTo(OlympicPlayTests.Card));Assert.That(n.Practice.CompletedActions,Is.EqualTo(s.Practice.CompletedActions));Assert.That(n.Practice.Risk.Result.Winner,Is.Not.EqualTo(side));Assert.That(r.OlympicResult.Outcome,Is.EqualTo(OlympicPracticeOutcome.Interrupted));}
            else AssertFinished(s,n);
            Assert.That(s.Practice.Risk.CoupReceipts,Is.Empty);
        }
        [Test] public void Boycott_defcon_restricts_coup_and_realign_targets()
        {
            var s=Pending(level:5);Assert.That(Use(s,CardActionKind.UseOlympicCoup,C("france")).Error,Is.EqualTo(RuleError.IllegalTarget));
            var n=Use(s,CardActionKind.UseOlympicRealignment).NextState;Assert.That(Use(n,CardActionKind.RollRealignment,C("france")).Error,Is.EqualTo(RuleError.IllegalTarget));Assert.That(n.SpentOps,Is.Zero);
        }
        [Test] public void Decline_keeps_defcon_change_and_finishes_without_rng_or_military()
        {
            var s=Pending();var r=Use(s,CardActionKind.DeclineOlympicOperations);Assert.That(r.Accepted,Is.True);AssertFinished(s,r.NextState);
            Assert.That(r.NextState.Practice.Risk.Defcon.Level,Is.EqualTo(4));Assert.That(r.NextState.Practice.Risk.Military,Is.SameAs(s.Practice.Risk.Military));Pcg32RandomTests.Same(s.RandomState,r.NextState.RandomState);
            Assert.That(r.OlympicResult.Outcome,Is.EqualTo(OlympicPracticeOutcome.OperationsDeclined));Assert.That(Use(r.NextState,CardActionKind.DeclineOlympicOperations).Accepted,Is.False);
        }
        [TestCase(CardActionKind.UseOlympicInfluence)][TestCase(CardActionKind.UseOlympicRealignment)]
        public void Once_chosen_mode_cannot_change_or_refill_and_other_card_actions_stay_blocked(CardActionKind kind)
        {
            var s=Use(Pending(),kind).NextState;
            foreach(var k in new[]{CardActionKind.UseOlympicInfluence,CardActionKind.UseOlympicRealignment,CardActionKind.UseOlympicCoup,CardActionKind.DeclineOlympicOperations})Assert.That(Use(s,k,k==CardActionKind.UseOlympicCoup?C("angola"):(CountryId?)null).Accepted,Is.False);
            Assert.That(ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.Space,"space",s.Cards.GetHand(s.Practice.Actor).First()).Accepted,Is.False);
            Assert.That(Use(s,kind==CardActionKind.UseOlympicInfluence?CardActionKind.FinishRealignment:CardActionKind.FinishInfluence).Accepted,Is.False);
            Assert.That(ActionPracticeRules.PlanOlympicContinuation(s).Accepted,Is.False);
        }
        [TestCase("side")][TestCase("card")][TestCase("target")][TestCase("id")][TestCase("coup-target")]
        public void Bad_payload_never_publishes_a_draft(string fault)
        {
            var s=Pending();var r=ActionPracticeRules.Plan(s,fault=="side"?PlayerSide.US:PlayerSide.USSR,fault=="coup-target"?CardActionKind.UseOlympicCoup:CardActionKind.UseOlympicInfluence,fault=="id"?" ":"use",fault=="card"?OlympicPlayTests.Card:(CardId?)null,fault=="target"?C("angola"):(CountryId?)null);
            Assert.That(r.Accepted,Is.False);Assert.That(r.NextState,Is.Null);Assert.That(s.Practice.Olympic.Mode,Is.Null);Assert.That(s.Operations.Budget,Is.Zero);
        }
        [TestCase("game")][TestCase("version")][TestCase("random")][TestCase("source")]
        public void Foreign_parent_context_cannot_claim_event_ops(string fault)
        {
            var s=Pending();
            var forged=new GameState(s.Map,s.Countries,s.Operations,fault=="game"?"foreign":s.GameId,fault=="version"?s.Version+1:s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,cards:fault=="source"?OlympicPlayTests.Ready().Cards:s.Cards,randomState:fault=="random"?OlympicPlayTests.Dice(1,1):s.RandomState,practice:s.Practice);
            Assert.That(ActionPracticeRules.CanChooseOlympicOperations(forged,s.Practice.Actor,OperationsMode.Influence),Is.EqualTo(RuleError.StaleContext));
        }
        [Test] public void Forged_operations_cannot_bypass_direct_influence_entry()
        {
            var s=Use(Pending(),CardActionKind.UseOlympicInfluence).NextState;var forgedOps=OperationsContext.FromRoundStart("foreign",s.Practice.Actor,99,s.Map,s.Countries,s.Operations.Costs);
            var forged=new GameState(s.Map,s.Countries,forgedOps,s.GameId,s.Version,s.SpentOps,s.EventSequence,turn:s.Turn,cards:s.Cards,randomState:s.RandomState,practice:s.Practice);
            Assert.That(InfluenceRules.PlanPlacement(forged,forgedOps,new[]{C("angola")}).Error,Is.EqualTo(RuleError.StaleContext));Assert.That(Use(forged,CardActionKind.FinishInfluence).Error,Is.EqualTo(RuleError.StaleContext));
            Assert.That(ActionPracticeRules.CanContinueOlympicOperations(s,PlayerSide.US,OperationsMode.Influence),Is.EqualTo(RuleError.WrongActor));
        }
        [Test] public void Context_and_quote_require_pending_boycott_and_map_mode()
        {
            Assert.That(ActionPracticeRules.CanChooseOlympicOperations(null,PlayerSide.US,OperationsMode.Influence),Is.EqualTo(RuleError.InvalidCommand));
            var ready=OlympicPlayTests.Ready();Assert.That(Use(ready,CardActionKind.UseOlympicInfluence).Accepted,Is.False);
            var awaiting=OlympicPlayTests.Play(ready).NextState;Assert.That(Use(awaiting,CardActionKind.UseOlympicInfluence).Accepted,Is.False);
            var s=Pending();Assert.That(ActionPracticeRules.CanChooseOlympicOperations(s,PlayerSide.USSR,(OperationsMode)99),Is.EqualTo(RuleError.InvalidCommand));
            Assert.Throws<ArgumentException>(()=>ActionPracticeRules.QuoteOlympicOperations(s,PlayerSide.USSR,OpsValuePurpose.Space));
        }
    }
}
