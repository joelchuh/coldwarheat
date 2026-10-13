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
    public enum EventCommandKind { Begin, Resolve }
    public sealed class EventCommandEnvelope : SessionCommand
    {
        public EventCommandKind Kind { get; }
        public string ExecutionId { get; }
        public string DecisionId { get; }
        public IReadOnlyList<CountryId> Targets { get; }
        public EventCommandEnvelope(string id,string game,long version,EventCommandKind kind,string executionId,string decisionId=null,IEnumerable<CountryId> targets=null)
            : base(id,game,version)
        {
            Kind=kind;ExecutionId=executionId;DecisionId=decisionId;
            Targets=Array.AsReadOnly((targets??Enumerable.Empty<CountryId>()).Take(InfluenceEventSelection.MaxTargets+1).ToArray());
        }
        internal override bool SameContent(SessionCommand other)=>other is EventCommandEnvelope e
            && GameId==e.GameId && ExpectedVersion==e.ExpectedVersion && Kind==e.Kind && ExecutionId==e.ExecutionId
            && DecisionId==e.DecisionId && Targets.SequenceEqual(e.Targets);
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
        public RuleError Error { get; }
        public InfluenceEventError EffectError { get; }
        public bool Accepted=>Error==RuleError.None;
        public PlayerViewSnapshot View { get; }
        public IReadOnlyList<InfluenceChanged> Changes { get; }
        public long? DecisionRequestedSequence { get; }
        public long? ResolutionSequence { get; }
        internal EventCommandResult(RuleError error,PlayerViewSnapshot view,InfluenceEventTransition transition=null)
        {
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
        internal EventTargetsResult(RuleError error,PlayerSide viewer,GameState state,InfluenceEventQuery query=null)
        {
            Error=error;Version=state.Version;Viewer=viewer;ExecutionId=state.InfluenceEvent?.Id;DecisionId=state.InfluenceEvent?.DecisionId;
            RequiredTargets=query?.RequiredTargets??0;Targets=Array.AsReadOnly(query?.LegalCountries.ToArray()??Array.Empty<CountryId>());
        }
    }
}
