using System;
using System.Linq;
using System.Collections.Generic;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        static RuleError SpaceDiscardBindingError(GameState s)
        {
            var p=s.Practice;
            if(!ReferenceEquals(p.Space.Track.Definition,p.Definition.Space) || !ReferenceEquals(p.Space.Calendar,p.Definition.Calendar)
                || !ReferenceEquals(s.Turn.Parameters,p.Definition.Calendar.ForTurn(s.Turn.Number).Parameters)
                || !ReferenceEquals(s.Turn.Parameters,p.Space.Turn.Parameters) || !ReferenceEquals(s.Turn.Parameters,p.Risk.Military.Turn.Parameters))return RuleError.StaleContext;
            var error=SpaceEndTurnDiscardState.QueryOwner(p.Space.Track,out var owner);if(error!=RuleError.None)return error;
            var choice=p.SpaceDiscard;
            if(!owner.HasValue)return choice==null?RuleError.None:RuleError.StaleContext;
            if(choice==null || choice.Owner!=owner.Value || !ReferenceEquals(choice.Track,p.Space.Track)
                || !ReferenceEquals(choice.Audit,p.TurnEnd) || !ReferenceEquals(choice.Cards,s.Cards))return RuleError.StaleContext;
            return RuleError.None;
        }
        static RuleError SpaceDiscardContinuationError(GameState s)
        {
            var error=SpaceDiscardBindingError(s);if(error!=RuleError.None)return error;
            return s.Practice.SpaceDiscard?.Status==SpaceEndTurnDiscardStatus.Pending?RuleError.ActionNotReady:RuleError.None;
        }
        public static RuleError CanResolveSpaceDiscard(GameState state,PlayerSide side)
        {
            if(state==null || side!=PlayerSide.US && side!=PlayerSide.USSR)return RuleError.InvalidCommand;
            if(state.Ended)return RuleError.GameAlreadyEnded;
            var p=state.Practice;if(p==null || p.TurnEnd?.Decision.Outcome!=VictoryOutcome.Continue)return RuleError.ActionNotReady;
            var error=SpaceDiscardBindingError(state);if(error!=RuleError.None)return error;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            if(state.InfluenceEvent!=null)return RuleError.EventInProgress;
            if(p.Stage!=ActionPracticeStage.Complete || !p.Rounds.IsComplete || p.ActiveCard.HasValue
                || p.OpponentOps!=null || p.HeadlineSelection!=null || p.HasPendingHeadline || state.Cards.ResolvingCards.Count!=0 || state.Cards.HeadlineReserved.Count!=0)return RuleError.ActionNotReady;
            if(p.FinalScoring!=null || p.Risk.Result!=null || !ReferenceEquals(p.Risk.Military.Assessment,p.TurnEnd.Assessment)
                || p.VpForUS!=p.TurnEnd.Decision.SignedVp || p.OpsEffects.Instances.Count!=0 || state.Cards.China.Face!=CardFace.FaceUp
                || state.Operations.Budget!=0 || state.SpentOps!=0 || p.Risk.Military.Turn.Number!=state.Turn.Number
                || p.Risk.Military.UsEarned!=p.Risk.Definition.Military.ResetEarned || p.Risk.Military.UssrEarned!=p.Risk.Definition.Military.ResetEarned)return RuleError.StaleContext;
            var choice=p.SpaceDiscard;if(choice==null)return RuleError.ActionNotReady;
            if(side!=choice.Owner)return RuleError.WrongActor;
            return choice.Status==SpaceEndTurnDiscardStatus.Pending?RuleError.None:RuleError.ActionSliceComplete;
        }
        static CardActionTransition PlanSpaceDiscard(GameState state,PlayerSide side,CardActionKind kind,string actionId,CardId? card,CountryId? target)
        {
            var discard=kind==CardActionKind.DiscardHeldCard;
            if(string.IsNullOrWhiteSpace(actionId) || actionId.Length>128 || target.HasValue || discard!=card.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var error=CanResolveSpaceDiscard(state,side);if(error!=RuleError.None)return new CardActionTransition(error);
            var p=state.Practice;var choice=p.SpaceDiscard;
            if(discard && !choice.Candidates.Contains(card.Value))return new CardActionTransition(RuleError.InvalidSelection);
            try
            {
                var cards=state.Cards;
                if(discard)
                {
                    var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();
                    foreach(var owner in new[]{PlayerSide.US,PlayerSide.USSR})hands[owner]=owner==side?cards.GetHand(owner).Where(id=>!id.Equals(card.Value)).ToArray():cards.GetHand(owner);
                    cards=new CardZones(p.Definition.Cards,cards.DrawPile,hands,cards.EraReserve,cards.DiscardPile.Append(card.Value),cards.RemovedCards,cards.ResolvingCards,cards.China,cards.EnteredEras,cards.HeadlineReserved);
                }
                var next=new ActionPracticeState(p.Definition,p.Stage,p.CompletedActions,null,p.Space,p.VpForUS,p.Risk,p.OpsEffects,
                    turnEnd:p.TurnEnd,rounds:p.Rounds,headline:p.Headline,spaceDiscard:choice.Resolve(card,cards));
                return Commit(state,next,cards,state.RandomState);
            }
            catch(OverflowException){return new CardActionTransition(RuleError.NumericOverflow);}
        }
    }
}
