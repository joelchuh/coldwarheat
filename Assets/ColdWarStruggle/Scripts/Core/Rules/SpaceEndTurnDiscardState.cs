using System;
using System.Linq;
using System.Collections.Generic;
namespace ColdWarStruggle.Core
{
    public enum SpaceEndTurnDiscardStatus { Pending, Declined, Discarded, NoCards }
    // Authority-only candidates. Sessions must project this through PublicSpaceEndTurnDiscard.
    public sealed class SpaceEndTurnDiscardState
    {
        public PlayerSide Owner { get; }
        public SpaceEndTurnDiscardStatus Status { get; }
        public IReadOnlyList<CardId> Candidates { get; }
        public CardId? DiscardedCard { get; }
        public string DecisionId { get; }
        internal SpaceTrackState Track { get; }
        internal PracticeTurnEndReceipt Audit { get; }
        internal CardZones Cards { get; }
        SpaceEndTurnDiscardState(PlayerSide owner,SpaceEndTurnDiscardStatus status,IEnumerable<CardId> candidates,CardId? discarded,
            string id,SpaceTrackState track,PracticeTurnEndReceipt audit,CardZones cards)
        {
            Owner=owner;Status=status;Candidates=Array.AsReadOnly(candidates.ToArray());DiscardedCard=discarded;DecisionId=id;Track=track;Audit=audit;Cards=cards;
        }
        internal static RuleError QueryOwner(SpaceTrackState track,out PlayerSide? owner)
        {
            owner=null;var us=SpaceAbilityRules.Query(track,PlayerSide.US).EndTurnDiscardMaximum;var su=SpaceAbilityRules.Query(track,PlayerSide.USSR).EndTurnDiscardMaximum;
            if(us>1 || su>1 || us>0 && su>0)return RuleError.UnsupportedCardAction;
            if(us==1)owner=PlayerSide.US;else if(su==1)owner=PlayerSide.USSR;
            return RuleError.None;
        }
        internal static SpaceEndTurnDiscardState ForAudit(SpaceTrackState track,PracticeTurnEndReceipt audit,CardZones cards,CardCatalog catalog,int turn)
        {
            if(audit.Decision.Outcome!=VictoryOutcome.Continue)return null;
            if(QueryOwner(track,out var owner)!=RuleError.None)throw new ArgumentException("Unsupported end-turn discard entitlement.");
            if(!owner.HasValue)return null;
            var candidates=cards.GetHand(owner.Value).Where(id=>catalog.GetCard(id).Kind==CardKind.Event).OrderBy(id=>id.Value,StringComparer.Ordinal).ToArray();
            return new SpaceEndTurnDiscardState(owner.Value,candidates.Length==0?SpaceEndTurnDiscardStatus.NoCards:SpaceEndTurnDiscardStatus.Pending,
                candidates,null,"practice.turn."+turn+".space-discard",track,audit,cards);
        }
        internal SpaceEndTurnDiscardState Resolve(CardId? discarded,CardZones cards)
        {
            if(Status!=SpaceEndTurnDiscardStatus.Pending || discarded.HasValue && !Candidates.Contains(discarded.Value))throw new InvalidOperationException("No valid pending discard.");
            return new SpaceEndTurnDiscardState(Owner,discarded.HasValue?SpaceEndTurnDiscardStatus.Discarded:SpaceEndTurnDiscardStatus.Declined,
                Candidates,discarded,DecisionId,Track,Audit,cards);
        }
    }
}
