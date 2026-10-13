using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class DeploymentQuery
    {
        public RuleError Error { get; }
        public int RequiredAmount { get; }
        public IReadOnlyList<CountryId> Targets { get; }
        internal DeploymentQuery(RuleError error,int amount=0,IEnumerable<CountryId> targets=null)
        { Error=error;RequiredAmount=amount;Targets=Array.AsReadOnly((targets??Enumerable.Empty<CountryId>()).OrderBy(c=>c.Value,StringComparer.Ordinal).ToArray()); }
    }
    public sealed class DeploymentTransition
    {
        public RuleError Error { get; }
        public bool Accepted=>Error==RuleError.None;
        public GameState NextState { get; }
        public IReadOnlyList<InfluenceChanged> Changes { get; }
        public long? DecisionRequestedSequence { get; }
        public long? CompletionSequence { get; }
        internal DeploymentTransition(RuleError error,GameState next=null,IEnumerable<InfluenceChanged> changes=null)
        {
            Error=error;NextState=next;Changes=Array.AsReadOnly((changes??Enumerable.Empty<InfluenceChanged>()).ToArray());
            if(next!=null) { if(next.Deployment.Complete)CompletionSequence=next.EventSequence;else DecisionRequestedSequence=next.EventSequence; }
        }
    }
    public static class InitialDeploymentFlow
    {
        // Prepared states belong to trusted flow/fixtures; this does not shuffle, deal or authorize a complete game.
        public static DeploymentTransition Begin(GameState state,SetupDeploymentDefinition definition,string id)
        {
            if(state==null || string.IsNullOrWhiteSpace(id) || id.Length>128)return new DeploymentTransition(RuleError.InvalidCommand);
            if(state.Ended)return new DeploymentTransition(RuleError.GameAlreadyEnded);
            if(state.Deployment!=null)return new DeploymentTransition(RuleError.SetupInProgress);
            if(definition==null || !ReferenceEquals(state.Map,definition.Map))return new DeploymentTransition(RuleError.InvalidDefinition);
            if(state.InfluenceEvent!=null || state.Operations.Budget!=0 || state.SpentOps!=0 || state.Countries.Values.Any(c=>c.US!=0 || c.USSR!=0))
                return new DeploymentTransition(RuleError.InvalidSetupState);
            try
            {
                var work=new InfluenceMutation(state);ApplyFixed(work,definition.Stages[0]);
                return Finish(work,definition,id,0);
            }
            catch(OverflowException) { return new DeploymentTransition(RuleError.NumericOverflow); }
        }
        static RuleError Check(GameState state,PlayerSide actor)
        {
            if(state==null)return RuleError.InvalidCommand;
            if(state.Ended)return RuleError.GameAlreadyEnded;
            if(state.Deployment==null)return RuleError.SetupNotActive;
            if(state.Deployment.Complete)return RuleError.SetupComplete;
            if(actor!=state.Deployment.Owner)return RuleError.WrongActor;
            if(state.Version!=state.Deployment.DecisionSourceVersion)return RuleError.StaleDecision;
            if(state.Operations.Budget!=0 || state.SpentOps!=0 || state.InfluenceEvent!=null)return RuleError.InvalidSetupState;
            return RuleError.None;
        }
        public static DeploymentQuery QueryDecision(GameState state,PlayerSide actor)
        {
            var error=Check(state,actor);if(error!=RuleError.None)return new DeploymentQuery(error);
            var stage=state.Deployment.Definition.Stages[state.Deployment.StageIndex];
            return new DeploymentQuery(RuleError.None,stage.FreeAmount,state.Map.GetCountriesInRegion(stage.Region).Select(c=>c.Id));
        }
        public static DeploymentTransition Resolve(GameState state,PlayerSide actor,string decisionId,IReadOnlyList<InfluenceAllocation> allocations)
        {
            var query=QueryDecision(state,actor);if(query.Error!=RuleError.None)return new DeploymentTransition(query.Error);
            if(decisionId!=state.Deployment.DecisionId)return new DeploymentTransition(RuleError.StaleDecision);
            if(allocations==null || allocations.Count==0 || allocations.Count>128)return new DeploymentTransition(RuleError.InvalidCommand);
            var copy=allocations.ToArray();var seen=new HashSet<CountryId>();long total=0;
            foreach(var allocation in copy)
            {
                if(!seen.Add(allocation.Country))return new DeploymentTransition(RuleError.DuplicateCountry);
                if(allocation.Amount<1)return new DeploymentTransition(RuleError.InvalidAllocationAmount);
                if(!state.Map.Contains(allocation.Country))return new DeploymentTransition(RuleError.UnknownCountry);
                if(!query.Targets.Contains(allocation.Country))return new DeploymentTransition(RuleError.OutsideSetupRegion);
                total+=allocation.Amount;
            }
            if(total!=query.RequiredAmount)return new DeploymentTransition(RuleError.IncorrectAllocationTotal);
            try
            {
                var work=new InfluenceMutation(state);
                foreach(var allocation in copy.OrderBy(a=>a.Country.Value,StringComparer.Ordinal))work.Add(allocation.Country,actor,allocation.Amount);
                var execution=state.Deployment;var nextIndex=execution.StageIndex+1;
                if(nextIndex<execution.Definition.Stages.Count)ApplyFixed(work,execution.Definition.Stages[nextIndex]);
                return Finish(work,execution.Definition,execution.Id,nextIndex);
            }
            catch(OverflowException) { return new DeploymentTransition(RuleError.NumericOverflow); }
        }
        static void ApplyFixed(InfluenceMutation work,SetupDeploymentStage stage)
        { foreach(var allocation in stage.FixedInfluence)work.Add(allocation.Country,stage.Side,allocation.Amount); }
        static DeploymentTransition Finish(InfluenceMutation work,SetupDeploymentDefinition definition,string id,int stageIndex)
        {
            var next=work.Commit();next=next.WithDeployment(new SetupDeploymentExecution(id,definition,stageIndex,next.Version));
            return new DeploymentTransition(RuleError.None,next,work.Changes);
        }
    }
}
