using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class TurnDealTests
    {
        static CardId Id(string n)=>CardZonesTests.Id(n);
        static CardId[] Ids(params string[] names)=>names.Select(Id).ToArray();
        static TurnCalendar Calendar(int early=2,int mid=2,int late=2)=>new TurnCalendar(new[]{
            new EraTurnParameters(GameEra.Early,1,3,early,6),new EraTurnParameters(GameEra.Mid,4,7,mid,7),new EraTurnParameters(GameEra.Late,8,10,late,7)});
        static CardZones Zones(string[] draw,string[] us,string[] su,string[] discard=null,string[] removed=null,string[] resolving=null,bool mid=false)=>new CardZones(
            CardZonesTests.Catalog(),Ids(draw),new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,Ids(us)},{PlayerSide.USSR,Ids(su)}},
            Ids(mid?new[]{"z"}:new[]{"m","z"}),Ids(discard??Array.Empty<string>()),Ids(removed??Array.Empty<string>()),Ids(resolving??Array.Empty<string>()),
            new ChinaCardState(Id("china"),PlayerSide.US,CardFace.FaceDown),mid?new[]{GameEra.Early,GameEra.Mid}:new[]{GameEra.Early});
        sealed class ZeroRandom:IRandomProvider
        {
            ulong count;
            public int NextInt(int min,int max){Assert.That(min,Is.Zero);Assert.That(max,Is.GreaterThan(1));count++;return 0;}
            public RandomSnapshot Capture()=>new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,count,1,count);
            public IRandomProvider Fork()=>new ZeroRandom{count=count};
        }
        static TurnDealPlan Deal(CardZones z,int turn=2,TurnCalendar c=null,IRandomProvider rng=null)=>TurnDealService.Plan(CardZonesTests.Catalog(),z,(c??Calendar()).ForTurn(turn),rng??new ZeroRandom());
        [Test] public void Existing_draw_is_dealt_from_front_skipping_full_hand_without_rng_or_china_change()
        {
            var z=Zones(new[]{"c","d"},new[]{"a","b"},Array.Empty<string>());var rng=new ZeroRandom();var p=Deal(z,rng:rng);
            Assert.That(p.Cards.GetHand(PlayerSide.US),Is.EqualTo(Ids("a","b")));Assert.That(p.Cards.GetHand(PlayerSide.USSR),Is.EqualTo(Ids("c","d")));
            Assert.That(p.UsDealt,Is.Zero);Assert.That(p.UssrDealt,Is.EqualTo(2));Assert.That(p.DiscardReshuffles,Is.Zero);Assert.That(p.EraAdded,Is.False);
            Assert.That(p.Cards.China,Is.SameAs(z.China));Assert.That(p.Before,Is.SameAs(z));Assert.That(p.Cards.DrawPile,Is.Empty);Assert.That(p.Random.DrawCount,Is.Zero);
            Assert.That(z.DrawPile,Is.EqualTo(Ids("c","d")));Pcg32RandomTests.Same(rng.Capture(),p.Random);
        }
        [Test] public void Old_deck_exhausts_before_discard_shuffle_and_deal_alternates_ussr_then_us()
        {
            var z=Zones(new[]{"a"},Array.Empty<string>(),Array.Empty<string>(),new[]{"b","c","d"});var rng=new ZeroRandom();var p=Deal(z,rng:rng);
            Assert.That(p.Cards.GetHand(PlayerSide.USSR),Is.EqualTo(Ids("a","d")));Assert.That(p.Cards.GetHand(PlayerSide.US),Is.EqualTo(Ids("c","b")));
            Assert.That(p.Cards.DiscardPile,Is.Empty);Assert.That(p.DiscardReshuffles,Is.EqualTo(1));Assert.That(p.Random.DrawCount,Is.EqualTo(2));Assert.That(rng.Capture().DrawCount,Is.Zero);
        }
        [Test] public void Mid_era_shuffles_new_reserve_with_remaining_draw_and_keeps_discards_until_needed()
        {
            var z=Zones(new[]{"a"},new[]{"b"},new[]{"c"},new[]{"d"});var p=Deal(z,4);
            Assert.That(p.Cards.GetHand(PlayerSide.USSR),Is.EqualTo(Ids("c","m")));Assert.That(p.Cards.GetHand(PlayerSide.US),Is.EqualTo(Ids("b","a")));
            Assert.That(p.Cards.DiscardPile,Is.EqualTo(Ids("d")));Assert.That(p.Cards.EraReserve,Is.EqualTo(Ids("z")));
            Assert.That(p.Cards.EnteredEras,Is.EqualTo(new[]{GameEra.Early,GameEra.Mid}));Assert.That(p.EraAdded,Is.True);Assert.That(p.DiscardReshuffles,Is.Zero);Assert.That(p.Random.DrawCount,Is.EqualTo(1));
            var retry=Deal(p.Cards,4,rng:new Pcg32Random(p.Random));Assert.That(retry.EraAdded,Is.False);Assert.That(retry.UsDealt+retry.UssrDealt,Is.Zero);Pcg32RandomTests.Same(p.Random,retry.Random);
        }
        [Test] public void Late_era_adds_only_late_and_ignores_removed_cards()
        {
            var z=Zones(new[]{"a"},new[]{"b"},new[]{"c"},new[]{"d"},new[]{"m"},mid:true);var p=Deal(z,8);
            Assert.That(p.Cards.GetHand(PlayerSide.USSR),Is.EqualTo(Ids("c","z")));Assert.That(p.Cards.GetHand(PlayerSide.US),Is.EqualTo(Ids("b","a")));
            Assert.That(p.Cards.RemovedCards,Is.EqualTo(Ids("m")));Assert.That(p.Cards.DiscardPile,Is.EqualTo(Ids("d")));Assert.That(p.Cards.EraReserve,Is.Empty);
        }
        [Test] public void Era_shuffle_happens_even_with_full_hands_but_not_again_on_retry()
        {
            var z=Zones(new[]{"a"},new[]{"b"},new[]{"c"},new[]{"d"});var p=Deal(z,4,Calendar(1,1));
            Assert.That(p.Cards.DrawPile,Is.EqualTo(Ids("m","a")));Assert.That(p.UsDealt+p.UssrDealt,Is.Zero);Assert.That(p.EraAdded,Is.True);
            var q=Deal(p.Cards,4,Calendar(1,1),new Pcg32Random(p.Random));Assert.That(q.Cards.DrawPile,Is.EqualTo(p.Cards.DrawPile));Pcg32RandomTests.Same(q.Random,p.Random);
        }
        [Test] public void Era_draw_can_then_exhaust_and_reshuffle_discard_without_removed_or_future_cards()
        {
            var z=Zones(new[]{"a"},Array.Empty<string>(),Array.Empty<string>(),new[]{"b","c"},new[]{"d"});var p=Deal(z,4);
            Assert.That(p.Cards.GetHand(PlayerSide.USSR),Is.EqualTo(Ids("m","c")));Assert.That(p.Cards.GetHand(PlayerSide.US),Is.EqualTo(Ids("a","b")));
            Assert.That(p.DiscardReshuffles,Is.EqualTo(1));Assert.That(p.Cards.RemovedCards,Is.EqualTo(Ids("d")));Assert.That(p.Cards.EraReserve,Is.EqualTo(Ids("z")));
        }
        [Test] public void Above_target_hand_is_preserved_and_full_turn_one_needs_no_second_deal()
        {
            var z=Zones(new[]{"d"},new[]{"a","b"},new[]{"c"});var p=Deal(z,1,Calendar(1));
            Assert.That(p.UsDealt+p.UssrDealt,Is.Zero);Assert.That(p.Cards.DrawPile,Is.EqualTo(z.DrawPile));Assert.That(p.Cards.GetHand(PlayerSide.US),Is.EqualTo(z.GetHand(PlayerSide.US)));Assert.That(p.Random.DrawCount,Is.Zero);
        }
        [TestCase(5)] [TestCase(8)] public void Unentered_era_outside_first_turn_or_skipping_era_is_rejected(int turn)
        {Assert.That(()=>Deal(Zones(new[]{"a","d"},new[]{"b"},new[]{"c"}),turn),Throws.InstanceOf<ArgumentException>());}
        [Test] public void Future_entered_era_and_pending_resolving_are_rejected()
        {
            var future=Zones(new[]{"a","m"},new[]{"b"},new[]{"c"},new[]{"d"},mid:true);Assert.That(()=>Deal(future),Throws.InstanceOf<ArgumentException>());
            var pending=Zones(new[]{"a"},new[]{"b"},new[]{"c"},resolving:new[]{"d"});Assert.That(()=>Deal(pending),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Insufficient_draw_and_discard_and_huge_target_reject_without_modifying_inputs()
        {
            var z=Zones(new[]{"a"},new[]{"b"},new[]{"c"},removed:new[]{"d"});var rng=Pcg32Random.FromSeed(1,1);var before=rng.Capture();
            Assert.That(()=>Deal(z,rng:rng),Throws.InstanceOf<ArgumentException>());Assert.That(()=>Deal(z,c:Calendar(int.MaxValue),rng:rng),Throws.InstanceOf<ArgumentException>());
            Assert.That(z.DrawPile,Is.EqualTo(Ids("a")));Pcg32RandomTests.Same(before,rng.Capture());
        }
        [Test] public void Random_exhaustion_rejects_whole_era_plan_without_mutating_source()
        {
            var z=Zones(new[]{"a"},new[]{"b"},new[]{"c"},new[]{"d"});var rng=new Pcg32Random(new RandomSnapshot(RandomSnapshot.SupportedAlgorithm,3,1,ulong.MaxValue));var before=rng.Capture();
            Assert.Throws<OverflowException>(()=>Deal(z,4,rng:rng));Pcg32RandomTests.Same(before,rng.Capture());Assert.That(z.EraReserve,Is.EqualTo(Ids("m","z")));
        }
        [Test] public void Null_inputs_and_incompatible_catalog_are_rejected()
        {
            var c=CardZonesTests.Catalog();var z=CardZonesTests.Zones();var t=Calendar().ForTurn(2);var rng=new ZeroRandom();
            Assert.That(()=>TurnDealService.Plan(null,z,t,rng),Throws.InstanceOf<ArgumentException>());Assert.That(()=>TurnDealService.Plan(c,null,t,rng),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>TurnDealService.Plan(c,z,null,rng),Throws.InstanceOf<ArgumentException>());Assert.That(()=>TurnDealService.Plan(c,z,t,null),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>TurnDealService.Plan(new CardCatalog(c.Cards.Where(x=>!x.Id.Equals(Id("d")))),z,t,rng),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Actual_103_card_catalog_refills_to_calendar_target_and_replays_same_random_snapshot()
        {
            var catalog=CardCatalogLoaderTests.Load();var initial=InitialDealService.Create(catalog,InitialDealTests.Definition(8),Pcg32Random.FromSeed(42,54));var z=initial.Cards;
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,z.GetHand(PlayerSide.US).Take(2).ToArray()},{PlayerSide.USSR,z.GetHand(PlayerSide.USSR).Take(3).ToArray()}};
            var discard=z.GetHand(PlayerSide.US).Skip(2).Concat(z.GetHand(PlayerSide.USSR).Skip(3));
            z=new CardZones(catalog,z.DrawPile,hands,z.EraReserve,discard,z.RemovedCards,z.ResolvingCards,z.China,z.EnteredEras);
            var t=Calendar(8,9,9).ForTurn(2);var rng=new Pcg32Random(initial.Random);var a=TurnDealService.Plan(catalog,z,t,rng);var b=TurnDealService.Plan(catalog,z,t,new Pcg32Random(initial.Random));
            Assert.That(a.Cards.GetHand(PlayerSide.US),Has.Count.EqualTo(8));Assert.That(a.Cards.GetHand(PlayerSide.USSR),Has.Count.EqualTo(8));Assert.That(a.Cards.DrawPile,Has.Count.EqualTo(8));Assert.That(a.Cards.DiscardPile,Has.Count.EqualTo(11));
            Assert.That(a.Cards.GetHand(PlayerSide.US),Is.EqualTo(b.Cards.GetHand(PlayerSide.US)));Assert.That(a.Cards.GetHand(PlayerSide.USSR),Is.EqualTo(b.Cards.GetHand(PlayerSide.USSR)));Pcg32RandomTests.Same(a.Random,b.Random);Pcg32RandomTests.Same(initial.Random,rng.Capture());
        }
    }
}

