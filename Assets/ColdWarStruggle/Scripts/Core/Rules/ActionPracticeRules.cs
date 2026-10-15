using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        public static OpsGeneralQuote QuoteCard(GameState state,PlayerSide side,CardId card,OpsValuePurpose purpose)
        {
            if(state?.Practice==null)throw new ArgumentException("OPS quote needs a real practice state.");
            var p=state.Practice;
            return OpsGeneralRules.QuoteCard(p.Definition.Ops,p.Definition.Cards.GetCard(card),side,purpose,p.OpsEffects.GetModifiers(state.Turn));
        }
        static RuleError Check(GameState s,PlayerSide side)
        {
            if(s==null || (side!=PlayerSide.US && side!=PlayerSide.USSR))return RuleError.InvalidCommand;
            if(s.Ended)return RuleError.GameAlreadyEnded;
            if(s.Practice==null)return RuleError.ActionNotReady;
            if(s.Cards.HeadlineReserved.Count!=0 || s.Practice.HasPendingHeadline || s.Practice.HeadlineSelection!=null)return RuleError.ActionNotReady;
            if(s.Practice.Stage==ActionPracticeStage.Complete)return RuleError.ActionSliceComplete;
            if(side!=s.Practice.Actor)return RuleError.WrongActor;
            if(s.InfluenceEvent!=null)return RuleError.EventInProgress;
            return RuleError.None;
        }
        public static RuleError CanPlay(GameState state,PlayerSide side,CardId card,CardActionKind kind)
        {
            if(kind==CardActionKind.EventThenInfluence || kind==CardActionKind.InfluenceThenEvent || kind==CardActionKind.OpponentEventFirst || kind==CardActionKind.OpponentOpsFirst)return CanPlayOpponentOps(state,side,card);
            var error=Check(state,side);if(error!=RuleError.None)return error;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            if(state.Practice.Stage!=ActionPracticeStage.ChooseCard)return RuleError.ActionNotReady;
            if(kind!=CardActionKind.Space && kind!=CardActionKind.Influence && kind!=CardActionKind.Coup && kind!=CardActionKind.Scoring && kind!=CardActionKind.Event && kind!=CardActionKind.Realignment)return RuleError.UnsupportedCardAction;
            if(kind==CardActionKind.Realignment && state.Practice.Definition.Realignment==null)return RuleError.UnsupportedCardAction;
            if(kind==CardActionKind.Coup && state.Practice.Risk==null)return RuleError.UnsupportedCardAction;
            if(kind==CardActionKind.Scoring && state.Practice.Definition.Scoring==null)return RuleError.UnsupportedCardAction;
            if(card.Equals(state.Cards.China.Id))
            {
                if(kind!=CardActionKind.Influence && kind!=CardActionKind.Coup && kind!=CardActionKind.Realignment && kind!=CardActionKind.Space)return RuleError.UnsupportedCardAction;
                var eligibility=ChinaCardRules.CanUse(state.Practice.Definition.China,state.Cards,side,state.Practice.Space.Turn.Parameters.NormalActionsPerSide-state.Practice.CompletedActions/2);
                if(eligibility!=RuleError.None)return eligibility;
                return kind==CardActionKind.Space?CanUseSpace(state,side,card):RuleError.None;
            }
            if(!state.Cards.GetHand(side).Contains(card))return RuleError.CardNotOwned;
            var definition=state.Practice.Definition.Cards.GetCard(card);
            if(kind==CardActionKind.Event)return definition.Kind==CardKind.Event && (state.Practice.Definition.InfluenceEvents?.Effects.ContainsKey(card)==true || state.Practice.Definition.DefconVpEvents?.Effects.ContainsKey(card)==true || state.Practice.Definition.TurnOpsEvents?.Effects.ContainsKey(card)==true)?RuleError.None:RuleError.UnsupportedCardAction;
            if(kind==CardActionKind.Scoring)return definition.Kind==CardKind.Scoring?RuleError.None:RuleError.UnsupportedCardAction;
            if(definition.Kind!=CardKind.Event)return RuleError.UnsupportedCardAction;
            if(kind==CardActionKind.Influence || kind==CardActionKind.Coup || kind==CardActionKind.Realignment)
            {
                var opponent=side==PlayerSide.US?CardAffiliation.USSR:CardAffiliation.US;
                return definition.Affiliation==opponent?RuleError.UnsupportedCardAction:RuleError.None;
            }
            return CanUseSpace(state,side,card);
        }
        static RuleError CanUseSpace(GameState state,PlayerSide side,CardId card)
        {
            var quote=QuoteCard(state,side,card,OpsValuePurpose.Space);
            var request=SpaceAttemptRequest.FromQuote("query","query",state.Turn.Number,quote);
            return SpaceAttemptRules.ValidateAttempt(state.Practice.Space,request)==SpaceAttemptEligibility.Allowed?RuleError.None:RuleError.SpaceAttemptUnavailable;
        }
        public static CardActionTransition Plan(GameState state,PlayerSide side,CardActionKind kind,string actionId,CardId? card=null,CountryId? target=null)
        {
            if(kind==CardActionKind.AssessTurnEnd)return PlanTurnEnd(state,side,actionId,card,target);
            if(kind==CardActionKind.BeginNextTurn)return PlanNextTurn(state,side,actionId,card,target);
            if(kind==CardActionKind.EventThenInfluence || kind==CardActionKind.InfluenceThenEvent || kind==CardActionKind.OpponentEventFirst || kind==CardActionKind.OpponentOpsFirst)return PlanOpponentInfluence(state,side,kind,actionId,card,target);
            if(kind==CardActionKind.UseOpponentInfluence || kind==CardActionKind.UseOpponentRealignment || kind==CardActionKind.UseOpponentCoup)return PlanOpponentOperations(state,side,kind,actionId,card,target);
            if(kind==CardActionKind.Realignment || kind==CardActionKind.RollRealignment || kind==CardActionKind.FinishRealignment)return PlanRealignment(state,side,kind,actionId,card,target);
            if(kind==CardActionKind.Event)return PlanEvent(state,side,actionId,card,target);
            if(kind==CardActionKind.Scoring)return PlanScoring(state,side,actionId,card,target);
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
                    return CollectHeadlinesIfEnabled(Commit(state,new ActionPracticeState(p.Definition,ActionPracticeStage.ChooseCard,0,null,p.Space,p.VpForUS,p.Risk,p.OpsEffects,headline:p.Headline),state.Cards,state.RandomState));
                }
                if(kind==CardActionKind.FinishInfluence)
                {
                    if(state.Deployment!=null || p.Stage!=ActionPracticeStage.PlacingInfluence)return new CardActionTransition(RuleError.ActionNotReady);
                    if(p.OpponentOps!=null)return AdvanceOpponentStep(state,CardResolutionStep.Operations);
                    var zones=Move(state,p.ActiveCard.Value,false,false);
                    return Commit(state,Next(p,p.Space,p.VpForUS),zones,state.RandomState);
                }
                error=CanPlay(state,side,card.Value,kind);if(error!=RuleError.None)return new CardActionTransition(error);
                var printed=p.Definition.Cards.GetCard(card.Value);
                if(kind==CardActionKind.Influence)
                {
                    var quote=QuoteCard(state,side,card.Value,OpsValuePurpose.Influence);
                    var regional=RegionalBudget(state,quote);
                    var zones=Move(state,card.Value,true,true);
                    return Commit(state,new ActionPracticeState(p.Definition,ActionPracticeStage.PlacingInfluence,p.CompletedActions,card,p.Space,p.VpForUS,p.Risk,p.OpsEffects,headline:p.Headline),zones,state.RandomState,regional?.EffectiveBudget??quote.GeneralValue,regionalBudget:regional);
                }
                var spaceQuote=QuoteCard(state,side,card.Value,OpsValuePurpose.Space);
                // Scoped to the authoritative action slot, not a client supplied RNG or OPS value.
                var request=SpaceAttemptRequest.FromQuote(PracticeSlotId(state),PracticeSlotId(state),state.Turn.Number,spaceQuote);
                var plan=SpaceAttemptRules.PlanAttempt(p.Space,request,new Pcg32Random(state.RandomState));
                if(plan.AlreadyApplied || !plan.ApplyCardDisposition)throw new InvalidOperationException("Action cursor does not match space receipts.");
                var vp=checked(p.VpForUS+plan.VpDeltaToApply);var risk=p.Risk;
                if(risk!=null)
                {
                    var decision=VictoryService.Evaluate(risk.Definition.Victory,new ClosedVpBatchEvidence(request.Id,printed.Id.Value,p.VpForUS,Math.Max(0,plan.VpDeltaToApply),checked((int)Math.Max(0L,-(long)plan.VpDeltaToApply))));
                    risk=risk.WithVictory(decision);vp=decision.SignedVp;
                }
                var next=AfterImmediate(p,card.Value,plan.After,vp,risk);
                return Commit(state,next,Move(state,card.Value,true,risk?.Result?.Outcome==VictoryOutcome.Finished),plan.RandomAfter,receipt:plan.Receipt);
            }
            catch(OverflowException) { return new CardActionTransition(RuleError.NumericOverflow); }
        }
        static ActionPracticeState Next(ActionPracticeState p,SpaceAttemptState space,int vp,PracticeRiskState risk=null,TurnOpsEffectState opsEffects=null)
        {
            if(p.HasPendingHeadline)
            {
                var queue=p.Headline.AdvanceAfterResolution(p.ActiveCard??p.Headline.Current.Card,p.Actor);
                return new ActionPracticeState(p.Definition,queue.IsComplete?ActionPracticeStage.ChooseCard:ActionPracticeStage.HeadlineReady,
                    p.CompletedActions,null,space,vp,risk??p.Risk,opsEffects??p.OpsEffects,headline:queue);
            }
            var completed=checked(p.CompletedActions+1);
            var stage=completed==checked(p.Space.Turn.Parameters.NormalActionsPerSide*2)?ActionPracticeStage.Complete:ActionPracticeStage.ChooseCard;
            return new ActionPracticeState(p.Definition,stage,completed,null,space,vp,risk??p.Risk,opsEffects??p.OpsEffects,headline:p.Headline);
        }
        static CardActionTransition Commit(GameState s,ActionPracticeState p,CardZones cards,RandomSnapshot random,int budget=0,SpaceAttemptReceipt receipt=null,RegionScoringResult scoring=null,OpsRegionalBudget regionalBudget=null)
        {
            var ops=OperationsContext.FromRoundStart(p.HasPendingHeadline?s.GameId+"."+p.Headline.CurrentResolutionId:PracticeOperationsId(s,p.CompletedActions),p.Actor,budget,s.Map,s.Countries,s.Operations.Costs,regionalBudget);
            return new CardActionTransition(RuleError.None,s.WithPractice(p,ops,cards,random,ended:p.Risk?.Result?.Outcome==VictoryOutcome.Finished),receipt,scoring:scoring);
        }
        static OpsRegionalBudget RegionalBudget(GameState state,OpsGeneralQuote quote)
        {
            var china=state.Practice.Definition.China;
            return china!=null && quote.Card!=null && quote.Card.Id.Equals(china.Card)?new OpsRegionalBudget(state.Map,quote,new[]{china.Bonus}):null;
        }
        static bool HasActivePhysicalCard(GameState state)
        {
            var p=state.Practice;if(!p.ActiveCard.HasValue)return false;
            if(p.ActiveCard.Value.Equals(state.Cards.China.Id))return p.Definition.China!=null && state.Cards.ResolvingCards.Count==0
                && state.Cards.China.Face==CardFace.FaceDown && state.Cards.China.Owner==(p.Actor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US);
            return state.Cards.ResolvingCards.Count==1 && state.Cards.ResolvingCards.Contains(p.ActiveCard.Value);
        }
        enum PhysicalCardSource { Hand, HeadlineReserved, Resolving }
        static CardZones Move(GameState s,CardId id,bool fromHand,bool toResolving,bool remove=false)
        {
            if(id.Equals(s.Cards.China.Id))
            {
                if(remove)throw new ArgumentException("China cannot be removed as an event.");
                if(fromHand)return ChinaCardRules.TransferOnUse(s.Practice.Definition.China,s.Cards,s.Practice.Actor);
                if(!HasActivePhysicalCard(s))throw new InvalidOperationException("Missing active China reference.");
                return s.Cards;
            }
            if(toResolving && remove)throw new ArgumentException("A card cannot resolve and be removed together.");
            var cards=s.Cards;var actor=s.Practice.Actor;
            var source=fromHand?(s.Practice.HasPendingHeadline?PhysicalCardSource.HeadlineReserved:PhysicalCardSource.Hand):PhysicalCardSource.Resolving;
            if(source==PhysicalCardSource.HeadlineReserved && (s.Practice.Stage!=ActionPracticeStage.HeadlineReady
                || !s.Practice.Headline.Current.Card.Equals(id) || cards.GetHeadline(actor)?.Equals(id)!=true || cards.ResolvingCards.Count!=0))throw new ArgumentException("Missing current reserved headline.");
            if(source==PhysicalCardSource.Hand && !cards.GetHand(actor).Contains(id))throw new ArgumentException("Missing physical hand card.");
            if(!fromHand && (cards.ResolvingCards.Count!=1 || !cards.ResolvingCards.Contains(id)))throw new InvalidOperationException("Missing active physical card.");
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})hands.Add(side,cards.GetHand(side).Where(c=>!(source==PhysicalCardSource.Hand && side==actor && c.Equals(id))).ToArray());
            return new CardZones(s.Practice.Definition.Cards,cards.DrawPile,hands,cards.EraReserve,
                toResolving || remove?cards.DiscardPile:cards.DiscardPile.Concat(new[]{id}),remove?cards.RemovedCards.Concat(new[]{id}):cards.RemovedCards,
                toResolving?cards.ResolvingCards.Concat(new[]{id}):cards.ResolvingCards.Where(c=>!c.Equals(id)),cards.China,cards.EnteredEras,source==PhysicalCardSource.HeadlineReserved?cards.HeadlineReserved.Where(x=>x.Key!=actor).ToDictionary(x=>x.Key,x=>x.Value):cards.HeadlineReserved);
        }
    }
}
