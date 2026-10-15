using System;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        static ActionPracticeState AfterImmediate(ActionPracticeState p,CardId card,SpaceAttemptState space,int vp,PracticeRiskState risk)
            =>risk?.Result?.Outcome==VictoryOutcome.Finished
                ?new ActionPracticeState(p.Definition,ActionPracticeStage.Complete,p.CompletedActions,card,space,vp,risk,p.OpsEffects,headline:p.Headline)
                :Next(p,space,vp,risk);
        static CardActionTransition PlanScoring(GameState state,PlayerSide side,string actionId,CardId? card,CountryId? target)
        {
            if(string.IsNullOrWhiteSpace(actionId) || actionId.Length>128 || !card.HasValue || target.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var error=CanPlay(state,side,card.Value,CardActionKind.Scoring);if(error!=RuleError.None)return new CardActionTransition(error);
            return ResolveScoringCard(state,card.Value);
        }
        internal static CardActionTransition ResolveScoringCard(GameState state,CardId card)
        {
            try
            {
                var p=state.Practice;var printed=p.Definition.Cards.GetCard(card);var slot=state.Practice.HasPendingHeadline?state.Practice.Headline.CurrentResolutionId:PracticeSlotId(state);
                var snapshot=new ScoringSnapshot(state.Map,state.GameId+"."+slot,state.Version,state.Countries,Array.Empty<string>());
                var score=RegionScoringService.CalculateUnmodified(p.Definition.Scoring,snapshot,printed.ScoringRegion);
                var decision=VictoryService.Evaluate(p.Risk.Definition.Victory,new ScoringResolvedEvidence(slot,printed.Id.Value,p.VpForUS,score));
                var risk=p.Risk.WithVictory(decision);var terminal=decision.Outcome==VictoryOutcome.Finished;
                var next=AfterImmediate(p,card,p.Space,decision.SignedVp,risk);
                return Commit(state,next,Move(state,card,true,terminal,!terminal && printed.RemoveAfterResolvedEvent),state.RandomState,scoring:score);
            }
            catch(OverflowException) {return new CardActionTransition(RuleError.NumericOverflow);}
        }
    }
}
