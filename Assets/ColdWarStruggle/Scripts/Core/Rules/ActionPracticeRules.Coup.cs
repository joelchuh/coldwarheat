using System;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        public static RuleError CheckCoupTarget(GameState state,PlayerSide side,CardId card,CountryId target)
        {
            var error=CanPlay(state,side,card,CardActionKind.Coup);if(error!=RuleError.None)return error;
            if(!state.Countries.TryGetValue(target,out var country))return RuleError.UnknownCountry;
            return CoupRules.CheckTarget(state.Practice.Risk.Defcon,target,country,side,Array.Empty<string>())==CoupTargetReason.Allowed?RuleError.None:RuleError.IllegalTarget;
        }
        static CardActionTransition PlanCoup(GameState state,PlayerSide side,string actionId,CardId? card,CountryId? target)
        {
            if(string.IsNullOrWhiteSpace(actionId) || actionId.Length>128 || !card.HasValue || !target.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var error=CheckCoupTarget(state,side,card.Value,target.Value);if(error!=RuleError.None)return new CardActionTransition(error);
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
