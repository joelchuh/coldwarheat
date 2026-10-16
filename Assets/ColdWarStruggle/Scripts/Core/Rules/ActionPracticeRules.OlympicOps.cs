using System;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        public static RuleError CanChooseOlympicOperations(GameState state,PlayerSide side,OperationsMode mode)
        {
            if((side!=PlayerSide.US && side!=PlayerSide.USSR) || !Enum.IsDefined(typeof(OperationsMode),mode))return RuleError.InvalidCommand;
            var error=CheckOlympicContext(state);if(error!=RuleError.None)return error;
            var p=state.Practice;var execution=p.Olympic;
            if(execution.Flow.Stage!=OlympicResolutionStage.AwaitingOperations || execution.Mode.HasValue || p.Stage!=ActionPracticeStage.ChoosingOperations)return RuleError.ActionNotReady;
            if(side!=execution.Flow.Sponsor)return RuleError.WrongActor;
            if(mode==OperationsMode.Realignment && p.Definition.Realignment==null || mode==OperationsMode.Coup && p.Risk==null)return RuleError.UnsupportedCardAction;
            return RuleError.None;
        }
        public static RuleError CanContinueOlympicOperations(GameState state,PlayerSide side,OperationsMode mode)
        {
            if(side!=PlayerSide.US && side!=PlayerSide.USSR)return RuleError.InvalidCommand;
            var error=CheckOlympicContext(state);if(error!=RuleError.None)return error;
            var execution=state.Practice.Olympic;
            if(side!=execution.Flow.Sponsor)return RuleError.WrongActor;
            if(!execution.Mode.HasValue)return RuleError.ActionNotReady;
            if(mode!=execution.Mode || (mode!=OperationsMode.Influence && mode!=OperationsMode.Realignment))return RuleError.ModeMismatch;
            return RuleError.None;
        }
        public static OpsGeneralQuote QuoteOlympicOperations(GameState state,PlayerSide side,OpsValuePurpose purpose)
        {
            if(purpose!=OpsValuePurpose.Influence && purpose!=OpsValuePurpose.Realignment && purpose!=OpsValuePurpose.Coup)throw new ArgumentException("Olympic boycott grants map operations only.");
            var mode=purpose==OpsValuePurpose.Influence?OperationsMode.Influence:purpose==OpsValuePurpose.Realignment?OperationsMode.Realignment:OperationsMode.Coup;
            if(CanChooseOlympicOperations(state,side,mode)!=RuleError.None)throw new ArgumentException("An authorized pending Olympic boycott is required.");
            var p=state.Practice;var d=p.Definition.Olympic;
            return OpsGeneralRules.QuoteAsIfCard(p.Definition.Ops,d.EffectId,d.BoycottBaseOps,side,purpose,p.OpsEffects.GetModifiers(state.Turn));
        }
        static CardActionTransition PlanOlympicOperations(GameState state,PlayerSide side,CardActionKind kind,string id,CardId? card,CountryId? target)
        {
            if(string.IsNullOrWhiteSpace(id) || id.Length>128 || card.HasValue || (kind==CardActionKind.UseOlympicCoup)!=target.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var mode=kind==CardActionKind.UseOlympicCoup?OperationsMode.Coup:kind==CardActionKind.UseOlympicRealignment?OperationsMode.Realignment:OperationsMode.Influence;
            var error=CanChooseOlympicOperations(state,side,mode);if(error!=RuleError.None)return new CardActionTransition(error);
            try
            {
                var p=state.Practice;var execution=p.Olympic;
                if(kind==CardActionKind.DeclineOlympicOperations)
                    return FinishOlympicRoot(state,OlympicResolutionRules.DeclineOperations(execution.Flow,side,execution.Flow.Revision),OlympicPracticeOutcome.OperationsDeclined);
                if(mode==OperationsMode.Coup)return PlanOlympicCoup(state,side,target.Value);
                var quote=QuoteOlympicOperations(state,side,mode==OperationsMode.Influence?OpsValuePurpose.Influence:OpsValuePurpose.Realignment);
                var ops=mode==OperationsMode.Influence
                    ?OperationsContext.FromRoundStart(PracticeOperationsId(state,p.CompletedActions)+".olympic",side,quote.GeneralValue,state.Map,execution.RootCountries,state.Operations.Costs)
                    :new OperationsContext(PracticeOperationsId(state,p.CompletedActions)+".olympic",side,quote.GeneralValue,Array.Empty<CountryId>(),OperationsMode.Realignment,state.Operations.Costs);
                var next=new ActionPracticeState(p.Definition,mode==OperationsMode.Influence?ActionPracticeStage.PlacingInfluence:ActionPracticeStage.Realigning,p.CompletedActions,p.ActiveCard,p.Space,p.VpForUS,p.Risk,p.OpsEffects,rounds:p.Rounds,headline:p.Headline,olympic:execution.WithOperations(state,ops));
                return new CardActionTransition(RuleError.None,state.WithPractice(next,ops,state.Cards,state.RandomState));
            }
            catch(OverflowException){return new CardActionTransition(RuleError.NumericOverflow);}
            catch(ArgumentException){return new CardActionTransition(RuleError.InvalidDefinition);}
            catch(InvalidOperationException){return new CardActionTransition(RuleError.InvalidDefinition);}
        }
        static CardActionTransition FinishOlympicRoot(GameState state,OlympicResolution flow,OlympicPracticeOutcome outcome,CoupAttemptReceipt coup=null)
        {
            var p=state.Practice;
            var done=Commit(state,Next(p,p.Space,p.VpForUS),Move(state,p.ActiveCard.Value,false,false),state.RandomState);
            return new CardActionTransition(RuleError.None,done.NextState,coup:coup,olympicResult:new OlympicPracticeResult(flow,outcome));
        }
        static CardActionTransition PlanOlympicCoup(GameState state,PlayerSide side,CountryId target)
        {
            var p=state.Practice;var flow=p.Olympic.Flow;
            var error=CheckCoupTarget(state,side,p.ActiveCard.Value,target);if(error!=RuleError.None)return new CardActionTransition(error);
            var quote=QuoteOlympicOperations(state,side,OpsValuePurpose.Coup);var slot=PracticeSlotId(state);
            var request=new CoupAttemptRequest(slot+".olympic.coup",state.GameId,slot,p.Definition.Olympic.EffectId,state.Version,state.Turn.Number,target,side,flow.RootPhasing,quote.GeneralValue,quote.GeneralValue);
            var plan=CoupAttemptRules.PlanAttempt(p.Risk.BindCurrent(state),request,new Pcg32Random(state.RandomState));
            if(plan.AlreadyApplied)throw new InvalidOperationException("Olympic root already spent coup authorization.");
            var risk=p.Risk.After(plan,p.VpForUS);var terminal=risk.Result?.Outcome==VictoryOutcome.Finished;
            var next=new ActionPracticeState(p.Definition,terminal?ActionPracticeStage.Complete:ActionPracticeStage.ResolvingEvent,p.CompletedActions,p.ActiveCard,p.Space,p.VpForUS,risk,p.OpsEffects,rounds:p.Rounds,headline:p.Headline);
            var ops=OperationsContext.FromRoundStart(PracticeOperationsId(state,p.CompletedActions),p.Actor,0,state.Map,plan.After.Countries,state.Operations.Costs);
            var after=state.WithPractice(next,ops,state.Cards,plan.After.Random,plan.After.Countries,terminal);
            return terminal?new CardActionTransition(RuleError.None,after,coup:plan.Receipt,olympicResult:new OlympicPracticeResult(flow,OlympicPracticeOutcome.Interrupted))
                :FinishOlympicRoot(after,flow,OlympicPracticeOutcome.OperationsCompleted,plan.Receipt);
        }
    }
}
