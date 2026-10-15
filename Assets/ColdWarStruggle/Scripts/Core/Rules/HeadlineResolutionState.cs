using System;
using System.Collections.Generic;
using System.Globalization;
namespace ColdWarStruggle.Core
{
    // Authority-owned ordinary candidate cursor. Advancing it is not proof that an effect ran;
    // the coordinator must finalize that root transaction before advancing.
    public sealed class HeadlineResolutionState
    {
        public GameTurnContext Turn { get; }
        public IReadOnlyList<HeadlineOrderEntry> Order { get; }
        public int CompletedCount { get; }
        public bool IsComplete=>CompletedCount==Order.Count;
        public HeadlineOrderEntry Current=>IsComplete?null:Order[CompletedCount];
        public string CurrentResolutionId=>IsComplete?null:"headline.turn."+Turn.Number.ToString(CultureInfo.InvariantCulture)+"."+Current.PlayedBy;
        HeadlineResolutionState(GameTurnContext turn,IReadOnlyList<HeadlineOrderEntry> order,int completed)
        {Turn=turn;Order=order;CompletedCount=completed;}
        public static HeadlineResolutionState CreateOrdinary(HeadlineOrderParameters parameters,CardCatalog catalog,CardZones cards,GameTurnContext turn)
        {
            if(parameters==null || catalog==null || cards==null || turn==null)throw new ArgumentNullException();
            if(cards.HeadlineReserved.Count!=2 || cards.ResolvingCards.Count!=0)throw new ArgumentException("Two reserved headlines and no active resolution are required.");
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,cards.GetHand(PlayerSide.US)},{PlayerSide.USSR,cards.GetHand(PlayerSide.USSR)}};
            _=new CardZones(catalog,cards.DrawPile,hands,cards.EraReserve,cards.DiscardPile,cards.RemovedCards,cards.ResolvingCards,cards.China,cards.EnteredEras,cards.HeadlineReserved);
            var order=HeadlineOrderRules.BuildOrdinaryOrder(parameters,catalog,cards.GetHeadline(PlayerSide.US).Value,cards.GetHeadline(PlayerSide.USSR).Value);
            return new HeadlineResolutionState(turn,order,0);
        }
        public HeadlineResolutionState AdvanceAfterResolution(CardId card,PlayerSide playedBy)
        {
            if(IsComplete || !Current.Card.Equals(card) || Current.PlayedBy!=playedBy)throw new ArgumentException("Only the current headline root may complete once.");
            return new HeadlineResolutionState(Turn,Order,CompletedCount+1);
        }
    }
}
