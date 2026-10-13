using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;

namespace ColdWarStruggle.Application
{
    public sealed class DeploymentCommandEnvelope : SessionCommand
    {
        public string ExecutionId { get; }
        public string DecisionId { get; }
        public IReadOnlyList<InfluenceAllocation> Allocations { get; }
        public DeploymentCommandEnvelope(string id,string game,long version,string executionId,string decisionId,IEnumerable<InfluenceAllocation> allocations)
            :base(id,game,version)
        {
            ExecutionId=executionId;DecisionId=decisionId;
            Allocations=Array.AsReadOnly((allocations??Enumerable.Empty<InfluenceAllocation>()).Take(129).ToArray());
        }
        internal override bool SameContent(SessionCommand other)=>other is DeploymentCommandEnvelope d
            && GameId==d.GameId && ExpectedVersion==d.ExpectedVersion && ExecutionId==d.ExecutionId
            && DecisionId==d.DecisionId && Allocations.SequenceEqual(d.Allocations);
    }
    public sealed class PublicDeploymentView
    {
        public string ExecutionId { get; }
        public int StageIndex { get; }
        public PlayerSide? Owner { get; }
        public string DecisionId { get; }
        public long? DecisionSourceVersion { get; }
        public bool Complete { get; }
        public string Region { get; }
        public int RequiredAmount { get; }
        internal PublicDeploymentView(SetupDeploymentExecution e)
        {
            ExecutionId=e.Id;StageIndex=e.StageIndex;Owner=e.Owner;DecisionId=e.DecisionId;DecisionSourceVersion=e.DecisionSourceVersion;Complete=e.Complete;
            if(!e.Complete) { var stage=e.Definition.Stages[e.StageIndex];Region=stage.Region;RequiredAmount=stage.FreeAmount; }
        }
    }
    public sealed class DeploymentCommandResult
    {
        public RuleError Error { get; }
        public bool Accepted=>Error==RuleError.None;
        public PlayerViewSnapshot View { get; }
        public IReadOnlyList<InfluenceChanged> Changes { get; }
        public long? DecisionRequestedSequence { get; }
        public long? CompletionSequence { get; }
        internal DeploymentCommandResult(RuleError error,PlayerViewSnapshot view,DeploymentTransition transition=null)
        {
            Error=error;View=view;Changes=Array.AsReadOnly(transition?.Changes.ToArray()??Array.Empty<InfluenceChanged>());
            DecisionRequestedSequence=transition?.DecisionRequestedSequence;CompletionSequence=transition?.CompletionSequence;
        }
    }
    public sealed class DeploymentTargetsResult
    {
        public RuleError Error { get; }
        public long Version { get; }
        public PlayerSide Viewer { get; }
        public string ExecutionId { get; }
        public string DecisionId { get; }
        public int RequiredAmount { get; }
        public IReadOnlyList<CountryId> Targets { get; }
        internal DeploymentTargetsResult(RuleError error,PlayerSide viewer,GameState state,DeploymentQuery query=null)
        {
            Error=error;Viewer=viewer;Version=state.Version;ExecutionId=state.Deployment?.Id;DecisionId=state.Deployment?.DecisionId;
            RequiredAmount=query?.RequiredAmount??0;Targets=Array.AsReadOnly(query?.Targets.ToArray()??Array.Empty<CountryId>());
        }
    }
}
