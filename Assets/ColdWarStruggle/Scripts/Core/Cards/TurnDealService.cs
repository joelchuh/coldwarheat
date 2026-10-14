using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class TurnDealPlan
    {
        public CardZones Before { get; }
        public CardZones Cards { get; }
        public GameTurnContext Turn { get; }
        public RandomSnapshot Random { get; }
        public int UsDealt { get; }
        public int UssrDealt { get; }
        public bool EraAdded { get; }
        public int DiscardReshuffles { get; }
        internal TurnDealPlan(CardZones before,CardZones cards,GameTurnContext turn,RandomSnapshot random,int usDealt,int ussrDealt,bool eraAdded,int reshuffles)
        { Before=before;Cards=cards;Turn=turn;Random=random;UsDealt=usDealt;UssrDealt=ussrDealt;EraAdded=eraAdded;DiscardReshuffles=reshuffles; }
    }
    public static class TurnDealService
    {
        public static TurnDealPlan Plan(CardCatalog catalog,CardZones cards,GameTurnContext turn,IRandomProvider random)
        {
            if (catalog == null || cards == null || turn == null || random == null) throw new ArgumentNullException();
            if (cards.ResolvingCards.Count != 0) throw new ArgumentException("Resolve all cards before a turn deal.");
            // Revalidate the complete partition against this authority's catalog, before using any random output.
            var hands = new Dictionary<PlayerSide,IReadOnlyList<CardId>>
            {
                { PlayerSide.US, cards.GetHand(PlayerSide.US) },
                { PlayerSide.USSR, cards.GetHand(PlayerSide.USSR) }
            };
            new CardZones(catalog,cards.DrawPile,hands,cards.EraReserve,cards.DiscardPile,
                cards.RemovedCards,cards.ResolvingCards,cards.China,cards.EnteredEras);
            var lastEra = cards.EnteredEras[cards.EnteredEras.Count - 1];
            bool addEra = lastEra != turn.Era;
            if (addEra && ((int)turn.Era != (int)lastEra + 1 || turn.Number != turn.Parameters.FirstTurn))
                throw new ArgumentException("Only the next era may enter, on its first turn.");
            var arriving = addEra
                ? cards.EraReserve.Where(id => catalog.GetCard(id).Era == turn.Era).ToArray()
                : Array.Empty<CardId>();
            int target = turn.Parameters.HandTarget;
            long usNeed = Math.Max(0L,(long)target - hands[PlayerSide.US].Count);
            long suNeed = Math.Max(0L,(long)target - hands[PlayerSide.USSR].Count);
            if (usNeed + suNeed > (long)cards.DrawPile.Count + arriving.Length + cards.DiscardPile.Count)
                throw new ArgumentException("Insufficient available cards for the requested hand targets.");

            var working = random.Fork();
            var draw = cards.DrawPile.Concat(arriving).ToList();
            if (addEra) Shuffle(draw,working);
            var discard = cards.DiscardPile.ToList();
            var us = hands[PlayerSide.US].ToList();
            var su = hands[PlayerSide.USSR].ToList();
            int cursor = 0, reshuffles = 0;
            void DealOne(List<CardId> hand)
            {
                if (hand.Count >= target) return;
                if (cursor == draw.Count)
                {
                    // Refill only when another draw is needed; never mix discards into an era shuffle.
                    draw = discard; discard = new List<CardId>(); cursor = 0;
                    Shuffle(draw,working); reshuffles++;
                }
                hand.Add(draw[cursor++]);
            }
            while (us.Count < target || su.Count < target)
            {
                DealOne(su);
                DealOne(us);
            }
            var nextHands = new Dictionary<PlayerSide,IReadOnlyList<CardId>>
                {{ PlayerSide.US,us },{ PlayerSide.USSR,su }};
            var entered = cards.EnteredEras.ToList();
            if (addEra) entered.Add(turn.Era);
            var remainingReserve = cards.EraReserve.Where(id => !addEra || catalog.GetCard(id).Era != turn.Era);
            var next = new CardZones(catalog,draw.Skip(cursor),nextHands,remainingReserve,discard,
                cards.RemovedCards,cards.ResolvingCards,cards.China,entered);
            return new TurnDealPlan(cards,next,turn,working.Capture(),us.Count-hands[PlayerSide.US].Count,
                su.Count-hands[PlayerSide.USSR].Count,addEra,reshuffles);
        }
        static void Shuffle(List<CardId> cards,IRandomProvider random)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = random.NextInt(0,i+1);
                var card = cards[i]; cards[i] = cards[j]; cards[j] = card;
            }
        }
    }
}
