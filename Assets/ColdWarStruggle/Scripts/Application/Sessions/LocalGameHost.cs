using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed class LocalGameHost
    {
        readonly object gate=new object();
        readonly bool localTest;
        GameState state;
        readonly Dictionary<(PlayerSide,string),(CommandEnvelope,PlayerCommandResult)> receipts=new Dictionary<(PlayerSide,string),(CommandEnvelope,PlayerCommandResult)>();
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
            if(side!=state.Operations.Actor)return RuleError.WrongActor;
            if(state.Operations.Mode!=OperationsMode.Influence)return RuleError.ModeMismatch;
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
        Task<PlayerCommandResult> Submit(PlayerSide side,CommandEnvelope command,CancellationToken token)
        {
            lock(gate)
            {
                if(token.IsCancellationRequested)return Task.FromCanceled<PlayerCommandResult>(token);
                if(command==null || string.IsNullOrWhiteSpace(command.CommandId) || command.CommandId.Length>128 || string.IsNullOrWhiteSpace(command.GameId)
                    || string.IsNullOrWhiteSpace(command.ContextId) || command.ExpectedVersion<0 || command.Targets==null || command.Targets.Count==0 || command.Targets.Count>128)
                    return Task.FromResult(Reject(RuleError.InvalidCommand,side));
                var key=(side,command.CommandId);
                if(receipts.TryGetValue(key,out var receipt))
                    return Task.FromResult(command.SameContent(receipt.Item1)?receipt.Item2:Reject(RuleError.CommandIdConflict,side));
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
                receipts.Add(key,(command,result));
                return Task.FromResult(result);
            }
        }
        sealed class PlayerSession : IGameSession
        {
            readonly LocalGameHost host;
            public PlayerSide Side { get; }
            internal PlayerSession(LocalGameHost host,PlayerSide side) { this.host=host;Side=side; }
            public PlayerViewSnapshot ReadView()=>host.Read(Side);
            public LegalTargetsResult QueryInfluenceTargets(string contextId,long expectedVersion)=>host.Query(Side,contextId,expectedVersion);
            public Task<PlayerCommandResult> SubmitAsync(CommandEnvelope command,CancellationToken cancellationToken=default)=>host.Submit(Side,command,cancellationToken);
        }
    }
}
