using System;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        static CardActionTransition PlanEvent(GameState state,PlayerSide side,string actionId,CardId? card,CountryId? target)
        {
            if(string.IsNullOrWhiteSpace(actionId) || actionId.Length>128 || !card.HasValue || target.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var error=CanPlay(state,side,card.Value,CardActionKind.Event);if(error!=RuleError.None)return new CardActionTransition(error);
            try
            {
                var p=state.Practice;var effect=p.Definition.InfluenceEvents.Effects[card.Value];
                var pending=new ActionPracticeState(p.Definition,ActionPracticeStage.ResolvingEvent,p.CompletedActions,card,p.Space,p.VpForUS,p.Risk);
                // Intermediate immutable states never leave the host's commit boundary.
                var prepared=Commit(state,pending,Move(state,card.Value,true,true),state.RandomState).NextState
                    .WithInfluenceEvent(new InfluenceEventExecution("practice.event."+p.CompletedActions,effect,side,InfluenceEventPolicy.NoActiveInterference));
                var raw=InfluenceEventFlow.Begin(prepared,side);var completed=CompleteEventAction(raw);
                return completed.Accepted?new CardActionTransition(RuleError.None,completed.NextState,eventTransition:raw):new CardActionTransition(completed.Error);
            }
            catch(OverflowException) { return new CardActionTransition(RuleError.NumericOverflow); }
        }
        // Apply before publishing an event response: effect + card disposal + root advance form one transaction.
        // Standalone diagnostic events deliberately retain their existing resolved execution.
        public static InfluenceEventTransition CompleteEventAction(InfluenceEventTransition transition)
        {
            if(transition==null)return new InfluenceEventTransition(RuleError.InvalidCommand);
            if(!transition.Accepted || transition.NextState.Practice==null)return transition;
            var s=transition.NextState;var p=s.Practice;var e=s.InfluenceEvent;
            if(p.Stage!=ActionPracticeStage.ResolvingEvent || !p.ActiveCard.HasValue || e==null || e.PhasingPlayer!=p.Actor
                || e.Policy!=InfluenceEventPolicy.NoActiveInterference || p.Definition.InfluenceEvents==null
                || !p.Definition.InfluenceEvents.Effects.TryGetValue(p.ActiveCard.Value,out var effect) || !ReferenceEquals(effect,e.Definition)
                || s.Cards.ResolvingCards.Count!=1 || !s.Cards.ResolvingCards.Contains(p.ActiveCard.Value))return new InfluenceEventTransition(RuleError.InvalidDefinition);
            if(e.Stage==InfluenceEventStage.AwaitingChoice)return transition;
            if(e.Stage!=InfluenceEventStage.Resolved)return new InfluenceEventTransition(RuleError.EventNotStarted);
            try
            {
                var printed=p.Definition.Cards.GetCard(p.ActiveCard.Value);
                var cards=Move(s,p.ActiveCard.Value,false,false,printed.RemoveAfterResolvedEvent);
                var done=Commit(s.WithInfluenceEvent(null),Next(p,p.Space,p.VpForUS),cards,s.RandomState);
                return new InfluenceEventTransition(RuleError.None,next:done.NextState,changes:transition.Changes,
                    decision:transition.DecisionRequestedSequence,resolution:transition.ResolutionSequence);
            }
            catch(OverflowException) { return new InfluenceEventTransition(RuleError.NumericOverflow); }
        }
    }
}
