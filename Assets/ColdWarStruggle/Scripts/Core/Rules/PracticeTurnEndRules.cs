using System;
using System.Linq;
using System.Collections.Generic;
namespace ColdWarStruggle.Core
{
    public sealed class PracticeTurnEndReceipt
    {
        public MilitaryTurnEndAssessment Assessment { get; }
        public int BeforeVp { get; }
        public bool UsHolds { get; }
        public bool UssrHolds { get; }
        public PlayerSide? MilitaryCandidate { get; }
        public VictoryDecision Decision { get; }
        public bool ChinaFlipped { get; }
        public int ExpiredOpsEffects { get; }
        internal PracticeTurnEndReceipt(MilitaryTurnEndAssessment assessment,int before,bool us,bool ussr,PlayerSide? candidate,VictoryDecision decision,bool flipped,int expired)
        { Assessment=assessment;BeforeVp=before;UsHolds=us;UssrHolds=ussr;MilitaryCandidate=candidate;Decision=decision;ChinaFlipped=flipped;ExpiredOpsEffects=expired; }
    }
    public static partial class ActionPracticeRules
    {
        public static RuleError CanAssessTurnEnd(GameState state,PlayerSide side)
        {
            if(state==null || (side!=PlayerSide.US && side!=PlayerSide.USSR))return RuleError.InvalidCommand;
            if(state.Ended)return RuleError.GameAlreadyEnded;
            var p=state.Practice;if(p==null)return RuleError.ActionNotReady;
            if(state.Cards.HeadlineReserved.Count!=0)return RuleError.ActionNotReady;
            if(!p.Definition.TurnEndEnabled)return RuleError.UnsupportedCardAction;
            if(p.TurnEnd!=null)return RuleError.ActionSliceComplete;
            if(SpaceEndTurnDiscardState.QueryOwner(p.Space.Track,out _)!=RuleError.None)return RuleError.UnsupportedCardAction;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            if(state.InfluenceEvent!=null)return RuleError.EventInProgress;
            if(p.Stage!=ActionPracticeStage.Complete || !p.Rounds.IsComplete
                || p.ActiveCard.HasValue || p.OpponentOps!=null || state.Cards.ResolvingCards.Count!=0)return RuleError.ActionNotReady;
            if(p.Risk.Result!=null || p.Risk.Military.Assessment!=null || p.Risk.Military.Turn.Number!=state.Turn.Number)return RuleError.StaleContext;
            return RuleError.None;
        }
        static CardActionTransition PlanTurnEnd(GameState state,PlayerSide side,string actionId,CardId? card,CountryId? target)
        {
            if(string.IsNullOrWhiteSpace(actionId) || actionId.Length>128 || card.HasValue || target.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var error=CanAssessTurnEnd(state,side);if(error!=RuleError.None)return new CardActionTransition(error);
            try
            {
                var p=state.Practice;var id="practice.turn."+state.Turn.Number;
                var military=MilitaryOperationsService.AssessTurnEnd(p.Risk.Military,id+".military",p.Risk.Defcon);
                var pending=VictoryService.Evaluate(p.Risk.Definition.Victory,new MilitaryAssessedEvidence(id+".military","R2015.8.2",p.VpForUS,military.Assessment)).PendingMilitary;
                bool Holds(PlayerSide owner)=>state.Cards.GetHand(owner).Any(c=>p.Definition.Cards.GetCard(c).Kind==CardKind.Scoring);
                var us=Holds(PlayerSide.US);var su=Holds(PlayerSide.USSR);
                var decision=VictoryService.Evaluate(p.Risk.Definition.Victory,new HeldScoringAuditedEvidence(id+".held","R2015.10.3.1",pending,us,su));
                if(decision.Outcome==VictoryOutcome.UnresolvedAdjudication)return new CardActionTransition(RuleError.EventRequiresRuling);
                var risk=p.Risk.WithTurnEnd(military,decision);var continuing=decision.Outcome==VictoryOutcome.Continue;
                var cards=state.Cards;bool flip=continuing && cards.China.Face==CardFace.FaceDown;
                if(flip)
                {
                    var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,cards.GetHand(PlayerSide.US)},{PlayerSide.USSR,cards.GetHand(PlayerSide.USSR)}};
                    cards=new CardZones(p.Definition.Cards,cards.DrawPile,hands,cards.EraReserve,cards.DiscardPile,cards.RemovedCards,cards.ResolvingCards,
                        new ChinaCardState(cards.China.Id,cards.China.Owner,CardFace.FaceUp),cards.EnteredEras,cards.HeadlineReserved);
                }
                var expired=continuing?p.OpsEffects.Instances.Count:0;
                var effects=continuing?p.OpsEffects.Expire(state.Turn):p.OpsEffects;
                var receipt=new PracticeTurnEndReceipt(military.Assessment,p.VpForUS,us,su,pending.Candidate,decision,flip,expired);
                var next=new ActionPracticeState(p.Definition,ActionPracticeStage.Complete,p.CompletedActions,null,p.Space,decision.SignedVp,risk,effects,turnEnd:receipt,rounds:p.Rounds,headline:p.Headline,spaceDiscard:SpaceEndTurnDiscardState.ForAudit(p.Space.Track,receipt,cards,p.Definition.Cards,state.Turn.Number));
                return Commit(state,next,cards,state.RandomState);
            }
            catch(OverflowException) { return new CardActionTransition(RuleError.NumericOverflow); }
        }
    }
}
