using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed partial class LocalGameHost
    {
        readonly object gate=new object();
        readonly bool localTest;
        GameState state;
        readonly Dictionary<(PlayerSide,string),CommandReceipt> receipts=new Dictionary<(PlayerSide,string),CommandReceipt>();
        sealed class CommandReceipt
        {
            internal readonly SessionCommand Command;
            internal readonly PlayerCommandResult Placement;
            internal readonly CardActionResult CardAction;
            internal readonly EventCommandResult Event;
            internal readonly DeploymentCommandResult Deployment;
            internal CommandReceipt(CardActionCommand command,CardActionResult result) { Command=command;CardAction=result; }
            internal CommandReceipt(CommandEnvelope command,PlayerCommandResult result) { Command=command;Placement=result; }
            internal CommandReceipt(EventCommandEnvelope command,EventCommandResult result) { Command=command;Event=result; }
            internal CommandReceipt(DeploymentCommandEnvelope command,DeploymentCommandResult result) { Command=command;Deployment=result; }
        }
        public IGameSession UsSession { get; }
        public IGameSession UssrSession { get; }
        public LocalGameHost(GameState state,bool enableLocalTestView)
        {
            this.state=state??throw new ArgumentNullException(nameof(state));localTest=enableLocalTestView;
            UsSession=new PlayerSession(this,PlayerSide.US);UssrSession=new PlayerSession(this,PlayerSide.USSR);
        }
        public LocalTestSnapshot ReadLocalTestSnapshot()
        {
            lock(gate) { if(!localTest)throw new InvalidOperationException("Local test view is disabled.");return new LocalTestSnapshot(state); }
        }
        PlayerViewSnapshot Read(PlayerSide side) { lock(gate)return new PlayerViewSnapshot(state,side); }
        RuleError Check(PlayerSide side,string contextId,long version)
        {
            if(version!=state.Version)return RuleError.StaleVersion;
            if(contextId!=state.Operations.Id)return RuleError.StaleContext;
            if(state.Ended)return RuleError.GameAlreadyEnded;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            if(state.InfluenceEvent!=null && state.InfluenceEvent.Stage!=InfluenceEventStage.Resolved)return RuleError.EventInProgress;
            if(side!=state.Operations.Actor)return RuleError.WrongActor;
            if(state.Operations.Mode!=OperationsMode.Influence)return RuleError.ModeMismatch;
            if(state.Practice!=null && state.Practice.Stage!=ActionPracticeStage.PlacingInfluence)return RuleError.ActionNotReady;
            return RuleError.None;
        }
        LegalTargetsResult Query(PlayerSide side,string contextId,long version)
        {
            lock(gate)
            {
                var error=Check(side,contextId,version);
                var ids=error==RuleError.None?state.Map.Countries.Where(c=>InfluenceRules.PlanPlacement(state,state.Operations,new[]{c.Id}).Accepted).Select(c=>c.Id).ToArray():new CountryId[0];
                return new LegalTargetsResult(error,state.Version,side,contextId,ids);
            }
        }
        PlayerCommandResult Reject(RuleError error,PlayerSide side) => new PlayerCommandResult(error,new PlayerViewSnapshot(state,side));
        static bool ValidEnvelope(SessionCommand command)=>command!=null && !string.IsNullOrWhiteSpace(command.CommandId)
            && command.CommandId.Length<=128 && !string.IsNullOrWhiteSpace(command.GameId) && command.ExpectedVersion>=0;
        Task<PlayerCommandResult> Submit(PlayerSide side,CommandEnvelope command,CancellationToken token)
        {
            lock(gate)
            {
                if(token.IsCancellationRequested)return Task.FromCanceled<PlayerCommandResult>(token);
                if(!ValidEnvelope(command)
                    || string.IsNullOrWhiteSpace(command.ContextId) || command.ExpectedVersion<0 || command.Targets==null || command.Targets.Count==0 || command.Targets.Count>128)
                    return Task.FromResult(Reject(RuleError.InvalidCommand,side));
                var key=(side,command.CommandId);
                if(receipts.TryGetValue(key,out var receipt))
                    return Task.FromResult(command.SameContent(receipt.Command)?receipt.Placement:Reject(RuleError.CommandIdConflict,side));
                var error=command.GameId!=state.GameId?RuleError.WrongGame:Check(side,command.ContextId,command.ExpectedVersion);
                PlayerCommandResult result;
                if(error!=RuleError.None)result=Reject(error,side);
                else
                {
                    var plan=InfluenceRules.PlanPlacement(state,state.Operations,command.Targets);
                    if(!plan.Accepted)result=Reject(plan.Error,side);
                    else
                    {
                        result=new PlayerCommandResult(RuleError.None,new PlayerViewSnapshot(plan.NextState,side),plan.Costs,plan.Events);
                        // No callbacks or rendering run inside the authoritative commit boundary.
                        state=plan.NextState;
                    }
                }
                receipts.Add(key,new CommandReceipt(command,result));
                return Task.FromResult(result);
            }
        }
        RuleError CheckEventContext(string executionId,long version)
        {
            if(version!=state.Version)return RuleError.StaleVersion;
            if(state.Ended)return RuleError.GameAlreadyEnded;
            if(state.Deployment!=null)return RuleError.SetupInProgress;
            if(state.InfluenceEvent==null)return RuleError.EventNotAvailable;
            if(executionId!=state.InfluenceEvent.Id)return RuleError.StaleContext;
            return RuleError.None;
        }
        EventTargetsResult QueryEvent(PlayerSide side,string executionId,long version)
        {
            lock(gate)
            {
                var error=CheckEventContext(executionId,version);
                var query=error==RuleError.None?InfluenceEventFlow.QueryDecision(state,side,out error):null;
                return new EventTargetsResult(error,side,state,query);
            }
        }
        EventCommandResult RejectEvent(RuleError error,PlayerSide side,InfluenceEventTransition transition=null)
            =>new EventCommandResult(error,new PlayerViewSnapshot(state,side),transition);
        static bool ValidEventEnvelope(EventCommandEnvelope command)
        {
            if(!ValidEnvelope(command) || string.IsNullOrWhiteSpace(command.ExecutionId) || command.ExecutionId.Length>128
                || command.Targets.Count>InfluenceEventSelection.MaxTargets || command.Allocations.Count>InfluenceEventSelection.MaxTargets)return false;
            if(command.Kind==EventCommandKind.Begin)
                return command.DecisionId==null && command.Targets.Count==0 && command.Allocations.Count==0;
            if(string.IsNullOrWhiteSpace(command.DecisionId) || command.DecisionId.Length>256)return false;
            switch(command.Kind)
            {
                case EventCommandKind.Resolve:return command.Targets.Count>0 && command.Allocations.Count==0;
                case EventCommandKind.ResolveAllocation:return command.Targets.Count==0 && command.Allocations.Count>0;
                default:return false;
            }
        }
        Task<EventCommandResult> SubmitEvent(PlayerSide side,EventCommandEnvelope command,CancellationToken token)
        {
            lock(gate)
            {
                if(token.IsCancellationRequested)return Task.FromCanceled<EventCommandResult>(token);
                if(!ValidEventEnvelope(command))
                    return Task.FromResult(RejectEvent(RuleError.InvalidCommand,side));
                var key=(side,command.CommandId);
                if(receipts.TryGetValue(key,out var receipt))
                    return Task.FromResult(command.SameContent(receipt.Command)?receipt.Event:RejectEvent(RuleError.CommandIdConflict,side));
                var error=command.GameId!=state.GameId?RuleError.WrongGame:CheckEventContext(command.ExecutionId,command.ExpectedVersion);
                EventCommandResult result;
                if(error!=RuleError.None)result=RejectEvent(error,side);
                else
                {
                    var transition=command.Kind==EventCommandKind.Begin?InfluenceEventFlow.Begin(state,side)
                        :command.Kind==EventCommandKind.ResolveAllocation?InfluenceEventFlow.ResolveAllocation(state,side,command.DecisionId,command.Allocations)
                        :InfluenceEventFlow.Resolve(state,side,command.DecisionId,command.Targets);
                    transition=ActionPracticeRules.CompleteEventAction(transition);
                    if(!transition.Accepted)result=RejectEvent(transition.Error,side,transition);
                    else
                    {
                        result=new EventCommandResult(RuleError.None,new PlayerViewSnapshot(transition.NextState,side),transition);
                        state=transition.NextState;
                    }
                }
                receipts.Add(key,new CommandReceipt(command,result));
                return Task.FromResult(result);
            }
        }
        sealed class PlayerSession : IGameSession
        {
            readonly LocalGameHost host;
            public PlayerSide Side { get; }
            internal PlayerSession(LocalGameHost host,PlayerSide side) { this.host=host;Side=side; }
            public PlayerViewSnapshot ReadView()=>host.Read(Side);
            public LegalTargetsResult QueryRealignmentTargets(string contextId,long expectedVersion)=>host.QueryRealignment(Side,contextId,expectedVersion);
            public LegalTargetsResult QueryCoupTargets(CardId card,long expectedVersion)=>host.QueryCoup(Side,card,expectedVersion);
            public ScoringPreviewResult QueryScoringPreview(string region,long expectedVersion)=>host.QueryScoring(region,expectedVersion);
            public CardActionOptions QueryCardActions()=>host.QueryCards(Side);
            public Task<CardActionResult> SubmitCardActionAsync(CardActionCommand command,CancellationToken cancellationToken=default)=>host.SubmitCard(Side,command,cancellationToken);
            public LegalTargetsResult QueryInfluenceTargets(string contextId,long expectedVersion)=>host.Query(Side,contextId,expectedVersion);
            public Task<PlayerCommandResult> SubmitAsync(CommandEnvelope command,CancellationToken cancellationToken=default)=>host.Submit(Side,command,cancellationToken);
            public EventTargetsResult QueryEventTargets(string executionId,long expectedVersion)=>host.QueryEvent(Side,executionId,expectedVersion);
            public Task<EventCommandResult> SubmitEventAsync(EventCommandEnvelope command,CancellationToken cancellationToken=default)
                =>host.SubmitEvent(Side,command,cancellationToken);
            public DeploymentTargetsResult QueryDeploymentTargets(string executionId,long expectedVersion)=>host.QueryDeployment(Side,executionId,expectedVersion);
            public Task<DeploymentCommandResult> SubmitDeploymentAsync(DeploymentCommandEnvelope command,CancellationToken cancellationToken=default)
                =>host.SubmitDeployment(Side,command,cancellationToken);
        }
    }
}
