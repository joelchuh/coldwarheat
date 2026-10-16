using System;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class PracticeFinalScoringReceipt
    {
        public int BeforeVp { get; }
        public FinalScoringResult Result { get; }
        public VictoryDecision Decision { get; }
        internal PracticeFinalScoringReceipt(int before,FinalScoringResult result,VictoryDecision decision)
        {
            BeforeVp=before;Result=result??throw new ArgumentNullException(nameof(result));Decision=decision??throw new ArgumentNullException(nameof(decision));
            if(decision.Checkpoint!=VictoryCheckpoint.FinalScoringCompleted || decision.Outcome!=VictoryOutcome.Finished)throw new ArgumentException("Expected a closed final-scoring decision.");
        }
    }
    public static partial class ActionPracticeRules
    {
        public static RuleError CanCompleteFinalScoring(GameState state,PlayerSide side)
        {
            if(state==null || (side!=PlayerSide.US && side!=PlayerSide.USSR))return RuleError.InvalidCommand;
            if(state.Ended)return RuleError.GameAlreadyEnded;
            var p=state.Practice;if(p==null)return RuleError.ActionNotReady;
            if(p.Definition.FinalScoring==null)return RuleError.UnsupportedCardAction;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            if(state.InfluenceEvent!=null)return RuleError.EventInProgress;
            if(state.Turn.Number!=10 || state.Turn.Number!=p.Definition.Calendar.LastTurn)return RuleError.ActionNotReady;
            if(!ReferenceEquals(state.Turn.Parameters,p.Definition.Calendar.ForTurn(state.Turn.Number).Parameters)
                || !ReferenceEquals(state.Turn.Parameters,p.Space.Turn.Parameters)
                || !ReferenceEquals(state.Turn.Parameters,p.Risk.Military.Turn.Parameters))return RuleError.StaleContext;
            if(p.Stage!=ActionPracticeStage.Complete || p.CompletedActions!=state.Turn.Parameters.NormalActionsPerSide*2
                || p.TurnEnd?.Decision.Outcome!=VictoryOutcome.Continue || p.ActiveCard.HasValue || p.OpponentOps!=null
                || p.HeadlineSelection!=null || p.HasPendingHeadline || state.Cards.HeadlineReserved.Count!=0
                || state.Cards.ResolvingCards.Count!=0)return RuleError.ActionNotReady;
            if(p.FinalScoring!=null || p.Risk.Result!=null || !ReferenceEquals(p.Risk.Military.Assessment,p.TurnEnd.Assessment)
                || p.Risk.Military.Turn.Number!=state.Turn.Number || p.Space.Turn.Number!=state.Turn.Number
                || p.VpForUS!=p.TurnEnd.Decision.SignedVp || p.OpsEffects.Instances.Count!=0
                || state.Cards.China.Face!=CardFace.FaceUp || !state.Cards.China.Id.Equals(p.Definition.FinalScoring.ChinaCardId)
                || state.Operations.Budget!=0 || state.SpentOps!=0
                || p.Risk.Military.UsEarned!=p.Risk.Definition.Military.ResetEarned || p.Risk.Military.UssrEarned!=p.Risk.Definition.Military.ResetEarned
                || !new[]{GameEra.Early,GameEra.Mid,GameEra.Late}.All(state.Cards.EnteredEras.Contains)
                || new[]{PlayerSide.US,PlayerSide.USSR}.Any(sideId=>state.Cards.GetHand(sideId).Any(id=>p.Definition.Cards.GetCard(id).Kind==CardKind.Scoring)))return RuleError.StaleContext;
            if(HasExtraActionAbility(p.Space.Track) || SpaceAbilityRules.Query(p.Space.Track,PlayerSide.US).EndTurnDiscardMaximum>0
                || SpaceAbilityRules.Query(p.Space.Track,PlayerSide.USSR).EndTurnDiscardMaximum>0)return RuleError.UnsupportedCardAction;
            return RuleError.None;
        }
        static CardActionTransition PlanFinalScoring(GameState state,PlayerSide side,string actionId,CardId? card,CountryId? target)
        {
            if(string.IsNullOrWhiteSpace(actionId) || actionId.Length>128 || card.HasValue || target.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var error=CanCompleteFinalScoring(state,side);if(error!=RuleError.None)return new CardActionTransition(error);
            try
            {
                var p=state.Practice;var id="practice.turn."+state.Turn.Number+".final";
                // This bounded runtime has no active scoring modifiers. Unsupported event handlers cannot enter it.
                var snapshot=new ScoringSnapshot(state.Map,id,state.Version,state.Countries,Array.Empty<string>());
                var result=FinalScoringService.CalculateUnmodified(p.Definition.FinalScoring,snapshot,state.Cards.China);
                var decision=VictoryService.Evaluate(p.Risk.Definition.Victory,new FinalScoringCompletedEvidence(id,"R2015.10.3.2",p.VpForUS,result));
                var receipt=new PracticeFinalScoringReceipt(p.VpForUS,result,decision);
                var next=new ActionPracticeState(p.Definition,ActionPracticeStage.Complete,p.CompletedActions,null,p.Space,decision.SignedVp,
                    p.Risk.WithVictory(decision),p.OpsEffects,turnEnd:p.TurnEnd,headline:p.Headline,finalScoring:receipt);
                return Commit(state,next,state.Cards,state.RandomState);
            }
            catch(OverflowException){return new CardActionTransition(RuleError.NumericOverflow);}
        }
    }
}
