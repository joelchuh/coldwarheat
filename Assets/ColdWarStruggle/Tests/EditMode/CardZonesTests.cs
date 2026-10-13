using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class CardZonesTests
    {
        public static CardId Id(string name)=>new CardId("card."+name);
        public static ChinaCardState China()=>new ChinaCardState(Id("china"),PlayerSide.USSR,CardFace.FaceUp);
        public static CardCatalog Catalog()
        {
            var list=new List<CardDefinition>();
            foreach(var name in new[]{"a","b","c","d","m","z"})
            {
                var era=name=="m"?GameEra.Mid:name=="z"?GameEra.Late:GameEra.Early;
                list.Add(new CardDefinition(Id(name),list.Count+1,"name","rules",CardKind.Event,2,CardAffiliation.Neutral,era,CardDeckEntry.EraDeck,false,false,null,CardCatalogTests.Effect()));
            }
            list.Add(new CardDefinition(Id("china"),7,"name","rules",CardKind.China,4,CardAffiliation.None,null,CardDeckEntry.SpecialChina,false,false,null,null));
            return new CardCatalog(list);
        }
        public static Dictionary<PlayerSide,IReadOnlyList<CardId>> Hands()=>new Dictionary<PlayerSide,IReadOnlyList<CardId>>
            {{PlayerSide.USSR,new[]{Id("b")}},{PlayerSide.US,new[]{Id("c")}}};
        public static CardZones Zones(IEnumerable<CardId> draw=null,IReadOnlyDictionary<PlayerSide,IReadOnlyList<CardId>> hands=null,
            IEnumerable<CardId> reserve=null,IEnumerable<CardId> discard=null,IEnumerable<CardId> removed=null,IEnumerable<CardId> resolving=null,
            IEnumerable<GameEra> eras=null,CardCatalog catalog=null)=>new CardZones(catalog??Catalog(),draw??new[]{Id("d"),Id("a")},hands??Hands(),
                reserve??new[]{Id("z"),Id("m")},discard??Array.Empty<CardId>(),removed??Array.Empty<CardId>(),resolving??Array.Empty<CardId>(),China(),eras??new[]{GameEra.Early});
        [Test] public void Zones_copy_inputs_preserve_deck_order_and_canonicalize_reserve()
        {
            var draw=new List<CardId>{Id("d"),Id("a")};var hand=new List<CardId>{Id("b")};var hands=Hands();hands[PlayerSide.USSR]=hand;
            var reserve=new List<CardId>{Id("z"),Id("m")};var eras=new List<GameEra>{GameEra.Early};var state=Zones(draw,hands,reserve,eras:eras);
            draw.Clear();hand.Clear();reserve.Clear();hands.Clear();eras.Clear();
            Assert.That(state.DrawPile,Is.EqualTo(new[]{Id("d"),Id("a")}));Assert.That(state.GetHand(PlayerSide.USSR),Is.EqualTo(new[]{Id("b")}));
            Assert.That(state.EraReserve,Is.EqualTo(new[]{Id("m"),Id("z")}));Assert.That(state.EnteredEras,Is.EqualTo(new[]{GameEra.Early}));
            Assert.Throws<NotSupportedException>(()=>((IList<CardId>)state.DrawPile)[0]=Id("b"));
            Assert.Throws<NotSupportedException>(()=>((IList<CardId>)state.GetHand(PlayerSide.USSR))[0]=Id("a"));
            Assert.That(()=>state.GetHand((PlayerSide)99),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("duplicate-deck")] [TestCase("duplicate-hand")] [TestCase("duplicate-discard")] [TestCase("duplicate-removed")] [TestCase("duplicate-resolving")]
        [TestCase("missing")] [TestCase("unknown")] [TestCase("default-id")] [TestCase("china-in-deck")] [TestCase("unentered-in-hand")]
        [TestCase("entered-in-reserve")] [TestCase("missing-hand")] [TestCase("extra-hand")] [TestCase("null-hand")]
        [TestCase("missing-china-definition")] [TestCase("unbounded-input")]
        public void Invalid_zone_partition_is_rejected(string defect)
        {
            var draw=new List<CardId>{Id("d"),Id("a")};var hands=Hands();var reserve=new List<CardId>{Id("m"),Id("z")};
            var discard=new List<CardId>();var removed=new List<CardId>();var resolving=new List<CardId>();var catalog=Catalog();
            switch(defect)
            {
                case "duplicate-deck":draw.Add(Id("a"));break;
                case "duplicate-hand":hands[PlayerSide.US]=new[]{Id("b")};break;
                case "duplicate-discard":discard.Add(Id("a"));break;
                case "duplicate-removed":removed.Add(Id("a"));break;
                case "duplicate-resolving":resolving.Add(Id("a"));break;
                case "missing":draw.RemoveAt(0);break;
                case "unknown":draw[0]=Id("unknown");break;
                case "default-id":draw[0]=default;break;
                case "china-in-deck":draw.Add(Id("china"));break;
                case "unentered-in-hand":hands[PlayerSide.US]=new[]{Id("z")};reserve.Remove(Id("z"));draw.Add(Id("c"));break;
                case "entered-in-reserve":draw.Remove(Id("a"));reserve.Add(Id("a"));break;
                case "missing-hand":hands.Remove(PlayerSide.US);break;
                case "extra-hand":hands[(PlayerSide)99]=Array.Empty<CardId>();break;
                case "null-hand":hands[PlayerSide.US]=null;break;
                case "missing-china-definition":catalog=new CardCatalog(catalog.Cards.Where(c=>c.Kind!=CardKind.China));break;
                case "unbounded-input":draw=Enumerable.Repeat(Id("a"),300).ToList();break;
            }
            Assert.That(()=>Zones(draw,hands,reserve,discard,removed,resolving,catalog:catalog),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Every_ordinary_zone_participates_in_completeness_and_future_era_validation()
        {
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,Array.Empty<CardId>()},{PlayerSide.USSR,Array.Empty<CardId>()}};
            var state=Zones(new[]{Id("d")},hands,new[]{Id("z")},new[]{Id("a"),Id("m")},new[]{Id("c")},new[]{Id("b")},new[]{GameEra.Mid,GameEra.Early});
            Assert.That(state.DiscardPile,Is.EqualTo(new[]{Id("a"),Id("m")}));Assert.That(state.RemovedCards.Single(),Is.EqualTo(Id("c")));Assert.That(state.ResolvingCards.Single(),Is.EqualTo(Id("b")));
            Assert.That(state.EnteredEras,Is.EqualTo(new[]{GameEra.Early,GameEra.Mid}));Assert.That(state.China.Id,Is.EqualTo(Id("china")));
        }
        [Test] public void Invalid_entered_eras_and_china_state_are_rejected()
        {
            foreach(var eras in new[]{Array.Empty<GameEra>(),new[]{GameEra.Early,GameEra.Early},new[]{GameEra.Mid},new[]{GameEra.Early,GameEra.Late},new[]{(GameEra)99}})
                Assert.That(()=>Zones(eras:eras),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new ChinaCardState(Id("a"),PlayerSide.US,CardFace.FaceUp),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new ChinaCardState(Id("china"),(PlayerSide)99,CardFace.FaceUp),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new ChinaCardState(Id("china"),PlayerSide.US,(CardFace)99),Throws.InstanceOf<ArgumentException>());
            Assert.DoesNotThrow(()=>new ChinaCardState(Id("china"),PlayerSide.US,CardFace.FaceDown));
        }
        [Test] public void Null_required_collections_catalog_and_china_are_rejected()
        {
            var c=Catalog();var e=Array.Empty<CardId>();var eras=new[]{GameEra.Early};var china=China();
            Assert.That(()=>new CardZones(null,e,Hands(),e,e,e,e,china,eras),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardZones(c,null,Hands(),e,e,e,e,china,eras),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardZones(c,e,null,e,e,e,e,china,eras),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardZones(c,e,Hands(),null,e,e,e,china,eras),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardZones(c,e,Hands(),e,null,e,e,china,eras),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardZones(c,e,Hands(),e,e,null,e,china,eras),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardZones(c,e,Hands(),e,e,e,null,china,eras),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardZones(c,e,Hands(),e,e,e,e,null,eras),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new CardZones(c,e,Hands(),e,e,e,e,china,null),Throws.InstanceOf<ArgumentException>());
        }
    }
}
