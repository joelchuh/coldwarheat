using System;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        public static RuleError CanSkipEmptyHand(GameState state,PlayerSide side)
        {
            var error=Check(state,side);if(error!=RuleError.None)return error;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            var p=state.Practice;
            if(p.Stage!=ActionPracticeStage.ChooseCard || p.Rounds.IsOptional || !p.Rounds.CanBegin(p.Space.Track,side)
                || p.ActiveCard.HasValue || p.OpponentOps!=null || state.Cards.ResolvingCards.Count!=0 || p.TurnEnd!=null
                || state.Cards.GetHand(side).Count!=0)return RuleError.ActionNotReady;
            return RuleError.None;
        }
        static CardActionTransition PlanSkipEmptyHand(GameState state,PlayerSide side,string actionId,CardId? card,CountryId? target)
        {
            if(string.IsNullOrWhiteSpace(actionId) || actionId.Length>128 || card.HasValue || target.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var error=CanSkipEmptyHand(state,side);if(error!=RuleError.None)return new CardActionTransition(error);
            try
            {
                var p=state.Practice;var rounds=p.Rounds.SkipOrdinarySlot(p.Space.Track,side);
                var next=new ActionPracticeState(p.Definition,rounds.IsComplete?ActionPracticeStage.Complete:ActionPracticeStage.ChooseCard,
                    rounds.TotalCompleted,null,p.Space,p.VpForUS,p.Risk,p.OpsEffects,headline:p.Headline,rounds:rounds);
                return Commit(state,next,state.Cards,state.RandomState);
            }
            catch(OverflowException){return new CardActionTransition(RuleError.NumericOverflow);}
        }
    }
}
