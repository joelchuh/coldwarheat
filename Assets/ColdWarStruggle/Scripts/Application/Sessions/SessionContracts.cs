using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed class CommandEnvelope : SessionCommand
    {
        public string ContextId { get; }
        public IReadOnlyList<CountryId> Targets { get; }
        public CommandEnvelope(string commandId,string gameId,long expectedVersion,string contextId,IEnumerable<CountryId> targets)
            :base(commandId,gameId,expectedVersion)
        { ContextId=contextId;Targets=targets==null?null:Array.AsReadOnly(targets.Take(129).ToArray()); }
        internal override bool SameContent(SessionCommand other) => other is CommandEnvelope c && GameId==c.GameId && ExpectedVersion==c.ExpectedVersion && ContextId==c.ContextId && Targets.SequenceEqual(c.Targets);
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
        public PublicOlympicView Olympic { get; }
        public PublicWarView War { get; }
        public PublicFiveYearPlanResult FiveYearPlan { get; }
        public PublicBlockadeView Blockade { get; }
        public string GameId { get; }
        public PlayerSide Viewer { get; }
        public long Version { get; }
        public long EventSequence { get; }
        public string ContextId { get; }
        public PlayerSide Actor { get; }
        public int RemainingOps { get; }
        public PublicRegionalOps RegionalOps { get; }
        public IReadOnlyList<CountryView> Countries { get; }
        public IReadOnlyList<string> OwnHand { get; }
        public int OpponentHandCount { get; }
        public PublicEventView Event { get; }
        public GameTurnContext Turn { get; }
        public PublicDeploymentView Deployment { get; }
        public PublicCardSummary Cards { get; }
        public PublicPracticeView Practice { get; }
        public PublicHeadlineView Headline { get; }
        public PublicSpaceEndTurnDiscard SpaceDiscard { get; }
        internal PlayerViewSnapshot(GameState s,PlayerSide viewer)
        {
            Olympic=s.Practice?.Olympic==null?null:new PublicOlympicView(s,viewer);
            War=s.Practice?.War==null?null:new PublicWarView(s,viewer);
            FiveYearPlan=s.Practice?.FiveYearPlan==null?null:new PublicFiveYearPlanResult(s.Practice.FiveYearPlan,s.Ended);
            Blockade=s.Practice?.Blockade==null?null:new PublicBlockadeView(s,viewer);
            SpaceDiscard=s.Practice?.SpaceDiscard==null?null:new PublicSpaceEndTurnDiscard(s,viewer);
            Headline=s.Practice?.HeadlineSelection!=null || s.Practice?.Headline!=null?new PublicHeadlineView(s,viewer):null;
            RegionalOps=s.Operations.RegionalBudget==null?null:new PublicRegionalOps(s.Operations.RegionalBudget);
            GameId=s.GameId;Viewer=viewer;Version=s.Version;EventSequence=s.EventSequence;ContextId=s.Operations.Id;Actor=s.Operations.Actor;RemainingOps=s.Operations.Budget-s.SpentOps;
            Countries=Array.AsReadOnly(s.Map.Countries.Select(c=>new CountryView(c,s.Countries[c.Id])).ToArray());
            OwnHand=Array.AsReadOnly(s.GetHand(viewer).ToArray());OpponentHandCount=s.GetHand(viewer==PlayerSide.US?PlayerSide.USSR:PlayerSide.US).Count;
            Event=s.InfluenceEvent==null?null:new PublicEventView(s.InfluenceEvent);Turn=s.Turn;
            Deployment=s.Deployment==null?null:new PublicDeploymentView(s.Deployment);
            Cards=s.Cards==null?null:new PublicCardSummary(s.Cards);
            Practice=s.Practice==null?null:new PublicPracticeView(s.Practice,ActionPracticeRules.CanAssessTurnEnd(s,viewer),ActionPracticeRules.CanBeginNextTurn(s,viewer),ActionPracticeRules.CanCompleteFinalScoring(s,viewer),ActionPracticeRules.CanDeclineExtraActions(s,viewer),ActionPracticeRules.CanSkipEmptyHand(s,viewer));
        }
    }
    public sealed class LocalTestSnapshot
    {
        public PlayerViewSnapshot PublicView { get; }
        public IReadOnlyList<string> USHand { get; }
        public IReadOnlyList<string> USSRHand { get; }
        public IReadOnlyDictionary<PlayerSide,CardId> ReservedHeadlines { get; }
        internal LocalTestSnapshot(GameState state)
        { ReservedHeadlines=new ReadOnlyDictionary<PlayerSide,CardId>(state.Cards?.HeadlineReserved.ToDictionary(x=>x.Key,x=>x.Value)??new Dictionary<PlayerSide,CardId>());PublicView=new PlayerViewSnapshot(state,PlayerSide.US);USHand=Array.AsReadOnly(state.GetHand(PlayerSide.US).ToArray());USSRHand=Array.AsReadOnly(state.GetHand(PlayerSide.USSR).ToArray()); }
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
        Task<HeadlineCommandResult> SubmitHeadlineAsync(HeadlineCommand command,CancellationToken cancellationToken=default);
        LegalTargetsResult QueryRealignmentTargets(string contextId,long expectedVersion);
        LegalTargetsResult QueryCoupTargets(CardId card,long expectedVersion);
        ScoringPreviewResult QueryScoringPreview(string region,long expectedVersion);
        PublicCoupQuote QueryCoupQuote(CardId card,CountryId target,long expectedVersion);
        CardActionOptions QueryCardActions();
        Task<CardActionResult> SubmitCardActionAsync(CardActionCommand command,CancellationToken cancellationToken=default);
        PlayerSide Side { get; }
        PlayerViewSnapshot ReadView();
        LegalTargetsResult QueryInfluenceTargets(string contextId,long expectedVersion);
        Task<PlayerCommandResult> SubmitAsync(CommandEnvelope command,CancellationToken cancellationToken=default);
        EventTargetsResult QueryEventTargets(string executionId,long expectedVersion);
        Task<EventCommandResult> SubmitEventAsync(EventCommandEnvelope command,CancellationToken cancellationToken=default);
        DeploymentTargetsResult QueryDeploymentTargets(string executionId,long expectedVersion);
        Task<DeploymentCommandResult> SubmitDeploymentAsync(DeploymentCommandEnvelope command,CancellationToken cancellationToken=default);
    }
}
