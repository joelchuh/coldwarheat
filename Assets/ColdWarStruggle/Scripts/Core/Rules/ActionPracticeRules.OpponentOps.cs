using System;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        public static RuleError CanPlayOpponentInfluence(GameState state,PlayerSide side,CardId card)=>CanPlayOpponentOps(state,side,card);
        public static RuleError CanPlayOpponentOps(GameState state,PlayerSide side,CardId card)
        {
            var error=CanPlay(state,side,card,CardActionKind.Event);if(error!=RuleError.None)return error;
            var p=state.Practice;var affiliation=p.Definition.Cards.GetCard(card).Affiliation;
            if(affiliation!=(side==PlayerSide.US?CardAffiliation.USSR:CardAffiliation.US))return RuleError.UnsupportedCardAction;
            if(p.OpponentOps!=null || state.Cards.ResolvingCards.Count!=0)return RuleError.ActionNotReady;
            return RuleError.None;
        }
        static CardActionTransition PlanOpponentInfluence(GameState state,PlayerSide side,CardActionKind kind,string actionId,CardId? card,CountryId? target)
        {
            if(string.IsNullOrWhiteSpace(actionId) || actionId.Length>128 || !card.HasValue || target.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var error=CanPlayOpponentInfluence(state,side,card.Value);if(error!=RuleError.None)return new CardActionTransition(error);
            try
            {
                var first=kind==CardActionKind.EventThenInfluence || kind==CardActionKind.OpponentEventFirst;
                var legacy=kind==CardActionKind.EventThenInfluence || kind==CardActionKind.InfluenceThenEvent;
                var sequence=OpponentOpsSequence.Create(card.Value,side,first?OpponentEventOrder.EventFirst:OpponentEventOrder.OperationsFirst,legacy?OperationsMode.Influence:(OperationsMode?)null);
                var p=state.Practice;
                var pending=new ActionPracticeState(p.Definition,ActionPracticeStage.ResolvingEvent,p.CompletedActions,card,p.Space,p.VpForUS,p.Risk,p.OpsEffects,sequence,rounds:p.Rounds,headline:p.Headline);
                var prepared=Commit(state,pending,Move(state,card.Value,true,true),state.RandomState).NextState;
                return sequence.CurrentStep==CardResolutionStep.Event?BeginOpponentEvent(prepared):BeginOpponentOperations(prepared,sequence);
            }
            catch(OverflowException) { return new CardActionTransition(RuleError.NumericOverflow); }
        }
        static bool ValidOpponentRoot(GameState s)
        {
            var p=s.Practice;var q=p?.OpponentOps;
            return q!=null && q.Actor==p.Actor && p.ActiveCard.HasValue && q.Card.Equals(p.ActiveCard.Value)
                && s.Cards.ResolvingCards.Count==1 && s.Cards.ResolvingCards.Contains(q.Card);
        }
        static CardActionTransition BeginOpponentOperations(GameState s,OpponentOpsSequence sequence)
        {
            if(!ValidOpponentRoot(s) || sequence.CurrentStep!=CardResolutionStep.Operations)return new CardActionTransition(RuleError.InvalidDefinition);
            var p=s.Practice;
            // Quote and reachability are evaluated at the actual OPS-mode choice, after earlier effects.
            if(!sequence.Mode.HasValue)
            {
                var pending=new ActionPracticeState(p.Definition,ActionPracticeStage.ChoosingOperations,p.CompletedActions,sequence.Card,p.Space,p.VpForUS,p.Risk,p.OpsEffects,sequence,rounds:p.Rounds,headline:p.Headline);
                return Commit(s,pending,s.Cards,s.RandomState);
            }
            if(sequence.Mode!=OperationsMode.Influence && sequence.Mode!=OperationsMode.Realignment)return new CardActionTransition(RuleError.InvalidDefinition);
            var realign=sequence.Mode==OperationsMode.Realignment;
            var quote=QuoteCard(s,p.Actor,sequence.Card,realign?OpsValuePurpose.Realignment:OpsValuePurpose.Influence);
            var next=new ActionPracticeState(p.Definition,realign?ActionPracticeStage.Realigning:ActionPracticeStage.PlacingInfluence,p.CompletedActions,sequence.Card,p.Space,p.VpForUS,p.Risk,p.OpsEffects,sequence,rounds:p.Rounds,headline:p.Headline);
            if(!realign)return Commit(s,next,s.Cards,s.RandomState,quote.GeneralValue);
            var ops=new OperationsContext(PracticeOperationsId(s,p.CompletedActions),p.Actor,quote.GeneralValue,Array.Empty<CountryId>(),OperationsMode.Realignment,s.Operations.Costs);
            return new CardActionTransition(RuleError.None,s.WithPractice(next,ops,s.Cards,s.RandomState));
        }
        public static RuleError CanChooseOpponentOperations(GameState s,PlayerSide side,OperationsMode mode)
        {
            var error=Check(s,side);if(error!=RuleError.None)return error;
            if(!Enum.IsDefined(typeof(OperationsMode),mode))return RuleError.InvalidCommand;
            if(s.Deployment!=null)return RuleError.SetupInProgress;
            if(s.Practice.Stage!=ActionPracticeStage.ChoosingOperations)return RuleError.ActionNotReady;
            if(!ValidOpponentRoot(s) || s.Practice.OpponentOps.CurrentStep!=CardResolutionStep.Operations || s.Practice.OpponentOps.Mode.HasValue)return RuleError.InvalidDefinition;
            if(mode==OperationsMode.Realignment && s.Practice.Definition.Realignment==null || mode==OperationsMode.Coup && s.Practice.Risk==null)return RuleError.UnsupportedCardAction;
            return RuleError.None;
        }
        static CardActionTransition PlanOpponentOperations(GameState s,PlayerSide side,CardActionKind kind,string id,CardId? card,CountryId? target)
        {
            if(string.IsNullOrWhiteSpace(id) || id.Length>128 || card.HasValue || (kind==CardActionKind.UseOpponentCoup)!=target.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var mode=kind==CardActionKind.UseOpponentCoup?OperationsMode.Coup:kind==CardActionKind.UseOpponentRealignment?OperationsMode.Realignment:OperationsMode.Influence;
            var error=CanChooseOpponentOperations(s,side,mode);if(error!=RuleError.None)return new CardActionTransition(error);
            try
            {
                var sequence=s.Practice.OpponentOps.SelectMode(mode);
                return mode==OperationsMode.Coup?PlanOpponentCoup(s,side,target.Value,sequence):BeginOpponentOperations(s,sequence);
            }
            catch(OverflowException) { return new CardActionTransition(RuleError.NumericOverflow); }
        }

        static CardActionTransition AdvanceOpponentStep(GameState s,CardResolutionStep step)
        {
            if(!ValidOpponentRoot(s) || s.Practice.OpponentOps.CurrentStep!=step)return new CardActionTransition(RuleError.InvalidDefinition);
            var p=s.Practice;var sequence=p.OpponentOps.Advance(step);
            if(sequence.CurrentStep==CardResolutionStep.Complete)
            {
                var printed=p.Definition.Cards.GetCard(sequence.Card);
                return Commit(s,Next(p,p.Space,p.VpForUS),Move(s,sequence.Card,false,false,sequence.EventOccurred && printed.RemoveAfterResolvedEvent),s.RandomState);
            }
            if(sequence.CurrentStep==CardResolutionStep.Operations)return BeginOpponentOperations(s,sequence);
            var pending=new ActionPracticeState(p.Definition,ActionPracticeStage.ResolvingEvent,p.CompletedActions,sequence.Card,p.Space,p.VpForUS,p.Risk,p.OpsEffects,sequence,rounds:p.Rounds,headline:p.Headline);
            return BeginOpponentEvent(Commit(s,pending,s.Cards,s.RandomState).NextState);
        }
        static CardActionTransition BeginOpponentEvent(GameState s)
        {
            if(!ValidOpponentRoot(s) || s.Practice.OpponentOps.CurrentStep!=CardResolutionStep.Event || s.Practice.Stage!=ActionPracticeStage.ResolvingEvent)return new CardActionTransition(RuleError.InvalidDefinition);
            var p=s.Practice;var q=p.OpponentOps;
            if(p.Definition.Blockade?.Card.Id.Equals(q.Card)==true)return BeginBlockade(s,true);
            if(p.Definition.InfluenceEvents?.Effects.TryGetValue(q.Card,out var influence)==true)
            {
                var prepared=s.WithInfluenceEvent(new InfluenceEventExecution("practice.event."+p.CompletedActions,influence,p.Actor,InfluenceEventPolicy.NoActiveInterference));
                var raw=InfluenceEventFlow.Begin(prepared,p.Actor);var done=CompleteEventAction(raw);
                return done.Accepted?new CardActionTransition(RuleError.None,done.NextState,eventTransition:raw):new CardActionTransition(done.Error);
            }
            if(p.Definition.DefconVpEvents?.Effects.TryGetValue(q.Card,out var track)==true)
            {
                var plan=DefconVpEventService.Plan(track,p.Risk.Defcon,p.Risk.Definition.Victory,p.VpForUS,q.EventSide,p.Actor,"practice.event."+p.CompletedActions,DefconVpEventPolicy.NoActiveInterference);
                var risk=p.Risk.After(plan);var terminal=risk.Result?.Outcome==VictoryOutcome.Finished;
                var after=new ActionPracticeState(p.Definition,terminal?ActionPracticeStage.Complete:ActionPracticeStage.ResolvingEvent,p.CompletedActions,q.Card,p.Space,plan.Decision.SignedVp,risk,p.OpsEffects,q,rounds:p.Rounds,headline:p.Headline);
                var committed=Commit(s,after,s.Cards,s.RandomState);
                var done=terminal?committed:AdvanceOpponentStep(committed.NextState,CardResolutionStep.Event);
                return done.Accepted?new CardActionTransition(RuleError.None,done.NextState,defconVpEvent:plan):done;
            }
            if(p.Definition.TurnOpsEvents?.Effects.TryGetValue(q.Card,out var flat)==true)
            {
                var activation=p.OpsEffects.Activate(flat,q.EventSide,"practice.event."+p.CompletedActions,checked(s.EventSequence+1),s.Turn);
                if(!activation.Accepted)return new CardActionTransition(activation.Error);
                if(activation.AlreadyApplied)return new CardActionTransition(RuleError.StaleContext);
                var after=new ActionPracticeState(p.Definition,ActionPracticeStage.ResolvingEvent,p.CompletedActions,q.Card,p.Space,p.VpForUS,p.Risk,activation.After,q,rounds:p.Rounds,headline:p.Headline);
                var committed=Commit(s,after,s.Cards,s.RandomState);var done=AdvanceOpponentStep(committed.NextState,CardResolutionStep.Event);
                return done.Accepted?new CardActionTransition(RuleError.None,done.NextState,activatedOpsEffect:activation.Instance):done;
            }
            return new CardActionTransition(RuleError.UnsupportedCardAction);
        }
    }
}
