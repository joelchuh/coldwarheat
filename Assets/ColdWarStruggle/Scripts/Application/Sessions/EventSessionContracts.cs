using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;

namespace ColdWarStruggle.Application
{
    public abstract class SessionCommand
    {
        public string CommandId { get; }
        public string GameId { get; }
        public long ExpectedVersion { get; }
        protected SessionCommand(string commandId,string gameId,long expectedVersion)
        { CommandId=commandId;GameId=gameId;ExpectedVersion=expectedVersion; }
        internal abstract bool SameContent(SessionCommand other);
    }
    public enum EventCommandKind { Begin, Resolve, ResolveAllocation, ResolveRelocation }
    public sealed class EventCommandEnvelope : SessionCommand
    {
        public EventCommandKind Kind { get; }
        public string ExecutionId { get; }
        public string DecisionId { get; }
        public IReadOnlyList<CountryId> Targets { get; }
        public IReadOnlyList<InfluenceAllocation> Allocations { get; }
        public IReadOnlyList<InfluenceAllocation> Sources { get; }=Array.AsReadOnly(Array.Empty<InfluenceAllocation>());
        public IReadOnlyList<InfluenceAllocation> Destinations { get; }=Array.AsReadOnly(Array.Empty<InfluenceAllocation>());
        public static EventCommandEnvelope Relocate(string id,string game,long version,string executionId,string decisionId,IEnumerable<InfluenceAllocation> sources,IEnumerable<InfluenceAllocation> destinations)
            =>new EventCommandEnvelope(id,game,version,executionId,decisionId,sources,destinations);
        private EventCommandEnvelope(string id,string game,long version,string executionId,string decisionId,IEnumerable<InfluenceAllocation> sources,IEnumerable<InfluenceAllocation> destinations)
            :base(id,game,version)
        {
            Kind=EventCommandKind.ResolveRelocation;ExecutionId=executionId;DecisionId=decisionId;
            Targets=Array.AsReadOnly(Array.Empty<CountryId>());Allocations=Array.AsReadOnly(Array.Empty<InfluenceAllocation>());
            Sources=sources==null?null:Array.AsReadOnly(sources.Take(InfluenceEventSelection.MaxTargets+1).ToArray());
            Destinations=destinations==null?null:Array.AsReadOnly(destinations.Take(InfluenceEventSelection.MaxTargets+1).ToArray());
        }
        public EventCommandEnvelope(string id,string game,long version,EventCommandKind kind,string executionId,string decisionId=null,IEnumerable<CountryId> targets=null)
            : base(id,game,version)
        {
            Kind=kind;ExecutionId=executionId;DecisionId=decisionId;
            // The older single-choice constructor cannot imply a valid zero relocation.
            if(kind==EventCommandKind.ResolveRelocation){Sources=null;Destinations=null;}
            Targets=Array.AsReadOnly((targets??Enumerable.Empty<CountryId>()).Take(InfluenceEventSelection.MaxTargets+1).ToArray());
            Allocations=Array.AsReadOnly(Array.Empty<InfluenceAllocation>());
        }
        private EventCommandEnvelope(string id,string game,long version,string executionId,string decisionId,IEnumerable<InfluenceAllocation> allocations)
            : base(id,game,version)
        {
            Kind=EventCommandKind.ResolveAllocation;ExecutionId=executionId;DecisionId=decisionId;
            Targets=Array.AsReadOnly(Array.Empty<CountryId>());
            Allocations=Array.AsReadOnly((allocations??Enumerable.Empty<InfluenceAllocation>()).Take(InfluenceEventSelection.MaxTargets+1).ToArray());
        }
        public static EventCommandEnvelope Allocate(string id,string game,long version,string executionId,string decisionId,IEnumerable<InfluenceAllocation> allocations)
            =>new EventCommandEnvelope(id,game,version,executionId,decisionId,allocations);
        static bool SameAllocations(IReadOnlyList<InfluenceAllocation> left,IReadOnlyList<InfluenceAllocation> right)
            =>left==null?right==null:right!=null && left.SequenceEqual(right);
        internal override bool SameContent(SessionCommand other)=>other is EventCommandEnvelope e
            && GameId==e.GameId && ExpectedVersion==e.ExpectedVersion && Kind==e.Kind && ExecutionId==e.ExecutionId
            && DecisionId==e.DecisionId && Targets.SequenceEqual(e.Targets) && Allocations.SequenceEqual(e.Allocations)
            && SameAllocations(Sources,e.Sources) && SameAllocations(Destinations,e.Destinations);
    }
    public sealed class PublicEventView
    {
        public string ExecutionId { get; }
        public string CardId { get; }
        public PlayerSide PhasingPlayer { get; }
        public PlayerSide Owner { get; }
        public InfluenceEventStage Stage { get; }
        public string DecisionId { get; }
        public long? DecisionSourceVersion { get; }
        public InfluenceEventStatus? Outcome { get; }
        internal PublicEventView(InfluenceEventExecution e)
        {
            ExecutionId=e.Id;CardId=e.Definition.CardId;PhasingPlayer=e.PhasingPlayer;Owner=e.Owner;Stage=e.Stage;
            DecisionId=e.DecisionId;DecisionSourceVersion=e.DecisionSourceVersion;Outcome=e.Outcome;
        }
    }
    public sealed class EventCommandResult
    {
        public PublicFiveYearPlanResult FiveYearPlan { get; }
        public RuleError Error { get; }
        public InfluenceEventError EffectError { get; }
        public bool Accepted=>Error==RuleError.None;
        public PlayerViewSnapshot View { get; }
        public IReadOnlyList<InfluenceChanged> Changes { get; }
        public IReadOnlyList<PublicHeadlineStep> HeadlineSteps { get; }
        public long? DecisionRequestedSequence { get; }
        public long? ResolutionSequence { get; }
        internal EventCommandResult(RuleError error,PlayerViewSnapshot view,InfluenceEventTransition transition=null,IReadOnlyList<HeadlineResolutionStep> headlineSteps=null)
        {
            HeadlineSteps=Array.AsReadOnly((headlineSteps??Array.Empty<HeadlineResolutionStep>()).Select(step=>new PublicHeadlineStep(step)).ToArray());
            // Only the first step belongs to this choice; later headlines have separate receipts.
            var fiveYear=transition?.FiveYearPlanResult??headlineSteps?.FirstOrDefault()?.Result.FiveYearPlanResult;
            FiveYearPlan=fiveYear==null?null:new PublicFiveYearPlanResult(fiveYear);
            Error=error;View=view;EffectError=transition?.EffectError??InfluenceEventError.None;
            Changes=Array.AsReadOnly(transition?.Changes.ToArray()??Array.Empty<InfluenceChanged>());
            DecisionRequestedSequence=transition?.DecisionRequestedSequence;ResolutionSequence=transition?.ResolutionSequence;
        }
    }
    public sealed class EventTargetsResult
    {
        public RuleError Error { get; }
        public long Version { get; }
        public PlayerSide Viewer { get; }
        public string ExecutionId { get; }
        public string DecisionId { get; }
        public int RequiredTargets { get; }
        public IReadOnlyList<CountryId> Targets { get; }
        public InfluenceDecisionKind Kind { get; }
        public int RequiredAmount { get; }
        public IReadOnlyList<InfluenceAllocation> Capacities { get; }
        public int MaximumAmount { get; }
        public IReadOnlyList<InfluenceAllocation> SourceCapacities { get; }=Array.AsReadOnly(Array.Empty<InfluenceAllocation>());
        public IReadOnlyList<InfluenceAllocation> DestinationCapacities { get; }=Array.AsReadOnly(Array.Empty<InfluenceAllocation>());
        internal EventTargetsResult(RuleError error,PlayerSide viewer,GameState state,InfluenceEventQuery query=null)
        {
            Error=error;Version=state.Version;Viewer=viewer;ExecutionId=state.InfluenceEvent?.Id;DecisionId=state.InfluenceEvent?.DecisionId;
            RequiredTargets=query?.RequiredTargets??0;Targets=Array.AsReadOnly(query?.LegalCountries.ToArray()??Array.Empty<CountryId>());
            Kind=query?.Kind??InfluenceDecisionKind.None;RequiredAmount=query?.RequiredAmount??0;
            Capacities=Array.AsReadOnly(query?.Capacities.ToArray()??Array.Empty<InfluenceAllocation>());
            MaximumAmount=query?.MaximumAmount??0;
            SourceCapacities=Array.AsReadOnly(query?.SourceCapacities.ToArray()??Array.Empty<InfluenceAllocation>());
            DestinationCapacities=Array.AsReadOnly(query?.DestinationCapacities.ToArray()??Array.Empty<InfluenceAllocation>());
        }
    }
}
