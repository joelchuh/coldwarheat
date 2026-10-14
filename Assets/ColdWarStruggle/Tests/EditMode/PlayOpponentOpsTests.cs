using System;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayOpponentOpsTests
    {
        internal static CountryId C(string s)=>new CountryId("country."+s);
        internal static CardActionTransition Start(GameState s,bool first)=>ActionPracticeRules.Plan(s,s.Practice.Actor,first?CardActionKind.OpponentEventFirst:CardActionKind.OpponentOpsFirst,"begin",s.Cards.GetHand(s.Practice.Actor).Last());
        internal static CardActionTransition Use(GameState s,CardActionKind mode,CountryId? target=null)=>ActionPracticeRules.Plan(s,s.Practice.Actor,mode,"use",target:target);
        internal static GameState Ready(string card="card.duck_and_cover",int level=5)
        { var s=PlayRealignmentTests.Ready(card,level);var b=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);b[C("angola")]=new CountryState(3,3);return PlayScoringTests.Replace(s,b,Pcg32Random.FromSeed(42,54).Capture()); }
        [TestCase(true)][TestCase(false)] public void Sequence_defers_mode_choice_and_locks_it_once_current_ops_begins(bool first)
        {
            var q=OpponentOpsSequence.Create(new CardId("card.duck_and_cover"),PlayerSide.USSR,first?OpponentEventOrder.EventFirst:OpponentEventOrder.OperationsFirst,null);
            Assert.That(q.Mode,Is.Null);
            if(first){Assert.Throws<InvalidOperationException>(()=>q.SelectMode(OperationsMode.Coup));q=q.Advance(CardResolutionStep.Event);}
            Assert.Throws<InvalidOperationException>(()=>q.Advance(CardResolutionStep.Operations));Assert.Throws<ArgumentOutOfRangeException>(()=>q.SelectMode((OperationsMode)99));
            var selected=q.SelectMode(OperationsMode.Realignment);Assert.That(q.Mode,Is.Null);Assert.That(selected.Mode,Is.EqualTo(OperationsMode.Realignment));Assert.Throws<InvalidOperationException>(()=>selected.SelectMode(OperationsMode.Coup));
            var next=selected.Advance(CardResolutionStep.Operations);Assert.That(next.Mode,Is.EqualTo(OperationsMode.Realignment));if(!first)next=next.Advance(CardResolutionStep.Event);Assert.That(next.CurrentStep,Is.EqualTo(CardResolutionStep.Complete));
        }
        [TestCase(true)][TestCase(false)] public void Generic_influence_defers_budget_then_runs_legacy_placement_and_one_root(bool first)
        {
            var s=Ready("card.containment");var a=Start(s,first);Assert.That(a.Accepted,Is.True,a.Error.ToString());var n=a.NextState;Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChoosingOperations));Assert.That(n.Practice.OpponentOps.Mode,Is.Null);Assert.That(n.Operations.Budget,Is.Zero);Assert.That(InfluenceRules.PlanPlacement(n,n.Operations,new[]{C("nigeria")}).Accepted,Is.False);Pcg32RandomTests.Same(s.RandomState,n.RandomState);
            var chosen=Use(n,CardActionKind.UseOpponentInfluence);Assert.That(chosen.Accepted,Is.True);n=chosen.NextState;Assert.That(n.Practice.OpponentOps.Mode,Is.EqualTo(OperationsMode.Influence));Assert.That(n.Operations.Budget,Is.EqualTo(3));var place=InfluenceRules.PlanPlacement(n,n.Operations,new[]{C("nigeria")});Assert.That(place.Accepted,Is.True);
            var finish=ActionPracticeRules.Plan(place.NextState,n.Practice.Actor,CardActionKind.FinishInfluence,"finish");Assert.That(finish.Accepted,Is.True);Assert.That(finish.NextState.Practice.CompletedActions,Is.EqualTo(1));Assert.That(finish.NextState.Cards.RemovedCards,Does.Contain(new CardId("card.containment")));Assert.That(finish.NextState.Practice.OpsEffects.Instances.Single().AffectedSide,Is.EqualTo(PlayerSide.US));
        }
        [TestCase(true)][TestCase(false)] public void Realignment_holds_root_through_opponent_choice_and_releases_star_once(bool first)
        {
            var s=PlayOpponentInfluenceTests.ReadyUS("card.suez_crisis");var b=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);b[C("angola")]=new CountryState(3,3);s=PlayScoringTests.Replace(s,b,Pcg32Random.FromSeed(42,54).Capture());
            var a=ActionPracticeRules.Plan(s,PlayerSide.US,first?CardActionKind.OpponentEventFirst:CardActionKind.OpponentOpsFirst,"start",new CardId("card.suez_crisis"));Assert.That(a.Accepted,Is.True);var n=a.NextState;
            if(first){Assert.That(n.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.USSR));n=PlayOpponentInfluenceTests.Resolve(n).NextState;}
            Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.ChoosingOperations));Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.US));var use=Use(n,CardActionKind.UseOpponentRealignment);Assert.That(use.Accepted,Is.True);n=use.NextState;Assert.That(n.Operations.Mode,Is.EqualTo(OperationsMode.Realignment));Assert.That(n.Operations.Budget,Is.EqualTo(3));Assert.That(n.Practice.OpponentOps.Mode,Is.EqualTo(OperationsMode.Realignment));
            var roll=ActionPracticeRules.Plan(n,PlayerSide.US,CardActionKind.RollRealignment,"roll",target:C("angola"));Assert.That(roll.Accepted,Is.True);Assert.That(roll.Realignment.UsRoll,Is.EqualTo(4));Assert.That(roll.Realignment.UssrRoll,Is.EqualTo(4));Assert.That(roll.NextState.SpentOps,Is.EqualTo(1));Assert.That(roll.NextState.Practice.Risk.Military.UsEarned,Is.Zero);
            var end=ActionPracticeRules.Plan(roll.NextState,PlayerSide.US,CardActionKind.FinishRealignment,"finish");Assert.That(end.Accepted,Is.True);n=end.NextState;
            if(!first){Assert.That(n.Practice.CompletedActions,Is.EqualTo(1));Assert.That(n.InfluenceEvent.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(n.Cards.ResolvingCards.Count,Is.EqualTo(1));n=PlayOpponentInfluenceTests.Resolve(n).NextState;}
            Assert.That(n.Practice.CompletedActions,Is.EqualTo(2));Assert.That(n.Practice.OpponentOps,Is.Null);Assert.That(n.Cards.ResolvingCards,Is.Empty);Assert.That(n.Cards.RemovedCards.Count(x=>x.Equals(new CardId("card.suez_crisis"))),Is.EqualTo(1));Assert.That(n.RandomState.DrawCount,Is.EqualTo(2));
        }
        [TestCase(true)][TestCase(false)] public void Coup_and_event_complete_one_root_with_actual_actor_military_and_star_disposition(bool first)
        {
            var s=Ready("card.containment");var start=Start(s,first);Assert.That(start.Accepted,Is.True);var r=Use(start.NextState,CardActionKind.UseOpponentCoup,C("angola"));Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.CoupReceipt.Calculation.Roll,Is.EqualTo(4));Assert.That(r.CoupReceipt.Calculation.Strength,Is.EqualTo(3));Assert.That(r.CoupReceipt.MilitaryCredit.Amount,Is.EqualTo(3));Assert.That(r.NextState.Practice.Risk.Military.UssrEarned,Is.EqualTo(3));Assert.That(r.NextState.Practice.Risk.Defcon.Level,Is.EqualTo(4));Assert.That(r.NextState.Practice.CompletedActions,Is.EqualTo(1));Assert.That(r.NextState.Practice.OpsEffects.Instances.Single().AffectedSide,Is.EqualTo(PlayerSide.US));Assert.That(r.NextState.Cards.RemovedCards,Does.Contain(new CardId("card.containment")));Assert.That(r.NextState.Cards.ResolvingCards,Is.Empty);Assert.That(r.NextState.RandomState.DrawCount,Is.EqualTo(1));if(!first)Assert.That(r.ActivatedOpsEffect,Is.Not.Null);
        }
        [Test] public void Event_first_defcon_change_restricts_targets_but_leaves_other_ops_modes_available()
        {
            var s=Ready(level:5);var b=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);b[C("france")]=new CountryState(3,3);s=PlayScoringTests.Replace(s,b);var a=Start(s,true);Assert.That(a.Accepted,Is.True);var n=a.NextState;Assert.That(n.Practice.Risk.Defcon.Level,Is.EqualTo(4));Assert.That(ActionPracticeRules.CheckCoupTarget(n,PlayerSide.USSR,new CardId("card.duck_and_cover"),C("france")),Is.EqualTo(RuleError.IllegalTarget));Assert.That(Use(n,CardActionKind.UseOpponentCoup,C("france")).Error,Is.EqualTo(RuleError.IllegalTarget));var r=Use(n,CardActionKind.UseOpponentRealignment);Assert.That(r.Accepted,Is.True);Assert.That(ActionPracticeRules.CheckRealignmentTarget(r.NextState,PlayerSide.USSR,C("france")),Is.EqualTo(RuleError.IllegalTarget));Assert.That(ActionPracticeRules.CheckRealignmentTarget(r.NextState,PlayerSide.USSR,C("angola")),Is.EqualTo(RuleError.None));
        }
        [TestCase(true)][TestCase(false)] public void Nuclear_war_in_coup_or_following_event_stops_root_and_keeps_resolving_card(bool first)
        {
            var s=Ready(level:3);var a=Start(s,first);Assert.That(a.Accepted,Is.True);var r=Use(a.NextState,CardActionKind.UseOpponentCoup,C("angola"));Assert.That(r.Accepted,Is.True,r.Error.ToString());Assert.That(r.NextState.Ended,Is.True);Assert.That(r.NextState.Practice.Risk.Result.Winner,Is.EqualTo(PlayerSide.US));Assert.That(r.NextState.Practice.CompletedActions,Is.Zero);Assert.That(r.NextState.Cards.ResolvingCards,Is.EqualTo(new[]{new CardId("card.duck_and_cover")}));Assert.That(r.NextState.Cards.DiscardPile,Is.Empty);Assert.That(r.NextState.Practice.Risk.Military.UssrEarned,Is.EqualTo(3));Assert.That(r.NextState.Practice.VpForUS,Is.EqualTo(first?3:0));Assert.That(r.NextState.RandomState.DrawCount,Is.EqualTo(1));Assert.That(r.CoupReceipt,Is.Not.Null);if(!first)Assert.That(r.DefconVpEvent.Decision.Outcome,Is.EqualTo(VictoryOutcome.Finished));Assert.That(Use(r.NextState,CardActionKind.UseOpponentInfluence).Error,Is.EqualTo(RuleError.GameAlreadyEnded));
        }
        [Test] public void Coup_nuclear_before_event_never_fires_or_credits_event()
        {
            var a=Start(Ready(level:2),false);var r=Use(a.NextState,CardActionKind.UseOpponentCoup,C("angola"));Assert.That(r.Accepted,Is.True);Assert.That(r.NextState.Ended,Is.True);Assert.That(r.DefconVpEvent,Is.Null);Assert.That(r.NextState.Practice.OpponentOps.CompletedSteps,Is.Zero);Assert.That(r.NextState.Practice.OpponentOps.EventOccurred,Is.False);Assert.That(r.NextState.Practice.VpForUS,Is.Zero);
        }
        [TestCase(true)][TestCase(false)] public void Overflow_in_coup_followup_does_not_publish_dice_military_or_event(bool version)
        {
            var n=Start(Ready("card.containment"),false).NextState;n=PlayScoringTests.Replace(n,version:version?long.MaxValue-2:(long?)null,sequence:version?null:(long?)long.MaxValue-2);var r=Use(n,CardActionKind.UseOpponentCoup,C("angola"));Assert.That(r.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(r.NextState,Is.Null);Assert.That(n.Practice.Risk.Military.UssrEarned,Is.Zero);Assert.That(n.Practice.OpsEffects.Instances,Is.Empty);Assert.That(n.RandomState.DrawCount,Is.Zero);Assert.That(n.Practice.OpponentOps.Mode,Is.Null);Assert.That(n.GetInfluence(C("angola"),PlayerSide.US),Is.EqualTo(3));
        }
        [Test] public void Invalid_mode_payload_stage_and_identity_do_not_change_card_or_root()
        {
            var s=Ready();Assert.That(Use(s,CardActionKind.UseOpponentInfluence).Error,Is.EqualTo(RuleError.ActionNotReady));var n=Start(s,false).NextState;Assert.That(ActionPracticeRules.Plan(n,PlayerSide.US,CardActionKind.UseOpponentInfluence,"wrong").Error,Is.EqualTo(RuleError.WrongActor));Assert.That(ActionPracticeRules.Plan(n,PlayerSide.USSR,CardActionKind.UseOpponentInfluence,"card",new CardId("card.duck_and_cover")).Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(Use(n,CardActionKind.UseOpponentInfluence,C("angola")).Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(Use(n,CardActionKind.UseOpponentCoup).Error,Is.EqualTo(RuleError.InvalidCommand));var chosen=Use(n,CardActionKind.UseOpponentRealignment);Assert.That(chosen.Accepted,Is.True);Assert.That(Use(chosen.NextState,CardActionKind.UseOpponentInfluence).Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(ActionPracticeRules.Plan(chosen.NextState,PlayerSide.USSR,CardActionKind.FinishInfluence,"bad-end").Error,Is.EqualTo(RuleError.ActionNotReady));
        }

        [TestCase(true)][TestCase(false)] public void Realignment_finish_overflow_preserves_previously_accepted_roll(bool version)
        {
            var pending=Start(Ready("card.containment"),false).NextState;var chosen=Use(pending,CardActionKind.UseOpponentRealignment);Assert.That(chosen.Accepted,Is.True);var roll=ActionPracticeRules.Plan(chosen.NextState,PlayerSide.USSR,CardActionKind.RollRealignment,"roll",target:C("angola"));Assert.That(roll.Accepted,Is.True);var n=PlayScoringTests.Replace(roll.NextState,version:version?long.MaxValue-1:(long?)null,sequence:version?null:(long?)long.MaxValue-1);var done=ActionPracticeRules.Plan(n,PlayerSide.USSR,CardActionKind.FinishRealignment,"finish");Assert.That(done.Error,Is.EqualTo(RuleError.NumericOverflow));Assert.That(done.NextState,Is.Null);Assert.That(n.SpentOps,Is.EqualTo(1));Assert.That(n.RandomState.DrawCount,Is.EqualTo(2));Assert.That(n.Practice.OpponentOps.CompletedSteps,Is.Zero);Assert.That(n.Practice.OpsEffects.Instances,Is.Empty);Assert.That(n.Cards.ResolvingCards.Count,Is.EqualTo(1));
        }
        [TestCase(2,0)][TestCase(5,19)] public void Event_first_terminal_never_opens_pending_ops_or_draws_rng(int level,int vp)
        {
            var s=PlayDefconVpEventTests.Ready(level:level,vp:vp);var r=Start(s,true);Assert.That(r.Accepted,Is.True);Assert.That(r.NextState.Ended,Is.True);Assert.That(r.NextState.Practice.Stage,Is.EqualTo(ActionPracticeStage.Complete));Assert.That(r.NextState.Practice.OpponentOps.Mode,Is.Null);Assert.That(r.NextState.Practice.CompletedActions,Is.Zero);Pcg32RandomTests.Same(s.RandomState,r.NextState.RandomState);Assert.That(Use(r.NextState,CardActionKind.UseOpponentRealignment).Error,Is.EqualTo(RuleError.GameAlreadyEnded));
        }
        [Test] public void Coup_then_vp_win_applies_coup_but_skips_root_completion()
        {
            var s=PlayDefconVpEventTests.Ready(vp:19);var b=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);b[C("angola")]=new CountryState(3,3);s=PlayScoringTests.Replace(s,b);var start=Start(s,false);Assert.That(start.Accepted,Is.True);var r=Use(start.NextState,CardActionKind.UseOpponentCoup,C("angola"));Assert.That(r.Accepted,Is.True);Assert.That(r.CoupReceipt,Is.Not.Null);Assert.That(r.DefconVpEvent.Decision.Outcome,Is.EqualTo(VictoryOutcome.Finished));Assert.That(r.NextState.Practice.VpForUS,Is.EqualTo(21));Assert.That(r.NextState.Practice.Risk.Result.Checkpoint,Is.EqualTo(VictoryCheckpoint.VpBatchClosed));Assert.That(r.NextState.Practice.CompletedActions,Is.Zero);Assert.That(r.NextState.Cards.ResolvingCards.Count,Is.EqualTo(1));Assert.That(r.NextState.Practice.Risk.Military.UssrEarned,Is.EqualTo(3));
        }
        [TestCase(true)][TestCase(false)] public void Unimplemented_opponent_prerequisite_event_is_rejected_for_generic_order(bool first)
        {var s=Ready("card.nato");var r=Start(s,first);Assert.That(r.Error,Is.EqualTo(RuleError.UnsupportedCardAction));Assert.That(r.NextState,Is.Null);Assert.That(s.Cards.ResolvingCards,Is.Empty);}
    }
}
