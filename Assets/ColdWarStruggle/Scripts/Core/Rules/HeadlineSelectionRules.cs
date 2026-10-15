using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    // Internal plan, never a network response: State contains both private hands and reservations.
    public sealed class HeadlineSelectionResult
    {
        public RuleError Error { get; }
        public bool Accepted=>Error==RuleError.None;
        public HeadlineSelectionState State { get; }
        internal HeadlineSelectionResult(RuleError error,HeadlineSelectionState state){Error=error;State=state;}
    }
    public static class HeadlineSelectionRules
    {
        static bool MayRespond(HeadlineSelectionState state,PlayerSide side)
        {
            var other=side==PlayerSide.US?PlayerSide.USSR:PlayerSide.US;
            return !state.Ready && !state.Cards.GetHeadline(side).HasValue
                && (state.ChoosesSecond!=side || state.Cards.GetHeadline(other).HasValue);
        }
        public static bool CanChoose(HeadlineSelectionState state,PlayerSide side)
        {
            if(state==null)throw new ArgumentNullException(nameof(state));
            if(side!=PlayerSide.US && side!=PlayerSide.USSR)throw new ArgumentOutOfRangeException(nameof(side));
            return MayRespond(state,side) && state.Cards.GetHand(side).Any(id=>state.SupportedCards.Contains(id));
        }
        public static HeadlineSelectionResult PlanSelection(HeadlineSelectionState state,PlayerSide side,CardId card)
        {
            if(state==null)throw new ArgumentNullException(nameof(state));
            HeadlineSelectionResult Reject(RuleError error)=>new HeadlineSelectionResult(error,state);
            if(side!=PlayerSide.US && side!=PlayerSide.USSR)return Reject(RuleError.InvalidCommand);
            if(!MayRespond(state,side))return Reject(RuleError.ActionNotReady);
            if(!state.Catalog.Contains(card))return Reject(RuleError.InvalidCommand);
            if(state.Catalog.GetCard(card).Kind==CardKind.China)return Reject(RuleError.UnsupportedCardAction);
            if(!state.Cards.GetHand(side).Contains(card))return Reject(RuleError.CardNotOwned);
            if(!state.SupportedCards.Contains(card))return Reject(RuleError.UnsupportedCardAction);
            var c=state.Cards;var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();
            foreach(var owner in new[]{PlayerSide.US,PlayerSide.USSR})hands.Add(owner,c.GetHand(owner).Where(id=>owner!=side || !id.Equals(card)).ToArray());
            var reserved=c.HeadlineReserved.ToDictionary(pair=>pair.Key,pair=>pair.Value);reserved.Add(side,card);
            var next=new CardZones(state.Catalog,c.DrawPile,hands,c.EraReserve,c.DiscardPile,c.RemovedCards,c.ResolvingCards,c.China,c.EnteredEras,reserved);
            return new HeadlineSelectionResult(RuleError.None,state.WithCards(next));
        }
    }
}
