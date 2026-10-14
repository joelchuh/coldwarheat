using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class ChinaCardDefinition
    {
        public CardCatalog Cards { get; } public MapDefinition Map { get; } public CardId Card { get; } public OpsRegionalBonus Bonus { get; }
        public ChinaCardDefinition(CardCatalog cards,MapDefinition map,CardId card,OpsRegionalBonus bonus)
        {
            if(cards==null || map==null || bonus==null)throw new ArgumentNullException();
            if(card.Value!="card.china" || cards.GetCard(card).Kind!=CardKind.China || map.GetCountriesInRegion(bonus.Region).Count==0)throw new ArgumentException("Invalid China card definition.");
            Cards=cards;Map=map;Card=card;Bonus=bonus;
        }
    }
    public static class ChinaCardRules
    {
        public static RuleError CanUse(ChinaCardDefinition definition,CardZones cards,PlayerSide actor,int remainingActions)
        {
            if(definition==null)return RuleError.UnsupportedCardAction;
            if(cards==null || actor!=PlayerSide.US && actor!=PlayerSide.USSR)return RuleError.InvalidCommand;
            if(!cards.China.Id.Equals(definition.Card))return RuleError.InvalidDefinition;
            if(cards.China.Owner!=actor)return RuleError.CardNotOwned;
            if(cards.China.Face!=CardFace.FaceUp)return RuleError.ChinaUnavailable;
            if(remainingActions<1)return RuleError.ActionSliceComplete;
            return cards.GetHand(actor).Count(id=>definition.Cards.GetCard(id).Kind==CardKind.Scoring)>=remainingActions?RuleError.ScoringMustBePlayed:RuleError.None;
        }
        public static CardZones TransferOnUse(ChinaCardDefinition definition,CardZones cards,PlayerSide actor)
        {
            if(definition==null || cards==null)throw new ArgumentNullException();
            if(cards.China.Owner!=actor || cards.China.Face!=CardFace.FaceUp || !cards.China.Id.Equals(definition.Card))throw new ArgumentException("Only the face-up holder can use China.");
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,cards.GetHand(PlayerSide.US)},{PlayerSide.USSR,cards.GetHand(PlayerSide.USSR)}};
            return new CardZones(definition.Cards,cards.DrawPile,hands,cards.EraReserve,cards.DiscardPile,cards.RemovedCards,cards.ResolvingCards,
                new ChinaCardState(definition.Card,actor==PlayerSide.US?PlayerSide.USSR:PlayerSide.US,CardFace.FaceDown),cards.EnteredEras);
        }
    }
}
