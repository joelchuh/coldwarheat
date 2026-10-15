using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed class HeadlineCommand : SessionCommand
    {
        public CardId Card { get; }
        public HeadlineCommand(string id,string game,long version,CardId card):base(id,game,version){Card=card;}
        internal override bool SameContent(SessionCommand other)=>other is HeadlineCommand c && c.GameId==GameId && c.ExpectedVersion==ExpectedVersion && c.Card.Equals(Card);
    }
    public enum PublicHeadlineStatus { Collecting, Resolving, Completed, Terminal }
    public sealed class PublicHeadlineView
    {
        public PublicHeadlineStatus Status { get; }
        public HeadlineSelectionView Selection { get; }
        public IReadOnlyList<HeadlineOrderEntry> Order { get; }
        public int CompletedCount { get; }
        public HeadlineOrderStrategy? OrderStrategy { get; }
        public IReadOnlyList<HeadlineRootCompletion> CompletedRoots { get; }
        public PlayerSide? CurrentPlayedBy { get; }
        public PlayerSide? WaitingOwner { get; }
        internal PublicHeadlineView(GameState state,PlayerSide viewer)
        {
            var p=state.Practice;
            Selection=p.HeadlineSelection==null?null:HeadlineSelectionView.Create(p.HeadlineSelection,viewer);
            Order=Array.AsReadOnly((p.Headline?.Order??Array.Empty<HeadlineOrderEntry>()).ToArray());
            OrderStrategy=p.Headline?.OrderStrategy;CompletedRoots=Array.AsReadOnly((p.Headline?.CompletedRoots??Array.Empty<HeadlineRootCompletion>()).ToArray());
            CompletedCount=p.Headline?.CompletedCount??0;CurrentPlayedBy=p.Headline?.Current?.PlayedBy;
            WaitingOwner=p.HasPendingHeadline && state.InfluenceEvent?.Stage==InfluenceEventStage.AwaitingChoice?state.InfluenceEvent.Owner:(PlayerSide?)null;
            Status=Selection!=null?PublicHeadlineStatus.Collecting:p.Headline.IsComplete?PublicHeadlineStatus.Completed:
                state.Ended?PublicHeadlineStatus.Terminal:PublicHeadlineStatus.Resolving;
        }
    }
    public sealed class PublicHeadlineStep
    {
        public CardId Card { get; }
        public PlayerSide PlayedBy { get; }
        public bool WaitingForChoice { get; }
        public bool Terminal { get; }
        public HeadlineRootCompletion Completion { get; }
        public PublicCardEventResult Event { get; }
        public PublicRegionScore Scoring { get; }
        public PublicDefconVpEventResult DefconVpEvent { get; }
        public PublicTurnOpsEffect ActivatedOpsEffect { get; }
        internal PublicHeadlineStep(HeadlineResolutionStep step)
        {
            var r=step.Result;Card=step.Card;PlayedBy=step.PlayedBy;Terminal=r.NextState.Ended;
            Completion=r.NextState.Practice.Headline.CompletedRoots.SingleOrDefault(x=>x.Card.Equals(Card) && x.PlayedBy==PlayedBy);
            WaitingForChoice=r.NextState.InfluenceEvent?.Stage==InfluenceEventStage.AwaitingChoice;
            Event=r.EventTransition==null?null:new PublicCardEventResult(r.EventTransition);
            Scoring=r.ScoringResult==null?null:new PublicRegionScore(r.ScoringResult);
            DefconVpEvent=r.DefconVpEvent==null?null:new PublicDefconVpEventResult(r.DefconVpEvent);
            ActivatedOpsEffect=r.ActivatedOpsEffect==null?null:new PublicTurnOpsEffect(r.ActivatedOpsEffect);
        }
    }
    public sealed class HeadlineCommandResult
    {
        public RuleError Error { get; }
        public bool Accepted=>Error==RuleError.None;
        public PlayerViewSnapshot View { get; }
        public IReadOnlyList<PublicHeadlineStep> Steps { get; }
        internal HeadlineCommandResult(RuleError error,PlayerViewSnapshot view,IReadOnlyList<HeadlineResolutionStep> steps=null)
        {Error=error;View=view;Steps=Array.AsReadOnly((steps??Array.Empty<HeadlineResolutionStep>()).Select(s=>new PublicHeadlineStep(s)).ToArray());}
    }
}
