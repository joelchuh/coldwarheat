using System;
using System.Collections.Generic;
using System.Linq;
namespace ColdWarStruggle.Core
{
    public enum CardFace { FaceUp, FaceDown }
    public sealed class ChinaCardState
    {
        public CardId Id { get; }
        public PlayerSide Owner { get; }
        public CardFace Face { get; }
        public ChinaCardState(CardId id,PlayerSide owner,CardFace face)
        {
            if(id.Value!="card.china" || !Enum.IsDefined(typeof(PlayerSide),owner) || !Enum.IsDefined(typeof(CardFace),face))
                throw new ArgumentException("Invalid China card state.");
            Id=id;Owner=owner;Face=face;
        }
    }
    public sealed class CardZones
    {
        public IReadOnlyList<CardId> DrawPile { get; }
        public IReadOnlyList<CardId> EraReserve { get; }
        public IReadOnlyList<CardId> DiscardPile { get; }
        public IReadOnlyList<CardId> RemovedCards { get; }
        public IReadOnlyList<CardId> ResolvingCards { get; }
        public IReadOnlyList<GameEra> EnteredEras { get; }
        public ChinaCardState China { get; }
        readonly IReadOnlyList<CardId> usHand;
        readonly IReadOnlyList<CardId> ussrHand;
        public CardZones(CardCatalog catalog,IEnumerable<CardId> drawPile,IReadOnlyDictionary<PlayerSide,IReadOnlyList<CardId>> hands,
            IEnumerable<CardId> reserve,IEnumerable<CardId> discard,IEnumerable<CardId> removed,IEnumerable<CardId> resolving,
            ChinaCardState china,IEnumerable<GameEra> enteredEras)
        {
            if(catalog==null || china==null)throw new ArgumentNullException();
            if(catalog.Cards.Count(c=>c.Kind==CardKind.China)!=1 || catalog.GetCard(china.Id).Kind!=CardKind.China)
                throw new ArgumentException("Catalog requires the separate China card.");
            var eras=enteredEras?.Take(4).ToArray();
            if(eras==null || eras.Length<1 || eras.Length>3 || eras.Distinct().Count()!=eras.Length || eras.Any(e=>!Enum.IsDefined(typeof(GameEra),e)))
                throw new ArgumentException("Invalid entered eras.");
            eras=eras.OrderBy(e=>e).ToArray();
            var expected=new[]{GameEra.Early,GameEra.Mid,GameEra.Late};
            for(int i=0;i<eras.Length;i++)if(eras[i]!=expected[i])throw new ArgumentException("Entered eras must be consecutive from Early.");
            if(hands==null || hands.Count!=2 || !hands.ContainsKey(PlayerSide.US) || !hands.ContainsKey(PlayerSide.USSR))
                throw new ArgumentException("Both player hands are required.");
            var seen=new HashSet<CardId>{china.Id};
            IReadOnlyList<CardId> Copy(IEnumerable<CardId> source,bool reserveZone=false,bool sort=false)
            {
                var values=source?.Take(catalog.Cards.Count+1).ToArray();
                if(values==null || values.Length>=catalog.Cards.Count)throw new ArgumentException("Invalid card zone size.");
                foreach(var id in values)
                {
                    var card=catalog.GetCard(id);
                    if(card.Kind==CardKind.China || !seen.Add(id))throw new ArgumentException("Card occurs in multiple zones.");
                    if(eras.Contains(card.Era.Value)==reserveZone)throw new ArgumentException("Card era does not match its zone.");
                }
                if(sort)values=values.OrderBy(id=>id.Value,StringComparer.Ordinal).ToArray();
                return Array.AsReadOnly(values);
            }
            DrawPile=Copy(drawPile);usHand=Copy(hands[PlayerSide.US]);ussrHand=Copy(hands[PlayerSide.USSR]);
            EraReserve=Copy(reserve,true,true);DiscardPile=Copy(discard);RemovedCards=Copy(removed,sort:true);ResolvingCards=Copy(resolving);
            if(seen.Count!=catalog.Cards.Count)throw new ArgumentException("Card partition is incomplete.");
            China=china;EnteredEras=Array.AsReadOnly(eras);
        }
        public IReadOnlyList<CardId> GetHand(PlayerSide side)=>side switch
        {
            PlayerSide.US=>usHand,PlayerSide.USSR=>ussrHand,_=>throw new ArgumentOutOfRangeException(nameof(side))
        };
    }
}
