using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public sealed class InitialDealDefinition
    {
        public GameTurnContext Turn { get; }
        public int HandTarget=>Turn.Parameters.HandTarget;
        public ChinaCardState China { get; }
        public InitialDealDefinition(GameTurnContext turn,ChinaCardState china)
        {
            if(turn==null || china==null)throw new ArgumentNullException();
            if(turn.Number!=1 || turn.Era!=GameEra.Early || china.Owner!=PlayerSide.USSR || china.Face!=CardFace.FaceUp)
                throw new ArgumentException("Unsupported initial deal definition.");
            Turn=turn;China=china;
        }
    }
    public sealed class InitialDealResult
    {
        public CardZones Cards { get; }
        public RandomSnapshot Random { get; }
        internal InitialDealResult(CardZones cards,RandomSnapshot random) { Cards=cards;Random=random; }
    }
    public static class InitialDealService
    {
        public static InitialDealResult Create(CardCatalog catalog,InitialDealDefinition definition,IRandomProvider random)
        {
            if(catalog==null || definition==null || random==null)throw new ArgumentNullException();
            if(catalog.Cards.Count(c=>c.Kind==CardKind.China)!=1 || catalog.GetCard(definition.China.Id).Kind!=CardKind.China)
                throw new ArgumentException("Catalog requires the separate China card.");
            var shuffled=catalog.GetEraCards(definition.Turn.Era).Select(c=>c.Id).ToArray();
            if(shuffled.Length<2L*definition.HandTarget)throw new ArgumentException("Insufficient initial era cards.");
            // A caller must commit the returned cards and random snapshot together.
            var working=random.Fork();
            for(int i=shuffled.Length-1;i>0;i--)
            {
                int j=working.NextInt(0,i+1);
                var card=shuffled[i];shuffled[i]=shuffled[j];shuffled[j]=card;
            }
            var ussr=new CardId[definition.HandTarget];var us=new CardId[definition.HandTarget];int cursor=0;
            for(int i=0;i<definition.HandTarget;i++){ussr[i]=shuffled[cursor++];us[i]=shuffled[cursor++];}
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.USSR,ussr},{PlayerSide.US,us}};
            var reserve=catalog.Cards.Where(c=>c.DeckEntry==CardDeckEntry.EraDeck && c.Era!=definition.Turn.Era).Select(c=>c.Id);
            var empty=Array.Empty<CardId>();
            var zones=new CardZones(catalog,shuffled.Skip(cursor),hands,reserve,empty,empty,empty,definition.China,new[]{definition.Turn.Era});
            return new InitialDealResult(zones,working.Capture());
        }
    }
}
