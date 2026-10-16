using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public static class HandDiscardRules
    {
        // Authority only. Do not publish this collection or its count to the opponent.
        public static IReadOnlyList<OpsGeneralQuote> QualifyingCards(CardZones cards,PlayerSide owner,OpsGeneralDefinition ops,int minimum,IEnumerable<OpsFlatModifier> modifiers)
        {
            if(cards==null)throw new ArgumentNullException(nameof(cards));
            var applied=OpsGeneralRules.ValidateContext(ops,owner,OpsValuePurpose.OpsThreshold,modifiers);
            if(minimum<ops.Minimum || minimum>ops.OrdinaryMaximum)throw new ArgumentOutOfRangeException(nameof(minimum));
            return Array.AsReadOnly(cards.GetHand(owner).Select(cards.Catalog.GetCard).Where(c=>c.Kind==CardKind.Event)
                .OrderBy(c=>c.Id.Value,StringComparer.Ordinal)
                .Select(c=>OpsGeneralRules.QuoteCard(ops,c,owner,OpsValuePurpose.OpsThreshold,applied))
                .Where(q=>q.GeneralValue>=minimum).ToArray());
        }
        // Physical move only: callers authorize the current decision before invoking it.
        // Scoring-card/random discards need their own explicit contract.
        public static CardZones DiscardEventCard(CardZones cards,PlayerSide owner,CardId card)
        {
            if(cards==null)throw new ArgumentNullException(nameof(cards));
            OpsGeneralGuard.Side(owner);
            if(!cards.GetHand(owner).Contains(card) || cards.Catalog.GetCard(card).Kind!=CardKind.Event)
                throw new ArgumentException("Selected card must be an ordinary event in the owner's current hand.");
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})
                hands[side]=side==owner?cards.GetHand(side).Where(id=>!id.Equals(card)).ToArray():cards.GetHand(side);
            return new CardZones(cards.Catalog,cards.DrawPile,hands,cards.EraReserve,cards.DiscardPile.Append(card),cards.RemovedCards,
                cards.ResolvingCards,cards.China,cards.EnteredEras,cards.HeadlineReserved);
        }
    }
}
