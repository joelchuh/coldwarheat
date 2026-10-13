using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        readonly Dictionary<CountryId,int> deploymentDraft=new Dictionary<CountryId,int>();
        DeploymentTargetsResult deploymentTargets;
        DeploymentCommandEnvelope lastDeploymentCommand;
        PlayerSide lastDeploymentSide,deploymentDraftSide;
        string deploymentDraftGame,deploymentDraftExecution,deploymentDraftDecision;
        long deploymentDraftVersion=-1;
        public DeploymentCommandResult LastDeploymentResult { get; private set; }
        public IReadOnlyList<InfluenceAllocation> DeploymentDraft
        {
            get
            {
                EnsureDeploymentContext(ReadView());
                return Array.AsReadOnly(deploymentDraft.OrderBy(p=>p.Key.Value,StringComparer.Ordinal).Select(p=>new InfluenceAllocation(p.Key,p.Value)).ToArray());
            }
        }
        public bool CanConfirmDeployment
        { get { var q=DeploymentTargets();return q.Error==RuleError.None && q.RequiredAmount>0 && deploymentDraft.Values.Sum(v=>(long)v)==q.RequiredAmount; } }
        public bool CanRetryDeployment=>lastDeploymentCommand!=null && lastDeploymentSide==SelectedSide;
        void EnsureDeploymentContext(PlayerViewSnapshot view)
        {
            if(deploymentDraftGame==view.GameId && deploymentDraftVersion==view.Version && deploymentDraftSide==SelectedSide
                && deploymentDraftExecution==view.Deployment?.ExecutionId && deploymentDraftDecision==view.Deployment?.DecisionId)return;
            deploymentDraft.Clear();deploymentTargets=null;deploymentDraftGame=view.GameId;deploymentDraftVersion=view.Version;deploymentDraftSide=SelectedSide;
            deploymentDraftExecution=view.Deployment?.ExecutionId;deploymentDraftDecision=view.Deployment?.DecisionId;MarkInteractionChanged();
        }
        void ResetDeploymentInteraction(bool clearReceipt)
        {
            deploymentDraft.Clear();deploymentTargets=null;deploymentDraftGame=null;deploymentDraftVersion=-1;deploymentDraftExecution=null;deploymentDraftDecision=null;
            LastDeploymentResult=null;if(clearReceipt)lastDeploymentCommand=null;MarkInteractionChanged();
        }
        public DeploymentTargetsResult DeploymentTargets()
        {
            var view=ReadView();EnsureDeploymentContext(view);
            return deploymentTargets??(deploymentTargets=Session.QueryDeploymentTargets(view.Deployment?.ExecutionId,view.Version));
        }
        public bool CanChangeDeploymentAllocation(CountryId country,int delta)
        {
            if(delta!=1 && delta!=-1)return false;
            var q=DeploymentTargets();if(q.Error!=RuleError.None || !q.Targets.Contains(country))return false;
            deploymentDraft.TryGetValue(country,out var amount);
            return delta<0?amount>0:deploymentDraft.Values.Sum(v=>(long)v)<q.RequiredAmount;
        }
        public bool ChangeDeploymentAllocation(CountryId country,int delta)
        {
            if(!CanChangeDeploymentAllocation(country,delta))return false;
            deploymentDraft.TryGetValue(country,out var amount);amount+=delta;
            if(amount==0)deploymentDraft.Remove(country);else deploymentDraft[country]=amount;
            MarkInteractionChanged();return true;
        }
        public void ClearDeploymentDraft()
        {
            EnsureDeploymentContext(ReadView());if(deploymentDraft.Count==0)return;
            deploymentDraft.Clear();MarkInteractionChanged();
        }
        public async Task<DeploymentCommandResult> ConfirmDeploymentAsync()
        {
            var view=ReadView();EnsureDeploymentContext(view);
            var command=new DeploymentCommandEnvelope(Guid.NewGuid().ToString("N"),view.GameId,view.Version,view.Deployment?.ExecutionId,view.Deployment?.DecisionId,DeploymentDraft);
            var epoch=generation;lastDeploymentCommand=command;lastDeploymentSide=SelectedSide;
            var result=await Session.SubmitDeploymentAsync(command);
            if(epoch==generation) { LastDeploymentResult=result;deploymentTargets=null;legal=null;EnsureDeploymentContext(ReadView()); }
            return result;
        }
        public async Task<DeploymentCommandResult> RetryDeploymentAsync()
        {
            if(!CanRetryDeployment)throw new InvalidOperationException("No deployment command for this side.");
            var epoch=generation;var result=await Session.SubmitDeploymentAsync(lastDeploymentCommand);
            if(epoch==generation) { LastDeploymentResult=result;EnsureDeploymentContext(ReadView()); }
            return result;
        }
    }
}
