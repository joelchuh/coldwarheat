using System;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        static string PracticeSlotId(GameState s)=>"practice.turn."+s.Turn.Number+".action."+s.Practice.CompletedActions;
        static string PracticeOperationsId(GameState s,int completed)=>s.GameId+".turn."+s.Turn.Number+".action."+completed;
        static bool HasExtraActionAbility(SpaceTrackState track)=>SpaceAbilityRules.Query(track,PlayerSide.US).ActionRoundAllowance.HasValue
            || SpaceAbilityRules.Query(track,PlayerSide.USSR).ActionRoundAllowance.HasValue;
        public static RuleError CanBeginNextTurn(GameState state,PlayerSide side)
        {
            if(state==null || (side!=PlayerSide.US && side!=PlayerSide.USSR))return RuleError.InvalidCommand;
            if(state.Ended)return RuleError.GameAlreadyEnded;
            var p=state.Practice;if(p==null)return RuleError.ActionNotReady;
            if(state.Cards.HeadlineReserved.Count!=0)return RuleError.ActionNotReady;
            if(!p.Definition.NextTurnEnabled)return RuleError.UnsupportedCardAction;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            if(state.InfluenceEvent!=null)return RuleError.EventInProgress;
            if(p.Stage!=ActionPracticeStage.Complete || p.TurnEnd?.Decision.Outcome!=VictoryOutcome.Continue
                || p.CompletedActions!=state.Turn.Parameters.NormalActionsPerSide*2 || p.ActiveCard.HasValue
                || p.OpponentOps!=null || state.Cards.ResolvingCards.Count!=0)return RuleError.ActionNotReady;
            if(p.Risk.Result!=null || !ReferenceEquals(p.Risk.Military.Assessment,p.TurnEnd.Assessment)
                || p.Risk.Military.Turn.Number!=state.Turn.Number || p.Space.Turn.Number!=state.Turn.Number
                || p.OpsEffects.Instances.Count!=0 || state.Cards.China.Face!=CardFace.FaceUp
                || p.Risk.Military.UsEarned!=p.Risk.Definition.Military.ResetEarned
                || p.Risk.Military.UssrEarned!=p.Risk.Definition.Military.ResetEarned)return RuleError.StaleContext;
            if(state.Turn.Number>=p.Definition.Calendar.LastTurn)return RuleError.ActionSliceComplete;
            if(HasExtraActionAbility(p.Space.Track) || SpaceAbilityRules.Query(p.Space.Track,PlayerSide.US).EndTurnDiscardMaximum>0
                || SpaceAbilityRules.Query(p.Space.Track,PlayerSide.USSR).EndTurnDiscardMaximum>0)return RuleError.UnsupportedCardAction;
            return RuleError.None;
        }
        static CardActionTransition PlanNextTurn(GameState state,PlayerSide side,string actionId,CardId? card,CountryId? target)
        {
            if(string.IsNullOrWhiteSpace(actionId) || actionId.Length>128 || card.HasValue || target.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var error=CanBeginNextTurn(state,side);if(error!=RuleError.None)return new CardActionTransition(error);
            try
            {
                var p=state.Practice;var nextTurn=p.Definition.Calendar.ForTurn(checked(state.Turn.Number+1));
                var defcon=DefconService.PlanChange(p.Risk.Defcon,DefconChangeKind.TurnStartImprovement).After;
                var space=SpaceAttemptRules.BeginNextTurn(p.Space,nextTurn.Number);
                // Use the calendar context carried by Space so every new subsystem shares one turn object.
                nextTurn=space.Turn;
                var risk=p.Risk.BeginNextTurn(nextTurn,defcon);
                var deal=TurnDealService.Plan(p.Definition.Cards,state.Cards,nextTurn,new Pcg32Random(state.RandomState));
                var next=new ActionPracticeState(p.Definition,ActionPracticeStage.ChooseCard,0,null,space,p.VpForUS,risk,TurnOpsEffectState.Create(nextTurn));
                var ops=OperationsContext.FromRoundStart(state.GameId+".turn."+nextTurn.Number+".action.0",PlayerSide.USSR,0,state.Map,state.Countries,state.Operations.Costs);
                return CollectHeadlinesIfEnabled(new CardActionTransition(RuleError.None,state.WithNextPracticeTurn(next,ops,deal.Cards,deal.Random,nextTurn)));
            }
            catch(OverflowException){return new CardActionTransition(RuleError.NumericOverflow);}
            catch(ArgumentException){return new CardActionTransition(RuleError.TurnPreparationUnavailable);}
        }
    }
}
