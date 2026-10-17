using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public static partial class ActionPracticeRules
    {
        static IReadOnlyCollection<CardId> SupportedFiveYearPlanChildren(ActionPracticeDefinition d)=>d.Cards.Cards
            .Where(c=>c.Kind==CardKind.Event && c.Affiliation==CardAffiliation.US
                && (d.InfluenceEvents?.Effects.ContainsKey(c.Id)==true || d.DefconVpEvents?.Effects.ContainsKey(c.Id)==true || d.TurnOpsEvents?.Effects.ContainsKey(c.Id)==true || d.JapanPact?.Card.Equals(c.Id)==true))
            .Select(c=>c.Id).ToArray();
        static ActionPracticeState FiveYearState(ActionPracticeState p,FiveYearPlanExecution execution,PracticeRiskState risk=null,int? vp=null,TurnOpsEffectState ops=null,bool terminal=false)
            =>new ActionPracticeState(p.Definition,terminal?ActionPracticeStage.Complete:ActionPracticeStage.ResolvingEvent,p.CompletedActions,p.Definition.FiveYearPlan.Card.Id,
                p.Space,vp??p.VpForUS,risk??p.Risk,ops??p.OpsEffects,p.OpponentOps,rounds:p.Rounds,headline:p.Headline,fiveYearPlan:execution);
        static CardActionTransition BeginFiveYearPlan(GameState state,bool alreadyResolving)
        {
            try
            {
                var p=state.Practice;var d=p.Definition.FiveYearPlan;
                if(d==null || p.FiveYearPlan!=null || (alreadyResolving?(!HasActivePhysicalCard(state) || !Nullable.Equals(p.ActiveCard,(CardId?)d.Card.Id)):state.Cards.ResolvingCards.Count!=0))
                    return new CardActionTransition(RuleError.InvalidDefinition);
                var prepared=alreadyResolving?state:Commit(state,FiveYearState(p,null),Move(state,d.Card.Id,true,true),state.RandomState).NextState;
                var selection=FiveYearPlanRules.Plan(d,prepared.Cards,prepared.RandomState,SupportedFiveYearPlanChildren(p.Definition));
                var execution=new FiveYearPlanExecution(prepared,selection,EventResolutionId(prepared));
                var started=Commit(prepared,FiveYearState(prepared.Practice,execution),selection.Cards,selection.RandomAfter).NextState;
                if(selection.Route!=FiveYearPlanRoute.AwaitingUsEvent)return FinishFiveYearPlan(started);
                var child=selection.SelectedCard.Value;var current=started.Practice;
                if(p.Definition.JapanPact?.Card.Equals(child)==true)return PlanFiveYearJapanPact(started,execution);
                if(p.Definition.InfluenceEvents?.Effects.TryGetValue(child,out var influence)==true)
                {
                    var raw=InfluenceEventFlow.Begin(started.WithInfluenceEvent(new InfluenceEventExecution(execution.ChildResolutionId,influence,p.Actor,InfluenceEventPolicy.NoActiveInterference)),p.Actor);
                    var done=CompleteEventAction(raw);
                    return done.Accepted?new CardActionTransition(RuleError.None,done.NextState,eventTransition:raw,fiveYearPlanResult:done.FiveYearPlanResult):new CardActionTransition(done.Error);
                }
                if(p.Definition.DefconVpEvents?.Effects.TryGetValue(child,out var track)==true)
                {
                    var plan=DefconVpEventService.Plan(track,current.Risk.Defcon,current.Risk.Definition.Victory,current.VpForUS,PlayerSide.US,p.Actor,execution.ChildResolutionId,DefconVpEventPolicy.NoActiveInterference);
                    var risk=current.Risk.After(plan);var terminal=risk.Result?.Outcome==VictoryOutcome.Finished;
                    var changed=Commit(started,FiveYearState(current,execution,risk,plan.Decision.SignedVp,terminal:terminal),started.Cards,started.RandomState);
                    var done=terminal?new CardActionTransition(RuleError.None,changed.NextState,fiveYearPlanResult:new FiveYearPlanResult(execution,FiveYearPlanOutcome.Interrupted)):FinishFiveYearPlan(changed.NextState);
                    return done.Accepted?new CardActionTransition(RuleError.None,done.NextState,defconVpEvent:plan,fiveYearPlanResult:done.FiveYearPlanResult):done;
                }
                if(p.Definition.TurnOpsEvents?.Effects.TryGetValue(child,out var flat)==true)
                {
                    var activation=current.OpsEffects.Activate(flat,PlayerSide.US,execution.ChildResolutionId,checked(started.EventSequence+1),started.Turn);
                    if(!activation.Accepted)return new CardActionTransition(activation.Error);
                    if(activation.AlreadyApplied)return new CardActionTransition(RuleError.StaleContext);
                    var changed=Commit(started,FiveYearState(current,execution,ops:activation.After),started.Cards,started.RandomState);
                    var done=FinishFiveYearPlan(changed.NextState);
                    return done.Accepted?new CardActionTransition(RuleError.None,done.NextState,activatedOpsEffect:activation.Instance,fiveYearPlanResult:done.FiveYearPlanResult):done;
                }
                return new CardActionTransition(RuleError.UnsupportedCardAction);
            }
            catch(NotSupportedException){return new CardActionTransition(RuleError.UnsupportedCardAction);}
            catch(OverflowException){return new CardActionTransition(RuleError.NumericOverflow);}
            catch(ArgumentException){return new CardActionTransition(RuleError.InvalidDefinition);}
        }
        static InfluenceEventTransition CompleteFiveYearPlanInfluence(InfluenceEventTransition transition)
        {
            var s=transition.NextState;var p=s.Practice;var f=p.FiveYearPlan;var e=s.InfluenceEvent;
            if(f==null || !f.MatchesRoot(p) || !f.MatchesAuthority(s.GameId,s.Turn,s.Cards,s.RandomState) || f.RootResolutionId!=EventResolutionId(s)
                || f.Selection.Route!=FiveYearPlanRoute.AwaitingUsEvent || e==null || e.Id!=f.ChildResolutionId || e.PhasingPlayer!=p.Actor || e.Owner!=PlayerSide.US
                || e.Policy!=InfluenceEventPolicy.NoActiveInterference || p.Definition.InfluenceEvents?.Effects.TryGetValue(f.Selection.SelectedCard.Value,out var effect)!=true
                || !ReferenceEquals(e.Definition,effect))return new InfluenceEventTransition(RuleError.StaleContext);
            if(e.Stage==InfluenceEventStage.AwaitingChoice)return new InfluenceEventTransition(RuleError.None,next:s,changes:transition.Changes,decision:transition.DecisionRequestedSequence,
                resolution:transition.ResolutionSequence,fiveYearPlanResult:new FiveYearPlanResult(f,FiveYearPlanOutcome.AwaitingUsEvent));
            if(e.Stage!=InfluenceEventStage.Resolved)return new InfluenceEventTransition(RuleError.EventNotStarted);
            try
            {
                var done=FinishFiveYearPlan(s.WithInfluenceEvent(null));
                return done.Accepted?new InfluenceEventTransition(RuleError.None,next:done.NextState,changes:transition.Changes,decision:transition.DecisionRequestedSequence,
                    resolution:transition.ResolutionSequence,fiveYearPlanResult:done.FiveYearPlanResult):new InfluenceEventTransition(done.Error);
            }
            catch(OverflowException){return new InfluenceEventTransition(RuleError.NumericOverflow);}
            catch(ArgumentException){return new InfluenceEventTransition(RuleError.InvalidDefinition);}
        }
        static CardActionTransition FinishFiveYearPlan(GameState state)
        {
            var p=state.Practice;var f=p.FiveYearPlan;
            if(f==null || !f.MatchesRoot(p) || !f.MatchesAuthority(state.GameId,state.Turn,state.Cards,state.RandomState) || state.Ended)
                return new CardActionTransition(RuleError.StaleContext);
            var cards=state.Cards;var child=f.Selection.Route==FiveYearPlanRoute.AwaitingUsEvent;
            if(child)
            {
                var printed=p.Definition.Cards.GetCard(f.Selection.SelectedCard.Value);var remove=printed.RemoveAfterResolvedEvent;
                var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,cards.GetHand(PlayerSide.US)},{PlayerSide.USSR,cards.GetHand(PlayerSide.USSR)}};
                cards=new CardZones(cards.Catalog,cards.DrawPile,hands,cards.EraReserve,remove?cards.DiscardPile:cards.DiscardPile.Append(printed.Id),
                    remove?cards.RemovedCards.Append(printed.Id):cards.RemovedCards,cards.ResolvingCards.Where(id=>!id.Equals(printed.Id)),cards.China,cards.EnteredEras,cards.HeadlineReserved);
            }
            var cleared=Commit(state,FiveYearState(p,null),cards,state.RandomState).NextState;
            var done=p.OpponentOps!=null?AdvanceOpponentStep(cleared,CardResolutionStep.Event)
                :Commit(cleared,Next(cleared.Practice,p.Space,p.VpForUS),Move(cleared,f.Definition.Card.Id,false,false),cleared.RandomState);
            var outcome=child?FiveYearPlanOutcome.UsEventResolved:f.Selection.Route==FiveYearPlanRoute.EmptyHand?FiveYearPlanOutcome.NoCard:FiveYearPlanOutcome.Discarded;
            return done.Accepted?new CardActionTransition(RuleError.None,done.NextState,fiveYearPlanResult:new FiveYearPlanResult(f,outcome)):done;
        }
    }
}
