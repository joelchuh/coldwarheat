using System;
using System.Linq;
using System.Collections.Generic;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        public static RuleError CanRespondToBlockade(GameState state,PlayerSide side)
        {
            if(state==null || side!=PlayerSide.US && side!=PlayerSide.USSR)return RuleError.InvalidCommand;
            if(state.Ended)return RuleError.GameAlreadyEnded;
            var p=state.Practice;var choice=p?.Blockade;if(choice==null)return RuleError.ActionNotReady;
            if(side!=choice.Owner)return RuleError.WrongActor;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            if(state.InfluenceEvent!=null)return RuleError.EventInProgress;
            if(p.Stage!=ActionPracticeStage.ResolvingEvent || !p.ActiveCard.HasValue || !p.ActiveCard.Value.Equals(choice.Definition.Card.Id)
                || !ReferenceEquals(p.Definition.Blockade,choice.Definition) || !ReferenceEquals(state.Map,choice.Definition.Map)
                || !ReferenceEquals(state.Cards,choice.Cards) || !ReferenceEquals(state.Cards.Catalog,p.Definition.Cards)
                || !ReferenceEquals(p.OpsEffects,choice.OpsEffects) || !ReferenceEquals(state.Turn.Parameters,choice.Turn.Parameters)
                || state.Turn.Number!=choice.Turn.Number || state.Turn.Era!=choice.Turn.Era
                || state.GameId!=choice.GameId || state.Version!=choice.ExpectedVersion || choice.Id!=EventResolutionId(state)
                || state.Cards.ResolvingCards.Count!=1 || !state.Cards.ResolvingCards.Contains(p.ActiveCard.Value)
                || state.Operations.Budget!=0 || state.SpentOps!=0
                || p.OpponentOps!=null && (!ValidOpponentRoot(state) || p.OpponentOps.CurrentStep!=CardResolutionStep.Event))return RuleError.StaleContext;
            return RuleError.None;
        }
        static CardActionTransition BeginBlockade(GameState state,bool alreadyResolving)
        {
            try
            {
                var p=state.Practice;var d=p.Definition.Blockade;
                if(alreadyResolving?(!HasActivePhysicalCard(state) || !p.ActiveCard.Value.Equals(d.Card.Id)):state.Cards.ResolvingCards.Count!=0)
                    return new CardActionTransition(RuleError.InvalidDefinition);
                var cards=alreadyResolving?state.Cards:Move(state,d.Card.Id,true,true);
                var choice=new BlockadeDecision(d,cards,p.OpsEffects,state.Turn,EventResolutionId(state),state.GameId,checked(state.Version+1));
                var pending=new ActionPracticeState(p.Definition,ActionPracticeStage.ResolvingEvent,p.CompletedActions,d.Card.Id,p.Space,p.VpForUS,p.Risk,p.OpsEffects,p.OpponentOps,rounds:p.Rounds,headline:p.Headline,blockade:choice);
                var prepared=Commit(state,pending,cards,state.RandomState).NextState;
                return choice.Candidates.Count==0?CompleteBlockade(prepared,null)
                    :new CardActionTransition(RuleError.None,prepared,blockadeResult:new BlockadeResolutionResult(choice,BlockadeOutcome.AwaitingChoice));
            }
            catch(OverflowException){return new CardActionTransition(RuleError.NumericOverflow);}
            catch(ArgumentException){return new CardActionTransition(RuleError.InvalidDefinition);}
        }
        static CardActionTransition PlanBlockadeResponse(GameState state,PlayerSide side,CardActionKind kind,string actionId,CardId? card,CountryId? target)
        {
            var discard=kind==CardActionKind.DiscardForBlockade;
            if(string.IsNullOrWhiteSpace(actionId) || actionId.Length>128 || target.HasValue || discard!=card.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            var error=CanRespondToBlockade(state,side);if(error!=RuleError.None)return new CardActionTransition(error);
            if(discard && !state.Practice.Blockade.Candidates.Any(q=>q.Card.Id.Equals(card.Value)))return new CardActionTransition(RuleError.InvalidSelection);
            try
            {
                var done=CompleteBlockade(state,card);
                if(!done.Accepted || !state.Practice.HasPendingHeadline)return done;
                var entry=state.Practice.Headline.Current;
                var steps=new List<HeadlineResolutionStep>{new HeadlineResolutionStep(entry.Card,entry.PlayedBy,done)};
                var next=done.NextState;
                if(!next.Ended && next.Practice.HasPendingHeadline)
                {
                    var following=HeadlineResolutionRules.Advance(next);
                    if(!following.Accepted)return new CardActionTransition(following.Error);
                    next=following.NextState;steps.AddRange(following.Steps);
                }
                return new CardActionTransition(RuleError.None,next,blockadeResult:done.BlockadeResult,headlineSteps:steps);
            }
            catch(OverflowException){return new CardActionTransition(RuleError.NumericOverflow);}
            catch(ArgumentException){return new CardActionTransition(RuleError.InvalidDefinition);}
        }
        // All drafts stay inside the host's transaction, including the following headline.
        static CardActionTransition CompleteBlockade(GameState state,CardId? discard)
        {
            var p=state.Practice;var choice=p.Blockade;
            var cards=discard.HasValue?HandDiscardRules.DiscardEventCard(state.Cards,choice.Owner,discard.Value):state.Cards;
            var cleared=new ActionPracticeState(p.Definition,ActionPracticeStage.ResolvingEvent,p.CompletedActions,p.ActiveCard,p.Space,p.VpForUS,p.Risk,p.OpsEffects,p.OpponentOps,rounds:p.Rounds,headline:p.Headline);
            var current=Commit(state,cleared,cards,state.RandomState).NextState;
            IReadOnlyList<InfluenceChanged> changes=Array.Empty<InfluenceChanged>();
            if(!discard.HasValue)
            {
                var mutation=new InfluenceMutation(current);mutation.RemoveAll(choice.Definition.FallbackCountry,choice.Owner);
                current=mutation.Commit();changes=mutation.Changes;
            }
            var done=p.OpponentOps!=null?AdvanceOpponentStep(current,CardResolutionStep.Event)
                :Commit(current,Next(current.Practice,current.Practice.Space,current.Practice.VpForUS),Move(current,choice.Definition.Card.Id,false,false,choice.Definition.Card.RemoveAfterResolvedEvent),current.RandomState);
            if(!done.Accepted)return done;
            var result=new BlockadeResolutionResult(choice,discard.HasValue?BlockadeOutcome.Discarded:BlockadeOutcome.InfluenceRemoved,discard,changes);
            return new CardActionTransition(RuleError.None,done.NextState,blockadeResult:result);
        }
    }
}
