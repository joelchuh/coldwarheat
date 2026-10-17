using System;
using System.Collections.Generic;
using System.Linq;

namespace ColdWarStruggle.Core
{
    public sealed class InfluenceEventTransition
    {
        public FiveYearPlanResult FiveYearPlanResult { get; }
        public RuleError Error { get; }
        public InfluenceEventError EffectError { get; }
        public bool Accepted=>Error==RuleError.None;
        public GameState NextState { get; }
        public IReadOnlyList<InfluenceChanged> Changes { get; }
        public long? DecisionRequestedSequence { get; }
        public long? ResolutionSequence { get; }
        internal InfluenceEventTransition(RuleError error,InfluenceEventError effectError=InfluenceEventError.None,
            GameState next=null,IEnumerable<InfluenceChanged> changes=null,long? decision=null,long? resolution=null,FiveYearPlanResult fiveYearPlanResult=null)
        {
            FiveYearPlanResult=fiveYearPlanResult;Error=error;EffectError=effectError;NextState=next;Changes=Array.AsReadOnly((changes??Enumerable.Empty<InfluenceChanged>()).ToArray());
            DecisionRequestedSequence=decision;ResolutionSequence=resolution;
        }
    }
    public static class InfluenceEventFlow
    {
        public static InfluenceEventQuery QueryDecision(GameState state,PlayerSide actor,out RuleError error)
        {
            error=Check(state);if(error!=RuleError.None)return null;
            var execution=state.InfluenceEvent;
            if(execution.Stage!=InfluenceEventStage.AwaitingChoice)error=RuleError.EventNotStarted;
            else if(actor!=execution.Owner)error=RuleError.WrongActor;
            else if(state.Version!=execution.DecisionSourceVersion)error=RuleError.StaleDecision;
            if(error!=RuleError.None)return null;
            var query=InfluenceEventRules.Query(state,execution.Definition,execution.Policy);
            if(query.Error==InfluenceEventError.None)return query;
            error=MapError(query.Error);return null;
        }
        public static InfluenceEventTransition Begin(GameState state,PlayerSide actor)
        {
            var error=Check(state);if(error!=RuleError.None)return new InfluenceEventTransition(error);
            var execution=state.InfluenceEvent;
            if(execution.Stage!=InfluenceEventStage.Ready)return new InfluenceEventTransition(RuleError.EventInProgress);
            if(actor!=execution.PhasingPlayer)return new InfluenceEventTransition(RuleError.WrongActor);
            var plan=InfluenceEventRules.Plan(state,execution.Definition,null,execution.Policy);
            if(plan.Resolved)return Complete(execution,plan);
            if(plan.Status!=InfluenceEventStatus.AwaitingChoice)return new InfluenceEventTransition(MapError(plan.Error),plan.Error);
            try
            {
                var next=state.Apply(state.Countries,state.SpentOps,1);
                next=next.WithInfluenceEvent(execution.Wait(next.Version));
                return new InfluenceEventTransition(RuleError.None,next:next,decision:next.EventSequence);
            }
            catch(OverflowException) { return new InfluenceEventTransition(RuleError.NumericOverflow); }
        }
        public static InfluenceEventTransition Resolve(GameState state,PlayerSide actor,string decisionId,IReadOnlyList<CountryId> targets)
            =>ResolveSelected(state,actor,decisionId,targets,null,InfluenceDecisionKind.CountrySet);
        public static InfluenceEventTransition ResolveAllocation(GameState state,PlayerSide actor,string decisionId,IReadOnlyList<InfluenceAllocation> allocations)
            =>ResolveSelected(state,actor,decisionId,null,allocations,InfluenceDecisionKind.Allocation);
        public static InfluenceEventTransition ResolveRelocation(GameState state,PlayerSide actor,string decisionId,IReadOnlyList<InfluenceAllocation> sources,IReadOnlyList<InfluenceAllocation> destinations)
            =>ResolveSelected(state,actor,decisionId,null,null,InfluenceDecisionKind.Relocation,sources,destinations);
        static InfluenceEventTransition ResolveSelected(GameState state,PlayerSide actor,string decisionId,
            IReadOnlyList<CountryId> targets,IReadOnlyList<InfluenceAllocation> allocations,InfluenceDecisionKind kind,
            IReadOnlyList<InfluenceAllocation> sources=null,IReadOnlyList<InfluenceAllocation> destinations=null)
        {
            var error=Check(state);if(error!=RuleError.None)return new InfluenceEventTransition(error);
            var execution=state.InfluenceEvent;
            if(execution.Stage!=InfluenceEventStage.AwaitingChoice)return new InfluenceEventTransition(RuleError.EventNotStarted);
            if(actor!=execution.Owner)return new InfluenceEventTransition(RuleError.WrongActor);
            if(decisionId!=execution.DecisionId || state.Version!=execution.DecisionSourceVersion)return new InfluenceEventTransition(RuleError.StaleDecision);
            var selection=kind==InfluenceDecisionKind.Relocation?InfluenceEventSelection.Relocate(actor,state.Version,sources,destinations)
                :kind==InfluenceDecisionKind.Allocation?InfluenceEventSelection.Allocate(actor,state.Version,allocations)
                :new InfluenceEventSelection(actor,state.Version,targets);
            var plan=InfluenceEventRules.Plan(state,execution.Definition,selection,execution.Policy);
            return plan.Resolved?Complete(execution,plan):new InfluenceEventTransition(MapError(plan.Error),plan.Error);
        }
        static RuleError Check(GameState state)
        {
            if(state==null)return RuleError.InvalidCommand;
            if(state.Ended)return RuleError.GameAlreadyEnded;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            if(state.InfluenceEvent==null)return RuleError.EventNotAvailable;
            if(state.InfluenceEvent.Stage==InfluenceEventStage.Resolved)return RuleError.EventAlreadyResolved;
            return RuleError.None;
        }
        static InfluenceEventTransition Complete(InfluenceEventExecution execution,InfluenceEventPlan plan)
            =>new InfluenceEventTransition(RuleError.None,next:plan.NextState.WithInfluenceEvent(execution.Complete(plan.Status)),changes:plan.Changes,resolution:plan.ResolutionSequence);
        internal static RuleError MapError(InfluenceEventError error)
        {
            switch(error)
            {
                case InfluenceEventError.InvalidDefinition:return RuleError.InvalidDefinition;
                case InfluenceEventError.MissingTurnContext:return RuleError.MissingTurnContext;
                case InfluenceEventError.SetupInProgress:return RuleError.SetupInProgress;
                case InfluenceEventError.InvalidSelection:return RuleError.InvalidSelection;
                case InfluenceEventError.WrongDecisionOwner:return RuleError.WrongActor;
                case InfluenceEventError.StaleSelection:return RuleError.StaleVersion;
                case InfluenceEventError.NumericOverflow:return RuleError.NumericOverflow;
                case InfluenceEventError.GameAlreadyEnded:return RuleError.GameAlreadyEnded;
                case InfluenceEventError.InterferenceUnknown:case InfluenceEventError.CandidateShortage:case InfluenceEventError.RelocationOverlap:return RuleError.EventRequiresRuling;
                case InfluenceEventError.Forbidden:return RuleError.EventForbidden;
                default:return RuleError.InvalidCommand;
            }
        }
    }
}
