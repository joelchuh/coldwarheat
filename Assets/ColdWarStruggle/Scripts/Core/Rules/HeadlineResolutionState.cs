using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace ColdWarStruggle.Core
{
    // Authority-owned selected-headline cursor. Root records do not prove physical disposal:
    // the coordinator must complete each effect/cancellation transaction before advancing.
    public sealed class HeadlineResolutionState
    {
        readonly DefectorsHeadlineDefinition defectors;
        public HeadlineOrderStrategy OrderStrategy { get; }
        public IReadOnlyList<HeadlineRootCompletion> CompletedRoots { get; }
        public CardId? CurrentCancellationSource
        {
            get
            {
                if(IsComplete || defectors==null || Current.PlayedBy!=PlayerSide.USSR)return null;
                if(Current.Card.Equals(defectors.Card))return defectors.Card;
                if(OrderStrategy==HeadlineOrderStrategy.USSelectedDefectorsFirst && CompletedRoots.Count==1
                    && CompletedRoots[0].PlayedBy==PlayerSide.US && CompletedRoots[0].Card.Equals(defectors.Card)
                    && CompletedRoots[0].Outcome==HeadlineRootOutcome.Resolved)return defectors.Card;
                return null;
            }
        }
        public GameTurnContext Turn { get; }
        public IReadOnlyList<HeadlineOrderEntry> Order { get; }
        public int CompletedCount=>CompletedRoots.Count;
        public bool IsComplete=>CompletedCount==Order.Count;
        public HeadlineOrderEntry Current=>IsComplete?null:Order[CompletedCount];
        public string CurrentResolutionId=>IsComplete?null:"headline.turn."+Turn.Number.ToString(CultureInfo.InvariantCulture)+"."+Current.PlayedBy;
        HeadlineResolutionState(GameTurnContext turn,IReadOnlyList<HeadlineOrderEntry> order,HeadlineOrderStrategy strategy=HeadlineOrderStrategy.OrdinaryPrintedValue,
            DefectorsHeadlineDefinition definition=null,IEnumerable<HeadlineRootCompletion> completed=null)
        {Turn=turn;Order=order;OrderStrategy=strategy;defectors=definition;CompletedRoots=Array.AsReadOnly((completed??Array.Empty<HeadlineRootCompletion>()).ToArray());}
        public static HeadlineResolutionState CreateSelected(HeadlineOrderParameters parameters,CardCatalog catalog,CardZones cards,GameTurnContext turn,DefectorsHeadlineDefinition defectors)
        {
            if(defectors!=null && !ReferenceEquals(defectors.Catalog,catalog))throw new ArgumentException("Defectors must share the headline catalog.");
            var ordinary=CreateOrdinary(parameters,catalog,cards,turn);
            if(defectors==null)return ordinary;
            var priority=cards.GetHeadline(PlayerSide.US)?.Equals(defectors.Card)==true;
            var order=priority?Array.AsReadOnly(ordinary.Order.OrderBy(x=>x.PlayedBy==PlayerSide.US?0:1).ToArray()):ordinary.Order;
            return new HeadlineResolutionState(turn,order,priority?HeadlineOrderStrategy.USSelectedDefectorsFirst:HeadlineOrderStrategy.OrdinaryPrintedValue,defectors);
        }
        public static HeadlineResolutionState CreateOrdinary(HeadlineOrderParameters parameters,CardCatalog catalog,CardZones cards,GameTurnContext turn)
        {
            if(parameters==null || catalog==null || cards==null || turn==null)throw new ArgumentNullException();
            if(cards.HeadlineReserved.Count!=2 || cards.ResolvingCards.Count!=0)throw new ArgumentException("Two reserved headlines and no active resolution are required.");
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,cards.GetHand(PlayerSide.US)},{PlayerSide.USSR,cards.GetHand(PlayerSide.USSR)}};
            _=new CardZones(catalog,cards.DrawPile,hands,cards.EraReserve,cards.DiscardPile,cards.RemovedCards,cards.ResolvingCards,cards.China,cards.EnteredEras,cards.HeadlineReserved);
            var order=HeadlineOrderRules.BuildOrdinaryOrder(parameters,catalog,cards.GetHeadline(PlayerSide.US).Value,cards.GetHeadline(PlayerSide.USSR).Value);
            return new HeadlineResolutionState(turn,order);
        }
        void RequireCurrent(CardId card,PlayerSide playedBy)
        {
            if(IsComplete || !Current.Card.Equals(card) || Current.PlayedBy!=playedBy)throw new ArgumentException("Only the current headline root may complete once.");
        }
        public HeadlineResolutionState AdvanceAfterResolution(CardId card,PlayerSide playedBy)
        {
            RequireCurrent(card,playedBy);
            if(CurrentCancellationSource.HasValue)throw new ArgumentException("A cancelled headline cannot be recorded as a successful event.");
            return After(new HeadlineRootCompletion(card,playedBy,HeadlineRootOutcome.Resolved));
        }
        public HeadlineResolutionState AdvanceAfterCancellation(CardId card,PlayerSide playedBy)
        {
            RequireCurrent(card,playedBy);var source=CurrentCancellationSource;
            if(!source.HasValue)throw new ArgumentException("This selected headline has no valid cancellation source.");
            return After(new HeadlineRootCompletion(card,playedBy,HeadlineRootOutcome.Cancelled,source));
        }
        HeadlineResolutionState After(HeadlineRootCompletion completion)=>new HeadlineResolutionState(Turn,Order,OrderStrategy,defectors,CompletedRoots.Concat(new[]{completion}));
    }
}
