using System;
using System.Threading;
using System.Threading.Tasks;
using ColdWarStruggle.Core;

namespace ColdWarStruggle.Application
{
    public sealed partial class LocalGameHost
    {
        RuleError CheckDeploymentContext(string executionId,long version)
        {
            if(version!=state.Version)return RuleError.StaleVersion;
            if(state.Ended)return RuleError.GameAlreadyEnded;
            if(state.Deployment==null)return RuleError.SetupNotActive;
            if(executionId!=state.Deployment.Id)return RuleError.StaleContext;
            return RuleError.None;
        }
        DeploymentTargetsResult QueryDeployment(PlayerSide side,string executionId,long version)
        {
            lock(gate)
            {
                var error=CheckDeploymentContext(executionId,version);DeploymentQuery query=null;
                if(error==RuleError.None) { query=InitialDeploymentFlow.QueryDecision(state,side);error=query.Error; }
                return new DeploymentTargetsResult(error,side,state,query);
            }
        }
        DeploymentCommandResult RejectDeployment(RuleError error,PlayerSide side)
            =>new DeploymentCommandResult(error,new PlayerViewSnapshot(state,side));
        static bool ValidDeploymentEnvelope(DeploymentCommandEnvelope command)=>ValidEnvelope(command)
            && !string.IsNullOrWhiteSpace(command.ExecutionId) && command.ExecutionId.Length<=128
            && !string.IsNullOrWhiteSpace(command.DecisionId) && command.DecisionId.Length<=256
            && command.Allocations.Count>0 && command.Allocations.Count<=128;
        Task<DeploymentCommandResult> SubmitDeployment(PlayerSide side,DeploymentCommandEnvelope command,CancellationToken token)
        {
            lock(gate)
            {
                if(token.IsCancellationRequested)return Task.FromCanceled<DeploymentCommandResult>(token);
                if(!ValidDeploymentEnvelope(command))return Task.FromResult(RejectDeployment(RuleError.InvalidCommand,side));
                var key=(side,command.CommandId);
                if(receipts.TryGetValue(key,out var receipt))
                    return Task.FromResult(command.SameContent(receipt.Command)?receipt.Deployment:RejectDeployment(RuleError.CommandIdConflict,side));
                var error=command.GameId!=state.GameId?RuleError.WrongGame:CheckDeploymentContext(command.ExecutionId,command.ExpectedVersion);
                DeploymentCommandResult result;
                if(error!=RuleError.None)result=RejectDeployment(error,side);
                else
                {
                    var transition=InitialDeploymentFlow.Resolve(state,side,command.DecisionId,command.Allocations);
                    if(!transition.Accepted)result=RejectDeployment(transition.Error,side);
                    else
                    {
                        result=new DeploymentCommandResult(RuleError.None,new PlayerViewSnapshot(transition.NextState,side),transition);
                        state=transition.NextState;
                    }
                }
                receipts.Add(key,new CommandReceipt(command,result));
                return Task.FromResult(result);
            }
        }
    }
}
