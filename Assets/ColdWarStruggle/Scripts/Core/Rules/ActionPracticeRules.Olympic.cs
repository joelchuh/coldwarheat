using System;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        static string OlympicResolutionId(GameState state)=>"practice.olympic.turn."+state.Turn.Number+".action."+state.Practice.CompletedActions;
        static RuleError CheckOlympicContext(GameState state)
        {
            if(state==null)return RuleError.InvalidCommand;
            if(state.Ended)return RuleError.GameAlreadyEnded;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            if(state.InfluenceEvent!=null)return RuleError.EventInProgress;
            var execution=state.Practice?.Olympic;if(execution==null)return RuleError.ActionNotReady;
            if(!execution.Matches(state) || execution.Flow.ResolutionId!=OlympicResolutionId(state) || state.Operations.Budget!=0 || state.SpentOps!=0)
                return RuleError.StaleContext;
            return RuleError.None;
        }
        public static RuleError CanRespondToOlympic(GameState state,PlayerSide side)
        {
            if(side!=PlayerSide.US && side!=PlayerSide.USSR)return RuleError.InvalidCommand;
            var error=CheckOlympicContext(state);if(error!=RuleError.None)return error;
            var flow=state.Practice.Olympic.Flow;
            if(flow.Stage!=OlympicResolutionStage.AwaitingOpponent)return RuleError.ActionNotReady;
            return flow.PendingOwner==side?RuleError.None:RuleError.WrongActor;
        }
        // Trusted host pump only; never a CardActionKind available to a player session.
        public static CardActionTransition PlanOlympicContinuation(GameState state)
        {
            var error=CheckOlympicContext(state);if(error!=RuleError.None)return new CardActionTransition(error);
            var flow=state.Practice.Olympic.Flow;
            if(flow.Stage!=OlympicResolutionStage.Rolling)return new CardActionTransition(RuleError.ActionNotReady);
            try {return ApplyOlympicFlow(state,OlympicResolutionRules.ContinueOne(flow,flow.Continuation,new Pcg32Random(state.RandomState)));}
            catch(OverflowException){return new CardActionTransition(RuleError.NumericOverflow);}
            catch(ArgumentException){return new CardActionTransition(RuleError.InvalidDefinition);}
            catch(InvalidOperationException){return new CardActionTransition(RuleError.InvalidDefinition);}
        }
        static CardActionTransition BeginOlympic(GameState state)
        {
            try
            {
                var p=state.Practice;var d=p.Definition.Olympic;var risk=p.Risk;
                if(d==null || risk==null || p.HasPendingHeadline || p.Stage!=ActionPracticeStage.ChooseCard || p.OpponentOps!=null
                    || p.Olympic!=null || p.Blockade!=null || p.War!=null || p.FiveYearPlan!=null || risk.Result!=null
                    || !ReferenceEquals(risk.Definition,p.Definition.Risk) || !ReferenceEquals(d.Cards,state.Cards.Catalog)
                    || !ReferenceEquals(state.Map,risk.Definition.Defcon.Map) || state.Cards.ResolvingCards.Count!=0)
                    return new CardActionTransition(RuleError.InvalidDefinition);
                var cards=Move(state,d.Card.Id,true,true);
                var before=new OlympicEventState(d,risk.Defcon,risk.Definition.Victory,p.VpForUS,state.RandomState);
                var flow=OlympicResolutionRules.Begin(before,cards,state.GameId,OlympicResolutionId(state),checked(state.Version+1),OlympicSourceKind.ActionRound,p.Actor,p.Actor);
                var execution=new OlympicPracticeExecution(state,flow);
                var pending=new ActionPracticeState(p.Definition,ActionPracticeStage.ResolvingEvent,p.CompletedActions,d.Card.Id,p.Space,p.VpForUS,risk,p.OpsEffects,rounds:p.Rounds,headline:p.Headline,olympic:execution);
                var done=Commit(state,pending,cards,state.RandomState);
                return new CardActionTransition(RuleError.None,done.NextState,olympicResult:new OlympicPracticeResult(flow,OlympicPracticeOutcome.AwaitingChoice));
            }
            catch(OverflowException){return new CardActionTransition(RuleError.NumericOverflow);}
            catch(ArgumentException){return new CardActionTransition(RuleError.InvalidDefinition);}
            catch(InvalidOperationException){return new CardActionTransition(RuleError.InvalidDefinition);}
        }
        static CardActionTransition PlanOlympicResponse(GameState state,PlayerSide side,CardActionKind kind,string id,CardId? card,CountryId? target)
        {
            if(string.IsNullOrWhiteSpace(id) || id.Length>128 || card.HasValue || target.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var error=CanRespondToOlympic(state,side);if(error!=RuleError.None)return new CardActionTransition(error);
            try
            {
                var flow=state.Practice.Olympic.Flow;var branch=kind==CardActionKind.ParticipateOlympic?OlympicBranch.Participate:OlympicBranch.Boycott;
                // The closed practice registry has flat OPS effects, no Olympic dice/branch interventions.
                return ApplyOlympicFlow(state,OlympicResolutionRules.Respond(flow,side,flow.DecisionId,flow.Revision,branch,
                    branch==OlympicBranch.Participate?new Pcg32Random(state.RandomState):null));
            }
            catch(OverflowException){return new CardActionTransition(RuleError.NumericOverflow);}
            catch(ArgumentException){return new CardActionTransition(RuleError.InvalidDefinition);}
            catch(InvalidOperationException){return new CardActionTransition(RuleError.InvalidDefinition);}
        }
        static CardActionTransition ApplyOlympicFlow(GameState state,OlympicResolution flow)
        {
            var p=state.Practice;var execution=p.Olympic;var risk=p.Risk.AfterOlympic(execution.Flow.Current,flow.Current,flow.Decision);
            var terminal=flow.Decision.Outcome==VictoryOutcome.Finished;var complete=flow.Stage==OlympicResolutionStage.Completed;
            var stage=terminal?ActionPracticeStage.Complete:flow.Stage==OlympicResolutionStage.AwaitingOperations?ActionPracticeStage.ChoosingOperations:ActionPracticeStage.ResolvingEvent;
            var next=new ActionPracticeState(p.Definition,stage,p.CompletedActions,p.ActiveCard,p.Space,flow.Current.VpForUS,risk,p.OpsEffects,rounds:p.Rounds,headline:p.Headline,
                olympic:complete?null:execution.WithFlow(flow,risk));
            var current=Commit(state,next,state.Cards,flow.Current.Random).NextState;
            var result=terminal?OlympicPracticeOutcome.Interrupted:flow.Stage==OlympicResolutionStage.Rolling?OlympicPracticeOutcome.Tie
                :flow.Stage==OlympicResolutionStage.AwaitingOperations?OlympicPracticeOutcome.AwaitingOperations:OlympicPracticeOutcome.ContestCompleted;
            if(complete && !terminal)current=Commit(current,Next(next,next.Space,next.VpForUS),Move(current,p.ActiveCard.Value,false,false),current.RandomState).NextState;
            return new CardActionTransition(RuleError.None,current,olympicResult:new OlympicPracticeResult(flow,result));
        }
    }
}
