using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class FiveYearPlanRulesTests
    {
        internal static CardId Id(string n)=>new CardId("card."+n);
        internal static FiveYearPlanDefinition Definition(CardCatalog catalog=null)=>new FiveYearPlanDefinition(catalog??CardCatalogLoaderTests.Load(),Id("five_year_plan"),"effect.five_year_plan",PlayerSide.USSR,CardAffiliation.US,1);
        internal static RandomSnapshot Random()=>Pcg32Random.FromSeed(42,54).Capture();
        internal static CardZones Zones(CardCatalog catalog,string[] su,string[] us=null,string[] discard=null,string[] removed=null,string reserved=null,bool parentResolving=true,string[] extraResolving=null)
        {
            var root=Id("five_year_plan");var usCards=(us??Array.Empty<string>()).Select(Id).Concat(parentResolving?Array.Empty<CardId>():new[]{root}).ToArray();
            var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,usCards},{PlayerSide.USSR,su.Select(Id).ToArray()}};
            var discarded=(discard??Array.Empty<string>()).Select(Id).ToArray();var removedCards=(removed??Array.Empty<string>()).Select(Id).ToArray();var resolving=(parentResolving?new[]{root}:Array.Empty<CardId>()).Concat((extraResolving??Array.Empty<string>()).Select(Id)).ToArray();
            var headlines=new Dictionary<PlayerSide,CardId>();if(reserved!=null)headlines.Add(PlayerSide.USSR,Id(reserved));
            var used=hands.Values.SelectMany(x=>x).Concat(discarded).Concat(removedCards).Concat(resolving).Concat(headlines.Values).ToHashSet();
            return new CardZones(catalog,catalog.Cards.Where(c=>c.Kind!=CardKind.China && !used.Contains(c.Id)).Select(c=>c.Id),hands,Array.Empty<CardId>(),discarded,removedCards,resolving,
                new ChinaCardState(Id("china"),PlayerSide.USSR,CardFace.FaceUp),new[]{GameEra.Early,GameEra.Mid,GameEra.Late},headlines);
        }
        static IReadOnlyCollection<CardId> Supported(CardCatalog catalog)=>catalog.Cards.Where(c=>c.Kind==CardKind.Event && c.Affiliation==CardAffiliation.US).Select(c=>c.Id).ToArray();
        [Test] public void Definition_binds_exact_catalog_and_printed_source()
        {
            var cards=CardCatalogLoaderTests.Load();var d=Definition(cards);Assert.That(d.Cards,Is.SameAs(cards));Assert.That(d.Card,Is.SameAs(cards.GetCard(Id("five_year_plan"))));Assert.That(d.HandOwner,Is.EqualTo(PlayerSide.USSR));Assert.That(d.TriggeredAffiliation,Is.EqualTo(CardAffiliation.US));Assert.That(d.DiscardCount,Is.EqualTo(1));
        }
        [TestCase("owner")][TestCase("affiliation")][TestCase("count")][TestCase("effect")][TestCase("source")]
        public void Definition_rejects_mismatched_bindings_and_semantics(string kind)
        {
            var c=CardCatalogLoaderTests.Load();Assert.Throws<ArgumentException>(()=>new FiveYearPlanDefinition(c,Id(kind=="source"?"marshall_plan":"five_year_plan"),kind=="effect"?"effect.marshall_plan":"effect.five_year_plan",kind=="owner"?PlayerSide.US:PlayerSide.USSR,kind=="affiliation"?CardAffiliation.Neutral:CardAffiliation.US,kind=="count"?2:1));
        }
        [Test] public void Empty_hand_keeps_zones_and_random_without_selecting()
        {
            var d=Definition();var cards=Zones(d.Cards,Array.Empty<string>(),us:new[]{"marshall_plan"},reserved:"truman_doctrine");var random=Random();var p=FiveYearPlanRules.Plan(d,cards,random,Array.Empty<CardId>());
            Assert.That(p.Route,Is.EqualTo(FiveYearPlanRoute.EmptyHand));Assert.That(p.SelectedCard,Is.Null);Assert.That(p.Before,Is.SameAs(cards));Assert.That(p.Cards,Is.SameAs(cards));Assert.That(p.RandomBefore,Is.SameAs(random));Pcg32RandomTests.Same(p.RandomAfter,random);
        }
        [TestCase("asia_scoring")][TestCase("olympic_games")][TestCase("blockade")]
        public void Non_US_singleton_is_discarded_without_event_or_parent_move(string name)
        {
            var d=Definition();var cards=Zones(d.Cards,new[]{name},us:new[]{"marshall_plan"},discard:new[]{"fidel"},removed:new[]{"containment"},reserved:"truman_doctrine");var random=Random();var p=FiveYearPlanRules.Plan(d,cards,random,Array.Empty<CardId>());
            Assert.That(p.Route,Is.EqualTo(FiveYearPlanRoute.Discarded));Assert.That(p.SelectedCard,Is.EqualTo(Id(name)));Assert.That(p.Cards.GetHand(PlayerSide.USSR),Is.Empty);Assert.That(p.Cards.DiscardPile,Is.EqualTo(new[]{Id("fidel"),Id(name)}));Assert.That(p.Cards.ResolvingCards,Is.EqualTo(new[]{d.Card.Id}));
            Assert.That(p.Cards.DrawPile,Is.EqualTo(cards.DrawPile));Assert.That(p.Cards.GetHand(PlayerSide.US),Is.EqualTo(cards.GetHand(PlayerSide.US)));Assert.That(p.Cards.RemovedCards,Is.EqualTo(cards.RemovedCards));Assert.That(p.Cards.HeadlineReserved,Is.EquivalentTo(cards.HeadlineReserved));Assert.That(p.Cards.China,Is.SameAs(cards.China));
            Assert.That(p.RandomAfter.DrawCount,Is.EqualTo(random.DrawCount+1));Pcg32RandomTests.Same(random,Random());Assert.That(cards.GetHand(PlayerSide.USSR),Is.EqualTo(new[]{Id(name)}));
        }
        [TestCase("marshall_plan")][TestCase("independent_reds")]
        public void US_child_moves_to_resolving_without_already_discarding_or_removing_it(string name)
        {
            var d=Definition();var cards=Zones(d.Cards,new[]{name});var p=FiveYearPlanRules.Plan(d,cards,Random(),new[]{Id(name)});
            Assert.That(p.Route,Is.EqualTo(FiveYearPlanRoute.AwaitingUsEvent));Assert.That(p.SelectedCard,Is.EqualTo(Id(name)));Assert.That(p.Cards.ResolvingCards,Is.EqualTo(new[]{d.Card.Id,Id(name)}));Assert.That(p.Cards.DiscardPile,Is.Empty);Assert.That(p.Cards.RemovedCards,Is.Empty);Assert.That(p.Cards.GetHand(PlayerSide.USSR),Is.Empty);Assert.That(p.RandomAfter.DrawCount,Is.EqualTo(1));
        }
        [Test] public void Stable_card_order_matches_bounded_PCG_and_does_not_depend_on_hand_order()
        {
            var d=Definition();var first=new[]{"truman_doctrine","asia_scoring","fidel","olympic_games"};var a=Zones(d.Cards,first);var b=Zones(d.Cards,first.Reverse().ToArray());var random=Random();var expected=new Pcg32Random(random);var ordered=first.Select(Id).OrderBy(x=>x.Value,StringComparer.Ordinal).ToArray();var selected=ordered[expected.NextInt(0,ordered.Length)];
            var p=FiveYearPlanRules.Plan(d,a,random,Supported(d.Cards));var q=FiveYearPlanRules.Plan(d,b,random,Supported(d.Cards));Assert.That(p.SelectedCard,Is.EqualTo(selected));Assert.That(q.SelectedCard,Is.EqualTo(selected));Pcg32RandomTests.Same(p.RandomAfter,expected.Capture());Pcg32RandomTests.Same(p.RandomAfter,q.RandomAfter);Pcg32RandomTests.Same(random,Random());
        }
        [Test] public void Every_US_candidate_requires_support_before_random_even_if_other_card_would_be_chosen()
        {
            var d=Definition();var cards=Zones(d.Cards,new[]{"asia_scoring","truman_doctrine"});RandomSnapshot random=null;
            for(ulong seed=0;seed<100;seed++){var r=Pcg32Random.FromSeed(seed,54);var before=r.Capture();if(r.NextInt(0,2)==0){random=before;break;}}
            Assert.That(random,Is.Not.Null);var saved=random;Assert.Throws<NotSupportedException>(()=>FiveYearPlanRules.Plan(d,cards,random,Array.Empty<CardId>()));Pcg32RandomTests.Same(random,saved);Assert.That(cards.GetHand(PlayerSide.USSR).Count,Is.EqualTo(2));
        }
        [TestCase("unknown")][TestCase("non-us")][TestCase("duplicate")]
        public void Invalid_authority_capability_sets_are_rejected_even_when_hand_is_empty(string kind)
        {
            var d=Definition();var cards=Zones(d.Cards,Array.Empty<string>());var set=kind=="duplicate"?new[]{Id("marshall_plan"),Id("marshall_plan")}:new[]{Id(kind=="unknown"?"unknown":"olympic_games")};Assert.Throws<ArgumentException>(()=>FiveYearPlanRules.Plan(d,cards,Random(),set));
        }
        [TestCase(false)][TestCase(true)] public void Missing_or_nonexclusive_parent_source_is_rejected(bool extra)
        {
            var d=Definition();var cards=Zones(d.Cards,new[]{"asia_scoring"},parentResolving:extra,extraResolving:extra?new[]{"fidel"}:null);Assert.Throws<ArgumentException>(()=>FiveYearPlanRules.Plan(d,cards,Random(),Array.Empty<CardId>()));
        }
        [Test] public void Different_catalog_reference_is_rejected_without_random_or_zone_change()
        {
            var d=Definition();var cards=Zones(CardCatalogLoaderTests.Load(),new[]{"asia_scoring"});var random=Random();Assert.Throws<ArgumentException>(()=>FiveYearPlanRules.Plan(d,cards,random,Array.Empty<CardId>()));Assert.That(cards.GetHand(PlayerSide.USSR).Count,Is.EqualTo(1));Pcg32RandomTests.Same(random,Random());
        }
        [Test] public void Exhausted_random_rejects_without_moving_the_card()
        {
            var d=Definition();var cards=Zones(d.Cards,new[]{"asia_scoring"});var seed=Random();var exhausted=new RandomSnapshot(seed.AlgorithmId,seed.State,seed.Increment,ulong.MaxValue);Assert.Throws<OverflowException>(()=>FiveYearPlanRules.Plan(d,cards,exhausted,Array.Empty<CardId>()));Assert.That(cards.GetHand(PlayerSide.USSR).Count,Is.EqualTo(1));Assert.That(cards.DiscardPile,Is.Empty);Assert.That(exhausted.DrawCount,Is.EqualTo(ulong.MaxValue));
        }
    }
}
