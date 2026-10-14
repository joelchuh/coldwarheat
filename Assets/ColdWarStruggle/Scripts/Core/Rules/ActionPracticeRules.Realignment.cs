using System;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        static RuleError CheckRealignmentContext(GameState state,PlayerSide side)
        {
            var error=Check(state,side);if(error!=RuleError.None)return error;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            var p=state.Practice;
            if(p.Stage!=ActionPracticeStage.Realigning)return RuleError.ActionNotReady;
            if(p.Definition.Realignment==null || p.Risk==null || !HasActivePhysicalCard(state))return RuleError.InvalidDefinition;
            if(state.Operations.Actor!=side)return RuleError.WrongActor;
            if(state.Operations.Mode!=OperationsMode.Realignment)return RuleError.ModeMismatch;
            return RuleError.None;
        }
        public static RuleError CanRollRealignment(GameState state,PlayerSide side)
        {
            var error=CheckRealignmentContext(state,side);if(error!=RuleError.None)return error;
            return state.Operations.Budget-state.SpentOps<state.Practice.Definition.Realignment.OpsCostPerRoll?RuleError.InsufficientBudget:RuleError.None;
        }
        public static RuleError CheckRealignmentTarget(GameState state,PlayerSide side,CountryId target)
        {
            var error=CanRollRealignment(state,side);if(error!=RuleError.None)return error;
            if(!state.Countries.TryGetValue(target,out var country))return RuleError.UnknownCountry;
            if(state.Operations.RegionalBudget!=null && !state.Operations.RegionalBudget.TrySpend(target,state.Practice.Definition.Realignment.OpsCostPerRoll,out _))return RuleError.InsufficientBudget;
            // Supported Flat effects have already set the roll budget; there are no registered die interventions.
            return RealignmentRules.CheckTarget(state.Practice.Risk.Defcon,target,country,side,Array.Empty<string>())==RealignmentTargetReason.Allowed?RuleError.None:RuleError.IllegalTarget;
        }
        static CardActionTransition PlanRealignment(GameState state,PlayerSide side,CardActionKind kind,string actionId,CardId? card,CountryId? target)
        {
            var begin=kind==CardActionKind.Realignment;
            if(string.IsNullOrWhiteSpace(actionId) || actionId.Length>128 || begin!=card.HasValue || (kind==CardActionKind.RollRealignment)!=target.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var error=begin?CanPlay(state,side,card.Value,kind):CheckRealignmentContext(state,side);
            if(error!=RuleError.None)return new CardActionTransition(error);
            var p=state.Practice;
            try
            {
                if(begin)
                {
                    var quote=QuoteCard(state,side,card.Value,OpsValuePurpose.Realignment);
                    var regional=RegionalBudget(state,quote);
                    var next=new ActionPracticeState(p.Definition,ActionPracticeStage.Realigning,p.CompletedActions,card,p.Space,p.VpForUS,p.Risk,p.OpsEffects);
                    var ops=new OperationsContext(state.GameId+".action."+p.CompletedActions,side,regional?.EffectiveBudget??quote.GeneralValue,Array.Empty<CountryId>(),OperationsMode.Realignment,state.Operations.Costs,regional);
                    return new CardActionTransition(RuleError.None,state.WithPractice(next,ops,Move(state,card.Value,true,true),state.RandomState));
                }
                if(kind==CardActionKind.FinishRealignment)
                    return p.OpponentOps!=null?AdvanceOpponentStep(state,CardResolutionStep.Operations):Commit(state,Next(p,p.Space,p.VpForUS),Move(state,p.ActiveCard.Value,false,false),state.RandomState);
                error=CheckRealignmentTarget(state,side,target.Value);if(error!=RuleError.None)return new CardActionTransition(error);
                var definition=p.Definition.Realignment;var operations=state.Operations;
                if(operations.RegionalBudget!=null)
                {
                    if(!operations.RegionalBudget.TrySpend(target.Value,definition.OpsCostPerRoll,out var regional))return new CardActionTransition(RuleError.InsufficientBudget);
                    operations=operations.WithRegionalBudget(regional);
                }
                // A private restored cursor makes both dice speculative until the complete state commits.
                var random=new Pcg32Random(state.RandomState);
                var us=random.NextInt(definition.DieMinimum-1,definition.DieMaximum)+1;
                var ussr=random.NextInt(definition.DieMinimum-1,definition.DieMaximum)+1;
                var result=RealignmentRules.Calculate(definition,p.Risk.Defcon,state.Countries,target.Value,side,us,ussr,Array.Empty<string>());
                var countries=state.Countries.ToDictionary(x=>x.Key,x=>x.Value);countries[target.Value]=result.After;
                var after=state.ApplyRealignment(countries,checked(state.SpentOps+definition.OpsCostPerRoll),random.Capture(),operations);
                return new CardActionTransition(RuleError.None,after,realignment:result);
            }
            catch(OverflowException) { return new CardActionTransition(RuleError.NumericOverflow); }
        }
    }
}
