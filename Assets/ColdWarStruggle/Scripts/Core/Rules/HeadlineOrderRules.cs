using System;
using System.Collections.Generic;
namespace ColdWarStruggle.Core
{
    public sealed class HeadlineOrderParameters
    {
        public int ScoringValue { get; }
        public PlayerSide TieFirstSide { get; }
        public HeadlineOrderParameters(int scoringValue,PlayerSide tieFirstSide)
        {
            if(scoringValue<0)throw new ArgumentOutOfRangeException(nameof(scoringValue));
            if(tieFirstSide!=PlayerSide.US && tieFirstSide!=PlayerSide.USSR)throw new ArgumentOutOfRangeException(nameof(tieFirstSide));
            ScoringValue=scoringValue;TieFirstSide=tieFirstSide;
        }
    }
    public sealed class HeadlineOrderEntry
    {
        public CardId Card { get; }
        public PlayerSide PlayedBy { get; }
        public int Value { get; }
        internal HeadlineOrderEntry(CardId card,PlayerSide playedBy,int value){Card=card;PlayedBy=playedBy;Value=value;}
    }
    public static class HeadlineOrderRules
    {
        // Ordinary candidate ordering only. The future resolver must handle special priority/cancellation
        // (including Defectors) before using it as an execution queue. No event is run here.
        public static IReadOnlyList<HeadlineOrderEntry> BuildOrdinaryOrder(HeadlineOrderParameters parameters,CardCatalog catalog,CardId usCard,CardId ussrCard)
        {
            if(parameters==null || catalog==null)throw new ArgumentNullException();
            if(usCard.Equals(ussrCard))throw new ArgumentException("One physical card cannot be both headlines.");
            HeadlineOrderEntry Entry(CardId id,PlayerSide side)
            {
                var card=catalog.GetCard(id);
                if(card.Kind==CardKind.China)throw new ArgumentException("China cannot be a headline.");
                return new HeadlineOrderEntry(id,side,card.Kind==CardKind.Scoring?parameters.ScoringValue:card.PrintedOps.Value);
            }
            var us=Entry(usCard,PlayerSide.US);var su=Entry(ussrCard,PlayerSide.USSR);
            bool usFirst=us.Value>su.Value || us.Value==su.Value && parameters.TieFirstSide==PlayerSide.US;
            return Array.AsReadOnly(usFirst?new[]{us,su}:new[]{su,us});
        }
    }
}
