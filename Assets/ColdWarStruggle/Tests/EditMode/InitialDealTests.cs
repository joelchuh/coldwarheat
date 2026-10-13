using System;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class InitialDealTests
    {
        public static InitialDealDefinition Definition(int handTarget=1)=>new InitialDealDefinition(new TurnCalendar(new[]{
            new EraTurnParameters(GameEra.Early,1,3,handTarget,6),new EraTurnParameters(GameEra.Mid,4,7,9,7),new EraTurnParameters(GameEra.Late,8,10,9,7)}).ForTurn(1),CardZonesTests.China());
        sealed class ZeroRandom : IRandomProvider
        {
            ulong count;
            public int NextInt(int min,int max) { Assert.That(min,Is.Zero);Assert.That(max,Is.GreaterThan(1));count++;return 0; }
            public RandomSnapshot Capture()=>new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,count,1,count);
            public IRandomProvider Fork()=>new ZeroRandom {count=count};
        }
        [Test] public void Known_shuffle_deals_from_front_alternating_ussr_then_us_and_keeps_china_separate()
        {
            var random=new ZeroRandom();var result=InitialDealService.Create(CardZonesTests.Catalog(),Definition(),random);var state=result.Cards;
            Assert.That(state.GetHand(PlayerSide.USSR),Is.EqualTo(new[]{CardZonesTests.Id("b")}));Assert.That(state.GetHand(PlayerSide.US),Is.EqualTo(new[]{CardZonesTests.Id("c")}));
            Assert.That(state.DrawPile,Is.EqualTo(new[]{CardZonesTests.Id("d"),CardZonesTests.Id("a")}));Assert.That(state.EraReserve,Has.Count.EqualTo(2));
            Assert.That(state.China.Owner,Is.EqualTo(PlayerSide.USSR));Assert.That(state.China.Face,Is.EqualTo(CardFace.FaceUp));
            Assert.That(result.Random.DrawCount,Is.EqualTo(3));Assert.That(random.Capture().DrawCount,Is.Zero);
        }
        [Test] public void Actual_catalog_produces_8_each_19_draw_67_reserve_and_one_china_deterministically()
        {
            var catalog=CardCatalogLoaderTests.Load();var random=Pcg32Random.FromSeed(42,54);var before=random.Capture();
            var a=InitialDealService.Create(catalog,Definition(8),random);var b=InitialDealService.Create(catalog,Definition(8),new Pcg32Random(before));
            Assert.That(a.Cards.GetHand(PlayerSide.USSR),Has.Count.EqualTo(8));Assert.That(a.Cards.GetHand(PlayerSide.US),Has.Count.EqualTo(8));Assert.That(a.Cards.DrawPile,Has.Count.EqualTo(19));Assert.That(a.Cards.EraReserve,Has.Count.EqualTo(67));
            Assert.That(a.Cards.DiscardPile.Concat(a.Cards.RemovedCards).Concat(a.Cards.ResolvingCards),Is.Empty);
            Assert.That(a.Cards.DrawPile,Is.EqualTo(b.Cards.DrawPile));Assert.That(a.Cards.GetHand(PlayerSide.US),Is.EqualTo(b.Cards.GetHand(PlayerSide.US)));Assert.That(a.Cards.GetHand(PlayerSide.USSR),Is.EqualTo(b.Cards.GetHand(PlayerSide.USSR)));
            Pcg32RandomTests.Same(a.Random,b.Random);Pcg32RandomTests.Same(before,random.Capture());
            Assert.That(a.Random.DrawCount,Is.EqualTo(34));
            Assert.That(a.Cards.GetHand(PlayerSide.US).Concat(a.Cards.GetHand(PlayerSide.USSR)).Concat(a.Cards.DrawPile).All(id=>catalog.GetCard(id).Era==GameEra.Early),Is.True);
        }
        [Test] public void Exact_exhaustion_is_allowed_but_insufficient_cards_or_random_failure_leave_source_unchanged()
        {
            var catalog=CardZonesTests.Catalog();var random=Pcg32Random.FromSeed(0,0);var before=random.Capture();
            Assert.That(InitialDealService.Create(catalog,Definition(2),random).Cards.DrawPile,Is.Empty);
            Assert.That(()=>InitialDealService.Create(catalog,Definition(3),random),Throws.InstanceOf<ArgumentException>());Pcg32RandomTests.Same(before,random.Capture());
            random=new Pcg32Random(new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,3,1,ulong.MaxValue-1));before=random.Capture();
            Assert.Throws<OverflowException>(()=>InitialDealService.Create(catalog,Definition(),random));Pcg32RandomTests.Same(before,random.Capture());
        }
        [Test] public void Initial_definition_and_creation_fail_closed_for_invalid_inputs()
        {
            var def=Definition();var c=CardZonesTests.Catalog();var rng=Pcg32Random.FromSeed(1,1);
            Assert.That(()=>new InitialDealDefinition(null,def.China),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new InitialDealDefinition(def.Turn,null),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new InitialDealDefinition(def.Turn,new ChinaCardState(def.China.Id,PlayerSide.US,CardFace.FaceUp)),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>new InitialDealDefinition(def.Turn,new ChinaCardState(def.China.Id,PlayerSide.USSR,CardFace.FaceDown)),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>InitialDealService.Create(null,def,rng),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>InitialDealService.Create(c,null,rng),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>InitialDealService.Create(c,def,null),Throws.InstanceOf<ArgumentException>());
            var noChina=new CardCatalog(c.Cards.Where(x=>x.Kind!=CardKind.China));Assert.That(()=>InitialDealService.Create(noChina,def,rng),Throws.InstanceOf<ArgumentException>());
        }
    }
}
