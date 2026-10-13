using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed class CommandEnvelope
    {
        public string CommandId { get; }
        public string GameId { get; }
        public long ExpectedVersion { get; }
        public string ContextId { get; }
        public IReadOnlyList<CountryId> Targets { get; }
        public CommandEnvelope(string commandId,string gameId,long expectedVersion,string contextId,IEnumerable<CountryId> targets)
        { CommandId=commandId;GameId=gameId;ExpectedVersion=expectedVersion;ContextId=contextId;Targets=targets==null?null:Array.AsReadOnly(targets.Take(129).ToArray()); }
        internal bool SameContent(CommandEnvelope other) => GameId==other.GameId && ExpectedVersion==other.ExpectedVersion && ContextId==other.ContextId && Targets.SequenceEqual(other.Targets);
    }
    public sealed class CountryView
    {
        public CountryDefinition Definition { get; }
        public int US { get; }
        public int USSR { get; }
        public PlayerSide? Controller { get; }
        internal CountryView(CountryDefinition definition,CountryState state)
        { Definition=definition;US=state.US;USSR=state.USSR;Controller=ControlRules.GetController(US,USSR,definition.Stability); }
    }
    public sealed class PlayerViewSnapshot
    {
        public string GameId { get; }
        public PlayerSide Viewer { get; }
        public long Version { get; }
        public long EventSequence { get; }
        public string ContextId { get; }
        public PlayerSide Actor { get; }
        public int RemainingOps { get; }
        public IReadOnlyList<CountryView> Countries { get; }
        public IReadOnlyList<string> OwnHand { get; }
        public int OpponentHandCount { get; }
        internal PlayerViewSnapshot(GameState s,PlayerSide viewer)
        {
            GameId=s.GameId;Viewer=viewer;Version=s.Version;EventSequence=s.EventSequence;ContextId=s.Operations.Id;Actor=s.Operations.Actor;RemainingOps=s.Operations.Budget-s.SpentOps;
            Countries=Array.AsReadOnly(s.Map.Countries.Select(c=>new CountryView(c,s.Countries[c.Id])).ToArray());
            OwnHand=Array.AsReadOnly(s.GetHand(viewer).ToArray());OpponentHandCount=s.GetHand(viewer==PlayerSide.US?PlayerSide.USSR:PlayerSide.US).Count;
        }
    }
    public sealed class LocalTestSnapshot
    {
        public PlayerViewSnapshot PublicView { get; }
        public IReadOnlyList<string> USHand { get; }
        public IReadOnlyList<string> USSRHand { get; }
        internal LocalTestSnapshot(GameState state)
        { PublicView=new PlayerViewSnapshot(state,PlayerSide.US);USHand=Array.AsReadOnly(state.GetHand(PlayerSide.US).ToArray());USSRHand=Array.AsReadOnly(state.GetHand(PlayerSide.USSR).ToArray()); }
    }
    public sealed class PlayerCommandResult
    {
        public RuleError Error { get; }
        public bool Accepted => Error==RuleError.None;
        public PlayerViewSnapshot View { get; }
        public IReadOnlyList<int> Costs { get; }
        public IReadOnlyList<InfluencePlaced> Events { get; }
        internal PlayerCommandResult(RuleError error,PlayerViewSnapshot view,IReadOnlyList<int> costs=null,IReadOnlyList<InfluencePlaced> events=null)
        { Error=error;View=view;Costs=Array.AsReadOnly(costs?.ToArray()??new int[0]);Events=Array.AsReadOnly(events?.ToArray()??new InfluencePlaced[0]); }
    }
    public sealed class LegalTargetsResult
    {
        public RuleError Error { get; }
        public long Version { get; }
        public PlayerSide Viewer { get; }
        public string ContextId { get; }
        public IReadOnlyList<CountryId> Targets { get; }
        internal LegalTargetsResult(RuleError error,long version,PlayerSide viewer,string contextId,IEnumerable<CountryId> targets)
        { Error=error;Version=version;Viewer=viewer;ContextId=contextId;Targets=Array.AsReadOnly(targets.ToArray()); }
    }
    public interface IGameSession
    {
        PlayerSide Side { get; }
        PlayerViewSnapshot ReadView();
        LegalTargetsResult QueryInfluenceTargets(string contextId,long expectedVersion);
        Task<PlayerCommandResult> SubmitAsync(CommandEnvelope command,CancellationToken cancellationToken=default);
    }
}
