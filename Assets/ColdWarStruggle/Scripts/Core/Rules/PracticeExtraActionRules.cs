using System;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        public static RuleError CanDeclineExtraActions(GameState state,PlayerSide side)
        {
            var error=Check(state,side);if(error!=RuleError.None)return error;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            var p=state.Practice;
            if(p.Stage!=ActionPracticeStage.ChooseCard || !p.Rounds.IsOptional || p.ActiveCard.HasValue || p.OpponentOps!=null
                || state.Cards.ResolvingCards.Count!=0 || p.TurnEnd!=null)return RuleError.ActionNotReady;
            return RuleError.None;
        }
        static CardActionTransition PlanDeclineExtraActions(GameState state,PlayerSide side,string actionId,CardId? card,CountryId? target)
        {
            if(string.IsNullOrWhiteSpace(actionId) || actionId.Length>128 || card.HasValue || target.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var error=CanDeclineExtraActions(state,side);if(error!=RuleError.None)return new CardActionTransition(error);
            try
            {
                var p=state.Practice;var rounds=p.Rounds.DeclineRemaining(p.Space.Track,side);
                var next=new ActionPracticeState(p.Definition,rounds.IsComplete?ActionPracticeStage.Complete:ActionPracticeStage.ChooseCard,
                    p.CompletedActions,null,p.Space,p.VpForUS,p.Risk,p.OpsEffects,headline:p.Headline,rounds:rounds);
                return Commit(state,next,state.Cards,state.RandomState);
            }
            catch(OverflowException){return new CardActionTransition(RuleError.NumericOverflow);}
        }
    }
}
