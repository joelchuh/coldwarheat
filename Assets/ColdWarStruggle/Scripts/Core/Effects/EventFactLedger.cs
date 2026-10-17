using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum EventFactQueryStatus { Satisfied, NotSatisfied, RequiresRuling, Rejected }
    public sealed class EventFactQuery
    {
        public EventFactQueryStatus Status { get; }
        public RuleError Error { get; }
        internal EventFactQuery(EventFactQueryStatus status,RuleError error=RuleError.None){Status=status;Error=error;}
    }
    public sealed class SuccessfulEventFact
    {
        public string ResolutionId { get; }
        public CardId Card { get; }
        public string EffectId { get; }
        public PlayerSide Owner { get; }
        public PlayerSide PhasingPlayer { get; }
        public InfluenceEventStatus Outcome { get; }
        public long CommittedVersion { get; }
        public long CommitSequence { get; }
        internal SuccessfulEventFact(InfluenceEventExecution execution,long version,long sequence)
        {
            ResolutionId=execution.Id;Card=new CardId(execution.Definition.CardId);EffectId=execution.Definition.EffectId;
            Owner=execution.Owner;PhasingPlayer=execution.PhasingPlayer;Outcome=execution.Outcome.Value;
            CommittedVersion=version;CommitSequence=sequence;
        }
        internal bool Matches(SuccessfulEventFact other)=>ResolutionId==other.ResolutionId && Card.Equals(other.Card)
            && EffectId==other.EffectId && Owner==other.Owner && PhasingPlayer==other.PhasingPlayer && Outcome==other.Outcome
            && CommittedVersion==other.CommittedVersion && CommitSequence==other.CommitSequence;
    }
    public sealed class EventFactRecording
    {
        public RuleError Error { get; }
        public bool Accepted=>Error==RuleError.None;
        public EventFactLedger After { get; }
        public SuccessfulEventFact Fact { get; }
        public bool AlreadyApplied { get; }
        internal EventFactRecording(RuleError error,EventFactLedger after=null,SuccessfulEventFact fact=null,bool applied=false){Error=error;After=after;Fact=fact;AlreadyApplied=applied;}
    }
    public sealed class EventFactLedger
    {
        public InfluenceEventFactDefinition Definition { get; }
        public string GameId { get; }
        public bool HistoryKnown { get; }
        public IReadOnlyList<SuccessfulEventFact> Facts { get; }
        EventFactLedger(InfluenceEventFactDefinition definition,string gameId,bool historyKnown,IEnumerable<SuccessfulEventFact> facts)
        {Definition=definition;GameId=gameId;HistoryKnown=historyKnown;Facts=Array.AsReadOnly(facts.ToArray());}
        public static EventFactLedger Create(InfluenceEventFactDefinition definition,string gameId,bool historyKnown)
        {
            if(definition==null || !ValidId(gameId))throw new ArgumentException("Missing event fact authority.");
            return new EventFactLedger(definition,gameId,historyKnown,Array.Empty<SuccessfulEventFact>());
        }
        // Trusted engine adapter only. The host must publish After atomically with the parent event.
        public EventFactRecording RecordCompleted(InfluenceEventTransition transition)
        {
            if(transition==null || !transition.Accepted || transition.EffectError!=InfluenceEventError.None || transition.NextState==null)
                return new EventFactRecording(RuleError.InvalidCommand);
            var state=transition.NextState;
            if(state.GameId!=GameId)return new EventFactRecording(RuleError.WrongGame);
            if(!ReferenceEquals(state.Map,Definition.Map) || state.Cards==null || !ReferenceEquals(state.Cards.Catalog,Definition.Cards))
                return new EventFactRecording(RuleError.InvalidDefinition);
            if(state.Ended)return new EventFactRecording(RuleError.GameAlreadyEnded);
            if(state.Deployment!=null)return new EventFactRecording(RuleError.SetupInProgress);
            var execution=state.InfluenceEvent;
            if(execution==null || execution.Stage!=InfluenceEventStage.Resolved)return new EventFactRecording(RuleError.EventNotStarted);
            var card=new CardId(execution.Definition.CardId);
            if(!Definition.Effects.TryGetValue(card,out var covered) || !ReferenceEquals(execution.Definition,covered)
                || execution.Policy!=InfluenceEventPolicy.NoActiveInterference || !ValidId(execution.Id))return new EventFactRecording(RuleError.InvalidDefinition);
            if(execution.Outcome!=InfluenceEventStatus.ResolvedChange && execution.Outcome!=InfluenceEventStatus.ResolvedNoChange)
                return new EventFactRecording(RuleError.EventNotStarted);
            if(state.Version<=0 || state.EventSequence<=0 || transition.ResolutionSequence!=state.EventSequence
                || (execution.DecisionSourceVersion.HasValue && state.Version-1!=execution.DecisionSourceVersion.Value)
                || (execution.Outcome==InfluenceEventStatus.ResolvedChange)!=(transition.Changes.Count>0))return new EventFactRecording(RuleError.StaleContext);
            var fact=new SuccessfulEventFact(execution,state.Version,transition.ResolutionSequence.Value);
            var existing=Facts.FirstOrDefault(f=>f.ResolutionId==fact.ResolutionId);
            if(existing!=null)return existing.Matches(fact)?new EventFactRecording(RuleError.None,this,existing,true):new EventFactRecording(RuleError.CommandIdConflict);
            var last=Facts.LastOrDefault();
            if(last!=null && (fact.CommittedVersion<=last.CommittedVersion || fact.CommitSequence<=last.CommitSequence))return new EventFactRecording(RuleError.StaleContext);
            return new EventFactRecording(RuleError.None,new EventFactLedger(Definition,GameId,HistoryKnown,Facts.Concat(new[]{fact})),fact);
        }
        public EventFactQuery HasSuccessfulEvent(CardId card)=>EvaluateAny(new[]{card});
        public EventFactQuery EvaluateAny(IEnumerable<CardId> cards)
        {
            var query=cards?.Take(Definition.Cards.Cards.Count+1).ToArray();
            // Validate the entire request before any successful witness can short-circuit it.
            if(query==null || query.Length==0 || query.Length>Definition.Cards.Cards.Count || query.Distinct().Count()!=query.Length
                || query.Any(c=>!Definition.Cards.Contains(c) || Definition.Cards.GetCard(c).Kind!=CardKind.Event))
                return new EventFactQuery(EventFactQueryStatus.Rejected,RuleError.InvalidDefinition);
            if(Facts.Any(f=>query.Contains(f.Card)))return new EventFactQuery(EventFactQueryStatus.Satisfied);
            return HistoryKnown && query.All(Definition.Effects.ContainsKey)?new EventFactQuery(EventFactQueryStatus.NotSatisfied)
                :new EventFactQuery(EventFactQueryStatus.RequiresRuling,RuleError.EventRequiresRuling);
        }
        static bool ValidId(string id)=>!string.IsNullOrWhiteSpace(id) && id.Length<=128 && id==id.Trim();
    }
}
