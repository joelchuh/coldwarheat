using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Application
{
    public sealed class PublicCardSummary
    {
        public int DrawPileCount { get; }
        public int EraReserveCount { get; }
        public int DiscardCount { get; }
        public int RemovedCount { get; }
        public int ResolvingCount { get; }
        public ChinaCardState China { get; }
        public IReadOnlyList<GameEra> EnteredEras { get; }
        internal PublicCardSummary(CardZones cards)
        {
            if(cards==null)throw new ArgumentNullException(nameof(cards));
            DrawPileCount=cards.DrawPile.Count;EraReserveCount=cards.EraReserve.Count;DiscardCount=cards.DiscardPile.Count;
            RemovedCount=cards.RemovedCards.Count;ResolvingCount=cards.ResolvingCards.Count;China=cards.China;
            EnteredEras=Array.AsReadOnly(cards.EnteredEras.ToArray());
        }
    }
}
