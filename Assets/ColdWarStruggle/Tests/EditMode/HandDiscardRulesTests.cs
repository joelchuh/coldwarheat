using System;
using System.Collections.Generic;
using System.Linq;
using ColdWarStruggle.Core;
using NUnit.Framework;
namespace ColdWarStruggle.Tests
{
    public class HandDiscardRulesTests
    {
        static CardId Id(string name)=>new CardId("card."+name);
        internal static CardZones Zones(bool empty=false)
        {
            var cards=new List<CardDefinition>();
            foreach(var name in new[]{"a","b","c","opponent","headline","resolving","discarded","removed","reserve","draw"})
                cards.Add(new CardDefinition(Id(name),cards.Count+1,"name","rules",CardKind.Event,name=="a"?2:name=="b"?3:4,
                    CardAffiliation.US,name=="reserve"?GameEra.Mid:GameEra.Early,CardDeckEntry.EraDeck,true,false,null,CardCatalogTests.Effect()));
            cards.Add(new CardDefinition(Id("score"),11,"name","rules",CardKind.Scoring,null,CardAffiliation.None,GameEra.Early,CardDeckEntry.EraDeck,false,false,"region.asia",CardCatalogTests.Effect()));
            cards.Add(new CardDefinition(Id("china"),12,"name","rules",CardKind.China,4,CardAffiliation.None,null,CardDeckEntry.SpecialChina,false,false,null,null));
            var us=new[]{Id("c"),Id("a"),Id("score"),Id("b")};
            return new CardZones(new CardCatalog(cards),empty?us.Append(Id("draw")):new[]{Id("draw")},
                new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,empty?Array.Empty<CardId>():us},{PlayerSide.USSR,new[]{Id("opponent")}}},
                new[]{Id("reserve")},new[]{Id("discarded")},new[]{Id("removed")},new[]{Id("resolving")},
                new ChinaCardState(Id("china"),PlayerSide.US,CardFace.FaceUp),new[]{GameEra.Early},new Dictionary<PlayerSide,CardId>{{PlayerSide.US,Id("headline")}});
        }
        [TestCase(0,"b,c")][TestCase(1,"a,b,c")][TestCase(-1,"c")][TestCase(-9,"")]
        public void Threshold_uses_owners_effective_ops_and_only_ordinary_hand_events(int delta,string expected)
        {
            var zones=Zones();var ops=OpsGeneralTests.Definition();
            var quotes=HandDiscardRules.QualifyingCards(zones,PlayerSide.US,ops,3,new[]{new OpsFlatModifier("own",PlayerSide.US,delta),new OpsFlatModifier("other",PlayerSide.USSR,99)});
            Assert.That(string.Join(",",quotes.Select(q=>q.Card.Id.Value.Substring(5))),Is.EqualTo(expected));
            foreach(var q in quotes){Assert.That(q.Purpose,Is.EqualTo(OpsValuePurpose.OpsThreshold));Assert.That(q.Actor,Is.EqualTo(PlayerSide.US));Assert.That(q.Definition,Is.SameAs(ops));Assert.That(q.Card,Is.SameAs(zones.Catalog.GetCard(q.Card.Id)));Assert.That(q.GeneralValue,Is.GreaterThanOrEqualTo(3));}
            Assert.That(zones.GetHand(PlayerSide.US).Count,Is.EqualTo(4));
            Assert.Throws<NotSupportedException>(()=>((IList<OpsGeneralQuote>)quotes).Clear());
        }
        [Test] public void Combined_modifiers_are_evaluated_once_and_threshold_is_data_driven()
        {
            var source=new List<OpsFlatModifier>{new OpsFlatModifier("purge",PlayerSide.US,-1),new OpsFlatModifier("containment",PlayerSide.US,1)};
            var quotes=HandDiscardRules.QualifyingCards(Zones(),PlayerSide.US,OpsGeneralTests.Definition(),4,source);source.Clear();
            Assert.That(quotes.Select(q=>q.Card.Id),Is.EqualTo(new[]{Id("c")}));Assert.That(quotes[0].AppliedModifiers.Count,Is.EqualTo(2));
            var other=HandDiscardRules.QualifyingCards(Zones(),PlayerSide.USSR,OpsGeneralTests.Definition(),3,Array.Empty<OpsFlatModifier>());
            Assert.That(other.Select(q=>q.Card.Id),Is.EqualTo(new[]{Id("opponent")}));
        }
        [TestCase(false)][TestCase(true)] public void Invalid_context_is_rejected_even_without_candidates(bool empty)
        {
            var z=Zones(empty);var d=OpsGeneralTests.Definition();var mods=Array.Empty<OpsFlatModifier>();var m=new OpsFlatModifier("x",PlayerSide.US,1);
            Assert.That(()=>HandDiscardRules.QualifyingCards(null,PlayerSide.US,d,3,mods),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>HandDiscardRules.QualifyingCards(z,(PlayerSide)99,d,3,mods),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>HandDiscardRules.QualifyingCards(z,PlayerSide.US,null,3,mods),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>HandDiscardRules.QualifyingCards(z,PlayerSide.US,d,0,mods),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>HandDiscardRules.QualifyingCards(z,PlayerSide.US,d,5,mods),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>HandDiscardRules.QualifyingCards(z,PlayerSide.US,d,3,null),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>HandDiscardRules.QualifyingCards(z,PlayerSide.US,d,3,new[]{m,m}),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>HandDiscardRules.QualifyingCards(z,PlayerSide.US,d,3,new OpsFlatModifier[]{null}),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>HandDiscardRules.QualifyingCards(z,PlayerSide.US,d,3,Enumerable.Range(0,129).Select(i=>new OpsFlatModifier("m"+i,PlayerSide.US,0))),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>HandDiscardRules.QualifyingCards(z,PlayerSide.US,new OpsGeneralDefinition(1,4,new[]{OpsValuePurpose.Coup}),3,mods),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Empty_hand_has_no_candidates_but_China_and_reserved_card_do_not_help()
        {Assert.That(HandDiscardRules.QualifyingCards(Zones(true),PlayerSide.US,OpsGeneralTests.Definition(),3,Array.Empty<OpsFlatModifier>()),Is.Empty);}
        [Test] public void Scoring_only_hand_has_no_candidates_and_still_checks_modifier_integrity()
        {
            var old=Zones();var hands=new Dictionary<PlayerSide,IReadOnlyList<CardId>>{{PlayerSide.US,new[]{Id("score")}},{PlayerSide.USSR,old.GetHand(PlayerSide.USSR)}};
            var cards=new CardZones(old.Catalog,old.DrawPile.Concat(old.GetHand(PlayerSide.US).Where(id=>!id.Equals(Id("score")))),hands,
                old.EraReserve,old.DiscardPile,old.RemovedCards,old.ResolvingCards,old.China,old.EnteredEras,old.HeadlineReserved);
            Assert.That(HandDiscardRules.QualifyingCards(cards,PlayerSide.US,OpsGeneralTests.Definition(),3,Array.Empty<OpsFlatModifier>()),Is.Empty);
            var m=new OpsFlatModifier("duplicate",PlayerSide.US,1);
            Assert.That(()=>HandDiscardRules.QualifyingCards(cards,PlayerSide.US,OpsGeneralTests.Definition(),3,new[]{m,m}),Throws.InstanceOf<ArgumentException>());
        }
        [Test] public void Discard_starred_event_preserves_all_other_zones_and_source_and_is_not_removal()
        {
            var before=Zones();var after=HandDiscardRules.DiscardEventCard(before,PlayerSide.US,Id("b"));
            Assert.That(after,Is.Not.SameAs(before));Assert.That(after.Catalog,Is.SameAs(before.Catalog));
            Assert.That(after.GetHand(PlayerSide.US),Is.EqualTo(before.GetHand(PlayerSide.US).Where(id=>!id.Equals(Id("b")))));
            Assert.That(after.DiscardPile,Is.EqualTo(before.DiscardPile.Append(Id("b"))));Assert.That(before.GetHand(PlayerSide.US),Has.Member(Id("b")));Assert.That(before.DiscardPile,Has.No.Member(Id("b")));
            Assert.That(after.RemovedCards,Is.EqualTo(before.RemovedCards));Assert.That(after.ResolvingCards,Is.EqualTo(before.ResolvingCards));Assert.That(after.HeadlineReserved,Is.EqualTo(before.HeadlineReserved));
            Assert.That(after.GetHand(PlayerSide.USSR),Is.EqualTo(before.GetHand(PlayerSide.USSR)));Assert.That(after.China,Is.SameAs(before.China));Assert.That(after.EnteredEras,Is.EqualTo(before.EnteredEras));Assert.That(after.DrawPile,Is.EqualTo(before.DrawPile));Assert.That(after.EraReserve,Is.EqualTo(before.EraReserve));
            Assert.That(()=>HandDiscardRules.DiscardEventCard(after,PlayerSide.US,Id("b")),Throws.InstanceOf<ArgumentException>());
        }
        [TestCase("score")][TestCase("china")][TestCase("opponent")][TestCase("headline")][TestCase("resolving")][TestCase("discarded")][TestCase("removed")][TestCase("reserve")][TestCase("draw")][TestCase("unknown")]
        public void Wrong_selection_is_rejected_without_fallback(string name)
        {var z=Zones();Assert.That(()=>HandDiscardRules.DiscardEventCard(z,PlayerSide.US,Id(name)),Throws.InstanceOf<ArgumentException>());Assert.That(z.DiscardPile.Count,Is.EqualTo(1));Assert.That(z.GetHand(PlayerSide.US).Count,Is.EqualTo(4));}
        [Test] public void Invalid_discard_owner_and_missing_context_are_rejected()
        {
            Assert.That(()=>HandDiscardRules.DiscardEventCard(null,PlayerSide.US,Id("a")),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>HandDiscardRules.DiscardEventCard(Zones(),(PlayerSide)99,Id("a")),Throws.InstanceOf<ArgumentException>());
            Assert.That(()=>HandDiscardRules.DiscardEventCard(Zones(),PlayerSide.US,default),Throws.InstanceOf<ArgumentException>());
        }
    }
}
