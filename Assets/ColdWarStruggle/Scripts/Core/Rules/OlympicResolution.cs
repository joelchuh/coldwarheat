using System;
namespace ColdWarStruggle.Core
{
    public enum OlympicSourceKind { ActionRound, Headline }
    public enum OlympicBranch { Participate, Boycott }
    public enum OlympicResolutionStage { AwaitingOpponent, Rolling, AwaitingOperations, Completed }
    // Authority-only in-memory state. A physical partition is not player authorization.
    public sealed class OlympicResolution
    {
        public OlympicEventState Current { get; }
        public CardZones SourceCards { get; }
        public string GameId { get; }
        public string ResolutionId { get; }
        public string DecisionId => ResolutionId + "/opponent";
        public long Revision { get; }
        public OlympicSourceKind SourceKind { get; }
        public PlayerSide Sponsor { get; }
        public PlayerSide RootPhasing { get; }
        public PlayerSide? PendingOwner => Stage==OlympicResolutionStage.AwaitingOpponent
            ?(Sponsor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US)
            :Stage==OlympicResolutionStage.AwaitingOperations?Sponsor:(PlayerSide?)null;
        public OlympicResolutionStage Stage { get; }
        public OlympicBranch? Branch { get; }
        public OlympicPairReceipt LastPair { get; }
        public OlympicBoycottPlan Boycott { get; }
        public OlympicOperationsOffer Operations => Stage==OlympicResolutionStage.AwaitingOperations?Boycott.Operations:null;
        public VictoryDecision Decision { get; }
        public OlympicContinuation Continuation { get; }
        internal OlympicResolution(OlympicEventState current,CardZones cards,string game,string id,long revision,
            OlympicSourceKind kind,PlayerSide sponsor,PlayerSide root,OlympicResolutionStage stage,
            OlympicBranch? branch=null,OlympicPairReceipt last=null,OlympicBoycottPlan boycott=null,VictoryDecision decision=null)
        {
            Current=current;SourceCards=cards;GameId=game;ResolutionId=id;Revision=revision;SourceKind=kind;Sponsor=sponsor;RootPhasing=root;
            Stage=stage;Branch=branch;LastPair=last;Boycott=boycott;Decision=decision;
            Continuation=stage==OlympicResolutionStage.Rolling?new OlympicContinuation(this):null;
        }
    }
    // Linked receipts avoid copying all earlier ties on every pair. No persistence digest is implied.
    public sealed class OlympicPairReceipt
    {
        public OlympicPairReceipt Previous { get; }
        public long RoundIndex { get; }
        public long CommittedRevision { get; }
        public OlympicPairPlan Plan { get; }
        internal OlympicPairReceipt(OlympicPairReceipt previous,long revision,OlympicPairPlan plan)
        {Previous=previous;RoundIndex=checked((previous?.RoundIndex??0)+1);CommittedRevision=revision;Plan=plan;}
    }
    public sealed class OlympicContinuation
    {
        public long Revision => Owner.Revision;
        public long NextRoundIndex => checked(Owner.LastPair.RoundIndex+1);
        internal OlympicResolution Owner { get; }
        internal OlympicContinuation(OlympicResolution owner){Owner=owner;}
    }
}
