using System;
using System.Linq;
using System.Collections.Generic;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class PlayOpponentInfluenceTests
    {
        internal static CountryId C(string id)=>new CountryId("country."+id);
        internal static GameState ReadyUS(string card="card.nasser")
        {
            var s=PlayRealignmentTests.Ready();var a=ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.Influence,"skip-start",new CardId("card.olympic_games"));Assert.That(a.Accepted,Is.True);
            var done=ActionPracticeRules.Plan(a.NextState,PlayerSide.USSR,CardActionKind.FinishInfluence,"skip-end");Assert.That(done.Accepted,Is.True);return PlayTurnOpsEventTests.Give(done.NextState,PlayerSide.US,card);
        }
        internal static CardActionTransition Start(GameState s,string card, bool eventFirst)=>ActionPracticeRules.Plan(s,s.Practice.Actor,eventFirst?CardActionKind.EventThenInfluence:CardActionKind.InfluenceThenEvent,"opponent-start",new CardId(card));
        internal static CardActionTransition Finish(GameState s)=>ActionPracticeRules.Plan(s,s.Practice.Actor,CardActionKind.FinishInfluence,"opponent-finish");
        internal static InfluenceEventTransition Resolve(GameState s)
        {
            var e=s.InfluenceEvent;var q=InfluenceEventFlow.QueryDecision(s,e.Owner,out var error);Assert.That(error,Is.EqualTo(RuleError.None));InfluenceEventTransition raw;
            if(q.Kind==InfluenceDecisionKind.Allocation){var left=q.RequiredAmount;var a=new List<InfluenceAllocation>();foreach(var cap in q.Capacities){var n=Math.Min(cap.Amount,left);if(n>0)a.Add(new InfluenceAllocation(cap.Country,n));left-=n;}Assert.That(left,Is.Zero);raw=InfluenceEventFlow.ResolveAllocation(s,e.Owner,e.DecisionId,a);}
            else raw=InfluenceEventFlow.Resolve(s,e.Owner,e.DecisionId,q.LegalCountries.Take(q.RequiredTargets).ToArray());
            Assert.That(raw.Accepted,Is.True);return ActionPracticeRules.CompleteEventAction(raw);
        }
        [TestCase(true,3)][TestCase(false,2)] public void Nasser_order_changes_board_and_keeps_one_root_until_both_steps_finish(bool first,int finalUS)
        {
            var s=ReadyUS();var b=s.Countries.ToDictionary(x=>x.Key,x=>x.Value);b[C("egypt")]=new CountryState(4,0);s=PlayScoringTests.Replace(s,b);var a=Start(s,"card.nasser",first);Assert.That(a.Accepted,Is.True,a.Error.ToString());var n=a.NextState;
            Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(n.Practice.CompletedActions,Is.EqualTo(1));Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.PlacingInfluence));Assert.That(n.Practice.OpponentOps.CurrentStep,Is.EqualTo(CardResolutionStep.Operations));Assert.That(n.Cards.ResolvingCards,Is.EqualTo(new[]{new CardId("card.nasser")}));Assert.That(n.Cards.RemovedCards,Is.Empty);Assert.That(n.Operations.Budget,Is.EqualTo(1));Assert.That(n.GetInfluence(C("egypt"),PlayerSide.US),Is.EqualTo(first?2:4));
            var place=InfluenceRules.PlanPlacement(n,n.Operations,new[]{C("egypt")});Assert.That(place.Accepted,Is.True);var end=Finish(place.NextState);Assert.That(end.Accepted,Is.True,end.Error.ToString());Assert.That(end.NextState.GetInfluence(C("egypt"),PlayerSide.US),Is.EqualTo(finalUS));Assert.That(end.NextState.GetInfluence(C("egypt"),PlayerSide.USSR),Is.EqualTo(2));Assert.That(end.NextState.Practice.CompletedActions,Is.EqualTo(2));Assert.That(end.NextState.Practice.OpponentOps,Is.Null);Assert.That(end.NextState.Cards.ResolvingCards,Is.Empty);Assert.That(end.NextState.Cards.RemovedCards,Does.Contain(new CardId("card.nasser")));Pcg32RandomTests.Same(s.RandomState,end.NextState.RandomState);
        }
        [TestCase(true)][TestCase(false)] public void Choice_owned_by_opponent_does_not_advance_or_change_root_actor(bool first)
        {
            var s=ReadyUS("card.suez_crisis");var a=Start(s,"card.suez_crisis",first);Assert.That(a.Accepted,Is.True);var pending=first?a.NextState:Finish(a.NextState).NextState;var e=pending.InfluenceEvent;Assert.That(e.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(e.PhasingPlayer,Is.EqualTo(PlayerSide.US));Assert.That(pending.Practice.Actor,Is.EqualTo(PlayerSide.US));Assert.That(pending.Practice.CompletedActions,Is.EqualTo(1));Assert.That(e.DecisionSourceVersion,Is.EqualTo(pending.Version));Assert.That(pending.Cards.ResolvingCards.Count,Is.EqualTo(1));
            Assert.That(InfluenceEventFlow.ResolveAllocation(pending,PlayerSide.US,e.DecisionId,Array.Empty<InfluenceAllocation>()).Error,Is.EqualTo(RuleError.WrongActor));Assert.That(Finish(pending).Error,Is.EqualTo(RuleError.EventInProgress));var resolved=Resolve(pending);Assert.That(resolved.Accepted,Is.True,resolved.Error.ToString());var n=resolved.NextState;
            if(first){Assert.That(n.Practice.CompletedActions,Is.EqualTo(1));Assert.That(n.Practice.Stage,Is.EqualTo(ActionPracticeStage.PlacingInfluence));Assert.That(n.Cards.RemovedCards,Is.Empty);n=Finish(n).NextState;}
            Assert.That(n.Practice.CompletedActions,Is.EqualTo(2));Assert.That(n.Practice.Actor,Is.EqualTo(PlayerSide.USSR));Assert.That(n.Cards.ResolvingCards,Is.Empty);Assert.That(n.Cards.RemovedCards,Does.Contain(new CardId("card.suez_crisis")));Assert.That(n.InfluenceEvent,Is.Null);
        }
        [TestCase(true)][TestCase(false)] public void Containment_persists_but_opposing_USSR_budget_uses_actual_actor(bool first)
        {
            var s=PlayRealignmentTests.Ready("card.containment");var a=Start(s,"card.containment",first);Assert.That(a.Accepted,Is.True);var n=a.NextState;Assert.That(n.Operations.Budget,Is.EqualTo(3));Assert.That(n.Practice.OpponentOps.EventOccurred,Is.EqualTo(first));if(first)Assert.That(n.Practice.OpsEffects.Instances.Single().AffectedSide,Is.EqualTo(PlayerSide.US));var done=Finish(n);Assert.That(done.Accepted,Is.True);Assert.That(done.NextState.Practice.OpsEffects.Instances.Single().Controller,Is.EqualTo(PlayerSide.US));Assert.That(done.NextState.Cards.RemovedCards,Does.Contain(new CardId("card.containment")));Assert.That(done.NextState.Practice.CompletedActions,Is.EqualTo(1));
        }
        [TestCase("card.nato")][TestCase("card.olympic_games")][TestCase("card.fidel")][TestCase("card.europe_scoring")]
        public void Missing_handler_or_nonopponent_cards_are_never_silently_ops_only(string card)
        {
            var s=PlayRealignmentTests.Ready(card);Assert.That(Start(s,card,true).Error,Is.EqualTo(RuleError.UnsupportedCardAction));Assert.That(Start(s,card,false).Error,Is.EqualTo(RuleError.UnsupportedCardAction));Assert.That(s.Cards.ResolvingCards,Is.Empty);
        }
        [TestCase(true)][TestCase(false)] public void No_change_star_event_still_occurs_and_is_removed_only_after_both_steps(bool first)
        {
            var s=ReadyUS("card.suez_crisis");s=PlayScoringTests.Replace(s,RegionScoringTests.Empty(s.Map));var a=Start(s,"card.suez_crisis",first);Assert.That(a.Accepted,Is.True);Assert.That(a.NextState.Cards.ResolvingCards.Count,Is.EqualTo(1));var done=Finish(a.NextState);Assert.That(done.Accepted,Is.True);Assert.That(done.NextState.Cards.RemovedCards,Does.Contain(new CardId("card.suez_crisis")));Assert.That(done.NextState.Practice.CompletedActions,Is.EqualTo(2));
        }
        [Test] public void Payload_actor_and_wrong_stage_reject_without_moving_physical_card()
        {
            var s=PlayRealignmentTests.Ready("card.containment");var id=new CardId("card.containment");Assert.That(ActionPracticeRules.Plan(s,PlayerSide.US,CardActionKind.EventThenInfluence,"wrong",id).Error,Is.EqualTo(RuleError.WrongActor));Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.EventThenInfluence,"bad").Error,Is.EqualTo(RuleError.InvalidCommand));Assert.That(ActionPracticeRules.Plan(s,PlayerSide.USSR,CardActionKind.EventThenInfluence,"bad",id,C("iran")).Error,Is.EqualTo(RuleError.InvalidCommand));var a=Start(s,"card.containment",false);Assert.That(a.Accepted,Is.True);Assert.That(Start(a.NextState,"card.containment",true).Error,Is.EqualTo(RuleError.ActionNotReady));Assert.That(ActionPracticeRules.Plan(a.NextState,PlayerSide.USSR,CardActionKind.Realignment,"other",id).Error,Is.EqualTo(RuleError.ActionNotReady));
        }
    }
}
