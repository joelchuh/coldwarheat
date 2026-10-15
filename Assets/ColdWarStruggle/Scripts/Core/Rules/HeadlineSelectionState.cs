using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    // Authority-only selection snapshot. It does not execute cards or authorize a game phase.
    public sealed class HeadlineSelectionState
    {
        public CardCatalog Catalog { get; }
        public CardZones Cards { get; }
        public IReadOnlyList<CardId> SupportedCards { get; }
        public PlayerSide? ChoosesSecond { get; }
        public bool Ready=>Cards.HeadlineReserved.Count==2;
        HeadlineSelectionState(CardCatalog catalog,CardZones cards,IReadOnlyList<CardId> supported,PlayerSide? second)
        {Catalog=catalog;Cards=cards;SupportedCards=supported;ChoosesSecond=second;}
        public static HeadlineSelectionState Begin(CardCatalog catalog,CardZones cards,SpaceTrackState track,IEnumerable<CardId> supportedCards)
        {
            if(catalog==null || cards==null || track==null || supportedCards==null)throw new ArgumentNullException();
            if(cards.HeadlineReserved.Count!=0 || cards.ResolvingCards.Count!=0)throw new ArgumentException("A new collection requires no reserved or resolving cards.");
            var supported=supportedCards.Take(catalog.Cards.Count+1).ToArray();
            if(supported.Length>catalog.Cards.Count || supported.Distinct().Count()!=supported.Length
                || supported.Any(id=>!catalog.Contains(id) || catalog.GetCard(id).Kind==CardKind.China))
                throw new ArgumentException("Invalid supported headline set.");
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,cards.GetHand(PlayerSide.US)},{PlayerSide.USSR,cards.GetHand(PlayerSide.USSR)}};
            // Revalidate against this authority's catalog; a valid partition from another catalog is insufficient.
            var validated=new CardZones(catalog,cards.DrawPile,hands,cards.EraReserve,cards.DiscardPile,cards.RemovedCards,cards.ResolvingCards,cards.China,cards.EnteredEras,cards.HeadlineReserved);
            PlayerSide? second=SpaceAbilityRules.Query(track,PlayerSide.US).OpponentRevealsHeadlineFirst?PlayerSide.US:
                SpaceAbilityRules.Query(track,PlayerSide.USSR).OpponentRevealsHeadlineFirst?PlayerSide.USSR:(PlayerSide?)null;
            return new HeadlineSelectionState(catalog,validated,Array.AsReadOnly(supported.OrderBy(id=>id.Value,StringComparer.Ordinal).ToArray()),second);
        }
        internal HeadlineSelectionState WithCards(CardZones cards)=>new HeadlineSelectionState(Catalog,cards,SupportedCards,ChoosesSecond);
    }
}
