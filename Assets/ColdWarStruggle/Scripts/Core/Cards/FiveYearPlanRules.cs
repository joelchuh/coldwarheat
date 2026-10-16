using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum FiveYearPlanRoute { EmptyHand, Discarded, AwaitingUsEvent }
    // Authority only: Before/Cards contain hidden physical zones, never send this plan to a player.
    public sealed class FiveYearPlanSelection
    {
        public CardZones Before { get; }
        public CardZones Cards { get; }
        public RandomSnapshot RandomBefore { get; }
        public RandomSnapshot RandomAfter { get; }
        public CardId? SelectedCard { get; }
        public FiveYearPlanRoute Route { get; }
        internal FiveYearPlanSelection(CardZones before,CardZones cards,RandomSnapshot randomBefore,RandomSnapshot randomAfter,CardId? selected,FiveYearPlanRoute route)
        {Before=before;Cards=cards;RandomBefore=randomBefore;RandomAfter=randomAfter;SelectedCard=selected;Route=route;}
    }
    public static class FiveYearPlanRules
    {
        public static FiveYearPlanSelection Plan(FiveYearPlanDefinition definition,CardZones cards,RandomSnapshot random,IReadOnlyCollection<CardId> supportedUsEvents)
        {
            if(definition==null || cards==null || random==null || supportedUsEvents==null)throw new ArgumentNullException();
            if(!ReferenceEquals(definition.Cards,cards.Catalog) || cards.ResolvingCards.Count!=1 || !cards.ResolvingCards[0].Equals(definition.Card.Id))
                throw new ArgumentException("Expected this catalog's unique resolving Five Year Plan source.");
            var supported=supportedUsEvents.Take(definition.Cards.Cards.Count+1).ToArray();
            if(supported.Length>definition.Cards.Cards.Count || supported.Distinct().Count()!=supported.Length
                || supported.Any(id=>!definition.Cards.Contains(id) || definition.Cards.GetCard(id).Kind!=CardKind.Event || definition.Cards.GetCard(id).Affiliation!=definition.TriggeredAffiliation))
                throw new ArgumentException("Invalid authority US event capability set.");
            var candidates=cards.GetHand(definition.HandOwner).OrderBy(id=>id.Value,StringComparer.Ordinal).ToArray();
            // Preflight every possible US event, before drawing. A missing handler cannot bias selection.
            if(candidates.Any(id=>definition.Cards.GetCard(id).Affiliation==definition.TriggeredAffiliation && !supported.Contains(id)))
                throw new NotSupportedException("A possible child event is not supported by this authority.");
            if(candidates.Length==0)return new FiveYearPlanSelection(cards,cards,random,random,null,FiveYearPlanRoute.EmptyHand);
            var working=new Pcg32Random(random);var selected=candidates[working.NextInt(0,candidates.Length)];
            var child=definition.Cards.GetCard(selected).Affiliation==definition.TriggeredAffiliation;
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>();
            foreach(var side in new[]{PlayerSide.US,PlayerSide.USSR})hands.Add(side,side==definition.HandOwner?cards.GetHand(side).Where(id=>!id.Equals(selected)).ToArray():cards.GetHand(side));
            var next=new CardZones(definition.Cards,cards.DrawPile,hands,cards.EraReserve,child?cards.DiscardPile:cards.DiscardPile.Append(selected),cards.RemovedCards,
                child?cards.ResolvingCards.Append(selected):cards.ResolvingCards,cards.China,cards.EnteredEras,cards.HeadlineReserved);
            return new FiveYearPlanSelection(cards,next,random,working.Capture(),selected,child?FiveYearPlanRoute.AwaitingUsEvent:FiveYearPlanRoute.Discarded);
        }
    }
}
