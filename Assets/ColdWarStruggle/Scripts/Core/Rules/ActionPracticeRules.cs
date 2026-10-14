using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        static RuleError Check(GameState s,PlayerSide side)
        {
            if(s==null || (side!=PlayerSide.US && side!=PlayerSide.USSR))return RuleError.InvalidCommand;
            if(s.Ended)return RuleError.GameAlreadyEnded;
            if(s.Practice==null)return RuleError.ActionNotReady;
            if(s.Practice.Stage==ActionPracticeStage.Complete)return RuleError.ActionSliceComplete;
            if(side!=s.Practice.Actor)return RuleError.WrongActor;
            if(s.InfluenceEvent!=null)return RuleError.EventInProgress;
            return RuleError.None;
        }
        public static RuleError CanPlay(GameState state,PlayerSide side,CardId card,CardActionKind kind)
        {
            var error=Check(state,side);if(error!=RuleError.None)return error;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            if(state.Practice.Stage!=ActionPracticeStage.ChooseCard)return RuleError.ActionNotReady;
            if(kind!=CardActionKind.Space && kind!=CardActionKind.Influence && kind!=CardActionKind.Coup)return RuleError.UnsupportedCardAction;
            if(kind==CardActionKind.Coup && state.Practice.Risk==null)return RuleError.UnsupportedCardAction;
            if(!state.Cards.GetHand(side).Contains(card))return RuleError.CardNotOwned;
            var definition=state.Practice.Definition.Cards.GetCard(card);
            if(definition.Kind!=CardKind.Event)return RuleError.UnsupportedCardAction;
            if(kind==CardActionKind.Influence || kind==CardActionKind.Coup)
            {
                var opponent=side==PlayerSide.US?CardAffiliation.USSR:CardAffiliation.US;
                return definition.Affiliation==opponent?RuleError.UnsupportedCardAction:RuleError.None;
            }
            var quote=OpsGeneralRules.QuoteCard(state.Practice.Definition.Ops,definition,side,OpsValuePurpose.Space,Array.Empty<OpsFlatModifier>());
            var request=SpaceAttemptRequest.FromQuote("query","query",state.Turn.Number,quote);
            return SpaceAttemptRules.ValidateAttempt(state.Practice.Space,request)==SpaceAttemptEligibility.Allowed?RuleError.None:RuleError.SpaceAttemptUnavailable;
        }
        public static CardActionTransition Plan(GameState state,PlayerSide side,CardActionKind kind,string actionId,CardId? card=null,CountryId? target=null)
        {
            if(kind==CardActionKind.Coup)return PlanCoup(state,side,actionId,card,target);
            if(target.HasValue)return new CardActionTransition(RuleError.InvalidCommand);
            if(string.IsNullOrWhiteSpace(actionId) || actionId.Length>128 || !Enum.IsDefined(typeof(CardActionKind),kind)
                || ((kind==CardActionKind.Influence || kind==CardActionKind.Space)!=card.HasValue))return new CardActionTransition(RuleError.InvalidCommand);
            var error=Check(state,side);if(error!=RuleError.None)return new CardActionTransition(error);
            var p=state.Practice;
            try
            {
                if(kind==CardActionKind.StartPractice)
                {
                    if(p.Stage!=ActionPracticeStage.AwaitingStart)return new CardActionTransition(RuleError.ActionNotReady);
                    if(state.Deployment==null || !state.Deployment.Complete)return new CardActionTransition(RuleError.SetupInProgress);
                    if(state.Cards.ResolvingCards.Count!=0)return new CardActionTransition(RuleError.ActionNotReady);
                    return Commit(state,new ActionPracticeState(p.Definition,ActionPracticeStage.ChooseCard,0,null,p.Space,p.VpForUS,p.Risk),state.Cards,state.RandomState);
                }
                if(kind==CardActionKind.FinishInfluence)
                {
                    if(state.Deployment!=null || p.Stage!=ActionPracticeStage.PlacingInfluence)return new CardActionTransition(RuleError.ActionNotReady);
                    var zones=Move(state,p.ActiveCard.Value,false,false);
                    return Commit(state,Next(p,p.Space,p.VpForUS),zones,state.RandomState);
                }
                error=CanPlay(state,side,card.Value,kind);if(error!=RuleError.None)return new CardActionTransition(error);
                var printed=p.Definition.Cards.GetCard(card.Value);
                if(kind==CardActionKind.Influence)
                {
                    var quote=OpsGeneralRules.QuoteCard(p.Definition.Ops,printed,side,OpsValuePurpose.Influence,Array.Empty<OpsFlatModifier>());
                    var zones=Move(state,card.Value,true,true);
                    return Commit(state,new ActionPracticeState(p.Definition,ActionPracticeStage.PlacingInfluence,p.CompletedActions,card,p.Space,p.VpForUS,p.Risk),zones,state.RandomState,quote.GeneralValue);
                }
                var spaceQuote=OpsGeneralRules.QuoteCard(p.Definition.Ops,printed,side,OpsValuePurpose.Space,Array.Empty<OpsFlatModifier>());
                // Scoped to the authoritative action slot, not a client supplied RNG or OPS value.
                var request=SpaceAttemptRequest.FromQuote("practice."+p.CompletedActions,"practice."+p.CompletedActions,state.Turn.Number,spaceQuote);
                var plan=SpaceAttemptRules.PlanAttempt(p.Space,request,new Pcg32Random(state.RandomState));
                if(plan.AlreadyApplied || !plan.ApplyCardDisposition)throw new InvalidOperationException("Action cursor does not match space receipts.");
                return Commit(state,Next(p,plan.After,checked(p.VpForUS+plan.VpDeltaToApply)),Move(state,card.Value,true,false),plan.RandomAfter,receipt:plan.Receipt);
            }
            catch(OverflowException) { return new CardActionTransition(RuleError.NumericOverflow); }
        }
        static ActionPracticeState Next(ActionPracticeState p,SpaceAttemptState space,int vp,PracticeRiskState risk=null)
        {
            var completed=checked(p.CompletedActions+1);
            var stage=completed==checked(p.Space.Turn.Parameters.NormalActionsPerSide*2)?ActionPracticeStage.Complete:ActionPracticeStage.ChooseCard;
            return new ActionPracticeState(p.Definition,stage,completed,null,space,vp,risk??p.Risk);
        }
        static CardActionTransition Commit(GameState s,ActionPracticeState p,CardZones cards,RandomSnapshot random,int budget=0,SpaceAttemptReceipt receipt=null)
        {
            var ops=OperationsContext.FromRoundStart(s.GameId+".action."+p.CompletedActions,p.Actor,budget,s.Map,s.Countries,s.Operations.Costs);
            return new CardActionTransition(RuleError.None,s.WithPractice(p,ops,cards,random),receipt);
        }
        static CardZones Move(GameState s,CardId id,bool fromHand,bool toResolving)
        {
            var cards=s.Cards;var actor=s.Practice.Actor;
            if(!fromHand && (cards.ResolvingCards.Count!=1 || !cards.ResolvingCards.Contains(id)))throw new InvalidOperationException("Missing active physical card.");
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})hands.Add(side,cards.GetHand(side).Where(c=>!(fromHand && side==actor && c.Equals(id))).ToArray());
            return new CardZones(s.Practice.Definition.Cards,cards.DrawPile,hands,cards.EraReserve,
                toResolving?cards.DiscardPile:cards.DiscardPile.Concat(new[]{id}),cards.RemovedCards,
                toResolving?cards.ResolvingCards.Concat(new[]{id}):cards.ResolvingCards.Where(c=>!c.Equals(id)),cards.China,cards.EnteredEras);
        }
    }
}
