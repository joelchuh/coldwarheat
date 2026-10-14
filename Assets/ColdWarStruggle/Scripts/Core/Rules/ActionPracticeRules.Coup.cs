using System;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        public static RuleError CheckCoupCard(GameState state,PlayerSide side,CardId card)
        {
            if(state?.Practice?.Stage!=ActionPracticeStage.ChoosingOperations)return CanPlay(state,side,card,CardActionKind.Coup);
            var error=CanChooseOpponentOperations(state,side,OperationsMode.Coup);if(error!=RuleError.None)return error;
            return state.Practice.ActiveCard.Value.Equals(card)?RuleError.None:RuleError.CardNotOwned;
        }
        public static RuleError CheckCoupTarget(GameState state,PlayerSide side,CardId card,CountryId target)
        {
            var error=CheckCoupCard(state,side,card);if(error!=RuleError.None)return error;
            if(!state.Countries.TryGetValue(target,out var country))return RuleError.UnknownCountry;
            return CoupRules.CheckTarget(state.Practice.Risk.Defcon,target,country,side,Array.Empty<string>())==CoupTargetReason.Allowed?RuleError.None:RuleError.IllegalTarget;
        }
        static CardActionTransition PlanOpponentCoup(GameState state,PlayerSide side,CountryId target,OpponentOpsSequence sequence)
        {
            var error=CheckCoupTarget(state,side,sequence.Card,target);if(error!=RuleError.None)return new CardActionTransition(error);
            var p=state.Practice;var quote=QuoteCard(state,side,sequence.Card,OpsValuePurpose.Coup);var slot="practice."+p.CompletedActions;
            var request=new CoupAttemptRequest(slot,state.GameId,slot,sequence.Card.Value,state.Version,state.Turn.Number,target,side,p.Actor,quote.GeneralValue,quote.GeneralValue);
            var plan=CoupAttemptRules.PlanAttempt(p.Risk.BindCurrent(state),request,new Pcg32Random(state.RandomState));
            if(plan.AlreadyApplied)throw new InvalidOperationException("Action cursor does not match coup receipts.");
            var risk=p.Risk.After(plan,p.VpForUS);var terminal=risk.Result?.Outcome==VictoryOutcome.Finished;
            var next=new ActionPracticeState(p.Definition,terminal?ActionPracticeStage.Complete:ActionPracticeStage.ResolvingEvent,p.CompletedActions,sequence.Card,p.Space,p.VpForUS,risk,p.OpsEffects,sequence);
            var ops=OperationsContext.FromRoundStart(state.GameId+".action."+p.CompletedActions,p.Actor,0,state.Map,plan.After.Countries,state.Operations.Costs);
            var after=state.WithPractice(next,ops,state.Cards,plan.After.Random,plan.After.Countries,terminal);
            var done=terminal?new CardActionTransition(RuleError.None,after):AdvanceOpponentStep(after,CardResolutionStep.Operations);
            if(!done.Accepted)return done;
            // A coup and its following event may both finish in this command. Preserve both receipts.
            return new CardActionTransition(RuleError.None,done.NextState,coup:plan.Receipt,eventTransition:done.EventTransition,defconVpEvent:done.DefconVpEvent,activatedOpsEffect:done.ActivatedOpsEffect);
        }
        static CardActionTransition PlanCoup(GameState state,PlayerSide side,string actionId,CardId? card,CountryId? target)
        {
            if(string.IsNullOrWhiteSpace(actionId) || actionId.Length>128 || !card.HasValue || !target.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var error=CanPlay(state,side,card.Value,CardActionKind.Coup);if(error!=RuleError.None)return new CardActionTransition(error);
            error=CheckCoupTarget(state,side,card.Value,target.Value);if(error!=RuleError.None)return new CardActionTransition(error);
            try
            {
                var p=state.Practice;var printed=p.Definition.Cards.GetCard(card.Value);
                var quote=QuoteCard(state,side,card.Value,OpsValuePurpose.Coup);
                var slot="practice."+p.CompletedActions;
                var request=new CoupAttemptRequest(slot,state.GameId,slot,printed.Id.Value,state.Version,state.Turn.Number,target.Value,side,side,quote.GeneralValue,quote.GeneralValue);
                var plan=CoupAttemptRules.PlanAttempt(p.Risk.BindCurrent(state),request,new Pcg32Random(state.RandomState));
                if(plan.AlreadyApplied)throw new InvalidOperationException("Action cursor does not match coup receipts.");
                var risk=p.Risk.After(plan,p.VpForUS);var nuclear=risk.Result?.Outcome==VictoryOutcome.Finished;
                // Nuclear war interrupts the root action: preserve its physical resolving card for audit.
                var next=nuclear?new ActionPracticeState(p.Definition,ActionPracticeStage.Complete,p.CompletedActions,card,p.Space,p.VpForUS,risk,p.OpsEffects):Next(p,p.Space,p.VpForUS,risk);
                var ops=OperationsContext.FromRoundStart(state.GameId+".action."+next.CompletedActions,next.Actor,0,state.Map,plan.After.Countries,state.Operations.Costs);
                return new CardActionTransition(RuleError.None,state.WithPractice(next,ops,Move(state,card.Value,true,nuclear),plan.After.Random,plan.After.Countries,nuclear),coup:plan.Receipt);
            }
            catch(OverflowException) {return new CardActionTransition(RuleError.NumericOverflow);}
        }
    }
}
